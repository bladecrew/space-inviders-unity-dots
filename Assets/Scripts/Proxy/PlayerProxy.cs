using System.Collections.Generic;
using Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Utils;

namespace Proxy
{
    [DisallowMultipleComponent]
    [RequiresEntityConversion]
    public class PlayerProxy : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        [SerializeField] private Material threeHpMaterial;
        [SerializeField] private Material twoHpMaterial;
        [SerializeField] private Material oneHpMaterial;
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private float moveSpeed;
        [SerializeField] private int health;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            _PlayerMovementData(entity, dstManager);
            _AddPositionData(entity, dstManager);
            _AddShootingData(entity, dstManager, conversionSystem);
            _AddInputData(entity, dstManager);
            _AddHealthData(entity, dstManager);
            _AddSharedHealthData(entity, dstManager);
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(bulletPrefab);
        }

        private void _PlayerMovementData(Entity entity, EntityManager dstManager)
        {
            var movementData = new MovementData
            {
                MoveSpeed = moveSpeed
            };

            dstManager.AddComponentData(entity, movementData);
        }

        private void _AddHealthData(Entity entity, EntityManager dstManager)
        {
            var healthData = new HealthData
            {
                Health = health
            };

            dstManager.AddComponentData(entity, healthData);
        }


        private void _AddSharedHealthData(Entity entity, EntityManager dstManager)
        {
            dstManager.SetSharedComponentData(entity, new HealthSharedData
            {
                OneHpMaterial = oneHpMaterial,
                TwoHpMaterial = twoHpMaterial,
                ThreeHpMaterial = threeHpMaterial,
                Mesh = GetComponent<Mesh>()
            });
        }

        private void _AddPositionData(Entity entity, EntityManager dstManager)
        {
            var bounds = SceneParams.CameraViewParams();
            var playerPosition = Vector3.zero;
            playerPosition.x = (bounds.max.x + bounds.min.x) / 2;
            playerPosition.y = bounds.min.y + 1f;
            dstManager.AddComponentData(entity, new Translation {Value = playerPosition});

            dstManager.SetComponentData(entity,
                new LocalToWorld {Value = float4x4.TRS(playerPosition, Quaternion.identity, 3)}
            );
        }

        private void _AddShootingData(Entity entity, EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)
        {
            var shootingData = new ShootingData
            {
                Bullet = conversionSystem.GetPrimaryEntity(bulletPrefab)
            };

            dstManager.AddComponentData(entity, shootingData);
        }

        private void _AddInputData(Entity entity, EntityManager dstManager)
        {
            dstManager.AddComponent(entity, typeof(InputData));
        }
    }
}