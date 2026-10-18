using System;
using System.Collections.Generic;
using Darkest.Domain.Combat.Events;
using Darkest.Domain.Core;
using Darkest.Domain.Dungeon;
using Darkest.Domain.Game;
using Darkest.Domain.Progression;
using Darkest.Unity.Presentation.Views;

namespace Darkest.Unity.Presentation.Audio
{
    /// <summary>
    /// 이벤트 → 효과음(Observer). 전투 중 소리는 연출 비트(BeatStarted)에 맞춰 내고(도메인 이벤트는 연출보다 먼저 몰려 나오므로),
    /// 전투 밖 소리는 도메인 이벤트에 바로 반응한다. 세션마다 새로 만들고 Dispose로 구독을 끊는다.
    /// </summary>
    public sealed class SoundPresenter : IDisposable
    {
        private readonly ISoundPlayer _player;
        private readonly BattleAnimationPlayer _animations;
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

        public SoundPresenter(IEventBus events, BattleAnimationPlayer animations, ISoundPlayer player)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _animations = animations;
            if (_animations != null) _animations.BeatStarted += OnBeat;

            On<BattleStarted>(events, SoundId.BattleStart);
            On<RoomEntered>(events, SoundId.RoomEnter);
            On<LootGained>(events, SoundId.Loot);
            On<TrinketFound>(events, SoundId.Trinket);
            On<CampMade>(events, SoundId.Camp);
            On<CampSkillUsed>(events, SoundId.CampSkill);
            On<CurioResolved>(events, SoundId.Curio);
            On<HeroLeveledUp>(events, SoundId.LevelUp);
            _subscriptions.Add(events.Subscribe<BattleRetreatAttempted>(e => { if (e.Succeeded) _player.Play(SoundId.Fled); }));
            _subscriptions.Add(events.Subscribe<BattleEnded>(e => _player.Play(e.Result == BattleResult.Victory ? SoundId.Victory : SoundId.Defeat)));
        }

        private void On<T>(IEventBus events, SoundId sound) where T : IDomainEvent
        {
            _subscriptions.Add(events.Subscribe<T>(_ => _player.Play(sound)));
        }

        private void OnBeat(BeatKind kind) => _player.Play(SoundFor(kind));

        private static SoundId SoundFor(BeatKind kind)
        {
            switch (kind)
            {
                case BeatKind.Crit: return SoundId.Crit;
                case BeatKind.Heal: return SoundId.Heal;
                case BeatKind.Miss: return SoundId.Miss;
                case BeatKind.Status: return SoundId.Status;
                case BeatKind.Stress: return SoundId.Stress;
                case BeatKind.Death: return SoundId.Death;
                default: return SoundId.Hit;
            }
        }

        public void Dispose()
        {
            if (_animations != null) _animations.BeatStarted -= OnBeat;
            for (int i = 0; i < _subscriptions.Count; i++) _subscriptions[i].Dispose();
            _subscriptions.Clear();
        }
    }
}
