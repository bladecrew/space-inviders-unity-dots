using System;
using System.Data.SqlTypes;
using Unity.Entities;
using UnityEngine;

namespace Components
{
    [Serializable]
    public struct HealthSharedComponent : ISharedComponentData, IEquatable<HealthSharedComponent>, INullable
    {
        public Mesh Mesh;
        public Material ThreeHpMaterial;
        public Material TwoHpMaterial;
        public Material OneHpMaterial;
        public int CurrentHp;

        public Material MaterialByHp()
        {
            switch (CurrentHp)
            {
                case 1:
                    return OneHpMaterial;
                case 2:
                    return TwoHpMaterial;
                default:
                    return ThreeHpMaterial;
            }
        }

        public override int GetHashCode()
        {
            var hash = CurrentHp;

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

        public bool IsNull => Mesh == null &&
                                 ThreeHpMaterial == null &&
                                 TwoHpMaterial == null &&
                                 OneHpMaterial == null &&
                                 CurrentHp == 0;

        public bool Equals(HealthSharedComponent healthSharedComponent)
        {
            return healthSharedComponent.Mesh == Mesh &&
                   healthSharedComponent.ThreeHpMaterial == ThreeHpMaterial &&
                   healthSharedComponent.TwoHpMaterial == TwoHpMaterial &&
                   healthSharedComponent.OneHpMaterial == OneHpMaterial &&
                   healthSharedComponent.CurrentHp == CurrentHp;
        }
    }
}