using Components;
using Unity.Entities;
using UnityEngine;

namespace Proxy
{
    public class ExplosionProxy : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField] public float destroyTime;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            _AddExplosionComponent(entity, dstManager);
        }

        private void _AddExplosionComponent(Entity entity, EntityManager dstManager)
        {
            dstManager.AddComponentData(entity, new ExplosionComponent
            {
                DestroyTime = destroyTime
            });
        }
    }
}