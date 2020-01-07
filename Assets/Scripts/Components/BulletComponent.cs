using Unity.Entities;

namespace Components
{
    public struct BulletComponent : IComponentData
    {
        public float MovementSpeed;
        public bool IsEnemyBullet;
    }
}