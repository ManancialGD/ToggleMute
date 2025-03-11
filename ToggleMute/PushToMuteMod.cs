using BepInEx.Configuration;
using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace ToggleMute
{
    [BepInPlugin("com.coddingcat.togglemute", "ToggleMute", "1.0.0")]
    public class PushToMuteMod : BaseUnityPlugin
    {
        public static ConfigEntry<KeyCode> MuteKey { get; private set; }
        public static ConfigEntry<float> SoundVolume { get; private set; }
        public static ConfigEntry<int> AnimationTime { get; private set; }
        private Harmony harmony;
        private static GameObject hudCanvas;
        public AssetBundle muteIconBundle;
        public static PushToMuteMod Instance;

        private void Awake()
        {
            Instance = this;
            gameObject.hideFlags = HideFlags.HideAndDontSave;

            BindCofigs();

            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            harmony = new Harmony("com.coddingcat.pushtomute");
            harmony.PatchAll();
            Logger.LogInfo($"Push-to-Mute mod loaded! Key: {MuteKey.Value}");

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void BindCofigs()
        {
            MuteKey = Config.Bind(
                "General",
                "MuteKey",
                KeyCode.M,
                "Key to toggle Push-to-Mute (Change in config file)"
            );

            SoundVolume = Config.Bind(
                "General",
                "SoundVolume",
                0.3f,
                "Volume of mute/unmute sound (0.0 - 1.0)"
            );

            AnimationTime = Config.Bind(
                "General",
                "Animation Duration",
                200,
                "Duration of the mute icon animation in milliseconds."
            );
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //Debug.Log("Scene loaded: " + scene.name);
            PushToMutePatch.UpdateUI(PushToMutePatch.isMuted);
        }

        private void LogAssetNames(AssetBundle bundle)
        {
            if (bundle == null) return;
            string[] assetNames = bundle.GetAllAssetNames();
            foreach (var name in assetNames)
            {
                Debug.Log($"Asset in bundle: {name}");
            }
        }

        public static GameObject GetHudCanvas()
        {
            if (hudCanvas == null)
            {
                hudCanvas = GameObject.Find("UI/HUD/HUD Canvas");
                if (hudCanvas == null)
                {
                    Debug.LogError("HUD Canvas not found");
                }
            }
            return hudCanvas;
        }
    }

}
