namespace Voxand.Engine.Systems.Structures;
public class ChunkedStack<T>
{
    LinkedList<T[]> linkedList;
    int numChunksAllocated;
    LinkedListNode<T[]> currentChunk;
    int currentChunkIndex;
    int indexOfNext = 0;

    public int ChunkCapacity { get; set; }
    public int Count { get; private set; } = 0;
    public bool Empty => Count == 0;

    public ChunkedStack(int chunkCapacity)
    {
        this.ChunkCapacity = chunkCapacity;
        T[] chunk = new T[chunkCapacity];
        currentChunk = new(chunk);
        linkedList = [];
        linkedList.AddLast(currentChunk);
        numChunksAllocated = 1;
        currentChunkIndex = 0;
    }

    public void Push(T item)
    {
        if (indexOfNext == ChunkCapacity)
        {
            currentChunkIndex++;
            if (currentChunkIndex == numChunksAllocated)
            {
                T[] newChunkData = new T[ChunkCapacity];
                newChunkData[0] = item;
                currentChunk = new(newChunkData);
                linkedList.AddLast(currentChunk);
                numChunksAllocated++;
            }
            else
            {
                currentChunk = currentChunk.Next!;
                currentChunk.Value[0] = item;
            }
            indexOfNext = 1;
        }
        else
        {
            currentChunk.Value[indexOfNext] = item;
            indexOfNext++;
        }
        Count++;
    }

    public T Pop()
    {
        if (Count == 0)
            throw new InvalidOperationException("Chunked stack empty.");

        T result = currentChunk.Value[indexOfNext - 1];

        if (indexOfNext - 1 == 0)
        {
            if (currentChunkIndex > 0)
            {
                indexOfNext = ChunkCapacity;
                currentChunk = currentChunk.Previous ?? currentChunk;
                currentChunkIndex--;
            }
            else
            {
                indexOfNext = 0;
            }
        }
        else
        {
            indexOfNext--;
        }
        Count--;
        return result;
    }

    public IEnumerable<T[]> ReadChunks()
    {
        LinkedListNode<T[]> node = linkedList.First!;
        for (int i = 0; i <= currentChunkIndex; i++)
        {
            yield return node.Value;
            node = node.Next!;
        }
    }
}