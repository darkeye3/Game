using System;
using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Events;
using Darkest.Domain.Core;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Game;
using Darkest.Domain.Progression;
using Darkest.Domain.Stress;
using Darkest.Unity.Presentation.Views;

namespace Darkest.Unity.Presentation.Presenters
{
    /// <summary>도메인 이벤트(Observer) → 한국어 로그 한 줄. 연출 큐(애니메이션)도 같은 이벤트를 구독해 붙이면 된다.</summary>
    public sealed class EventLogPresenter : IDisposable
    {
        private readonly EventLogView _view;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public EventLogPresenter(IEventBus events, EventLogView view)
        {
            _view = view;
            On<RoundStarted>(events, e => "── 라운드 " + e.Round + " ──");
            On<SkillUsed>(events, e => e.Actor.DisplayName + " ▶ " + e.Skill.DisplayName + (e.PrimaryTarget != null && e.PrimaryTarget != e.Actor ? " → " + e.PrimaryTarget.DisplayName : ""));
            On<AttackMissed>(events, e => "  빗나감! (" + e.Target.DisplayName + ")");
            On<DamageDealt>(events, Describe);
            On<Healed>(events, e => "  " + e.Target.DisplayName + " 회복 +" + e.Amount);
            On<StatusApplied>(events, e => "  " + e.Target.DisplayName + " " + TextFormat.StatusName(e.Kind));
            On<StatusResisted>(events, e => "  " + e.Target.DisplayName + " 저항 " + TextFormat.StatusName(e.Kind));
            On<TurnSkipped>(events, e => e.Reason == TurnSkipReason.Stunned ? e.Actor.DisplayName + " 기절해서 행동 불가" : e.Actor.DisplayName + " 대기");
            On<CombatantMoved>(events, e => "  " + e.Combatant.DisplayName + " " + e.FromRank + "열 → " + e.ToRank + "열");
            On<CombatantDied>(events, e => "✝ " + e.Combatant.DisplayName + " 사망");
            On<BattleEnded>(events, e => e.Result == BattleResult.Victory ? "★ 전투 승리" : "☠ 전투 패배");
            On<StressChanged>(events, e => e.Delta > 0 ? "  " + e.Hero.DisplayName + " 스트레스 +" + e.Delta : null);
            On<ResolveTriggered>(events, e => "!! " + e.Hero.DisplayName + "의 결의 시험: " + e.Resolve.DisplayName + " (" + TextFormat.ResolveKind(e.Resolve) + ")");
            On<ResolveActed>(events, e => "  " + e.Hero.DisplayName + " [" + e.Resolve.DisplayName + "] 발동");
            On<HeartAttackSuffered>(events, e => "!! " + e.Hero.DisplayName + " 심장마비 → " + (e.Outcome == DamageOutcome.Killed ? "사망" : "죽음의 문턱"));
            On<RoomEntered>(events, e => "방 " + e.Room.Id + " 진입: " + TextFormat.RoomTypeName(e.Room.Type));
            On<HungerChecked>(events, e => e.HadFood ? "식량 소모 (파티 인원수만큼)" : "식량이 모자라다! 파티가 굶주린다");
            On<HeroLeveledUp>(events, e => "▲ " + e.Hero.DisplayName + " 레벨 " + e.Level + " 달성");
            On<BattleRetreatAttempted>(events, e => e.Succeeded ? "» 도주 성공 — 마을로 도망친다" : "후퇴 실패! " + e.Actor.DisplayName + "의 차례가 넘어갔다");
            On<LootGained>(events, e => "골드 +" + e.Gold + " (" + e.Source + ")");
            On<CurioResolved>(events, e => e.Curio.DisplayName + ": " + e.Outcome.Description);
            On<TrinketFound>(events, e => "◆ 장신구 획득 (" + e.Source + "): " + ItemTextFormat.Trinket(e.Trinket));
            On<CampSkillUsed>(events, e => "야영: " + e.User.DisplayName + " ▶ " + e.Skill.DisplayName + (e.Target != null && e.Target != e.User ? " → " + e.Target.DisplayName : ""));
            On<CampMade>(events, e => "야영: 파티 HP +" + e.HealedTotal + ", 스트레스 -" + e.StressRelief + " (장작불이 꺼지며 어두워졌다). 야영 스킬을 쓸 수 있다");
        }

        private void On<T>(IEventBus events, Func<T, string> format) where T : IDomainEvent
        {
            _subscriptions.Add(events.Subscribe<T>(e => _view.Append(format(e))));
        }

        private static string Describe(DamageDealt e)
        {
            string source = e.Source == null ? "  지속 피해: " : "  ";
            string crit = e.IsCrit ? " 치명타!" : "";
            string outcome = e.Outcome == DamageOutcome.EnteredDeathsDoor ? " → 죽음의 문턱!"
                : e.Outcome == DamageOutcome.SurvivedDeathblow ? " → 버텨냈다!" : "";
            return source + e.Target.DisplayName + " -" + e.Amount + crit + outcome;
        }

        public void Dispose()
        {
            for (int i = 0; i < _subscriptions.Count; i++) _subscriptions[i].Dispose();
            _subscriptions.Clear();
        }
    }
}
