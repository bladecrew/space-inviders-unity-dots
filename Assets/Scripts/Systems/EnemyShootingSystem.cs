using Data;
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
            Entities.ForEach((ref LocalToWorld position, ref ShootingData shootingData, ref EnemyData enemyData) =>
            {
                var shootingPeriod = enemyData.ShootingPeriodDynamic + Time.DeltaTime;
                if (shootingPeriod < enemyData.ShootingPeriod)
                {
                    enemyData.ShootingPeriodDynamic = shootingPeriod;
                    return;
                }

                enemyData.ShootingPeriodDynamic = 0f;

                var commandBuffer = _bufferSystem.CreateCommandBuffer();
                var entity = commandBuffer.Instantiate(shootingData.Bullet);
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