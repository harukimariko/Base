using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

[InitializeOnLoad]
public static class FixedScenePlay
{
    private const string ToolbarPath = "Project/固定シーン再生";
    private const string PendingKey = "FixedScenePlay.Pending";
    private const string PreviousSceneKey = "FixedScenePlay.PreviousScene";
    private static string PreferenceKey => "FixedScenePlay.Scene." + Application.dataPath;

    static FixedScenePlay()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += RestoreIfIdle;
    }

    public static SceneAsset StartScene
    {
        get
        {
            var defaultGuid = AssetDatabase.AssetPathToGUID("Assets/Scenes/Boot.unity");
            var guid = EditorPrefs.GetString(PreferenceKey, defaultGuid);
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(guid));
        }
        set
        {
            EditorPrefs.SetString(PreferenceKey,
                value == null ? "" : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value)));
            MainToolbar.Refresh(ToolbarPath);
        }
    }

    [MainToolbarElement(ToolbarPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement CreateToolbarButton()
    {
        var scene = StartScene;
        var label = scene == null ? "起動シーン未設定" : scene.name + " から再生";
        var content = new MainToolbarContent(label,
            EditorGUIUtility.IconContent("PlayButton").image as Texture2D,
            "設定したシーンから再生します。右クリックで起動シーンを変更できます。");

        return new MainToolbarButton(content, Play)
        {
            enabled = scene != null && !EditorApplication.isPlayingOrWillChangePlaymode &&
                      !EditorApplication.isCompiling,
            populateContextMenu = menu => menu.AppendAction("起動シーンの設定", _ => FixedScenePlayWindow.Open())
        };
    }

    [MenuItem("Tools/固定シーン再生/再生")]
    public static void Play()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            return;

        var scene = StartScene;
        if (scene == null)
        {
            FixedScenePlayWindow.Open();
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var previous = EditorSceneManager.playModeStartScene;
        SessionState.SetString(PreviousSceneKey,
            previous == null ? "" : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(previous)));
        SessionState.SetBool(PendingKey, true);
        EditorSceneManager.playModeStartScene = scene;
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            RestorePreviousStartScene();

        MainToolbar.Refresh(ToolbarPath);
    }

    private static void RestoreIfIdle()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
            RestorePreviousStartScene();
    }

    private static void RestorePreviousStartScene()
    {
        if (!SessionState.GetBool(PendingKey, false))
            return;

        var guid = SessionState.GetString(PreviousSceneKey, "");
        EditorSceneManager.playModeStartScene =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(guid));
        SessionState.EraseBool(PendingKey);
        SessionState.EraseString(PreviousSceneKey);
    }
}

public class FixedScenePlayWindow : EditorWindow
{
    [MenuItem("Tools/固定シーン再生/設定")]
    public static void Open()
    {
        GetWindow<FixedScenePlayWindow>("固定シーン再生");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("起動シーン", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "専用ボタンを押すと、編集中のシーンに関係なく指定したシーンから再生します。停止後は編集中のシーンに戻ります。設定はこのPC・このプロジェクト用に保存されます。",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
        {
            EditorGUI.BeginChangeCheck();
            var scene = (SceneAsset)EditorGUILayout.ObjectField(
                "起動シーン", FixedScenePlay.StartScene, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck())
                FixedScenePlay.StartScene = scene;

            using (new EditorGUI.DisabledScope(scene == null || EditorApplication.isCompiling))
            {
                if (GUILayout.Button("指定シーンから再生"))
                    FixedScenePlay.Play();
            }
        }
    }
}
