using System.Collections.Generic;
using Darkest.Domain.Core;

namespace Darkest.Domain.Dungeon
{
    public enum RoomType
    {
        Entrance,
        Empty,
        Battle,
        Curio,
        Treasure,
        Boss
    }

    public sealed class Room
    {
        private readonly List<int> _neighbors = new List<int>(4);

        public Room(int id, RoomType type, string encounterId, int goldReward)
        {
            Id = id;
            Type = type;
            EncounterId = encounterId;
            GoldReward = goldReward;
            // 입구/빈 방은 처음부터 정리된 상태.
            IsCleared = type == RoomType.Entrance || type == RoomType.Empty;
        }

        public int Id { get; }
        public RoomType Type { get; }

        /// <summary>전투방이면 몬스터 편성 ID, 골동품방이면 골동품 ID.</summary>
        public string EncounterId { get; }

        public int GoldReward { get; }
        public bool IsVisited { get; internal set; }
        public bool IsCleared { get; internal set; }
        public IReadOnlyList<int> Neighbors => _neighbors;

        internal void Connect(Room other)
        {
            if (!_neighbors.Contains(other.Id)) _neighbors.Add(other.Id);
            if (!other._neighbors.Contains(Id)) other._neighbors.Add(Id);
        }

        public override string ToString() => "Room" + Id + "(" + Type + (IsCleared ? ",cleared" : "") + ")";
    }

    public sealed class DungeonMap
    {
        private readonly List<Room> _rooms;

        public DungeonMap(List<Room> rooms, int entranceId, int bossRoomId, int seed)
        {
            Guard.NotNull(rooms, nameof(rooms));
            Guard.That(rooms.Count > 1, "방이 2개 이상 필요합니다.");
            _rooms = rooms;
            EntranceId = entranceId;
            BossRoomId = bossRoomId;
            Seed = seed;
        }

        /// <summary>손으로 짠 맵(튜토리얼, 테스트, 에디터 툴)용 생성 경로.</summary>
        public static DungeonMap FromLayout(List<Room> rooms, IReadOnlyList<(int a, int b)> links, int entranceId, int bossRoomId)
        {
            var map = new DungeonMap(rooms, entranceId, bossRoomId, 0);
            for (int i = 0; i < links.Count; i++) map.Get(links[i].a).Connect(map.Get(links[i].b));
            return map;
        }

        public IReadOnlyList<Room> Rooms => _rooms;
        public int EntranceId { get; }
        public int BossRoomId { get; }

        /// <summary>이 맵을 만든 seed. 버그 리포트에 함께 남긴다.</summary>
        public int Seed { get; }

        public Room Get(int id)
        {
            for (int i = 0; i < _rooms.Count; i++)
            {
                if (_rooms[i].Id == id) return _rooms[i];
            }
            throw new KeyNotFoundException("방 ID 없음: " + id);
        }

        public bool AreAdjacent(int a, int b)
        {
            var room = Get(a);
            for (int i = 0; i < room.Neighbors.Count; i++)
            {
                if (room.Neighbors[i] == b) return true;
            }
            return false;
        }
    }
}
