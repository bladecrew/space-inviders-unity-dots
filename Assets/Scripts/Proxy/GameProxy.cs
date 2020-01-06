using System.Collections.Generic;
using Data;
using Unity.Entities;
using UnityEngine;

namespace Proxy
{
    [DisallowMultipleComponent]
    [RequiresEntityConversion]
    public class GameProxy : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        [SerializeField] private GameObject enemyGameObject;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            _AddGameData(entity, dstManager, conversionSystem);
        }

        private void _AddGameData(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var gameData = new GameComponent
            {
                Enemy = conversionSystem.GetPrimaryEntity(enemyGameObject),
                CurrentEnemies = 1,
                State = State.Play
            };

            dstManager.AddComponentData(entity, gameData);
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(enemyGameObject);
        }
    }
}