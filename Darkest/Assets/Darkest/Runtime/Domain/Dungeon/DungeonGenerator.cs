using System.Collections.Generic;
using Darkest.Domain.Content;
using Darkest.Domain.Core;

namespace Darkest.Domain.Dungeon
{
    /// <summary>
    /// 입구 → (메인 경로 N개) → 보스방 일직선에, 메인 경로 방에 곁가지 방을 붙이는 단순 생성기.
    /// 같은 seed면 항상 같은 맵(재현성).
    /// </summary>
    public sealed class DungeonGenerator
    {
        private readonly IContentCatalog _catalog;
        private readonly DungeonGenerationRules _rules;

        public DungeonGenerator(IContentCatalog catalog, DungeonGenerationRules rules)
        {
            _catalog = Guard.NotNull(catalog, nameof(catalog));
            _rules = Guard.NotNull(rules, nameof(rules));
            Guard.That(rules.MainPathRooms >= 1, "메인 경로 방은 1개 이상");
        }

        public DungeonMap Generate(IRandom random)
        {
            var rooms = new List<Room>();
            int nextId = 0;

            var entrance = new Room(nextId++, RoomType.Entrance, null, 0);
            entrance.IsVisited = true;
            rooms.Add(entrance);

            var previous = entrance;
            var mainPath = new List<Room>(_rules.MainPathRooms);
            for (int i = 0; i < _rules.MainPathRooms; i++)
            {
                var room = CreateRandomRoom(nextId++, random);
                previous.Connect(room);
                rooms.Add(room);
                mainPath.Add(room);
                previous = room;
            }

            var bossGroup = random.Pick(_catalog.BossGroups);
            var boss = new Room(nextId++, RoomType.Boss, bossGroup.Id, bossGroup.GoldReward);
            previous.Connect(boss);
            rooms.Add(boss);

            for (int i = 0; i < _rules.BranchRooms; i++)
            {
                var branch = CreateRandomRoom(nextId++, random);
                random.Pick(mainPath).Connect(branch);
                rooms.Add(branch);
            }

            return new DungeonMap(rooms, entrance.Id, boss.Id, random.Seed);
        }

        private Room CreateRandomRoom(int id, IRandom random)
        {
            var type = RollRoomType(random);
            switch (type)
            {
                case RoomType.Battle:
                    var group = random.Pick(_catalog.NormalGroups);
                    return new Room(id, type, group.Id, group.GoldReward);
                case RoomType.Curio:
                    return new Room(id, type, random.Pick(_catalog.Curios).Id, 0);
                case RoomType.Treasure:
                    return new Room(id, type, null, random.RangeInclusive(_rules.TreasureGoldMin, _rules.TreasureGoldMax));
                default:
                    return new Room(id, RoomType.Empty, null, 0);
            }
        }

        private RoomType RollRoomType(IRandom random)
        {
            int curioWeight = _catalog.Curios.Count > 0 ? _rules.CurioWeight : 0;
            int total = _rules.BattleWeight + curioWeight + _rules.TreasureWeight + _rules.EmptyWeight;
            Guard.That(total > 0, "방 가중치 합이 0입니다.");

            int roll = random.Range(0, total);
            if ((roll -= _rules.BattleWeight) < 0) return RoomType.Battle;
            if ((roll -= curioWeight) < 0) return RoomType.Curio;
            if ((roll -= _rules.TreasureWeight) < 0) return RoomType.Treasure;
            return RoomType.Empty;
        }
    }
}
