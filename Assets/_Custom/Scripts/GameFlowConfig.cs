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

    [Tooltip("Escena que carga el boton Jugar del menu principal. Se usa " +
        "como respaldo del NIVEL 1 si la lista de abajo esta vacia.")]
    public string gameplayScene = "LevelPrueba";

    [Tooltip("Escenas de los niveles en orden: la primera es NIVEL 1, la " +
        "segunda NIVEL 2, etc. Todas tienen que estar en Build Settings.")]
    public string[] levelScenes = { "LevelPrueba", "Level2", "Level3" };

    // Escena del nivel numero N (1 = el primero). Cadena vacia si ese
    // nivel todavia no existe en la lista.
    public string GetLevelScene(int levelNumber)
    {
        if (levelScenes == null || levelNumber < 1 || levelNumber > levelScenes.Length)
            return string.Empty;

        return levelScenes[levelNumber - 1];
    }

    // Que numero de nivel es una escena (1, 2, 3...). Devuelve 0 si esa
    // escena no esta en la lista: lo usa la pantalla de victoria para
    // saber que nivel acaba de superarse.
    public int GetLevelNumber(string sceneName)
    {
        if (levelScenes == null || string.IsNullOrEmpty(sceneName))
            return 0;

        for (int i = 0; i < levelScenes.Length; i++)
            if (levelScenes[i] == sceneName)
                return i + 1;

        return 0;
    }

    public int LevelCount
    {
        get { return levelScenes != null ? levelScenes.Length : 0; }
    }
}
