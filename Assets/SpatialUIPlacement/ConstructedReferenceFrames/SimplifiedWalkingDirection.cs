using UnityEngine;

public class SimplifiedWalkingDirection : MonoBehaviour
{
#pragma warning disable 649
    [SerializeField]
    private Transform _headset;
    [SerializeField]
    [Tooltip("The game object along which the participant walk")]
    private Transform _track;
#pragma warning restore 649

    private void Start()
    {
        // Series of sanity checks
        if (_headset == null && (_headset = Camera.main?.transform) == null)
        {
            Debug.LogError($"{nameof(SimplifiedWalkingDirection)}: couldn't find the main camera in the scene. Disabling the script");
            enabled = false;
            return;
        }

        if (_track == null)
        {
            Debug.LogError($"{nameof(SimplifiedWalkingDirection)}: the '{nameof(_track)}' object cannot be left unassigned. Disabling the script");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        // Check which direction, relative to the track orientation, the user is looking
        bool sameDirection = Vector3.Dot(_headset.forward, _track.forward) >= 0;
        // Always parallel to the track
        transform.rotation = Quaternion.LookRotation(sameDirection ? _track.forward : -_track.forward);
    }
}
