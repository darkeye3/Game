using System.Text;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Combat.Status;
using Darkest.Domain.Combat.Events;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Game;
using Darkest.Domain.Stress;

namespace Darkest.Unity.Presentation.Presenters
{
    /// <summary>도메인 상태 → 표시 문자열. 표기 규칙을 한 곳에 모아 화면마다 다르게 쓰지 않게 한다.</summary>
    public static class TextFormat
    {
        public static void AppendCombatant(StringBuilder sb, Combatant combatant, int rank)
        {
            sb.Append(rank).Append("열 ").Append(combatant.DisplayName);
            if (combatant is Hero leveled) sb.Append(" Lv").Append(leveled.Level);
            sb
              .Append("  HP ").Append(combatant.CurrentHp).Append('/').Append(combatant.MaxHp);

            if (combatant is Hero hero)
            {
                sb.Append("  스트레스 ").Append(hero.Stress.Current);
                if (hero.Stress.Resolve != null) sb.Append(" [").Append(hero.Stress.Resolve.DisplayName).Append(']');
                if (hero.IsAtDeathsDoor) sb.Append(" <죽음의 문턱>");
            }

            var statuses = combatant.Statuses.All;
            for (int i = 0; i < statuses.Count; i++) sb.Append(' ').Append(StatusName(statuses[i].Kind));
            sb.Append('\n');
        }

        public static string Formation(Formation formation)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < formation.Members.Count; i++) AppendCombatant(sb, formation.Members[i], i + 1);
            return sb.ToString();
        }

        public static string BattleSummary(BattleSummary summary)
        {
            var sb = new StringBuilder();
            sb.Append(ResultName(summary.Result)).Append("  (").Append(summary.Rounds).Append("라운드)\n");
            if (summary.GoldGained > 0) sb.Append("전리품 +").Append(summary.GoldGained).Append("G\n");
            for (int i = 0; i < summary.Fallen.Count; i++) sb.Append("✝ ").Append(summary.Fallen[i].DisplayName).Append('\n');
            return sb.ToString();
        }

        public static string ResultName(BattleResult result)
        {
            switch (result)
            {
                case BattleResult.Victory: return "승리!";
                case BattleResult.Fled: return "후퇴 — 마을로 도망쳤다.";
                default: return "패배...";
            }
        }

        /// <summary>스킬 툴팁: 사용 열, 대상 열, 명중, 피해 배율, 부가 효과.</summary>
        public static string SkillTooltip(SkillDefinition skill)
        {
            var sb = new StringBuilder();
            sb.Append(skill.DisplayName).Append("  사용 ").Append(skill.UsableFrom.ToRankString()).Append("열");
            if (skill.Side != TargetSide.Self)
                sb.Append(" → ").Append(skill.IsAttack ? "적 " : "아군 ").Append(skill.TargetRanks.ToRankString()).Append("열")
                  .Append(skill.HitsAllTargets ? " 전체" : "");
            if (skill.IsAttack) sb.Append("  명중 ").Append(skill.Accuracy);
            if (skill.DealsDamage) sb.Append("  피해 ").Append(skill.DamagePercent).Append('%');
            if (skill.CritBonus != 0) sb.Append("  치명 +").Append(skill.CritBonus);
            if (skill.SelfMove != 0) sb.Append("  이동 ").Append(skill.SelfMove > 0 ? "뒤로 " : "앞으로 ").Append(System.Math.Abs(skill.SelfMove));
            for (int i = 0; i < skill.Effects.Count; i++) sb.Append(" / ").Append(skill.Effects[i].Describe());
            return sb.ToString();
        }

        public static string StatusName(StatusKind kind)
        {
            switch (kind)
            {
                case StatusKind.Bleed: return "(출혈)";
                case StatusKind.Blight: return "(역병)";
                case StatusKind.Stun: return "(기절)";
                default: return "(" + kind + ")";
            }
        }

        public static string RoomName(Room room, bool reveal)
        {
            if (!reveal && room.Type != RoomType.Boss) return "방 " + room.Id + " (미탐색)";
            return "방 " + room.Id + " (" + RoomTypeName(room.Type) + (room.IsCleared ? ", 정리됨" : "") + ")";
        }

        public static string RoomTypeName(RoomType type)
        {
            switch (type)
            {
                case RoomType.Entrance: return "입구";
                case RoomType.Empty: return "빈 방";
                case RoomType.Battle: return "전투";
                case RoomType.Curio: return "골동품";
                case RoomType.Treasure: return "보물";
                case RoomType.Boss: return "보스";
                default: return type.ToString();
            }
        }

        public static string LightName(LightLevel level)
        {
            switch (level)
            {
                case LightLevel.Radiant: return "밝음";
                case LightLevel.Dim: return "흐림";
                case LightLevel.Shadowy: return "어둑";
                default: return "암흑";
            }
        }

        public static string ResolveKind(ResolveDefinition resolve) => resolve.IsVirtue ? "미덕" : "고통";
    }
}
