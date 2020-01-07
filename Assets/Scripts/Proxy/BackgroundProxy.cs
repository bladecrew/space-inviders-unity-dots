using Components;
using Unity.Entities;
using UnityEngine;
using System.Collections.Generic;

namespace Proxy
{
    [RequiresEntityConversion]
    public class BackgroundProxy : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        [SerializeField] private int maxBackgroundsCount = 2;
        [SerializeField] private int bottomCorner = -11;
        [SerializeField] private int topCorner = 11;
        [SerializeField] private int spawnCorner;
        [SerializeField] private GameObject backgroundGo;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            _AddBackgroundComponent(entity, dstManager, conversionSystem);
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(backgroundGo);
        }

        private void _AddBackgroundComponent(Entity entity, EntityManager dstManager,
            GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new BackgroundComponent
                {
                    Background = conversionSystem.GetPrimaryEntity(backgroundGo),
                    MaxBackgroundsCount = maxBackgroundsCount,
                    TopCorner = topCorner,
                    BottomCorner = bottomCorner,
                    SpawnCorner = spawnCorner
                }
            );
        }
    }
}