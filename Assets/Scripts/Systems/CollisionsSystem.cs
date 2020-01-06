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
    public class CollisionsSystem : JobComponentSystem
    {
        private EntityQuery _enemiesComponents;
        private EntityQuery _bulletComponents;
        private EntityQuery _healthComponents;
        private EntityCommandBufferSystem _bufferSystem;

        protected override void OnCreate()
        {
            _bufferSystem = World.GetOrCreateSystem<EntityCommandBufferSystem>();

            var enemyQuery = new EntityQueryDesc
            {
                None = new ComponentType[] {typeof(InputComponent)},
                All = new ComponentType[] {typeof(Translation), typeof(EnemyComponent)}
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
        }

        [BurstCompile]
        private struct CollisionJob : IJobParallelFor
        {
            [WriteOnly] public EntityCommandBuffer.Concurrent BufferCommand;

            [ReadOnly] public NativeArray<Translation> EnemiesTranslations;
            [ReadOnly] public NativeArray<Translation> BulletTranslation;
            [ReadOnly] public NativeArray<Translation> HealthTranslation;
            [ReadOnly] public NativeArray<HealthComponent> HealthDatas;
            [ReadOnly] public NativeArray<BulletComponent> BulletDatas;
            [ReadOnly] public NativeArray<Entity> EnemiesEntities;
            [ReadOnly] public NativeArray<Entity> BulletsEntities;
            [ReadOnly] public NativeArray<Entity> HealthEntities;
            [ReadOnly] public Bounds Bounds;

            // todo : refactor on raycasts
            public void Execute(int i)
            {
                if (BulletTranslation.Length == 0)
                    _DeleteOutOfBounds(i);

                for (var bulletIndex = i; bulletIndex < BulletTranslation.Length; bulletIndex++)
                {
                    var bulletTranslation = BulletTranslation[bulletIndex];
                    var bulletData = BulletDatas[bulletIndex];
                    var needDestroyBullet = bulletTranslation.Value.y > Bounds.max.y;

                    for (var enemyIndex = i; enemyIndex < EnemiesTranslations.Length; enemyIndex++)
                    {
                        var enemyTranslation = EnemiesTranslations[enemyIndex];
                        var needDestroyEnemy = enemyTranslation.Value.y <= Bounds.min.y + 1f;

                        if (needDestroyEnemy || bulletData.IsEnemyBullet)
                        {
                            for (var healthDataIndex = 0; healthDataIndex < HealthDatas.Length; healthDataIndex++)
                            {
                                if (needDestroyEnemy)
                                {
                                    var healthData = HealthDatas[healthDataIndex];
                                    var healthEntity = HealthEntities[healthDataIndex];

                                    BufferCommand.SetComponent(i, healthEntity,
                                        new HealthComponent {Health = --healthData.Health});
                                }
                                else
                                {
                                    var healthTranslation = HealthTranslation[healthDataIndex];
                                    
                                    if (!Maths.Intersect(healthTranslation, bulletTranslation))
                                        continue;

                                    var healthData = HealthDatas[healthDataIndex];
                                    var healthEntity = HealthEntities[healthDataIndex];

                                    needDestroyBullet = true;
                                    BufferCommand.SetComponent(i, healthEntity,
                                        new HealthComponent {Health = --healthData.Health});
                                }
                            }
                        }

                        if (!bulletData.IsEnemyBullet && Maths.Intersect(bulletTranslation, enemyTranslation))
                            needDestroyBullet = needDestroyEnemy = true;

                        if (needDestroyEnemy)
                            BufferCommand.DestroyEntity(i, EnemiesEntities[enemyIndex]);
                    }

                    if (needDestroyBullet)
                        BufferCommand.DestroyEntity(i, BulletsEntities[bulletIndex]);
                }
            }

            private void _DeleteOutOfBounds(int i)
            {
                for (var enemyIndex = i; enemyIndex < EnemiesTranslations.Length; enemyIndex++)
                {
                    var enemyTranslation = EnemiesTranslations[enemyIndex];
                    var needDestroyEnemy = enemyTranslation.Value.y <= Bounds.min.y + 1f;

                    if (needDestroyEnemy)
                    {
                        for (var healthDataIndex = 0; healthDataIndex < HealthDatas.Length; healthDataIndex++)
                        {
                            var healthData = HealthDatas[healthDataIndex];
                            var healthEntity = HealthEntities[healthDataIndex];

                            BufferCommand.SetComponent(i, healthEntity,
                                new HealthComponent {Health = --healthData.Health});
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
            var bulletDatas = _bulletComponents.ToComponentDataArray<BulletComponent>(Allocator.TempJob);
            var healthDatas = _healthComponents.ToComponentDataArray<HealthComponent>(Allocator.TempJob);
            var enemiesEntities = _enemiesComponents.ToEntityArray(Allocator.TempJob);
            var bulletsEntities = _bulletComponents.ToEntityArray(Allocator.TempJob);
            var healthEntities = _healthComponents.ToEntityArray(Allocator.TempJob);

            var bufferCommand = _bufferSystem.CreateCommandBuffer().ToConcurrent();
            var job = new CollisionJob
            {
                EnemiesTranslations = enemiesTranslation,
                BulletTranslation = bulletTranslation,
                HealthTranslation = healthTranslation,
                HealthDatas = healthDatas,
                BulletDatas = bulletDatas,
                EnemiesEntities = enemiesEntities,
                BulletsEntities = bulletsEntities,
                HealthEntities = healthEntities,
                BufferCommand = bufferCommand,
                Bounds = SceneParams.CameraViewParams()
            };

            var collisionJobHandle = job.Schedule(enemiesTranslation.Length + bulletTranslation.Length, 32);
            collisionJobHandle.Complete();

            enemiesTranslation.Dispose();
            bulletTranslation.Dispose();
            healthTranslation.Dispose();
            healthDatas.Dispose();
            bulletDatas.Dispose();
            enemiesEntities.Dispose();
            bulletsEntities.Dispose();
            healthEntities.Dispose();

            return collisionJobHandle;
        }
    }
}