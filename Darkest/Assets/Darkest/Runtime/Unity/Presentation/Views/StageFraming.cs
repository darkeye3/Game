using System;
using UnityEngine;

namespace Darkest.Unity.Presentation.Views
{
    /// <summary>카메라가 잡아야 할 값(직교 크기 + 중심). 순수 계산 결과라 테스트/로그로 확인하기 쉽다.</summary>
    public readonly struct StageFraming
    {
        public StageFraming(float orthographicSize, Vector2 center)
        {
            OrthographicSize = orthographicSize;
            Center = center;
        }

        public float OrthographicSize { get; }
        public Vector2 Center { get; }

        public override string ToString() => "size=" + OrthographicSize.ToString("0.00") + " center=" + Center;
    }

    /// <summary>
    /// "월드 영역 bounds를 화면 세로 띠 [bandBottom, bandTop] 안에 다 보이게" 하는 직교 카메라 값을 계산한다.
    /// 화면 비율이 바뀌어도 진형이 UI 패널 뒤로 숨거나 좌우로 잘리지 않게 하는 것이 목적.
    /// </summary>
    public static class StageFramingMath
    {
        public static StageFraming Fit(Rect bounds, float aspect, float bandBottom, float bandTop, float margin)
        {
            if (aspect <= 0f) throw new ArgumentOutOfRangeException(nameof(aspect), "화면 비율은 0보다 커야 합니다.");
            if (bandBottom < 0f || bandTop > 1f || bandTop <= bandBottom)
                throw new ArgumentOutOfRangeException(nameof(bandTop), "띠 범위는 0 <= bottom < top <= 1 이어야 합니다.");

            float band = bandTop - bandBottom;
            // 화면 전체 높이 = 2*size. 띠 높이 = 2*size*band, 화면 폭 = 2*size*aspect.
            float sizeForHeight = bounds.height / (2f * band);
            float sizeForWidth = bounds.width / (2f * aspect);
            float size = Mathf.Max(sizeForHeight, sizeForWidth) * (1f + margin);

            // 화면 비율 f 위치의 월드 y = centerY - size + 2*size*f. 띠 중앙에 bounds 중앙이 오도록 centerY를 푼다.
            float bandCenter = (bandBottom + bandTop) * 0.5f;
            float centerY = bounds.center.y + size - 2f * size * bandCenter;
            return new StageFraming(size, new Vector2(bounds.center.x, centerY));
        }
    }
}
