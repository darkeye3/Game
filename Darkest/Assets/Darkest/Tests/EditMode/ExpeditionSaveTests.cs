using Darkest.Domain.Combat;
using Darkest.Domain.Content;
using Darkest.Domain.Core;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Game;
using Darkest.Domain.Save;
using NUnit.Framework;

namespace Darkest.Tests
{
    /// <summary>원정 중 저장 → 문자열 → 불러오기 왕복.</summary>
    public class ExpeditionSaveTests
    {
        private static GameSession NewSession(int seed) =>
            GameSession.CreateNew(new GameConfig(), DefaultContent.Create(), new SeededRandom(seed), new EventBus(), NullLogger.Instance);

        private static GameSession LoadFrom(GameSaveData data, int seed)
        {
            var serializer = new LineSaveSerializer();
            var text = serializer.Serialize(data);
            return GameSession.Load(new GameConfig(), DefaultContent.Create(), new SeededRandom(seed), new EventBus(),
                NullLogger.Instance, serializer.Deserialize(text));
        }

        [Test]
        public void MidExpeditionSave_RestoresMapPartyInventoryAndProgress()
        {
            var session = NewSession(3);
            Assert.That(session.TryEmbark(session.Town.Roster, new Provisions(10, 4, 1)), Is.Null);
            var expedition = session.Expedition;
            expedition.Party.Members[0].ApplyDamage(5, new ScriptedRandom());
            Assert.That(session.CanSave, Is.True);

            var loaded = LoadFrom(session.CaptureSave(), 99);

            Assert.That(loaded.Mode, Is.EqualTo(GameMode.Exploring));
            var restored = loaded.Expedition;
            Assert.That(restored.CurrentRoomId, Is.EqualTo(expedition.CurrentRoomId));
            Assert.That(restored.Light.Value, Is.EqualTo(expedition.Light.Value));
            Assert.That(restored.Inventory.Food, Is.EqualTo(10));
            Assert.That(restored.Inventory.Torches, Is.EqualTo(4));
            Assert.That(restored.Inventory.Firewood, Is.EqualTo(1));
            Assert.That(restored.Map.Seed, Is.EqualTo(expedition.Map.Seed));
            Assert.That(restored.Map.Rooms.Count, Is.EqualTo(expedition.Map.Rooms.Count));
            for (int i = 0; i < expedition.Map.Rooms.Count; i++)
            {
                var a = expedition.Map.Rooms[i];
                var b = restored.Map.Get(a.Id);
                Assert.That(b.Type, Is.EqualTo(a.Type));
                Assert.That(b.EncounterId, Is.EqualTo(a.EncounterId));
                Assert.That(b.IsCleared, Is.EqualTo(a.IsCleared));
                Assert.That(b.Neighbors, Is.EquivalentTo(a.Neighbors));
            }
            Assert.That(restored.Party.Members.Count, Is.EqualTo(expedition.Party.Members.Count));
            Assert.That(restored.Party.Members[0].Id, Is.EqualTo(expedition.Party.Members[0].Id));
            Assert.That(restored.Party.Members[0].CurrentHp, Is.EqualTo(expedition.Party.Members[0].CurrentHp));
            Assert.That(restored.Party.Members[0], Is.SameAs(loaded.Town.FindHero(expedition.Party.Members[0].Id)),
                "파티 영웅은 명부의 같은 객체여야 귀환 정산이 맞는다");
        }

        [Test]
        public void MidExpeditionSave_KeepsFallenAndDeathsDoor()
        {
            var session = NewSession(4);
            Assert.That(session.TryEmbark(session.Town.Roster, new Provisions(10, 4, 1)), Is.Null);
            var party = session.Expedition.Party;
            var doomed = (Hero)party.Members[0];
            var wounded = (Hero)party.Members[1];
            var random = new ScriptedRandom(TestKit.AlwaysFail);
            doomed.ApplyDamage(doomed.MaxHp, random);
            doomed.ApplyDamage(1, random); // 문턱에서 한 번 더 → 사망
            wounded.ApplyDamage(wounded.MaxHp, random);
            party.RemoveDead(new System.Collections.Generic.List<Combatant>());

            var loaded = LoadFrom(session.CaptureSave(), 5);

            Assert.That(loaded.Expedition.Fallen.Count, Is.EqualTo(1));
            Assert.That(loaded.Expedition.Fallen[0].Id, Is.EqualTo(doomed.Id));
            Assert.That(((Hero)loaded.Town.FindHero(wounded.Id)).IsAtDeathsDoor, Is.True);
            Assert.That(loaded.Expedition.Party.Members.Count, Is.EqualTo(party.Members.Count));

            loaded.Retreat();
            int rosterBefore = loaded.Town.Roster.Count;
            loaded.ReturnToTown();
            Assert.That(loaded.Town.Roster.Count, Is.EqualTo(rosterBefore - 1), "사망자는 귀환 시 명부에서 빠진다");
        }

        [Test]
        public void CannotSaveDuringBattleOrResult()
        {
            var session = NewSession(6);
            Assert.That(session.TryEmbark(session.Town.Roster, new Provisions(10, 4, 1)), Is.Null);
            session.Retreat();
            Assert.That(session.Mode, Is.EqualTo(GameMode.ExpeditionResult));
            Assert.That(session.CanSave, Is.False);
            Assert.Throws<System.InvalidOperationException>(() => session.CaptureSave());
        }

        [Test]
        public void Version1Save_StillLoads()
        {
            var data = new LineSaveSerializer().Deserialize("darkest-save|1\ntown|500|3|10\nhero|h1|warden|아리아|20|5|\n");
            Assert.That(data.Expedition, Is.Null);
            Assert.That(data.Roster[0].IsDead, Is.False);
        }

        [Test]
        public void ExpeditionDetailBeforeHeader_IsRejected()
        {
            Assert.Throws<System.FormatException>(() =>
                new LineSaveSerializer().Deserialize("darkest-save|2\ntown|1|1|1\nroom|0|Entrance||0|1|1\n"));
        }
    }
}
