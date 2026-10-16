using Darkest.Domain.Core;

namespace Darkest.Domain.Dungeon
{
    /// <summary>탐험용 소모품 + 획득 골드.</summary>
    public sealed class Inventory
    {
        public Inventory(int food, int torches)
        {
            Food = Guard.NonNegative(food, nameof(food));
            Torches = Guard.NonNegative(torches, nameof(torches));
        }

        public int Food { get; private set; }
        public int Torches { get; private set; }
        public int Gold { get; private set; }

        public bool TryConsumeFood()
        {
            if (Food <= 0) return false;
            Food--;
            return true;
        }

        public bool TryConsumeTorch()
        {
            if (Torches <= 0) return false;
            Torches--;
            return true;
        }

        public void AddFood(int amount) => Food += Guard.NonNegative(amount, nameof(amount));
        public void AddGold(int amount) => Gold += Guard.NonNegative(amount, nameof(amount));
    }

    public enum LightLevel
    {
        Radiant,
        Dim,
        Shadowy,
        Dark
    }

    /// <summary>횃불 빛 수치. 낮을수록 복도 스트레스가 커진다.</summary>
    public sealed class TorchLight
    {
        private readonly ExpeditionRules _rules;

        public TorchLight(ExpeditionRules rules)
        {
            _rules = Guard.NotNull(rules, nameof(rules));
            Value = rules.MaxLight;
        }

        public int Value { get; private set; }

        public LightLevel Level =>
            Value >= _rules.RadiantThreshold ? LightLevel.Radiant :
            Value >= _rules.DimThreshold ? LightLevel.Dim :
            Value >= _rules.ShadowyThreshold ? LightLevel.Shadowy : LightLevel.Dark;

        public void Change(int delta)
        {
            int next = Value + delta;
            Value = next < 0 ? 0 : next > _rules.MaxLight ? _rules.MaxLight : next;
        }

        public int StressPerStep()
        {
            switch (Level)
            {
                case LightLevel.Radiant: return _rules.StressPerStepRadiant;
                case LightLevel.Dim: return _rules.StressPerStepDim;
                case LightLevel.Shadowy: return _rules.StressPerStepShadowy;
                default: return _rules.StressPerStepDark;
            }
        }
    }
}
