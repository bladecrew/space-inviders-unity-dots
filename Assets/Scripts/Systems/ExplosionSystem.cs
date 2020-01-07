using Components;
using Unity.Collections;
using Unity.Entities;

namespace Systems
{
    public class ExplosionSystem : ComponentSystem
    {
        private EntityCommandBufferSystem _bufferSystem;
        private EntityQuery _explosions;

        protected override void OnCreate()
        {
            var query = new EntityQueryDesc
            {
                All = new ComponentType[] {typeof(ExplosionComponent)}
            };

            _explosions = GetEntityQuery(query);
            _bufferSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            if (_explosions.CalculateEntityCount() == 0)
                return;

            var entities = _explosions.ToEntityArray(Allocator.TempJob);
            var components = _explosions.ToComponentDataArray<ExplosionComponent>(Allocator.TempJob);
            var bufferCommand = _bufferSystem.CreateCommandBuffer();

            for (var i = 0; i < entities.Length; i++)
            {
                var component = components[i];
                var entity = entities[i];

                if (component.DestroyTimeDynamic >= component.DestroyTime)
                {
                    bufferCommand.DestroyEntity(entity);
                    continue;
                }

                component.DestroyTimeDynamic += Time.DeltaTime;
                bufferCommand.SetComponent(entity, component);
            }

            entities.Dispose();
            components.Dispose();
        }
    }
}