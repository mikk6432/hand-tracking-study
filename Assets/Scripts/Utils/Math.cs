using System;
using System.Collections.Generic;
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
        
        public static T[] balancedLatinSquare<T>(T[] array, int participantID)
        {
            /*
             * REMARK: Odd sizes require twice as many rows to be balanced
             * https://cs.uwaterloo.ca/~dmasson/tools/latin_square/
             */
            var result = new T[array.Length];

            for (int i = 0, j = 0, h = 0; i < array.Length; i++)
            {
                int val = i < 2 || i % 2 != 0 ? 
                    j++ :
                    array.Length - 1 - h++;

                var idx = (val + participantID) % array.Length;
                result[i] = array[idx];
            }

            if (array.Length % 2 != 0 && participantID % 2 != 0) 
                Array.Reverse(result);
        
            return result;
        }

        public static IEnumerable<int> FittsLaw(int len)
        {
            if (len <= 0 || len % 2 == 0) throw new ArgumentException("FittsLaw can take only positive odd length");
            int current = 0;
            while (true)
            {
                yield return current;
                current = (current + len / 2) % len;
            }
        }
    }
}