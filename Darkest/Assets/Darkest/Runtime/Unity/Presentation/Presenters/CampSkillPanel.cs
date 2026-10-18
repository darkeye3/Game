using System;
using Darkest.Domain.Combat;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Game;
using Darkest.Unity.Presentation.Ui;
using Darkest.Unity.Presentation.Views;

namespace Darkest.Unity.Presentation.Presenters
{
    /// <summary>
    /// 탐험 화면의 야영 스킬 목록. 아군 1명 대상 스킬은 2단계(스킬 → 대상)로 고른다.
    /// 상태(고른 스킬)는 이 패널만 가지며, 규칙 판정은 모두 도메인(CampSession)이 한다.
    /// </summary>
    internal sealed class CampSkillPanel
    {
        private readonly GameSession _session;
        private readonly DungeonView _view;
        private readonly Action<string> _after;
        private Hero _pendingUser;
        private CampSkillDefinition _pendingSkill;

        /// <param name="after">스킬 사용 결과(에러 또는 null) 표시 + 다시 그리기.</param>
        public CampSkillPanel(GameSession session, DungeonView view, Action<string> after)
        {
            _session = session;
            _view = view;
            _after = after;
        }

        public void Reset()
        {
            _pendingUser = null;
            _pendingSkill = null;
        }

        public void Render(Expedition expedition)
        {
            var camp = expedition.ActiveCamp;
            var list = _view.CampSkills;
            list.Begin();
            if (camp == null)
            {
                Reset();
                _view.SetCampTitle("야영 스킬 (야영 중에만)");
            }
            else if (_pendingSkill != null) RenderTargets(expedition, list);
            else RenderSkills(expedition, camp, list);
            list.End();
        }

        private void RenderSkills(Expedition expedition, CampSession camp, PooledButtonList list)
        {
            _view.SetCampTitle("야영 스킬 — 남은 포인트 " + camp.RespiteRemaining);
            for (int i = 0; i < expedition.Party.Members.Count; i++)
            {
                var hero = (Hero)expedition.Party.Members[i];
                var skills = camp.SkillsOf(hero);
                for (int s = 0; s < skills.Count; s++)
                {
                    var skill = skills[s];
                    bool usable = camp.CanUse(hero, skill) == null;
                    list.Add(hero.DisplayName + ": " + ItemTextFormat.CampSkill(skill), () => OnSkillClicked(hero, skill), usable);
                }
            }
        }

        private void RenderTargets(Expedition expedition, PooledButtonList list)
        {
            _view.SetCampTitle(_pendingSkill.DisplayName + " — 대상을 고르세요");
            for (int i = 0; i < expedition.Party.Members.Count; i++)
            {
                var target = (Hero)expedition.Party.Members[i];
                list.Add("대상: " + target.DisplayName + "  HP " + target.CurrentHp + "/" + target.MaxHp + "  스트레스 " + target.Stress.Current,
                    () => Use(_pendingUser, _pendingSkill, target));
            }
            list.Add("취소", () => { Reset(); _after(null); });
        }

        private void OnSkillClicked(Hero user, CampSkillDefinition skill)
        {
            if (skill.Target != CampSkillTarget.Ally)
            {
                Use(user, skill, user);
                return;
            }
            _pendingUser = user;
            _pendingSkill = skill;
            _after(null);
        }

        private void Use(Hero user, CampSkillDefinition skill, Hero target)
        {
            Reset();
            _after(_session.TryUseCampSkill(user, skill, target));
        }
    }
}
