#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class CubeFractureUtilityWindow : EditorWindow
{
    private Vector3Int _gridSize = new Vector3Int(4, 4, 4);
    private float _boundsPadding = 0.0005f;

    private Material _cutMaterial;

    private bool _addMeshColliders = true;
    private bool _convexColliders = true;

    private bool _addRigidbodies = true;
    private float _fragmentMass = 1f;

    private bool _disableFragmentsInPrefab = false;

    [MenuItem("Tools/Cube Fracture Utility")]
    public static void Open()
    {
        CubeFractureUtilityWindow window = GetWindow<CubeFractureUtilityWindow>("Cube Fracture Utility");
        window.minSize = new Vector2(420f, 320f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Cube Fracture (Prefab Bake Next To Source)", EditorStyles.boldLabel);

        _gridSize = EditorGUILayout.Vector3IntField("Grid Size", _gridSize);
        _boundsPadding = EditorGUILayout.FloatField("Bounds Padding", _boundsPadding);

        _cutMaterial = (Material)EditorGUILayout.ObjectField("Cut Material", _cutMaterial, typeof(Material), false);

        EditorGUILayout.Space(10f);

        _addMeshColliders = EditorGUILayout.Toggle("Add MeshColliders", _addMeshColliders);
        _convexColliders = EditorGUILayout.Toggle("Convex Colliders", _convexColliders);

        _addRigidbodies = EditorGUILayout.Toggle("Add Rigidbodies", _addRigidbodies);
        _fragmentMass = EditorGUILayout.FloatField("Fragment Mass", _fragmentMass);

        _disableFragmentsInPrefab = EditorGUILayout.Toggle("Disable Fragments In Prefab", _disableFragmentsInPrefab);

        EditorGUILayout.Space(14f);

        bool hasSelection = Selection.objects != null && Selection.objects.Length > 0;
        GUI.enabled = hasSelection == true;

        if (GUILayout.Button("Bake Selected (Create Prefab Next To Source Prefab)") == true)
        {
            BakeSelected();
        }

        GUI.enabled = true;

        EditorGUILayout.Space(6f);

        EditorGUILayout.HelpBox(
            "Выбери prefab в Project (или инстанс prefab в сцене). " +
            "Утилита создаст новый prefab рядом с исходным (в той же папке) + Mesh assets в подпапке Meshes.",
            MessageType.Info
        );
    }

    private void BakeSelected()
    {
        UnityEngine.Object[] selectedObjects = Selection.objects;

        int index = 0;
        while (index < selectedObjects.Length)
        {
            GameObject selectedGameObject = selectedObjects[index] as GameObject;
            if (selectedGameObject != null)
            {
                BakeSelectedGameObject(selectedGameObject);
            }

            index++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void BakeSelectedGameObject(GameObject selectedGameObject)
    {
        GameObject prefabSource = GetPrefabSource(selectedGameObject);
        if (prefabSource == null)
        {
            return;
        }

        string prefabAssetPath = AssetDatabase.GetAssetPath(prefabSource);
        if (string.IsNullOrEmpty(prefabAssetPath) == true)
        {
            return;
        }

        string prefabFolder = Path.GetDirectoryName(prefabAssetPath);
        if (string.IsNullOrEmpty(prefabFolder) == true)
        {
            return;
        }

        EnsureAssetFolder(prefabFolder);

        string meshesFolder = prefabFolder.Replace("\\", "/") + "/Meshes";
        EnsureAssetFolder(meshesFolder);

        BakePrefabAsset(prefabSource, prefabFolder.Replace("\\", "/"), meshesFolder);
    }

    private void BakePrefabAsset(GameObject prefabSource, string prefabFolder, string meshesFolder)
    {
        GameObject bakeRoot = PrefabUtility.InstantiatePrefab(prefabSource) as GameObject;
        if (bakeRoot == null)
        {
            return;
        }

        MeshFilter meshFilter = bakeRoot.GetComponentInChildren<MeshFilter>();
        MeshRenderer meshRenderer = bakeRoot.GetComponentInChildren<MeshRenderer>();

        if (meshFilter == null || meshRenderer == null)
        {
            DestroyImmediate(bakeRoot);
            return;
        }

        Mesh sourceMesh = meshFilter.sharedMesh;
        if (sourceMesh == null)
        {
            DestroyImmediate(bakeRoot);
            return;
        }

        Material[] materials = BuildMaterials(meshRenderer.sharedMaterials, _cutMaterial);

        int targetSubMeshCount = sourceMesh.subMeshCount;
        int cutSubMeshIndex = -1;

        if (_cutMaterial != null)
        {
            cutSubMeshIndex = targetSubMeshCount;
            targetSubMeshCount = targetSubMeshCount + 1;
        }

        List<Mesh> fragmentMeshes = CubeMeshFracturer.CreateFragments(
            sourceMesh,
            _gridSize,
            _boundsPadding,
            cutSubMeshIndex,
            targetSubMeshCount,
            prefabSource.name + "_Fragment"
        );

        if (fragmentMeshes.Count <= 0)
        {
            DestroyImmediate(bakeRoot);
            return;
        }

        string fracturedPrefabName = prefabSource.name + "_Fractured";
        string fracturedPrefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabFolder + "/" + fracturedPrefabName + ".prefab");

        GameObject fracturedRoot = new GameObject(fracturedPrefabName);
        fracturedRoot.transform.position = Vector3.zero;
        fracturedRoot.transform.rotation = Quaternion.identity;
        fracturedRoot.transform.localScale = Vector3.one;

        int fragmentIndex = 0;
        while (fragmentIndex < fragmentMeshes.Count)
        {
            Mesh fragmentMesh = fragmentMeshes[fragmentIndex];
            fragmentMesh.name = prefabSource.name + "_FragmentMesh_" + fragmentIndex.ToString("000");

            string meshAssetPath = AssetDatabase.GenerateUniqueAssetPath(meshesFolder + "/" + fragmentMesh.name + ".asset");
            AssetDatabase.CreateAsset(fragmentMesh, meshAssetPath);

            GameObject fragmentObject = new GameObject(prefabSource.name + "_Fragment_" + fragmentIndex.ToString("000"));
            fragmentObject.transform.SetParent(fracturedRoot.transform, false);
            fragmentObject.transform.localPosition = Vector3.zero;
            fragmentObject.transform.localRotation = Quaternion.identity;
            fragmentObject.transform.localScale = Vector3.one;

            MeshFilter fragmentMeshFilter = fragmentObject.AddComponent<MeshFilter>();
            fragmentMeshFilter.sharedMesh = fragmentMesh;

            MeshRenderer fragmentMeshRenderer = fragmentObject.AddComponent<MeshRenderer>();
            fragmentMeshRenderer.sharedMaterials = materials;

            if (_addMeshColliders == true)
            {
                MeshCollider meshCollider = fragmentObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = fragmentMesh;
                meshCollider.convex = _convexColliders;
            }

            if (_addRigidbodies == true)
            {
                Rigidbody rigidbody = fragmentObject.AddComponent<Rigidbody>();
                rigidbody.mass = _fragmentMass;
            }

            if (_disableFragmentsInPrefab == true)
            {
                fragmentObject.SetActive(false);
            }

            fragmentIndex++;
        }

        PrefabUtility.SaveAsPrefabAsset(fracturedRoot, fracturedPrefabPath);

        DestroyImmediate(fracturedRoot);
        DestroyImmediate(bakeRoot);
    }

    private GameObject GetPrefabSource(GameObject selectedGameObject)
    {
        string assetPath = AssetDatabase.GetAssetPath(selectedGameObject);
        if (string.IsNullOrEmpty(assetPath) == false)
        {
            return selectedGameObject;
        }

        GameObject correspondingPrefab = PrefabUtility.GetCorrespondingObjectFromSource(selectedGameObject);
        if (correspondingPrefab != null)
        {
            return correspondingPrefab;
        }

        return null;
    }

    private static Material[] BuildMaterials(Material[] sourceMaterials, Material cutMaterial)
    {
        if (cutMaterial == null)
        {
            return sourceMaterials;
        }

        Material[] materials = new Material[sourceMaterials.Length + 1];

        int materialIndex = 0;
        while (materialIndex < sourceMaterials.Length)
        {
            materials[materialIndex] = sourceMaterials[materialIndex];
            materialIndex++;
        }

        materials[materials.Length - 1] = cutMaterial;
        return materials;
    }

    private static void EnsureAssetFolder(string assetPath)
    {
        assetPath = assetPath.Replace("\\", "/");

        if (AssetDatabase.IsValidFolder(assetPath) == true)
        {
            return;
        }

        string[] parts = assetPath.Split('/');
        string currentPath = parts[0];

        int partIndex = 1;
        while (partIndex < parts.Length)
        {
            string nextPath = currentPath + "/" + parts[partIndex];
            if (AssetDatabase.IsValidFolder(nextPath) == false)
            {
                AssetDatabase.CreateFolder(currentPath, parts[partIndex]);
            }

            currentPath = nextPath;
            partIndex++;
        }

        string absolutePath = Application.dataPath;
        if (assetPath.StartsWith("Assets/") == true)
        {
            absolutePath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
        }
        else if (assetPath == "Assets")
        {
            absolutePath = Application.dataPath;
        }

        Directory.CreateDirectory(absolutePath);
    }
}

public static class CubeMeshFracturer
{
    public static List<Mesh> CreateFragments(Mesh sourceMesh, Vector3Int gridSize, float boundsPadding, int cutSubMeshIndex, int targetSubMeshCount, string meshNamePrefix)
    {
        int cellsX = Mathf.Max(1, gridSize.x);
        int cellsY = Mathf.Max(1, gridSize.y);
        int cellsZ = Mathf.Max(1, gridSize.z);

        MeshData sourceData = MeshData.FromMesh(sourceMesh, targetSubMeshCount);

        Bounds bounds = sourceMesh.bounds;
        bounds.Expand(boundsPadding);

        Vector3 cellSize = new Vector3(bounds.size.x / cellsX, bounds.size.y / cellsY, bounds.size.z / cellsZ);

        List<Mesh> meshes = new List<Mesh>();
        int fragmentIndex = 0;

        int cellX = 0;
        while (cellX < cellsX)
        {
            int cellY = 0;
            while (cellY < cellsY)
            {
                int cellZ = 0;
                while (cellZ < cellsZ)
                {
                    Vector3 cellMin = new Vector3(
                        bounds.min.x + cellSize.x * cellX,
                        bounds.min.y + cellSize.y * cellY,
                        bounds.min.z + cellSize.z * cellZ
                    );

                    Vector3 cellMax = new Vector3(
                        cellMin.x + cellSize.x,
                        cellMin.y + cellSize.y,
                        cellMin.z + cellSize.z
                    );

                    Plane planeXMin = new Plane(Vector3.right, new Vector3(cellMin.x, 0f, 0f));
                    Plane planeXMax = new Plane(Vector3.left, new Vector3(cellMax.x, 0f, 0f));

                    Plane planeYMin = new Plane(Vector3.up, new Vector3(0f, cellMin.y, 0f));
                    Plane planeYMax = new Plane(Vector3.down, new Vector3(0f, cellMax.y, 0f));

                    Plane planeZMin = new Plane(Vector3.forward, new Vector3(0f, 0f, cellMin.z));
                    Plane planeZMax = new Plane(Vector3.back, new Vector3(0f, 0f, cellMax.z));

                    MeshData clipped = sourceData;

                    clipped = MeshClipper.ClipByPlane(clipped, planeXMin, cutSubMeshIndex);
                    clipped = MeshClipper.ClipByPlane(clipped, planeXMax, cutSubMeshIndex);
                    clipped = MeshClipper.ClipByPlane(clipped, planeYMin, cutSubMeshIndex);
                    clipped = MeshClipper.ClipByPlane(clipped, planeYMax, cutSubMeshIndex);
                    clipped = MeshClipper.ClipByPlane(clipped, planeZMin, cutSubMeshIndex);
                    clipped = MeshClipper.ClipByPlane(clipped, planeZMax, cutSubMeshIndex);

                    if (clipped.VertexCount > 0)
                    {
                        Mesh fragmentMesh = clipped.ToMesh(meshNamePrefix + "_" + fragmentIndex.ToString("000"), targetSubMeshCount);
                        meshes.Add(fragmentMesh);
                        fragmentIndex++;
                    }

                    cellZ++;
                }

                cellY++;
            }

            cellX++;
        }

        return meshes;
    }
}

internal static class MeshClipper
{
    public static MeshData ClipByPlane(MeshData source, Plane plane, int cutSubMeshIndex)
    {
        MeshBuilder meshBuilder = new MeshBuilder(source.Triangles.Length);
        List<CutSegment> cutSegments = new List<CutSegment>();

        int subMeshIndex = 0;
        while (subMeshIndex < source.Triangles.Length)
        {
            int[] triangles = source.Triangles[subMeshIndex];
            int triangleIndex = 0;

            while (triangleIndex < triangles.Length)
            {
                int indexA = triangles[triangleIndex + 0];
                int indexB = triangles[triangleIndex + 1];
                int indexC = triangles[triangleIndex + 2];

                VertexData vertexA = source.GetVertex(indexA);
                VertexData vertexB = source.GetVertex(indexB);
                VertexData vertexC = source.GetVertex(indexC);

                bool insideA = plane.GetDistanceToPoint(vertexA.Position) >= 0f;
                bool insideB = plane.GetDistanceToPoint(vertexB.Position) >= 0f;
                bool insideC = plane.GetDistanceToPoint(vertexC.Position) >= 0f;

                int insideCount = 0;
                if (insideA == true) insideCount++;
                if (insideB == true) insideCount++;
                if (insideC == true) insideCount++;

                if (insideCount == 3)
                {
                    meshBuilder.AddTriangle(subMeshIndex, vertexA, vertexB, vertexC);
                }
                else if (insideCount == 1)
                {
                    VertexData insideVertex;
                    VertexData outsideVertex1;
                    VertexData outsideVertex2;

                    if (insideA == true)
                    {
                        insideVertex = vertexA;
                        outsideVertex1 = vertexB;
                        outsideVertex2 = vertexC;
                    }
                    else if (insideB == true)
                    {
                        insideVertex = vertexB;
                        outsideVertex1 = vertexC;
                        outsideVertex2 = vertexA;
                    }
                    else
                    {
                        insideVertex = vertexC;
                        outsideVertex1 = vertexA;
                        outsideVertex2 = vertexB;
                    }

                    VertexData cutVertex1 = Intersect(insideVertex, outsideVertex1, plane);
                    VertexData cutVertex2 = Intersect(insideVertex, outsideVertex2, plane);

                    meshBuilder.AddTriangle(subMeshIndex, insideVertex, cutVertex1, cutVertex2);

                    if (cutSubMeshIndex >= 0)
                    {
                        cutSegments.Add(new CutSegment(cutVertex1.Position, cutVertex2.Position));
                    }
                }
                else if (insideCount == 2)
                {
                    VertexData outsideVertex;
                    VertexData insideVertex1;
                    VertexData insideVertex2;

                    if (insideA == false)
                    {
                        outsideVertex = vertexA;
                        insideVertex1 = vertexB;
                        insideVertex2 = vertexC;
                    }
                    else if (insideB == false)
                    {
                        outsideVertex = vertexB;
                        insideVertex1 = vertexC;
                        insideVertex2 = vertexA;
                    }
                    else
                    {
                        outsideVertex = vertexC;
                        insideVertex1 = vertexA;
                        insideVertex2 = vertexB;
                    }

                    VertexData cutVertex1 = Intersect(insideVertex1, outsideVertex, plane);
                    VertexData cutVertex2 = Intersect(insideVertex2, outsideVertex, plane);

                    meshBuilder.AddTriangle(subMeshIndex, insideVertex1, insideVertex2, cutVertex2);
                    meshBuilder.AddTriangle(subMeshIndex, insideVertex1, cutVertex2, cutVertex1);

                    if (cutSubMeshIndex >= 0)
                    {
                        cutSegments.Add(new CutSegment(cutVertex1.Position, cutVertex2.Position));
                    }
                }

                triangleIndex += 3;
            }

            subMeshIndex++;
        }

        if (cutSubMeshIndex >= 0 && cutSegments.Count >= 3)
        {
            Vector3 capNormal = -plane.normal;
            CapBuilder.AddCaps(meshBuilder, cutSegments, plane.normal, capNormal, cutSubMeshIndex);
        }

        return meshBuilder.BuildMeshData();
    }

    private static VertexData Intersect(VertexData insideVertex, VertexData outsideVertex, Plane plane)
    {
        float insideDistance = plane.GetDistanceToPoint(insideVertex.Position);
        float outsideDistance = plane.GetDistanceToPoint(outsideVertex.Position);

        float t = insideDistance / (insideDistance - outsideDistance);

        Vector3 position = Vector3.Lerp(insideVertex.Position, outsideVertex.Position, t);
        Vector3 normal = Vector3.Lerp(insideVertex.Normal, outsideVertex.Normal, t).normalized;
        Vector2 uv = Vector2.Lerp(insideVertex.Uv, outsideVertex.Uv, t);

        return new VertexData(position, normal, uv);
    }
}

internal static class CapBuilder
{
    public static void AddCaps(MeshBuilder meshBuilder, List<CutSegment> cutSegments, Vector3 planeNormal, Vector3 capNormal, int capSubMeshIndex)
    {
        Dictionary<Vector3Key, Vector3> positions = new Dictionary<Vector3Key, Vector3>();
        Dictionary<Vector3Key, List<Vector3Key>> adjacency = new Dictionary<Vector3Key, List<Vector3Key>>();
        HashSet<EdgeKey> uniqueEdges = new HashSet<EdgeKey>();

        float quantizeScale = 100000f;

        int segmentIndex = 0;
        while (segmentIndex < cutSegments.Count)
        {
            Vector3 pointA = cutSegments[segmentIndex].PointA;
            Vector3 pointB = cutSegments[segmentIndex].PointB;

            Vector3Key keyA = Vector3Key.From(pointA, quantizeScale);
            Vector3Key keyB = Vector3Key.From(pointB, quantizeScale);

            if (keyA.Equals(keyB) == false)
            {
                EdgeKey edgeKey = new EdgeKey(keyA, keyB);
                if (uniqueEdges.Add(edgeKey) == true)
                {
                    if (positions.ContainsKey(keyA) == false) positions.Add(keyA, pointA);
                    if (positions.ContainsKey(keyB) == false) positions.Add(keyB, pointB);

                    AddNeighbor(adjacency, keyA, keyB);
                    AddNeighbor(adjacency, keyB, keyA);
                }
            }

            segmentIndex++;
        }

        Vector3 basisU = Vector3.Cross(planeNormal, Vector3.up);
        if (basisU.sqrMagnitude < 0.0001f)
        {
            basisU = Vector3.Cross(planeNormal, Vector3.right);
        }
        basisU.Normalize();
        Vector3 basisV = Vector3.Cross(planeNormal, basisU).normalized;

        HashSet<Vector3Key> unprocessed = new HashSet<Vector3Key>(adjacency.Keys);

        while (unprocessed.Count > 0)
        {
            Vector3Key startKey = GetFirst(unprocessed);
            List<Vector3Key> loop = BuildLoop(adjacency, unprocessed, startKey);

            if (loop.Count >= 3)
            {
                AddLoop(meshBuilder, loop, positions, basisU, basisV, capNormal, capSubMeshIndex);
            }
        }
    }

    private static void AddLoop(MeshBuilder meshBuilder, List<Vector3Key> loop, Dictionary<Vector3Key, Vector3> positions, Vector3 basisU, Vector3 basisV, Vector3 capNormal, int capSubMeshIndex)
    {
        Vector3 center = Vector3.zero;

        int index = 0;
        while (index < loop.Count)
        {
            center += positions[loop[index]];
            index++;
        }

        center /= loop.Count;

        List<CapVertex> capVertices = new List<CapVertex>(loop.Count);

        index = 0;
        while (index < loop.Count)
        {
            Vector3 position = positions[loop[index]];
            Vector3 local = position - center;

            float u = Vector3.Dot(local, basisU);
            float v = Vector3.Dot(local, basisV);
            float angle = Mathf.Atan2(v, u);

            capVertices.Add(new CapVertex(position, angle));
            index++;
        }

        capVertices.Sort(CapVertex.CompareByAngle);

        if (capVertices.Count >= 3)
        {
            Vector3 cross = Vector3.Cross(capVertices[1].Position - center, capVertices[2].Position - center);
            float direction = Vector3.Dot(cross, capNormal);
            if (direction < 0f)
            {
                capVertices.Reverse();
            }

            VertexData centerVertex = new VertexData(center, capNormal, new Vector2(Vector3.Dot(center, basisU), Vector3.Dot(center, basisV)));

            int vertexIndex = 0;
            while (vertexIndex < capVertices.Count)
            {
                int nextIndex = (vertexIndex + 1) % capVertices.Count;

                Vector3 positionA = capVertices[vertexIndex].Position;
                Vector3 positionB = capVertices[nextIndex].Position;

                VertexData vertexA = new VertexData(positionA, capNormal, new Vector2(Vector3.Dot(positionA, basisU), Vector3.Dot(positionA, basisV)));
                VertexData vertexB = new VertexData(positionB, capNormal, new Vector2(Vector3.Dot(positionB, basisU), Vector3.Dot(positionB, basisV)));

                meshBuilder.AddTriangle(capSubMeshIndex, centerVertex, vertexA, vertexB);

                vertexIndex++;
            }
        }
    }

    private static List<Vector3Key> BuildLoop(Dictionary<Vector3Key, List<Vector3Key>> adjacency, HashSet<Vector3Key> unprocessed, Vector3Key startKey)
    {
        List<Vector3Key> loop = new List<Vector3Key>();

        Vector3Key currentKey = startKey;
        Vector3Key previousKey = default(Vector3Key);
        bool previousIsSet = false;

        loop.Add(currentKey);
        unprocessed.Remove(currentKey);

        while (true)
        {
            List<Vector3Key> neighbors = adjacency[currentKey];
            if (neighbors.Count < 1)
            {
                break;
            }

            Vector3Key nextKey = neighbors[0];

            if (previousIsSet == true && neighbors.Count >= 2)
            {
                if (neighbors[0].Equals(previousKey) == true)
                {
                    nextKey = neighbors[1];
                }
                else
                {
                    nextKey = neighbors[0];
                }
            }

            if (nextKey.Equals(startKey) == true)
            {
                break;
            }

            previousKey = currentKey;
            previousIsSet = true;
            currentKey = nextKey;

            if (unprocessed.Contains(currentKey) == true)
            {
                unprocessed.Remove(currentKey);
            }

            loop.Add(currentKey);
        }

        return loop;
    }

    private static Vector3Key GetFirst(HashSet<Vector3Key> set)
    {
        IEnumerator<Vector3Key> enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        Vector3Key value = enumerator.Current;
        enumerator.Dispose();
        return value;
    }

    private static void AddNeighbor(Dictionary<Vector3Key, List<Vector3Key>> adjacency, Vector3Key from, Vector3Key to)
    {
        if (adjacency.TryGetValue(from, out List<Vector3Key> list) == false)
        {
            list = new List<Vector3Key>();
            adjacency.Add(from, list);
        }

        list.Add(to);
    }

    private readonly struct CapVertex
    {
        public readonly Vector3 Position;
        public readonly float Angle;

        public CapVertex(Vector3 position, float angle)
        {
            Position = position;
            Angle = angle;
        }

        public static int CompareByAngle(CapVertex a, CapVertex b)
        {
            return a.Angle.CompareTo(b.Angle);
        }
    }
}

internal sealed class MeshBuilder
{
    private readonly List<Vector3> _positions;
    private readonly List<Vector3> _normals;
    private readonly List<Vector2> _uvs;
    private readonly List<List<int>> _triangles;

    public MeshBuilder(int subMeshCount)
    {
        _positions = new List<Vector3>();
        _normals = new List<Vector3>();
        _uvs = new List<Vector2>();
        _triangles = new List<List<int>>(subMeshCount);

        int subMeshIndex = 0;
        while (subMeshIndex < subMeshCount)
        {
            _triangles.Add(new List<int>());
            subMeshIndex++;
        }
    }

    public void AddTriangle(int subMeshIndex, VertexData a, VertexData b, VertexData c)
    {
        int indexA = AddVertex(a);
        int indexB = AddVertex(b);
        int indexC = AddVertex(c);

        _triangles[subMeshIndex].Add(indexA);
        _triangles[subMeshIndex].Add(indexB);
        _triangles[subMeshIndex].Add(indexC);
    }

    private int AddVertex(VertexData vertex)
    {
        int index = _positions.Count;
        _positions.Add(vertex.Position);
        _normals.Add(vertex.Normal);
        _uvs.Add(vertex.Uv);
        return index;
    }

    public MeshData BuildMeshData()
    {
        Vector3[] positions = _positions.ToArray();
        Vector3[] normals = _normals.ToArray();
        Vector2[] uvs = _uvs.ToArray();

        int[][] triangles = new int[_triangles.Count][];
        int subMeshIndex = 0;
        while (subMeshIndex < _triangles.Count)
        {
            triangles[subMeshIndex] = _triangles[subMeshIndex].ToArray();
            subMeshIndex++;
        }

        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        if (positions.Length > 0)
        {
            bounds = new Bounds(positions[0], Vector3.zero);
            int i = 1;
            while (i < positions.Length)
            {
                bounds.Encapsulate(positions[i]);
                i++;
            }
        }

        return new MeshData(positions, normals, uvs, triangles, bounds);
    }
}

internal readonly struct MeshData
{
    public readonly Vector3[] Positions;
    public readonly Vector3[] Normals;
    public readonly Vector2[] Uvs;
    public readonly int[][] Triangles;
    public readonly Bounds Bounds;

    public int VertexCount => Positions.Length;

    public MeshData(Vector3[] positions, Vector3[] normals, Vector2[] uvs, int[][] triangles, Bounds bounds)
    {
        Positions = positions;
        Normals = normals;
        Uvs = uvs;
        Triangles = triangles;
        Bounds = bounds;
    }

    public static MeshData FromMesh(Mesh mesh, int targetSubMeshCount)
    {
        Vector3[] positions = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector2[] uvs = mesh.uv;

        int[][] triangles = new int[targetSubMeshCount][];

        int subMeshIndex = 0;
        while (subMeshIndex < targetSubMeshCount)
        {
            if (subMeshIndex < mesh.subMeshCount)
            {
                triangles[subMeshIndex] = mesh.GetTriangles(subMeshIndex);
            }
            else
            {
                triangles[subMeshIndex] = Array.Empty<int>();
            }

            subMeshIndex++;
        }

        return new MeshData(positions, normals, uvs, triangles, mesh.bounds);
    }

    public VertexData GetVertex(int index)
    {
        Vector3 position = Positions[index];
        Vector3 normal = Normals.Length > 0 ? Normals[index] : Vector3.up;
        Vector2 uv = Uvs.Length > 0 ? Uvs[index] : Vector2.zero;

        return new VertexData(position, normal, uv);
    }

    public Mesh ToMesh(string meshName, int subMeshCount)
    {
        Mesh mesh = new Mesh();
        mesh.name = meshName;

        if (Positions.Length > 65535)
        {
            mesh.indexFormat = IndexFormat.UInt32;
        }

        mesh.vertices = Positions;
        mesh.normals = Normals.Length > 0 ? Normals : null;
        mesh.uv = Uvs.Length > 0 ? Uvs : null;

        mesh.subMeshCount = subMeshCount;

        int subMeshIndex = 0;
        while (subMeshIndex < subMeshCount)
        {
            int[] triangles = subMeshIndex < Triangles.Length ? Triangles[subMeshIndex] : Array.Empty<int>();
            mesh.SetTriangles(triangles, subMeshIndex, false);
            subMeshIndex++;
        }

        mesh.RecalculateBounds();
        return mesh;
    }
}

internal readonly struct VertexData
{
    public readonly Vector3 Position;
    public readonly Vector3 Normal;
    public readonly Vector2 Uv;

    public VertexData(Vector3 position, Vector3 normal, Vector2 uv)
    {
        Position = position;
        Normal = normal;
        Uv = uv;
    }
}

internal readonly struct CutSegment
{
    public readonly Vector3 PointA;
    public readonly Vector3 PointB;

    public CutSegment(Vector3 pointA, Vector3 pointB)
    {
        PointA = pointA;
        PointB = pointB;
    }
}

internal readonly struct Vector3Key : IEquatable<Vector3Key>
{
    public readonly int X;
    public readonly int Y;
    public readonly int Z;

    public Vector3Key(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static Vector3Key From(Vector3 position, float quantizeScale)
    {
        int x = Mathf.RoundToInt(position.x * quantizeScale);
        int y = Mathf.RoundToInt(position.y * quantizeScale);
        int z = Mathf.RoundToInt(position.z * quantizeScale);
        return new Vector3Key(x, y, z);
    }

    public bool Equals(Vector3Key other)
    {
        return X == other.X && Y == other.Y && Z == other.Z;
    }

    public override bool Equals(object obj)
    {
        if (obj is Vector3Key other)
        {
            return Equals(other);
        }

        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + X;
            hash = hash * 31 + Y;
            hash = hash * 31 + Z;
            return hash;
        }
    }
}

internal readonly struct EdgeKey : IEquatable<EdgeKey>
{
    public readonly Vector3Key A;
    public readonly Vector3Key B;

    public EdgeKey(Vector3Key first, Vector3Key second)
    {
        if (Compare(first, second) <= 0)
        {
            A = first;
            B = second;
        }
        else
        {
            A = second;
            B = first;
        }
    }

    public bool Equals(EdgeKey other)
    {
        return A.Equals(other.A) == true && B.Equals(other.B) == true;
    }

    public override bool Equals(object obj)
    {
        if (obj is EdgeKey other)
        {
            return Equals(other);
        }

        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 23;
            hash = hash * 31 + A.GetHashCode();
            hash = hash * 31 + B.GetHashCode();
            return hash;
        }
    }

    private static int Compare(Vector3Key left, Vector3Key right)
    {
        if (left.X != right.X) return left.X.CompareTo(right.X);
        if (left.Y != right.Y) return left.Y.CompareTo(right.Y);
        return left.Z.CompareTo(right.Z);
    }
}
#endif