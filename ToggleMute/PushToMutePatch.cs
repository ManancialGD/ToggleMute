using BepInEx.Logging;
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

        private static GameObject muteIcon;
        private static GameObject cutLineIcon;

        private static Image muteIconImage;
        private static Image cutLineImage;

        private static Color muteIconColor;
        private static Color cutLineColor;

        private static AssetBundle muteIconBundle;

        private static AudioSource audioSource;

        private static AudioClip muteSound;
        private static AudioClip unmuteSound;

        public static PlayerVoiceChat instance;

        public static Coroutine AnimateIconCoroutine;

        public static bool isMuted = false;
        private static float t = 0;

        public static void UpdateUI(bool Animation)
        {
            if (muteIcon == null)
            {
                InitMuteIcon();
            }

            isMuted = Animation;
            AnimateIconCoroutine = PushToMuteMod.Instance.StartCoroutine(AnimateIcon());
        }

        [HarmonyPostfix]
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
                audioSource.clip = isMuted ? muteSound : unmuteSound;
                audioSource.Play();

                recorder.TransmitEnabled = !isMuted;
                recorder.RecordingEnabled = !isMuted;
            }

            bool isAnimating = AnimateIconCoroutine != null;
            bool needAnimation = !((t == 1 && isMuted) || (t == 0 && !isMuted));

            if (!isAnimating && needAnimation)
                AnimateIconCoroutine = __instance.StartCoroutine(AnimateIcon());
        }


        private static void InitMuteIcon()
        {
            //Debug.Log("hello im here to init the mute icon :3");

            GameObject hudCanvas = PushToMuteMod.GetHudCanvas();
            if (hudCanvas == null) return;

            // Setup Container.
            GameObject muteIconContainer = new GameObject("MuteIconContainer");

            RectTransform muteIconContainerTransofrm = muteIconContainer.AddComponent<RectTransform>();
            muteIconContainerTransofrm.SetParent(hudCanvas.transform, false);

            Vector2 iconSize = new Vector2(40, 40);

            // Anchor on Bottom-Right
            muteIconContainerTransofrm.anchorMin = new Vector2(1, 0);
            muteIconContainerTransofrm.anchorMax = new Vector2(1, 0);

            muteIconContainerTransofrm.pivot = new Vector2(1, 0); // Pivot on Bottom Right

            muteIconContainerTransofrm.anchoredPosition = new Vector2(-10, 10);

            muteIconContainerTransofrm.sizeDelta = iconSize;

            // Setup MuteIcon.
            muteIcon = new GameObject("MuteIcon");

            RectTransform rectTransform = muteIcon.AddComponent<RectTransform>();
            rectTransform.SetParent(muteIconContainerTransofrm);

            rectTransform.pivot = new Vector2(.5f, .5f); // Pivot on center

            rectTransform.anchoredPosition = Vector3.zero;
            rectTransform.sizeDelta = iconSize;

            // Setup CutLine.
            cutLineIcon = new GameObject("CutLine");
            RectTransform lineTransform = cutLineIcon.AddComponent<RectTransform>();

            // Set parent to the microphone's RectTransform
            lineTransform.SetParent(muteIconContainerTransofrm, false); // rectTransform is the muteIcon's RectTransform

            // Anchor on bottom-left
            lineTransform.anchorMin = Vector2.zero;
            lineTransform.anchorMax = Vector2.zero;

            lineTransform.pivot = Vector2.zero; // Pivot on bottom-left

            lineTransform.anchoredPosition = Vector3.zero;

            // Set sizeDelta to match microphone's size
            lineTransform.sizeDelta = iconSize; // Same as the microphone's sizeDelta

            // Add the images
            muteIconImage = muteIcon.AddComponent<Image>();
            cutLineImage = cutLineIcon.AddComponent<Image>();

            string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (muteIconBundle == null) muteIconBundle = AssetBundle.LoadFromFile(Path.Combine(sAssemblyLocation, "togglemutebundle"));

            if (muteIconBundle != null)
            {
                Sprite muteSprite = muteIconBundle.LoadAsset<Sprite>("assets/unmuted.png");
                Sprite cutLineSprite = muteIconBundle.LoadAsset<Sprite>("assets/cutLine.png");

                muteSound = muteIconBundle.LoadAsset<AudioClip>("assets/on.ogg");
                unmuteSound = muteIconBundle.LoadAsset<AudioClip>("assets/off.ogg");

                if (muteSprite != null && cutLineSprite != null)
                {
                    muteIconImage.sprite = muteSprite;
                    cutLineImage.sprite = cutLineSprite;
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
            cutLineColor = cutLineImage.color;

            if (audioSource == null)
                audioSource = muteIcon.AddComponent<AudioSource>();

            audioSource.volume = PushToMuteMod.SoundVolume.Value;
        }

        private static IEnumerator AnimateIcon()
        {
            while (true)
            {
                // Null verifications
                if (muteIcon == null || muteIconImage == null)
                {
                    AnimateIconCoroutine = null;
                    yield break;
                }

                // Check if the animation is over.
                if (t == 1 && isMuted)
                {
                    muteIcon.transform.localScale = Vector3.one;
                    muteIconColor.a = 1;
                    muteIconImage.color = muteIconColor;

                    cutLineIcon.transform.localScale = Vector3.one;
                    cutLineColor.a = 1;
                    cutLineImage.color = cutLineColor;

                    AnimateIconCoroutine = null;

                    yield break;
                }
                else if (t == 0 && !isMuted)
                {
                    muteIcon.transform.localScale = Vector3.zero;
                    muteIconColor.a = 0;
                    muteIconImage.color = muteIconColor;

                    cutLineIcon.transform.localScale = Vector3.zero;
                    cutLineColor.a = 0;
                    cutLineImage.color = cutLineColor;

                    AnimateIconCoroutine = null;
                    yield break;
                }

                // Devide by 0 is a nono.
                if (PushToMuteMod.AnimationTime.Value == 0)
                    t = isMuted ? 1f : 0f;
                else
                {
                    float animationTimeInSeconds = PushToMuteMod.AnimationTime.Value * 0.001f; // Convert milliseconds to seconds
                    t = Mathf.MoveTowards(t, isMuted ? 1f : 0f, Time.deltaTime / animationTimeInSeconds);
                }

                float easeT = EaseInOut(t);

                // Scale animation
                muteIcon.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, easeT);
                cutLineIcon.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, easeT);

                // Alpha animation
                muteIconColor = muteIconImage.color;
                muteIconColor.a = Mathf.Lerp(0f, 1f, easeT);

                cutLineColor = cutLineImage.color;
                cutLineColor.a = Mathf.Lerp(0f, 1f, easeT);

                // Setting the color back
                cutLineImage.color = cutLineColor;
                muteIconImage.color = muteIconColor;

                yield return null;
            }
        }

        /// <summary>
        /// Applies an ease-in-out interpolation to the given input value.
        /// </summary>
        /// <param name="t">The interpolation factor, typically in the range [0, 1].</param>
        /// <returns>The eased value.</returns>
        private static float EaseInOut(float t)
        {
            return t * t * (3f - 2f * t);
        }

    }
}
