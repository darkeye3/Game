using System;
using System.Text;
using static Darkest.Domain.Save.LineFormat;

namespace Darkest.Domain.Save
{
    /// <summary>
    /// 사람이 읽을 수 있는 줄 단위 포맷. Unity 의존 없이 단위 테스트로 왕복(round-trip)을 검증할 수 있다.
    /// <code>
    /// darkest-save|3
    /// town|gold|week|nextId
    /// rng|state                     building|type|level        stash|trinketId...
    /// hero|id|classId|name|hp|stress|resolveId|dead|deathsDoor|xp
    ///   equip|heroId|slot|trinketId   status|heroId|kind|dmg|turns   buff|heroId|stat|amount   (해당 hero 줄 뒤)
    /// exp|room|steps|light|food|torches|firewood|gold|entrance|boss|seed|battlesWon   (원정 중 저장일 때만)
    ///   party|heroId...  embarked|heroId...  room|id|type|encounterId|gold|cleared|visited  link|a|b
    ///   loot|trinketId...  camp|respite|heroId:skillId...
    /// </code>
    /// </summary>
    public sealed class LineSaveSerializer : ISaveSerializer
    {
        private const string Header = "darkest-save";

        public string Serialize(GameSaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var sb = new StringBuilder();
            AppendLine(sb, Header, Int(data.Version));
            AppendLine(sb, "town", Int(data.Gold), Int(data.Week), Int(data.NextId));
            if (data.RandomState.HasValue) AppendLine(sb, "rng", ULong(data.RandomState.Value));
            foreach (var building in data.Buildings) AppendLine(sb, "building", building.Key, Int(building.Value));
            if (data.TrinketStash.Count > 0) AppendIds(sb, "stash", data.TrinketStash);
            foreach (var hero in data.Roster) AppendHero(sb, hero);
            if (data.Expedition != null) ExpeditionLineRecords.Append(sb, data.Expedition);
            return sb.ToString();
        }

        private static void AppendHero(StringBuilder sb, HeroSaveData hero)
        {
            AppendLine(sb, "hero", hero.Id, hero.ClassId, hero.Name, Int(hero.Hp), Int(hero.Stress), hero.ResolveId ?? string.Empty,
                Bool(hero.IsDead), Bool(hero.IsAtDeathsDoor), Int(hero.Experience));
            for (int slot = 0; slot < hero.Trinkets.Count; slot++)
            {
                if (!string.IsNullOrEmpty(hero.Trinkets[slot])) AppendLine(sb, "equip", hero.Id, Int(slot), hero.Trinkets[slot]);
            }
            foreach (var status in hero.Statuses)
                AppendLine(sb, "status", hero.Id, status.Kind, Int(status.DamagePerTurn), Int(status.RemainingTurns));
            foreach (var buff in hero.CampBuffs) AppendLine(sb, "buff", hero.Id, buff.Stat, Int(buff.Amount));
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
                    if (fields[0] == Header)
                    {
                        data.Version = ParseInt(fields, 1);
                        if (data.Version > GameSaveData.CurrentVersion)
                            throw new FormatException("지원하지 않는 세이브 버전: " + data.Version);
                        hasHeader = true;
                    }
                    else if (fields[0] == "town")
                    {
                        data.Gold = ParseInt(fields, 1);
                        data.Week = ParseInt(fields, 2);
                        data.NextId = ParseInt(fields, 3);
                        hasTown = true;
                    }
                    else if (!TryReadTownRecord(data, fields) && !ExpeditionLineRecords.TryRead(data, fields))
                    {
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

        private static bool TryReadTownRecord(GameSaveData data, string[] fields)
        {
            switch (fields[0])
            {
                case "rng": data.RandomState = ParseULong(fields, 1); return true;
                case "building": data.Buildings[Field(fields, 1)] = ParseInt(fields, 2); return true;
                case "stash": data.TrinketStash.AddRange(Tail(fields)); return true;
                case "hero": data.Roster.Add(ParseHero(fields)); return true;
                case "equip": SetTrinket(HeroOf(data, fields), ParseInt(fields, 2), Field(fields, 3)); return true;
                case "status":
                    HeroOf(data, fields).Statuses.Add(new StatusSaveData(Field(fields, 2), ParseInt(fields, 3), ParseInt(fields, 4)));
                    return true;
                case "buff": HeroOf(data, fields).CampBuffs.Add(new StatBonusSaveData(Field(fields, 2), ParseInt(fields, 3))); return true;
                default: return false;
            }
        }

        private static HeroSaveData ParseHero(string[] fields) => new HeroSaveData
        {
            Id = Field(fields, 1),
            ClassId = Field(fields, 2),
            Name = Field(fields, 3),
            Hp = ParseInt(fields, 4),
            Stress = ParseInt(fields, 5),
            ResolveId = fields.Length > 6 ? fields[6] : string.Empty,
            IsDead = fields.Length > 7 && ParseBool(fields, 7),
            IsAtDeathsDoor = fields.Length > 8 && ParseBool(fields, 8),
            Experience = fields.Length > 9 ? ParseInt(fields, 9) : 0
        };

        private static void SetTrinket(HeroSaveData hero, int slot, string trinketId)
        {
            if (slot < 0) throw new FormatException("음수 장신구 슬롯: " + slot);
            while (hero.Trinkets.Count <= slot) hero.Trinkets.Add(string.Empty);
            hero.Trinkets[slot] = trinketId;
        }

        /// <summary>영웅 세부 줄(equip/status/buff)은 해당 hero 줄 뒤에 와야 한다.</summary>
        private static HeroSaveData HeroOf(GameSaveData data, string[] fields)
        {
            string id = Field(fields, 1);
            for (int i = data.Roster.Count - 1; i >= 0; i--)
            {
                if (data.Roster[i].Id == id) return data.Roster[i];
            }
            throw new FormatException(fields[0] + " 줄의 영웅이 앞에 없습니다: " + id);
        }
    }
}
