using Components;
using Unity.Entities;
using Unity.Transforms;

namespace Systems
{
    public class BulletMovementSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ref Translation translation, ref BulletComponent bulletComponent) =>
            {
                translation.Value.y += bulletComponent.IsEnemyBullet
                    ? -Time.DeltaTime * bulletComponent.MovementSpeed
                    : Time.DeltaTime * bulletComponent.MovementSpeed;
            });
        }
    }
}