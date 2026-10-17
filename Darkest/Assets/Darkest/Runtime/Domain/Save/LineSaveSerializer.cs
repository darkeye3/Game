using System;
using System.Globalization;
using System.Text;

namespace Darkest.Domain.Save
{
    /// <summary>
    /// 사람이 읽을 수 있는 줄 단위 포맷. Unity 의존 없이 단위 테스트로 왕복(round-trip)을 검증할 수 있다.
    /// <code>
    /// darkest-save|1
    /// town|gold|week|nextId
    /// hero|id|classId|name|hp|stress|resolveId|dead|deathsDoor|xp
    /// exp|room|steps|light|food|torches|firewood|gold|entrance|boss|seed|battlesWon   (원정 중 저장일 때만)
    /// party|heroId...        embarked|heroId...
    /// room|id|type|encounterId|gold|cleared|visited
    /// link|a|b
    /// </code>
    /// 필드 안의 '|', '\', 줄바꿈은 역슬래시로 이스케이프한다.
    /// </summary>
    public sealed class LineSaveSerializer : ISaveSerializer
    {
        private const string Header = "darkest-save";
        private const char Separator = '|';
        private const char Escape = '\\';

        public string Serialize(GameSaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var sb = new StringBuilder();
            AppendLine(sb, Header, Int(data.Version));
            AppendLine(sb, "town", Int(data.Gold), Int(data.Week), Int(data.NextId));
            foreach (var hero in data.Roster)
                AppendLine(sb, "hero", hero.Id, hero.ClassId, hero.Name, Int(hero.Hp), Int(hero.Stress), hero.ResolveId ?? string.Empty,
                    Bool(hero.IsDead), Bool(hero.IsAtDeathsDoor), Int(hero.Experience));
            if (data.Expedition != null) AppendExpedition(sb, data.Expedition);
            return sb.ToString();
        }

        private static void AppendExpedition(StringBuilder sb, ExpeditionSaveData e)
        {
            AppendLine(sb, "exp", Int(e.CurrentRoomId), Int(e.StepsTaken), Int(e.Light), Int(e.Food), Int(e.Torches),
                Int(e.Firewood), Int(e.Gold), Int(e.EntranceId), Int(e.BossRoomId), Int(e.MapSeed), Int(e.BattlesWon));
            AppendIds(sb, "party", e.PartyIds);
            AppendIds(sb, "embarked", e.EmbarkedIds);
            foreach (var room in e.Rooms)
                AppendLine(sb, "room", Int(room.Id), room.Type, room.EncounterId ?? string.Empty, Int(room.Gold), Bool(room.IsCleared), Bool(room.IsVisited));
            foreach (var link in e.Links) AppendLine(sb, "link", Int(link.A), Int(link.B));
        }

        private static void AppendIds(StringBuilder sb, string tag, System.Collections.Generic.List<string> ids)
        {
            var fields = new string[ids.Count + 1];
            fields[0] = tag;
            for (int i = 0; i < ids.Count; i++) fields[i + 1] = ids[i];
            AppendLine(sb, fields);
        }

        public GameSaveData Deserialize(string text)
        {
            if (string.IsNullOrEmpty(text)) throw new FormatException("세이브 데이터가 비었습니다.");
            var lines = text.Replace("\r\n", "\n").Split('\n');
            var data = new GameSaveData();
            bool hasHeader = false;
            bool hasTown = false;

            for (int lineNo = 0; lineNo < lines.Length; lineNo++)
            {
                if (lines[lineNo].Length == 0) continue;
                var fields = SplitEscaped(lines[lineNo]);
                try
                {
                    switch (fields[0])
                    {
                        case Header:
                            data.Version = ParseInt(fields, 1);
                            if (data.Version > GameSaveData.CurrentVersion)
                                throw new FormatException("지원하지 않는 세이브 버전: " + data.Version);
                            hasHeader = true;
                            break;
                        case "town":
                            data.Gold = ParseInt(fields, 1);
                            data.Week = ParseInt(fields, 2);
                            data.NextId = ParseInt(fields, 3);
                            hasTown = true;
                            break;
                        case "hero":
                            data.Roster.Add(ParseHero(fields));
                            break;
                        case "exp":
                            data.Expedition = ParseExpedition(fields);
                            break;
                        case "party":
                            ExpeditionOf(data).PartyIds.AddRange(Tail(fields));
                            break;
                        case "embarked":
                            ExpeditionOf(data).EmbarkedIds.AddRange(Tail(fields));
                            break;
                        case "room":
                            ExpeditionOf(data).Rooms.Add(ParseRoom(fields));
                            break;
                        case "link":
                            ExpeditionOf(data).Links.Add(new RoomLinkSaveData(ParseInt(fields, 1), ParseInt(fields, 2)));
                            break;
                        default:
                            throw new FormatException("알 수 없는 레코드: " + fields[0]);
                    }
                }
                catch (Exception e) when (e is FormatException || e is IndexOutOfRangeException)
                {
                    // 원인은 유지하고 줄 번호만 덧붙여 다시 던진다(조용히 삼키지 않음).
                    throw new FormatException("세이브 " + (lineNo + 1) + "번째 줄 파싱 실패: " + e.Message, e);
                }
            }

            if (!hasHeader || !hasTown) throw new FormatException("세이브 헤더 또는 마을 정보가 없습니다.");
            return data;
        }

        private static HeroSaveData ParseHero(string[] fields) => new HeroSaveData
        {
            Id = fields[1],
            ClassId = fields[2],
            Name = fields[3],
            Hp = ParseInt(fields, 4),
            Stress = ParseInt(fields, 5),
            ResolveId = fields.Length > 6 ? fields[6] : string.Empty,
            IsDead = fields.Length > 7 && ParseBool(fields, 7),
            IsAtDeathsDoor = fields.Length > 8 && ParseBool(fields, 8),
            Experience = fields.Length > 9 ? ParseInt(fields, 9) : 0
        };

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
            Type = f[2],
            EncounterId = f[3],
            Gold = ParseInt(f, 4),
            IsCleared = ParseBool(f, 5),
            IsVisited = ParseBool(f, 6)
        };

        /// <summary>원정 세부 줄은 반드시 exp 줄 뒤에 와야 한다.</summary>
        private static ExpeditionSaveData ExpeditionOf(GameSaveData data) =>
            data.Expedition ?? throw new FormatException("exp 줄보다 원정 세부 정보가 먼저 나왔습니다.");

        private static string[] Tail(string[] fields)
        {
            var tail = new string[fields.Length - 1];
            Array.Copy(fields, 1, tail, 0, tail.Length);
            return tail;
        }

        private static string Bool(bool value) => value ? "1" : "0";

        private static bool ParseBool(string[] fields, int index)
        {
            int value = ParseInt(fields, index);
            if (value != 0 && value != 1) throw new FormatException("0/1이 아님: " + fields[index]);
            return value == 1;
        }

        private static string Int(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static int ParseInt(string[] fields, int index)
        {
            if (index >= fields.Length) throw new FormatException("필드 부족(" + index + ")");
            if (!int.TryParse(fields[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                throw new FormatException("정수 아님: " + fields[index]);
            return value;
        }

        private static void AppendLine(StringBuilder sb, params string[] fields)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) sb.Append(Separator);
                AppendEscaped(sb, fields[i] ?? string.Empty);
            }
            sb.Append('\n');
        }

        private static void AppendEscaped(StringBuilder sb, string value)
        {
            foreach (char c in value)
            {
                if (c == Separator || c == Escape) sb.Append(Escape).Append(c);
                else if (c == '\n') sb.Append(Escape).Append('n');
                else if (c == '\r') sb.Append(Escape).Append('r');
                else sb.Append(c);
            }
        }

        private static string[] SplitEscaped(string line)
        {
            var fields = new System.Collections.Generic.List<string>();
            var current = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == Escape && i + 1 < line.Length)
                {
                    char next = line[++i];
                    current.Append(next == 'n' ? '\n' : next == 'r' ? '\r' : next);
                }
                else if (c == Separator)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else current.Append(c);
            }
            fields.Add(current.ToString());
            return fields.ToArray();
        }
    }
}
