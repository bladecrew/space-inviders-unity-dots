using Unity.Entities;

namespace Components
{
    public struct ExplosionComponent : IComponentData
    {
        public float DestroyTime;
        public float DestroyTimeDynamic;
    }
}