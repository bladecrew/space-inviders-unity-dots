using Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;

namespace Systems
{
    public class HealthSystem : JobComponentSystem
    {
        private EntityQuery _healthComponents;
        private EntityCommandBufferSystem _bufferSystem;

        protected override void OnCreate()
        {
            var healthQuery = new EntityQueryDesc
            {
                All = new ComponentType[] {typeof(HealthSharedData), typeof(HealthData)}
            };

            _healthComponents = GetEntityQuery(healthQuery);
            _bufferSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return inputDeps;
            
            Entities
                .WithoutBurst()
                .WithStoreEntityQueryInField(ref _healthComponents)
                .ForEach((Entity entity, HealthSharedData healthSharedData, ref HealthData healthData) =>
                {
                    var bufferCommand = _bufferSystem.CreateCommandBuffer();
                    bufferCommand.SetSharedComponent(entity, new RenderMesh
                    {
                        mesh = healthSharedData.Mesh,
                        material = healthSharedData.MaterialByHealthData(healthData)
                    });
                }).Run();

            return inputDeps;
        }
    }
}