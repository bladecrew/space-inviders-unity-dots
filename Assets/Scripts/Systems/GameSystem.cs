using Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Utils;
using Random = UnityEngine.Random;

namespace Systems
{
    public class GameSystem : ComponentSystem
    {
        private EntityCommandBufferSystem _bufferSystem;
        private bool _isPaused;

        protected override void OnCreate()
        {
            _bufferSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((ref GameComponent gameComponent) =>
            {
                if (gameComponent.IsPaused)
                    return;

                var components = GetEntityQuery(new EntityQueryDesc
                {
                    All = new ComponentType[] {typeof(EnemyComponent)}
                });

                var entityCounts = components.CalculateEntityCount();
                if (entityCounts > 1 && gameComponent.CurrentEnemies > 1)
                    return;

                var commandBuffer = _bufferSystem.CreateCommandBuffer();

                // after system restarted
                if (entityCounts > 1 && gameComponent.CurrentEnemies <= 1)
                {
                    var entities = components.ToEntityArray(Allocator.TempJob);
                    var isFirstEntity = true;
                    foreach (var entity in entities)
                    {
                        if (isFirstEntity)
                        {
                            isFirstEntity = false;
                            continue;
                        }

                        commandBuffer.DestroyEntity(entity);
                    }

                    entities.Dispose();
                }

                _CreateEnemies(gameComponent, commandBuffer);

                gameComponent.CurrentEnemies++;
            });
        }

        private static void _CreateEnemies(GameComponent gameComponent, EntityCommandBuffer commandBuffer)
        {
            for (var index = 0; index < gameComponent.CurrentEnemies; index++)
            {
                var enemyComponent = new EnemyComponent
                {
                    CurrentDirection = Maths.Roll()
                        ? EnemyMovementDirection.Left
                        : EnemyMovementDirection.Right
                };

                if (index >= 0 && index < 2)
                {
                    enemyComponent.LineChangingTime = Random.Range(0.5f, 0.7f);
                    enemyComponent.IsNonStop = false;
                }
                else if (index >= 2 && index < 4)
                {
                    enemyComponent.LineChangingTime = Random.Range(0.3f, 0.4f);
                    enemyComponent.IsNonStop = false;
                }
                else if (index >= 4 && index < 6)
                {
                    enemyComponent.LineChangingTime = Random.Range(0.3f, 0.4f);
                    enemyComponent.IsNonStop = true;
                    enemyComponent.SerpentineDegree = Random.Range(10, 22);
                }
                else
                {
                    enemyComponent.LineChangingTime = Random.Range(0.3f, 0.4f);
                    enemyComponent.IsNonStop = true;
                    enemyComponent.SerpentineDegree = Random.Range(10, 22);
                }

                enemyComponent.ShootingPeriod = 2f;

                var entity = commandBuffer.Instantiate(gameComponent.Enemy);
                commandBuffer.SetComponent(entity, enemyComponent);

                _SetPosition(entity, commandBuffer);
            }
        }

        private static void _SetPosition(Entity entity, EntityCommandBuffer commandBuffer)
        {
            var bounds = SceneParams.CameraViewParams();
            var x = Random.Range(bounds.min.x, bounds.max.x);
            var y = bounds.max.y + 1f;

            var localToWorld = new Translation
            {
                Value = new float3(x, y, 0)
            };

            commandBuffer.SetComponent(entity, localToWorld);
        }
    }
}