using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Photon.Voice.Unity;
using Photon.Pun;
using BepInEx.Configuration;
using System.IO;
using System.Reflection;
using UnityEngine.SceneManagement;

[BepInPlugin("com.coddingcat.togglemute", "ToggleMute", "1.0.0")]
public class PushToMuteMod : BaseUnityPlugin
{
    public static ConfigEntry<KeyCode> MuteKey { get; private set; }
    public static ConfigEntry<float> SoundVolume { get; private set; }

    private Harmony harmony;
    private static GameObject hudCanvas;
    public AssetBundle muteIconBundle;
    public static PushToMuteMod Instance;



    private void Awake()
    {

        Instance = this;
        gameObject.hideFlags = HideFlags.HideAndDontSave;

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

        string sAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        harmony = new Harmony("com.coddingcat.pushtomute");
        harmony.PatchAll();
        Logger.LogInfo($"Push-to-Mute mod loaded! Key: {MuteKey.Value}");

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //Debug.Log("Scene loaded: " + scene.name);
        PushToMutePatch.UpdateUI(PushToMutePatch.isMuted);
    }

        void LogAssetNames(AssetBundle bundle)
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

/*
[HarmonyPatch(typeof(MenuPageSettingsControls), "Start")]
public class MenuPageSettingsControlPatch
{
    static void Postfix(MenuPageSettingsControls __instance)
    {
        Transform menuSelectionBox = __instance.transform.Find("Scroll Box/Mask/Scroller");
        if (menuSelectionBox == null)
        {
            Debug.LogError("Menu Selection Box");
            return;
        }

        Transform bigButton = menuSelectionBox.Find("Big Button Push To Talk");
        if (bigButton == null)
        {
            Debug.LogError("Big Button Push To Talk");
            return;
        }

        Debug.Log("Found Big Button");

        Transform clonedButton = GameObject.Instantiate(bigButton, bigButton.parent);
        clonedButton.name = "Cloned Big Button";

        RectTransform rectTransform = clonedButton.GetComponent<RectTransform>();

        TextMeshProUGUI tmpText = clonedButton.transform.Find("Element Name").GetComponent<TextMeshProUGUI>();

        tmpText.text = "Toggle Mute";
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition += new Vector2(0, -40);
            Debug.Log("Cloned button moved down successfully.");
        }
        else
        {
            Debug.LogError("RectTransform not found on cloned button!");
        }
    }

}
*/

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

[HarmonyPatch(typeof(PlayerVoiceChat), "OnDestroy")]
class PushToMuteCleanupPatch
{
    static void Prefix(PlayerVoiceChat __instance)
    {
        PushToMutePatch.playerRecorders.Remove(__instance);
    }
}

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
