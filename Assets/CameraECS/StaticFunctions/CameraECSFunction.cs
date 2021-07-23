using Unity.Burst;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;

namespace CameraECS
{
    /// <summary>
    /// Disclaimer
    /// made by eizenhorn
    /// from Unity forum
    /// </summary>
    public static class CameraEcsMethods
    {
        /// <summary>
        /// Convert point from world space to screen space
        /// </summary>
        /// <param name="point">Point in World Space</param>
        /// <param name="cameraPos">Camera position in World Space</param>
        /// <param name="camProjMatrix">Camera.projectionMatrix</param>
        /// <param name="camUp">Camera.transform.up</param>
        /// <param name="camRight">Camera.transform.right</param>
        /// <param name="camForward">Camera.transform.forward</param>
        /// <param name="pixelWidth">Camera.pixelWidth</param>
        /// <param name="pixelHeight">Camera.pixelHeight</param>
        /// <param name="scaleFactor">Canvas.scaleFactor</param>
        /// <returns></returns>
        public static float2 ConvertWorldToScreenCoordinates(float3 point, float3 cameraPos, float4x4 camProjMatrix, float3 camUp, float3 camRight, float3 camForward, float pixelWidth, float pixelHeight, float scaleFactor)
        {
            /*
            * 1 convert P_world to P_camera
            */
            float4 pointInCameraCoodinates = ConvertWorldToCameraCoordinates(point, cameraPos, camUp, camRight, camForward);


            /*
            * 2 convert P_camera to P_clipped
            */
            float4 pointInClipCoordinates = math.mul(camProjMatrix, pointInCameraCoodinates);

            /*
            * 3 convert P_clipped to P_ndc
            * Normalized Device Coordinates
            */
            float4 pointInNdc = pointInClipCoordinates / pointInClipCoordinates.w;


            /*
            * 4 convert P_ndc to P_screen
            */
            float2 pointInScreenCoordinates;
            pointInScreenCoordinates.x = pixelWidth / 2.0f * (pointInNdc.x + 1);
            pointInScreenCoordinates.y = pixelHeight / 2.0f * (pointInNdc.y + 1);


            // return screencoordinates with canvas scale factor (if canvas coords required)
            return pointInScreenCoordinates / scaleFactor;
        }

        private static float4 ConvertWorldToCameraCoordinates(float3 point, float3 cameraPos, float3 camUp, float3 camRight, float3 camForward)
        {
            // translate the point by the negative camera-offset
            //and convert to Vector4
            float4 translatedPoint = new float4(point - cameraPos, 1f);

            // create transformation matrix
            float4x4 transformationMatrix = float4x4.identity;
            transformationMatrix.c0 = new float4(camRight.x, camUp.x, -camForward.x, 0);
            transformationMatrix.c1 = new float4(camRight.y, camUp.y, -camForward.y, 0);
            transformationMatrix.c2 = new float4(camRight.z, camUp.z, -camForward.z, 0);

            float4 transformedPoint = math.mul(transformationMatrix, translatedPoint);

            return transformedPoint;
        }
    }

}
