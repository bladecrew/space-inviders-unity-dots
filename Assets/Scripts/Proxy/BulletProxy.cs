using Components;
using Unity.Entities;
using UnityEngine;

namespace Proxy
{
    [RequiresEntityConversion]
    public class BulletProxy : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField] private float movementSpeed;
        [SerializeField] private bool isEnemyBullet;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            _AddBulletComponent(entity, dstManager);
        }

        private void _AddBulletComponent(Entity entity, EntityManager dstManager)
        {
            var data = new BulletComponent
            {
                MovementSpeed = movementSpeed,
                IsEnemyBullet = isEnemyBullet
            };

            dstManager.AddComponentData(entity, data);
        }
    }
}