using UnityEngine;

public class SimplifiedWalkingDirection : MonoBehaviour
{
    [SerializeField]
    private Transform _headset;
    [SerializeField]
    private CircleTrack circularTrack;
    [SerializeField] private StraightTrack straightTrack;
    [SerializeField] private Transform standing;
    public ExperimentManager.Context track = ExperimentManager.Context.Standing;

    private void Start()
    {
        // Series of sanity checks
        if (_headset == null && (_headset = Camera.main?.transform) == null)
        {
            Debug.LogError($"{nameof(SimplifiedWalkingDirection)}: couldn't find the main camera in the scene. Disabling the script");
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (track == ExperimentManager.Context.Circle)
        {
            var t = circularTrack.WalkingDirection();
            transform.rotation = t.rotation;
            transform.position = t.position;
            return;
        }
        if (track == ExperimentManager.Context.Walking)
        {
            var t = straightTrack.WalkingDirection();
            transform.rotation = t.rotation;
            transform.position = t.position;
            return;
        }
        // Check which direction, relative to the track orientation, the user is looking
        bool sameDirection = Vector3.Dot(_headset.forward, standing.forward) >= 0;
        // Always parallel to the track
        transform.rotation = Quaternion.LookRotation(sameDirection ? standing.forward : -standing.forward);
        transform.position = standing.position;
    }
}
