using Darkest.Domain.Combat.AI;
using Darkest.Domain.Combat.Resolution;
using Darkest.Domain.Core;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Combat.Flow
{
    /// <summary>
    /// 전투가 의존하는 서비스 묶음(간단 DI 컨테이너 역할). 테스트에서는 원하는 구현만 갈아끼운다.
    /// </summary>
    public sealed class BattleServices
    {
        public BattleServices(IRandom random, IEventBus events, IGameLogger logger, CombatRules rules,
            StressService stress, IHitResolver hitResolver = null, IDamageCalculator damageCalculator = null,
            ITurnOrderStrategy turnOrder = null, IEnemyAI enemyAI = null)
        {
            Random = Guard.NotNull(random, nameof(random));
            Events = Guard.NotNull(events, nameof(events));
            Logger = logger ?? NullLogger.Instance;
            Rules = Guard.NotNull(rules, nameof(rules));
            Stress = Guard.NotNull(stress, nameof(stress));
            HitResolver = hitResolver ?? new DefaultHitResolver(rules);
            DamageCalculator = damageCalculator ?? new DefaultDamageCalculator(rules);
            TurnOrder = turnOrder ?? new SpeedInitiativeTurnOrder(rules);
            EnemyAI = enemyAI ?? new RandomEnemyAI();
        }

        public IRandom Random { get; }
        public IEventBus Events { get; }
        public IGameLogger Logger { get; }
        public CombatRules Rules { get; }
        public StressService Stress { get; }
        public IHitResolver HitResolver { get; }
        public IDamageCalculator DamageCalculator { get; }
        public ITurnOrderStrategy TurnOrder { get; }
        public IEnemyAI EnemyAI { get; }
    }
}
