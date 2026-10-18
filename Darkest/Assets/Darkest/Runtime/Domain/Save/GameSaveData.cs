using System.Collections.Generic;

namespace Darkest.Domain.Save
{
    /// <summary>
    /// 세이브 스냅샷(순수 데이터). 런타임 객체를 직접 직렬화하지 않고 이 DTO를 거쳐
    /// 저장 포맷이 도메인 클래스 리팩터링에 휘둘리지 않게 한다.
    /// </summary>
    public sealed class GameSaveData
    {
        /// <summary>
        /// 2: 원정 중 저장(ExpeditionSaveData)과 영웅 사망/문턱 플래그.
        /// 3: 난수 상태, 건물 단계, 장신구, 상태이상, 야영 버프/야영 상태. 이전 버전도 그대로 읽는다.
        /// </summary>
        public const int CurrentVersion = 3;

        public int Version { get; set; } = CurrentVersion;
        public int Gold { get; set; }
        public int Week { get; set; }
        public int NextId { get; set; }
        public List<HeroSaveData> Roster { get; } = new List<HeroSaveData>();

        /// <summary>저장 시점의 난수 진행 상태. 없으면(구버전) 새 seed로 이어간다.</summary>
        public ulong? RandomState { get; set; }

        /// <summary>건물 종류 이름 → 단계.</summary>
        public Dictionary<string, int> Buildings { get; } = new Dictionary<string, int>();

        /// <summary>보관함 장신구 ID.</summary>
        public List<string> TrinketStash { get; } = new List<string>();

        /// <summary>원정 중에 저장했으면 그 진행도, 마을에서 저장했으면 null.</summary>
        public ExpeditionSaveData Expedition { get; set; }
    }

    public sealed class ExpeditionSaveData
    {
        public int CurrentRoomId { get; set; }
        public int StepsTaken { get; set; }
        public int BattlesWon { get; set; }
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

        /// <summary>이번 원정에서 주운 장신구 ID.</summary>
        public List<string> FoundTrinkets { get; } = new List<string>();

        /// <summary>야영 중에 저장했으면 야영 상태, 아니면 null.</summary>
        public CampSaveData Camp { get; set; }
    }

    public sealed class CampSaveData
    {
        public int RespiteRemaining { get; set; }

        /// <summary>"영웅ID:스킬ID"</summary>
        public List<string> UsedKeys { get; } = new List<string>();
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
        public int Experience { get; set; }

        /// <summary>슬롯 순서 장신구 ID. 빈 슬롯은 빈 문자열.</summary>
        public List<string> Trinkets { get; } = new List<string>();

        public List<StatusSaveData> Statuses { get; } = new List<StatusSaveData>();
        public List<StatBonusSaveData> CampBuffs { get; } = new List<StatBonusSaveData>();
    }

    public sealed class StatusSaveData
    {
        public StatusSaveData(string kind, int damagePerTurn, int remainingTurns)
        {
            Kind = kind;
            DamagePerTurn = damagePerTurn;
            RemainingTurns = remainingTurns;
        }

        public string Kind { get; }
        public int DamagePerTurn { get; }
        public int RemainingTurns { get; }
    }

    public sealed class StatBonusSaveData
    {
        public StatBonusSaveData(string stat, int amount)
        {
            Stat = stat;
            Amount = amount;
        }

        public string Stat { get; }
        public int Amount { get; }
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
