using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Ventana para configurar y probar el flujo de escenas (menu principal ->
// partida) sin escarbar en MainMenuController ni en Build Settings a mano.
// Se abre desde el menu Tower Defense > Escenas.
//
// Sirve para dos cosas:
//  - Modificar: edita el GameFlowConfig (que escena carga el boton Jugar)
//    y la lista de Build Settings.
//  - Testear: entra directo en Play Mode sobre cualquier escena, sin pasar
//    por el menu cada vez.
public class SceneFlowWindow : EditorWindow
{
    private GameFlowConfig _config;
    private SceneAsset _mainMenuSceneAsset;
    private SceneAsset _gameplaySceneAsset;
    private SceneAsset _newBuildScene;
    private SceneAsset _playModeStartScene;
    private Vector2 _scroll;

    [MenuItem("Tower Defense/Escenas")]
    public static void Open()
    {
        SceneFlowWindow window = GetWindow<SceneFlowWindow>("Escenas");
        window.minSize = new Vector2(420f, 360f);
        window.Show();
    }

    private void OnEnable()
    {
        if (_config == null)
            _config = FindConfig();

        SyncFromConfig();
        _playModeStartScene = EditorSceneManager.playModeStartScene;
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        DrawConfigSection();
        EditorGUILayout.Space();
        DrawBuildSettingsSection();
        EditorGUILayout.Space();
        DrawDebugSection();

        EditorGUILayout.EndScrollView();
    }

    // Que escena es el menu y cual carga el boton Jugar. Esto es lo que
    // lee MainMenuController en tiempo real, via el asset GameFlowConfig.
    private void DrawConfigSection()
    {
        EditorGUILayout.LabelField("Flujo del menu principal", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        GameFlowConfig newConfig = (GameFlowConfig)EditorGUILayout.ObjectField(
            "Game Flow Config", _config, typeof(GameFlowConfig), false);
        if (newConfig != _config)
        {
            _config = newConfig;
            SyncFromConfig();
        }
        if (GUILayout.Button("Buscar", GUILayout.Width(60f)))
        {
            _config = FindConfig();
            SyncFromConfig();
        }
        EditorGUILayout.EndHorizontal();

        if (_config == null)
        {
            EditorGUILayout.HelpBox(
                "No hay ningun GameFlowConfig en el proyecto todavia.",
                MessageType.Warning);
            if (GUILayout.Button("Crear Assets/_Custom/Data/GameFlowConfig.asset"))
            {
                _config = CreateConfig();
                SyncFromConfig();
            }
            return;
        }

        EditorGUI.BeginChangeCheck();
        SceneAsset menuScene = (SceneAsset)EditorGUILayout.ObjectField(
            "Escena del menu", _mainMenuSceneAsset, typeof(SceneAsset), false);
        SceneAsset playScene = (SceneAsset)EditorGUILayout.ObjectField(
            "Escena al pulsar Jugar", _gameplaySceneAsset, typeof(SceneAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            _mainMenuSceneAsset = menuScene;
            _gameplaySceneAsset = playScene;
            ApplyToConfig();
        }

        if (_gameplaySceneAsset != null && !IsInBuildSettings(_gameplaySceneAsset))
        {
            EditorGUILayout.HelpBox(
                "\"" + _gameplaySceneAsset.name + "\" no esta en Build Settings: " +
                "SceneManager.LoadScene no la va a encontrar, ni en el editor ni en un build.",
                MessageType.Error);
            if (GUILayout.Button("Agregar a Build Settings"))
                AddToBuildSettings(_gameplaySceneAsset);
        }

        EditorGUILayout.HelpBox(
            "MainMenuController (en la escena del menu) tiene que tener este " +
            "mismo asset puesto en su campo \"Game Flow\".",
            MessageType.Info);
    }

    // Lista editable de Build Settings: activar/desactivar, reordenar,
    // quitar y agregar escenas nuevas arrastrandolas.
    private void DrawBuildSettingsSection()
    {
        EditorGUILayout.LabelField("Build Settings", EditorStyles.boldLabel);

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Length == 0)
            EditorGUILayout.HelpBox("No hay escenas en Build Settings.", MessageType.Warning);

        for (int i = 0; i < scenes.Length; i++)
        {
            EditorBuildSettingsScene s = scenes[i];
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            bool enabled = EditorGUILayout.Toggle(s.enabled, GUILayout.Width(20f));
            string label = i + ": " + System.IO.Path.GetFileNameWithoutExtension(s.path);
            if (GUILayout.Button(label, EditorStyles.label, GUILayout.MinWidth(140f)))
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path));

            EditorGUI.BeginDisabledGroup(i == 0);
            if (GUILayout.Button("▲", GUILayout.Width(24f)))
                MoveScene(i, i - 1);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(i == scenes.Length - 1);
            if (GUILayout.Button("▼", GUILayout.Width(24f)))
                MoveScene(i, i + 1);
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Abrir", GUILayout.Width(50f)))
                OpenSceneSafely(s.path);

            if (GUILayout.Button("Quitar", GUILayout.Width(55f)))
                RemoveScene(i);

            EditorGUILayout.EndHorizontal();

            if (enabled != s.enabled)
                SetSceneEnabled(i, enabled);
        }

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        _newBuildScene = (SceneAsset)EditorGUILayout.ObjectField(
            "Agregar escena", _newBuildScene, typeof(SceneAsset), false);
        EditorGUI.BeginDisabledGroup(_newBuildScene == null);
        if (GUILayout.Button("Agregar", GUILayout.Width(70f)))
        {
            AddToBuildSettings(_newBuildScene);
            _newBuildScene = null;
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
    }

    // Atajos para probar sin pasar por el menu cada vez.
    private void DrawDebugSection()
    {
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _playModeStartScene = (SceneAsset)EditorGUILayout.ObjectField(
            "Empezar Play Mode siempre en", _playModeStartScene, typeof(SceneAsset), false);
        if (EditorGUI.EndChangeCheck())
            EditorSceneManager.playModeStartScene = _playModeStartScene;

        EditorGUILayout.HelpBox(
            "Con esto asignado, el boton Play del editor arranca siempre en esa " +
            "escena sin importar cual tengas abierta. Dejalo vacio para volver al " +
            "comportamiento normal (arranca en la escena que tengas abierta).",
            MessageType.Info);

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(Application.isPlaying);

        EditorGUI.BeginDisabledGroup(_gameplaySceneAsset == null);
        string playLabel = _gameplaySceneAsset != null ? "▶ Probar \"" + _gameplaySceneAsset.name + "\" ahora" : "▶ Probar la escena de juego ahora";
        if (GUILayout.Button(playLabel))
            PlayScene(_gameplaySceneAsset);
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(_mainMenuSceneAsset == null);
        string menuLabel = _mainMenuSceneAsset != null ? "▶ Jugar desde \"" + _mainMenuSceneAsset.name + "\"" : "▶ Jugar desde el menu";
        if (GUILayout.Button(menuLabel))
            PlayScene(_mainMenuSceneAsset);
        EditorGUI.EndDisabledGroup();

        EditorGUI.EndDisabledGroup();

        if (Application.isPlaying)
            EditorGUILayout.HelpBox("Sal de Play Mode para poder cambiar de escena.", MessageType.None);
    }

    private void SyncFromConfig()
    {
        if (_config == null)
        {
            _mainMenuSceneAsset = null;
            _gameplaySceneAsset = null;
            return;
        }

        _mainMenuSceneAsset = FindSceneAsset(_config.mainMenuScene);
        _gameplaySceneAsset = FindSceneAsset(_config.gameplayScene);
    }

    private void ApplyToConfig()
    {
        if (_config == null)
            return;

        Undo.RecordObject(_config, "Cambiar flujo de escenas");
        _config.mainMenuScene = _mainMenuSceneAsset != null ? _mainMenuSceneAsset.name : "";
        _config.gameplayScene = _gameplaySceneAsset != null ? _gameplaySceneAsset.name : "";
        EditorUtility.SetDirty(_config);
    }

    // Busca primero en Build Settings (asi coincide con lo que de verdad va
    // a usar SceneManager.LoadScene) y si no aparece, en todo el proyecto.
    private static SceneAsset FindSceneAsset(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return null;

        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            if (System.IO.Path.GetFileNameWithoutExtension(s.path) == sceneName)
                return AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path);
        }

        string[] guids = AssetDatabase.FindAssets(sceneName + " t:Scene");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                return AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }

        return null;
    }

    private static bool IsInBuildSettings(SceneAsset scene)
    {
        string path = AssetDatabase.GetAssetPath(scene);
        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
        {
            if (s.path == path)
                return true;
        }
        return false;
    }

    private static void AddToBuildSettings(SceneAsset scene)
    {
        string path = AssetDatabase.GetAssetPath(scene);
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        foreach (EditorBuildSettingsScene s in scenes)
        {
            if (s.path == path)
                return;
        }

        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void SetSceneEnabled(int index, bool enabled)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        scenes[index].enabled = enabled;
        EditorBuildSettings.scenes = scenes;
    }

    private static void MoveScene(int from, int to)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        EditorBuildSettingsScene item = scenes[from];
        scenes.RemoveAt(from);
        scenes.Insert(to, item);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void RemoveScene(int index)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        scenes.RemoveAt(index);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // Pregunta por guardar cambios sin commitear antes de cambiar de escena
    // (abrir otra escena sin guardar borra lo que tengas sin guardar).
    private static void OpenSceneSafely(string path)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(path);
    }

    // Abre la escena y entra en Play Mode directo, como si la hubieras
    // abierto a mano y hubieras pulsado Play. Tambien deja esa escena
    // marcada como inicio de Play Mode, para que coincida con el toggle
    // de arriba en vez de contradecirlo.
    private static void PlayScene(SceneAsset scene)
    {
        if (scene == null)
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.playModeStartScene = scene;
        EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(scene));
        EditorApplication.isPlaying = true;
    }

    private static GameFlowConfig FindConfig()
    {
        string[] guids = AssetDatabase.FindAssets("t:GameFlowConfig");
        if (guids.Length == 0)
            return null;

        return AssetDatabase.LoadAssetAtPath<GameFlowConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static GameFlowConfig CreateConfig()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Custom/Data"))
            AssetDatabase.CreateFolder("Assets/_Custom", "Data");

        GameFlowConfig config = ScriptableObject.CreateInstance<GameFlowConfig>();
        string path = "Assets/_Custom/Data/GameFlowConfig.asset";
        AssetDatabase.CreateAsset(config, path);
        AssetDatabase.SaveAssets();
        return config;
    }
}
