using System;
using CombatOverhaul.Colliders;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace COGetTransformMatrixFix;

public class COGetTransformMatrixFixModSystem : ModSystem
{
    private static ICoreClientAPI _clientApi;

    public override void StartClientSide(ICoreClientAPI api)
    {
        _clientApi = api;
        var harmony = new Harmony(Mod.Info.ModID);

        var original =
            AccessTools.Method(typeof(ShapeElementCollider), "GetTransformMatrix",
                new[] { typeof(int), typeof(float[]) });
        var patch = AccessTools.Method(typeof(COGetTransformMatrixFixModSystem), nameof(ReplacementPatch));

        harmony.Patch(original, prefix: new HarmonyMethod(patch));

        // var original =
        //     AccessTools.Method(typeof(ShapeElementCollider), "GetTransformMatrix",
        //         new[] { typeof(int), typeof(float[]) });
        // var patch = AccessTools.Method(typeof(COGetTransformMatrixFixModSystem), nameof(Patch));
        //
        // harmony.Patch(original, finalizer: new HarmonyMethod(patch));
    }

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(ShapeElementCollider), "GetTransformMatrix")]
    public static Exception Patch(Exception __exception, int jointId, float[] TransformationMatrices4x4)
    {
        if (__exception == null)
        {
            return null;
        }

        try
        {
            _clientApi.SendChatMessage("Exception while getting transform matrix: " + __exception.Message,
                GlobalConstants.InfoLogChatGroup);
        }
        catch (Exception e)
        {
            // ignored
        }


        return null;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShapeElementCollider), "GetTransformMatrix")]
    public static bool ReplacementPatch(ShapeElementCollider __instance, ref double[] __result, int jointId,
        float[] TransformationMatrices4x4)
    {
        var transformMatrix = new double[16];
        Mat4d.Identity(transformMatrix);
        for (var elementIndex = 0; elementIndex < 16; elementIndex++)
        {
            var transformMatricesIndex = GetIndex(jointId, elementIndex);
            if (transformMatricesIndex != null)
            {
                if (transformMatricesIndex.Value < 0 ||
                    transformMatricesIndex.Value >= TransformationMatrices4x4.Length)
                {
                    __result = transformMatrix;
                    return false;
                }

                transformMatrix[elementIndex] = TransformationMatrices4x4[transformMatricesIndex.Value];
            }
        }

        __result = transformMatrix;
        return false;
    }

    private static int? GetIndex(int jointId, int matrixElementIndex)
    {
        var index = 16 * jointId;
        if (matrixElementIndex < 0) return null;

        return index + matrixElementIndex;
    }
}