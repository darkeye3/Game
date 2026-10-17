using System;
using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Resolution;
using Darkest.Domain.Content;
using Darkest.Domain.Core;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Hamlet;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Save
{
    /// <summary>
    /// 탐험 중(Exploring) 원정 ↔ ExpeditionSaveData. 전투 중 상태와 지속 효과(출혈 등), 난수 진행은 저장하지 않는다.
    /// 복원이 실패하면 원정만 버리고 마을은 살린다(세이브 전체를 잃지 않게).
    /// </summary>
    public sealed class ExpeditionSaveMapper
    {
        private const string LogCategory = "Save";

        private readonly ExpeditionRules _rules;
        private readonly CombatRules _combatRules;
        private readonly StressService _stress;
        private readonly IContentCatalog _catalog;
        private readonly IRandom _random;
        private readonly IEventBus _events;
        private readonly IGameLogger _logger;

        public ExpeditionSaveMapper(ExpeditionRules rules, CombatRules combatRules, StressService stress,
            IContentCatalog catalog, IRandom random, IEventBus events, IGameLogger logger)
        {
            _rules = Guard.NotNull(rules, nameof(rules));
            _combatRules = Guard.NotNull(combatRules, nameof(combatRules));
            _stress = Guard.NotNull(stress, nameof(stress));
            _catalog = Guard.NotNull(catalog, nameof(catalog));
            _random = Guard.NotNull(random, nameof(random));
            _events = Guard.NotNull(events, nameof(events));
            _logger = logger ?? NullLogger.Instance;
        }

        public static ExpeditionSaveData Capture(Expedition expedition)
        {
            Guard.NotNull(expedition, nameof(expedition));
            Guard.That(expedition.Phase == ExpeditionPhase.Exploring, "탐험 중에만 원정을 저장할 수 있습니다: " + expedition.Phase);

            var inventory = expedition.Inventory;
            var map = expedition.Map;
            var data = new ExpeditionSaveData
            {
                CurrentRoomId = expedition.CurrentRoomId,
                StepsTaken = expedition.StepsTaken,
                BattlesWon = expedition.BattlesWon,
                Light = expedition.Light.Value,
                Food = inventory.Food,
                Torches = inventory.Torches,
                Firewood = inventory.Firewood,
                Gold = inventory.Gold,
                EntranceId = map.EntranceId,
                BossRoomId = map.BossRoomId,
                MapSeed = map.Seed
            };
            for (int i = 0; i < expedition.Party.Members.Count; i++) data.PartyIds.Add(expedition.Party.Members[i].Id);
            for (int i = 0; i < expedition.Embarked.Count; i++) data.EmbarkedIds.Add(expedition.Embarked[i].Id);
            CaptureMap(map, data);
            return data;
        }

        /// <returns>복원된 원정. 세이브가 마을 명부/콘텐츠와 맞지 않으면 에러 로그 후 null.</returns>
        public Expedition Restore(ExpeditionSaveData data, Town town)
        {
            Guard.NotNull(data, nameof(data));
            Guard.NotNull(town, nameof(town));
            try
            {
                return RestoreOrThrow(data, town);
            }
            catch (Exception e) when (e is KeyNotFoundException || e is InvalidOperationException || e is ArgumentException)
            {
                _logger.Error(LogCategory, "원정 복원 실패 — 마을에서 다시 시작합니다: " + e.Message);
                return null;
            }
        }

        private Expedition RestoreOrThrow(ExpeditionSaveData data, Town town)
        {
            var party = new Formation(Team.Heroes);
            foreach (var id in data.PartyIds) party.Add(FindHero(town, id));
            var embarked = new List<Hero>(data.EmbarkedIds.Count);
            foreach (var id in data.EmbarkedIds) embarked.Add(FindHero(town, id));

            var inventory = new Inventory(data.Food, data.Torches, data.Firewood);
            inventory.AddGold(data.Gold);

            var expedition = new Expedition(party, RestoreMap(data), inventory, _rules, _combatRules, _stress, _catalog, _random, _events, _logger);
            expedition.RestoreProgress(embarked, data.CurrentRoomId, data.StepsTaken, data.Light, data.BattlesWon);
            return expedition;
        }

        private static Hero FindHero(Town town, string id) =>
            town.FindHero(id) ?? throw new KeyNotFoundException("원정 세이브의 영웅이 명부에 없습니다: " + id);

        private static void CaptureMap(DungeonMap map, ExpeditionSaveData data)
        {
            foreach (var room in map.Rooms)
            {
                data.Rooms.Add(new RoomSaveData
                {
                    Id = room.Id,
                    Type = room.Type.ToString(),
                    EncounterId = room.EncounterId ?? string.Empty,
                    Gold = room.GoldReward,
                    IsCleared = room.IsCleared,
                    IsVisited = room.IsVisited
                });
                // 양방향 연결이므로 작은 ID → 큰 ID 쪽만 기록한다.
                foreach (int neighbor in room.Neighbors)
                {
                    if (room.Id < neighbor) data.Links.Add(new RoomLinkSaveData(room.Id, neighbor));
                }
            }
        }

        private static DungeonMap RestoreMap(ExpeditionSaveData data)
        {
            var rooms = new List<Room>(data.Rooms.Count);
            foreach (var saved in data.Rooms)
            {
                if (!Enum.TryParse(saved.Type, out RoomType type))
                    throw new ArgumentException("알 수 없는 방 종류: " + saved.Type);
                var encounter = string.IsNullOrEmpty(saved.EncounterId) ? null : saved.EncounterId;
                rooms.Add(new Room(saved.Id, type, encounter, saved.Gold) { IsCleared = saved.IsCleared, IsVisited = saved.IsVisited });
            }
            var links = new List<(int a, int b)>(data.Links.Count);
            foreach (var link in data.Links) links.Add((link.A, link.B));
            return DungeonMap.FromLayout(rooms, links, data.EntranceId, data.BossRoomId, data.MapSeed);
        }
    }
}
