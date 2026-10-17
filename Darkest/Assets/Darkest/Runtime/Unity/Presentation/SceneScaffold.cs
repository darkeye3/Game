using Darkest.Unity.Infrastructure;
using Darkest.Unity.Presentation.Ui;
using Darkest.Unity.Presentation.Views;
using UnityEngine;

namespace Darkest.Unity.Presentation
{
    /// <summary>
    /// 씬에 필요한 카메라/캔버스/화면들을 코드로 한 번만 만든다(빈 씬 + GameBootstrap 하나로 실행 가능).
    /// 프리팹 기반 UI로 바꿀 때는 이 클래스만 "프리팹 인스턴스화"로 교체하면 된다.
    /// </summary>
    public sealed class SceneScaffold
    {
        private const float CameraSize = 5f;
        private const float CameraDepth = -10f;
        private static readonly Color CameraBackground = new Color(0.05f, 0.04f, 0.05f);

        public SceneScaffold(Transform root, ISpriteProvider sprites)
        {
            Ui = new UiFactory();
            EnsureCamera();

            var stageGo = new GameObject("BattleStage");
            stageGo.transform.SetParent(root, false);
            Stage = stageGo.AddComponent<BattleStageView>();
            Stage.Initialize(sprites, Ui.Font);
            Animations = stageGo.AddComponent<BattleAnimationPlayer>();
            Animations.Initialize(Stage, Ui.Font);

            var canvas = Ui.CreateCanvas("DarkestCanvas");
            canvas.transform.SetParent(root, false);
            var canvasRect = (RectTransform)canvas.transform;

            Town = CreateScreen<TownView>(canvasRect);
            Dungeon = CreateScreen<DungeonView>(canvasRect);
            Battle = CreateScreen<BattleView>(canvasRect);
            Result = CreateScreen<ResultView>(canvasRect);

            Log = canvas.gameObject.AddComponent<EventLogView>();
            Log.Build(Ui, canvasRect, Vector2.zero, new Vector2(1f, UiTheme.LogPanelHeight));
        }

        public UiFactory Ui { get; }
        public BattleStageView Stage { get; }
        public BattleAnimationPlayer Animations { get; }
        public TownView Town { get; }
        public DungeonView Dungeon { get; }
        public BattleView Battle { get; }
        public ResultView Result { get; }
        public EventLogView Log { get; }

        private T CreateScreen<T>(RectTransform canvas) where T : ScreenView
        {
            var view = canvas.gameObject.AddComponent<T>();
            view.Build(Ui, canvas);
            return view;
        }

        private static void EnsureCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
                camera.tag = "MainCamera";
            }
            camera.orthographic = true;
            camera.orthographicSize = CameraSize;
            camera.transform.position = new Vector3(0f, 0f, CameraDepth);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CameraBackground;
        }
    }
}
