using System.Collections.Generic;

namespace Darkest.Domain.Save
{
    /// <summary>
    /// 세이브 스냅샷(순수 데이터). 런타임 객체를 직접 직렬화하지 않고 이 DTO를 거쳐
    /// 저장 포맷이 도메인 클래스 리팩터링에 휘둘리지 않게 한다.
    /// </summary>
    public sealed class GameSaveData
    {
        public const int CurrentVersion = 1;

        public int Version { get; set; } = CurrentVersion;
        public int Gold { get; set; }
        public int Week { get; set; }
        public int NextId { get; set; }
        public List<HeroSaveData> Roster { get; } = new List<HeroSaveData>();
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
