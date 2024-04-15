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

    private void Update()
    {
        if (track == ExperimentManager.Context.Circle)
        {
            var t = circularTrack.WalkingDirection(_headset);
            transform.rotation = t.rotation;
            transform.position = t.position;
            return;
        }
        if (track == ExperimentManager.Context.Walking)
        {
            var t = straightTrack.WalkingDirection(_headset);
            transform.rotation = t.rotation;
            transform.position = t.position;
            return;
        }
        // Check which direction, relative to the track orientation, the user is looking
        bool sameDirection = Vector3.Dot(_headset.forward, standing.forward) >= 0;

        var headsetLocally = standing.InverseTransformPoint(_headset.position);
        // Always parallel to the track
        transform.rotation = Quaternion.LookRotation(sameDirection ? standing.forward : -standing.forward);
        transform.position = standing.position + standing.rotation * new Vector3(0, 0, headsetLocally.z);
    }
}
