using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Inspector del camino: en modo Procedural reconstruye la "C" al cambiar
// cualquier ajuste; en modo Manual solo muestra y reconecta manualWaypoints.
[CustomEditor(typeof(PathBuilder))]
public class PathBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PathBuilder builder = (PathBuilder)target;

        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("mode"));

        bool manual = builder.mode == PathMode.Manual;
        if (manual)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("manualWaypoints"), true);
        else
            DrawPropertiesExcluding(serializedObject, "m_Script", "mode", "manualWaypoints");

        bool changed = serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        if (manual)
        {
            EditorGUILayout.HelpBox(
                "Arrastra los puntos del camino en orden: el primero es por donde salen " +
                "los enemigos, el ultimo es donde termina. Sirve para un camino ya trazado " +
                "a mano sobre otro mapa o mesh (por ejemplo el de un companero).\n" +
                "Si vienes del modo Procedural, borra a mano los hijos \"Tiles\" y " +
                "\"Waypoints\" que ya no uses.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "El recorrido empieza arriba a la derecha y termina abajo a la derecha.\n" +
                "Si un modelo sale girado, ajusta su Yaw a 90, 180 o 270.",
                MessageType.Info);
        }

        if (changed)
            Rebuild(builder, "Cambiar ajustes del camino");

        if (GUILayout.Button(manual ? "Actualizar camino manual" : "Reconstruir camino"))
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
