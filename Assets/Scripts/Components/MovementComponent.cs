using Unity.Entities;
 
 namespace Data
 {
     [GenerateAuthoringComponent]
     public struct MovementComponent : IComponentData
     {
         public float MoveSpeed;
     }
 }