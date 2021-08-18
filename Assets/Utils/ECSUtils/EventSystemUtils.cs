using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace KaizerWaldCode.Utils
{
    public static class ECSUtils
    {
        /// <summary>
        /// Set requiered Event Tag for a system (1 all)
        /// May also Set entityManager if required
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="queryDesc"></param>
        /// <param name="em"></param>
        #region SINGLE EVENT ALL
        public static void SystemEventRequire<T1>(ref EntityQueryDesc queryDesc, ref EntityManager em) where T1 : struct, IComponentData
        {
            queryDesc = new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(T1) },
            };
            em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        public static void SystemEventRequire<T1>(ref EntityQueryDesc queryDesc) where T1 : struct, IComponentData
        {
            queryDesc = new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(T1) },
            };
        }
        #endregion SINGLE EVENT ALL
        /// <summary>
        /// Set requiered Event Tag for a system (1 all + 1 None)
        /// May also Set entityManager if required
        /// </summary>
        /// <typeparam name="TAll"></typeparam>
        /// <typeparam name="TNone"></typeparam>
        /// <param name="queryDesc"></param>
        /// <param name="em"></param>
        #region SINGLE EVENT ALL + NONE
        public static void SystemEventRequire<TAll, TNone>(ref EntityQueryDesc queryDesc, ref EntityManager em)
            where TAll : struct, IComponentData
            where TNone : struct, IComponentData
        {

            queryDesc = new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(TAll) },
                None = new ComponentType[] { typeof(TNone) },
            };
            em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        public static void SystemEventRequire<TAll, TNone>(ref EntityQueryDesc queryDesc)
            where TAll : struct, IComponentData
            where TNone : struct, IComponentData
        {

            queryDesc = new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(TAll) },
                None = new ComponentType[] { typeof(TNone) },
            };
        }
        #endregion SINGLE EVENT ALL + NONE

        #region SINGLE EVENT ALL DOUBLE NONE
        public static void SystemEventRequire<TAll, TNone1, TNone2>(ref EntityQueryDesc queryDesc, ref EntityManager em)
            where TAll : struct, IComponentData
            where TNone1 : struct, IComponentData
            where TNone2 : struct, IComponentData
        {

            queryDesc = new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(TAll) },
                None = new ComponentType[] { typeof(TNone1), typeof(TNone2) },
            };
            em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        public static void SystemEventRequire<TAll, TNone1, TNone2>(ref EntityQueryDesc queryDesc)
            where TAll : struct, IComponentData
            where TNone1 : struct, IComponentData
            where TNone2 : struct, IComponentData
        {

            queryDesc = new EntityQueryDesc()
            {
                All = new ComponentType[] { typeof(TAll) },
                None = new ComponentType[] { typeof(TNone1), typeof(TNone2) },
            };
        }
        #endregion SINGLE EVENT ALL DOUBLE NONE

        public static void EndEventSystem<TCurrentEvent, TNewEvent>(Entity eventHolder, EntityManager em)
            where TCurrentEvent : struct, IComponentData
            where TNewEvent : struct, IComponentData
        {
            em.RemoveComponent<TCurrentEvent>(eventHolder);
            em.AddComponent<TNewEvent>(eventHolder);
        }
    }
}
