using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Room/Interior/Chunk Installers/Dynamic On Damage")]
public sealed class RoomInteriorChunkDynamicOnDamageInstaller : ChunkRootInstaller
{
    [SerializeField, Min(1)] private int _minHealth = 30;
    [SerializeField, Min(1)] private int _maxHealth = 50;
    [SerializeField] private GameObject _feedbackPrefab;
    [SerializeField] private GameObject _popupPrefab;
    [SerializeField, Min(0f)] private float _popupOffsetY = 0.35f;

    public override void Install(ref ChunkRootContext context)
    {
        if (context.VariantSwitcher == null)
        {
            throw new InvalidOperationException(nameof(context.VariantSwitcher));
        }

        if (_maxHealth < _minHealth)
        {
            throw new InvalidOperationException(nameof(_maxHealth));
        }

        Health health = context.RootObject.GetComponent<Health>();

        if (health == null)
        {
            health = context.RootObject.AddComponent<Health>();
        }

        int healthValue = UnityEngine.Random.Range(_minHealth, _maxHealth + 1);

        health.SetAutoRegen(false);
        health.SetValue(healthValue);

        InstallPopup(context, health);
        InstallFeedback(context.StaticVisualObject, health, false);
        InstallFeedback(context.NotStaticVisualObject, health, true);

        RoomInteriorChunkDynamicOnDamage chunkDynamicOnDamage = context.RootObject.GetComponent<RoomInteriorChunkDynamicOnDamage>();

        if (chunkDynamicOnDamage == null)
        {
            chunkDynamicOnDamage = context.RootObject.AddComponent<RoomInteriorChunkDynamicOnDamage>();
        }

        chunkDynamicOnDamage.Initialize(context.VariantSwitcher, context.StaticVisualObject, context.NotStaticVisualObject, health);
    }

    private void InstallPopup(ChunkRootContext context, Health health)
    {
        if (_popupPrefab == null)
        {
            throw new MissingReferenceException(nameof(_popupPrefab));
        }

        GameObject popupObject = UnityEngine.Object.Instantiate(_popupPrefab, context.RootObject.transform);
        popupObject.name = _popupPrefab.name;

        Transform popupTransform = popupObject.transform;
        popupTransform.localPosition = GetPopupLocalPosition(context.StaticVisualObject);
        popupTransform.localRotation = Quaternion.identity;
        popupTransform.localScale = Vector3.one;

        DamagePopupOnHealth damagePopupOnHealth = popupObject.GetComponent<DamagePopupOnHealth>();

        if (damagePopupOnHealth == null)
        {
            throw new MissingReferenceException(nameof(damagePopupOnHealth));
        }

        damagePopupOnHealth.Initialize(health, popupTransform);
    }

    private void InstallFeedback(GameObject visualObject, Health health, bool useShake)
    {
        if (_feedbackPrefab == null)
        {
            throw new MissingReferenceException(nameof(_feedbackPrefab));
        }

        if (visualObject == null)
        {
            throw new MissingReferenceException(nameof(visualObject));
        }

        GameObject feedbackObject = UnityEngine.Object.Instantiate(_feedbackPrefab, visualObject.transform);
        feedbackObject.name = _feedbackPrefab.name;
        feedbackObject.transform.localPosition = Vector3.zero;
        feedbackObject.transform.localRotation = Quaternion.identity;
        feedbackObject.transform.localScale = Vector3.one;

        HealthFeedbackTrigger healthFeedbackTrigger = feedbackObject.GetComponent<HealthFeedbackTrigger>();

        if (healthFeedbackTrigger == null)
        {
            throw new MissingReferenceException(nameof(healthFeedbackTrigger));
        }

        FeedbackGroup feedbackGroup = feedbackObject.GetComponent<FeedbackGroup>();

        if (feedbackGroup == null)
        {
            throw new MissingReferenceException(nameof(feedbackGroup));
        }

        MeshRenderer meshRenderer = visualObject.GetComponent<MeshRenderer>();

        if (meshRenderer == null)
        {
            throw new MissingReferenceException(nameof(meshRenderer));
        }

        ColorEmissionFeedback colorEmissionFeedback = feedbackObject.GetComponent<ColorEmissionFeedback>();

        if (colorEmissionFeedback == null)
        {
            throw new MissingReferenceException(nameof(colorEmissionFeedback));
        }

        colorEmissionFeedback.Initialize(meshRenderer);

        ShakeFeedback shakeFeedback = feedbackObject.GetComponent<ShakeFeedback>();

        if (shakeFeedback != null)
        {
            shakeFeedback.Initialize(visualObject.transform, visualObject.transform);
        }

        if (useShake && shakeFeedback != null)
        {
            feedbackGroup.Initialize(new Feedback[] { shakeFeedback, colorEmissionFeedback });
        }
        else
        {
            feedbackGroup.Initialize(new Feedback[] { colorEmissionFeedback });
        }

        healthFeedbackTrigger.Initialize(health, feedbackGroup);
    }

    private Vector3 GetPopupLocalPosition(GameObject visualObject)
    {
        if (visualObject == null)
        {
            throw new MissingReferenceException(nameof(visualObject));
        }

        MeshFilter meshFilter = visualObject.GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            throw new MissingReferenceException(nameof(meshFilter));
        }

        if (meshFilter.sharedMesh == null)
        {
            throw new MissingReferenceException(nameof(meshFilter.sharedMesh));
        }

        Bounds meshBounds = meshFilter.sharedMesh.bounds;
        Vector3 popupLocalPosition = meshBounds.center;
        popupLocalPosition.y = meshBounds.max.y + _popupOffsetY;

        return popupLocalPosition;
    }
}
