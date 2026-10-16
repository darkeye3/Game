using System.Collections.Generic;
using System.Text;
using Darkest.Domain.Combat;
using Darkest.Domain.Content;
using Darkest.Domain.Core;
using Darkest.Domain.Dungeon;
using NUnit.Framework;

namespace Darkest.Tests
{
    public class DungeonTests
    {
        private static string Describe(DungeonMap map)
        {
            var sb = new StringBuilder();
            foreach (var room in map.Rooms)
            {
                sb.Append(room.Id).Append(room.Type).Append(room.EncounterId).Append(':');
                foreach (var n in room.Neighbors) sb.Append(n).Append(',');
                sb.Append(';');
            }
            return sb.ToString();
        }

        [Test]
        public void Generator_SameSeedSameMap()
        {
            var generator = new DungeonGenerator(DefaultContent.Create(), new DungeonGenerationRules());
            Assert.That(Describe(generator.Generate(new SeededRandom(7))), Is.EqualTo(Describe(generator.Generate(new SeededRandom(7)))));
        }

        [Test]
        public void Generator_BossReachableFromEntrance([Range(1, 20)] int seed)
        {
            var rules = new DungeonGenerationRules();
            var map = new DungeonGenerator(DefaultContent.Create(), rules).Generate(new SeededRandom(seed));

            Assert.That(map.Rooms.Count, Is.EqualTo(rules.MainPathRooms + rules.BranchRooms + 2));
            Assert.That(map.Get(map.BossRoomId).Type, Is.EqualTo(RoomType.Boss));

            var visited = new HashSet<int> { map.EntranceId };
            var frontier = new Queue<int>(visited);
            while (frontier.Count > 0)
            {
                foreach (var n in map.Get(frontier.Dequeue()).Neighbors)
                    if (visited.Add(n)) frontier.Enqueue(n);
            }
            Assert.That(visited.Count, Is.EqualTo(map.Rooms.Count), "모든 방이 연결되어야 한다");
        }

        [Test]
        public void TorchLight_LevelsAndClamp()
        {
            var rules = new ExpeditionRules();
            var light = new TorchLight(rules);
            Assert.That(light.Level, Is.EqualTo(LightLevel.Radiant));
            light.Change(-rules.MaxLight * 2);
            Assert.That(light.Value, Is.EqualTo(0));
            Assert.That(light.Level, Is.EqualTo(LightLevel.Dark));
            Assert.That(light.StressPerStep(), Is.EqualTo(rules.StressPerStepDark));
        }

        [Test]
        public void Curio_RollRespectsWeights()
        {
            var curio = new CurioDefinition("c", "c", new[]
            {
                new CurioOutcome(1, CurioEffectKind.Gold, 1, "a"),
                new CurioOutcome(3, CurioEffectKind.Gold, 2, "b")
            });
            Assert.That(curio.Roll(new ScriptedRandom(0)).Description, Is.EqualTo("a"));
            Assert.That(curio.Roll(new ScriptedRandom(1)).Description, Is.EqualTo("b"));
            Assert.That(curio.Roll(new ScriptedRandom(3)).Description, Is.EqualTo("b"));
        }

        private static Expedition CreateExpedition(DungeonMap map, ExpeditionRules rules, Inventory inventory, IRandom random, IEventBus events)
        {
            var catalog = DefaultContent.Create();
            var party = TestKit.Party(TestKit.Hero("a"), TestKit.Hero("b"));
            return new Expedition(party, map, inventory, rules, TestKit.StressService(random, events), catalog, random, events, NullLogger.Instance);
        }

        private static DungeonMap TwoRoomMap(RoomType secondType, string encounterId = null, int gold = 0)
        {
            var entrance = new Room(0, RoomType.Entrance, null, 0);
            var second = new Room(1, secondType, encounterId, gold);
            var boss = new Room(2, RoomType.Boss, "grp_boss_ossuary", 0);
            return DungeonMap.FromLayout(new List<Room> { entrance, second, boss }, new[] { (0, 1), (1, 2) }, 0, 2);
        }

        [Test]
        public void Move_ConsumesLight_AndHungerEatsFood()
        {
            var rules = new ExpeditionRules { CorridorSteps = 4, HungerStepInterval = 4, LightDecayPerStep = 5 };
            var events = new EventBus();
            var hunger = TestKit.Collect<HungerChecked>(events);
            var inventory = new Inventory(food: 1, torches: 0);
            var expedition = CreateExpedition(TwoRoomMap(RoomType.Empty), rules, inventory, new ScriptedRandom(), events);

            Assert.That(expedition.TryMoveTo(1), Is.Null);

            Assert.That(expedition.CurrentRoomId, Is.EqualTo(1));
            Assert.That(expedition.Light.Value, Is.EqualTo(rules.MaxLight - 20));
            Assert.That(hunger.Count, Is.EqualTo(1));
            Assert.That(hunger[0].HadFood, Is.True);
            Assert.That(inventory.Food, Is.EqualTo(0));
        }

        [Test]
        public void Move_RejectsNonAdjacentRoom()
        {
            var expedition = CreateExpedition(TwoRoomMap(RoomType.Empty), new ExpeditionRules(), new Inventory(0, 0), new ScriptedRandom(), new EventBus());
            Assert.That(expedition.TryMoveTo(2), Is.Not.Null);
        }

        [Test]
        public void EnteringBattleRoom_SetsPendingEncounter_AndVictoryClears()
        {
            var expedition = CreateExpedition(TwoRoomMap(RoomType.Battle, "grp_bones", 120), new ExpeditionRules(), new Inventory(5, 0), new ScriptedRandom(), new EventBus());

            expedition.TryMoveTo(1);
            Assert.That(expedition.Phase, Is.EqualTo(ExpeditionPhase.InBattle));
            Assert.That(expedition.PendingEncounter.Id, Is.EqualTo("grp_bones"));
            Assert.That(expedition.TryMoveTo(2), Is.Not.Null, "전투 중엔 이동 불가");

            expedition.ResolveBattle(Domain.Combat.Events.BattleResult.Victory);
            Assert.That(expedition.Phase, Is.EqualTo(ExpeditionPhase.Exploring));
            Assert.That(expedition.CurrentRoom.IsCleared, Is.True);
            Assert.That(expedition.Inventory.Gold, Is.EqualTo(120));
        }

        [Test]
        public void TreasureRoom_GivesGoldOnEntry()
        {
            var expedition = CreateExpedition(TwoRoomMap(RoomType.Treasure, null, 300), new ExpeditionRules(), new Inventory(5, 0), new ScriptedRandom(), new EventBus());
            expedition.TryMoveTo(1);
            Assert.That(expedition.Inventory.Gold, Is.EqualTo(300));
        }

        [Test]
        public void Torch_RestoresLight_ConsumesTorch()
        {
            var rules = new ExpeditionRules();
            var inventory = new Inventory(5, 1);
            var expedition = CreateExpedition(TwoRoomMap(RoomType.Empty), rules, inventory, new ScriptedRandom(), new EventBus());
            Assert.That(expedition.TryUseTorch(), Is.Not.Null, "최대 밝기에서는 사용 불가");

            expedition.TryMoveTo(1);
            Assert.That(expedition.TryUseTorch(), Is.Null);
            Assert.That(inventory.Torches, Is.EqualTo(0));
            Assert.That(expedition.TryUseTorch(), Is.Not.Null);
        }
    }
}
