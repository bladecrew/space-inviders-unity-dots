using Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    public class BackgroundMovingSystem : ComponentSystem
    {
        private EntityCommandBufferSystem _bufferSystem;

        protected override void OnCreate()
        {
            _bufferSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((ref BackgroundComponent backgroundComponent) =>
            {
                var query = new EntityQueryDesc
                {
                    All = new ComponentType[] {typeof(Translation), typeof(BackgroundInstanceComponent)}
                };

                var backgroundInstances = GetEntityQuery(query);

                var bufferCommand = _bufferSystem.CreateCommandBuffer();
                var entities = backgroundInstances.ToEntityArray(Allocator.TempJob);
                var entitiesPositions = backgroundInstances.ToComponentDataArray<Translation>(Allocator.TempJob);

                if (entities.Length == 0)
                {
                    bufferCommand.Instantiate(backgroundComponent.Background);
                    entities.Dispose();
                    entitiesPositions.Dispose();
                    return;
                }

                for (var i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];

                    var entityTranslation = entitiesPositions[i];

                    entityTranslation.Value.y -= Time.DeltaTime;

                    bufferCommand.SetComponent(entity, entityTranslation);

                    if (entityTranslation.Value.y <= backgroundComponent.BottomCorner)
                    {
                        bufferCommand.DestroyEntity(entity);
                        continue;
                    }

                    if (entityTranslation.Value.y > backgroundComponent.SpawnCorner ||
                        entities.Length >= backgroundComponent.MaxBackgroundsCount)
                        continue;

                    var backgroundEntity = bufferCommand.Instantiate(backgroundComponent.Background);
                    bufferCommand.SetComponent(backgroundEntity, new Translation
                    {
                        Value = new float3(0f, backgroundComponent.TopCorner, 0f)
                    });
                }

                entities.Dispose();
                entitiesPositions.Dispose();
            });
        }
    }
}