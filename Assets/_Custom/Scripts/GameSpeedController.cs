using UnityEngine;

// Punto unico para cambiar la velocidad general del juego (Time.timeScale).
// Todo lo que ya usa Time.deltaTime (movimiento de enemigos, cooldown de
// las torres, animaciones) se acelera solo con esto, sin tocar cada
// sistema por separado.
// Hoy la usa la ventana Tower Defense > Game Debug para probar x2/x3; esta
// lista para que un boton real del HUD (todavia no agregado) llame
// SetSpeed(3f) el dia que se quiera ese modo "oficial".
public static class GameSpeedController
{
    public const float DefaultSpeed = 1f;

    // Ultima velocidad "de juego" pedida, sin contar la pausa (que congela
    // con Time.timeScale = 0 pero no cuenta como una velocidad nueva).
    // GameHUDController.TogglePause usa esto para volver a la velocidad que
    // habia antes de pausar, en vez de resetear siempre a 1x.
    public static float CurrentSpeed { get; private set; } = DefaultSpeed;

    // Cambia la velocidad del juego. 1 = normal, 2 = doble, etc.
    public static void SetSpeed(float multiplier)
    {
        CurrentSpeed = Mathf.Max(0.01f, multiplier);
        Time.timeScale = CurrentSpeed;
    }

    // Vuelve a aplicar la velocidad guardada sin cambiarla. La usa quien
    // saca al juego de una pausa (Time.timeScale = 0), para no perder la
    // velocidad que estaba activa antes de pausar.
    public static void ApplyCurrentSpeed()
    {
        Time.timeScale = CurrentSpeed;
    }
}
