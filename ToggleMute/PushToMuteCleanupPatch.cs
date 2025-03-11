using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ToggleMute
{
    [HarmonyPatch(typeof(PlayerVoiceChat), "OnDestroy")]
    class PushToMuteCleanupPatch
    {
        [HarmonyPrefix]
        static void Prefix(PlayerVoiceChat __instance)
        {
            // Log when the cleanup process starts
            if (PushToMutePatch.playerRecorders.ContainsKey(__instance))
            {
                PushToMutePatch.playerRecorders.Remove(__instance);
                PushToMutePatch.AnimateIconCoroutine = null;
            }
            else
            {
                Debug.Log($"PlayerVoiceChat OnDestroy: No recorder found for {__instance.GetComponent<PhotonView>().Owner.NickName}.");
            }
        }
    }
}
