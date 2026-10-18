using System;
using Darkest.Domain.Core;
using UnityEngine;

namespace Darkest.Unity.Presentation.Audio
{
    /// <summary>
    /// AudioSource.PlayOneShot으로 재생하는 ISoundPlayer. 같은 소리가 한 프레임에 몰려(전체 공격 등) 귀가 아프지 않게
    /// 소리마다 최소 재생 간격을 둔다. 음소거/볼륨은 생성 시 주입한다(인스펙터 → GameBootstrap).
    /// </summary>
    public sealed class AudioSourceSoundPlayer : ISoundPlayer
    {
        private const string LogCategory = "Sound";
        private const float MinRepeatSeconds = 0.05f;

        private readonly AudioSource _source;
        private readonly ISoundLibrary _library;
        private readonly IGameLogger _logger;
        private readonly float[] _lastPlayed = new float[Enum.GetValues(typeof(SoundId)).Length];
        private readonly float _volume;
        private readonly bool _muted;

        public AudioSourceSoundPlayer(AudioSource source, ISoundLibrary library, float volume, bool muted, IGameLogger logger)
        {
            _source = source ? source : throw new ArgumentNullException(nameof(source));
            _library = library ?? throw new ArgumentNullException(nameof(library));
            _logger = logger ?? NullLogger.Instance;
            _volume = Mathf.Clamp01(volume);
            _muted = muted;
            for (int i = 0; i < _lastPlayed.Length; i++) _lastPlayed[i] = float.NegativeInfinity;
            _source.playOnAwake = false;
        }

        public void Play(SoundId id)
        {
            if (_muted) return;
            int index = (int)id;
            float now = Time.unscaledTime;
            if (now - _lastPlayed[index] < MinRepeatSeconds) return;
            _lastPlayed[index] = now;

            var clip = _library.Get(id);
            if (clip == null)
            {
                _logger.Warn(LogCategory, id + " 효과음을 만들 수 없습니다.");
                return;
            }
            _source.PlayOneShot(clip, _volume);
        }
    }

    /// <summary>소리 없는 구현(테스트, 오디오 비활성 빌드용 Null Object).</summary>
    public sealed class SilentSoundPlayer : ISoundPlayer
    {
        public static readonly SilentSoundPlayer Instance = new SilentSoundPlayer();
        private SilentSoundPlayer() { }
        public void Play(SoundId id) { }
    }
}
