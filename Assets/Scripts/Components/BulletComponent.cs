using Unity.Entities;

namespace Data
{
    public struct BulletComponent : IComponentData
    {
        public float MovementSpeed;
        public bool IsEnemyBullet;
    }
}