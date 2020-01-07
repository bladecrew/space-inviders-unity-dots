using System.Collections.Generic;
using Components;
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
        [SerializeField] private GameObject explosionPrefab;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new EnemyComponent());

            _AddShootingComponent(entity, dstManager, conversionSystem);
            _AddPositionComponent(entity, dstManager);
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(bulletPrefab);
            referencedPrefabs.Add(explosionPrefab);
        }

        private void _AddShootingComponent(Entity entity, EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)
        {
            var shootingComponent = new ShootingComponent
            {
                Bullet = conversionSystem.GetPrimaryEntity(bulletPrefab),
                Explosion = conversionSystem.GetPrimaryEntity(explosionPrefab)
            };

            dstManager.AddComponentData(entity, shootingComponent);
        }

        private void _AddPositionComponent(Entity entity, EntityManager dstManager)
        {
            var bounds = SceneParams.CameraViewParams();
            var enemyPosition = Vector3.zero;
            enemyPosition.y = bounds.max.y - 1f;
            dstManager.AddComponentData(entity, new Translation {Value = enemyPosition});

            dstManager.SetComponentData(entity,
                new LocalToWorld {Value = float4x4.TRS(enemyPosition, Quaternion.identity, 3)}
            );
        }
    }
}