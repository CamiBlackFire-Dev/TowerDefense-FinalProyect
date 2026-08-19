using NUnit.Framework;
using UnityEngine;

// Pruebas de GameSpeedController. Time.timeScale es un valor global del
// motor, asi que cada prueba lo deja en 1x al terminar para no afectar a
// las demas.
public class GameSpeedControllerTests
{
    [TearDown]
    public void Limpiar()
    {
        GameSpeedController.SetSpeed(GameSpeedController.DefaultSpeed);
    }

    [Test]
    public void SetSpeed_CambiaTimeScaleYCurrentSpeed()
    {
        GameSpeedController.SetSpeed(2f);

        Assert.AreEqual(2f, GameSpeedController.CurrentSpeed, 0.001f);
        Assert.AreEqual(2f, Time.timeScale, 0.001f);
    }

    [Test]
    public void SetSpeed_NoDejaValoresEnCeroONegativos()
    {
        GameSpeedController.SetSpeed(-5f);

        Assert.Greater(GameSpeedController.CurrentSpeed, 0f);
        Assert.Greater(Time.timeScale, 0f);
    }

    // ApplyCurrentSpeed vuelve a poner la ultima velocidad guardada sin
    // cambiarla; lo usa quien saca al juego de una pausa (que congela con
    // Time.timeScale = 0 sin pasar por SetSpeed).
    [Test]
    public void ApplyCurrentSpeed_RestauraLaVelocidadGuardadaDespuesDeCongelar()
    {
        GameSpeedController.SetSpeed(3f);
        Time.timeScale = 0f;

        GameSpeedController.ApplyCurrentSpeed();

        Assert.AreEqual(3f, Time.timeScale, 0.001f);
        Assert.AreEqual(3f, GameSpeedController.CurrentSpeed, 0.001f);
    }
}
