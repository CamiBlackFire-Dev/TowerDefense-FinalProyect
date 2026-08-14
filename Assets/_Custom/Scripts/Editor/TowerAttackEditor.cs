using UnityEditor;
using UnityEngine;

// Inspector de la torre con el alcance a la mano: dibuja el circulo en la
// vista de escena y deja arrastrarlo con el raton para probar distancias.
// Si la torre tiene catalogo, el arrastre escribe en el TowerData del nivel
// (que es de donde salen los valores de verdad); si no, escribe en el
// campo range de la propia torre.
[CustomEditor(typeof(TowerAttack))]
public class TowerAttackEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TowerAttack attack = (TowerAttack)target;
        TowerData data = LevelData(attack);

        EditorGUILayout.Space();

        if (data != null)
        {
            EditorGUILayout.HelpBox(
                "El alcance sale del catalogo (nivel " + LevelOf(attack) + "). Arrastra el " +
                "circulo en la escena o usa el slider de abajo: los dos escriben " +
                "en el catalogo, asi que afectan a todas las torres de ese nivel.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            float range = EditorGUILayout.Slider("Alcance del nivel", data.range, 0f, 30f);
            if (EditorGUI.EndChangeCheck())
                SetCatalogRange(data, range, attack);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Esta torre no tiene catalogo: usa su propio campo Range. " +
                "Puedes arrastrar el circulo en la escena para ajustarlo.",
                MessageType.Info);
        }

        if (GUILayout.Button("Abrir Game Debug"))
            GameDebugWindow.Open();
    }

    // Circulo con tiradores alrededor de la torre seleccionada.
    private void OnSceneGUI()
    {
        TowerAttack attack = (TowerAttack)target;

        Handles.color = GameDebugWindow.ColorForLevel(LevelOf(attack));

        EditorGUI.BeginChangeCheck();
        float newRange = Handles.RadiusHandle(
            Quaternion.identity, attack.transform.position, attack.range);

        if (!EditorGUI.EndChangeCheck())
            return;

        newRange = Mathf.Max(0f, newRange);

        TowerData data = LevelData(attack);
        if (data != null)
        {
            SetCatalogRange(data, newRange, attack);
        }
        else
        {
            Undo.RecordObject(attack, "Cambiar alcance de la torre");
            attack.range = newRange;
            EditorUtility.SetDirty(attack);
        }
    }

    // Guarda el alcance en el catalogo y refresca las torres de la escena
    // para verlo al momento (tambien en Play Mode).
    private static void SetCatalogRange(TowerData data, float range, TowerAttack attack)
    {
        Undo.RecordObject(data, "Cambiar alcance de la torre");
        data.range = Mathf.Max(0f, range);
        EditorUtility.SetDirty(data);

        TowerAttack[] towers = Object.FindObjectsByType<TowerAttack>(FindObjectsSortMode.None);
        foreach (TowerAttack tower in towers)
        {
            if (tower.gameObject.scene.IsValid())
                tower.ForceRefreshStats();
        }
    }

    private static int LevelOf(TowerAttack attack)
    {
        Tower tower = attack.GetComponent<Tower>();
        return tower != null ? tower.Level : 1;
    }

    // Datos del nivel que le tocan a esta torre, o null si no hay catalogo.
    private static TowerData LevelData(TowerAttack attack)
    {
        if (attack.catalog == null)
            return null;

        return attack.catalog.GetLevelData(LevelOf(attack));
    }
}
