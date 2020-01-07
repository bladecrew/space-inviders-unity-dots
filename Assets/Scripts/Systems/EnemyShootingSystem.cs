using System.Linq;
using Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.SceneManagement;
using Utils;

namespace Systems
{
    [UpdateBefore(typeof(EnemyMovementSystem))]
    [UpdateBefore(typeof(CollisionsSystem))]
    public class EnemyShootingSystem : ComponentSystem
    {
        private BeginInitializationEntityCommandBufferSystem _bufferSystem;
        private EntityQuery _enemies;

        protected override void OnCreate()
        {
            var query = new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(LocalToWorld), typeof(ShootingComponent), typeof(EnemyComponent), typeof(Translation)
                }
            };

            _enemies = GetEntityQuery(query);
            _bufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var bounds = SceneParams.CameraViewParams();
            var entities = _enemies.ToEntityArray(Allocator.TempJob);
            var positions = _enemies.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);
            var translations = _enemies.ToComponentDataArray<Translation>(Allocator.TempJob);
            var shootingComponents = _enemies.ToComponentDataArray<ShootingComponent>(Allocator.TempJob);
            var enemyComponents = _enemies.ToComponentDataArray<EnemyComponent>(Allocator.TempJob);

            var commandBuffer = _bufferSystem.CreateCommandBuffer();

            for (var i = 0; i < positions.Length; i++)
            {
                var translation = translations[i];
                if(translation.Value.y >= bounds.max.y|| translations.Any(t => t.Value.x == translation.Value.x && t.Value.y < translation.Value.y))
                    continue;
                
                var position = positions[i];
                
                var shootingComponent = shootingComponents[i];
                var enemyComponent = enemyComponents[i];
                var enemyEntity = entities[i];

                var shootingPeriod = enemyComponent.ShootingPeriodDynamic + Time.DeltaTime;
                if (shootingPeriod < enemyComponent.ShootingPeriod)
                {
                    enemyComponent.ShootingPeriodDynamic = shootingPeriod;
                    EntityManager.SetComponentData(enemyEntity, enemyComponent);
                    continue;
                }

                enemyComponent.ShootingPeriodDynamic = 0f;
                var bulletEntity = commandBuffer.Instantiate(shootingComponent.Bullet);
                var localToWorld = new Translation
                {
                    Value = position.Position
                };
                
                localToWorld.Value.y += -1f;

                commandBuffer.SetComponent(bulletEntity, localToWorld);
                EntityManager.SetComponentData(enemyEntity, enemyComponent);
            }

            positions.Dispose();
            entities.Dispose();
            translations.Dispose();
            shootingComponents.Dispose();
            enemyComponents.Dispose();
        }
    }
}