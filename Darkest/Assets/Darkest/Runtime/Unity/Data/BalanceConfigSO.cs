using Darkest.Domain.Dungeon;
using Darkest.Domain.Game;
using UnityEngine;

namespace Darkest.Unity.Data
{
    /// <summary>
    /// 밸런스 수치 SO. 도메인 기본값(GameConfig)을 인스펙터에서 덮어쓴다.
    /// 새 규칙을 추가할 때: 도메인 Rules 클래스에 프로퍼티 → BalanceSections.cs의 해당 섹션에 필드 + ApplyTo 한 줄.
    /// </summary>
    [CreateAssetMenu(menuName = "Darkest/Balance Config", fileName = "BalanceConfig")]
    public sealed class BalanceConfigSO : ScriptableObject
    {
        private static readonly int LightLevelCount = System.Enum.GetValues(typeof(LightLevel)).Length;

        [SerializeField] private CombatSection combat = new CombatSection();
        [SerializeField] private StressSection stress = new StressSection();
        [SerializeField] private DungeonSection dungeon = new DungeonSection();
        [SerializeField] private TownSection town = new TownSection();
        [SerializeField] private ProgressionSection progression = new ProgressionSection();

        public GameConfig CreateConfig()
        {
            var config = new GameConfig();
            combat.ApplyTo(config.Combat);
            stress.ApplyTo(config.Stress);
            dungeon.ApplyTo(config.Dungeon);
            dungeon.ApplyTo(config.Expedition);
            town.ApplyTo(config.Town);
            progression.ApplyTo(config.Progression);
            return config;
        }

        private void OnValidate()
        {
            // 배열 길이가 틀리면 CreateConfig에서 IndexOutOfRange가 나므로 인스펙터 편집 시점에 바로 알린다.
            if (dungeon.lightThresholds.Length != LightLevelCount - 1 || dungeon.stressPerStepByLight.Length != LightLevelCount)
                Debug.LogError("[Darkest][Balance] 빛 단계 배열 길이가 틀렸습니다(경계 " + (LightLevelCount - 1) + "개, 단계별 값 " + LightLevelCount + "개).", this);
            if (town.buildingUpgradeCosts.Length < town.buildingMaxLevel)
                Debug.LogError("[Darkest][Balance] 건물 업그레이드 비용 개수가 최대 단계보다 적습니다.", this);
        }
    }
}
