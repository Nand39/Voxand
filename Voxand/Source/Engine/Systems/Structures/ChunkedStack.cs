namespace Voxand.Engine.Systems.Structures;
public class ChunkedStack<T>
{
    LinkedList<T[]> linkedList;
    int chunkCapacity;
    LinkedListNode<T[]> lastChunk;
    int indexOfNext = 0;

    public int Count { get; private set; } = 0;
    public bool Empty => Count == 0;

    public ChunkedStack(int chunkCapacity)
    {
        this.chunkCapacity = chunkCapacity;
        T[] chunk = new T[chunkCapacity];
        lastChunk = new(chunk);
        linkedList = [];
        linkedList.AddLast(lastChunk);
    }

    public void Push(T item)
    {
        if (indexOfNext == chunkCapacity)
        {
            T[] newChunk = new T[chunkCapacity];
            newChunk[0] = item;
            lastChunk = new(newChunk);
            linkedList.AddLast(lastChunk);
            indexOfNext = 1;
        }
        else
        {
            lastChunk.Value[indexOfNext] = item;
            indexOfNext++;
        }
        Count++;
    }

    public T Pop()
    {
        if (Count == 0)
            throw new InvalidOperationException("Chunked stack empty.");

        T result = lastChunk.Value[indexOfNext - 1];

        if (indexOfNext - 1 == 0)
        {
            indexOfNext = chunkCapacity;
            lastChunk = lastChunk.Previous ?? lastChunk;
        }
        else
        {
            indexOfNext--;
        }
        Count--;
        return result;
    }
}