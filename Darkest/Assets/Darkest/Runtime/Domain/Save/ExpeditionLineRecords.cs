using System;
using System.Text;
using static Darkest.Domain.Save.LineFormat;

namespace Darkest.Domain.Save
{
    /// <summary>원정 세부 레코드(exp/party/embarked/room/link/loot/camp) 읽기·쓰기. 세부 줄은 반드시 exp 줄 뒤에 온다.</summary>
    internal static class ExpeditionLineRecords
    {
        public static void Append(StringBuilder sb, ExpeditionSaveData e)
        {
            AppendLine(sb, "exp", Int(e.CurrentRoomId), Int(e.StepsTaken), Int(e.Light), Int(e.Food), Int(e.Torches),
                Int(e.Firewood), Int(e.Gold), Int(e.EntranceId), Int(e.BossRoomId), Int(e.MapSeed), Int(e.BattlesWon));
            AppendIds(sb, "party", e.PartyIds);
            AppendIds(sb, "embarked", e.EmbarkedIds);
            foreach (var room in e.Rooms)
                AppendLine(sb, "room", Int(room.Id), room.Type, room.EncounterId ?? string.Empty, Int(room.Gold), Bool(room.IsCleared), Bool(room.IsVisited));
            foreach (var link in e.Links) AppendLine(sb, "link", Int(link.A), Int(link.B));
            if (e.FoundTrinkets.Count > 0) AppendIds(sb, "loot", e.FoundTrinkets);
            if (e.Camp != null)
            {
                var fields = new string[e.Camp.UsedKeys.Count + 2];
                fields[0] = "camp";
                fields[1] = Int(e.Camp.RespiteRemaining);
                for (int i = 0; i < e.Camp.UsedKeys.Count; i++) fields[i + 2] = e.Camp.UsedKeys[i];
                AppendLine(sb, fields);
            }
        }

        /// <returns>이 레코드를 처리했으면 true(원정 레코드가 아니면 false).</returns>
        public static bool TryRead(GameSaveData data, string[] fields)
        {
            switch (fields[0])
            {
                case "exp": data.Expedition = ParseExpedition(fields); return true;
                case "party": Of(data).PartyIds.AddRange(Tail(fields)); return true;
                case "embarked": Of(data).EmbarkedIds.AddRange(Tail(fields)); return true;
                case "room": Of(data).Rooms.Add(ParseRoom(fields)); return true;
                case "link": Of(data).Links.Add(new RoomLinkSaveData(ParseInt(fields, 1), ParseInt(fields, 2))); return true;
                case "loot": Of(data).FoundTrinkets.AddRange(Tail(fields)); return true;
                case "camp":
                    var camp = new CampSaveData { RespiteRemaining = ParseInt(fields, 1) };
                    camp.UsedKeys.AddRange(Tail(fields, 2));
                    Of(data).Camp = camp;
                    return true;
                default:
                    return false;
            }
        }

        private static ExpeditionSaveData ParseExpedition(string[] f) => new ExpeditionSaveData
        {
            CurrentRoomId = ParseInt(f, 1),
            StepsTaken = ParseInt(f, 2),
            Light = ParseInt(f, 3),
            Food = ParseInt(f, 4),
            Torches = ParseInt(f, 5),
            Firewood = ParseInt(f, 6),
            Gold = ParseInt(f, 7),
            EntranceId = ParseInt(f, 8),
            BossRoomId = ParseInt(f, 9),
            MapSeed = ParseInt(f, 10),
            BattlesWon = f.Length > 11 ? ParseInt(f, 11) : 0
        };

        private static RoomSaveData ParseRoom(string[] f) => new RoomSaveData
        {
            Id = ParseInt(f, 1),
            Type = Field(f, 2),
            EncounterId = Field(f, 3),
            Gold = ParseInt(f, 4),
            IsCleared = ParseBool(f, 5),
            IsVisited = ParseBool(f, 6)
        };

        private static ExpeditionSaveData Of(GameSaveData data) =>
            data.Expedition ?? throw new FormatException("exp 줄보다 원정 세부 정보가 먼저 나왔습니다.");
    }
}
