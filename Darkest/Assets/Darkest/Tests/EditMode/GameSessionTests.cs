using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.AI;
using Darkest.Domain.Content;
using Darkest.Domain.Core;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Game;
using Darkest.Domain.Save;
using NUnit.Framework;

namespace Darkest.Tests
{
    /// <summary>마을 → 탐험 → 전투 → 귀환 전체 흐름 통합 테스트(자동 플레이 봇).</summary>
    public class GameSessionTests
    {
        private static GameSession NewSession(int seed) =>
            GameSession.CreateNew(new GameConfig(), DefaultContent.Create(), new SeededRandom(seed), new EventBus(), NullLogger.Instance);

        [Test]
        public void Embark_ValidatesParty()
        {
            var session = NewSession(1);
            Assert.That(session.TryEmbark(new List<Hero>(), new Provisions(4, 4)), Is.Not.Null);
            var outsider = TestKit.Hero("outsider");
            Assert.That(session.TryEmbark(new[] { outsider }, new Provisions(4, 4)), Is.Not.Null);
            var first = session.Town.Roster[0];
            Assert.That(session.TryEmbark(new[] { first, first }, new Provisions(4, 4)), Is.Not.Null);
            Assert.That(session.Mode, Is.EqualTo(GameMode.Town));
        }

        [Test]
        public void FullExpedition_AutoPlay_ReturnsToTown([Range(1, 30)] int seed)
        {
            var session = NewSession(seed);
            int rosterBefore = session.Town.Roster.Count;
            Assert.That(session.TryEmbark(session.Town.Roster, new Provisions(10, 4, 1)), Is.Null);

            AutoPlay(session, seed);

            Assert.That(session.Mode, Is.EqualTo(GameMode.ExpeditionResult));
            var phase = session.Expedition.Phase;
            int fallen = session.Expedition.Fallen.Count;
            session.ReturnToTown();

            Assert.That(session.Mode, Is.EqualTo(GameMode.Town));
            Assert.That(session.Town.Week, Is.EqualTo(2));
            Assert.That(session.Town.Roster.Count, Is.EqualTo(rosterBefore - fallen));
            Assert.That(phase, Is.Not.EqualTo(ExpeditionPhase.Exploring));
        }

        [Test]
        public void SaveAndLoad_PreservesTown()
        {
            var session = NewSession(5);
            var serializer = new LineSaveSerializer();
            var text = serializer.Serialize(session.CaptureSave());

            var loaded = GameSession.Load(new GameConfig(), DefaultContent.Create(), new SeededRandom(6), new EventBus(),
                NullLogger.Instance, serializer.Deserialize(text));

            Assert.That(loaded.Town.Gold, Is.EqualTo(session.Town.Gold));
            Assert.That(loaded.Town.Roster.Count, Is.EqualTo(session.Town.Roster.Count));
            Assert.That(loaded.Town.Roster[0].Id, Is.EqualTo(session.Town.Roster[0].Id));
        }

        /// <summary>
        /// 단순 봇: 전투는 무작위 AI로, 탐험은 미정리 방 우선 + 보스방으로 전진. 막히면 후퇴.
        /// </summary>
        private static void AutoPlay(GameSession session, int seed)
        {
            var pilot = new RandomEnemyAI();
            var random = new SeededRandom(seed * 31);
            int guard = 0;

            while (session.Mode != GameMode.ExpeditionResult && guard++ < 5000)
            {
                if (session.IsBattleConcluded)
                {
                    // 끝난 전투는 확인 전까지 전투 모드에 머물고, 더 이상 명령을 받지 않는다.
                    Assert.That(session.Battle.IsOver, Is.True);
                    Assert.That(session.SubmitBattleCommand(new Domain.Combat.Commands.PassCommand(session.Battle.Context.Heroes.Members.Count > 0 ? session.Battle.Context.Heroes.Members[0] : session.Expedition.Embarked[0])), Is.Not.Null);
                    Assert.That(session.ContinueAfterBattle(), Is.Null);
                    Assert.That(session.Mode, Is.Not.EqualTo(GameMode.Battle));
                    Assert.That(session.LastBattleSummary, Is.Null);
                    continue;
                }
                if (session.Mode == GameMode.Battle)
                {
                    var battle = session.Battle;
                    Assert.That(session.SubmitBattleCommand(pilot.Decide(battle.Context, battle.CurrentActor)), Is.Null);
                    continue;
                }

                var expedition = session.Expedition;
                if (expedition.CurrentRoom.Type == RoomType.Curio && !expedition.CurrentRoom.IsCleared)
                {
                    Assert.That(session.TryInteractCurio(), Is.Null);
                    continue;
                }
                if (expedition.Light.Level >= LightLevel.Shadowy) session.TryUseTorch();
                if (expedition.Inventory.Firewood > 0 && expedition.CurrentRoom.IsCleared && expedition.Party.Members[0].CurrentHp * 2 < expedition.Party.Members[0].MaxHp)
                    Assert.That(session.TryCamp(), Is.Null.Or.EqualTo("식량이 부족합니다. (필요 " + expedition.CampFoodCost + ")"));

                var neighbors = expedition.CurrentRoom.Neighbors;
                int next = neighbors[random.Range(0, neighbors.Count)];
                if (guard > 200) { session.Retreat(); continue; }
                Assert.That(session.TryMoveTo(next), Is.Null);
            }
            Assert.That(guard, Is.LessThan(5000), "자동 플레이가 끝나지 않았다");
        }
    }
}
