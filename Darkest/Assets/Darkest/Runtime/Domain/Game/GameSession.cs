using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Commands;
using Darkest.Domain.Combat.Events;
using Darkest.Domain.Combat.Flow;
using Darkest.Domain.Content;
using Darkest.Domain.Core;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Hamlet;
using Darkest.Domain.Save;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Game
{
    public enum GameMode
    {
        Town,
        Exploring,
        Battle,
        ExpeditionResult
    }

    public sealed class GameModeChanged : IDomainEvent
    {
        public GameModeChanged(GameMode mode) { Mode = mode; }
        public GameMode Mode { get; }
        public override string ToString() => "GameMode " + Mode;
    }

    /// <summary>
    /// 게임 전체 흐름 Facade: 마을 ↔ 탐험 ↔ 전투 전환. 각 하위 시스템은 서로를 모르고 이 클래스가 연결한다.
    /// Presenter는 이 객체 하나와 IEventBus만 알면 된다.
    /// </summary>
    public sealed class GameSession
    {
        private const string LogCategory = "Game";

        private readonly GameConfig _config;
        private readonly IRandom _random;
        private readonly IGameLogger _logger;
        private readonly DungeonGenerator _dungeonGenerator;
        private readonly CombatantFactory _factory;
        private readonly SaveMapper _saveMapper;
        private readonly ExpeditionSaveMapper _expeditionSaveMapper;
        private readonly BattleServices _battleServices;

        private GameSession(GameConfig config, IContentCatalog catalog, IRandom random, IEventBus events,
            IGameLogger logger, GameSaveData save)
        {
            _config = Guard.NotNull(config, nameof(config));
            Catalog = Guard.NotNull(catalog, nameof(catalog));
            _random = Guard.NotNull(random, nameof(random));
            Events = Guard.NotNull(events, nameof(events));
            _logger = logger ?? NullLogger.Instance;

            Stress = new StressService(config.Stress, catalog.Afflictions, catalog.Virtues, random, events, _logger);
            _factory = new CombatantFactory(catalog, new SequentialIdGenerator(), config.Stress.MaxStress);
            _saveMapper = new SaveMapper(catalog, _factory, Stress, _logger);
            _expeditionSaveMapper = new ExpeditionSaveMapper(config.Expedition, config.Combat, Stress, catalog, random, events, _logger);
            _dungeonGenerator = new DungeonGenerator(catalog, config.Dungeon);
            _battleServices = new BattleServices(random, events, _logger, config.Combat, Stress);

            var town = save == null ? new Town(config.Town.StartingGold, 1) : _saveMapper.Restore(save);
            TownService = new TownService(town, config.Town, Stress, _factory, random, events);
            if (save == null) TownService.SetupNewGame();
            else TownService.RefreshRecruits();
            if (save?.Expedition != null) ResumeExpedition(save.Expedition);

            _logger.Info(LogCategory, (save == null ? "새 게임" : "불러오기") + " seed=" + random.Seed);
        }

        public static GameSession CreateNew(GameConfig config, IContentCatalog catalog, IRandom random,
            IEventBus events, IGameLogger logger) => new GameSession(config, catalog, random, events, logger, null);

        public static GameSession Load(GameConfig config, IContentCatalog catalog, IRandom random,
            IEventBus events, IGameLogger logger, GameSaveData save) =>
            new GameSession(config, catalog, random, events, logger, Guard.NotNull(save, nameof(save)));

        public IContentCatalog Catalog { get; }
        public IEventBus Events { get; }
        public StressService Stress { get; }
        public TownService TownService { get; }
        public Town Town => TownService.Town;
        public GameMode Mode { get; private set; } = GameMode.Town;
        public Expedition Expedition { get; private set; }
        /// <summary>
        /// 진행 중이거나 막 끝난 전투. 전투가 끝나도 ContinueAfterBattle 전까지는 Battle 모드에 머문다
        /// (마지막 일격 연출과 결과 표시를 화면이 마칠 수 있게).
        /// </summary>
        public BattleSession Battle { get; private set; }

        /// <summary>막 끝난 전투의 결과. 전투 중이거나 전투가 없으면 null.</summary>
        public BattleSummary LastBattleSummary { get; private set; }

        public bool IsBattleConcluded => Mode == GameMode.Battle && LastBattleSummary != null;
        public int Seed => _random.Seed;

        public string TryEmbark(IReadOnlyList<Hero> party, Provisions provisions)
        {
            if (Mode != GameMode.Town) return "마을에서만 출발할 수 있습니다.";
            string error = ValidateParty(party);
            if (error != null) return error;

            error = TownService.TryBuyProvisions(provisions, out var inventory);
            if (error != null) return error;

            var formation = new Formation(Team.Heroes);
            for (int i = 0; i < party.Count; i++) formation.Add(party[i]);

            var map = _dungeonGenerator.Generate(_random);
            Expedition = new Expedition(formation, map, inventory, _config.Expedition, _config.Combat, Stress, Catalog, _random, Events, _logger);
            SetMode(GameMode.Exploring);
            return null;
        }

        public string TryMoveTo(int roomId)
        {
            if (Mode != GameMode.Exploring) return "탐험 중이 아닙니다.";
            string error = Expedition.TryMoveTo(roomId);
            if (error == null) AfterExpeditionAction();
            return error;
        }

        public string TryInteractCurio()
        {
            if (Mode != GameMode.Exploring) return "탐험 중이 아닙니다.";
            string error = Expedition.TryInteractCurio();
            if (error == null) AfterExpeditionAction();
            return error;
        }

        public string TryUseTorch() => Mode == GameMode.Exploring ? Expedition.TryUseTorch() : "탐험 중이 아닙니다.";

        public string TryCamp() => Mode == GameMode.Exploring ? Expedition.TryCamp() : "탐험 중이 아닙니다.";

        public void Retreat()
        {
            if (Mode != GameMode.Exploring) return;
            Expedition.Retreat();
            AfterExpeditionAction();
        }

        public string SubmitBattleCommand(IBattleCommand command)
        {
            if (Mode != GameMode.Battle || Battle == null) return "전투 중이 아닙니다.";
            if (Battle.IsOver) return "전투가 이미 끝났습니다.";
            string error = Battle.Submit(command);
            if (error == null && Battle.IsOver) FinishBattle();
            return error;
        }

        /// <summary>전투 결과 확인 후 탐험(또는 탐험 결과)으로 넘어간다.</summary>
        public string ContinueAfterBattle()
        {
            if (!IsBattleConcluded) return "끝난 전투가 없습니다.";
            Battle = null;
            LastBattleSummary = null;
            SetMode(Expedition.IsFinished ? GameMode.ExpeditionResult : GameMode.Exploring);
            return null;
        }

        public void ReturnToTown()
        {
            if (Mode != GameMode.ExpeditionResult) return;
            TownService.SettleExpedition(Expedition);
            Expedition = null;
            SetMode(GameMode.Town);
        }

        /// <summary>마을, 또는 탐험 중(전투/결과 화면 제외)에 저장할 수 있다.</summary>
        public bool CanSave => Mode == GameMode.Town || (Mode == GameMode.Exploring && Expedition.Phase == ExpeditionPhase.Exploring);

        public GameSaveData CaptureSave()
        {
            Guard.That(CanSave, "지금은 저장할 수 없습니다: " + Mode);
            var data = _saveMapper.Capture(Town);
            if (Mode == GameMode.Exploring) data.Expedition = ExpeditionSaveMapper.Capture(Expedition);
            return data;
        }

        private void ResumeExpedition(ExpeditionSaveData saved)
        {
            Expedition = _expeditionSaveMapper.Restore(saved, Town);
            if (Expedition == null) return; // 복원 실패는 매퍼가 로그를 남겼다. 마을에서 시작.
            Mode = GameMode.Exploring; // 생성 중이라 구독자가 없다. 화면은 부트스트랩이 Mode를 보고 연다.
            _logger.Info(LogCategory, "원정 이어하기: 방 " + Expedition.CurrentRoomId + ", 이동 " + Expedition.StepsTaken + "칸");
        }

        private void AfterExpeditionAction()
        {
            if (Expedition.Phase == ExpeditionPhase.InBattle) StartBattle();
            else if (Expedition.IsFinished) SetMode(GameMode.ExpeditionResult);
        }

        private void StartBattle()
        {
            var monsters = _factory.CreateMonsterFormation(Expedition.PendingEncounter);
            Expedition.ApplyLightToMonsters(monsters);
            Battle = new BattleSession(Expedition.Party, monsters, _battleServices);
            Battle.Start();
            // 첫 영웅 차례까지 진행된 뒤에 화면을 바꿔야 UI가 올바른 행동자를 그린다.
            // 영웅 차례가 오기 전에 끝난 경우(몬스터 선공 전멸)에도 전투 화면에서 연출과 결과를 보여준다.
            if (Battle.IsOver) FinishBattle();
            SetMode(GameMode.Battle);
        }

        private void FinishBattle()
        {
            var result = Battle.Phase == BattlePhase.Victory ? BattleResult.Victory : BattleResult.Defeat;
            int goldBefore = Expedition.Inventory.Gold;
            int fallenBefore = Expedition.Fallen.Count;
            Expedition.ResolveBattle(result);

            var fallen = new List<Hero>();
            for (int i = fallenBefore; i < Expedition.Fallen.Count; i++) fallen.Add(Expedition.Fallen[i]);
            LastBattleSummary = new BattleSummary(result, Battle.Context.Round, Expedition.Inventory.Gold - goldBefore, fallen);
            _logger.Info(LogCategory, LastBattleSummary.ToString());
        }

        private string ValidateParty(IReadOnlyList<Hero> party)
        {
            if (party == null || party.Count == 0) return "파티에 영웅이 없습니다.";
            if (party.Count > Formation.MaxRanks) return "파티는 최대 4명입니다.";
            for (int i = 0; i < party.Count; i++)
            {
                var hero = party[i];
                if (hero == null || Town.FindHero(hero.Id) != hero) return "명부에 없는 영웅이 있습니다.";
                if (hero.IsDead) return hero.DisplayName + "은(는) 사망했습니다.";
                for (int j = 0; j < i; j++)
                {
                    if (party[j] == hero) return "같은 영웅을 두 번 넣을 수 없습니다.";
                }
            }
            return null;
        }

        private void SetMode(GameMode mode)
        {
            if (Mode == mode) return;
            Mode = mode;
            _logger.Info(LogCategory, "모드 → " + mode);
            Events.Publish(new GameModeChanged(mode));
        }
    }
}
