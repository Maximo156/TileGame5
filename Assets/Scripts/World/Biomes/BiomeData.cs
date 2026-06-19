using Unity.Collections;
using Unity.Jobs;

public struct BiomeData
{
    public NativeArray<float> HeightMap;

    public NativeArray<int> SelectedBiome;

    public BiomeData(int chunkCount, int chunkWidth)
    {
        var length = chunkCount * chunkWidth * chunkWidth;
        HeightMap = new NativeArray<float>(length, Allocator.Persistent);

        SelectedBiome = new NativeArray<int>(length, Allocator.Persistent);
    }

    public void Dispose()
    {
        HeightMap.Dispose();
        SelectedBiome.Dispose();
    }

    public JobHandle Dispose(JobHandle handle)
    {
        return JobHandle.CombineDependencies(
            HeightMap.Dispose(handle),
            SelectedBiome.Dispose(handle)
        );
    }
}