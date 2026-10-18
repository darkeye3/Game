using System.Collections.Generic;
using Darkest.Domain.Core;
using Darkest.Domain.Items;

namespace Darkest.Domain.Dungeon
{
    /// <summary>출발 전 구매할 보급품 수량(값 객체). 항목이 늘어도 메서드 시그니처가 늘어나지 않게 묶는다.</summary>
    public readonly struct Provisions
    {
        public Provisions(int food, int torches, int firewood = 0)
        {
            Food = food;
            Torches = torches;
            Firewood = firewood;
        }

        public int Food { get; }
        public int Torches { get; }
        public int Firewood { get; }

        public bool IsValid => Food >= 0 && Torches >= 0 && Firewood >= 0;

        public Provisions WithFood(int food) => new Provisions(food, Torches, Firewood);
        public Provisions WithTorches(int torches) => new Provisions(Food, torches, Firewood);
        public Provisions WithFirewood(int firewood) => new Provisions(Food, Torches, firewood);
    }

    /// <summary>탐험용 소모품 + 획득 골드·장신구.</summary>
    public sealed class Inventory
    {
        private readonly List<TrinketDefinition> _trinkets = new List<TrinketDefinition>();

        public Inventory(int food, int torches, int firewood = 0)
        {
            Food = Guard.NonNegative(food, nameof(food));
            Torches = Guard.NonNegative(torches, nameof(torches));
            Firewood = Guard.NonNegative(firewood, nameof(firewood));
        }

        public Inventory(Provisions provisions) : this(provisions.Food, provisions.Torches, provisions.Firewood) { }

        public int Food { get; private set; }
        public int Torches { get; private set; }
        public int Firewood { get; private set; }
        public int Gold { get; private set; }

        /// <summary>이번 탐험에서 주운 장신구. 귀환하면 마을 보관함으로 간다(전멸 시 잃음).</summary>
        public IReadOnlyList<TrinketDefinition> Trinkets => _trinkets;

        public bool TryConsumeFirewood()
        {
            if (Firewood <= 0) return false;
            Firewood--;
            return true;
        }

        /// <summary>amount개를 한 번에 소모. 부족하면 아무것도 소모하지 않고 false.</summary>
        public bool TryConsumeFood(int amount)
        {
            Guard.NonNegative(amount, nameof(amount));
            if (Food < amount) return false;
            Food -= amount;
            return true;
        }

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
        public void AddTrinket(TrinketDefinition trinket) => _trinkets.Add(Guard.NotNull(trinket, nameof(trinket)));
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
