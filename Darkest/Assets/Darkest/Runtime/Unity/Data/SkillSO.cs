using System.Collections.Generic;
using Darkest.Domain.Combat.Skills;
using UnityEngine;

namespace Darkest.Unity.Data
{
    [CreateAssetMenu(menuName = "Darkest/Skill", fileName = "Skill_")]
    public sealed class SkillSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Tooltip("사용 가능 열. 예: 1,2")]
        [SerializeField] private string usableFrom = "1,2";
        [SerializeField] private TargetSide side = TargetSide.Enemy;
        [Tooltip("타격 가능 열. 예: 1,2,3")]
        [SerializeField] private string targetRanks = "1,2";
        [SerializeField] private bool hitsAllTargets;
        [SerializeField] private int accuracy = 85;
        [Tooltip("무기 피해 대비 %. 0이면 피해 없음")]
        [SerializeField] private int damagePercent = 100;
        [SerializeField] private int critBonus;
        [Tooltip("사용 후 이동. 음수=전진, 양수=후퇴")]
        [SerializeField] private int selfMove;
        [SerializeField] private List<SkillEffectData> effects = new List<SkillEffectData>();

        public string Id => id;

        /// <summary>SO → 도메인 정의. 같은 SO는 1회만 변환하도록 ContentDatabaseSO가 캐시한다.</summary>
        public SkillDefinition ToDefinition()
        {
            var runtimeEffects = new ISkillEffect[effects.Count];
            for (int i = 0; i < effects.Count; i++) runtimeEffects[i] = SkillEffectFactory.Create(effects[i].ToSpec());
            return new SkillDefinition(id, displayName, RankMaskExtensions.Parse(usableFrom), side,
                RankMaskExtensions.Parse(targetRanks), hitsAllTargets, accuracy, damagePercent, critBonus, selfMove, runtimeEffects);
        }

        internal void Populate(string newId, string newName, string from, TargetSide newSide, string targets, bool all,
            int newAccuracy, int newDamagePercent, int newCritBonus, int newSelfMove, List<SkillEffectData> newEffects)
        {
            id = newId; displayName = newName; usableFrom = from; side = newSide; targetRanks = targets;
            hitsAllTargets = all; accuracy = newAccuracy; damagePercent = newDamagePercent; critBonus = newCritBonus;
            selfMove = newSelfMove; effects = newEffects;
        }
    }
}
