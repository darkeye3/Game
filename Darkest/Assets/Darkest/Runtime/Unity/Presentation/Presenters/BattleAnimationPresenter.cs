using System;
using System.Collections.Generic;
using Darkest.Domain.Combat;
using Darkest.Domain.Combat.Events;
using Darkest.Domain.Core;
using Darkest.Domain.Stress;
using Darkest.Unity.Presentation.Views;
using UnityEngine;

namespace Darkest.Unity.Presentation.Presenters
{
    /// <summary>
    /// 전투 도메인 이벤트 → 연출 비트 변환(Observer). 전투 시작(BattleStarted)부터 BattlePresenter.Exit까지만 받는다
    /// (던전 복도에서 생기는 스트레스 이벤트가 연출 큐에 쌓이지 않도록).
    /// </summary>
    public sealed class BattleAnimationPresenter : IDisposable
    {
        private static readonly Color HitColor = new Color(1f, 0.9f, 0.85f);
        private static readonly Color CritColor = new Color(1f, 0.35f, 0.2f);
        private static readonly Color HealColor = new Color(0.45f, 1f, 0.5f);
        private static readonly Color MissColor = new Color(0.7f, 0.7f, 0.7f);
        private static readonly Color StatusColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color StressColor = new Color(0.75f, 0.5f, 1f);

        private readonly BattleAnimationPlayer _player;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private bool _listening;

        public BattleAnimationPresenter(IEventBus events, BattleAnimationPlayer player)
        {
            _player = player;
            _subscriptions.Add(events.Subscribe<BattleStarted>(_ => _listening = true));
            On<DamageDealt>(events, e => _player.Enqueue(e.Target.Id, DamageText(e), e.IsCrit ? CritColor : HitColor, e.IsCrit ? BeatKind.Crit : BeatKind.Hit));
            On<AttackMissed>(events, e => _player.Enqueue(e.Target.Id, "빗나감", MissColor, BeatKind.Miss));
            On<Healed>(events, e => _player.Enqueue(e.Target.Id, "+" + e.Amount, HealColor, BeatKind.Heal));
            On<StatusApplied>(events, e => _player.Enqueue(e.Target.Id, TextFormat.StatusName(e.Kind), StatusColor, BeatKind.Status));
            On<StressChanged>(events, e => { if (e.Delta > 0) _player.Enqueue(e.Hero.Id, "스트레스 +" + e.Delta, StressColor, BeatKind.Stress); });
            On<ResolveTriggered>(events, e => _player.Enqueue(e.Hero.Id, e.Resolve.DisplayName + "!", StressColor, BeatKind.Stress));
            On<CombatantDied>(events, e => _player.Enqueue(e.Combatant.Id, "✝", CritColor, BeatKind.Death));
        }

        public bool IsPlaying => _player.IsPlaying;

        public void Play(Action onFinished) => _player.Play(onFinished);

        /// <summary>전투 화면을 떠날 때: 수신 중지 + 남은 연출 폐기.</summary>
        public void Stop()
        {
            _listening = false;
            _player.Clear();
        }

        private void On<T>(IEventBus events, Action<T> handler) where T : IDomainEvent
        {
            _subscriptions.Add(events.Subscribe<T>(e => { if (_listening) handler(e); }));
        }

        private static string DamageText(DamageDealt e)
        {
            string text = "-" + e.Amount;
            if (e.IsCrit) text += "!";
            if (e.Outcome == DamageOutcome.EnteredDeathsDoor) text += "\n죽음의 문턱";
            return text;
        }

        public void Dispose()
        {
            Stop();
            for (int i = 0; i < _subscriptions.Count; i++) _subscriptions[i].Dispose();
            _subscriptions.Clear();
        }
    }
}
