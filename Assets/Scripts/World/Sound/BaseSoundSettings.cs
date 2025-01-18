using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseSoundSettings : ScriptableObject
{
    public abstract float GetSound(int x, int y);

    public abstract float[,] GetSoundArray(int x, int y, int chunkWidth);
}
