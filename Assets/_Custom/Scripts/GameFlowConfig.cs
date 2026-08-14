using UnityEngine;

// Que escena es cada cosa en el flujo del juego. MainMenuController lee
// gameplayScene para saber a donde entrar al pulsar Jugar, sin tener el
// nombre de la escena escrito a mano en el codigo.
// Se crea desde Assets > Create > Tower Defense > Game Flow Config, y se
// edita mas comodo desde la ventana Tower Defense > Escenas.
[CreateAssetMenu(fileName = "GameFlowConfig", menuName = "Tower Defense/Game Flow Config")]
public class GameFlowConfig : ScriptableObject
{
    [Tooltip("Escena del menu principal.")]
    public string mainMenuScene = "Main Menu";

    [Tooltip("Escena que carga el boton Jugar del menu principal.")]
    public string gameplayScene = "LevelPrueba";
}
