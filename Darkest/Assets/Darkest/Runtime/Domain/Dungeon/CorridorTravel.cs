using Darkest.Domain.Combat;
using Darkest.Domain.Core;
using Darkest.Domain.Stress;

namespace Darkest.Domain.Dungeon
{
    /// <summary>복도 한 칸 이동 규칙: 빛 감소 → 어둠 스트레스 → 주기적 허기 판정.</summary>
    public sealed class CorridorTravel
    {
        private const string LogCategory = "Expedition";

        private readonly ExpeditionRules _rules;
        private readonly StressService _stress;
        private readonly IRandom _random;
        private readonly IEventBus _events;
        private readonly IGameLogger _logger;

        public CorridorTravel(ExpeditionRules rules, StressService stress, IRandom random, IEventBus events, IGameLogger logger)
        {
            _rules = Guard.NotNull(rules, nameof(rules));
            _stress = Guard.NotNull(stress, nameof(stress));
            _random = Guard.NotNull(random, nameof(random));
            _events = Guard.NotNull(events, nameof(events));
            _logger = logger ?? NullLogger.Instance;
        }

        /// <param name="totalSteps">이번 칸을 포함한 누적 이동 칸 수(허기 주기 계산용)</param>
        public void Step(int stepIndex, int totalSteps, Formation party, Inventory inventory, TorchLight light)
        {
            light.Change(-_rules.LightDecayPerStep);
            _events.Publish(new CorridorStepTaken(stepIndex, light.Value));

            int stress = light.StressPerStep();
            if (stress > 0) _stress.ApplyToAll(party.Members, stress);

            if (_rules.HungerStepInterval > 0 && totalSteps % _rules.HungerStepInterval == 0) CheckHunger(party, inventory);
        }

        private void CheckHunger(Formation party, Inventory inventory)
        {
            bool ate = inventory.TryConsumeFood();
            _events.Publish(new HungerChecked(ate));
            if (ate) return;

            _logger.Info(LogCategory, "식량 부족 — 파티가 굶주립니다.");
            _stress.ApplyToAll(party.Members, _rules.HungerStress);
            for (int i = 0; i < party.Members.Count; i++) party.Members[i].ApplyDamage(_rules.HungerDamage, _random);
        }
    }
}
