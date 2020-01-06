using UnityEngine;

namespace Utils
{
    public static class CustomExtensions
    {
        public static Vector2 CalculateFromDegree(this Vector2 vector, float degree)
        {
            return new Vector2(vector.x, vector.y * degree * 0.005f);
        }
    }
}