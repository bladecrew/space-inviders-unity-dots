using System.ComponentModel;
using Data;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using Utils;

namespace Systems
{
    [AlwaysSynchronizeSystem]
    public class PlayerMovementSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct PlayerMovementJob : IJobForEach<Translation, InputData, MovementData>
        {
            public float DeltaTime;
            public Bounds Bounds;

            public void Execute(ref Translation translation, ref InputData inputData,ref MovementData movementData)
            {
                if (translation.Value.x < Bounds.min.x)
                    translation.Value.x = Bounds.max.x;
                else if (translation.Value.x > Bounds.max.x)
                    translation.Value.x = Bounds.min.x;
                else
                    translation.Value.x += inputData.InputX * movementData.MoveSpeed * DeltaTime;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var movementJob = new PlayerMovementJob
            {
                DeltaTime = Time.DeltaTime,
                Bounds = SceneParams.CameraViewParams()
            };

            return movementJob.Schedule(this, inputDeps);
        }
    }
}