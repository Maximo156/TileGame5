using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISpriteful
{
    Sprite Sprite {
        get;
    }

    Color Color
    {
        get => Color.white;
    }
}

public interface ISaveable
{
    public string Identifier { get; }
}
