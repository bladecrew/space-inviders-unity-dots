using Data;
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
            Entities.ForEach((ref GameData gameData) =>
            {
                if (gameData.IsPaused)
                    return;

                var components = GetEntityQuery(new EntityQueryDesc
                {
                    All = new ComponentType[] {typeof(EnemyData)}
                });

                var entityCounts = components.CalculateEntityCount();
                if (entityCounts > 1 && gameData.CurrentEnemies > 1)
                    return;

                var commandBuffer = _bufferSystem.CreateCommandBuffer();

                // after system restarted
                if (entityCounts > 1 && gameData.CurrentEnemies <= 1)
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

                _CreateEnemies(gameData, commandBuffer);

                gameData.CurrentEnemies++;
            });
        }

        private static void _CreateEnemies(GameData gameData, EntityCommandBuffer commandBuffer)
        {
            for (var index = 0; index < gameData.CurrentEnemies; index++)
            {
                var enemyData = new EnemyData
                {
                    CurrentDirection = Maths.Roll()
                        ? EnemyMovementDirection.Left
                        : EnemyMovementDirection.Right
                };

                if (index >= 0 && index < 2)
                {
                    enemyData.LineChangingTime = Random.Range(0.5f, 0.7f);
                    enemyData.IsNonStop = false;
                }
                else if (index >= 2 && index < 4)
                {
                    enemyData.LineChangingTime = Random.Range(0.3f, 0.4f);
                    enemyData.IsNonStop = false;
                }
                else if (index >= 4 && index < 6)
                {
                    enemyData.LineChangingTime = Random.Range(0.3f, 0.4f);
                    enemyData.IsNonStop = true;
                    enemyData.SerpentineDegree = Random.Range(10, 22);
                }
                else
                {
                    enemyData.LineChangingTime = Random.Range(0.3f, 0.4f);
                    enemyData.IsNonStop = true;
                    enemyData.SerpentineDegree = Random.Range(10, 22);
                }

                enemyData.ShootingPeriod = 2f;

                var entity = commandBuffer.Instantiate(gameData.Enemy);
                commandBuffer.SetComponent(entity, enemyData);

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