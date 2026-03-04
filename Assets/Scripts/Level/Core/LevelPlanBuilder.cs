using System.Collections.Generic;
using UnityEngine;

internal sealed class LevelPlanBuilder
{
    public LevelNode BuildPlan(LevelGenerationContext generationContext, System.Random random, LevelSequenceProfile levelSequenceProfile, LevelTreasureRatio treasureRatio)
    {
        generationContext.Nodes.Clear();

        int nodeId = 0;

        int combatCount = LevelGeneratorUtility.RandomRangeInclusive(random, levelSequenceProfile.MainCombatCountRange);
        combatCount = Mathf.Clamp(combatCount, 3, 64);

        LevelNode start = NewNode(generationContext, nodeId++, RoomType.Start, null, 0);

        List<LevelNode> mainPath = new List<LevelNode>(combatCount + 2);
        mainPath.Add(start);

        LevelNode cursor = start;

        for (int index = 0; index < combatCount; index++)
        {

            LevelNode combat = NewNode(generationContext, nodeId++, RoomType.Combat, cursor, 0);

            cursor.Children.Add(combat);
            cursor = combat;

            mainPath.Add(combat);

        }

        LevelNode boss = NewNode(generationContext, nodeId++, RoomType.Boss, cursor, 0);

        cursor.Children.Add(boss);
        mainPath.Add(boss);

        int branchBudget = Mathf.Clamp(LevelGeneratorUtility.RandomRangeInclusive(random, levelSequenceProfile.TotalBranchCountRange), 0, 16);

        for (int branchIndex = 0; branchIndex < branchBudget; branchIndex++)
        {

            List<LevelNode> candidates = BuildBranchAttachCandidates(generationContext, levelSequenceProfile);

            if (candidates.Count == 0)
                break;

            LevelNode attach = candidates[random.Next(0, candidates.Count)];
            int depth = attach.Depth + 1;

            if (depth > levelSequenceProfile.MaximumBranchDepth)
                continue;

            int length = Mathf.Max(1, LevelGeneratorUtility.RandomRangeInclusive(random, levelSequenceProfile.BranchLengthRange));

            LevelNode branchCursor = attach;

            for (int nodeIndex = 0; nodeIndex < length; nodeIndex++)
            {

                LevelNode created = NewNode(generationContext, nodeId++, RoomType.Combat, branchCursor, depth);

                branchCursor.Children.Add(created);
                branchCursor = created;

            }

        }

        AddTreasureBranchesByRatio(generationContext, ref nodeId, random, levelSequenceProfile, treasureRatio);

        return start;
    }

    private void AddTreasureBranchesByRatio(LevelGenerationContext generationContext, ref int nodeId, System.Random random, LevelSequenceProfile levelSequenceProfile, LevelTreasureRatio treasureRatio)
    {
        int denominator = Mathf.Max(1, treasureRatio.Denominator);
        int numerator = Mathf.Max(0, treasureRatio.Numerator);

        int totalCombatNodes = 0;

        for (int index = 0; index < generationContext.Nodes.Count; index++)
        {

            LevelNode node = generationContext.Nodes[index];

            if (node.RoomType != RoomType.Combat)
                continue;

            totalCombatNodes++;

        }

        int targetTreasureCount = (totalCombatNodes * numerator) / denominator;

        if (targetTreasureCount <= 0)
            return;

        int maxChildren = Mathf.Clamp(levelSequenceProfile.MaximumChildrenPerRoom, 1, 3);

        List<LevelNode> candidates = new List<LevelNode>(totalCombatNodes);

        for (int index = 0; index < generationContext.Nodes.Count; index++)
        {

            LevelNode node = generationContext.Nodes[index];

            if (node.RoomType != RoomType.Combat)
                continue;

            if (node.Parent == null)
                continue;

            if (node.Parent.RoomType == RoomType.Start)
                continue;

            if (node.Children.Count >= maxChildren)
                continue;

            if (HasTreasureChild(node) == true)
                continue;

            candidates.Add(node);

        }

        if (candidates.Count == 0)
            return;

        LevelGeneratorUtility.Shuffle(candidates, random);

        int placed = 0;

        for (int index = 0; index < candidates.Count && placed < targetTreasureCount; index++)
        {

            LevelNode parentCombat = candidates[index];

            if (parentCombat.Children.Count >= maxChildren)
                continue;

            if (HasTreasureChild(parentCombat) == true)
                continue;

            int depth = parentCombat.Depth + 1;

            if (depth > levelSequenceProfile.MaximumBranchDepth)
                continue;

            LevelNode treasure = NewNode(generationContext, nodeId++, RoomType.Treasure, parentCombat, depth);

            parentCombat.Children.Add(treasure);

            placed++;

        }
    }

    private static bool HasTreasureChild(LevelNode node)
    {
        for (int index = 0; index < node.Children.Count; index++)
        {

            if (node.Children[index].RoomType == RoomType.Treasure)
                return true;

        }

        return false;
    }

    private LevelNode NewNode(LevelGenerationContext generationContext, int id, RoomType type, LevelNode parent, int depth)
    {
        LevelNode node = new LevelNode();
        node.NodeId = id;
        node.RoomType = type;
        node.Parent = parent;
        node.Depth = depth;

        generationContext.Nodes.Add(node);

        return node;
    }

    private List<LevelNode> BuildBranchAttachCandidates(LevelGenerationContext generationContext, LevelSequenceProfile levelSequenceProfile)
    {
        List<LevelNode> result = new List<LevelNode>();

        int maxChildren = Mathf.Clamp(levelSequenceProfile.MaximumChildrenPerRoom, 1, 3);

        for (int i = 0; i < generationContext.Nodes.Count; i++)
        {

            LevelNode node = generationContext.Nodes[i];

            if (node.RoomType != RoomType.Combat)
                continue;

            if (node.Children.Count >= maxChildren)
                continue;

            result.Add(node);

        }

        return result;
    }
}
