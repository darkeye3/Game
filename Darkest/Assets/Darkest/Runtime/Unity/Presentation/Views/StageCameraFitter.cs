using Darkest.Domain.Core;
using Darkest.Unity.Presentation.Ui;
using UnityEngine;

namespace Darkest.Unity.Presentation.Views
{
    /// <summary>
    /// 캔버스 루트에 붙어 화면 크기 변경(OnRectTransformDimensionsChange)마다 카메라를 다시 맞춘다.
    /// Update로 매 프레임 검사하지 않고, uGUI가 주는 크기 변경 콜백만 쓴다.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class StageCameraFitter : MonoBehaviour
    {
        private const float Margin = 0.06f;
        private const float CameraDepth = -10f;

        private Camera _camera;
        private Transform _stage;
        private IGameLogger _logger;
        private int _lastWidth;
        private int _lastHeight;

        public void Initialize(Camera targetCamera, Transform stage, IGameLogger logger)
        {
            _camera = targetCamera;
            _stage = stage;
            _logger = logger ?? NullLogger.Instance;
            Refit();
        }

        private void OnRectTransformDimensionsChange() => Refit();

        private void Refit()
        {
            if (_camera == null || _stage == null) return;
            int width = Screen.width;
            int height = Screen.height;
            if (width <= 0 || height <= 0) return;
            if (width == _lastWidth && height == _lastHeight) return;
            _lastWidth = width;
            _lastHeight = height;

            var local = BattleStageView.LocalBounds;
            var worldMin = _stage.TransformPoint(local.min);
            var worldMax = _stage.TransformPoint(local.max);
            var bounds = Rect.MinMaxRect(worldMin.x, worldMin.y, worldMax.x, worldMax.y);

            float bandBottom = UiTheme.ToScreenFraction(UiTheme.BattlePanelTop);
            float bandTop = UiTheme.ToScreenFraction(UiTheme.BattleHeaderBottom);
            var framing = StageFramingMath.Fit(bounds, (float)width / height, bandBottom, bandTop, Margin);

            _camera.orthographicSize = framing.OrthographicSize;
            _camera.transform.position = new Vector3(framing.Center.x, framing.Center.y, CameraDepth);
            if (_logger.IsEnabled(LogLevel.Verbose))
                _logger.Verbose("UI", "스테이지 카메라 맞춤 " + width + "x" + height + " → " + framing);
        }
    }
}
