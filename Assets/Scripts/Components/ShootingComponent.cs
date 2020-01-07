using Unity.Entities;

namespace Components
{
    public struct ShootingComponent : IComponentData
    {
        public Entity Bullet;
        public Entity Explosion;
    }
}