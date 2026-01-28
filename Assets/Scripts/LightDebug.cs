using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class LightDebug : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || (!LightCalculation.PreviousChunks.IsCreated)) return;

        //DrawLightBordersGizmos(LightCalculation.PreviousChunks, LightCalculation.PreviousBorder, WorldSettings.ChunkWidth);
        DrawReceivedBorderLightGizmos(LightCalculation.PreviousChunks, LightCalculation.PreviousBorder, WorldSettings.ChunkWidth);
    }

    private void OnDisable()
    {
        if(!LightCalculation.PreviousChunks.IsCreated) return;

        LightCalculation.PreviousChunks.Dispose();
        LightCalculation.PreviousBorder.Dispose();
    }

    public static void DrawLightBordersGizmos(
    NativeArray<int2> chunks,
    NativeArray<byte> borderLight,
    int chunkWidth,
    float cellSize = 1f
)
    {
        Gizmos.color = Color.yellow;

        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            int2 chunkPos = chunks[chunkIndex];
            int baseOffset = chunkIndex * 4 * chunkWidth;

            int worldChunkX = chunkPos.x * chunkWidth;
            int worldChunkY = chunkPos.y * chunkWidth;

            // ---- Left edge (x = 0)
            for (int i = 0; i < chunkWidth; i++)
            {
                byte light = borderLight[baseOffset + 0 * chunkWidth + i];
                if (light == 0) continue;

                Vector3 pos = new(
                    (worldChunkX + 0 + 0.5f) * cellSize,
                    (worldChunkY + i + 0.5f) * cellSize,
                    0f
                );
                Gizmos.DrawWireCube(pos, Vector3.one * cellSize);
            }

            // ---- Right edge (x = chunkWidth - 1)
            for (int i = 0; i < chunkWidth; i++)
            {
                byte light = borderLight[baseOffset + 1 * chunkWidth + i];
                if (light == 0) continue;

                Vector3 pos = new(
                    (worldChunkX + chunkWidth - 1 + 0.5f) * cellSize,
                    (worldChunkY + i + 0.5f) * cellSize,
                    0f
                );

                Gizmos.DrawWireCube(pos, Vector3.one * cellSize);
            }

            // ---- Bottom edge (y = 0)
            for (int i = 0; i < chunkWidth; i++)
            {
                byte light = borderLight[baseOffset + 2 * chunkWidth + i];
                if (light == 0) continue;

                Vector3 pos = new(
                    (worldChunkX + i + 0.5f) * cellSize,
                    (worldChunkY + 0 + 0.5f) * cellSize,
                    0f
                );

                Gizmos.DrawWireCube(pos, Vector3.one * cellSize);
            }

            // ---- Top edge (y = chunkWidth - 1)
            for (int i = 0; i < chunkWidth; i++)
            {
                byte light = borderLight[baseOffset + 3 * chunkWidth + i];
                if (light == 0) continue;

                Vector3 pos = new(
                    (worldChunkX + i + 0.5f) * cellSize,
                    (worldChunkY + chunkWidth - 1 + 0.5f) * cellSize,
                    0f
                );

                Gizmos.DrawWireCube(pos, Vector3.one * cellSize);
            }
        }
    }

    public static void DrawReceivedBorderLightGizmos(
    NativeArray<int2> chunks,
    NativeArray<byte> borderLight,
    int chunkWidth,
    float cellSize = 1f
)
    {
        Gizmos.color = Color.cyan;

        for (int index = 0; index < chunks.Length; index++)
        {
            int2 chunkPos = chunks[index];

            int worldChunkX = chunkPos.x * chunkWidth;
            int worldChunkY = chunkPos.y * chunkWidth;

            int lIndex = FindChunkIndex(chunks, chunkPos + new int2(-1, 0));
            int rIndex = FindChunkIndex(chunks, chunkPos + new int2(1, 0));
            int dIndex = FindChunkIndex(chunks, chunkPos + new int2(0, -1));
            int uIndex = FindChunkIndex(chunks, chunkPos + new int2(0, 1));

            // ---- Receiving from LEFT neighbor (x < 0)
            if (lIndex != -1)
            {
                int baseOffset = lIndex * 4 * chunkWidth;
                for (int y = 0; y < chunkWidth; y++)
                {
                    byte light = borderLight[baseOffset + 1 * chunkWidth + y]; // right edge of left neighbor
                    if (light == 0) continue;

                    Vector3 pos = new(
                        (worldChunkX - 1 + 0.5f) * cellSize,
                        (worldChunkY + y + 0.5f) * cellSize,
                        0f
                    );

                    Gizmos.DrawWireCube(pos, Vector3.one * cellSize);
                }
            }

            // ---- Receiving from RIGHT neighbor (x >= chunkWidth)
            if (rIndex != -1)
            {
                int baseOffset = rIndex * 4 * chunkWidth;
                for (int y = 0; y < chunkWidth; y++)
                {
                    byte light = borderLight[baseOffset + 0 * chunkWidth + y]; // left edge of right neighbor
                    if (light == 0) continue;

                    Vector3 pos = new(
                        (worldChunkX + chunkWidth + 0.5f) * cellSize,
                        (worldChunkY + y + 0.5f) * cellSize,
                        0f
                    );

                    Gizmos.DrawWireCube(pos, Vector3.one * cellSize);
                }
            }

            // ---- Receiving from BOTTOM neighbor (y < 0)
            if (dIndex != -1)
            {
                int baseOffset = dIndex * 4 * chunkWidth;
                for (int x = 0; x < chunkWidth; x++)
                {
                    byte light = borderLight[baseOffset + 3 * chunkWidth + x]; // top edge of bottom neighbor
                    if (light == 0) continue;

                    Vector3 pos = new(
                        (worldChunkX + x + 0.5f) * cellSize,
                        (worldChunkY - 1 + 0.5f) * cellSize,
                        0f
                    );

                    Gizmos.DrawWireCube(pos, Vector3.one * cellSize);
                }
            }

            // ---- Receiving from TOP neighbor (y >= chunkWidth)
            if (uIndex != -1)
            {
                int baseOffset = uIndex * 4 * chunkWidth;
                for (int x = 0; x < chunkWidth; x++)
                {
                    byte light = borderLight[baseOffset + 2 * chunkWidth + x]; // bottom edge of top neighbor
                    if (light == 0) continue;

                    Vector3 pos = new(
                        (worldChunkX + x + 0.5f) * cellSize,
                        (worldChunkY + chunkWidth + 0.5f) * cellSize,
                        0f
                    );

                    Gizmos.DrawWireCube(pos, Vector3.one * cellSize);
                }
            }
        }
    }

    static int FindChunkIndex(NativeArray<int2> chunks, int2 chunk)
    {
        for (int i = chunks.Length - 1; i >= 0; i--)
            if (chunks[i].Equals(chunk))
                return i;
        return -1;
    }
}
