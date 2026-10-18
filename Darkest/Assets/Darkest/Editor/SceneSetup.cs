using Darkest.Unity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Darkest.Editor
{
    /// <summary>
    /// "Darkest/Create Main Scene": GameBootstrap 하나만 있는 씬을 만들어 저장한다.
    /// 동봉된 Main.unity가 깨졌거나 새로 만들고 싶을 때 쓴다.
    /// </summary>
    public static class SceneSetup
    {
        private const string ScenePath = "Assets/Darkest/Scenes/Main.unity";

        [MenuItem("Darkest/Create Main Scene")]
        public static void CreateMainScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[Darkest][Editor] 씬 생성: " + ScenePath + " — Play를 누르면 바로 실행됩니다.");
        }
    }
}
