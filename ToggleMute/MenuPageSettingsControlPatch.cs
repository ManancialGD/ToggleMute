using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;

namespace ToggleMute
{
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
}
