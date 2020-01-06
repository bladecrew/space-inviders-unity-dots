using System;
using Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using Utils;

namespace Systems
{
    [AlwaysSynchronizeSystem]
    public class EnemyMovementSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct EnemyMovementJob : IJobForEach<Translation, MovementComponent, EnemyComponent>
        {
            [ReadOnly] public float DeltaTime;
            [ReadOnly] public Bounds Bounds;    
            
            public void Execute(ref Translation translation, ref MovementComponent movementComponent, ref EnemyComponent enemyComponent)
            {
                var movementDirection = _MovementDirection(ref enemyComponent, ref translation);
                translation.Value.x += movementDirection.x * movementComponent.MoveSpeed;
                translation.Value.y += movementDirection.y * movementComponent.MoveSpeed;
            }

            private Vector2 _MovementDirection(ref EnemyComponent enemyComponent, ref Translation translation)
            {
                var lineChangingTime = enemyComponent.LineChangingTimeDynamic + DeltaTime;
                if (lineChangingTime >= enemyComponent.LineChangingTime)
                {
                    enemyComponent.LineChangingTimeDynamic = 0;

                    enemyComponent.CurrentDirection = enemyComponent.CurrentDirection == EnemyMovementDirection.Right
                        ? EnemyMovementDirection.Left
                        : EnemyMovementDirection.Right;
                }

                switch (enemyComponent.CurrentDirection)
                {
                    case EnemyMovementDirection.Right when translation.Value.x >= Bounds.max.x:
                    case EnemyMovementDirection.Left when translation.Value.x <= Bounds.min.x:
                        enemyComponent.LineChangingTimeDynamic = lineChangingTime;

                        if (enemyComponent.IsNonStop)
                        {
                            enemyComponent.CurrentDirection = enemyComponent.CurrentDirection == EnemyMovementDirection.Right
                                ? EnemyMovementDirection.Left
                                : EnemyMovementDirection.Right;
                        }

                        return new Vector2(0, -DeltaTime);

                    case EnemyMovementDirection.Left:
                        return new Vector2(-DeltaTime, -DeltaTime)
                            .CalculateFromDegree(enemyComponent.SerpentineDegree);

                    case EnemyMovementDirection.Right:
                        return new Vector2(DeltaTime, -DeltaTime)
                            .CalculateFromDegree(enemyComponent.SerpentineDegree);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var movementJob = new EnemyMovementJob
            {
                DeltaTime = Time.DeltaTime,
                Bounds = SceneParams.CameraViewParams()
            };

            return movementJob.Schedule(this, inputDeps);
        }
    }
}