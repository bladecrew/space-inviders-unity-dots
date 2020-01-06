using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
#if !NET_DOTS
using Unity.Properties;
#endif

namespace Unity.Entities
{
    public static unsafe partial class EntityPatcher
    {
#if ENABLE_PROFILER || UNITY_EDITOR
        static Profiling.ProfilerMarker s_ApplyChangeSetProfilerMarker = new Profiling.ProfilerMarker("EntityPatcher.ApplyChangeSet");
#endif
        static EntityQueryDesc EntityGuidQueryDesc { get; } = new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(EntityGuid)
            },
            Options = EntityQueryOptions.IncludeDisabled | EntityQueryOptions.IncludePrefab
        };
        
        static EntityQueryDesc PrefabQueryDesc { get; } = new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(EntityGuid), typeof(Prefab)
            },
            Options = EntityQueryOptions.IncludeDisabled | EntityQueryOptions.IncludePrefab
        };
        
        static EntityQueryDesc LinkedEntityGroupQueryDesc { get; } = new EntityQueryDesc
        {
            All = new ComponentType[]
            {
                typeof(EntityGuid), typeof(LinkedEntityGroup)
            },
            Options = EntityQueryOptions.IncludeDisabled | EntityQueryOptions.IncludePrefab
        };

        // Restore to BuildComponentToEntityMultiHashMap<TComponent> once fix goes in:
        // https://unity3d.atlassian.net/browse/DOTSR-354
        [BurstCompile]
        struct BuildComponentToEntityMultiHashMap : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<EntityGuid> ComponentType;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            
            [WriteOnly] public NativeMultiHashMap<EntityGuid, Entity>.ParallelWriter ComponentToEntity;

            public void Execute(ArchetypeChunk chunk, int entityIndex, int chunkIndex)
            {
                var components = chunk.GetNativeArray(ComponentType);
                var entities = chunk.GetNativeArray(EntityType);
                for (var i = 0; i != entities.Length; i++)
                {
                    ComponentToEntity.Add(components[i], entities[i]);
                }
            }
        }
        
        // Restore to BuildComponentToEntityMultiHashMap<TComponent> once fix goes in:
        // https://unity3d.atlassian.net/browse/DOTSR-354
        [BurstCompile]
        struct BuildComponentToEntityHashMap: IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<EntityGuid> ComponentType;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            
            [WriteOnly] public NativeHashMap<EntityGuid, Entity>.ParallelWriter ComponentToEntity;

            public void Execute(ArchetypeChunk chunk, int entityIndex, int chunkIndex)
            {
                var components = chunk.GetNativeArray(ComponentType);
                var entities = chunk.GetNativeArray(EntityType);
                for (var i = 0; i != entities.Length; i++)
                {
                    ComponentToEntity.TryAdd(components[i], entities[i]);
                }
            }
        }
        
        // Restore to BuildComponentToEntityMultiHashMap<TComponent> once fix goes in:
        // https://unity3d.atlassian.net/browse/DOTSR-354
        [BurstCompile]
        struct BuildEntityToComponentHashMap : IJobChunk
        {
            [ReadOnly] public ArchetypeChunkComponentType<EntityGuid> EntityGuidComponentType;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            
            [WriteOnly] public NativeHashMap<Entity, EntityGuid>.ParallelWriter EntityToEntityGuid;

            public void Execute(ArchetypeChunk chunk, int entityIndex, int chunkIndex)
            {
                var components = chunk.GetNativeArray(EntityGuidComponentType);
                var entities = chunk.GetNativeArray(EntityType);
                for (var i = 0; i != entities.Length; i++)
                {
                    EntityToEntityGuid.TryAdd(entities[i], components[i]);
                }
            }
        }
        
        [BurstCompile]
        struct CalculateLinkedEntityGroupEntitiesLengthJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public int* Count;
            [ReadOnly] public NativeArray<ArchetypeChunk> Chunks;
            [ReadOnly] public ArchetypeChunkBufferType<LinkedEntityGroup> LinkedEntityGroupType;
            
            public void Execute()
            {
                var count = 0;
                for (var chunkIndex = 0; chunkIndex < Chunks.Length; chunkIndex++)
                {
                    var linkedEntityGroups = Chunks[chunkIndex].GetBufferAccessor(LinkedEntityGroupType);
                    for (var linkedEntityGroupIndex = 0; linkedEntityGroupIndex < linkedEntityGroups.Length; linkedEntityGroupIndex++)
                    {
                        count += linkedEntityGroups[linkedEntityGroupIndex].Length; 
                    }
                }

                *Count = count;
            }
        }
        
        [BurstCompile]
        struct BuildLinkedEntityGroupHashMap : IJobChunk
        {
            [WriteOnly] public NativeHashMap<Entity, Entity>.ParallelWriter EntityToLinkedEntityGroupRoot;
            [ReadOnly] public ArchetypeChunkBufferType<LinkedEntityGroup> LinkedEntityGroupType;
            
            public void Execute(ArchetypeChunk chunk, int entityIndex, int chunkIndex)
            {
                var linkedEntityGroups = chunk.GetBufferAccessor(LinkedEntityGroupType);
                
                for (var bufferIndex = 0; bufferIndex != linkedEntityGroups.Length; bufferIndex++)
                {
                    var linkedEntityGroup = linkedEntityGroups[bufferIndex];
                    for (var elementIndex = 0; elementIndex != linkedEntityGroup.Length; elementIndex++)
                    {
                        EntityToLinkedEntityGroupRoot.TryAdd(linkedEntityGroup[elementIndex].Value, linkedEntityGroup[0].Value);
                    }
                }
            }
        }
        
        [BurstCompile]
        struct BuildPackedEntityLookupJob : IJobParallelFor
        {
            public int StartIndex;
            [ReadOnly] public NativeArray<EntityGuid> EntityGuids;
            [ReadOnly] public NativeMultiHashMap<EntityGuid, Entity> EntityGuidToEntity;
            [WriteOnly] public NativeMultiHashMap<int, Entity>.ParallelWriter PackedEntities;
            
            public void Execute(int index)
            {
                var entityGuid = EntityGuids[index + StartIndex];
                if (EntityGuidToEntity.TryGetFirstValue(entityGuid, out var entity, out var iterator))
                {
                    do
                    {
                        PackedEntities.Add(index + StartIndex, entity);
                    }
                    while (EntityGuidToEntity.TryGetNextValue(out entity, ref iterator));
                }
            }
        }

        struct BuildPackedTypeLookupJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ComponentTypeHash> TypeHashes;
            [WriteOnly] public NativeArray<ComponentType> PackedTypes;
            
            public void Execute(int index)
            {
                var typeHash = TypeHashes[index];
                var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(typeHash.StableTypeHash);
                var type = TypeManager.GetType(typeIndex);
                ComponentType componentType;
                if ((typeHash.Flags & ComponentTypeFlags.ChunkComponent) == ComponentTypeFlags.ChunkComponent)
                {
                    componentType = ComponentType.ChunkComponent(type);
                }
                else
                {
                    componentType = new ComponentType(type);
                }
                PackedTypes[index] = componentType;
            }
        }

        /// <summary>
        /// Applies the given change set to the given entity manager.
        /// </summary>
        /// <param name="entityManager">The <see cref="EntityManager"/> to apply the change set to.</param>
        /// <param name="changeSet">The <see cref="EntityChangeSet"/> to apply.</param>
        public static void ApplyChangeSet(EntityManager entityManager, EntityChangeSet changeSet)
        {
            if (!changeSet.IsCreated)
            {
                return;
            }
            
#if ENABLE_PROFILER || UNITY_EDITOR
            s_ApplyChangeSetProfilerMarker.Begin();
#endif

            var entityQuery = entityManager.CreateEntityQuery(EntityGuidQueryDesc);
            var prefabQuery = entityManager.CreateEntityQuery(PrefabQueryDesc);
            var linkedEntityGroupQuery = entityManager.CreateEntityQuery(LinkedEntityGroupQueryDesc);

            var entityCount = entityQuery.CalculateEntityCount();

            using (var packedEntities = new NativeMultiHashMap<int, Entity>(entityCount, Allocator.TempJob))
            using (var packedTypes = new NativeArray<ComponentType>(changeSet.TypeHashes.Length, Allocator.TempJob))
            using (var entityGuidToEntity = new NativeMultiHashMap<EntityGuid, Entity>(entityCount, Allocator.TempJob))
            using (var entityToEntityGuid = new NativeHashMap<Entity, EntityGuid>(entityQuery.CalculateEntityCount(), Allocator.TempJob))
            {
                BuildEntityLookups(
                    entityManager,
                    entityQuery,
                    entityGuidToEntity,
                    entityToEntityGuid);

                BuildPackedLookups(
                    changeSet,
                    entityGuidToEntity,
                    packedEntities,
                    packedTypes);

                ApplyDestroyEntities(
                    entityManager,
                    changeSet,
                    packedEntities,
                    entityGuidToEntity);

                ApplyCreateEntities(
                    entityManager,
                    changeSet,
                    packedEntities);

#if UNITY_EDITOR
                ApplyEntityNames(
                    entityManager,
                    changeSet,
                    packedEntities);
#endif

                ApplyRemoveComponents(
                    entityManager,
                    changeSet.RemoveComponents,
                    changeSet.Entities,
                    packedEntities,
                    packedTypes);

                ApplyAddComponents(
                    entityManager,
                    changeSet.AddComponents,
                    changeSet.Entities,
                    packedEntities,
                    packedTypes);

                ApplySetSharedComponents(
                    entityManager,
                    changeSet.SetSharedComponents,
                    changeSet.Entities,
                    packedEntities,
                    packedTypes);

                ApplySetManagedComponents(
                    entityManager,
                    changeSet.SetManagedComponents,
                    changeSet.Entities,
                    packedEntities,
                    packedTypes);

                ApplySetComponents(
                    entityManager,
                    changeSet.SetComponents,
                    changeSet.ComponentData,
                    changeSet.Entities,
                    packedEntities,
                    packedTypes,
                    entityGuidToEntity,
                    entityToEntityGuid);

                var linkedEntityGroupEntitiesLength = CalculateLinkedEntityGroupEntitiesLength(entityManager, linkedEntityGroupQuery);

                using (var entityGuidToPrefab = new NativeHashMap<EntityGuid, Entity>(prefabQuery.CalculateEntityCount(), Allocator.TempJob))
                using (var entityToLinkedEntityGroupRoot = new NativeHashMap<Entity, Entity>(linkedEntityGroupEntitiesLength, Allocator.TempJob))
                {
                    BuildPrefabAndLinkedEntityGroupLookups(
                        entityManager,
                        entityQuery,
                        prefabQuery,
                        linkedEntityGroupQuery,
                        entityGuidToPrefab,
                        entityToLinkedEntityGroupRoot);

                    ApplyLinkedEntityGroupRemovals(
                        entityManager,
                        changeSet.LinkedEntityGroupRemovals,
                        changeSet.Entities,
                        packedEntities,
                        entityGuidToEntity,
                        entityToEntityGuid,
                        entityToLinkedEntityGroupRoot);

                    ApplyLinkedEntityGroupAdditions(
                        entityManager,
                        changeSet.LinkedEntityGroupAdditions,
                        changeSet.Entities,
                        packedEntities,
                        entityGuidToEntity,
                        entityToEntityGuid,
                        entityGuidToPrefab,
                        entityToLinkedEntityGroupRoot);

                    ApplyEntityPatches(
                        entityManager,
                        changeSet.EntityReferenceChanges,
                        changeSet.Entities,
                        packedEntities,
                        packedTypes,
                        entityGuidToEntity,
                        entityToEntityGuid,
                        entityGuidToPrefab,
                        entityToLinkedEntityGroupRoot);
                }
                
                ApplyBlobAssetChanges(
                    entityManager,
                    changeSet.Entities,
                    packedEntities,
                    packedTypes,
                    changeSet.CreatedBlobAssets,
                    changeSet.BlobAssetData,
                    changeSet.DestroyedBlobAssets,
                    changeSet.BlobAssetReferenceChanges);
            }
#if ENABLE_PROFILER || UNITY_EDITOR
            s_ApplyChangeSetProfilerMarker.End();
#endif
        }
        
        /// <summary>
        /// Builds a lookup of <see cref="NativeMultiHashMap{TEntityGuidComponent, Entity}"/> for the target world.
        /// </summary>
        /// <remarks>
        /// This will run over ALL entities in the world. This is very expensive.
        /// </remarks>
        static void BuildEntityLookups(
            EntityManager entityManager,
            EntityQuery entityQuery,
            NativeMultiHashMap<EntityGuid, Entity> entityGuidToEntity,
            NativeHashMap<Entity, EntityGuid> entityToEntityGuid)
        {
            var buildEntityGuidToEntity = new BuildComponentToEntityMultiHashMap
            {
                EntityType = entityManager.GetArchetypeChunkEntityType(),
                ComponentType = entityManager.GetArchetypeChunkComponentType<EntityGuid>(true),
                ComponentToEntity = entityGuidToEntity.AsParallelWriter()
            }.Schedule(entityQuery);

            var buildEntityToEntityGuid = new BuildEntityToComponentHashMap
            {
                EntityType = entityManager.GetArchetypeChunkEntityType(),
                EntityGuidComponentType = entityManager.GetArchetypeChunkComponentType<EntityGuid>(true),
                EntityToEntityGuid = entityToEntityGuid.AsParallelWriter()
            }.Schedule(entityQuery);

            JobHandle.CombineDependencies(buildEntityGuidToEntity, buildEntityToEntityGuid).Complete();
        }

        static void BuildPrefabAndLinkedEntityGroupLookups(
            EntityManager entityManager,
            EntityQuery entityQuery,
            EntityQuery prefabQuery,
            EntityQuery linkedEntityGroupQuery,
            NativeHashMap<EntityGuid, Entity> entityGuidToPrefab,
            NativeHashMap<Entity, Entity> entityToLinkedEntityGroupRoot)
        {
            var buildPrefabLookups = new BuildComponentToEntityHashMap
            {
                EntityType = entityManager.GetArchetypeChunkEntityType(),
                ComponentType = entityManager.GetArchetypeChunkComponentType<EntityGuid>(true),
                ComponentToEntity = entityGuidToPrefab.AsParallelWriter()
            }.Schedule(prefabQuery);

            var buildLinkedEntityGroupLookups = new BuildLinkedEntityGroupHashMap
            {
                EntityToLinkedEntityGroupRoot = entityToLinkedEntityGroupRoot.AsParallelWriter(),
                LinkedEntityGroupType = entityManager.GetArchetypeChunkBufferType<LinkedEntityGroup>(true)
            }.Schedule(linkedEntityGroupQuery);

            JobHandle.CombineDependencies(buildPrefabLookups, buildLinkedEntityGroupLookups).Complete();
        }

        /// <summary>
        /// This method will generate lookups into the packed change set.
        ///
        /// 1) Maps existing entities in the world to <see cref="EntityChangeSet.Entities"/>
        /// 2) Maps types in the world to <see cref="EntityChangeSet.TypeHashes"/>
        ///
        /// These tables are used by subsequent methods to quickly access the packed data.
        /// </summary>
        static void BuildPackedLookups(
            EntityChangeSet changeSet,
            NativeMultiHashMap<EntityGuid, Entity> entityGuidToEntity,
            NativeMultiHashMap<int, Entity> packedEntities,
            NativeArray<ComponentType> packedTypes)
        {
            var buildPackedEntityLookups = new BuildPackedEntityLookupJob
            {
                StartIndex = changeSet.CreatedEntityCount,
                EntityGuids = changeSet.Entities,
                EntityGuidToEntity = entityGuidToEntity,
                PackedEntities = packedEntities.AsParallelWriter()
            }.Schedule(changeSet.Entities.Length - changeSet.CreatedEntityCount, 64);

            var buildPackedTypeLookups = new BuildPackedTypeLookupJob
            {
                TypeHashes = changeSet.TypeHashes,
                PackedTypes = packedTypes,
            }.Schedule(changeSet.TypeHashes.Length, 64);

            JobHandle.CombineDependencies(buildPackedEntityLookups, buildPackedTypeLookups).Complete();
        }

        /// <summary>
        /// Creates all new entities described in the <see cref="EntityChangeSet"/>
        /// </summary>
        /// <remarks>
        /// This method only creates the entities and does not set any data.
        /// </remarks>
        static void ApplyCreateEntities(
            EntityManager entityManager,
            EntityChangeSet changeSet,
            NativeMultiHashMap<int, Entity> packedEntities)
        {
            var types = stackalloc ComponentType[0];
            var entityGuidArchetype = entityManager.CreateArchetype(types, 0);
            using (var entities = new NativeArray<Entity>(changeSet.CreatedEntityCount, Allocator.Temp))
            {
                entityManager.CreateEntity(entityGuidArchetype, entities);
                for (var i = 0; i < changeSet.CreatedEntityCount; ++i)
                {
                    packedEntities.Add(i, entities[i]);
                }
            }
        }

#if UNITY_EDITOR
        static void ApplyEntityNames(
            EntityManager entityManager,
            EntityChangeSet changeSet,
            NativeMultiHashMap<int, Entity> packedEntities)
        {
            for (var i = 0; i < changeSet.Entities.Length; i++)
            {
                if (packedEntities.TryGetFirstValue(i, out var entity, out var it))
                {
                    do
                    {
                        entityManager.SetName(entity, changeSet.Names[i].ToString());
                    }
                    while (packedEntities.TryGetNextValue(out entity, ref it));
                }
            }
        }

#endif

        /// <summary>
        /// Destroys all entities described in the <see cref="EntityChangeSet"/>
        /// </summary>
        /// <remarks>
        /// Since building the <see cref="NativeMultiHashMap{TEntityGuidComponent, Entity}"/> the entire world is expensive
        /// this method will incrementally update the map based on the destroyed entities.
        /// </remarks>
        static void ApplyDestroyEntities(
            EntityManager entityManager,
            EntityChangeSet changeSet,
            NativeMultiHashMap<int, Entity> packedEntities,
            NativeMultiHashMap<EntityGuid, Entity> entityGuidToEntity)
        {
            for (var i = changeSet.Entities.Length - changeSet.DestroyedEntityCount; i < changeSet.Entities.Length; i++)
            {
                if (!packedEntities.TryGetFirstValue(i, out var entity, out var iterator))
                {
                    continue;
                }

                do
                {
                    // Perform incremental updates on the entityGuidToEntity map to avoid a full rebuild.
                    // @NOTE We do NOT remove from the `entityToEntityGuid` here since the LinkedEntityGroup removal will need it to map back groups.
                    entityGuidToEntity.Remove(changeSet.Entities[i], entity);

                    if (entityManager.EntityComponentStore->Exists(entity))
                    {
                        entityManager.DestroyEntity(entity);
                    }
                    else
                    {
                        Debug.LogWarning($"DestroyEntity({entity}) but it does not exist.");
                    }
                }
                while (packedEntities.TryGetNextValue(out entity, ref iterator));
            }
        }

        static void ApplyAddComponents(
            EntityManager entityManager,
            NativeArray<PackedComponent> addComponents,
            NativeArray<EntityGuid> packedEntityGuids,
            NativeMultiHashMap<int, Entity> packedEntities,
            NativeArray<ComponentType> packedTypes)
        {
            var linkedEntityGroupTypeIndex = TypeManager.GetTypeIndex<LinkedEntityGroup>();

            for (var i = 0; i < addComponents.Length; i++)
            {
                var packedComponent = addComponents[i];

                if (!packedEntities.TryGetFirstValue(packedComponent.PackedEntityIndex, out var entity, out var iterator))
                {
                    continue;
                }

                var component = packedTypes[packedComponent.PackedTypeIndex];

                do
                {
                    if (!entityManager.EntityComponentStore->HasComponent(entity, component))
                    {
                        entityManager.AddComponent(entity, component);

                        // magic is required to force the first entity in the LinkedEntityGroup to be the entity
                        // that owns the component. this magic doesn't seem to exist at a lower level, so let's
                        // shim it in here. we'll probably need to move the magic lower someday.
                        if (component.TypeIndex == linkedEntityGroupTypeIndex)
                        {
                            var buffer = entityManager.GetBuffer<LinkedEntityGroup>(entity);
                            buffer.Add(entity);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"AddComponent({packedEntityGuids[packedComponent.PackedEntityIndex]}, {component}) but the component already exists.");
                    }
                }
                while (packedEntities.TryGetNextValue(out entity, ref iterator));
            }
        }

        static void ApplyRemoveComponents(
            EntityManager entityManager,
            NativeArray<PackedComponent> removeComponents,
            NativeArray<EntityGuid> packedEntityGuids,
            NativeMultiHashMap<int, Entity> packedEntities,
            NativeArray<ComponentType> packedTypes)
        {
            var entityGuidTypeIndex = TypeManager.GetTypeIndex<EntityGuid>();

            for (var i = 0; i < removeComponents.Length; i++)
            {
                var packedComponent = removeComponents[i];

                if (!packedEntities.TryGetFirstValue(packedComponent.PackedEntityIndex, out var entity, out var iterator))
                {
                    continue;
                }

                var component = packedTypes[packedComponent.PackedTypeIndex];

                do
                {
                    if (component.TypeIndex == entityGuidTypeIndex)
                    {
                        // @TODO Add test cases around this.
                        // Should entityGuidToEntity be updated or should we throw and error.
                    }

                    if (entityManager.EntityComponentStore->HasComponent(entity, component))
                    {
                        entityManager.RemoveComponent(entity, component);
                    }
                    else
                    {
                        Debug.LogWarning($"RemoveComponent({packedEntityGuids[packedComponent.PackedEntityIndex]}, {component}) but the component already exists.");
                    }
                }
                while (packedEntities.TryGetNextValue(out entity, ref iterator));
            }
        }

        static void ApplySetSharedComponents(
            EntityManager entityManager,
            PackedSharedComponentDataChange[] sharedComponentDataChanges,
            NativeArray<EntityGuid> packedEntityGuid,
            NativeMultiHashMap<int, Entity> packedEntities,
            NativeArray<ComponentType> packedTypes)
        {
            for (var i = 0; i < sharedComponentDataChanges.Length; i++)
            {
                var packedSharedComponentDataChange = sharedComponentDataChanges[i];
                var packedComponent = packedSharedComponentDataChange.Component;

                if (!packedEntities.TryGetFirstValue(packedComponent.PackedEntityIndex, out var entity, out var iterator))
                {
                    continue;
                }

                var component = packedTypes[packedComponent.PackedTypeIndex];

                do
                {
                    if (!entityManager.Exists(entity))
                    {
                        Debug.LogWarning($"SetComponent<{component}>({packedEntityGuid[packedComponent.PackedEntityIndex]}) but entity does not exist.");
                    }
                    else if (!entityManager.HasComponent(entity, component))
                    {
                        Debug.LogWarning($"SetComponent<{component}>({packedEntityGuid[packedComponent.PackedEntityIndex]}) but component does not exist.");
                    }
                    else
                    {
                        entityManager.SetSharedComponentDataBoxedDefaultMustBeNull(entity, component.TypeIndex, packedSharedComponentDataChange.BoxedSharedValue);
                    }
                }
                while (packedEntities.TryGetNextValue(out entity, ref iterator));
            }
        }

        static void ApplySetManagedComponents(
            EntityManager entityManager,
            PackedManagedComponentDataChange[] managedComponentDataChanges,
            NativeArray<EntityGuid> packedEntityGuid,
            NativeMultiHashMap<int, Entity> packedEntities,
            NativeArray<ComponentType> packedTypes)
        {
            for (var i = 0; i < managedComponentDataChanges.Length; i++)
            {
                var packedManagedComponentDataChange = managedComponentDataChanges[i];
                var packedComponent = packedManagedComponentDataChange.Component;

                if (!packedEntities.TryGetFirstValue(packedComponent.PackedEntityIndex, out var entity, out var iterator))
                {
                    continue;
                }

                var component = packedTypes[packedComponent.PackedTypeIndex];

                do
                {
                    if (!entityManager.Exists(entity))
                    {
                        Debug.LogWarning($"SetComponent<{component}>({packedEntityGuid[packedComponent.PackedEntityIndex]}) but entity does not exist.");
                    }
                    else if (!entityManager.HasComponent(entity, component))
                    {
                        Debug.LogWarning($"SetComponent<{component}>({packedEntityGuid[packedComponent.PackedEntityIndex]}) but component does not exist.");
                    }
                    else
                    {
                        entityManager.SetComponentObject(entity, component, packedManagedComponentDataChange.BoxedValue);
                    }
                }
                while (packedEntities.TryGetNextValue(out entity, ref iterator));
            }
        }

        static void ApplySetComponents(
            EntityManager entityManager,
            NativeArray<PackedComponentDataChange> changes,
            NativeArray<byte> payload,
            NativeArray<EntityGuid> packedEntityGuids,
            NativeMultiHashMap<int, Entity> packedEntities,
            NativeArray<ComponentType> packedTypes,
            NativeMultiHashMap<EntityGuid, Entity> entityGuidToEntity,
            NativeHashMap<Entity, EntityGuid> entityToEntityGuid)
        {
            var entityGuidTypeIndex = TypeManager.GetTypeIndex<EntityGuid>();

            var offset = 0L;
            for (var i = 0; i < changes.Length; i++)
            {
                var packedComponentDataChange = changes[i];
                var packedComponent = packedComponentDataChange.Component;
                var component = packedTypes[packedComponent.PackedTypeIndex];
                var size = packedComponentDataChange.Size;
                var data = (byte*)payload.GetUnsafeReadOnlyPtr() + offset;
                var componentTypeInArchetype = new ComponentTypeInArchetype(component);

                if (packedEntities.TryGetFirstValue(packedComponent.PackedEntityIndex, out var entity, out var iterator))
                {
                    do
                    {
                        if (!entityManager.Exists(entity))
                        {
                            Debug.LogWarning($"SetComponent<{component}>({packedEntityGuids[packedComponent.PackedEntityIndex]}) but entity does not exist.");
                        }
                        else if (!entityManager.HasComponent(entity, component))
                        {
                            Debug.LogWarning($"SetComponent<{component}>({packedEntityGuids[packedComponent.PackedEntityIndex]}) but component does not exist.");
                        }
                        else
                        {
                            if (componentTypeInArchetype.IsZeroSized)
                            {
                                // Nothing to set.
                            }
                            else if (componentTypeInArchetype.IsBuffer)
                            {
                                var typeInfo = TypeManager.GetTypeInfo(componentTypeInArchetype.TypeIndex);
                                var elementSize = typeInfo.ElementSize;
                                var lengthInElements = size / elementSize;
                                var header = (BufferHeader*)entityManager.GetComponentDataRawRW(entity, component.TypeIndex);
                                BufferHeader.Assign(header, data, lengthInElements, elementSize, 16, false, 0);
                            }
                            else
                            {
                                var target = (byte*)entityManager.GetComponentDataRawRW(entity, component.TypeIndex);

                                // Perform incremental updates on the entityGuidToEntity map to avoid a full rebuild.
                                if (componentTypeInArchetype.TypeIndex == entityGuidTypeIndex)
                                {
                                    EntityGuid entityGuid;
                                    UnsafeUtility.MemCpy(&entityGuid, target, sizeof(EntityGuid));

                                    if (!entityGuid.Equals(default))
                                    {
                                        entityGuidToEntity.Remove(entityGuid, entity);
                                    }

                                    UnsafeUtility.MemCpy(&entityGuid, data + packedComponentDataChange.Offset, size);
                                    entityGuidToEntity.Add(entityGuid, entity);
                                    entityToEntityGuid.TryAdd(entity, entityGuid);
                                }

                                UnsafeUtility.MemCpy(target + packedComponentDataChange.Offset, data, size);
                            }
                        }
                    }
                    while (packedEntities.TryGetNextValue(out entity, ref iterator));
                }

                offset += size;
            }
        }

        internal struct OffsetEntityPair
        {
            public int Offset;
            public Entity TargetEntity;
        }

        internal struct EntityComponentPair : IEquatable<EntityComponentPair>
        {
            public Entity Entity;
            public ComponentType Component;

            public bool Equals(EntityComponentPair other)
            {
                return Entity == other.Entity && Component == other.Component;
            }
        }

        static void ApplyEntityPatches(
            EntityManager entityManager,
            NativeArray<EntityReferenceChange> changes,
            NativeArray<EntityGuid> packedEntityGuids,
            NativeMultiHashMap<int, Entity> packedEntities,
            NativeArray<ComponentType> packedTypes,
            NativeMultiHashMap<EntityGuid, Entity> entityGuidToEntity,
            NativeHashMap<Entity, EntityGuid> entityToEntityGuid,
            NativeHashMap<EntityGuid, Entity> entityGuidToPrefab,
            NativeHashMap<Entity, Entity> entityToLinkedEntityGroupRoot)
        {
            NativeMultiHashMap<EntityComponentPair, OffsetEntityPair> managedComponentPatchMap = new NativeMultiHashMap<EntityComponentPair, OffsetEntityPair>(changes.Length, Allocator.Temp);

            for (var i = 0; i < changes.Length; i++)
            {
                var patch = changes[i];
                var packedComponent = patch.Component;
                var component = packedTypes[packedComponent.PackedTypeIndex];
                var targetEntityGuid = patch.Value;
                var targetOffset = patch.Offset;
                var multipleTargetEntities = false;
                Entity targetEntity;

                if (targetEntityGuid.Equals(default))
                {
                    targetEntity = Entity.Null;
                }
                else
                {
                    if (!entityGuidToEntity.TryGetFirstValue(targetEntityGuid, out targetEntity, out var patchSourceIterator))
                    {
                        Debug.LogWarning($"PatchEntities<{component}>({packedEntityGuids[packedComponent.PackedEntityIndex]}) but entity with guid-to-patch-to does not exist.");
                        continue;
                    }
                    multipleTargetEntities = entityGuidToEntity.TryGetNextValue(out _, ref patchSourceIterator);
                }

                if (packedEntities.TryGetFirstValue(packedComponent.PackedEntityIndex, out var entity, out var iterator))
                {
                    do
                    {
                        if (!entityManager.Exists(entity))
                        {
                            Debug.LogWarning($"PatchEntities<{component}>({packedEntityGuids[packedComponent.PackedEntityIndex]}) but entity to patch does not exist.");
                        }
                        else if (!entityManager.HasComponent(entity, component))
                        {
                            Debug.LogWarning($"PatchEntities<{component}>({packedEntityGuids[packedComponent.PackedEntityIndex]}) but component in entity to patch does not exist.");
                        }
                        else
                        {
                            // If just one entity has the GUID we're patching to, we can just use that entity.
                            // but if multiple entities have that GUID, we need to patch to the (one) entity that's in the destination entity's "group."
                            // that group is defined by a LinkedEntityGroup component on the destination entity's "root entity," which contains an array of entity references.
                            // the destination entity's "root entity" is defined by whatever entity owns the (one) LinkedEntityGroup that refers to the destination entity.
                            // so, we had to build a lookup table earlier, to take us from "destination entity" to "root entity of my group," so we can find this LinkedEntityGroup
                            // component, and riffle through it to find the (one) entity with the GUID we're looking for.
                            if (multipleTargetEntities)
                            {
                                targetEntity = Entity.Null;

                                if (entityToLinkedEntityGroupRoot.TryGetValue(entity, out var linkedEntityGroupRoot))
                                {
                                    // This entity is part of a LinkedEntityGroup
                                    var linkedEntityGroup = entityManager.GetBuffer<LinkedEntityGroup>(linkedEntityGroupRoot);

                                    // Scan through the group and look for the entity with the target entityGuid.
                                    for (var elementIndex = 0; elementIndex < linkedEntityGroup.Length; elementIndex++)
                                    {
                                        // Get the entityGuid from each element.
                                        if (entityToEntityGuid.TryGetValue(linkedEntityGroup[elementIndex].Value, out var entityGuidInGroup))
                                        {
                                            if (entityGuidInGroup.Equals(targetEntityGuid))
                                            {
                                                // Match found this is our entity
                                                targetEntity = linkedEntityGroup[elementIndex].Value;
                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // We are not dealing with a LinkedEntityGroup at this point, let's hope it's a prefab.
                                    if (!entityGuidToPrefab.TryGetValue(targetEntityGuid, out targetEntity))
                                    {
                                        Debug.LogWarning($"PatchEntities<{component}>({packedEntityGuids[packedComponent.PackedEntityIndex]}) but 2+ entities for GUID of entity-to-patch-to, and no root for entity-to-patch is, so we can't disambiguate.");
                                        continue;
                                    }
                                }
                            }

                            if (component.IsBuffer)
                            {
                                var pointer = (byte*)entityManager.GetBufferRawRW(entity, component.TypeIndex);
                                UnsafeUtility.MemCpy(pointer + targetOffset, &targetEntity, sizeof(Entity));
                            }
                            else if(component.IsManagedComponent)
                            {
                                managedComponentPatchMap.Add(
                                    new EntityComponentPair() { Entity = entity, Component = component }, 
                                    new OffsetEntityPair() { Offset = targetOffset, TargetEntity = targetEntity });
                            }
                            else
                            {
                                var pointer = (byte*)entityManager.GetComponentDataRawRW(entity, component.TypeIndex);
                                UnsafeUtility.MemCpy(pointer + targetOffset, &targetEntity, sizeof(Entity));
                            }
                        }
                    }
                    while (packedEntities.TryGetNextValue(out entity, ref iterator));
                }
            }

            // Apply all managed entity patches
            using (var keys = managedComponentPatchMap.GetKeyArray(Allocator.Temp))
            {
                foreach(var ecPair in keys)
                {
                    var obj = entityManager.GetManagedComponentDataAsObject(ecPair.Entity, ecPair.Component);
                    var patches = managedComponentPatchMap.GetValuesForKey(ecPair);

                    PatchEntitiesInObject(obj, patches);
                    patches.Dispose();
                }
            }
            managedComponentPatchMap.Dispose();
        }

        struct Child
        {
            public Entity RootEntity;
            public Entity ChildEntity;
            public EntityGuid ChildEntityGuid;
        }

        static void ApplyLinkedEntityGroupAdditions(
            EntityManager entityManager,
            NativeArray<LinkedEntityGroupChange> linkedEntityGroupChanges,
            NativeArray<EntityGuid> packedEntityGuids,
            NativeMultiHashMap<int, Entity> packedEntities,
            NativeMultiHashMap<EntityGuid, Entity> entityGuidToEntity,
            NativeHashMap<Entity, EntityGuid> entityToEntityGuid,
            NativeHashMap<EntityGuid, Entity> entityGuidToPrefab,
            NativeHashMap<Entity, Entity> entityToLinkedEntityGroupRoot)
        {
            using (var additions = new NativeList<Child>(Allocator.TempJob))
            {
                for (var i = 0; i < linkedEntityGroupChanges.Length; i++)
                {
                    var linkedEntityGroupAddition = linkedEntityGroupChanges[i];

                    // If we are asked to add a child to a linked entity group, then that child's guid must correspond to
                    // exactly one entity in the destination world that also has a Prefab component. Since we made a lookup
                    // from EntityGuid to Prefab entity before, we can use it to find the specific entity we want.
                    if (entityGuidToPrefab.TryGetValue(linkedEntityGroupAddition.ChildEntityGuid, out var prefabEntityToInstantiate))
                    {
                        if (entityGuidToEntity.TryGetFirstValue(linkedEntityGroupAddition.RootEntityGuid, out var rootEntity, out var iterator))
                        {
                            do
                            {
                                if (rootEntity == prefabEntityToInstantiate)
                                {
                                    Debug.LogWarning($"Trying to instantiate self as child?");
                                    continue;
                                }

                                if (entityManager.HasComponent<Prefab>(rootEntity))
                                {
                                    entityManager.GetBuffer<LinkedEntityGroup>(rootEntity).Add(prefabEntityToInstantiate);
                                    entityToLinkedEntityGroupRoot.TryAdd(prefabEntityToInstantiate, rootEntity);
                                }
                                else
                                {
                                    var instantiatedEntity = entityManager.Instantiate(prefabEntityToInstantiate);
                                    var linkedEntityGroup = entityManager.GetBuffer<LinkedEntityGroup>(rootEntity);
                                    linkedEntityGroup.Add(instantiatedEntity);

                                    additions.Add(new Child
                                    {
                                        RootEntity = rootEntity,
                                        ChildEntity = instantiatedEntity,
                                        ChildEntityGuid = linkedEntityGroupAddition.ChildEntityGuid
                                    });
                                }
                            }
                            while (entityGuidToEntity.TryGetNextValue(out rootEntity, ref iterator));
                        }
                        else
                        {
                            Debug.LogWarning($"Tried to add a child to a linked entity group, but root entity didn't exist in destination world.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Tried to add a child to a linked entity group, but no such prefab exists in destination world.");
                    }
                }

                for (var i = 0; i < additions.Length; i++)
                {
                    var addition = additions[i];
                    for (var packedEntityGuidIndex = 0; packedEntityGuidIndex < packedEntityGuids.Length; ++packedEntityGuidIndex)
                    {
                        if (!packedEntityGuids[packedEntityGuidIndex].Equals(addition.ChildEntityGuid))
                        {
                            continue;
                        }

                        packedEntities.Add(packedEntityGuidIndex, addition.ChildEntity);
                        break;
                    }

                    entityToEntityGuid.TryAdd(addition.ChildEntity, addition.ChildEntityGuid);
                    entityGuidToEntity.Add(addition.ChildEntityGuid, addition.ChildEntity);
                    entityToLinkedEntityGroupRoot.TryAdd(addition.ChildEntity, addition.RootEntity);
                }
            }
        }

        static void ApplyLinkedEntityGroupRemovals(
            EntityManager entityManager,
            NativeArray<LinkedEntityGroupChange> linkedEntityGroupChanges,
            NativeArray<EntityGuid> packedEntityGuids,
            NativeMultiHashMap<int, Entity> packedEntities,
            NativeMultiHashMap<EntityGuid, Entity> entityGuidToEntity,
            NativeHashMap<Entity, EntityGuid> entityToEntityGuid,
            NativeHashMap<Entity, Entity> entityToLinkedEntityGroupRoot)
        {
            using (var removals = new NativeList<Child>(Allocator.TempJob))
            {
                for (var i = 0; i < linkedEntityGroupChanges.Length; ++i)
                {
                    var linkedEntityGroupRemoval = linkedEntityGroupChanges[i];
                    if (entityGuidToEntity.TryGetFirstValue(linkedEntityGroupRemoval.RootEntityGuid, out var rootEntity, out var iterator))
                    {
                        do
                        {
                            var linkedEntityGroup = entityManager.GetBuffer<LinkedEntityGroup>(rootEntity);

                            // Look for the remove child in the LinkedEntityGroupBuffer
                            for (var bufferIndex = 0; bufferIndex < linkedEntityGroup.Length; bufferIndex++)
                            {
                                var childEntity = linkedEntityGroup[bufferIndex].Value;

                                if (entityToEntityGuid.TryGetValue(childEntity, out var childEntityGuid) &&
                                    childEntityGuid.Equals(linkedEntityGroupRemoval.ChildEntityGuid))
                                {
                                    // This entity does not exist. It was most likely destroyed.
                                    // Remove it from the LinkedEntityGroup
                                    linkedEntityGroup.RemoveAt(bufferIndex);

                                    removals.Add(new Child
                                    {
                                        RootEntity = rootEntity,
                                        ChildEntity = childEntity,
                                        ChildEntityGuid = linkedEntityGroupRemoval.ChildEntityGuid,
                                    });

                                    if (entityManager.EntityComponentStore->Exists(childEntity))
                                    {
                                        entityManager.DestroyEntity(childEntity);
                                    }
                                    break;
                                }
                            }

                            // if we got here without destroying an entity, then maybe the destination world destroyed it before we synced?
                            // not sure if that is a fatal error, or what.
                        }
                        while (entityGuidToEntity.TryGetNextValue(out rootEntity, ref iterator));
                    }
                }

                for (var i = 0; i < removals.Length; ++i)
                {
                    var removal = removals[i];

                    for (var packedEntityGuidIndex = 0; packedEntityGuidIndex < packedEntityGuids.Length; ++packedEntityGuidIndex)
                    {
                        if (packedEntityGuids[packedEntityGuidIndex].Equals(removal.ChildEntityGuid))
                        {
                            packedEntities.Remove(packedEntityGuidIndex, removal.ChildEntity);
                            break;
                        }
                    }

                    entityToEntityGuid.Remove(removal.ChildEntity);
                    entityGuidToEntity.Remove(removal.ChildEntityGuid, removal.ChildEntity);
                    entityToLinkedEntityGroupRoot.Remove(removal.ChildEntity);
                }
            }
        }

        static int CalculateLinkedEntityGroupEntitiesLength(EntityManager entityManager, EntityQuery linkedEntityGroupQuery)
        {
            var count = 0;

            using (var chunks = linkedEntityGroupQuery.CreateArchetypeChunkArray(Allocator.TempJob))
            {
                new CalculateLinkedEntityGroupEntitiesLengthJob
                {
                    Count = &count,
                    Chunks = chunks,
                    LinkedEntityGroupType = entityManager.GetArchetypeChunkBufferType<LinkedEntityGroup>(true)
                }.Schedule().Complete();
            }

            return count;
        }
        internal static void PatchEntitiesInObject(object obj, NativeMultiHashMap<EntityComponentPair, OffsetEntityPair>.Enumerator patches)
        {
#if !NET_DOTS
            var visitor = new EntityDiffPatcher(patches);
            var changeTracker = new ChangeTracker();
            var type = obj.GetType();

            var resolved = PropertyBagResolver.Resolve(type);
            if (resolved != null)
            {
                resolved.Accept(ref obj, ref visitor, ref changeTracker);
            }
            else
                throw new ArgumentException($"Type '{type.FullName}' not supported for visiting.");
#endif
        }

#if !NET_DOTS
        internal unsafe class EntityDiffPatcher : PropertyVisitor
        {
            protected EntityPatchAdapter _EntityPatchAdapter { get; }

            public EntityDiffPatcher(NativeMultiHashMap<EntityComponentPair, OffsetEntityPair>.Enumerator patches)
            {
                _EntityPatchAdapter = new EntityPatchAdapter(patches);
                AddAdapter(_EntityPatchAdapter);
            }

            internal unsafe class EntityPatchAdapter : IPropertyVisitorAdapter
                , IVisitAdapter<Entity>
                , IVisitAdapter
            {
                NativeMultiHashMap<EntityComponentPair, OffsetEntityPair>.Enumerator Patches;

                public unsafe EntityPatchAdapter(NativeMultiHashMap<EntityComponentPair, OffsetEntityPair>.Enumerator patches)
                {
                    Patches = patches;
                }

                public unsafe VisitStatus Visit<TProperty, TContainer>(IPropertyVisitor visitor, TProperty property, ref TContainer container, ref Entity value, ref ChangeTracker changeTracker)
                    where TProperty : IProperty<TContainer, Entity>
                {
                    // Make a copy for we can re-use the enumerator
                    var patches = Patches;
                    foreach(var patch in patches)
                    {
                        if (value.Index == patch.Offset)
                        {
                            value = patch.TargetEntity;
                            break;
                        }
                    }

                    return VisitStatus.Handled;
                }

                public VisitStatus Visit<TProperty, TContainer, TValue>(IPropertyVisitor visitor, TProperty property, ref TContainer container, ref TValue value, ref ChangeTracker changeTracker) where TProperty : IProperty<TContainer, TValue>
                {
                    return VisitStatus.Unhandled;
                }
            }
        }
#endif
    }
}
