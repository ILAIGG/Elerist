using System;
using System.Collections.Generic;

public static class RunMapGenerator
{
    public const int CurrentMapVersion = 6;
    private const int LayerCount = 3;
    private const int MinimumNodesPerLayer = 2;
    private const int MaximumNodesPerLayer = 3;
    private const int TutorialNodeId = 0;
    private const int FirstGeneratedNodeId = 1000;

    public static RunMapNode[] Generate(int seed)
    {
        Random random = new(seed);
        LevelDefinition[] levels = LevelCatalog.GetWorldLevels(LevelCatalog.ForestWorldId);
        LevelDefinition finalLevel = LevelCatalog.GetLevel("Forest_BossBattle");
        List<RunMapNode> nodes = new()
        {
            new RunMapNode
            {
                Id = TutorialNodeId,
                Layer = 0,
                Position = 0,
                LevelId = "Tutorial",
                DisplayName = "Tutorial",
                DifficultyMultiplier = 1.0f,
                IsTutorial = true
            }
        };

        List<RunMapNode> previousLayer = new() { nodes[0] };
        for (int layer = 1; layer <= LayerCount; layer++)
        {
            int nodeCount = layer == LayerCount
                ? 1
                : random.Next(MinimumNodesPerLayer, MaximumNodesPerLayer + 1);
            List<RunMapNode> currentLayer = new(nodeCount);
            List<int> positions = new(nodeCount);
            for (int position = 0; position < nodeCount; position++)
                positions.Add(position);
            Shuffle(positions, random);

            for (int position = 0; position < nodeCount; position++)
            {
                bool isFinal = layer == LayerCount;
                LevelDefinition level = isFinal
                    ? finalLevel
                    : levels[random.Next(levels.Length)];
                float layerDifficulty = 1.0f + (layer * 0.08f);
                float levelAdjustment = (level.BaseDifficulty - 1.0f) * 0.05f;
                float variation = (float)(random.NextDouble() * 0.04 - 0.02);

                RunMapNode node = new()
                {
                    Id = FirstGeneratedNodeId + (layer * 10) + position,
                    Layer = layer,
                    Position = positions[position],
                    LevelId = level.Id,
                    DisplayName = level.DisplayName,
                    DifficultyMultiplier = MathF.Max(1.0f, layerDifficulty + levelAdjustment + variation),
                    IsFinal = isFinal
                };

                currentLayer.Add(node);
                nodes.Add(node);
            }

            currentLayer.Sort((left, right) => left.Position.CompareTo(right.Position));
            previousLayer.Sort((left, right) => left.Position.CompareTo(right.Position));
            ConnectLayers(previousLayer, currentLayer);
            previousLayer = currentLayer;
        }

        return nodes.ToArray();
    }

    public static bool IsValid(RunMapNode[] nodes)
    {
        if (nodes == null || nodes.Length == 0)
            return false;

        bool hasTutorial = false;
        bool hasFinal = false;
        HashSet<int> nodeIds = new();
        Dictionary<int, RunMapNode> nodesById = new();

        foreach (RunMapNode node in nodes)
        {
            if (node == null || !nodeIds.Add(node.Id))
                return false;

            nodesById[node.Id] = node;
            hasTutorial |= node.IsTutorial && node.Id == TutorialNodeId && node.Layer == 0;
            hasFinal |= node.IsFinal && node.Layer == LayerCount;
        }

        if (!hasTutorial || !hasFinal)
            return false;

        foreach (RunMapNode node in nodes)
        {
            foreach (int connectedId in node.ConnectedNodeIds)
            {
                if (!nodesById.TryGetValue(connectedId, out RunMapNode connectedNode))
                    return false;

                if (connectedNode.Layer != node.Layer + 1)
                    return false;
            }
        }

        return true;
    }

    private static void ConnectLayers(List<RunMapNode> previousLayer, List<RunMapNode> currentLayer)
    {
        for (int currentIndex = 0; currentIndex < currentLayer.Count; currentIndex++)
        {
            RunMapNode currentNode = currentLayer[currentIndex];
            int prerequisiteIndex = currentIndex * previousLayer.Count / currentLayer.Count;
            RunMapNode prerequisite = previousLayer[prerequisiteIndex];
            AddConnection(prerequisite, currentNode.Id);
        }

        for (int previousIndex = 0; previousIndex < previousLayer.Count; previousIndex++)
        {
            int targetIndex = previousIndex * currentLayer.Count / previousLayer.Count;
            AddConnection(previousLayer[previousIndex], currentLayer[targetIndex].Id);
        }
    }

    private static void Shuffle(List<int> values, Random random)
    {
        for (int index = values.Count - 1; index > 0; index--)
        {
            int swapIndex = random.Next(index + 1);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }

    private static void AddConnection(RunMapNode node, int connectedNodeId)
    {
        foreach (int existingId in node.ConnectedNodeIds)
        {
            if (existingId == connectedNodeId)
                return;
        }

        int[] connections = new int[node.ConnectedNodeIds.Length + 1];
        node.ConnectedNodeIds.CopyTo(connections, 0);
        connections[^1] = connectedNodeId;
        node.ConnectedNodeIds = connections;
    }

}
