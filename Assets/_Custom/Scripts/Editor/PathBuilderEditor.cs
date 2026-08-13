using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Inspector del camino: reconstruye la "C" al cambiar cualquier ajuste.
[CustomEditor(typeof(PathBuilder))]
public class PathBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PathBuilder builder = (PathBuilder)target;

        if (DrawDefaultInspector())
            Rebuild(builder, "Cambiar ajustes del camino");

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "El recorrido empieza arriba a la derecha y termina abajo a la derecha.\n" +
            "Si un modelo sale girado, ajusta su Yaw a 90, 180 o 270.",
            MessageType.Info);

        if (GUILayout.Button("Reconstruir camino"))
            Rebuild(builder, "Reconstruir camino");
    }

    private void Rebuild(PathBuilder builder, string undoName)
    {
        Undo.RecordObject(builder, undoName);
        builder.Rebuild();
        EditorUtility.SetDirty(builder);
        if (builder.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
    }
}
