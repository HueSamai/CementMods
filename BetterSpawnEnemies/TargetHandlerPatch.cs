using HarmonyLib;
using Il2CppFemur;
using MelonLoader;
using UnityEngine;


// this patch fixes a problem where beasts target the same gangID members because they share a list of targets
[HarmonyPatch(typeof(TargetingHandeler), nameof(TargetingHandeler.FindActors))]
public static class TargetHandlerPatch
{
    public static void Postfix(TargetingHandeler __instance)
    {
        Melon<BetterSpawnEnemies>.Logger.Msg("Postfix for targeting handler called!");

        if (!__instance.actor.IsAI || TargetingHandeler.actorsToTarget == null) return;
        
        __instance.currentTargetActor = null;
        float minDistance = float.PositiveInfinity;
        for (int k = 0; k < TargetingHandeler.actorsToTarget.Count; k++)
        {
            if (TargetingHandeler.actorsToTarget[k].gangID == __instance.actor.gangID)
                continue;

            Transform partTransform = TargetingHandeler.actorsToTarget[k].bodyHandeler.Head.PartTransform;
            if (partTransform == null) continue;

            Vector3 position = partTransform.position;
            float currentDistance = Vector3.SqrMagnitude(position - __instance.cUpper.bounds.center);
            if (currentDistance <= minDistance)
            {
                __instance.currentTargetActor = TargetingHandeler.actorsToTarget[k];
                minDistance = currentDistance;
            }
        }

        __instance.actorIntrest = __instance.currentTargetActor.bodyHandeler.Chest.PartTransform;
    }
}
