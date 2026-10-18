using UnityEngine;

namespace Darkest.Unity.Presentation.Audio
{
    /// <summary>효과음 재생 창구(Adapter 경계). Presenter는 무엇을 낼지만 정하고, 어떻게 내는지는 모른다.</summary>
    public interface ISoundPlayer
    {
        void Play(SoundId id);
    }

    /// <summary>SoundId → AudioClip. 실제 에셋 / 더미 생성 / 추후 Addressables 등을 숨긴다.</summary>
    public interface ISoundLibrary
    {
        /// <returns>재생할 클립. 만들 수 없으면 null(호출자가 조용히 건너뛰지 않고 로그를 남긴다).</returns>
        AudioClip Get(SoundId id);
    }
}
