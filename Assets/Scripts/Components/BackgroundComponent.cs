using Unity.Entities;

namespace Components
{
    public struct BackgroundComponent : IComponentData
    {
        public float MaxBackgroundsCount;
        public float BottomCorner;
        public float SpawnCorner;
        public float TopCorner;
        public Entity Background;
    }
}