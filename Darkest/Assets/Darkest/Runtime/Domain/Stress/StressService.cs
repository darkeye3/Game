using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Core;

namespace Darkest.Domain.Stress
{
    /// <summary>
    /// 스트레스 변화의 단일 진입점. 전투/던전/마을 어디서든 이 서비스를 거쳐야
    /// 결의 판정·심장마비·이벤트 발행이 한 곳에서 일관되게 일어난다.
    /// </summary>
    public sealed class StressService
    {
        private const string LogCategory = "Stress";

        private readonly StressRules _rules;
        private readonly IReadOnlyList<ResolveDefinition> _afflictions;
        private readonly IReadOnlyList<ResolveDefinition> _virtues;
        private readonly IRandom _random;
        private readonly IEventBus _events;
        private readonly IGameLogger _logger;

        public StressService(StressRules rules, IReadOnlyList<ResolveDefinition> afflictions,
            IReadOnlyList<ResolveDefinition> virtues, IRandom random, IEventBus events, IGameLogger logger)
        {
            _rules = Guard.NotNull(rules, nameof(rules));
            Guard.NotEmpty(afflictions, nameof(afflictions));
            Guard.NotEmpty(virtues, nameof(virtues));
            _afflictions = afflictions;
            _virtues = virtues;
            _random = Guard.NotNull(random, nameof(random));
            _events = Guard.NotNull(events, nameof(events));
            _logger = logger ?? NullLogger.Instance;
        }

        public StressRules Rules => _rules;

        public void Apply(Hero hero, int amount)
        {
            Guard.NotNull(hero, nameof(hero));
            if (hero.IsDead || amount == 0) return;

            int actual = hero.Stress.Change(amount);
            if (actual == 0) return;
            _events.Publish(new StressChanged(hero, actual));

            if (actual < 0)
            {
                if (hero.Stress.Current == 0) ClearResolve(hero);
                return;
            }

            if (hero.Stress.State == ResolveState.None && hero.Stress.Current >= _rules.ResolveThreshold)
                TriggerResolve(hero);

            if (hero.Stress.Current >= hero.Stress.Max)
                TriggerHeartAttack(hero);
        }

        public void ApplyToAll(IReadOnlyList<Combatant> party, int amount, Hero except = null)
        {
            for (int i = 0; i < party.Count; i++)
            {
                if (party[i] is Hero hero && hero != except) Apply(hero, amount);
            }
        }

        /// <summary>턴 시작 시 고통(폭주)/미덕(격려) 발동 판정.</summary>
        public void OnTurnStart(Hero hero, IReadOnlyList<Combatant> allies)
        {
            var resolve = hero.Stress.Resolve;
            if (resolve == null || resolve.TurnStartAllyStress == 0) return;
            if (!_random.RollPercent(resolve.TurnStartChance)) return;

            _events.Publish(new ResolveActed(hero, resolve));
            ApplyToAll(allies, resolve.TurnStartAllyStress, hero);
        }

        /// <summary>세이브 복원 전용: 판정 없이 수치/결의를 그대로 되돌린다.</summary>
        public void Restore(Hero hero, int stress, ResolveDefinition resolve)
        {
            hero.Stress.Set(stress);
            if (resolve == null) return;
            hero.Stress.SetResolve(resolve);
            hero.Modifiers.AddRange(resolve.Modifiers);
        }

        public void ClearResolve(Hero hero)
        {
            var resolve = hero.Stress.Resolve;
            if (resolve == null) return;
            hero.Modifiers.RemoveBySource(resolve.ModifierSourceId);
            hero.Stress.ClearResolve();
            _logger.Info(LogCategory, hero.DisplayName + " 결의 해제: " + resolve.DisplayName);
        }

        private void TriggerResolve(Hero hero)
        {
            bool virtuous = _random.RollPercent(_rules.VirtueChance);
            var resolve = _random.Pick(virtuous ? _virtues : _afflictions);

            hero.Stress.SetResolve(resolve);
            hero.Modifiers.AddRange(resolve.Modifiers);
            if (virtuous) hero.Stress.Set(_rules.StressAfterVirtue);

            _logger.Info(LogCategory, hero.DisplayName + " 결의 판정 → " + resolve.DisplayName);
            _events.Publish(new ResolveTriggered(hero, resolve));
        }

        private void TriggerHeartAttack(Hero hero)
        {
            var outcome = hero.SufferHeartAttack();
            hero.Stress.Set(_rules.StressAfterHeartAttack);
            _logger.Warn(LogCategory, hero.DisplayName + " 심장마비 → " + outcome);
            _events.Publish(new HeartAttackSuffered(hero, outcome));
        }
    }
}
