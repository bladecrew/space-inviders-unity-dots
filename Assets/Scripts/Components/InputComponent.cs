using Unity.Entities;

namespace Components
{
    public struct InputComponent : IComponentData
    {
        public bool IsShooting;
        public float InputX;
    }
}
