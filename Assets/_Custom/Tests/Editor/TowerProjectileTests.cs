using NUnit.Framework;
using UnityEngine;

// Pruebas del proyectil de las torres: vuela hacia el enemigo,
// le hace dano al llegar y desaparece si el objetivo ya no existe.
public class TowerProjectileTests
{
    private GameObject _projectileObject;
    private GameObject _enemyObject;

    [TearDown]
    public void Limpiar()
    {
        if (_projectileObject != null)
            Object.DestroyImmediate(_projectileObject);
        if (_enemyObject != null)
            Object.DestroyImmediate(_enemyObject);
    }

    // El proyectil se acerca al objetivo y le hace dano al pegar.
    [Test]
    public void VuelaHaciaElObjetivo_YLeHaceDano()
    {
        EnemyHealth enemy = CrearEnemigo(new Vector3(5f, 0f, 0f), 50f);
        TowerProjectile projectile = CrearProyectil(Vector3.zero);

        projectile.Setup(enemy.transform, 5f, null, null);

        // Se avanza el vuelo por pasos hasta que el proyectil desaparezca.
        int pasos = 0;
        while (projectile != null && pasos < 300)
        {
            projectile.MoveStep(0.1f);
            pasos++;
        }

        // Un objeto destruido de Unity se compara con ==, no con IsNull.
        Assert.IsTrue(projectile == null, "El proyectil deberia haberse destruido al pegar");
        Assert.AreEqual(45f, enemy.CurrentHealth, 0.01f);
    }

    // Si el objetivo muere antes de que llegue, el proyectil desaparece sin dano.
    [Test]
    public void ObjetivoMuerto_ElProyectilDesapareceSinDano()
    {
        EnemyHealth enemy = CrearEnemigo(new Vector3(3f, 0f, 0f), 5f);
        TowerProjectile projectile = CrearProyectil(Vector3.zero);

        projectile.Setup(enemy.transform, 5f, null, null);

        // El enemigo muere antes de que el proyectil llegue.
        enemy.TakeDamage(5f);
        projectile.MoveStep(0.1f);

        Assert.IsTrue(projectile == null, "El proyectil deberia desaparecer sin objetivo vivo");
    }

    // Un enemigo lejano alcanza a recibir un solo golpe.
    [Test]
    public void AlcanzaAlObjetivo_AunqueEsteLejos()
    {
        EnemyHealth enemy = CrearEnemigo(new Vector3(10f, 0f, 0f), 50f);
        TowerProjectile projectile = CrearProyectil(Vector3.zero);
        projectile.speed = 30f;

        projectile.Setup(enemy.transform, 5f, null, null);

        int pasos = 0;
        while (projectile != null && pasos < 300)
        {
            projectile.MoveStep(0.1f);
            pasos++;
        }

        Assert.IsTrue(projectile == null);
        Assert.AreEqual(45f, enemy.CurrentHealth, 0.01f);
    }

    private TowerProjectile CrearProyectil(Vector3 posicion)
    {
        _projectileObject = new GameObject("Projectile Test");
        _projectileObject.transform.position = posicion;
        return _projectileObject.AddComponent<TowerProjectile>();
    }

    private EnemyHealth CrearEnemigo(Vector3 posicion, float vida)
    {
        _enemyObject = new GameObject("Enemy Test");
        _enemyObject.transform.position = posicion;
        EnemyHealth health = _enemyObject.AddComponent<EnemyHealth>();
        health.Setup(vida, 10, null);
        return health;
    }
}
