using Data;
using Unity.Entities;
using Unity.Transforms;

namespace Systems
{
    public class BulletMovementSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ref Translation translation, ref BulletData bulletData) =>
            {
                translation.Value.y += bulletData.IsEnemyBullet
                    ? -Time.DeltaTime * bulletData.MovementSpeed
                    : Time.DeltaTime * bulletData.MovementSpeed;
            });
        }
    }
}