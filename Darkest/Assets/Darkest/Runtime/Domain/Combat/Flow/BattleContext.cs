using System.Collections.Generic;
using Darkest.Domain.Combat.AI;
using Darkest.Domain.Combat.Commands;
using Darkest.Domain.Combat.Resolution;
using Darkest.Domain.Core;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Combat.Flow
{
    /// <summary>
    /// 전투 1회의 "모든 상태"가 모이는 곳. 상태를 한 곳에 모아 두면 디버그 뷰가 이것 하나만 그리면 되고,
    /// 버그 재현 시 (seed + 명령 기록)만으로 같은 전투를 다시 만들 수 있다.
    /// </summary>
    public sealed class BattleContext
    {
        private readonly BattleServices _services;
        private readonly List<IBattleCommand> _history = new List<IBattleCommand>();
        private readonly Queue<Combatant> _turnQueue = new Queue<Combatant>();

        public BattleContext(Formation heroes, Formation monsters, BattleServices services)
        {
            Heroes = Guard.NotNull(heroes, nameof(heroes));
            Monsters = Guard.NotNull(monsters, nameof(monsters));
            _services = Guard.NotNull(services, nameof(services));
            Guard.That(heroes.Team == Team.Heroes && monsters.Team == Team.Monsters, "진형 팀 배치가 잘못되었습니다.");
        }

        public Formation Heroes { get; }
        public Formation Monsters { get; }
        public int Round { get; internal set; }
        public Combatant CurrentActor { get; internal set; }
        public IReadOnlyList<IBattleCommand> History => _history;
        public IReadOnlyCollection<Combatant> PendingTurns => _turnQueue;

        public IRandom Random => _services.Random;
        public IEventBus Events => _services.Events;
        public IGameLogger Logger => _services.Logger;
        public CombatRules Rules => _services.Rules;
        public StressService Stress => _services.Stress;
        public IHitResolver HitResolver => _services.HitResolver;
        public IDamageCalculator DamageCalculator => _services.DamageCalculator;
        public ITurnOrderStrategy TurnOrder => _services.TurnOrder;
        public IEnemyAI EnemyAI => _services.EnemyAI;

        public Formation FormationOf(Combatant combatant) => combatant.Team == Team.Heroes ? Heroes : Monsters;
        public Formation OpponentsOf(Combatant combatant) => combatant.Team == Team.Heroes ? Monsters : Heroes;

        internal Queue<Combatant> TurnQueue => _turnQueue;
        internal void Record(IBattleCommand command) => _history.Add(command);
    }
}
