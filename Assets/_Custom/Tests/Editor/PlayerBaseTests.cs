using NUnit.Framework;
using UnityEngine;

// Pruebas de las vidas del jugador cuando se le escapan enemigos.
public class PlayerBaseTests
{
    private GameObject _object;

    [TearDown]
    public void Limpiar()
    {
        if (_object != null)
            Object.DestroyImmediate(_object);
    }

    // Cada enemigo que llega al final quita vidas.
    [Test]
    public void TakeDamage_QuitaVidas()
    {
        PlayerBase player = CrearJugador(20);

        player.TakeDamage(1);

        Assert.AreEqual(19, player.Lives);
        Assert.IsTrue(player.IsAlive);
    }

    // Al quedarse sin vidas se avisa una sola vez.
    [Test]
    public void TakeDamage_SinVidas_AvisaLaDerrota()
    {
        PlayerBase player = CrearJugador(2);
        int derrotas = 0;
        player.Defeated += () => derrotas++;

        player.TakeDamage(1);
        player.TakeDamage(1);
        player.TakeDamage(1);

        Assert.AreEqual(0, player.Lives);
        Assert.IsFalse(player.IsAlive);
        Assert.AreEqual(1, derrotas);
    }

    // Las vidas nunca quedan en negativo.
    [Test]
    public void TakeDamage_GolpeGrande_NoBajaDeCero()
    {
        PlayerBase player = CrearJugador(3);

        player.TakeDamage(10);

        Assert.AreEqual(0, player.Lives);
    }

    // Reiniciar la partida vuelve a llenar las vidas.
    [Test]
    public void ResetLives_VuelveALlenarLasVidas()
    {
        PlayerBase player = CrearJugador(5);
        player.TakeDamage(5);

        player.ResetLives();

        Assert.AreEqual(5, player.Lives);
    }

    private PlayerBase CrearJugador(int vidas)
    {
        _object = new GameObject("Player Test");
        PlayerBase player = _object.AddComponent<PlayerBase>();
        player.maxLives = vidas;
        return player;
    }
}
