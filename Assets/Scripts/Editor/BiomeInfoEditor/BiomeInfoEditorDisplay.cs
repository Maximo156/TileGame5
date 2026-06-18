using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class BiomeInfoEditorDisplay
{
    int chunksPerRow = 30;
    private readonly VisualElement biomeDisplay;
    private readonly VisualElement cellImage;
    private RealmBiomeInfo biomeInfo;

    private Texture2D biomeTexture;
    private Texture2D cellTexture;

    Action Repaint;

    Action OnDispose = delegate {};

    public BiomeInfoEditorDisplay(VisualElement display, RealmBiomeInfo info, Action repaint)
    {
        biomeInfo = info;
        Repaint = repaint;

        biomeDisplay = display.Q<VisualElement>("BiomeDisplay");
        cellImage = display.Q<VisualElement>("DistDisplay");

        var zoomIn = display.Q<Button>("ZoomIn");
        var zoomOut = display.Q<Button>("ZoomOut");
        var sample = display.Q<Button>("Sample");

        zoomIn.clicked += ZoomIn;
        zoomOut.clicked += ZoomOut;
        sample.clicked += RunSample;

        OnDispose += () =>
        {
            zoomIn.clicked -= ZoomIn;
            zoomOut.clicked -= ZoomOut;
            sample.clicked -= RunSample;
        };

        RenderDisplay();
        InitControls();
    }

    void RunSample()
    {
        int sampleCount = 100;
        var t = System.Diagnostics.Stopwatch.StartNew();
        for(int i = 0; i < sampleCount; i++)
        {
            const int chunkWidth = 32;

            NativeArray<int2> chunks = new NativeArray<int2>(chunksPerRow * chunksPerRow, Allocator.Persistent);
            int c = 0;
            for (int x = 0; x < chunksPerRow; x++)
            {
                for (int y = 0; y < chunksPerRow; y++)
                {
                    chunks[c] = math.int2(x, y);
                    c++;
                }
            }

            var biomedata = new BiomeData(chunks.Length, chunkWidth);

            biomeInfo.ScheduelBiomeInfoGen(chunkWidth, chunks, ref biomedata).Complete();

            biomedata.Dispose();
            chunks.Dispose();
        }
        Debug.Log($"Avg: {t.Elapsed / sampleCount}");
    }

    void RenderDisplay()
    {
        if (biomeTexture != null)
        {
            biomeDisplay.style.backgroundImage = StyleKeyword.None;
            UnityEngine.Object.DestroyImmediate(biomeTexture);
            biomeTexture = null;
        }

        const int chunkWidth = 32;

        NativeArray<int2> chunks = new NativeArray<int2>(chunksPerRow * chunksPerRow, Allocator.Persistent);
        int c = 0;
        for(int x = 0; x < chunksPerRow; x++)
        {
            for (int y = 0; y < chunksPerRow; y++)
            {
                chunks[c] = math.int2(x, y);
                c++;
            }
        }
        
        var biomedata = new BiomeData(chunks.Length, chunkWidth);

        biomeInfo.ScheduelBiomeInfoGen(chunkWidth, chunks, ref biomedata).Complete();
        Color32[] colors = new Color32[chunks.Length * chunkWidth * chunkWidth];

        ConvertBiomesToColor(chunkWidth, chunks, biomedata, colors, biomeInfo);

        biomeTexture = new Texture2D(chunksPerRow * chunkWidth, chunksPerRow * chunkWidth)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        biomeTexture.SetPixels32(colors);
        biomeTexture.Apply();

        biomeDisplay.style.backgroundImage = biomeTexture;
        biomeDisplay.MarkDirtyRepaint();
        biomedata.Dispose();
        chunks.Dispose();
    }

    void InitControls()
    {
        cellImage.RegisterCallback<MouseDownEvent>(OnBiomeDisplayDown);
        cellImage.RegisterCallback<MouseMoveEvent>(OnBiomeDisplayMove);
        cellImage.RegisterCallback<MouseUpEvent>(OnBiomeDisplayUp);
        cellImage.RegisterCallback<MouseOutEvent>(OnBiomeDisplayOut);

        cellImage.generateVisualContent += DrawDots;

        OnDispose += () =>
        {
            cellImage.UnregisterCallback<MouseDownEvent>(OnBiomeDisplayDown);
            cellImage.UnregisterCallback<MouseMoveEvent>(OnBiomeDisplayMove);
            cellImage.UnregisterCallback<MouseUpEvent>(OnBiomeDisplayUp);
            cellImage.UnregisterCallback<MouseOutEvent>(OnBiomeDisplayOut);

            cellImage.generateVisualContent -= DrawDots;
        };

        DrawBiomeControlls();
    }

    const int distScale = 256;
    void DrawBiomeControlls()
    {
        if (cellTexture != null)
        {
            cellImage.style.backgroundImage = StyleKeyword.None;
            UnityEngine.Object.DestroyImmediate(cellTexture);
            cellTexture = null;
        }

        Color32[] colors = new Color32[distScale * distScale];

        for (int moisture = 0; moisture < distScale; moisture++)
        {
            for (int heat = 0; heat < distScale; heat++)
            {
                colors[moisture * distScale + heat] = biomeInfo.GetBiome(1, moisture * 1f / distScale, heat * 1f / distScale).EditorColor;
            }
        }

        cellTexture = new Texture2D(distScale, distScale, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        cellTexture.SetPixels32(colors);
        cellTexture.Apply();

        cellImage.style.backgroundImage = cellTexture;
    }

    const float DOT_RADIUS = 6f;
    void DrawDots(MeshGenerationContext ctx)
    {
        var painter = ctx.painter2D;

        var imageRect = cellImage.contentRect;

        float width = imageRect.width;
        float height = imageRect.height;

        foreach (var biome in biomeInfo.Biomes)
        {
            float x = biome.targetHeat * width;
            float y = (1f - biome.targetMoisture) * height;

            var center = new Vector2(x, y);

            painter.lineWidth = 4f;
            painter.strokeColor = Color.black;
            painter.BeginPath();
            painter.Arc(center, 5f, 0, 360);
            painter.Stroke();

            painter.lineWidth = 2f;
            painter.strokeColor = Color.white;
            painter.BeginPath();
            painter.Arc(center, 5f, 0, 360);
            painter.Stroke();
        }
    }

    int draggedBiomeIndex = -1;
    void OnBiomeDisplayDown(MouseDownEvent e)
    {
        if (e.button != 0)
            return;

        Vector2 mousePos = e.localMousePosition;

        for (int i = 0; i < biomeInfo.Biomes.Count; i++)
        {
            var biome = biomeInfo.Biomes[i];

            Vector2 pos = BiomeToPosition(
                biome.targetMoisture,
                biome.targetHeat);

            Rect hitRect = new Rect(
                pos.x - DOT_RADIUS,
                pos.y - DOT_RADIUS,
                DOT_RADIUS * 2,
                DOT_RADIUS * 2);

            if (hitRect.Contains(mousePos))
            {
                draggedBiomeIndex = i;
                e.StopPropagation();
                return;
            }
        }
    }

    void OnBiomeDisplayMove(MouseMoveEvent e)
    {
        if (draggedBiomeIndex < 0)
            return;

        Rect imageRect = cellImage.contentRect;

        Vector2 mouse = e.localMousePosition;

        // clamp inside actual visible image (not full UI rect)
        mouse.x = Mathf.Clamp(mouse.x, imageRect.x, imageRect.xMax);
        mouse.y = Mathf.Clamp(mouse.y, imageRect.y, imageRect.yMax);

        float heat = Mathf.InverseLerp(imageRect.x, imageRect.xMax, mouse.x);
        float moisture = 1f - Mathf.InverseLerp(imageRect.y, imageRect.yMax, mouse.y);

        var biome = biomeInfo.Biomes[draggedBiomeIndex];
        biome.targetHeat = heat;
        biome.targetMoisture = moisture;
        biomeInfo.Biomes[draggedBiomeIndex] = biome;

        Vector2 dotPos = new Vector2(
            imageRect.x + heat * imageRect.width,
            imageRect.y + (1f - moisture) * imageRect.height
        );

        DrawBiomeControlls();
        cellImage.MarkDirtyRepaint();
        Repaint();
        e.StopPropagation();
    }

    void OnBiomeDisplayUp(MouseUpEvent e)
    {
        EndMove();
        e.StopPropagation();
    }

    void OnBiomeDisplayOut(MouseOutEvent e)
    {
        EndMove();
        e.StopPropagation();
    }

    void EndMove()
    {
        biomeInfo.Dispose();
        RenderDisplay();
        Repaint();
        draggedBiomeIndex = -1;
    }

    Vector2 BiomeToPosition(float moisture, float heat)
    {
        return new Vector2(
            heat * distScale,
            (1f - moisture) * distScale
        );
    }

    public void OnDisable()
    {
        OnDispose?.Invoke();
        biomeInfo.Dispose();
    }

    void ConvertBiomesToColor(
     int chunkWidth,
     NativeArray<int2> chunks,
     BiomeData biomeData,
     Color32[] colors,
     RealmBiomeInfo biomeInfo)
    {
        int chunkSize = chunkWidth * chunkWidth;

        int2 min = chunks[0];
        int2 max = chunks[0];

        for (int i = 1; i < chunks.Length; i++)
        {
            min = math.min(min, chunks[i]);
            max = math.max(max, chunks[i]);
        }

        for (int chunkId = 0; chunkId < chunks.Length; chunkId++)
        {
            int2 normalized = chunks[chunkId] - min;

            int chunkX = normalized.y;
            int chunkY = normalized.x;

            int chunkOffsetX = chunkX * chunkWidth;
            int chunkOffsetY = chunkY * chunkWidth;

            for (int localIndex = 0; localIndex < chunkSize; localIndex++)
            {
                int globalIndex = chunkId * chunkSize + localIndex;

                int localX = localIndex % chunkWidth;
                int localY = localIndex / chunkWidth;

                int biomeIndex = biomeData.SelectedBiome[globalIndex];
                float height = biomeData.HeightMap[globalIndex];

                int x = chunkOffsetX + localX;
                int y = chunkOffsetY + localY;

                int textureIndex = x * (chunkWidth * chunksPerRow) + y;

                if (biomeInfo.TryGetBiome(height, biomeIndex, out var biomePreset))
                {
                    colors[textureIndex] = biomePreset.EditorColor;
                }
                else
                {
                    colors[textureIndex] = Color.blue;
                }
            }
        }
    }

    void ZoomIn()
    {
        chunksPerRow = Mathf.Max(chunksPerRow - 1, 1);
        RenderDisplay();
        Repaint();  
    }

    void ZoomOut()
    {
        chunksPerRow++;
        RenderDisplay();
        Repaint();
    }
}
