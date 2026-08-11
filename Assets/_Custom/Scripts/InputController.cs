using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

// Lee el teclado y envia comandos de movimiento sin conocer BoardManager.
public class InputController : MonoBehaviour
{
    public event Action<GridDirection> MoveRequested;
    public event Action BuyRequested;

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
    }

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

    private bool IsPressed(KeyControl primary, KeyControl alternative)
    {
        return primary.wasPressedThisFrame || alternative.wasPressedThisFrame;
    }

    private void SendMove(GridDirection direction)
    {
        if (MoveRequested != null)
            MoveRequested(direction);
    }
}
