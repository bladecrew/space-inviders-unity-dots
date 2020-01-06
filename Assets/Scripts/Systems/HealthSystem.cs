using System.Collections.Generic;
using Data;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;

namespace Systems
{
    public class HealthSystem : ComponentSystem
    {
        private EntityQuery _healthSharedQuery;

        protected override void OnCreate()
        {
            var healthQuery = new EntityQueryDesc
            {
                All = new ComponentType[] {typeof(HealthComponent), typeof(InputComponent)}
            };

            _healthSharedQuery = GetEntityQuery(healthQuery);
        }

        protected override void OnUpdate()
        {
            var healthComponents = _healthSharedQuery.ToComponentDataArray<HealthComponent>(Allocator.TempJob);
            var entities = _healthSharedQuery.ToEntityArray(Allocator.TempJob);
            var healthSharedComponents = new List<HealthSharedComponent>();
            EntityManager.GetAllUniqueSharedComponentData(healthSharedComponents);


            healthSharedComponents.RemoveAll(c => c.IsNull);

            for (var i = 0; i < healthSharedComponents.Count; i++)
            {
                var healthComponent = healthComponents[i];
                var healthSharedComponent = healthSharedComponents[i];

                if (healthComponent.Health == healthSharedComponent.CurrentHp)
                    continue;

                healthSharedComponent.CurrentHp = healthComponent.Health;

                var entity = entities[i];

                EntityManager.SetSharedComponentData(entity, new RenderMesh
                {
                    material = healthSharedComponent.MaterialByHp(),
                    mesh = healthSharedComponent.Mesh
                });
            }

            healthComponents.Dispose();
            entities.Dispose();
        }
    }
}