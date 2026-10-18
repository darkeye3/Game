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
        private readonly PartyHazard _hazard;
        private readonly IEventBus _events;
        private readonly IGameLogger _logger;

        public CorridorTravel(ExpeditionRules rules, StressService stress, PartyHazard hazard, IEventBus events, IGameLogger logger)
        {
            _rules = Guard.NotNull(rules, nameof(rules));
            _stress = Guard.NotNull(stress, nameof(stress));
            _hazard = Guard.NotNull(hazard, nameof(hazard));
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

        /// <summary>살아 있는 인원수만큼 식량을 먹는다. 모자라면 하나도 먹지 않고 전원이 굶주린다.</summary>
        private void CheckHunger(Formation party, Inventory inventory)
        {
            int needed = CountAlive(party);
            bool ate = inventory.TryConsumeFood(needed);
            _events.Publish(new HungerChecked(ate));
            if (ate) return;

            _logger.Info(LogCategory, "식량 부족(필요 " + needed + ", 보유 " + inventory.Food + ") — 파티가 굶주립니다.");
            _stress.ApplyToAll(party.Members, _rules.HungerStress);
            _hazard.DamageAll(party, _rules.HungerDamage);
        }

        private static int CountAlive(Formation party)
        {
            int count = 0;
            for (int i = 0; i < party.Members.Count; i++)
            {
                if (party.Members[i].IsAlive) count++;
            }
            return count;
        }
    }
}
