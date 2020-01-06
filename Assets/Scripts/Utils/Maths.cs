using Unity.Transforms;
using UnityEngine;

namespace Utils
{
    public static class Maths
    {
        public static bool Intersect(Translation box1, Translation box2)
        {
            var x1 = (int) box1.Value.x;
            var x2 = (int) box2.Value.x;
            var y1 = (int) box1.Value.y;
            var y2 = (int) box2.Value.y;
            var z1 = (int) box1.Value.z;
            var z2 = (int) box2.Value.z;

            return x1 == x2 && y1 == y2 && z1 == z2;
        }

        public static bool Roll()
        {
            return Random.Range(0, 2) == 0;
        }
    }
}