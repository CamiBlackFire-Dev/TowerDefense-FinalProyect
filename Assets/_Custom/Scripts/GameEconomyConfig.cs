using UnityEngine;

// Los numeros de oro del juego, en un solo lugar y compartidos por todos
// los niveles. Antes cada escena tenia los suyos sueltos (EconomyManager,
// TowerShop, GameHUDController, EnemySpawner), y se fueron desincronizando
// solos: LevelPrueba y Level2 arrancaban con 150 de oro y Level3 con 200
// sin que eso fuera una decision de diseno, solo el resultado de haberlos
// tocado por separado.
//
// Quien lo use lo lee al arrancar y, si no hay config asignada, se queda
// con el valor que tenga puesto en la escena: asi nada se rompe si el
// asset falta.
[CreateAssetMenu(fileName = "GameEconomyConfig", menuName = "Tower Defense/Game Economy Config")]
public class GameEconomyConfig : ScriptableObject
{
    [Header("Arranque")]
    // Con cuanto empieza el jugador. En 200 alcanza para 4 torres, que es
    // lo minimo para cubrir un nivel de varias entradas como Level3.
    public int startingMoney = 200;

    [Header("Ingresos")]
    // Lo que paga cada enemigo al morir.
    public int enemyReward = 10;
    // Oro fijo al terminar una oleada, se maten o no todos los enemigos.
    // Es la red de seguridad: sin esto, quedarse sin torres deja la
    // partida sin forma de recuperarse.
    public int waveSurvivalGold = 50;

    [Header("Torres")]
    public int towerCost = 50;

    [Header("Ballesta")]
    public int ballistaCost = 100;
    // Desbloquear un tipo de flecha nuevo (fuego, hielo, veneno).
    public int arrowUnlockCost = 120;
    // Cambiar entre flechas YA desbloqueadas. Es una decision tactica de
    // cada oleada, asi que tiene que salir barato: si cuesta como una torre
    // el jugador simplemente no lo usa.
    public int arrowSwitchCost = 25;

    [Header("Powerups")]
    public int ability1Cost = 100;
    public int ability2Cost = 150;
    public int ability3Cost = 150;
    public int ability4Cost = 150;
    public int ability5Cost = 150;
}
