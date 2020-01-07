using Unity.Entities;

namespace Components
{
    public struct GameComponent : IComponentData
    {
        public Entity Enemy;
        public int DefaultEnemies;
        public int MaxEnemies;
        public float EnemiesMoveSpeed;
        public int CurrentWave;
        public int Points;
    }
}