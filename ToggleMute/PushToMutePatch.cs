using HarmonyLib;
using Photon.Voice.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ToggleMute
{
    [HarmonyPatch(typeof(PlayerVoiceChat), "Update")]
    class PushToMutePatch
    {
        public static Dictionary<PlayerVoiceChat, Recorder> playerRecorders = new Dictionary<PlayerVoiceChat, Recorder>();
        public static bool isMuted = false;
        private static GameObject muteIcon;
        private static Image muteIconImage;
        private static Color muteIconColor;
        private static AssetBundle muteIconBundle;
        private static AudioSource audioSource;
        private static AudioClip muteSound;
        private static AudioClip unmuteSound;
        private static Sprite unmutedIcon;
        public static PlayerVoiceChat instance;

        public static void UpdateUI(bool Animation)
        {
            if (muteIcon == null)
            {
                InitMuteIcon();
            }

            isMuted = Animation;
            PushToMuteMod.Instance.StartCoroutine(AnimateMuteIcon(Animation, true));
        }

        static void Postfix(PlayerVoiceChat __instance)
        {
            instance = __instance;
            if (!playerRecorders.TryGetValue(__instance, out Recorder recorder)) return;

            KeyCode muteKey = PushToMuteMod.MuteKey.Value;

            if (muteIcon == null)
            {
                InitMuteIcon();
            }
            bool isChatActive = (bool)typeof(ChatManager)
                .GetField("chatActive", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(ChatManager.instance);


            if (isChatActive) return;

            if (Input.GetKeyDown(muteKey))
            {
                isMuted = !isMuted;

                recorder.TransmitEnabled = !isMuted;
                recorder.RecordingEnabled = !isMuted;

                __instance.StartCoroutine(AnimateMuteIcon(isMuted, false));
            }
        }


        private static void InitMuteIcon()
        {

            //Debug.Log("hello im here to init the mute icon :3");

            GameObject hudCanvas = PushToMuteMod.GetHudCanvas();
            if (hudCanvas == null) return;

            muteIcon = new GameObject("MuteIcon");
            muteIcon.transform.SetParent(hudCanvas.transform, false);

            RectTransform rectTransform = muteIcon.AddComponent<RectTransform>();
            rectTransform.gameObject.transform.position = new Vector3(680, 30, 0);
            rectTransform.sizeDelta = new Vector2(50, 50);

            muteIconImage = muteIcon.AddComponent<Image>();
            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (muteIconBundle == null) muteIconBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "togglemutebundle"));

            if (muteIconBundle != null)
            {
                Sprite muteSprite = muteIconBundle.LoadAsset<Sprite>("assets/muteicon.png");
                unmutedIcon = muteIconBundle.LoadAsset<Sprite>("assets/unmutedicon.png");
                muteSound = muteIconBundle.LoadAsset<AudioClip>("assets/on.ogg");
                unmuteSound = muteIconBundle.LoadAsset<AudioClip>("assets/off.ogg");

                if (muteSprite != null && unmutedIcon != null)
                {
                    muteIconImage.sprite = muteSprite;
                }
                else
                {
                    //Debug.LogError("sprites not found");
                }
            }
            else
            {
                //Debug.LogError("bundle failed to load");
            }

            muteIconColor = muteIconImage.color;
            muteIconColor.a = 0;
            muteIconImage.color = muteIconColor;

            muteIcon.SetActive(false);

            if (audioSource == null) audioSource = muteIcon.AddComponent<AudioSource>();
        }

        public static IEnumerator AnimateMuteIcon(bool show, bool force)
        {
            Sprite muteSprite = muteIconBundle.LoadAsset<Sprite>("assets/muteicon.png");
            if (muteIcon == null || muteIconImage == null || muteIconBundle == null)
            {
                Debug.LogError("Mute icon or bundle is not initialized");
                yield break;
            }

            muteIcon.SetActive(true);

            if (force)
            {
                float startAlpha = show ? 0 : 1;
                float endAlpha = show ? 1 : 0;
                Vector3 startScale = show ? Vector3.zero : Vector3.one;
                Vector3 endScale = show ? Vector3.one : Vector3.zero;

                muteIconColor.a = endAlpha;
                muteIconImage.color = muteIconColor;
                muteIcon.transform.localScale = endScale;

                muteIconImage.sprite = show ? muteSprite : unmutedIcon;
            }
            else
            {

                AudioClip sound = show ? muteSound : unmuteSound;
                if (sound != null)
                    audioSource.PlayOneShot(sound, PushToMuteMod.SoundVolume.Value);

                float duration = 0.3f;
                float elapsed = 0;
                float startAlpha = show ? 0 : 1;
                float endAlpha = show ? 1 : 0;
                Vector3 startScale = show ? Vector3.zero : Vector3.one;
                Vector3 endScale = show ? Vector3.one : Vector3.zero;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    t = Mathf.Sin(t * Mathf.PI * 0.5f);

                    muteIconColor.a = Mathf.Lerp(startAlpha, endAlpha, t);
                    muteIconImage.color = muteIconColor;
                    muteIcon.transform.localScale = Vector3.Lerp(startScale, endScale, t);

                    yield return null;
                }

                muteIconColor.a = endAlpha;
                muteIconImage.color = muteIconColor;
                muteIcon.transform.localScale = endScale;

                muteIconImage.sprite = show ? muteSprite : unmutedIcon;
            }
        }

    }
}
