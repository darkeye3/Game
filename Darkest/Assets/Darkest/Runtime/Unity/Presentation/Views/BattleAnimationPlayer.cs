using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Darkest.Unity.Presentation.Views
{
    public enum BeatKind
    {
        Hit,
        Crit,
        Heal,
        Miss,
        Status,
        Stress,
        Death
    }

    /// <summary>
    /// 전투 연출 큐. 도메인은 명령을 즉시 끝까지 처리하고, 여기서는 그 결과(이벤트)를 "비트" 단위로 순서대로 재생한다.
    /// - 코루틴은 한 번에 하나만(핸들로 중복 실행 방지), 프레임마다 WaitForSeconds를 new 하지 않는다.
    /// - 떠오르는 숫자는 TextMesh 풀에서 재사용.
    /// </summary>
    public sealed class BattleAnimationPlayer : MonoBehaviour
    {
        private const float BeatDuration = 0.45f;
        private const float ShakeAmplitude = 0.12f;
        private const float ShakeFrequency = 40f;
        private const float TextStartHeight = 1.5f;
        private const float TextRiseDistance = 0.6f;
        private const float TextCharacterSize = 0.1f;
        private const int TextFontSize = 48;
        private const int MaxQueuedBeats = 64;

        private readonly Queue<Beat> _beats = new Queue<Beat>();
        private BattleStageView _stage;
        private TextMesh _text;
        private Coroutine _routine;
        private Action _onFinished;

        public bool IsPlaying => _routine != null;

        /// <summary>비트 하나가 화면에 나타나는 순간. 효과음처럼 연출과 박자를 맞춰야 하는 구독자용.</summary>
        public event Action<BeatKind> BeatStarted;

        public void Initialize(BattleStageView stage, Font font)
        {
            _stage = stage;
            var go = new GameObject("FloatingText");
            go.transform.SetParent(transform, false);
            _text = go.AddComponent<TextMesh>();
            _text.font = font;
            _text.fontSize = TextFontSize;
            _text.characterSize = TextCharacterSize;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            go.SetActive(false);
        }

        public void Enqueue(string combatantId, string text, Color color, BeatKind kind)
        {
            if (_beats.Count >= MaxQueuedBeats)
            {
                // 연출이 밀려도 게임 진행을 막지 않는다. 오래된 비트를 버리고 경고만 남긴다.
                Debug.LogWarning("[Darkest][Anim] 연출 큐가 가득 차서 오래된 연출을 건너뜁니다.");
                _beats.Dequeue();
            }
            _beats.Enqueue(new Beat(combatantId, text, color, kind));
        }

        /// <summary>쌓인 비트를 재생하고 끝나면 onFinished. 비트가 없으면 즉시 호출.</summary>
        public void Play(Action onFinished)
        {
            _onFinished = onFinished; // 재생 중 다시 불리면 마지막 콜백만 유지한다.
            if (_routine != null) return;
            if (_beats.Count == 0)
            {
                Finish();
                return;
            }
            _routine = StartCoroutine(Run());
        }

        public void Clear()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            _beats.Clear();
            _onFinished = null;
            if (_text != null) _text.gameObject.SetActive(false);
        }

        private IEnumerator Run()
        {
            while (_beats.Count > 0)
            {
                var beat = _beats.Dequeue();
                if (!_stage.TryGetLastSlot(beat.CombatantId, out var slot)) continue;
                _stage.TryGetUnit(beat.CombatantId, out var unit);

                _text.text = beat.Text;
                _text.color = beat.Color;
                _text.gameObject.SetActive(true);
                BeatStarted?.Invoke(beat.Kind);

                for (float t = 0f; t < BeatDuration; t += Time.deltaTime)
                {
                    float progress = t / BeatDuration;
                    _text.transform.localPosition = slot + Vector3.up * (TextStartHeight + TextRiseDistance * progress);
                    if (unit != null && ShouldShake(beat.Kind))
                        unit.SetOffset(Vector3.right * (Mathf.Sin(t * ShakeFrequency) * ShakeAmplitude * (1f - progress)));
                    yield return null;
                }

                if (unit != null) unit.SetOffset(Vector3.zero);
                _text.gameObject.SetActive(false);
            }
            _routine = null;
            Finish();
        }

        private void Finish()
        {
            var callback = _onFinished;
            _onFinished = null;
            callback?.Invoke();
        }

        private static bool ShouldShake(BeatKind kind) => kind == BeatKind.Hit || kind == BeatKind.Crit || kind == BeatKind.Death;

        private void OnDisable() => Clear();

        private readonly struct Beat
        {
            public Beat(string combatantId, string text, Color color, BeatKind kind)
            {
                CombatantId = combatantId;
                Text = text;
                Color = color;
                Kind = kind;
            }

            public string CombatantId { get; }
            public string Text { get; }
            public Color Color { get; }
            public BeatKind Kind { get; }
        }
    }
}
