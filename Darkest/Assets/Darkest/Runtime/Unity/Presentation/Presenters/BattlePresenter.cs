using System;
using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.AI;
using Darkest.Domain.Combat.Commands;
using Darkest.Domain.Combat.Flow;
using Darkest.Domain.Combat.Skills;
using Darkest.Domain.Game;
using Darkest.Unity.Presentation.Views;

namespace Darkest.Unity.Presentation.Presenters
{
    /// <summary>
    /// 전투 입력 흐름: 스킬 선택 → 대상 선택 → UseSkillCommand 제출.
    /// 규칙 판정(사용 가능 열/유효 대상/명중률)은 도메인 SkillTargeting/HitResolver를 그대로 써서 UI와 로직이 어긋나지 않게 한다.
    /// </summary>
    public sealed class BattlePresenter : IScreenPresenter
    {
        private readonly GameSession _session;
        private readonly BattleView _view;
        private readonly BattleStageView _stage;
        private readonly BattleAnimationPresenter _animations;
        private readonly Action _renderAction;
        private readonly IEnemyAI _autoPilot = new RandomEnemyAI();
        private readonly List<Combatant> _targets = new List<Combatant>(Formation.MaxRanks);
        private readonly List<Combatant> _scratch = new List<Combatant>(Formation.MaxRanks);
        private SkillDefinition _selectedSkill;

        public BattlePresenter(GameSession session, BattleView view, BattleStageView stage, BattleAnimationPresenter animations)
        {
            _session = session;
            _view = view;
            _stage = stage;
            _animations = animations;
            _renderAction = Render; // 콜백 델리게이트를 매번 새로 만들지 않도록 캐시
            _view.ForwardClicked += OnForward;
            _view.BackwardClicked += OnBackward;
            _view.PassClicked += OnPass;
            _view.AutoClicked += OnAuto;
        }

        public GameMode Mode => GameMode.Battle;

        public void Enter()
        {
            ClearSelection();
            _view.SetMessage(string.Empty);
            Render();
            _animations.Play(_renderAction); // 화면 진입 전 몬스터 선공 등 쌓인 연출 재생
        }

        public void Exit()
        {
            _animations.Stop();
            _stage.Clear();
        }

        private BattleContext Context => _session.Battle?.Context;

        private void Render()
        {
            var battle = _session.Battle;
            if (battle == null) return;
            var context = battle.Context;
            var actor = battle.CurrentActor;

            _view.SetHeader("라운드 " + context.Round + "  —  " + (actor != null ? actor.DisplayName + "의 차례" : ""));
            _view.SetHeroes(TextFormat.Formation(context.Heroes));
            _view.SetMonsters(TextFormat.Formation(context.Monsters));
            RenderSkills(battle, actor);
            RenderTargets(context, actor);
            _stage.Render(context, _targets);
        }

        private void RenderSkills(BattleSession battle, Combatant actor)
        {
            _view.Skills.Begin();
            if (battle.IsAwaitingHeroCommand && actor != null)
            {
                foreach (var skill in actor.Skills)
                {
                    bool usable = SkillTargeting.CanUseFromCurrentRank(battle.Context, actor, skill);
                    if (usable)
                    {
                        SkillTargeting.CollectValidTargets(battle.Context, actor, skill, _scratch);
                        usable = _scratch.Count > 0;
                    }
                    var captured = skill;
                    _view.Skills.Add(skill.DisplayName, () => OnSkillSelected(captured), usable, skill == _selectedSkill);
                }
            }
            _view.Skills.End();
        }

        private void RenderTargets(BattleContext context, Combatant actor)
        {
            _view.Targets.Begin();
            for (int i = 0; i < _targets.Count; i++)
            {
                var target = _targets[i];
                int rank = context.FormationOf(target).RankOf(target);
                int hit = context.HitResolver.GetHitChance(actor, target, _selectedSkill);
                _view.Targets.Add(rank + "열 " + target.DisplayName + "  (명중 " + hit + "%)", () => Submit(new UseSkillCommand(actor, _selectedSkill, target)));
            }
            _view.Targets.End();
        }

        private void OnSkillSelected(SkillDefinition skill)
        {
            if (_animations.IsPlaying) return;
            var actor = _session.Battle.CurrentActor;
            _selectedSkill = skill;
            SkillTargeting.CollectValidTargets(Context, actor, skill, _targets);

            // 광역/자신 대상은 대상 선택 단계를 건너뛴다.
            if ((skill.HitsAllTargets || skill.Side == TargetSide.Self) && _targets.Count > 0)
            {
                Submit(new UseSkillCommand(actor, skill, _targets[0]));
                return;
            }
            Render();
        }

        private void OnForward() => SubmitIfHeroTurn(actor => new MoveCommand(actor, -1));
        private void OnBackward() => SubmitIfHeroTurn(actor => new MoveCommand(actor, 1));
        private void OnPass() => SubmitIfHeroTurn(actor => new PassCommand(actor));
        private void OnAuto() => SubmitIfHeroTurn(actor => _autoPilot.Decide(Context, actor));

        private void SubmitIfHeroTurn(Func<Combatant, IBattleCommand> build)
        {
            var battle = _session.Battle;
            if (battle == null || !battle.IsAwaitingHeroCommand || _animations.IsPlaying) return;
            Submit(build(battle.CurrentActor));
        }

        private void Submit(IBattleCommand command)
        {
            if (_animations.IsPlaying) return; // 연출 중 입력 무시
            string error = _session.SubmitBattleCommand(command);
            if (_session.Mode != GameMode.Battle) return; // 전투 종료 → 라우터가 화면 전환
            ClearSelection();
            _view.SetMessage(error ?? string.Empty);
            if (error != null) { Render(); return; }

            // 연출이 끝난 뒤에 화면을 갱신해야 쓰러진 유닛/이동이 연출 후에 반영된다.
            _view.Skills.Begin(); _view.Skills.End();
            _view.Targets.Begin(); _view.Targets.End();
            _animations.Play(_renderAction);
        }

        private void ClearSelection()
        {
            _selectedSkill = null;
            _targets.Clear();
        }

        public void Dispose()
        {
            _view.ForwardClicked -= OnForward;
            _view.BackwardClicked -= OnBackward;
            _view.PassClicked -= OnPass;
            _view.AutoClicked -= OnAuto;
        }
    }
}
