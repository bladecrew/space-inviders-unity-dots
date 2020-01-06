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
        private struct EnemyMovementJob : IJobForEach<Translation, MovementData, EnemyData>
        {
            [ReadOnly] public float DeltaTime;
            [ReadOnly] public Bounds Bounds;    
            
            public void Execute(ref Translation translation, ref MovementData movementData, ref EnemyData enemyData)
            {
                var movementDirection = _MovementDirection(ref enemyData, ref translation);
                translation.Value.x += movementDirection.x * movementData.MoveSpeed;
                translation.Value.y += movementDirection.y * movementData.MoveSpeed;
            }

            private Vector2 _MovementDirection(ref EnemyData enemyData, ref Translation translation)
            {
                var lineChangingTime = enemyData.LineChangingTimeDynamic + DeltaTime;
                if (lineChangingTime >= enemyData.LineChangingTime)
                {
                    enemyData.LineChangingTimeDynamic = 0;

                    enemyData.CurrentDirection = enemyData.CurrentDirection == EnemyMovementDirection.Right
                        ? EnemyMovementDirection.Left
                        : EnemyMovementDirection.Right;
                }

                switch (enemyData.CurrentDirection)
                {
                    case EnemyMovementDirection.Right when translation.Value.x >= Bounds.max.x:
                    case EnemyMovementDirection.Left when translation.Value.x <= Bounds.min.x:
                        enemyData.LineChangingTimeDynamic = lineChangingTime;

                        if (enemyData.IsNonStop)
                        {
                            enemyData.CurrentDirection = enemyData.CurrentDirection == EnemyMovementDirection.Right
                                ? EnemyMovementDirection.Left
                                : EnemyMovementDirection.Right;
                        }

                        return new Vector2(0, -DeltaTime);

                    case EnemyMovementDirection.Left:
                        return new Vector2(-DeltaTime, -DeltaTime)
                            .CalculateFromDegree(enemyData.SerpentineDegree);

                    case EnemyMovementDirection.Right:
                        return new Vector2(DeltaTime, -DeltaTime)
                            .CalculateFromDegree(enemyData.SerpentineDegree);
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