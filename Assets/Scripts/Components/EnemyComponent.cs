using Unity.Entities;

namespace Components
{
    public struct EnemyComponent : IComponentData
    {
        public EnemyMovementDirection CurrentDirection;
        public float LineChangingTime;
        public float LineChangingTimeDynamic;
        public bool IsNonStop;
        public float SerpentineDegree;
        public float ShootingPeriod;
        public float ShootingPeriodDynamic;
    }

    public enum EnemyMovementDirection
    {
        Left,
        Right
    }
}