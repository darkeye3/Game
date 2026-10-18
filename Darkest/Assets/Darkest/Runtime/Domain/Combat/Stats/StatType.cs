namespace Darkest.Domain.Combat.Stats
{
    /// <summary>
    /// 확률/퍼센트 계열 스탯은 모두 0~100 정수(%)로 다룬다. float 오차 없이 테스트하기 위해서다.
    /// </summary>
    public enum StatType
    {
        MaxHp = 0,
        Speed,
        Accuracy,
        CritChance,
        Dodge,
        Protection,
        DamageMin,
        DamageMax,
        StunResist,
        BleedResist,
        BlightResist,
        DeathblowResist,
        Count // 배열 크기용. 실제 스탯 아님.
    }
}
