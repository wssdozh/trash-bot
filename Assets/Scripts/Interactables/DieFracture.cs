using Unity.AI.Navigation;
using UnityEngine;

public sealed class DieFracture : MonoBehaviour
{
    [SerializeField] private Health _health;

    [SerializeField] private GameObject _intactObject;
    [SerializeField] private GameObject _fracturedObject;

    [SerializeField] private FractureFx _fractureFx;

    private bool _swapped = false;

    private void OnEnable()
    {
        if (_health == null)
        {
            _health = GetComponentInChildren<Health>(true);
        }

        if (_fractureFx == null && _fracturedObject != null)
        {
            _fractureFx = _fracturedObject.GetComponent<FractureFx>();
        }

        EnsureFracturedNavMeshIgnore();

        if (_health != null)
        {
            _health.Ended += OnHealthEnded;
        }
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.Ended -= OnHealthEnded;
        }
    }

    private void OnHealthEnded()
    {
        if (_swapped)
        {
            return;
        }

        _swapped = true;

        if (_fracturedObject != null)
        {
            _fracturedObject.transform.position = _intactObject.transform.position;
            _fracturedObject.transform.rotation = _intactObject.transform.rotation;
            _fracturedObject.transform.localScale = _intactObject.transform.localScale;
            _fracturedObject.SetActive(true);

            if (_fractureFx == null)
            {
                _fractureFx = _fracturedObject.GetComponentInChildren<FractureFx>(true);
            }

            if (_fractureFx != null)
            {
                _fractureFx.Play();
            }
        }

        if (_intactObject != null)
        {
            _intactObject.SetActive(false);
        }

        RequestNavMeshUpdate();
    }

    private void EnsureFracturedNavMeshIgnore()
    {
        if (_fracturedObject == null)
        {
            return;
        }

        NavMeshModifier navMeshModifier = _fracturedObject.GetComponent<NavMeshModifier>();

        if (navMeshModifier == null)
        {
            navMeshModifier = _fracturedObject.AddComponent<NavMeshModifier>();
        }

        navMeshModifier.ignoreFromBuild = true;
        navMeshModifier.applyToChildren = true;
    }

    private void RequestNavMeshUpdate()
    {
        LevelRuntimeNavMesh levelRuntimeNavMesh = GetComponentInParent<LevelRuntimeNavMesh>();

        if (levelRuntimeNavMesh == null)
        {
            return;
        }

        levelRuntimeNavMesh.RequestUpdate();
    }
}
