using HarmonyLib;
using UnityEngine;

[HarmonyPatch(typeof(Player), "OnDamaged")]
public static class RemoveDamageFlashOnDeath
{
    [HarmonyPostfix]
    static bool Prefix(Player __instance, HitData hit)
    {
        if (hit.GetTotalDamage() >= __instance.GetHealth())
        {
            return false;
        }
        return true;
    }
}