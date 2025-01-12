using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Realm
{
    readonly Queue<Vector2Int> needGen = new Queue<Vector2Int>();
    Dictionary<Vector2Int, Chunk> LoadedChunks = new Dictionary<Vector2Int, Chunk>();
}
