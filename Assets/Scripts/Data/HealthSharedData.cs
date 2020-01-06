using System;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Data
{
    public struct HealthSharedData : ISharedComponentData, IEquatable<RenderMesh>
    {
        public Mesh Mesh;
        public Material ThreeHpMaterial;
        public Material TwoHpMaterial;
        public Material OneHpMaterial;

        public Material MaterialByHealthData(HealthData healthData)
        {
            if (healthData.Health == 1)
                return OneHpMaterial;
            if (healthData.Health == 2)
                return TwoHpMaterial;

            return ThreeHpMaterial;
        }

        public override int GetHashCode()
        {
            var hash = 0;
            
            if (!ReferenceEquals(Mesh, null)) 
                hash ^= Mesh.GetHashCode();
            
            if (!ReferenceEquals(ThreeHpMaterial, null)) 
                hash ^= ThreeHpMaterial.GetHashCode();
            
            if (!ReferenceEquals(TwoHpMaterial, null)) 
                hash ^= TwoHpMaterial.GetHashCode();
            
            if (!ReferenceEquals(OneHpMaterial, null)) 
                hash ^= OneHpMaterial.GetHashCode();
            
            return hash;
        }

        public bool Equals(RenderMesh other)
        {
            return other.mesh == Mesh &&
                   (other.material == OneHpMaterial ||
                    other.material == TwoHpMaterial ||
                    other.material == ThreeHpMaterial);
        }
    }
}