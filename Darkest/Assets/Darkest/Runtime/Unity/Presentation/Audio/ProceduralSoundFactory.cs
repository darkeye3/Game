using UnityEngine;

namespace Darkest.Unity.Presentation.Audio
{
    /// <summary>
    /// 더미 효과음 생성기: 주파수가 미끄러지는 사인파 + 잡음 + 감쇠 엔벨로프. 에셋 없이도 타격감을 확인하기 위한 용도다.
    /// 소리 모양은 SoundId별 레시피 표 한 곳에서만 정한다.
    /// </summary>
    public static class ProceduralSoundFactory
    {
        private const int SampleRate = 22050;

        private readonly struct Recipe
        {
            public Recipe(float startHz, float endHz, float seconds, float noise, float volume)
            {
                StartHz = startHz;
                EndHz = endHz;
                Seconds = seconds;
                Noise = noise;
                Volume = volume;
            }

            public float StartHz { get; }
            public float EndHz { get; }
            public float Seconds { get; }
            /// <summary>0 = 순수한 음, 1 = 잡음만.</summary>
            public float Noise { get; }
            public float Volume { get; }
        }

        public static AudioClip Create(SoundId id)
        {
            var recipe = RecipeFor(id);
            int length = Mathf.Max(1, Mathf.RoundToInt(recipe.Seconds * SampleRate));
            var samples = new float[length];
            // 같은 소리는 항상 같은 파형이 되도록 잡음 seed를 SoundId로 고정한다.
            var noise = new System.Random((int)id + 1);
            float phase = 0f;
            for (int i = 0; i < length; i++)
            {
                float progress = (float)i / length;
                float hz = Mathf.Lerp(recipe.StartHz, recipe.EndHz, progress);
                phase += 2f * Mathf.PI * hz / SampleRate;
                float tone = Mathf.Sin(phase);
                float hiss = (float)(noise.NextDouble() * 2.0 - 1.0);
                float envelope = (1f - progress) * (1f - progress);
                samples[i] = Mathf.Lerp(tone, hiss, recipe.Noise) * envelope * recipe.Volume;
            }

            var clip = AudioClip.Create("dummy_" + id, length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static Recipe RecipeFor(SoundId id)
        {
            switch (id)
            {
                case SoundId.Hit: return new Recipe(220f, 90f, 0.12f, 0.6f, 0.8f);
                case SoundId.Crit: return new Recipe(440f, 70f, 0.25f, 0.5f, 1f);
                case SoundId.Miss: return new Recipe(900f, 600f, 0.1f, 0.8f, 0.4f);
                case SoundId.Heal: return new Recipe(520f, 880f, 0.3f, 0f, 0.5f);
                case SoundId.Status: return new Recipe(300f, 260f, 0.2f, 0.3f, 0.5f);
                case SoundId.Stress: return new Recipe(140f, 110f, 0.35f, 0.2f, 0.6f);
                case SoundId.Death: return new Recipe(160f, 40f, 0.6f, 0.4f, 0.9f);
                case SoundId.BattleStart: return new Recipe(110f, 220f, 0.4f, 0.1f, 0.8f);
                case SoundId.Victory: return new Recipe(440f, 990f, 0.5f, 0f, 0.6f);
                case SoundId.Defeat: return new Recipe(220f, 55f, 0.8f, 0.1f, 0.7f);
                case SoundId.Fled: return new Recipe(700f, 300f, 0.3f, 0.5f, 0.5f);
                case SoundId.RoomEnter: return new Recipe(180f, 150f, 0.15f, 0.7f, 0.3f);
                case SoundId.Loot: return new Recipe(1200f, 1600f, 0.15f, 0f, 0.4f);
                case SoundId.Trinket: return new Recipe(880f, 1760f, 0.4f, 0f, 0.5f);
                case SoundId.Camp: return new Recipe(90f, 80f, 0.6f, 0.9f, 0.4f);
                case SoundId.CampSkill: return new Recipe(400f, 600f, 0.2f, 0.1f, 0.4f);
                case SoundId.LevelUp: return new Recipe(330f, 1320f, 0.6f, 0f, 0.6f);
                case SoundId.Curio: return new Recipe(600f, 350f, 0.3f, 0.2f, 0.4f);
                default: return new Recipe(440f, 440f, 0.1f, 0f, 0.3f);
            }
        }
    }
}
