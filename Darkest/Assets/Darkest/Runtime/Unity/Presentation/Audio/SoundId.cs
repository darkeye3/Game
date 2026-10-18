namespace Darkest.Unity.Presentation.Audio
{
    /// <summary>
    /// 효과음 종류. 실제 소리 파일은 Resources/Darkest/Sounds/&lt;이름&gt;(예: Hit.wav)로 넣으면 그 파일을 쓰고,
    /// 없으면 ProceduralSoundFactory가 만든 더미 소리를 쓴다.
    /// </summary>
    public enum SoundId
    {
        Hit,
        Crit,
        Miss,
        Heal,
        Status,
        Stress,
        Death,
        BattleStart,
        Victory,
        Defeat,
        Fled,
        RoomEnter,
        Loot,
        Trinket,
        Camp,
        CampSkill,
        LevelUp,
        Curio
    }
}
