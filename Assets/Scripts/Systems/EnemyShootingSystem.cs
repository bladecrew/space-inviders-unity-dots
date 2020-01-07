using Components;
using Unity.Entities;
using Unity.Transforms;

namespace Systems
{
    public class EnemyShootingSystem : ComponentSystem
    {
        private BeginInitializationEntityCommandBufferSystem _bufferSystem;

        protected override void OnCreate()
        {
            _bufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((ref LocalToWorld position, ref ShootingComponent shootingComponent, ref EnemyComponent enemyComponent) =>
            {
                var shootingPeriod = enemyComponent.ShootingPeriodDynamic + Time.DeltaTime;
                if (shootingPeriod < enemyComponent.ShootingPeriod)
                {
                    enemyComponent.ShootingPeriodDynamic = shootingPeriod;
                    return;
                }

                enemyComponent.ShootingPeriodDynamic = 0f;

                var commandBuffer = _bufferSystem.CreateCommandBuffer();
                var entity = commandBuffer.Instantiate(shootingComponent.Bullet);
                var localToWorld = new Translation
                {
                    Value = position.Position
                };
                localToWorld.Value.y += -1f;
                

                commandBuffer.SetComponent(entity, localToWorld);
            });
        }
    }
}