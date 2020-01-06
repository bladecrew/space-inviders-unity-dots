using Unity.Entities;

namespace Data
{
    public struct InputComponent : IComponentData
    {
        public bool IsShooting;
        public float InputX;
    }
}
