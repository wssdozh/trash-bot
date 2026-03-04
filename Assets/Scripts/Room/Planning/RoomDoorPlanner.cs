using System.Collections.Generic;
using UnityEngine;

public sealed class RoomDoorPlanner : MonoBehaviour
{
    private struct DoorCandidate
    {
        public DoorSide Side;
        public int OpeningOffset;
        public Vector2Int InsideCenterCell;
        public float MaximumInnerNoise;
        public float AverageInnerNoise;
        public bool IsValid;

        public DoorCandidate(DoorSide side, int openingOffset, Vector2Int insideCenterCell, float maximumInnerNoise, float averageInnerNoise, bool isValid)
        {
            Side = side;
            OpeningOffset = openingOffset;
            InsideCenterCell = insideCenterCell;
            MaximumInnerNoise = maximumInnerNoise;
            AverageInnerNoise = averageInnerNoise;
            IsValid = isValid;
        }
    }

    [SerializeField] private int _minimumDoorCount = 2;
    [SerializeField] private int _maximumDoorCount = 4;

    [SerializeField] private int _openingWidthInBlocks = 2;
    [SerializeField] private int _openingHeightInBlocks = 2;

    [SerializeField, Min(1)] private int _innerWallClearDepthInCells = 2;

    [SerializeField, Min(0)] private int _cornerMarginInBlocks = 2;

    public List<RoomDoorPlan> CreateDoorPlans(
        Vector3Int roomSizeInBlocks,
        RoomTypeProfile roomTypeProfile,
        bool entranceDoorEnabled,
        bool exitDoorEnabled,
        System.Random random
    )
    {
        return CreateDoorPlans(
            roomSizeInBlocks,
            roomTypeProfile,
            entranceDoorEnabled,
            exitDoorEnabled,
            _minimumDoorCount,
            _maximumDoorCount,
            random
        );
    }

    public List<RoomDoorPlan> CreateDoorPlans(
        Vector3Int roomSizeInBlocks,
        RoomTypeProfile roomTypeProfile,
        bool entranceDoorEnabled,
        bool exitDoorEnabled,
        int minimumDoorCount,
        int maximumDoorCount,
        System.Random random
    )
    {
        int mandatoryDoorCount = 0;

        if (entranceDoorEnabled == true)
        {
            mandatoryDoorCount++;
        }

        if (exitDoorEnabled == true)
        {
            mandatoryDoorCount++;
        }

        int minimum = minimumDoorCount;
        int maximum = maximumDoorCount;

        if (maximum < minimum)
        {
            maximum = minimum;
        }

        int doorCount = minimum;

        if (maximum > minimum)
        {
            doorCount = random.Next(minimum, maximum + 1);
        }

        if (doorCount < mandatoryDoorCount)
        {
            doorCount = mandatoryDoorCount;
        }

        if (doorCount > 4)
        {
            doorCount = 4;
        }

        if (doorCount < 0)
        {
            doorCount = 0;
        }

        if (doorCount == 0)
        {
            return new List<RoomDoorPlan>(0);
        }

        float[,] noiseValues = CreateFlatNoise(roomSizeInBlocks.x, roomSizeInBlocks.z);

        if (roomTypeProfile != null && roomTypeProfile.NoiseProfile != null)
        {
            noiseValues = roomTypeProfile.NoiseProfile.GenerateNoiseMap(roomSizeInBlocks.x, roomSizeInBlocks.z);
        }

        float fillPercent = 0.45f;

        if (roomTypeProfile != null)
        {
            fillPercent = roomTypeProfile.BlockFillPercent;
        }

        float obstacleThreshold = ComputeObstacleThreshold(roomSizeInBlocks, noiseValues, fillPercent);

        Dictionary<DoorSide, List<DoorCandidate>> candidatesBySide = BuildCandidatesBySide(roomSizeInBlocks, noiseValues, obstacleThreshold);

        List<RoomDoorPlan> doorPlans = new List<RoomDoorPlan>(doorCount);
        HashSet<DoorSide> usedSides = new HashSet<DoorSide>();

        if (mandatoryDoorCount == 2)
        {
            DoorCandidate entranceCandidate;
            DoorCandidate exitCandidate;

            PickEntranceExitCandidates(candidatesBySide, random, out entranceCandidate, out exitCandidate);

            RoomDoorPlan entranceDoorPlan = new RoomDoorPlan(
                entranceCandidate.Side,
                DoorRole.Entrance,
                entranceCandidate.OpeningOffset,
                _openingWidthInBlocks,
                _openingHeightInBlocks
            );

            RoomDoorPlan exitDoorPlan = new RoomDoorPlan(
                exitCandidate.Side,
                DoorRole.Exit,
                exitCandidate.OpeningOffset,
                _openingWidthInBlocks,
                _openingHeightInBlocks
            );

            doorPlans.Add(entranceDoorPlan);
            doorPlans.Add(exitDoorPlan);

            usedSides.Add(entranceCandidate.Side);
            usedSides.Add(exitCandidate.Side);
        }
        else if (mandatoryDoorCount == 1)
        {
            DoorRole role = DoorRole.SideExit;

            if (entranceDoorEnabled == true)
            {
                role = DoorRole.Entrance;
            }
            else if (exitDoorEnabled == true)
            {
                role = DoorRole.Exit;
            }

            DoorCandidate singleCandidate = PickSingleCandidate(candidatesBySide, random);

            RoomDoorPlan singleDoorPlan = new RoomDoorPlan(
                singleCandidate.Side,
                role,
                singleCandidate.OpeningOffset,
                _openingWidthInBlocks,
                _openingHeightInBlocks
            );

            doorPlans.Add(singleDoorPlan);
            usedSides.Add(singleCandidate.Side);
        }

        int remainingDoorCount = doorCount - doorPlans.Count;

        if (remainingDoorCount <= 0)
        {
            return doorPlans;
        }

        List<DoorSide> remainingSides = new List<DoorSide>(4);
        remainingSides.Add(DoorSide.North);
        remainingSides.Add(DoorSide.East);
        remainingSides.Add(DoorSide.South);
        remainingSides.Add(DoorSide.West);

        for (int index = remainingSides.Count - 1; index >= 0; index--)
        {
            if (usedSides.Contains(remainingSides[index]) == true)
            {
                remainingSides.RemoveAt(index);
            }
        }

        ShuffleSides(remainingSides, random);

        if (remainingDoorCount > remainingSides.Count)
        {
            remainingDoorCount = remainingSides.Count;
        }

        for (int index = 0; index < remainingDoorCount; index++)
        {
            DoorSide side = remainingSides[index];
            DoorCandidate sideCandidate = PickCleanestCandidate(candidatesBySide[side], random);

            RoomDoorPlan sideExitDoorPlan = new RoomDoorPlan(
                side,
                DoorRole.SideExit,
                sideCandidate.OpeningOffset,
                _openingWidthInBlocks,
                _openingHeightInBlocks
            );

            doorPlans.Add(sideExitDoorPlan);
        }

        return doorPlans;
    }

    private DoorCandidate PickSingleCandidate(Dictionary<DoorSide, List<DoorCandidate>> candidatesBySide, System.Random random)
    {
        List<DoorCandidate> allCandidates = new List<DoorCandidate>();

        AddCandidates(allCandidates, candidatesBySide[DoorSide.North]);
        AddCandidates(allCandidates, candidatesBySide[DoorSide.East]);
        AddCandidates(allCandidates, candidatesBySide[DoorSide.South]);
        AddCandidates(allCandidates, candidatesBySide[DoorSide.West]);

        bool foundValid = false;
        float bestScore = float.MaxValue;

        DoorCandidate bestCandidate = allCandidates[0];

        for (int index = 0; index < allCandidates.Count; index++)
        {
            DoorCandidate candidate = allCandidates[index];

            float score = candidate.AverageInnerNoise + ((float)random.NextDouble() * 0.001f);

            if (candidate.IsValid == true)
            {
                if (foundValid == false || score < bestScore)
                {
                    foundValid = true;
                    bestScore = score;
                    bestCandidate = candidate;
                }
            }
            else
            {
                if (foundValid == false && score < bestScore)
                {
                    bestScore = score;
                    bestCandidate = candidate;
                }
            }
        }

        return bestCandidate;
    }

    private float[,] CreateFlatNoise(int width, int height)
    {
        float[,] values = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                values[x, y] = 0.5f;
            }
        }

        return values;
    }

    private float ComputeObstacleThreshold(Vector3Int roomSizeInBlocks, float[,] noiseValues, float fillPercent)
    {
        int width = roomSizeInBlocks.x;
        int height = roomSizeInBlocks.z;

        int cellCount = width * height;

        if (cellCount <= 0)
        {
            return 1f;
        }

        float[] samples = new float[cellCount];

        int sampleIndex = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                samples[sampleIndex] = noiseValues[x, y];
                sampleIndex++;
            }
        }

        System.Array.Sort(samples);

        float percent = Mathf.Clamp01(fillPercent);
        int filledCells = Mathf.RoundToInt(cellCount * percent);

        if (filledCells <= 0)
        {
            return 1f;
        }

        if (filledCells >= cellCount)
        {
            return 0f;
        }

        int thresholdIndex = cellCount - filledCells;
        thresholdIndex = Mathf.Clamp(thresholdIndex, 0, cellCount - 1);

        return samples[thresholdIndex];
    }

    private Dictionary<DoorSide, List<DoorCandidate>> BuildCandidatesBySide(Vector3Int roomSizeInBlocks, float[,] noiseValues, float obstacleThreshold)
    {
        Dictionary<DoorSide, List<DoorCandidate>> result = new Dictionary<DoorSide, List<DoorCandidate>>(4);

        result.Add(DoorSide.North, BuildCandidatesForSide(DoorSide.North, roomSizeInBlocks, noiseValues, obstacleThreshold));
        result.Add(DoorSide.East, BuildCandidatesForSide(DoorSide.East, roomSizeInBlocks, noiseValues, obstacleThreshold));
        result.Add(DoorSide.South, BuildCandidatesForSide(DoorSide.South, roomSizeInBlocks, noiseValues, obstacleThreshold));
        result.Add(DoorSide.West, BuildCandidatesForSide(DoorSide.West, roomSizeInBlocks, noiseValues, obstacleThreshold));

        return result;
    }

    private List<DoorCandidate> BuildCandidatesForSide(DoorSide side, Vector3Int roomSizeInBlocks, float[,] noiseValues, float obstacleThreshold)
    {
        int wallLength = GetWallLengthInBlocks(side, roomSizeInBlocks);

        int openingWidth = _openingWidthInBlocks;
        if (openingWidth < 1)
        {
            openingWidth = 1;
        }

        int maximumOffset = wallLength - openingWidth - _cornerMarginInBlocks;

        int minimumOffset = _cornerMarginInBlocks;

        List<DoorCandidate> candidates = new List<DoorCandidate>();

        if (maximumOffset < minimumOffset)
        {
            DoorCandidate fallbackCandidate = BuildCandidate(side, _cornerMarginInBlocks, roomSizeInBlocks, noiseValues, obstacleThreshold);
            candidates.Add(fallbackCandidate);

            return candidates;
        }

        for (int offset = minimumOffset; offset <= maximumOffset; offset++)
        {
            DoorCandidate candidate = BuildCandidate(side, offset, roomSizeInBlocks, noiseValues, obstacleThreshold);
            candidates.Add(candidate);
        }

        return candidates;
    }

    private DoorCandidate BuildCandidate(DoorSide side, int openingOffset, Vector3Int roomSizeInBlocks, float[,] noiseValues, float obstacleThreshold)
    {
        Vector2Int insideCenter = GetInsideCenterCell(side, openingOffset, roomSizeInBlocks);

        float maximum = 0f;
        float sum = 0f;

        bool valid = true;

        int width = roomSizeInBlocks.x;
        int height = roomSizeInBlocks.z;

        for (int depth = 1; depth <= _innerWallClearDepthInCells; depth++)
        {
            for (int offset = 0; offset < _openingWidthInBlocks; offset++)
            {
                Vector2Int cell = GetInnerCell(side, openingOffset, offset, depth, roomSizeInBlocks);

                int cellX = Mathf.Clamp(cell.x, 0, width - 1);
                int cellY = Mathf.Clamp(cell.y, 0, height - 1);

                float value = noiseValues[cellX, cellY];

                sum += value;

                if (value > maximum)
                {
                    maximum = value;
                }

                if (value >= obstacleThreshold)
                {
                    valid = false;
                }
            }
        }

        float average = sum / Mathf.Max(1, _innerWallClearDepthInCells * _openingWidthInBlocks);

        return new DoorCandidate(side, openingOffset, insideCenter, maximum, average, valid);
    }

    private Vector2Int GetInsideCenterCell(DoorSide side, int openingOffset, Vector3Int roomSizeInBlocks)
    {
        int half = Mathf.Max(1, _openingWidthInBlocks) / 2;

        if (side == DoorSide.North)
        {
            return new Vector2Int(openingOffset + half, roomSizeInBlocks.z - 1 - _innerWallClearDepthInCells);
        }

        if (side == DoorSide.South)
        {
            return new Vector2Int(openingOffset + half, _innerWallClearDepthInCells);
        }

        if (side == DoorSide.East)
        {
            return new Vector2Int(roomSizeInBlocks.x - 1 - _innerWallClearDepthInCells, openingOffset + half);
        }

        return new Vector2Int(_innerWallClearDepthInCells, openingOffset + half);
    }

    private Vector2Int GetInnerCell(DoorSide side, int openingOffset, int offsetInOpening, int depthFromWall, Vector3Int roomSizeInBlocks)
    {
        if (side == DoorSide.North)
        {
            return new Vector2Int(openingOffset + offsetInOpening, (roomSizeInBlocks.z - 1) - depthFromWall);
        }

        if (side == DoorSide.South)
        {
            return new Vector2Int(openingOffset + offsetInOpening, 0 + depthFromWall);
        }

        if (side == DoorSide.East)
        {
            return new Vector2Int((roomSizeInBlocks.x - 1) - depthFromWall, openingOffset + offsetInOpening);
        }

        return new Vector2Int(0 + depthFromWall, openingOffset + offsetInOpening);
    }

    private void PickEntranceExitCandidates(
        Dictionary<DoorSide, List<DoorCandidate>> candidatesBySide,
        System.Random random,
        out DoorCandidate entranceCandidate,
        out DoorCandidate exitCandidate
    )
    {
        DoorCandidate bestEntrance = PickCleanestCandidate(candidatesBySide[DoorSide.North], random);
        DoorCandidate bestExit = PickCleanestCandidate(candidatesBySide[DoorSide.South], random);

        List<DoorCandidate> all = new List<DoorCandidate>();

        AddCandidates(all, candidatesBySide[DoorSide.North]);
        AddCandidates(all, candidatesBySide[DoorSide.East]);
        AddCandidates(all, candidatesBySide[DoorSide.South]);
        AddCandidates(all, candidatesBySide[DoorSide.West]);

        float bestPairScore = float.MaxValue;

        DoorCandidate bestA = all[0];
        DoorCandidate bestB = all[0];

        for (int a = 0; a < all.Count; a++)
        {
            for (int b = 0; b < all.Count; b++)
            {
                if (a == b)
                {
                    continue;
                }

                DoorCandidate candidateA = all[a];
                DoorCandidate candidateB = all[b];

                if (candidateA.Side == candidateB.Side)
                {
                    continue;
                }

                int distance = ManhattanDistance(candidateA.InsideCenterCell, candidateB.InsideCenterCell);

                int targetDistance = ComputeTargetDistance(candidateA.Side, candidateB.Side, roomSizeInBlocks: Vector3Int.zero);

                int delta = Mathf.Abs(distance - targetDistance);

                float score = (candidateA.AverageInnerNoise + candidateB.AverageInnerNoise) + (delta * 0.01f);

                if (candidateA.IsValid == false || candidateB.IsValid == false)
                {
                    score += 10f;
                }

                score += (float)random.NextDouble() * 0.001f;

                if (score < bestPairScore)
                {
                    bestPairScore = score;
                    bestA = candidateA;
                    bestB = candidateB;
                }
            }
        }

        bool preferOpposite = random.NextDouble() < 0.65;

        if (preferOpposite == true)
        {
            entranceCandidate = bestA;
            exitCandidate = bestB;
            return;
        }

        entranceCandidate = bestEntrance;
        exitCandidate = bestExit;

        if (entranceCandidate.Side == exitCandidate.Side)
        {
            entranceCandidate = bestA;
            exitCandidate = bestB;
        }
    }

    private int ComputeTargetDistance(DoorSide sideA, DoorSide sideB, Vector3Int roomSizeInBlocks)
    {
        return 12;
    }

    private int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private DoorCandidate PickCleanestCandidate(List<DoorCandidate> candidates, System.Random random)
    {
        bool foundValid = false;
        float bestScore = float.MaxValue;

        DoorCandidate best = candidates[0];

        for (int index = 0; index < candidates.Count; index++)
        {
            DoorCandidate candidate = candidates[index];

            float score = candidate.AverageInnerNoise + ((float)random.NextDouble() * 0.001f);

            if (candidate.IsValid == true)
            {
                if (foundValid == false || score < bestScore)
                {
                    foundValid = true;
                    bestScore = score;
                    best = candidate;
                }
            }
            else
            {
                if (foundValid == false && score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
        }

        return best;
    }

    private void AddCandidates(List<DoorCandidate> buffer, List<DoorCandidate> candidates)
    {
        for (int index = 0; index < candidates.Count; index++)
        {
            buffer.Add(candidates[index]);
        }
    }

    private int GetWallLengthInBlocks(DoorSide side, Vector3Int roomSizeInBlocks)
    {
        if (side == DoorSide.North || side == DoorSide.South)
        {
            return roomSizeInBlocks.x;
        }

        return roomSizeInBlocks.z;
    }

    private void ShuffleSides(List<DoorSide> sides, System.Random random)
    {
        for (int index = sides.Count - 1; index > 0; index--)
        {
            int swapIndex = random.Next(0, index + 1);

            DoorSide swap = sides[index];
            sides[index] = sides[swapIndex];
            sides[swapIndex] = swap;
        }
    }
}
