using UnityEngine;

namespace SpatialUIPlacement
{
    public class ReferenceFrameIndicator : MonoBehaviour
    {
        private static readonly float LINE_LENGTH = 0.5f;
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + LINE_LENGTH * transform.right);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + LINE_LENGTH * transform.up);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + LINE_LENGTH * transform.forward);
        }
    }
}
