using Components;
using Unity.Entities;
using UnityEngine;

namespace Proxy
{
    public class BackgroundInstanceProxy : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            _AddBackgroundInstanceComponent(entity, dstManager);
        }

        private void _AddBackgroundInstanceComponent(Entity entity, EntityManager dstManager)
        {
            dstManager.AddComponentData(entity, new BackgroundInstanceComponent());
        }
    }
}