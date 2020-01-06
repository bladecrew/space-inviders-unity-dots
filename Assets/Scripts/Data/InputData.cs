using Unity.Entities;

namespace Data
{
    public struct InputData : IComponentData
    {
        public bool IsShooting;
        public float InputX;
    }
}
