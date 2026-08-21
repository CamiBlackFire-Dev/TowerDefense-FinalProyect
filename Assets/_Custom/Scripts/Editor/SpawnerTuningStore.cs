using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Guarda los numeros del EnemySpawner que se tocaron en Play Mode y los
// vuelve a poner al salir, que si no Unity los descarta.
//
// Solo copia numeros (floats, ints, bools), nunca referencias a objetos:
// un prefab o un manager guardado asi volveria roto, porque las
// referencias de Play Mode no valen en Edit Mode.
//
// El estado vive en EditorPrefs (no en un campo estatico) porque salir de
// Play Mode recarga el dominio y se llevaria por delante cualquier estatico.
[Serializable]
public class SpawnerTuning
{
    public int enemiesPerWave;
    public float spawnInterval;
    public float startDelay;
    public int spawnWaypointIndex;
    public int waveSurvivalGold;

    public float enemyHealth;
    public float enemySpeed;
    public int enemyReward;
    public int damagePerEnemy;

    public float healthGrowthPerWave;
    public float speedGrowthPerWave;
    public float shooterDamageGrowthPerWave;
    public int enemiesGrowthPerWave;
    public float shooterChanceGrowthPerWave;

    public float shooterChance;
    public float shooterRange;
    public float shooterDamage;
    public float shooterAttackRate;
    public float shooterProjectileSpeed;

    public void CopyFrom(EnemySpawner spawner)
    {
        enemiesPerWave = spawner.enemiesPerWave;
        spawnInterval = spawner.spawnInterval;
        startDelay = spawner.startDelay;
        spawnWaypointIndex = spawner.spawnWaypointIndex;
        waveSurvivalGold = spawner.waveSurvivalGold;

        enemyHealth = spawner.enemyHealth;
        enemySpeed = spawner.enemySpeed;
        enemyReward = spawner.enemyReward;
        damagePerEnemy = spawner.damagePerEnemy;

        healthGrowthPerWave = spawner.healthGrowthPerWave;
        speedGrowthPerWave = spawner.speedGrowthPerWave;
        shooterDamageGrowthPerWave = spawner.shooterDamageGrowthPerWave;
        enemiesGrowthPerWave = spawner.enemiesGrowthPerWave;
        shooterChanceGrowthPerWave = spawner.shooterChanceGrowthPerWave;

        shooterChance = spawner.shooterChance;
        shooterRange = spawner.shooterRange;
        shooterDamage = spawner.shooterDamage;
        shooterAttackRate = spawner.shooterAttackRate;
        shooterProjectileSpeed = spawner.shooterProjectileSpeed;
    }

    public void ApplyTo(EnemySpawner spawner)
    {
        spawner.enemiesPerWave = enemiesPerWave;
        spawner.spawnInterval = spawnInterval;
        spawner.startDelay = startDelay;
        spawner.spawnWaypointIndex = spawnWaypointIndex;
        spawner.waveSurvivalGold = waveSurvivalGold;

        spawner.enemyHealth = enemyHealth;
        spawner.enemySpeed = enemySpeed;
        spawner.enemyReward = enemyReward;
        spawner.damagePerEnemy = damagePerEnemy;

        spawner.healthGrowthPerWave = healthGrowthPerWave;
        spawner.speedGrowthPerWave = speedGrowthPerWave;
        spawner.shooterDamageGrowthPerWave = shooterDamageGrowthPerWave;
        spawner.enemiesGrowthPerWave = enemiesGrowthPerWave;
        spawner.shooterChanceGrowthPerWave = shooterChanceGrowthPerWave;

        spawner.shooterChance = shooterChance;
        spawner.shooterRange = shooterRange;
        spawner.shooterDamage = shooterDamage;
        spawner.shooterAttackRate = shooterAttackRate;
        spawner.shooterProjectileSpeed = shooterProjectileSpeed;
    }
}

// Escucha la salida de Play Mode aunque la ventana de debug este cerrada.
[InitializeOnLoad]
public static class SpawnerTuningStore
{
    private const string PendingKey = "TowerDefense.SpawnerTuning.Pending";
    private const string SceneKey = "TowerDefense.SpawnerTuning.Scene";

    static SpawnerTuningStore()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    public static bool HasPending
    {
        get { return !string.IsNullOrEmpty(EditorPrefs.GetString(PendingKey, string.Empty)); }
    }

    // Apunta los valores actuales para volcarlos al salir de Play Mode.
    // Se guarda tambien de que escena venian: aplicarlos en otra escena
    // seria pisarle el balance a un nivel que nadie estaba tocando.
    public static void SavePending(EnemySpawner spawner)
    {
        if (spawner == null)
            return;

        SpawnerTuning tuning = new SpawnerTuning();
        tuning.CopyFrom(spawner);

        EditorPrefs.SetString(PendingKey, JsonUtility.ToJson(tuning));
        EditorPrefs.SetString(SceneKey, spawner.gameObject.scene.path);
    }

    public static void DiscardPending()
    {
        EditorPrefs.DeleteKey(PendingKey);
        EditorPrefs.DeleteKey(SceneKey);
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
            return;

        string json = EditorPrefs.GetString(PendingKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return;

        string scenePath = EditorPrefs.GetString(SceneKey, string.Empty);
        DiscardPending();

        EnemySpawner spawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
        if (spawner == null)
            return;

        if (!string.IsNullOrEmpty(scenePath) && spawner.gameObject.scene.path != scenePath)
        {
            Debug.LogWarning(
                "Game Debug: los valores guardados eran de \"" + scenePath +
                "\" y ahora hay otra escena abierta. No se aplicaron.");
            return;
        }

        SpawnerTuning tuning = JsonUtility.FromJson<SpawnerTuning>(json);
        Undo.RecordObject(spawner, "Aplicar balance de Play Mode");
        tuning.ApplyTo(spawner);

        EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);

        Debug.Log("Game Debug: se aplicaron a la escena los valores que tocaste en Play Mode. " +
            "Guarda la escena (Ctrl+S) para dejarlos fijos.", spawner);
    }
}
