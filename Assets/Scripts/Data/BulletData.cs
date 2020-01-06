using Unity.Entities;

namespace Data
{
    public struct BulletData : IComponentData
    {
        public float MovementSpeed;
        public bool IsEnemyBullet;
    }
}