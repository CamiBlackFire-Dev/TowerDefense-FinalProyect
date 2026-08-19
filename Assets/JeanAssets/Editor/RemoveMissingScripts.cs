using UnityEditor;
using UnityEngine;

public static class RemoveMissingScripts
{
    [MenuItem("Tools/Remove Missing Scripts")]
    private static void Remove()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            Debug.LogWarning("Selecciona un GameObject primero.");
            return;
        }

        int removed =
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(
                selected
            );

        Debug.Log($"Scripts faltantes eliminados: {removed}");
    }
}
