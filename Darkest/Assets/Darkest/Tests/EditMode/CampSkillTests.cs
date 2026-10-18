using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Stats;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Game;
using NUnit.Framework;

namespace Darkest.Tests
{
    public class CampSkillTests
    {
        /// <summary>입구(처음부터 정리된 방)에서 바로 야영한 세션.</summary>
        private static GameSession Camped(int seed)
        {
            var session = SessionFixtures.New(seed);
            Assert.That(session.TryEmbark(session.Town.Roster, new Provisions(10, 0, 1)), Is.Null);
            Assert.That(session.TryCamp(), Is.Null);
            return session;
        }

        private static CampSkillDefinition FirstSkill(GameSession session, Hero hero) =>
            session.Expedition.ActiveCamp.SkillsOf(hero)[0];

        [Test]
        public void Camp_StartsSessionWithRespitePoints()
        {
            var session = Camped(1);
            Assert.That(session.Expedition.ActiveCamp, Is.Not.Null);
            Assert.That(session.Expedition.ActiveCamp.RespiteRemaining, Is.EqualTo(new GameConfig().Expedition.CampRespitePoints));
            Assert.That(session.TryCamp(), Is.Not.Null, "같은 야영에서 다시 야영할 수 없다");
        }

        [Test]
        public void UseSkill_SpendsPoints_AndOnlyOncePerHero()
        {
            var session = Camped(2);
            var hero = (Hero)session.Expedition.Party.Members[0];
            var skill = FirstSkill(session, hero);
            int points = session.Expedition.ActiveCamp.RespiteRemaining;

            Assert.That(session.TryUseCampSkill(hero, skill, hero), Is.Null);
            Assert.That(session.Expedition.ActiveCamp.RespiteRemaining, Is.EqualTo(points - skill.Cost));
            Assert.That(session.TryUseCampSkill(hero, skill, hero), Is.Not.Null);
        }

        [Test]
        public void OtherClassSkill_IsRejected()
        {
            var session = Camped(3);
            var hero = (Hero)session.Expedition.Party.Members[0];
            CampSkillDefinition foreign = null;
            foreach (var heroClass in session.Catalog.HeroClasses)
            {
                if (heroClass.Id != hero.ClassId) foreign = session.Catalog.CampSkillsFor(heroClass.Id)[0];
            }
            Assert.That(foreign, Is.Not.Null);
            Assert.That(session.TryUseCampSkill(hero, foreign, hero), Is.Not.Null);
        }

        [Test]
        public void Buff_LastsUntilReturn_AndLeavingCampEndsSession()
        {
            var session = Camped(4);
            // 수녀의 '축복'(아군 1명, 회피 +8)으로 버프 수명을 검증한다. seed 4 파티에는 수녀가 있다.
            Hero sister = null;
            foreach (var member in session.Expedition.Party.Members)
            {
                if (((Hero)member).ClassId == "sister") sister = (Hero)member;
            }
            if (sister == null) Assert.Ignore("seed 4 파티에 수녀가 없음");

            var target = (Hero)session.Expedition.Party.Members[0];
            int dodge = target.GetStat(StatType.Dodge);
            Assert.That(session.TryUseCampSkill(sister, session.Catalog.FindCampSkill("camp_sister_bless"), target), Is.Null);
            Assert.That(target.GetStat(StatType.Dodge), Is.EqualTo(dodge + 8));

            session.Retreat();
            session.ReturnToTown();
            Assert.That(target.GetStat(StatType.Dodge), Is.EqualTo(dodge), "야영 버프는 귀환하면 사라진다");
        }

        [Test]
        public void MovingAway_EndsCamp()
        {
            var session = Camped(5);
            var room = session.Expedition.CurrentRoom;
            session.TryMoveTo(room.Neighbors[0]);
            Assert.That(session.Expedition.ActiveCamp, Is.Null);
        }
    }
}
