using UnityEngine;
using UnityEngine.Animations;

[RequireComponent(typeof(LookAtConstraint))]
public class LookAtMainCameraAssigner : MonoBehaviour
{
    private LookAtConstraint _lookAtConstraint;

    private void Awake()
    {
        _lookAtConstraint = GetComponent<LookAtConstraint>();

        if (_lookAtConstraint == null)
        {
            throw new MissingComponentException(nameof(_lookAtConstraint));
        }

        if (Camera.main == null)
        {
            throw new MissingComponentException(nameof(Camera.main));
        }

        ConstraintSource cameraSource = new ConstraintSource();
        cameraSource.sourceTransform = Camera.main.transform;
        cameraSource.weight = 1f;

        _lookAtConstraint.AddSource(cameraSource);
        _lookAtConstraint.constraintActive = true;
    }
}