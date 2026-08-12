using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Convierte los modelos de Assets/_Custom/Models/Enemies en prefabs listos
// para usar: capa Enemy, collider a la medida, movimiento por el camino y vida.
// Se usa desde el menu Tower Defense > Crear prefabs de enemigos.
// Se puede repetir cuantas veces se quiera: los prefabs se sobreescriben.
public static class EnemyPrefabBuilder
{
    private const string ModelsFolder = "Assets/_Custom/Models/Enemies";
    private const string PrefabsFolder = "Assets/_Custom/Prefabs/Enemies";

    [MenuItem("Tower Defense/Crear prefabs de enemigos")]
    public static void CreateEnemyPrefabs()
    {
        List<GameObject> models = FindEnemyModels();
        if (models.Count == 0)
        {
            Debug.LogWarning("No se encontraron modelos de enemigos en " + ModelsFolder);
            return;
        }

        EnsurePrefabsFolder();

        // El animator se arma primero, para poder ponerselo a cada prefab.
        RuntimeAnimatorController animator = EnemyAnimatorBuilder.EnsureController();

        List<GameObject> prefabs = new List<GameObject>();
        float footOffset = 0f;

        foreach (GameObject model in models)
        {
            float modelOffset;
            GameObject prefab = CreatePrefab(model, animator, out modelOffset);
            if (prefab == null)
                continue;

            prefabs.Add(prefab);
            footOffset = Mathf.Max(footOffset, modelOffset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Prefabs de enemigos creados: " + prefabs.Count + " en " + PrefabsFolder);
        WireScene(prefabs, footOffset);
    }

    // Modelos sueltos de la carpeta de enemigos. Se saltan los rigs de
    // animacion y todo lo que este en subcarpetas (armas, props, etc).
    private static List<GameObject> FindEnemyModels()
    {
        List<GameObject> models = new List<GameObject>();
        string[] guids = AssetDatabase.FindAssets("t:Model", new string[] { ModelsFolder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

            if (path != ModelsFolder + "/" + fileName + System.IO.Path.GetExtension(path))
                continue;

            if (fileName.StartsWith("Rig_"))
                continue;

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model != null)
                models.Add(model);
        }

        models.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        return models;
    }

    // Arma un enemigo completo alrededor del modelo y lo guarda como prefab.
    // footOffset dice que tan arriba del pivote empieza el modelo.
    private static GameObject CreatePrefab(GameObject model, RuntimeAnimatorController animatorController,
        out float footOffset)
    {
        footOffset = 0f;

        GameObject root = new GameObject(model.name);

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            root.layer = enemyLayer;

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
        if (visual == null)
        {
            Object.DestroyImmediate(root);
            return null;
        }

        visual.name = "Visual";
        visual.transform.SetParent(root.transform, false);

        // El Animator va en el modelo, que es donde estan los huesos.
        // Root Motion apagado: quien mueve al enemigo es TestEnemyMovement.
        if (animatorController != null)
        {
            Animator animator = visual.GetComponent<Animator>();
            if (animator == null)
                animator = visual.AddComponent<Animator>();

            animator.runtimeAnimatorController = animatorController;
            animator.applyRootMotion = false;
        }

        // Collider a la medida del modelo: es lo que ven las torres.
        Bounds bounds = CalculateBounds(visual);
        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.height = Mathf.Max(0.2f, bounds.size.y);
        collider.radius = Mathf.Max(0.1f, Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f);
        collider.center = new Vector3(0f, bounds.center.y, 0f);

        root.AddComponent<TestEnemyMovement>();
        root.AddComponent<EnemyFacing>();
        root.AddComponent<EnemyHealth>();
        // La barra de vida se puede ajustar desde el propio prefab
        // (posicion, tamano y colores) sin entrar en Play Mode.
        root.AddComponent<EnemyHealthBar>();

        footOffset = -bounds.min.y;

        string path = PrefabsFolder + "/" + model.name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        return prefab;
    }

    // Junta los limites de todas las partes visibles del modelo.
    private static Bounds CalculateBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(new Vector3(0f, 0.5f, 0f), Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return bounds;
    }

    // Deja la escena abierta lista: el spawner con los enemigos nuevos y los
    // waypoints a la altura correcta segun el pivote de los modelos.
    private static void WireScene(List<GameObject> prefabs, float footOffset)
    {
        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.enemyPrefabs = prefabs.ToArray();
            EditorUtility.SetDirty(spawner);
            Debug.Log("EnemySpawner de la escena actualizado con los enemigos nuevos.", spawner);
        }

        PathBuilder builder = Object.FindFirstObjectByType<PathBuilder>();
        if (builder != null)
        {
            builder.enemyHalfHeight = footOffset;
            builder.Rebuild();
            EditorUtility.SetDirty(builder);
            Debug.Log("Waypoints ajustados a la altura de los enemigos.", builder);
        }

        if (spawner != null || builder != null)
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
    }

    private static void EnsurePrefabsFolder()
    {
        if (AssetDatabase.IsValidFolder(PrefabsFolder))
            return;

        AssetDatabase.CreateFolder("Assets/_Custom/Prefabs", "Enemies");
    }
}
