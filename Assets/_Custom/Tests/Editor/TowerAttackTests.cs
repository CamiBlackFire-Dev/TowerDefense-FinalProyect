using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Pruebas de que las torres ven a los enemigos y les hacen dano de verdad.
// La deteccion usa fisica real (Physics.OverlapSphere), igual que en el juego.
public class TowerAttackTests
{
    private GameObject _towerObject;
    private GameObject _enemyObject;
    private TowerCatalog _catalog;
    private TowerData _level1;
    private TowerData _level2;

    [TearDown]
    public void Limpiar()
    {
        if (_towerObject != null)
            Object.DestroyImmediate(_towerObject);
        if (_enemyObject != null)
            Object.DestroyImmediate(_enemyObject);
        if (_catalog != null)
            Object.DestroyImmediate(_catalog);
        if (_level1 != null)
            Object.DestroyImmediate(_level1);
        if (_level2 != null)
            Object.DestroyImmediate(_level2);
    }

    // La torre encuentra al enemigo que tiene cerca.
    [Test]
    public void Detector_EncuentraAlEnemigoCercano()
    {
        TowerAttack attack = CrearTorre(1);
        EnemyHealth enemy = CrearEnemigo(new Vector3(2f, 0f, 0f), 50f);

        Transform objetivo = BuscarObjetivo(attack, 10f);

        Assert.IsNotNull(objetivo, "La torre no detecto al enemigo que tenia al lado");
        Assert.AreEqual(enemy.gameObject, objetivo.gameObject);
    }

    // Un enemigo fuera del alcance no se detecta.
    [Test]
    public void Detector_IgnoraAlEnemigoLejano()
    {
        TowerAttack attack = CrearTorre(1);
        CrearEnemigo(new Vector3(30f, 0f, 0f), 50f);

        Transform objetivo = BuscarObjetivo(attack, 10f);

        Assert.IsNull(objetivo);
    }

    // El alcance del nivel decide a quien se le puede disparar.
    [Test]
    public void IsInRange_RespetaElAlcanceDelNivel()
    {
        TowerAttack attack = CrearTorre(1); // alcance 4
        EnemyHealth cerca = CrearEnemigo(new Vector3(3f, 0f, 0f), 50f);
        attack.RefreshStats();

        Assert.IsTrue(attack.IsInRange(cerca.transform));

        cerca.transform.position = new Vector3(9f, 0f, 0f);
        Assert.IsFalse(attack.IsInRange(cerca.transform));
    }

    // Disparar le quita vida al enemigo.
    [Test]
    public void Shoot_LeQuitaVidaAlEnemigo()
    {
        TowerAttack attack = CrearTorre(1); // dano 5
        EnemyHealth enemy = CrearEnemigo(new Vector3(2f, 0f, 0f), 50f);
        attack.RefreshStats();

        attack.Shoot(enemy.transform);

        Assert.AreEqual(45f, enemy.CurrentHealth, 0.01f);
    }

    // Con suficientes disparos el enemigo muere y desaparece.
    [Test]
    public void Shoot_ConSuficientesDisparos_MataAlEnemigo()
    {
        TowerAttack attack = CrearTorre(1); // dano 5
        EnemyHealth enemy = CrearEnemigo(new Vector3(2f, 0f, 0f), 10f);
        attack.RefreshStats();

        attack.Shoot(enemy.transform);
        attack.Shoot(enemy.transform);

        Assert.IsTrue(enemy == null, "El enemigo deberia haber muerto");
    }

    // Una torre de nivel 2 pega mas fuerte y llega mas lejos.
    [Test]
    public void RefreshStats_Nivel2_UsaLosDatosDelCatalogo()
    {
        TowerAttack attack = CrearTorre(2);

        attack.RefreshStats();

        Assert.AreEqual(12f, attack.damage, 0.01f);
        Assert.AreEqual(5f, attack.range, 0.01f);
    }

    // Corre la deteccion de TowerTargetDetector (es privada) y devuelve el objetivo.
    private Transform BuscarObjetivo(TowerAttack attack, float alcance)
    {
        TowerTargetDetector detector = attack.GetComponent<TowerTargetDetector>();
        detector.SetEnemyLayer(LayerMask.GetMask("Enemy"));
        detector.SetDetectionRange(alcance);

        // En el editor la fisica no se actualiza sola al mover objetos.
        Physics.SyncTransforms();

        MethodInfo buscar = typeof(TowerTargetDetector).GetMethod("FindClosestEnemy",
            BindingFlags.Instance | BindingFlags.NonPublic);
        buscar.Invoke(detector, null);

        return detector.CurrentTarget;
    }

    private TowerAttack CrearTorre(int nivel)
    {
        _towerObject = new GameObject("Tower Test");
        _towerObject.transform.position = Vector3.zero;

        Tower tower = _towerObject.AddComponent<Tower>();
        tower.SetLevel(nivel);

        TowerAttack attack = _towerObject.AddComponent<TowerAttack>();
        attack.catalog = CrearCatalogo();
        return attack;
    }

    private EnemyHealth CrearEnemigo(Vector3 posicion, float vida)
    {
        _enemyObject = new GameObject("Enemy Test");
        _enemyObject.transform.position = posicion;

        int capa = LayerMask.NameToLayer("Enemy");
        if (capa >= 0)
            _enemyObject.layer = capa;

        SphereCollider collider = _enemyObject.AddComponent<SphereCollider>();
        collider.radius = 0.5f;

        EnemyHealth health = _enemyObject.AddComponent<EnemyHealth>();
        health.Setup(vida, 10, null);
        return health;
    }

    // Catalogo de prueba con los mismos numeros que los assets del juego.
    private TowerCatalog CrearCatalogo()
    {
        _level1 = ScriptableObject.CreateInstance<TowerData>();
        _level1.level = 1;
        _level1.damage = 5f;
        _level1.range = 4f;
        _level1.attackRate = 1f;

        _level2 = ScriptableObject.CreateInstance<TowerData>();
        _level2.level = 2;
        _level2.damage = 12f;
        _level2.range = 5f;
        _level2.attackRate = 1.2f;

        _catalog = ScriptableObject.CreateInstance<TowerCatalog>();
        _catalog.levels = new TowerData[] { _level1, _level2 };
        return _catalog;
    }
}
