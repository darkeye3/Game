using Darkest.Domain.Core;

namespace Darkest.Domain.Dungeon
{
    public enum ExpeditionPhase
    {
        Exploring,
        InBattle,
        Completed,
        Failed,
        Retreated
    }

    public sealed class RoomEntered : IDomainEvent
    {
        public RoomEntered(Room room) { Room = room; }
        public Room Room { get; }
        public override string ToString() => "RoomEntered " + Room;
    }

    public sealed class CorridorStepTaken : IDomainEvent
    {
        public CorridorStepTaken(int stepIndex, int lightValue) { StepIndex = stepIndex; LightValue = lightValue; }
        public int StepIndex { get; }
        public int LightValue { get; }
        public override string ToString() => "Step " + StepIndex + " light=" + LightValue;
    }

    public sealed class HungerChecked : IDomainEvent
    {
        public HungerChecked(bool hadFood) { HadFood = hadFood; }
        public bool HadFood { get; }
        public override string ToString() => "Hunger " + (HadFood ? "먹음" : "굶음");
    }

    public sealed class LootGained : IDomainEvent
    {
        public LootGained(int gold, string source) { Gold = gold; Source = source; }
        public int Gold { get; }
        public string Source { get; }
        public override string ToString() => "Loot +" + Gold + " (" + Source + ")";
    }

    public sealed class CurioResolved : IDomainEvent
    {
        public CurioResolved(CurioDefinition curio, CurioOutcome outcome) { Curio = curio; Outcome = outcome; }
        public CurioDefinition Curio { get; }
        public CurioOutcome Outcome { get; }
        public override string ToString() => "Curio " + Curio.DisplayName + ": " + Outcome.Description;
    }

    public sealed class ExpeditionPhaseChanged : IDomainEvent
    {
        public ExpeditionPhaseChanged(ExpeditionPhase phase) { Phase = phase; }
        public ExpeditionPhase Phase { get; }
        public override string ToString() => "ExpeditionPhase " + Phase;
    }
}
