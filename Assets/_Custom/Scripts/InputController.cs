using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Lee el teclado y envia comandos de movimiento sin conocer BoardManager.
public class InputController : MonoBehaviour
{
    #region Events
    public event Action<GridDirection> MoveRequested;
    public event Action BuyRequested;
    // Cambiar de tablero, en los niveles que tienen mas de uno (Tab).
    public event Action NextBoardRequested;
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        if (!Application.isPlaying || Keyboard.current == null)
            return;

        if (IsPressed(Keyboard.current.upArrowKey, Keyboard.current.wKey))
        {
            SendMove(GridDirection.Up);
            return;
        }

        if (IsPressed(Keyboard.current.downArrowKey, Keyboard.current.sKey))
        {
            SendMove(GridDirection.Down);
            return;
        }

        if (IsPressed(Keyboard.current.leftArrowKey, Keyboard.current.aKey))
        {
            SendMove(GridDirection.Left);
            return;
        }

        if (IsPressed(Keyboard.current.rightArrowKey, Keyboard.current.dKey))
        {
            SendMove(GridDirection.Right);
            return;
        }

        // Tecla temporal para comprar torres hasta que exista la interfaz.
        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            if (BuyRequested != null)
                BuyRequested();
        }

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (NextBoardRequested != null)
                NextBoardRequested();
        }
    }
    #endregion

    #region Public API
    // Permite probar la correspondencia entre teclas y direcciones sin hardware.
    public static bool TryGetDirection(Key key, out GridDirection direction)
    {
        switch (key)
        {
            case Key.UpArrow:
            case Key.W:
                direction = GridDirection.Up;
                return true;
            case Key.DownArrow:
            case Key.S:
                direction = GridDirection.Down;
                return true;
            case Key.LeftArrow:
            case Key.A:
                direction = GridDirection.Left;
                return true;
            case Key.RightArrow:
            case Key.D:
                direction = GridDirection.Right;
                return true;
            default:
                direction = GridDirection.Left;
                return false;
        }
    }

    // Indica si una tecla corresponde a comprar una torre.
    public static bool IsBuyKey(Key key)
    {
        return key == Key.B;
    }
    #endregion

    #region Internal Helpers
    private bool IsPressed(KeyControl primary, KeyControl alternative)
    {
        return primary.wasPressedThisFrame || alternative.wasPressedThisFrame;
    }

    private void SendMove(GridDirection direction)
    {
        if (MoveRequested != null)
            MoveRequested(direction);
    }
    #endregion
}
