namespace Darkest.Domain.Hamlet
{
    public sealed class TownRules
    {
        public int StartingGold { get; set; } = 1500;
        public int StartingHeroes { get; set; } = 4;
        public int MaxRosterSize { get; set; } = 9;
        public int RecruitPoolSize { get; set; } = 3;
        public int RecruitCost { get; set; } = 300;

        public int TavernCost { get; set; } = 250;
        public int TavernStressRelief { get; set; } = 45;

        public int FoodPrice { get; set; } = 25;
        public int TorchPrice { get; set; } = 40;
        public int FirewoodPrice { get; set; } = 75;
    }
}
