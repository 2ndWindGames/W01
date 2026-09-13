using SWGUnity2DCore.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _01.Scripts.UI.Popup
{
    internal static class PopupPresentation
    {
        public static void Prepare(Transform root)
        {
            if (root.Find("ModalBackdrop") == null)
            {
                var backdrop = new GameObject("ModalBackdrop", typeof(RectTransform), typeof(Image));
                backdrop.layer = root.gameObject.layer;
                var rect = (RectTransform)backdrop.transform;
                rect.SetParent(root, false);
                rect.SetAsFirstSibling();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var image = backdrop.GetComponent<Image>();
                image.color = new Color(.01f, .02f, .06f, .76f);
                image.raycastTarget = true;
            }

            foreach (var label in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                label.raycastTarget = false;
                label.color = new Color(.87f, .95f, 1f);
                if (GameLocalization.IsKorean) label.fontStyle |= FontStyles.Bold;
                if (label.name != "txtTitle") continue;
                label.rectTransform.sizeDelta = new Vector2(760f, 85f);
                label.enableAutoSizing = true;
                label.fontSizeMin = 30f;
                label.fontSizeMax = 50f;
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.color = new Color(.48f, .91f, 1f);
            }
        }
    }
}
