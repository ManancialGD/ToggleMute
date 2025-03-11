using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace ToggleMute
{
    [HarmonyPatch(typeof(PlayerVoiceChat), "OnDestroy")]
    class PushToMuteCleanupPatch
    {
        static void Prefix(PlayerVoiceChat __instance)
        {
            PushToMutePatch.playerRecorders.Remove(__instance);
        }
    }
}
