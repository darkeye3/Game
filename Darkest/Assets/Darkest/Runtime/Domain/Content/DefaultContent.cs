using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Content
{
    /// <summary>
    /// 코드로 정의한 더미 콘텐츠. ScriptableObject 데이터가 없을 때의 fallback이자 테스트 픽스처다.
    /// 에디터 메뉴 "Darkest/Generate Content Assets"가 이 내용을 SO 에셋으로 내보낸다.
    /// </summary>
    public static class DefaultContent
    {
        public static ContentCatalog Create() => new ContentCatalog(
            new[] { Warden(), Hunter(), Alchemist(), Sister() },
            new[] { SkeletonSoldier(), SkeletonArcher(), Cultist(), Ghoul(), OssuaryLord() },
            new[]
            {
                new MonsterGroupDefinition("grp_bones", new[] { "skeleton_soldier", "skeleton_soldier", "skeleton_archer" }, false, 120),
                new MonsterGroupDefinition("grp_cult", new[] { "ghoul", "cultist", "cultist" }, false, 150),
                new MonsterGroupDefinition("grp_mixed", new[] { "ghoul", "skeleton_soldier", "cultist", "skeleton_archer" }, false, 200),
                new MonsterGroupDefinition("grp_boss_ossuary", new[] { "ossuary_lord", "skeleton_archer", "cultist" }, true, 800)
            },
            new[] { Altar(), Sarcophagus(), StrangeFountain() },
            new[]
            {
                new ResolveDefinition("aff_fearful", "공포", false, Mods("aff_fearful", (StatType.Accuracy, -10), (StatType.Dodge, 10)), 30, 8),
                new ResolveDefinition("aff_paranoid", "편집증", false, Mods("aff_paranoid", (StatType.Dodge, -10), (StatType.Speed, 2)), 35, 10),
                new ResolveDefinition("aff_hopeless", "절망", false, Mods("aff_hopeless", (StatType.Accuracy, -15), (StatType.Speed, -2)), 25, 12),
                new ResolveDefinition("vir_stalwart", "굳건함", true, Mods("vir_stalwart", (StatType.Protection, 15)), 30, -6),
                new ResolveDefinition("vir_courageous", "용맹", true, Mods("vir_courageous", (StatType.Accuracy, 10), (StatType.CritChance, 5)), 35, -8)
            },
            new[] { "라이너스", "베아트릭스", "오스왈드", "미렐라", "고드윈", "엘시", "하랄드", "이졸데", "콘라드", "세라" });

        // ── 영웅 ─────────────────────────────────────────────

        private static HeroClassDefinition Warden() => new HeroClassDefinition("warden", "수호기사",
            StatBlock.Create().Hp(33).Speed(1).Accuracy(0).Crit(3).Dodge(5).Protection(10).Damage(6, 12).Resist(40, 30, 30, 67).Build(),
            new[]
            {
                Skill("warden_smite", "강타", "1,2", TargetSide.Enemy, "1,2", false, 85, 100, 0),
                Skill("warden_stun", "방패 가격", "1,2", TargetSide.Enemy, "1,2", false, 90, 50, 0, 0, Effect(SkillEffectKind.Stun, 0, chance: 100)),
                Skill("warden_rally", "격려의 함성", "1,2,3", TargetSide.Ally, "1,2,3,4", true, 100, 0, 0, 0, Effect(SkillEffectKind.Stress, -5)),
                Skill("warden_charge", "돌진", "3,4", TargetSide.Enemy, "1,2", false, 80, 60, 0, -2)
            }, "hero_warden");

        private static HeroClassDefinition Hunter() => new HeroClassDefinition("hunter", "사냥꾼",
            StatBlock.Create().Hp(23).Speed(5).Accuracy(5).Crit(6).Dodge(15).Protection(0).Damage(5, 10).Resist(30, 30, 30, 67).Build(),
            new[]
            {
                Skill("hunter_slash", "베기", "1,2,3", TargetSide.Enemy, "1,2", false, 85, 100, 5),
                Skill("hunter_shot", "조준 사격", "2,3,4", TargetSide.Enemy, "2,3,4", false, 85, 85, 8),
                Skill("hunter_bleed", "갈고리 칼날", "1,2,3", TargetSide.Enemy, "1,2,3", false, 85, 50, 0, 0, Effect(SkillEffectKind.Bleed, 3, duration: 3, chance: 100)),
                Skill("hunter_pull", "올가미", "3,4", TargetSide.Enemy, "3,4", false, 90, 40, 0, 0, Effect(SkillEffectKind.MoveTarget, -2))
            }, "hero_hunter");

        private static HeroClassDefinition Alchemist() => new HeroClassDefinition("alchemist", "연금술사",
            StatBlock.Create().Hp(22).Speed(6).Accuracy(0).Crit(2).Dodge(10).Protection(0).Damage(4, 7).Resist(20, 20, 60, 67).Build(),
            new[]
            {
                Skill("alch_flask", "독병 투척", "2,3,4", TargetSide.Enemy, "3,4", false, 95, 25, 0, 0, Effect(SkillEffectKind.Blight, 4, duration: 3, chance: 100)),
                Skill("alch_gas", "질식 가스", "3,4", TargetSide.Enemy, "1,2", true, 90, 15, 0, 0, Effect(SkillEffectKind.Stun, 0, chance: 60)),
                Skill("alch_tonic", "강장제", "2,3,4", TargetSide.Ally, "1,2,3,4", false, 100, 0, 0, 0, Effect(SkillEffectKind.Heal, 3, 6)),
                Skill("alch_knife", "단검", "1,2,3", TargetSide.Enemy, "1,2", false, 85, 80, 0)
            }, "hero_alchemist");

        private static HeroClassDefinition Sister() => new HeroClassDefinition("sister", "수녀",
            StatBlock.Create().Hp(24).Speed(4).Accuracy(0).Crit(1).Dodge(5).Protection(0).Damage(4, 8).Resist(30, 30, 30, 67).Build(),
            new[]
            {
                Skill("sister_heal", "치유의 기도", "3,4", TargetSide.Ally, "1,2,3,4", false, 100, 0, 0, 0, Effect(SkillEffectKind.Heal, 4, 8)),
                Skill("sister_mass_heal", "성가", "3,4", TargetSide.Ally, "1,2,3,4", true, 100, 0, 0, 0, Effect(SkillEffectKind.Heal, 1, 3)),
                Skill("sister_light", "성광", "2,3,4", TargetSide.Enemy, "1,2,3", false, 85, 60, 0, 0, Effect(SkillEffectKind.Stun, 0, chance: 50)),
                Skill("sister_mace", "철퇴", "1,2", TargetSide.Enemy, "1,2", false, 85, 90, 0)
            }, "hero_sister");

        // ── 몬스터 ───────────────────────────────────────────

        private static MonsterDefinition SkeletonSoldier() => new MonsterDefinition("skeleton_soldier", "해골 병사",
            StatBlock.Create().Hp(14).Speed(2).Accuracy(0).Crit(3).Dodge(5).Protection(15).Damage(3, 7).Resist(25, 100, 10).Build(),
            new[]
            {
                Skill("skel_cut", "녹슨 칼", "1,2", TargetSide.Enemy, "1,2", false, 82, 100, 0),
                Skill("skel_shield", "방패 밀치기", "1,2", TargetSide.Enemy, "1,2", false, 82, 50, 0, 0, Effect(SkillEffectKind.MoveTarget, 1))
            }, "mon_skeleton_soldier");

        private static MonsterDefinition SkeletonArcher() => new MonsterDefinition("skeleton_archer", "해골 궁수",
            StatBlock.Create().Hp(10).Speed(6).Accuracy(0).Crit(6).Dodge(10).Protection(0).Damage(3, 6).Resist(10, 100, 10).Build(),
            new[] { Skill("skel_arrow", "화살", "2,3,4", TargetSide.Enemy, "1,2,3,4", false, 80, 100, 3) }, "mon_skeleton_archer");

        private static MonsterDefinition Cultist() => new MonsterDefinition("cultist", "광신도",
            StatBlock.Create().Hp(12).Speed(5).Accuracy(0).Crit(2).Dodge(10).Protection(0).Damage(2, 5).Resist(20, 20, 20).Build(),
            new[]
            {
                Skill("cult_whisper", "광기의 속삭임", "2,3,4", TargetSide.Enemy, "1,2,3,4", false, 85, 30, 0, 0, Effect(SkillEffectKind.Stress, 12)),
                Skill("cult_knife", "의식용 단검", "1,2,3", TargetSide.Enemy, "1,2", false, 80, 100, 0, 0, Effect(SkillEffectKind.Bleed, 2, duration: 2, chance: 80))
            }, "mon_cultist");

        private static MonsterDefinition Ghoul() => new MonsterDefinition("ghoul", "구울",
            StatBlock.Create().Hp(20).Speed(3).Accuracy(0).Crit(5).Dodge(0).Protection(10).Damage(4, 8).Resist(30, 30, 60).Build(),
            new[]
            {
                Skill("ghoul_claw", "할퀴기", "1,2", TargetSide.Enemy, "1,2", false, 82, 100, 0),
                Skill("ghoul_bite", "부패한 이빨", "1,2", TargetSide.Enemy, "1,2,3", false, 80, 70, 0, 0, Effect(SkillEffectKind.Blight, 2, duration: 3, chance: 80))
            }, "mon_ghoul");

        private static MonsterDefinition OssuaryLord() => new MonsterDefinition("ossuary_lord", "납골당 군주",
            StatBlock.Create().Hp(55).Speed(4).Accuracy(5).Crit(8).Dodge(5).Protection(20).Damage(6, 12).Resist(60, 100, 40).Build(),
            new[]
            {
                Skill("lord_sweep", "망자의 낫", "1,2", TargetSide.Enemy, "1,2,3", true, 80, 70, 0),
                Skill("lord_dread", "공포의 포효", "1,2,3", TargetSide.Enemy, "1,2,3,4", true, 85, 0, 0, 0, Effect(SkillEffectKind.Stress, 10)),
                Skill("lord_crush", "분쇄", "1", TargetSide.Enemy, "1", false, 85, 130, 5, 0, Effect(SkillEffectKind.Stun, 0, chance: 70))
            }, "mon_ossuary_lord");

        // ── 골동품 ───────────────────────────────────────────

        private static CurioDefinition Altar() => new CurioDefinition("curio_altar", "버려진 제단", new[]
        {
            new CurioOutcome(40, CurioEffectKind.PartyStress, -15, "희미한 축복이 마음을 달랜다."),
            new CurioOutcome(30, CurioEffectKind.PartyStress, 15, "제단에서 속삭임이 들린다..."),
            new CurioOutcome(30, CurioEffectKind.Nothing, 0, "아무 일도 일어나지 않았다.")
        });

        private static CurioDefinition Sarcophagus() => new CurioDefinition("curio_sarcophagus", "석관", new[]
        {
            new CurioOutcome(50, CurioEffectKind.Gold, 250, "금화가 쏟아진다!"),
            new CurioOutcome(25, CurioEffectKind.PartyDamage, 4, "독침 함정!"),
            new CurioOutcome(25, CurioEffectKind.Food, 2, "보존된 식량을 찾았다.")
        });

        private static CurioDefinition StrangeFountain() => new CurioDefinition("curio_fountain", "기묘한 샘", new[]
        {
            new CurioOutcome(45, CurioEffectKind.PartyHeal, 6, "맑은 물이 상처를 씻어낸다."),
            new CurioOutcome(30, CurioEffectKind.Light, 30, "물빛이 주변을 밝힌다."),
            new CurioOutcome(25, CurioEffectKind.PartyStress, 10, "물속에서 무언가 눈을 떴다.")
        });

        // ── 헬퍼 ─────────────────────────────────────────────

        private static SkillDefinition Skill(string id, string name, string usableFrom, TargetSide side, string targets,
            bool hitsAll, int accuracy, int damagePercent, int critBonus, int selfMove = 0, params ISkillEffect[] effects) =>
            new SkillDefinition(id, name, RankMaskExtensions.Parse(usableFrom), side, RankMaskExtensions.Parse(targets),
                hitsAll, accuracy, damagePercent, critBonus, selfMove, effects);

        private static ISkillEffect Effect(SkillEffectKind kind, int amount, int amountMax = 0, int duration = 0, int chance = 100) =>
            SkillEffectFactory.Create(new SkillEffectSpec(kind, amount, amountMax, duration, chance));

        private static StatModifier[] Mods(string resolveId, params (StatType stat, int amount)[] values)
        {
            var modifiers = new StatModifier[values.Length];
            for (int i = 0; i < values.Length; i++)
                modifiers[i] = new StatModifier(values[i].stat, values[i].amount, ResolveDefinition.SourceIdFor(resolveId));
            return modifiers;
        }
    }
}
