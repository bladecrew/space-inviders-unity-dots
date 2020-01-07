using Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using Utils;

namespace Systems
{
    public class CollisionsSystem : JobComponentSystem
    {
        private EntityQuery _enemiesComponents;
        private EntityQuery _bulletComponents;
        private EntityQuery _healthComponents;
        private EntityQuery _gameComponents;
        private EntityCommandBufferSystem _bufferSystem;

        protected override void OnCreate()
        {
            _bufferSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();

            var enemyQuery = new EntityQueryDesc
            {
                None = new ComponentType[] {typeof(InputComponent)},
                All = new ComponentType[] {typeof(Translation), typeof(EnemyComponent), typeof(ShootingComponent)}
            };
            _enemiesComponents = GetEntityQuery(enemyQuery);

            var bulletQuery = new EntityQueryDesc
            {
                None = new ComponentType[] {typeof(InputComponent)},
                All = new ComponentType[] {typeof(Translation), typeof(BulletComponent)}
            };
            _bulletComponents = GetEntityQuery(bulletQuery);

            var healthQuery = new EntityQueryDesc
            {
                All = new ComponentType[] {typeof(Translation), typeof(HealthComponent)}
            };
            _healthComponents = GetEntityQuery(healthQuery);

            var gameQuery = new EntityQueryDesc
            {
                All = new ComponentType[] {typeof(GameComponent)}
            };
            _gameComponents = GetEntityQuery(gameQuery);
        }

        [BurstCompile]
        private struct CollisionJob : IJobParallelFor
        {
            [WriteOnly] public EntityCommandBuffer.Concurrent BufferCommand;

            [ReadOnly] public NativeArray<Translation> EnemiesTranslations;
            [ReadOnly] public NativeArray<Translation> BulletTranslation;
            [ReadOnly] public NativeArray<Translation> HealthTranslation;
            [ReadOnly] public NativeArray<HealthComponent> HealthComponents;
            [ReadOnly] public NativeArray<BulletComponent> BulletComponents;
            [ReadOnly] public NativeArray<ShootingComponent> EnemiesComponents;
            [ReadOnly] public NativeArray<Entity> EnemiesEntities;
            [ReadOnly] public NativeArray<Entity> BulletsEntities;
            [ReadOnly] public NativeArray<Entity> HealthEntities;
            [ReadOnly] public Entity GameDataEntity;
            [ReadOnly] public GameComponent GameComponent;
            [ReadOnly] public Bounds Bounds;

            // todo : refactor on raycasts
            public void Execute(int i)
            {
                if (BulletTranslation.Length == 0)
                    _DeleteOutOfBounds(i);

                for (var bulletIndex = i; bulletIndex < BulletTranslation.Length; bulletIndex++)
                {
                    var bulletTranslation = BulletTranslation[bulletIndex];
                    var bulletComponent = BulletComponents[bulletIndex];
                    var needDestroyBullet = bulletTranslation.Value.y > Bounds.max.y ||
                                            bulletTranslation.Value.y < Bounds.min.y;

                    for (var enemyIndex = i; enemyIndex < EnemiesTranslations.Length; enemyIndex++)
                    {
                        var enemyTranslation = EnemiesTranslations[enemyIndex];
                        var needDestroyEnemy = enemyTranslation.Value.y <= Bounds.min.y + 1f;

                        if (needDestroyEnemy || bulletComponent.IsEnemyBullet)
                        {
                            for (var healthComponentIndex = 0;
                                healthComponentIndex < HealthComponents.Length;
                                healthComponentIndex++)
                            {
                                if (needDestroyEnemy)
                                {
                                    var healthEntity = HealthEntities[healthComponentIndex];

                                    BufferCommand.SetComponent(i, healthEntity,
                                        new HealthComponent {Health = 0});
                                }
                                else
                                {
                                    var healthTranslation = HealthTranslation[healthComponentIndex];

                                    if (!Maths.Intersect(healthTranslation, bulletTranslation))
                                        continue;

                                    var healthComponent = HealthComponents[healthComponentIndex];
                                    var healthEntity = HealthEntities[healthComponentIndex];

                                    needDestroyBullet = true;
                                    BufferCommand.SetComponent(i, healthEntity,
                                        new HealthComponent {Health = --healthComponent.Health});

                                    var explosion =
                                        BufferCommand.Instantiate(i, EnemiesComponents[enemyIndex].Explosion);
                                    BufferCommand.SetComponent(i, explosion, new Translation
                                    {
                                        Value = bulletTranslation.Value
                                    });
                                    GameComponent.Points++;
                                }
                            }
                        }

                        if (!bulletComponent.IsEnemyBullet && Maths.Intersect(bulletTranslation, enemyTranslation))
                            needDestroyBullet = needDestroyEnemy = true;

                        if (needDestroyEnemy)
                        {
                            var explosion = BufferCommand.Instantiate(i, EnemiesComponents[enemyIndex].Explosion);
                            BufferCommand.SetComponent(i, explosion, new Translation
                            {
                                Value = enemyTranslation.Value
                            });

                            BufferCommand.DestroyEntity(i, EnemiesEntities[enemyIndex]);
                        }
                    }

                    if (needDestroyBullet)
                        BufferCommand.DestroyEntity(i, BulletsEntities[bulletIndex]);
                }

                BufferCommand.SetComponent(i, GameDataEntity, GameComponent);
            }

            private void _DeleteOutOfBounds(int i)
            {
                for (var enemyIndex = i; enemyIndex < EnemiesTranslations.Length; enemyIndex++)
                {
                    var enemyTranslation = EnemiesTranslations[enemyIndex];
                    var needDestroyEnemy = enemyTranslation.Value.y <= Bounds.min.y + 1f;

                    if (needDestroyEnemy)
                    {
                        for (var healthComponentIndex = 0;
                            healthComponentIndex < HealthComponents.Length;
                            healthComponentIndex++)
                        {
                            var healthComponent = HealthComponents[healthComponentIndex];
                            var healthEntity = HealthEntities[healthComponentIndex];

                            BufferCommand.SetComponent(i, healthEntity,
                                new HealthComponent {Health = --healthComponent.Health});
                        }
                    }

                    if (needDestroyEnemy)
                        BufferCommand.DestroyEntity(i, EnemiesEntities[enemyIndex]);
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var enemiesTranslation = _enemiesComponents.ToComponentDataArray<Translation>(Allocator.TempJob);
            var bulletTranslation = _bulletComponents.ToComponentDataArray<Translation>(Allocator.TempJob);
            var healthTranslation = _healthComponents.ToComponentDataArray<Translation>(Allocator.TempJob);
            var bulletComponents = _bulletComponents.ToComponentDataArray<BulletComponent>(Allocator.TempJob);
            var healthComponents = _healthComponents.ToComponentDataArray<HealthComponent>(Allocator.TempJob);
            var gameComponents = _gameComponents.ToComponentDataArray<GameComponent>(Allocator.TempJob);
            var enemiesShootingComponents =
                _enemiesComponents.ToComponentDataArray<ShootingComponent>(Allocator.TempJob);
            var enemiesEntities = _enemiesComponents.ToEntityArray(Allocator.TempJob);
            var bulletsEntities = _bulletComponents.ToEntityArray(Allocator.TempJob);
            var healthEntities = _healthComponents.ToEntityArray(Allocator.TempJob);
            var gameEntities = _gameComponents.ToEntityArray(Allocator.TempJob);

            var bufferCommand = _bufferSystem.CreateCommandBuffer().ToConcurrent();
            var job = new CollisionJob
            {
                EnemiesTranslations = enemiesTranslation,
                BulletTranslation = bulletTranslation,
                HealthTranslation = healthTranslation,
                EnemiesComponents = enemiesShootingComponents,
                HealthComponents = healthComponents,
                BulletComponents = bulletComponents,
                EnemiesEntities = enemiesEntities,
                BulletsEntities = bulletsEntities,
                HealthEntities = healthEntities,
                BufferCommand = bufferCommand,
                GameDataEntity = gameEntities[0],
                GameComponent = gameComponents[0],
                Bounds = SceneParams.CameraViewParams()
            };

            var collisionJobHandle = job.Schedule(enemiesTranslation.Length + bulletTranslation.Length, 32);
            collisionJobHandle.Complete();

            enemiesTranslation.Dispose();
            bulletTranslation.Dispose();
            healthTranslation.Dispose();
            enemiesShootingComponents.Dispose();
            healthComponents.Dispose();
            bulletComponents.Dispose();
            enemiesEntities.Dispose();
            bulletsEntities.Dispose();
            healthEntities.Dispose();
            gameComponents.Dispose();
            gameEntities.Dispose();

            return collisionJobHandle;
        }
    }
}