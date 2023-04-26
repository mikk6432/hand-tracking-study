using UnityEngine;

namespace Utils
{
    public static class Math
    {
        public static (Vector3 world, Vector3 local) ProjectPointOntoOXYPlane(Transform transform, Vector3 point)
        {
            Vector3 normal = Vector3.Cross(transform.right, transform.up).normalized;
            
            float distance = Vector3.Dot(normal, point - transform.position);
            
            Vector3 projection = point - distance * normal;
            
            return (projection, transform.InverseTransformPoint(projection));
        }
    }
}