using Unity.Entities;

namespace Components
 {
     [GenerateAuthoringComponent]
     public struct MovementComponent : IComponentData
     {
         public float MoveSpeed;
     }
 }