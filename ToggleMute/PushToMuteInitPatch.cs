using HarmonyLib;
using Photon.Pun;
using Photon.Voice.Unity;
using System;
using System.Collections.Generic;
using System.Text;

namespace ToggleMute
{
    [HarmonyPatch(typeof(PlayerVoiceChat), "Awake")]
    class PushToMuteInitPatch
    {
        static void Postfix(PlayerVoiceChat __instance)
        {
            PhotonView photonView = __instance.GetComponent<PhotonView>();
            Recorder recorder = __instance.GetComponent<Recorder>();

            if (photonView != null && photonView.IsMine && recorder != null)
            {
                PushToMutePatch.playerRecorders[__instance] = recorder;
            }
        }
    }

}
