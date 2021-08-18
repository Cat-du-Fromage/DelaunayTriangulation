
using System.Collections;
using System.Collections.Generic;
//using UnityEngine;
using Unity.Physics;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Physics.Systems;
using Unity.Physics.Extensions;

namespace CharacterControllerECS.Utils
{
    public static class RaycastUtils
    {
        public enum CollisionLayer
        {
            Terrain = 1 << 0,
            Player = 1 << 1,
            //Item = 1 << 2,
            //ItemTrigger = 1 << 3
        }

        #region RAYCAST ECS

        //==========================================================================================================================
        /// <summary>
        /// ECS RAYCAST BASIC Construction
        /// </summary>
        /// <param name="fromPosition"></param>
        /// <param name="toPosition"></param>
        /// <returns></returns>
        public static Entity Raycast(float3 fromPosition, float3 toPosition, uint collisionFilter)
        {
            BuildPhysicsWorld buildPhysicsWorld =
                World.DefaultGameObjectInjectionWorld
                    .GetExistingSystem<BuildPhysicsWorld>(); //Seems to connect physics to the current world
            CollisionWorld
                collisionWorld =
                    buildPhysicsWorld.PhysicsWorld
                        .CollisionWorld; //Seems to connect methods uses for collision to the physics world we created

            RaycastInput raycastInput = new RaycastInput
            {
                Start = fromPosition,
                End = toPosition,
                //Layer filter
                Filter = new CollisionFilter
                {
                    BelongsTo = ~0u, //belongs to all layers
                    CollidesWith = collisionFilter, //collides with all layers
                    GroupIndex = 0,
                }
            };
            //throw a raycast
            RaycastHit raycastHit = new RaycastHit();
            if (collisionWorld.CastRay(raycastInput, out raycastHit))
            {
                //Return the entity hit
                Entity hitEntity = buildPhysicsWorld.PhysicsWorld.Bodies[raycastHit.RigidBodyIndex].Entity;
                return hitEntity;
            }
            else
            {
                return Entity.Null;
            }

        }

        //==========================================================================================================================

        #endregion RAYCAST ECS


        // the following needs to be in a systenBase or a jobComponentSytem to work
        [BurstCompile]
        public struct RaycastJob : IJobParallelFor
        {
            [ReadOnly] public CollisionWorld Colworld;
            [ReadOnly] public NativeArray<RaycastInput> inputs;
            public NativeArray<RaycastHit> results;

            public void Execute(int index)
            {
                RaycastHit hit;
                Colworld.CastRay(inputs[index], out hit);
                results[index] = hit;
            }
        }

        public static JobHandle ScheduleBatchRayCast(CollisionWorld world, NativeArray<RaycastInput> inputs,
            NativeArray<RaycastHit> results)
        {
            JobHandle rcj = new RaycastJob
            {
                inputs = inputs,
                results = results,
                Colworld = world

            }.Schedule(inputs.Length, 4, default);
            return rcj;
        }

        public static void SingleRayCast(CollisionWorld world, RaycastInput input, ref RaycastHit result)
        {
            NativeArray<RaycastInput> rayCommands = new NativeArray<RaycastInput>(1, Allocator.Temp);
            NativeArray<RaycastHit> rayResults = new NativeArray<RaycastHit>(1, Allocator.Temp);
            rayCommands[0] = input;
            JobHandle handle = ScheduleBatchRayCast(world, rayCommands, rayResults);
            handle.Complete();
            result = rayResults[0];
            rayCommands.Dispose();
            rayResults.Dispose();
        }

        /// <summary>
        /// When determining if two colliders should collide or a query should be performed, Unity Physics checks the
        /// BelongsTo bits of one against the CollidesWith bits of the other. Both objects must want to collide with each
        /// other for the collision to happen.
        /// </summary>
        /// <param name="filterA"></param>
        /// <param name="filterB"></param>
        /// <returns></returns>
        public static bool IsCollisionEnabled(CollisionFilter filterA, CollisionFilter filterB)
        {
            if (filterA.GroupIndex > 0 && filterA.GroupIndex == filterB.GroupIndex)
            {
                return true;
            }

            if (filterA.GroupIndex < 0 && filterA.GroupIndex == filterB.GroupIndex)
            {
                return false;
            }

            return
                (filterA.BelongsTo & filterB.CollidesWith) != 0 &&
                (filterB.BelongsTo & filterA.CollidesWith) != 0;
        }
    }


    #region TIPS

// HOW TO DEBUG RAYCASTINPUT HIT

/*
    var index = buildPhysicsWorld.PhysicsWorld.GetRigidBodyIndex(hitEntity);
    var filtere = buildPhysicsWorld.PhysicsWorld.GetCollisionFilter(buildPhysicsWorld.PhysicsWorld.GetRigidBodyIndex(hitEntity));
    Debug.Log("COLLISION OK? " + IsCollisionEnabled(filtere, raycastInput.Filter));
    Debug.Log("FILTER HIT GRoupINDEX " + filtere.GroupIndex);
    Debug.Log("FILTER RAY GRoupINDEX " + raycastInput.Filter.GroupIndex);
    Debug.Log("---------------------------------------------------------");
    Debug.Log("FILTER HIT belong " + filtere.BelongsTo);
    Debug.Log("FILTER RAY belong " + raycastInput.Filter.BelongsTo);
    Debug.Log("---------------------------------------------------------");
    Debug.Log("FILTER HIT CollidesWith " + filtere.CollidesWith);
    Debug.Log("FILTER RAY CollidesWith " + raycastInput.Filter.CollidesWith);
*/

// HOW TO GET THE FILTRE OF A UNIT

/*
    BuildPhysicsWorld buildPhysicsWorld = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>(); //Seems to connect physics to the current world
    PhysicsWorld world = buildPhysicsWorld.PhysicsWorld;
    CollisionWorld collisionWorld = world.CollisionWorld;

    Debug.Log("FILTER MAY " + " " + buildPhysicsWorld.PhysicsWorld.GetRigidBodyIndex(_UnitHit) + " " + _UnitHit);
    Debug.Log("FILTER TRYGETVALUEBAY NAME Unit " + " " + (uint) LayerMask.NameToLayer("UnitCollision") + " " + _UnitHit);
    Debug.Log("FILTER TRYGETVALUEBAY NAME test " + " " + (uint) LayerMask.GetMask("test") + " " + _UnitHit);
    CollisionFilter filtere = world.GetCollisionFilter(buildPhysicsWorld.PhysicsWorld.GetRigidBodyIndex(_UnitHit));
    Debug.Log("FILTER WORKS BelongsTo " + " " + filtere.BelongsTo + " " + _UnitHit);
    Debug.Log("FILTER WORKS CollidesWith " + " " + filtere.CollidesWith + " " + _UnitHit);
    Debug.Log("FILTER WORKS GroupIndex " + " " + filtere.GroupIndex + " " + _UnitHit);
*/

    #endregion TIPS
}