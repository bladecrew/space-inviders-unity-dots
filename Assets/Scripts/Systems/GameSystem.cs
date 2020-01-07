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
                var components = GetEntityQuery(new EntityQueryDesc
                {
                    All = new ComponentType[] {typeof(EnemyComponent)}
                });

                var entityCounts = components.CalculateEntityCount();
                if (entityCounts > 0 && gameComponent.CurrentWave != 0)
                    return;

                var commandBuffer = _bufferSystem.CreateCommandBuffer();

                // after system restarted
                if (entityCounts > 0 && gameComponent.CurrentWave == 0)
                {
                    var entities = components.ToEntityArray(Allocator.TempJob);
                    foreach (var entity in entities)
                        commandBuffer.DestroyEntity(entity);

                    entities.Dispose();
                }

                gameComponent.CurrentWave++;

                _CreateEnemies(gameComponent, commandBuffer);
            });
        }

        private static void _CreateEnemies(GameComponent gameComponent, EntityCommandBuffer commandBuffer)
        {
            var requiredCount = gameComponent.DefaultEnemies + gameComponent.CurrentWave;
            var direction = Maths.Roll()
                ? EnemyMovementDirection.Left
                : EnemyMovementDirection.Right;
            const float enemyLineChangingTime = 0.8f;

            if (requiredCount > gameComponent.MaxEnemies)
                requiredCount = gameComponent.MaxEnemies;

            var bounds = SceneParams.CameraViewParams();

            for (var i = 0; i < requiredCount; i++)
            {
                for (var j = 0; j < requiredCount - 2; j++)
                {
                    var enemyComponent = new EnemyComponent
                    {
                        CurrentDirection = direction,
                        LineChangingTime = enemyLineChangingTime,
                        IsNonStop = false,
                        ShootingPeriod = 2f
                    };

                    var entity = commandBuffer.Instantiate(gameComponent.Enemy);
                    commandBuffer.SetComponent(entity, enemyComponent);

                    var movementComponent = new MovementComponent
                    {
                        MoveSpeed = gameComponent.EnemiesMoveSpeed + gameComponent.CurrentWave / 2f
                    };
                    commandBuffer.AddComponent(entity, movementComponent);

                    var x = i * 1f;
                    var y = bounds.max.y + j * 1f;

                    var localToWorld = new Translation
                    {
                        Value = new float3(x, y, 0)
                    };

                    commandBuffer.SetComponent(entity, localToWorld);
                }
            }
        }
    }
}