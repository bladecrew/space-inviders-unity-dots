using Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems
{
    public class BackgroundMovingSystem : ComponentSystem
    {
        private EntityCommandBufferSystem _bufferSystem;
        private EntityQuery _backgrounds;
        private EntityQuery _backgroundComponents;

        protected override void OnCreate()
        {
            var backgroundComponentsQuery = new EntityQueryDesc
            {
                All = new ComponentType[] {typeof(BackgroundComponent)}
            };

            _backgroundComponents = GetEntityQuery(backgroundComponentsQuery);

            var query = new EntityQueryDesc
            {
                All = new ComponentType[] {typeof(Translation), typeof(BackgroundInstanceComponent)}
            };

            _backgrounds = GetEntityQuery(query);

            _bufferSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var entities = _backgrounds.ToEntityArray(Allocator.TempJob);
            var entitiesPositions = _backgrounds.ToComponentDataArray<Translation>(Allocator.TempJob);
            var backgroundComponents =
                _backgroundComponents.ToComponentDataArray<BackgroundComponent>(Allocator.TempJob);

            var bufferCommand = _bufferSystem.CreateCommandBuffer();

            foreach (var backgroundComponent in backgroundComponents)
            {
                if (entities.Length == 0)
                {
                    bufferCommand.Instantiate(backgroundComponent.Background);
                    entities.Dispose();
                    backgroundComponents.Dispose();
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
            }

            backgroundComponents.Dispose();
            entities.Dispose();
            entitiesPositions.Dispose();
        }
    }
}