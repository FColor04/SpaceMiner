using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;

namespace SpaceMiner.Utils;

public struct NetworkPlayerInput
{
    private byte _state;

    [Flags]
    enum @Flags : byte
    {
        Null,
        HorizontalInput = 1,
        HorizontalPositive = 2,
        VerticalInput = 4,
        VerticalPositive = 8
    }

    public NetworkPlayerInput()
    {
        _state = 0;
    }
    
    public NetworkPlayerInput(byte state)
    {
        _state = state;
    }

    public NetworkPlayerInput(bool up, bool down, bool left, bool right)
    {
        Flags output = Flags.Null;
        if (down)
            output |= Flags.VerticalInput;
        if (up)
            output |= Flags.VerticalInput | Flags.VerticalPositive;
        if (left)
            output |= Flags.HorizontalInput;
        if (right)
            output |= Flags.HorizontalInput | Flags.HorizontalPositive;
        _state = (byte) output;
    }

    public byte GetByte => _state;

    [Pure]
    public Vector2 GetMovementVector()
    {
        var playerInput = (Flags) _state;
        return new Vector2(
            playerInput.HasFlag(Flags.HorizontalInput) ? (playerInput.HasFlag(Flags.HorizontalPositive) ? 1 : -1) : 0,
            playerInput.HasFlag(Flags.VerticalInput) ? (playerInput.HasFlag(Flags.VerticalPositive) ? -1 : 1) : 0
        ).GetNormalized();
    }
}