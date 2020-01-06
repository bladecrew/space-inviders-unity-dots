using System.Collections.Generic;
using Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Utils;

namespace Proxy
{
    [RequiresEntityConversion]
    public class EnemyProxy : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private float moveSpeed;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var enemyData = new EnemyComponent();

            dstManager.AddComponentData(entity, enemyData);

            //_AddEnemyData(entity, dstManager);
            _AddMovementData(entity, dstManager);
            _AddShootingData(entity, dstManager, conversionSystem);
            _AddPositionData(entity, dstManager);
            _AddMovementData(entity, dstManager);
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(bulletPrefab);
        }
        
        private void _AddShootingData(Entity entity, EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)
        {
            var shootingData = new ShootingComponent
            {
                Bullet = conversionSystem.GetPrimaryEntity(bulletPrefab)
            };

            dstManager.AddComponentData(entity, shootingData);
        }

        private void _AddPositionData(Entity entity, EntityManager dstManager)
        {
            var bounds = SceneParams.CameraViewParams();
            var enemyPosition = Vector3.zero;
            enemyPosition.y = bounds.max.y - 1f;
            dstManager.AddComponentData(entity, new Translation {Value = enemyPosition});

            dstManager.SetComponentData(entity,
                new LocalToWorld {Value = float4x4.TRS(enemyPosition, Quaternion.identity, 3)}
            );
        }

        private void _AddMovementData(Entity entity, EntityManager dstManager)
        {
            var movementData = new MovementComponent
            {
                MoveSpeed = moveSpeed
            };

            dstManager.AddComponentData(entity, movementData);
        }

        /*private void _AddEnemyData(Entity entity, EntityManager dstManager)
        {
            var enemyData = new EnemyData
            {
                CurrentDirection = movementDirection,
                LineChangingTime = lineChangingTime,
                IsNonStop = isNonStop,
                SerpentineDegree = serpentineDegree
            };

            dstManager.AddComponentData(entity, enemyData);
        }*/
    }
}