using System.Collections.Generic;

namespace Darkest.Domain.Save
{
    /// <summary>
    /// 세이브 스냅샷(순수 데이터). 런타임 객체를 직접 직렬화하지 않고 이 DTO를 거쳐
    /// 저장 포맷이 도메인 클래스 리팩터링에 휘둘리지 않게 한다.
    /// </summary>
    public sealed class GameSaveData
    {
        /// <summary>2: 원정 중 저장(ExpeditionSaveData)과 영웅 사망/문턱 플래그 추가. 1도 그대로 읽는다.</summary>
        public const int CurrentVersion = 2;

        public int Version { get; set; } = CurrentVersion;
        public int Gold { get; set; }
        public int Week { get; set; }
        public int NextId { get; set; }
        public List<HeroSaveData> Roster { get; } = new List<HeroSaveData>();

        /// <summary>원정 중에 저장했으면 그 진행도, 마을에서 저장했으면 null.</summary>
        public ExpeditionSaveData Expedition { get; set; }
    }

    public sealed class ExpeditionSaveData
    {
        public int CurrentRoomId { get; set; }
        public int StepsTaken { get; set; }
        public int Light { get; set; }
        public int Food { get; set; }
        public int Torches { get; set; }
        public int Firewood { get; set; }
        public int Gold { get; set; }
        public int EntranceId { get; set; }
        public int BossRoomId { get; set; }
        public int MapSeed { get; set; }

        /// <summary>진형 순서(1열부터) 생존 영웅 ID.</summary>
        public List<string> PartyIds { get; } = new List<string>();

        /// <summary>출발 명단(사망자 포함).</summary>
        public List<string> EmbarkedIds { get; } = new List<string>();

        public List<RoomSaveData> Rooms { get; } = new List<RoomSaveData>();
        public List<RoomLinkSaveData> Links { get; } = new List<RoomLinkSaveData>();
    }

    public sealed class RoomSaveData
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public string EncounterId { get; set; } = string.Empty;
        public int Gold { get; set; }
        public bool IsCleared { get; set; }
        public bool IsVisited { get; set; }
    }

    public sealed class RoomLinkSaveData
    {
        public RoomLinkSaveData(int a, int b) { A = a; B = b; }
        public int A { get; }
        public int B { get; }
    }

    public sealed class HeroSaveData
    {
        public string Id { get; set; }
        public string ClassId { get; set; }
        public string Name { get; set; }
        public int Hp { get; set; }
        public int Stress { get; set; }

        /// <summary>고통/미덕 ID. 없으면 빈 문자열.</summary>
        public string ResolveId { get; set; } = string.Empty;

        public bool IsDead { get; set; }
        public bool IsAtDeathsDoor { get; set; }
    }

    /// <summary>저장 포맷 Strategy. 포맷을 바꾸려면(JSON/바이너리) 이 인터페이스를 새로 구현한다.</summary>
    public interface ISaveSerializer
    {
        string Serialize(GameSaveData data);
        GameSaveData Deserialize(string text);
    }

    /// <summary>저장소 Adapter 경계. 파일/PlayerPrefs/클라우드 등 실제 저장 위치는 Unity 쪽에서 구현한다.</summary>
    public interface ISaveRepository
    {
        bool TryLoad(out string text);
        void Save(string text);
        void Delete();
    }
}
