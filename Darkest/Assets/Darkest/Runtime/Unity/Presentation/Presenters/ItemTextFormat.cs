using System.Collections.Generic;
using System.Text;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Hamlet;
using Darkest.Domain.Items;

namespace Darkest.Unity.Presentation.Presenters
{
    /// <summary>장신구·야영 스킬·마을 건물 표시 문자열. TextFormat이 커지지 않게 영지/아이템 쪽 표기만 따로 둔다.</summary>
    public static class ItemTextFormat
    {
        public static string Trinket(TrinketDefinition trinket)
        {
            var sb = new StringBuilder();
            sb.Append(trinket.DisplayName).Append(trinket.Rarity == TrinketRarity.Rare ? " [희귀]" : "").Append("  ");
            AppendBonuses(sb, trinket.Bonuses);
            return sb.ToString();
        }

        public static string CampSkill(CampSkillDefinition skill)
        {
            var sb = new StringBuilder();
            sb.Append(skill.DisplayName).Append(" (").Append(skill.Cost).Append("점, ").Append(TargetName(skill.Target)).Append(")  ");
            for (int i = 0; i < skill.Effects.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                var effect = skill.Effects[i];
                switch (effect.Kind)
                {
                    case CampEffectKind.HealPercent: sb.Append("HP ").Append(effect.Amount).Append("% 회복"); break;
                    case CampEffectKind.Stress: sb.Append("스트레스 ").Append(Signed(effect.Amount)); break;
                    default: sb.Append(StatName(effect.Stat)).Append(' ').Append(Signed(effect.Amount)).Append("(다음 전투)"); break;
                }
            }
            return sb.ToString();
        }

        public static string TargetName(CampSkillTarget target)
        {
            switch (target)
            {
                case CampSkillTarget.Self: return "자신";
                case CampSkillTarget.Ally: return "아군 1명";
                default: return "파티 전체";
            }
        }

        public static string BuildingName(BuildingType type)
        {
            switch (type)
            {
                case BuildingType.Stagecoach: return "역마차";
                case BuildingType.Tavern: return "선술집";
                case BuildingType.Provisioner: return "보급상";
                case BuildingType.Guild: return "훈련소";
                default: return type.ToString();
            }
        }

        /// <summary>건물의 현재 효과 한 줄(단계 반영 수치).</summary>
        public static string BuildingEffect(BuildingType type, TownService town)
        {
            var b = town.Buildings;
            switch (type)
            {
                case BuildingType.Stagecoach: return "모집 후보 " + b.RecruitPoolSize + "명, 명부 " + b.RosterCapacity + "명";
                case BuildingType.Tavern: return "스트레스 -" + b.TavernStressRelief + ", " + b.TavernCost + "G";
                case BuildingType.Provisioner: return "식량 " + town.FoodPrice + "G, 횃불 " + town.TorchPrice + "G, 장작 " + town.FirewoodPrice + "G";
                case BuildingType.Guild: return "귀환 경험치 +" + b.ExpeditionXpBonus;
                default: return string.Empty;
            }
        }

        public static string StatName(StatType stat)
        {
            switch (stat)
            {
                case StatType.MaxHp: return "최대 HP";
                case StatType.Speed: return "속도";
                case StatType.Accuracy: return "명중";
                case StatType.CritChance: return "치명";
                case StatType.Dodge: return "회피";
                case StatType.Protection: return "방어";
                case StatType.DamageMin: return "최소 피해";
                case StatType.DamageMax: return "최대 피해";
                case StatType.StunResist: return "기절 저항";
                case StatType.BleedResist: return "출혈 저항";
                case StatType.BlightResist: return "역병 저항";
                case StatType.DeathblowResist: return "죽음일격 저항";
                default: return stat.ToString();
            }
        }

        private static void AppendBonuses(StringBuilder sb, IReadOnlyList<StatBonus> bonuses)
        {
            for (int i = 0; i < bonuses.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(StatName(bonuses[i].Stat)).Append(' ').Append(Signed(bonuses[i].Amount));
            }
        }

        private static string Signed(int value) => value >= 0 ? "+" + value : value.ToString();
    }
}
