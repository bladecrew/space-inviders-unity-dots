using Unity.Entities;

namespace Data
{
    public struct ShootingComponent : IComponentData
    {
        public Entity Bullet;
    }
}