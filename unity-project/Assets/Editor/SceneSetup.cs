using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Editor script to programmatically create Main.unity scene.
/// Run via: Unity.exe -batchmode -executeMethod SceneSetup.Setup -quit
/// Creates: Camera, Canvas (540x960 reference), EventSystem, GameBootstrap, GameRenderer.
/// </summary>
public static class SceneSetup
{
    [MenuItem("MobileForge/Setup Main Scene")]
    public static void Setup()
    {
        // Create new scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var cameraGO = new GameObject("Main Camera");
        var cam = cameraGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
        cam.orthographic = true;
        cameraGO.AddComponent<AudioListener>();
        cameraGO.tag = "MainCamera";

        // Canvas (540x960 mobile reference)
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(540, 960);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // GameRenderer on the Canvas
        canvasGO.AddComponent<GameRenderer>();

        // EventSystem
        var eventGO = new GameObject("EventSystem");
        eventGO.AddComponent<EventSystem>();
        eventGO.AddComponent<StandaloneInputModule>();

        // GameBootstrap (empty GameObject)
        var bootstrapGO = new GameObject("GameBootstrap");
        bootstrapGO.AddComponent<GameBootstrap>();

        // Save scene
        string scenePath = "Assets/Scenes/Main.unity";
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        EditorSceneManager.SaveScene(scene, scenePath);

        // Set as the scene in build settings
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };

        Debug.Log($"[SceneSetup] Created and saved {scenePath}");
    }
}
