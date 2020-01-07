using System.Collections.Generic;
using Components;
using Unity.Entities;
using UnityEngine;

namespace Proxy
{
    [DisallowMultipleComponent]
    [RequiresEntityConversion]
    public class GameProxy : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        [SerializeField] private GameObject enemyGameObject;
        [SerializeField] private int defaultEnemiesCount = 5;
        [SerializeField] private int maxEnemiesCount = 10;
        [SerializeField] private float enemiesMoveSpeed = 3;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            _AddGameData(entity, dstManager, conversionSystem);
        }

        private void _AddGameData(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var gameData = new GameComponent
            {
                Enemy = conversionSystem.GetPrimaryEntity(enemyGameObject),
                DefaultEnemies = defaultEnemiesCount,
                MaxEnemies = maxEnemiesCount,
                EnemiesMoveSpeed = enemiesMoveSpeed,
                CurrentWave = 0,
                Points = 0
            };

            dstManager.AddComponentData(entity, gameData);
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(enemyGameObject);
        }
    }
}