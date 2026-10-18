using System;
using Darkest.Domain.Core;
using UnityEngine;

namespace Darkest.Unity.Presentation.Audio
{
    /// <summary>
    /// Resources/Darkest/Sounds/&lt;SoundId&gt; 에셋을 먼저 찾고, 없으면 더미 소리를 만든다. 한 번 찾은 클립은 캐시해서
    /// 재생할 때마다 Resources.Load나 파형 생성을 반복하지 않는다(GC/지연 방지).
    /// </summary>
    public sealed class ResourcesSoundLibrary : ISoundLibrary
    {
        private const string LogCategory = "Sound";
        private const string ResourceFolder = "Darkest/Sounds/";

        private readonly AudioClip[] _cache = new AudioClip[Enum.GetValues(typeof(SoundId)).Length];
        private readonly IGameLogger _logger;

        public ResourcesSoundLibrary(IGameLogger logger)
        {
            _logger = logger ?? NullLogger.Instance;
        }

        public AudioClip Get(SoundId id)
        {
            int index = (int)id;
            if (_cache[index] != null) return _cache[index];

            var clip = Resources.Load<AudioClip>(ResourceFolder + id);
            if (clip == null)
            {
                _logger.Verbose(LogCategory, id + " 효과음 에셋 없음 → 더미 소리 생성");
                clip = ProceduralSoundFactory.Create(id);
            }
            _cache[index] = clip;
            return clip;
        }
    }
}
