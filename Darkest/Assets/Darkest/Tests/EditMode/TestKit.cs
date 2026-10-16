using System;
using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Flow;
using Darkest.Domain.Combat.Resolution;
using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Content;
using Darkest.Domain.Core;
using Darkest.Domain.Stress;

namespace Darkest.Tests
{
    /// <summary>
    /// 미리 정한 값을 순서대로 돌려주는 난수. 큐가 비면 minInclusive를 돌려준다(= 확률 판정 성공).
    /// </summary>
    internal sealed class ScriptedRandom : IRandom
    {
        private readonly Queue<int> _values = new Queue<int>();

        public ScriptedRandom(params int[] values)
        {
            foreach (var v in values) _values.Enqueue(v);
        }

        public int Seed => -1;

        public void Push(params int[] values)
        {
            foreach (var v in values) _values.Enqueue(v);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (_values.Count == 0) return minInclusive;
            int value = _values.Dequeue();
            if (value < minInclusive || (maxExclusive > minInclusive && value >= maxExclusive))
                throw new InvalidOperationException("스크립트 값 " + value + "이 범위 [" + minInclusive + "," + maxExclusive + ") 밖입니다.");
            return value;
        }
    }

    /// <summary>테스트용 객체 조립 헬퍼.</summary>
    internal static class TestKit
    {
        public const int AlwaysSucceed = 0;
        public const int AlwaysFail = 99;

        public static StatBlock Stats(int hp = 20, int speed = 0, int dodge = 0, int protection = 0,
            int min = 5, int max = 5, int crit = 0, int deathblow = 0) =>
            StatBlock.Create().Hp(hp).Speed(speed).Dodge(dodge).Protection(protection).Crit(crit)
                .Damage(min, max).Resist(0, 0, 0, deathblow).Build();

        public static SkillDefinition Attack(string id = "atk", string from = "1,2,3,4", string targets = "1,2,3,4",
            int accuracy = 100, int damagePercent = 100, bool hitsAll = false, params ISkillEffect[] effects) =>
            new SkillDefinition(id, id, RankMaskExtensions.Parse(from), TargetSide.Enemy, RankMaskExtensions.Parse(targets),
                hitsAll, accuracy, damagePercent, 0, 0, effects);

        public static Hero Hero(string id = "h", StatBlock stats = null, params SkillDefinition[] skills) =>
            new Hero(id, id, "test_class", stats ?? Stats(), skills.Length > 0 ? skills : new[] { Attack() }, null, 200);

        public static Monster Monster(string id = "m", StatBlock stats = null, params SkillDefinition[] skills) =>
            new Monster(id, id, "test_monster", stats ?? Stats(), skills.Length > 0 ? skills : new[] { Attack() }, null);

        public static Formation Party(params Combatant[] members)
        {
            var formation = new Formation(members.Length > 0 ? members[0].Team : Team.Heroes);
            foreach (var m in members) formation.Add(m);
            return formation;
        }

        public static StressService StressService(IRandom random, IEventBus events = null, StressRules rules = null)
        {
            var catalog = DefaultContent.Create();
            return new StressService(rules ?? new StressRules(), catalog.Afflictions, catalog.Virtues, random,
                events ?? new EventBus(), NullLogger.Instance);
        }

        public static BattleServices Services(IRandom random, IEventBus events = null, CombatRules rules = null, IGameLogger logger = null)
        {
            events = events ?? new EventBus();
            rules = rules ?? new CombatRules();
            return new BattleServices(random, events, logger ?? NullLogger.Instance, rules, StressService(random, events));
        }

        /// <summary>이벤트를 타입별로 수집한다.</summary>
        public static List<T> Collect<T>(IEventBus events) where T : IDomainEvent
        {
            var list = new List<T>();
            events.Subscribe<T>(list.Add);
            return list;
        }
    }
}
