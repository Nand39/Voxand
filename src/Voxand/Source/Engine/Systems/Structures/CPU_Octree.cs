using OpenTK.Mathematics;
using System.Runtime.CompilerServices;

namespace Voxand.Engine.Systems.Structures;
public class Octree<T>(T rootValue, Vector3 min, Vector3 max)
{
    sealed class Node(T value, Vector3 min, Vector3 max, uint depth)
    {
        public T Value { get; set; } = value;
        uint depth = depth;
        Vector3 midPoint = (max + min) * 0.5f;
        Vector3 min = min;
        Node[] descendants = new Node[8];

        public Node AddDescendant(int index, T value)
        {
            Vector3 newMin = min;
            Vector3 newMax = midPoint;

            if ((index & 1) != 0) newMin.X = midPoint.X;
            else newMax.X = midPoint.X;

            if ((index & 2) != 0) newMin.Y = midPoint.Y;
            else newMax.Y = midPoint.Y;

            if ((index & 4) != 0) newMin.Z = midPoint.Z;
            else newMax.Z = midPoint.Z;

            Node newNode = new Node(value, newMin, newMax, depth + 1);
            descendants[index] = newNode;
            return newNode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Node GetDescendant(int index)
        {
            return descendants[index];
        }

        public int GetNextDescendantIndex(Vector3 position)
        {
            if (position.X > midPoint.X)
            {
                if (position.Z > midPoint.Z)
                {
                    if (position.Y > midPoint.Y)
                    {
                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    if (position.Y > midPoint.Y)
                    {
                        return 2;
                    }
                    else
                    {
                        return 3;
                    }
                }
            }
            else
            {
                if (position.Z > midPoint.Z)
                {
                    if (position.Y > midPoint.Y)
                    {
                        return 4;
                    }
                    else
                    {
                        return 5;
                    }
                }
                else
                {
                    if (position.Y > midPoint.Y)
                    {
                        return 6;
                    }
                    else
                    {
                        return 7;
                    }
                }
            }
        }
    }

    Node root = new(rootValue, min, max, 0);

    public void Insert(T value, Vector3 position, uint depth)
    {
        InsertRecursive(value, root, position, depth);
    }
    public T Search(Vector3 position, uint maxDepth)
    {
        return SearchRecursive(root, position, maxDepth);
    }
    void InsertRecursive(T value, Node node, Vector3 position, uint depthRemains)
    {
        if (depthRemains == 0) 
        { 
            node.Value = value; 
            return; 
        }

        int nextIndex = node.GetNextDescendantIndex(position);
        Node next = node.GetDescendant(nextIndex);
        if (next == null)
            node = node.AddDescendant(nextIndex, value);
        else
            node = next;

        InsertRecursive(value, node, position, depthRemains - 1);
    }
    T SearchRecursive(Node node, Vector3 position, uint depthRemains)
    {
        if (depthRemains > 0)
        {
            int nextIndex = node.GetNextDescendantIndex(position);
            Node next = node.GetDescendant(nextIndex);
            if (next == null)
            {
                return node.Value;
            }
            return SearchRecursive(next, position, depthRemains - 1);
        }
        else
        {
            return node.Value;
        }
    }
}