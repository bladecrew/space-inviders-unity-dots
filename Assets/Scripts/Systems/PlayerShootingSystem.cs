using Components;
using Unity.Entities;
using Unity.Transforms;

namespace Systems
{
    public class PlayerShootingSystem : ComponentSystem
    {
        private BeginInitializationEntityCommandBufferSystem _bufferSystem;

        protected override void OnCreate()
        {
            _bufferSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            Entities.ForEach((ref ShootingComponent shootingComponent, ref LocalToWorld position,
                ref InputComponent playerInput) =>
            {
                if (!playerInput.IsShooting)
                    return;

                var commandBuffer = _bufferSystem.CreateCommandBuffer();
                var entity = commandBuffer.Instantiate(shootingComponent.Bullet);
                var localToWorld = new Translation
                {
                    Value = position.Position
                };

                commandBuffer.SetComponent(entity, localToWorld);
            });
        }
    }
}