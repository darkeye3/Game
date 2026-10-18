using Darkest.Domain.Combat.Events;
using Darkest.Domain.Content;
using Darkest.Domain.Core;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Game;
using NUnit.Framework;

namespace Darkest.Tests
{
    public class BattleRetreatTests
    {
        /// <summary>첫 전투가 시작될 때까지 이동한다. 실패하면 null.</summary>
        private static GameSession SessionInBattle(GameConfig config, int seed)
        {
            var session = GameSession.CreateNew(config, DefaultContent.Create(), new SeededRandom(seed), new EventBus(), NullLogger.Instance);
            Assert.That(session.TryEmbark(session.Town.Roster, new Provisions(10, 6, 1)), Is.Null);
            var random = new SeededRandom(seed);
            for (int guard = 0; guard < 50 && session.Mode == GameMode.Exploring; guard++)
            {
                var room = session.Expedition.CurrentRoom;
                if (room.Type == RoomType.Curio && !room.IsCleared) { session.TryInteractCurio(); continue; }
                session.TryMoveTo(room.Neighbors[random.Range(0, room.Neighbors.Count)]);
            }
            return session.Mode == GameMode.Battle && !session.IsBattleConcluded ? session : null;
        }

        [Test]
        public void SuccessfulRetreat_EndsExpeditionAsRetreated_AndAddsStress()
        {
            var config = new GameConfig();
            config.Combat.RetreatChancePercent = 100;
            config.Combat.StressOnRetreat = 10;
            var session = SessionInBattle(config, 2);
            Assume.That(session, Is.Not.Null, "이 seed에서 전투에 들어가지 못했다");
            var hero = session.Expedition.Party.Members[0] as Domain.Combat.Hero;
            int stressBefore = hero.Stress.Current;

            Assert.That(session.TryRetreatFromBattle(), Is.Null);

            Assert.That(session.IsBattleConcluded, Is.True);
            Assert.That(session.LastBattleSummary.Result, Is.EqualTo(BattleResult.Fled));
            Assert.That(hero.Stress.Current, Is.GreaterThanOrEqualTo(stressBefore + 10));
            Assert.That(session.ContinueAfterBattle(), Is.Null);
            Assert.That(session.Mode, Is.EqualTo(GameMode.ExpeditionResult));
            Assert.That(session.Expedition.Phase, Is.EqualTo(ExpeditionPhase.Retreated));
        }

        [Test]
        public void FailedRetreat_SpendsTheTurn_AndBattleContinues()
        {
            var config = new GameConfig();
            config.Combat.RetreatChancePercent = 0;
            var session = SessionInBattle(config, 2);
            Assume.That(session, Is.Not.Null);
            int historyBefore = session.Battle.Context.History.Count;

            Assert.That(session.TryRetreatFromBattle(), Is.Null);

            Assert.That(session.Battle.Context.History.Count, Is.GreaterThan(historyBefore), "실패하면 대기 명령으로 차례가 넘어간다");
            Assert.That(session.Expedition.Phase, Is.Not.EqualTo(ExpeditionPhase.Retreated));
        }
    }
}
