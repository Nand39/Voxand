namespace Sparkvox.src.Features.Voxels.Editing;
public class VoxelToolHotbar
{
    public const int MAX_SLOTS = 10;
    int[] techniqueSlots = new int[MAX_SLOTS];

    public int this[int index]
    {
        get => techniqueSlots[index];
        set => techniqueSlots[index] = value;
    }

    public VoxelToolHotbar()
    {
        for (int i = 0; i < MAX_SLOTS; i++)
            techniqueSlots[i] = -1;
    }
}