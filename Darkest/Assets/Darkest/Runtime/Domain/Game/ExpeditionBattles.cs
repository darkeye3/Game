using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Commands;
using Darkest.Domain.Combat.Events;
using Darkest.Domain.Combat.Flow;
using Darkest.Domain.Combat.Resolution;
using Darkest.Domain.Content;
using Darkest.Domain.Core;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Game
{
    public sealed class BattleRetreatAttempted : IDomainEvent
    {
        public BattleRetreatAttempted(Combatant actor, bool succeeded) { Actor = actor; Succeeded = succeeded; }
        public Combatant Actor { get; }
        public bool Succeeded { get; }
        public override string ToString() => "Retreat " + (Succeeded ? "ok" : "failed");
    }

    /// <summary>
    /// 원정 안의 전투 하나의 수명: 시작 → 명령/후퇴 → 종료 요약. 화면 모드 전환은 GameSession이 맡고,
    /// 여기서는 전투와 원정 사이의 결과 반영만 한다.
    /// </summary>
    internal sealed class ExpeditionBattles
    {
        private const string LogCategory = "Game";

        private readonly CombatantFactory _factory;
        private readonly BattleServices _services;
        private readonly CombatRules _rules;
        private readonly StressService _stress;
        private readonly IRandom _random;
        private readonly IEventBus _events;
        private readonly IGameLogger _logger;

        public ExpeditionBattles(CombatantFactory factory, BattleServices services, CombatRules rules, StressService stress,
            IRandom random, IEventBus events, IGameLogger logger)
        {
            _factory = Guard.NotNull(factory, nameof(factory));
            _services = Guard.NotNull(services, nameof(services));
            _rules = Guard.NotNull(rules, nameof(rules));
            _stress = Guard.NotNull(stress, nameof(stress));
            _random = Guard.NotNull(random, nameof(random));
            _events = Guard.NotNull(events, nameof(events));
            _logger = logger ?? NullLogger.Instance;
        }

        public BattleSession Current { get; private set; }
        public BattleSummary Summary { get; private set; }

        public void Start(Expedition expedition)
        {
            var monsters = _factory.CreateMonsterFormation(expedition.PendingEncounter);
            expedition.ApplyLightToMonsters(monsters);
            Current = new BattleSession(expedition.Party, monsters, _services);
            Summary = null;
            Current.Start();
            if (Current.IsOver) Finish(expedition); // 영웅 차례 전에 끝남(몬스터 선공 전멸)
        }

        public string Submit(Expedition expedition, IBattleCommand command)
        {
            if (Current == null) return "전투 중이 아닙니다.";
            if (Summary != null) return "전투가 이미 끝났습니다.";
            string error = Current.Submit(command);
            if (error == null && Current.IsOver) Finish(expedition);
            return error;
        }

        /// <summary>
        /// 현재 영웅의 차례에 후퇴를 시도한다. 성공하면 원정이 후퇴로 끝나고, 실패하면 그 영웅의 차례만 넘어간다.
        /// </summary>
        public string TryRetreat(Expedition expedition)
        {
            if (Current == null || Summary != null) return "전투 중이 아닙니다.";
            if (!Current.IsAwaitingHeroCommand) return "영웅의 차례에만 후퇴할 수 있습니다.";

            var actor = Current.CurrentActor;
            bool success = _random.RollPercent(_rules.RetreatChancePercent);
            _events.Publish(new BattleRetreatAttempted(actor, success));
            if (!success) return Submit(expedition, new PassCommand(actor));

            _stress.ApplyToAll(expedition.Party.Members, _rules.StressOnRetreat);
            expedition.Retreat();
            Summary = new BattleSummary(BattleResult.Fled, Current.Context.Round, 0, new List<Hero>());
            _logger.Info(LogCategory, Summary.ToString());
            return null;
        }

        public void Clear()
        {
            Current = null;
            Summary = null;
        }

        private void Finish(Expedition expedition)
        {
            var result = Current.Phase == BattlePhase.Victory ? BattleResult.Victory : BattleResult.Defeat;
            int goldBefore = expedition.Inventory.Gold;
            int fallenBefore = expedition.Fallen.Count;
            expedition.ResolveBattle(result);

            var fallen = new List<Hero>();
            for (int i = fallenBefore; i < expedition.Fallen.Count; i++) fallen.Add(expedition.Fallen[i]);
            Summary = new BattleSummary(result, Current.Context.Round, expedition.Inventory.Gold - goldBefore, fallen);
            _logger.Info(LogCategory, Summary.ToString());
        }
    }
}
