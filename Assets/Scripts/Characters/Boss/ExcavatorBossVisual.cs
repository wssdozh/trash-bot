using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class ExcavatorBossVisual : MonoBehaviour
{
    private const float ModelOffsetY = -0.22f;
    private const string ModelName = "Model";
    private const string BodyName = "Body";
    private const string LeftTrackName = "Left Track";
    private const string RightTrackName = "Right Track";
    private const string CabinPivotName = "Cabin Pivot";
    private const string CabinName = "Cabin";
    private const string CounterWeightName = "Counter Weight";
    private const string ArmPivotName = "Arm Pivot";
    private const string ArmName = "Arm";
    private const string ForearmPivotName = "Forearm Pivot";
    private const string ForearmName = "Forearm";
    private const string BucketPivotName = "Bucket Pivot";
    private const string BucketName = "Bucket";
    private const string ToothLeftName = "Tooth Left";
    private const string ToothMiddleName = "Tooth Middle";
    private const string ToothRightName = "Tooth Right";
    private const string PipeLeftName = "Pipe Left";
    private const string PipeRightName = "Pipe Right";
    private const string LampName = "Lamp";
    private const string AttackPointName = "Attack Point";

    [Header("Colors")]
    [SerializeField] private Color _bodyColor = new Color(0.58f, 0.46f, 0.18f);
    [SerializeField] private Color _trackColor = new Color(0.15f, 0.15f, 0.17f);
    [SerializeField] private Color _metalColor = new Color(0.36f, 0.34f, 0.31f);
    [SerializeField] private Color _warningColor = new Color(0.86f, 0.33f, 0.15f);

    private Transform _modelRoot;
    private Transform _cabinPivot;
    private Transform _armPivot;
    private Transform _forearmPivot;
    private Transform _bucketPivot;
    private Transform _attackPoint;

    private MaterialPropertyBlock _propertyBlock;
    private bool _isEditorRefreshQueued;

    public Transform AttackPoint
    {
        get
        {
            if (CanBuildVisual() == false)
            {
                return _attackPoint;
            }

            EnsureVisual();

            return _attackPoint;
        }
    }

    private void Reset()
    {
        if (CanBuildVisual() == false)
        {
            return;
        }

        EnsureVisual();
        ApplyDefaultPose();
    }

    private void Awake()
    {
        if (CanBuildVisual() == false)
        {
            return;
        }

        EnsureVisual();
        ApplyDefaultPose();
    }

    private void OnValidate()
    {
        QueueRefresh();
    }

    public void SetCabinYaw(float angle)
    {
        if (CanBuildVisual() == false)
        {
            return;
        }

        EnsureVisual();
        _cabinPivot.localRotation = Quaternion.Euler(0f, angle, 0f);
    }

    public void SetArmAngles(float armAngle, float forearmAngle, float bucketAngle)
    {
        if (CanBuildVisual() == false)
        {
            return;
        }

        EnsureVisual();
        _armPivot.localRotation = Quaternion.Euler(armAngle, 0f, 0f);
        _forearmPivot.localRotation = Quaternion.Euler(forearmAngle, 0f, 0f);
        _bucketPivot.localRotation = Quaternion.Euler(bucketAngle, 0f, 0f);
    }

    private void ApplyDefaultPose()
    {
        SetCabinYaw(0f);
        SetArmAngles(14f, -18f, 26f);
    }

    private void QueueRefresh()
    {
        if (CanBuildVisual() == false)
        {
            return;
        }

        if (Application.isPlaying)
        {
            EnsureVisual();
            ApplyDefaultPose();

            return;
        }

#if UNITY_EDITOR
        if (_isEditorRefreshQueued)
        {
            return;
        }

        _isEditorRefreshQueued = true;
        EditorApplication.delayCall += RefreshEditorVisual;
#endif
    }

#if UNITY_EDITOR
    private void RefreshEditorVisual()
    {
        EditorApplication.delayCall -= RefreshEditorVisual;
        _isEditorRefreshQueued = false;

        if (this == null)
        {
            return;
        }

        if (CanBuildVisual() == false)
        {
            return;
        }

        EnsureVisual();
        ApplyDefaultPose();
    }
#endif

    private bool CanBuildVisual()
    {
        if (Application.isPlaying)
        {
            return true;
        }

        if (gameObject.scene.IsValid() == false)
        {
            return false;
        }

#if UNITY_EDITOR
        if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
        {
            return false;
        }
#endif

        return true;
    }

    private void EnsureVisual()
    {
        _modelRoot = GetOrCreateEmpty(transform, ModelName);
        _modelRoot.localPosition = new Vector3(0f, ModelOffsetY, 0f);
        _modelRoot.localRotation = Quaternion.identity;
        _modelRoot.localScale = Vector3.one;
        Transform body = GetOrCreatePrimitive(_modelRoot, BodyName, PrimitiveType.Cube);
        Transform leftTrack = GetOrCreatePrimitive(_modelRoot, LeftTrackName, PrimitiveType.Cube);
        Transform rightTrack = GetOrCreatePrimitive(_modelRoot, RightTrackName, PrimitiveType.Cube);
        _cabinPivot = GetOrCreateEmpty(_modelRoot, CabinPivotName);
        Transform cabin = GetOrCreatePrimitive(_cabinPivot, CabinName, PrimitiveType.Cube);
        Transform counterWeight = GetOrCreatePrimitive(_cabinPivot, CounterWeightName, PrimitiveType.Cube);
        _armPivot = GetOrCreateEmpty(_cabinPivot, ArmPivotName);
        Transform arm = GetOrCreatePrimitive(_armPivot, ArmName, PrimitiveType.Cube);
        _forearmPivot = GetOrCreateEmpty(_armPivot, ForearmPivotName);
        Transform forearm = GetOrCreatePrimitive(_forearmPivot, ForearmName, PrimitiveType.Cube);
        _bucketPivot = GetOrCreateEmpty(_forearmPivot, BucketPivotName);
        Transform bucket = GetOrCreatePrimitive(_bucketPivot, BucketName, PrimitiveType.Cube);
        Transform toothLeft = GetOrCreatePrimitive(_bucketPivot, ToothLeftName, PrimitiveType.Cube);
        Transform toothMiddle = GetOrCreatePrimitive(_bucketPivot, ToothMiddleName, PrimitiveType.Cube);
        Transform toothRight = GetOrCreatePrimitive(_bucketPivot, ToothRightName, PrimitiveType.Cube);
        Transform pipeLeft = GetOrCreatePrimitive(_modelRoot, PipeLeftName, PrimitiveType.Cylinder);
        Transform pipeRight = GetOrCreatePrimitive(_modelRoot, PipeRightName, PrimitiveType.Cylinder);
        Transform lamp = GetOrCreatePrimitive(_cabinPivot, LampName, PrimitiveType.Cube);
        _attackPoint = GetOrCreateEmpty(_bucketPivot, AttackPointName);

        ApplyPart(body, new Vector3(0f, 0.8f, 0f), new Vector3(2.8f, 1.15f, 4.1f), _bodyColor);
        ApplyPart(leftTrack, new Vector3(-1.55f, 0.35f, 0f), new Vector3(0.7f, 0.5f, 4.4f), _trackColor);
        ApplyPart(rightTrack, new Vector3(1.55f, 0.35f, 0f), new Vector3(0.7f, 0.5f, 4.4f), _trackColor);

        _cabinPivot.localPosition = new Vector3(0f, 1.32f, 0.3f);
        _cabinPivot.localRotation = Quaternion.identity;
        _cabinPivot.localScale = Vector3.one;

        ApplyPart(cabin, new Vector3(0f, 0.25f, 0f), new Vector3(1.35f, 1f, 1.35f), _bodyColor);
        ApplyPart(counterWeight, new Vector3(0f, 0.05f, -0.95f), new Vector3(1.1f, 0.55f, 0.75f), _metalColor);

        _armPivot.localPosition = new Vector3(0.82f, 0.22f, 0.28f);
        _armPivot.localRotation = Quaternion.identity;
        _armPivot.localScale = Vector3.one;

        ApplyPart(arm, new Vector3(0f, 0f, 0.82f), new Vector3(0.34f, 0.34f, 1.64f), _metalColor);

        _forearmPivot.localPosition = new Vector3(0f, 0f, 1.58f);
        _forearmPivot.localRotation = Quaternion.identity;
        _forearmPivot.localScale = Vector3.one;

        ApplyPart(forearm, new Vector3(0f, 0f, 0.74f), new Vector3(0.3f, 0.3f, 1.48f), _metalColor);

        _bucketPivot.localPosition = new Vector3(0f, 0f, 1.4f);
        _bucketPivot.localRotation = Quaternion.identity;
        _bucketPivot.localScale = Vector3.one;

        ApplyPart(bucket, new Vector3(0f, -0.05f, 0.38f), new Vector3(0.92f, 0.55f, 0.76f), _bodyColor);
        ApplyPart(toothLeft, new Vector3(-0.24f, -0.22f, 0.8f), new Vector3(0.12f, 0.22f, 0.18f), _warningColor);
        ApplyPart(toothMiddle, new Vector3(0f, -0.22f, 0.84f), new Vector3(0.12f, 0.22f, 0.18f), _warningColor);
        ApplyPart(toothRight, new Vector3(0.24f, -0.22f, 0.8f), new Vector3(0.12f, 0.22f, 0.18f), _warningColor);

        ApplyPart(pipeLeft, new Vector3(-0.45f, 1.55f, -1.18f), new Vector3(0.22f, 0.45f, 0.22f), _metalColor);
        ApplyPart(pipeRight, new Vector3(0.45f, 1.55f, -1.18f), new Vector3(0.22f, 0.56f, 0.22f), _metalColor);
        ApplyPart(lamp, new Vector3(0f, 0.18f, 0.78f), new Vector3(0.38f, 0.22f, 0.18f), _warningColor);

        _attackPoint.localPosition = new Vector3(0f, -0.05f, 0.98f);
        _attackPoint.localRotation = Quaternion.identity;
        _attackPoint.localScale = Vector3.one;
    }

    private Transform GetOrCreateEmpty(Transform parentTransform, string objectName)
    {
        Transform childTransform = parentTransform.Find(objectName);

        if (childTransform != null)
        {
            return childTransform;
        }

        GameObject childObject = new GameObject(objectName);
        Transform result = childObject.transform;
        result.SetParent(parentTransform, false);

        return result;
    }

    private Transform GetOrCreatePrimitive(Transform parentTransform, string objectName, PrimitiveType primitiveType)
    {
        Transform childTransform = parentTransform.Find(objectName);

        if (childTransform != null)
        {
            RemoveColliders(childTransform.gameObject);

            return childTransform;
        }

        GameObject childObject = GameObject.CreatePrimitive(primitiveType);
        childObject.name = objectName;
        childObject.transform.SetParent(parentTransform, false);
        RemoveColliders(childObject);

        return childObject.transform;
    }

    private void ApplyPart(Transform partTransform, Vector3 localPosition, Vector3 localScale, Color color)
    {
        partTransform.localPosition = localPosition;
        partTransform.localRotation = Quaternion.identity;
        partTransform.localScale = localScale;
        ApplyColor(partTransform, color);
    }

    private void ApplyColor(Transform partTransform, Color color)
    {
        Renderer partRenderer = partTransform.GetComponent<Renderer>();

        if (partRenderer == null)
        {
            return;
        }

        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        _propertyBlock.Clear();
        _propertyBlock.SetColor("_Color", color);
        partRenderer.SetPropertyBlock(_propertyBlock);
    }

    private void RemoveColliders(GameObject targetObject)
    {
        Collider[] colliders = targetObject.GetComponents<Collider>();

        for (int colliderIndex = colliders.Length - 1; colliderIndex >= 0; colliderIndex--)
        {
            Collider targetCollider = colliders[colliderIndex];

            if (Application.isPlaying)
            {
                Destroy(targetCollider);
            }
            else
            {
#if UNITY_EDITOR
                DestroyImmediate(targetCollider, true);
#else
                Destroy(targetCollider);
#endif
            }
        }
    }
}
