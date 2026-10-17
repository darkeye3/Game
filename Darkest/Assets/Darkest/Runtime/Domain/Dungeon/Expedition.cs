using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Events;
using Darkest.Domain.Combat.Resolution;
using Darkest.Domain.Content;
using Darkest.Domain.Core;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Dungeon
{
    /// <summary>
    /// 던전 탐험 1회의 상태와 규칙. 이동(빛/허기/스트레스), 방 진입, 골동품, 전투 결과 반영을 담당한다.
    /// 전투 자체는 모른다 — 전투가 필요하면 Phase=InBattle과 PendingEncounter만 알려준다.
    /// </summary>
    public sealed class Expedition
    {
        private const string LogCategory = "Expedition";

        private readonly ExpeditionRules _rules;
        private readonly CurioResolver _curioResolver;
        private readonly CorridorTravel _corridor;
        private readonly Camp _camp;
        private readonly LightEffects _lightEffects;
        private readonly IContentCatalog _catalog;
        private readonly IRandom _random;
        private readonly IEventBus _events;
        private readonly IGameLogger _logger;
        private readonly List<Combatant> _deadBuffer = new List<Combatant>(Formation.MaxRanks);
        private readonly List<Hero> _embarked = new List<Hero>(Formation.MaxRanks);
        private readonly List<Hero> _fallen = new List<Hero>();

        public Expedition(Formation party, DungeonMap map, Inventory inventory, ExpeditionRules rules,
            CombatRules combatRules, StressService stress, IContentCatalog catalog, IRandom random, IEventBus events, IGameLogger logger)
        {
            Party = Guard.NotNull(party, nameof(party));
            Guard.That(!party.IsEmpty && party.Team == Team.Heroes, "영웅 파티가 비어 있습니다.");
            Map = Guard.NotNull(map, nameof(map));
            Inventory = Guard.NotNull(inventory, nameof(inventory));
            _rules = Guard.NotNull(rules, nameof(rules));
            _catalog = Guard.NotNull(catalog, nameof(catalog));
            _random = Guard.NotNull(random, nameof(random));
            _events = Guard.NotNull(events, nameof(events));
            _logger = logger ?? NullLogger.Instance;
            var hazard = new PartyHazard(combatRules, stress, random);
            _curioResolver = new CurioResolver(stress, hazard);
            _corridor = new CorridorTravel(rules, stress, hazard, events, _logger);
            _camp = new Camp(rules, stress, events);
            _lightEffects = new LightEffects(rules);
            Light = new TorchLight(rules);
            CurrentRoomId = map.EntranceId;
            for (int i = 0; i < party.Members.Count; i++) _embarked.Add((Hero)party.Members[i]);
        }

        public Formation Party { get; }
        public DungeonMap Map { get; }
        public Inventory Inventory { get; }
        public TorchLight Light { get; }
        public int CurrentRoomId { get; private set; }
        public Room CurrentRoom => Map.Get(CurrentRoomId);
        public int StepsTaken { get; private set; }
        public ExpeditionPhase Phase { get; private set; } = ExpeditionPhase.Exploring;
        public bool IsFinished => Phase == ExpeditionPhase.Completed || Phase == ExpeditionPhase.Failed || Phase == ExpeditionPhase.Retreated;

        public IReadOnlyList<Hero> Embarked => _embarked;

        /// <summary>탐험 중 사망한 영웅(마을 귀환 시 명부에서 제거용).</summary>
        public IReadOnlyList<Hero> Fallen => _fallen;

        /// <summary>InBattle일 때 싸워야 할 몬스터 편성.</summary>
        public MonsterGroupDefinition PendingEncounter { get; private set; }

        /// <returns>실패 사유, 성공 시 null</returns>
        public string TryMoveTo(int roomId)
        {
            if (Phase != ExpeditionPhase.Exploring) return "탐험 중이 아닙니다: " + Phase;
            if (!Map.AreAdjacent(CurrentRoomId, roomId)) return "인접한 방이 아닙니다.";
            if (!CurrentRoom.IsCleared) return "현재 방을 먼저 정리해야 합니다.";

            for (int step = 1; step <= _rules.CorridorSteps; step++)
            {
                StepsTaken++;
                _corridor.Step(step, StepsTaken, Party, Inventory, Light);
                if (CheckPartyWipe()) return null;
            }

            EnterRoom(Map.Get(roomId));
            return null;
        }

        public string TryInteractCurio()
        {
            var room = CurrentRoom;
            if (Phase != ExpeditionPhase.Exploring) return "탐험 중이 아닙니다.";
            if (room.Type != RoomType.Curio || room.IsCleared) return "조사할 골동품이 없습니다.";

            var curio = _catalog.GetCurio(room.EncounterId);
            var outcome = curio.Roll(_random);
            _curioResolver.Apply(outcome, Party, Inventory, Light);
            room.IsCleared = true;
            _events.Publish(new CurioResolved(curio, outcome));
            CheckPartyWipe();
            return null;
        }

        public string TryUseTorch()
        {
            if (IsFinished) return "탐험이 끝났습니다.";
            if (Light.Value >= _rules.MaxLight) return "이미 가장 밝습니다.";
            if (!Inventory.TryConsumeTorch()) return "횃불이 없습니다.";
            Light.Change(_rules.TorchLightRestore);
            return null;
        }

        public int CampFoodCost => _camp.FoodCost(Party);

        /// <summary>정리된 방에서만 야영 가능(전투 직후 안전한 곳이라는 의미).</summary>
        public string TryCamp()
        {
            if (Phase != ExpeditionPhase.Exploring) return "탐험 중이 아닙니다.";
            if (!CurrentRoom.IsCleared) return "방을 먼저 정리해야 합니다.";
            return _camp.TryMake(Party, Inventory, Light);
        }

        /// <summary>현재 빛 기준 몬스터 보정. GameSession이 전투 시작 시 호출한다.</summary>
        public void ApplyLightToMonsters(Formation monsters) => _lightEffects.ApplyToMonsters(monsters, Light.Level);

        public void Retreat()
        {
            if (IsFinished) return;
            SetPhase(ExpeditionPhase.Retreated);
        }

        public void ResolveBattle(BattleResult result)
        {
            Guard.That(Phase == ExpeditionPhase.InBattle, "전투 중이 아닌데 전투 결과가 들어왔습니다.");
            CollectFallen();
            PendingEncounter = null;

            if (result == BattleResult.Defeat || Party.IsEmpty)
            {
                SetPhase(ExpeditionPhase.Failed);
                return;
            }

            var room = CurrentRoom;
            room.IsCleared = true;
            GainGold(_lightEffects.ScaleLoot(room.GoldReward, Light.Level), "전투 보상");
            SetPhase(room.Type == RoomType.Boss ? ExpeditionPhase.Completed : ExpeditionPhase.Exploring);
        }

        private void EnterRoom(Room room)
        {
            CurrentRoomId = room.Id;
            room.IsVisited = true;
            _events.Publish(new RoomEntered(room));

            if (room.IsCleared) return;
            switch (room.Type)
            {
                case RoomType.Battle:
                case RoomType.Boss:
                    PendingEncounter = _catalog.GetMonsterGroup(room.EncounterId);
                    SetPhase(ExpeditionPhase.InBattle);
                    break;
                case RoomType.Treasure:
                    room.IsCleared = true;
                    GainGold(_lightEffects.ScaleLoot(room.GoldReward, Light.Level), "보물");
                    break;
            }
        }

        private void GainGold(int gold, string source)
        {
            if (gold <= 0) return;
            Inventory.AddGold(gold);
            _events.Publish(new LootGained(gold, source));
        }

        /// <returns>파티 전멸이면 true</returns>
        private bool CheckPartyWipe()
        {
            CollectFallen();
            if (!Party.IsEmpty) return false;
            SetPhase(ExpeditionPhase.Failed);
            return true;
        }

        /// <summary>
        /// 전투 중 사망자는 BattleSession이 진형에서 이미 빼므로, 진형이 아니라 출발 명단 기준으로 집계한다.
        /// </summary>
        private void CollectFallen()
        {
            _deadBuffer.Clear();
            Party.RemoveDead(_deadBuffer);
            for (int i = 0; i < _embarked.Count; i++)
            {
                var hero = _embarked[i];
                if (hero.IsDead && !_fallen.Contains(hero)) _fallen.Add(hero);
            }
        }

        private void SetPhase(ExpeditionPhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            _logger.Info(LogCategory, "탐험 상태 → " + phase);
            _events.Publish(new ExpeditionPhaseChanged(phase));
        }
    }
}
