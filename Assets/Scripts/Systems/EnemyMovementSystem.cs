using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using Utils;

namespace Systems
{
    [UpdateBefore(typeof(CollisionsSystem))]
    public class EnemyMovementSystem : JobComponentSystem
    {
        private EntityQuery _entities;
        private EntityCommandBufferSystem _bufferSystem;

        protected override void OnCreate()
        {
            var query = new EntityQueryDesc
            {
                All = new ComponentType[] {typeof(Translation), typeof(MovementComponent), typeof(EnemyComponent)}
            };

            _entities = GetEntityQuery(query);
            _bufferSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
        }

        [BurstCompile]
        private struct EnemyMovementJob : IJobParallelFor
        {
            [WriteOnly] public EntityCommandBuffer.Concurrent CommandBuffer;

            [ReadOnly] public NativeArray<Entity> EnemyEntities;
            [ReadOnly] public NativeArray<Translation> EnemiesTranslations;
            [ReadOnly] public NativeArray<EnemyComponent> EnemiesComponents;
            [ReadOnly] public NativeArray<MovementComponent> MovementComponents;
            [ReadOnly] public float DeltaTime;

            [ReadOnly] public Bounds Bounds;

            public void Execute(int index)
            {
                _MoveEnemies(index);
            }

            private void _MoveEnemies(int index)
            {
                var firstEnemyComponent = EnemiesComponents[0];
                var bounds = Bounds;
                var needNormalizeLine = false;
                var canProcessLineChanging = false;

                for (var i = 0; i < EnemiesTranslations.Length; i++)
                {
                    var translation = EnemiesTranslations[i];

                    if (translation.Value.x < bounds.max.x && translation.Value.x > bounds.min.x)
                        continue;

                    needNormalizeLine = firstEnemyComponent.LineChangingTimeDynamic >=
                                        firstEnemyComponent.LineChangingTime;

                    canProcessLineChanging = !needNormalizeLine;

                    break;
                }

                for (var i = index; i < EnemiesTranslations.Length; i++)
                {
                    var translation = EnemiesTranslations[i];
                    var entity = EnemyEntities[i];
                    var enemyComponent = EnemiesComponents[i];
                    var movementComponent = MovementComponents[i];
                    var direction = Vector2.zero;
                    var moveRight = enemyComponent.CurrentDirection == EnemyMovementDirection.Right;

                    if (needNormalizeLine)
                    {
                        enemyComponent.LineChangingTimeDynamic = 0;

                        enemyComponent.CurrentDirection = moveRight
                            ? EnemyMovementDirection.Left
                            : EnemyMovementDirection.Right;

                        moveRight = !moveRight;
                    }

                    if (canProcessLineChanging)
                        enemyComponent.LineChangingTimeDynamic += DeltaTime;

                    direction.y = canProcessLineChanging ? -DeltaTime : 0;
                    direction.x = canProcessLineChanging ? 0 : moveRight ? DeltaTime : -DeltaTime;
                    translation.Value.x += direction.x * movementComponent.MoveSpeed;
                    translation.Value.y += direction.y * movementComponent.MoveSpeed;
                    CommandBuffer.SetComponent(i, entity, translation);
                    CommandBuffer.SetComponent(i, entity, enemyComponent);
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var entities = _entities.ToEntityArray(Allocator.TempJob);
            var translation = _entities.ToComponentDataArray<Translation>(Allocator.TempJob);
            var enemiesComponents = _entities.ToComponentDataArray<EnemyComponent>(Allocator.TempJob);
            var movementComponents = _entities.ToComponentDataArray<MovementComponent>(Allocator.TempJob);

            var movementJob = new EnemyMovementJob
            {
                EnemyEntities = entities,
                EnemiesTranslations = translation,
                DeltaTime = Time.DeltaTime,
                EnemiesComponents = enemiesComponents,
                MovementComponents = movementComponents,
                Bounds = SceneParams.CameraViewParams(),
                CommandBuffer = _bufferSystem.CreateCommandBuffer().ToConcurrent()
            };

            var jobHandle = movementJob.Schedule(translation.Length, 32);
            jobHandle.Complete();

            entities.Dispose();
            translation.Dispose();
            enemiesComponents.Dispose();
            movementComponents.Dispose();

            return jobHandle;
        }
    }
}