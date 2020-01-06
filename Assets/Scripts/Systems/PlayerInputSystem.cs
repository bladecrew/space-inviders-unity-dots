using Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Systems
{
    public class PlayerInputSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct PlayerInputJob : IJobForEach<InputComponent>
        {
            [ReadOnly] public float InputX;
            [ReadOnly] public bool IsShooting;
            
            public void Execute(ref InputComponent inputComponent)
            {
                inputComponent.InputX = InputX;
                inputComponent.IsShooting = IsShooting;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var inputJob = new PlayerInputJob
            {
                InputX = Input.GetAxis("Horizontal"),
                IsShooting = Input.GetKeyDown(KeyCode.Space)
            };

            return inputJob.Schedule(this, inputDeps);
        }
    }
}