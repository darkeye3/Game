using System.Text;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Status;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Stress;

namespace Darkest.Unity.Presentation.Presenters
{
    /// <summary>도메인 상태 → 표시 문자열. 표기 규칙을 한 곳에 모아 화면마다 다르게 쓰지 않게 한다.</summary>
    public static class TextFormat
    {
        public static void AppendCombatant(StringBuilder sb, Combatant combatant, int rank)
        {
            sb.Append(rank).Append("열 ").Append(combatant.DisplayName)
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
