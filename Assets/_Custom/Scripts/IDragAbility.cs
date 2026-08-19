using UnityEngine;

// Lo que necesita saber el HUD para manejar un powerup de arrastrar y
// soltar, sin importar cual sea (bomba, EMP, repulsion, reparacion...).
// Todos los powerups de arrastre ya tenian estos mismos metodos por su
// cuenta; la interfaz solo les pone un nombre comun para que
// GameHUDController los trate a todos igual en vez de tener un caso
// especial por cada uno.
public interface IDragAbility
{
    // Empieza el arrastre: aparece el modelo que sigue al dedo/mouse.
    void BeginDrag();

    // Se llama cada frame mientras se sostiene, con la posicion del
    // puntero en pixeles de pantalla.
    void UpdateDrag(Vector2 screenPosition);

    // Suelta el powerup donde este apuntando ahora mismo.
    void EndDrag();

    // Cancela sin usar nada (por ejemplo si el arrastre se interrumpe).
    void CancelDrag();

    // True si al soltar habia un punto valido bajo el puntero. El HUD lo
    // consulta despues de EndDrag para saber si cobrar el uso: soltar
    // fuera del mapa no gasta el powerup.
    bool HasValidTarget { get; }
}
