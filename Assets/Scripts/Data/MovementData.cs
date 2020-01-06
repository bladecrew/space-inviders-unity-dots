using Unity.Entities;
 
 namespace Data
 {
     [GenerateAuthoringComponent]
     public struct MovementData : IComponentData
     {
         public float MoveSpeed;
     }
 }