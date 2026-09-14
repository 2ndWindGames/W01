using System.IO;
using System.Linq;
using _01.Scripts.UI;
using SWGUnity2DCore.Manager;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Explicit, one-shot authoring. Saved prefab controls remain editable in the Inspector.</summary>
[InitializeOnLoad]
public static class VioletTapComboAuthoring
{
    private const string Request = "output/qa-combo-6h/BUILD_COMBO_UI";
    static VioletTapComboAuthoring() => EditorApplication.update += () =>
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || !File.Exists(Request)) return;
        File.Delete(Request);
        Build();
    };

    [MenuItem("Tools/VioletTap/Author Combo Feedback UI")]
    public static void Build()
    {
        const string path = "Assets/Resources/Prefabs/UI/Popup/UI_GamePopup.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (prefab.transform.Find("ComboStatus") != null)
            {
                var existing = prefab.GetComponentInChildren<ComboStatusView>(true);
                existing.panel.sprite = null;
                existing.panel.type = UnityEngine.UI.Image.Type.Simple;
                var border = existing.panel.GetComponent<Outline>();
                if (border == null) border = existing.panel.gameObject.AddComponent<Outline>();
                border.effectColor = new Color(.2f, .85f, 1f, .65f);
                border.effectDistance = new Vector2(2, -2);
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                return;
            }
            var root = Rect("ComboStatus", prefab.transform, Vector2.zero, Vector2.zero);
            Stretch(root);
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 600;
            var view = root.gameObject.AddComponent<ComboStatusView>();
            view.card = Rect("ComboCard", root, new Vector2(860, 130), new Vector2(0, -352));
            view.card.anchorMin = view.card.anchorMax = new Vector2(.5f, 1f);
            view.panel = Image(view.card, new Color(.025f, .12f, .16f, .98f));
            var outline = view.panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.2f, .85f, 1f, .65f);
            outline.effectDistance = new Vector2(2, -2);
            view.comboLabel = Label("ComboCount", view.card, "05  COMBO", new Vector2(490, 62), new Vector2(-135, 25), 44);
            view.comboLabel.alignment = TextAlignmentOptions.MidlineLeft;
            view.multiplierLabel = Label("Multiplier", view.card, "x2 SCORE", new Vector2(240, 62), new Vector2(275, 25), 33);
            view.multiplierLabel.alignment = TextAlignmentOptions.MidlineRight;
            view.progressLabel = Label("ProgressLabel", view.card, "FEVER IN 5 TAPS   •   SPEED 1", new Vector2(780, 40), new Vector2(0, -25), 23);
            var track = Rect("ProgressTrack", view.card, new Vector2(780, 8), new Vector2(0, -53));
            Image(track, new Color(.1f, .16f, .23f));
            var fill = Rect("ProgressFill", track, Vector2.zero, Vector2.zero);
            Stretch(fill);
            view.progressFill = Image(fill, new Color(.2f, .9f, 1f));
            view.progressFill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            view.progressFill.type = UnityEngine.UI.Image.Type.Filled;
            view.progressFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            view.progressFill.fillAmount = .5f;
            view.failureRoot = Rect("FailureFeedback", root, Vector2.zero, Vector2.zero);
            Stretch(view.failureRoot);
            view.failureGroup = view.failureRoot.gameObject.AddComponent<CanvasGroup>();
            view.failureGroup.blocksRaycasts = false;
            view.failureGroup.interactable = false;
            view.failureLabel = Label("FailureMessage", view.failureRoot, "COMBO LOST!", new Vector2(800, 95), new Vector2(0, -490), 45);
            view.failureLabel.rectTransform.anchorMin = view.failureLabel.rectTransform.anchorMax = new Vector2(.5f, 1f);
            view.failureLabel.color = new Color(1f, .35f, .42f);
            view.failureEdges = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var edge = Rect("FailureEdge" + i, view.failureRoot, Vector2.zero, Vector2.zero);
                bool vertical = i < 2;
                edge.anchorMin = vertical ? new Vector2(i, 0) : new Vector2(0, i - 2);
                edge.anchorMax = vertical ? new Vector2(i, 1) : new Vector2(1, i - 2);
                edge.pivot = vertical ? new Vector2(i, .5f) : new Vector2(.5f, i - 2);
                edge.sizeDelta = vertical ? new Vector2(15, 0) : new Vector2(0, 15);
                view.failureEdges[i] = Image(edge, new Color(1, .1f, .2f, .5f));
            }
            view.failureRoot.gameObject.SetActive(false);
            var status = prefab.GetComponentsInChildren<TextMeshProUGUI>(true).Single(t => t.name == "txtStatus");
            Vector2 statusCenter = status.rectTransform.anchoredPosition + status.rectTransform.rect.center;
            status.rectTransform.sizeDelta = new Vector2(status.rectTransform.sizeDelta.x, 64);
            // The top pivot would shift the text upward when its box gets shorter.
            status.rectTransform.anchoredPosition = statusCenter - status.rectTransform.rect.center;
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            File.WriteAllText("output/qa-combo-6h/authoring.txt", "ComboStatus saved in UI_GamePopup.prefab.\n");
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
    private static Image Image(RectTransform rect, Color color)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color; image.raycastTarget = false;
        return image;
    }
    private static TextMeshProUGUI Label(string name, Transform parent, string text, Vector2 size, Vector2 position, float fontSize)
    {
        var label = Rect(name, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
        GameLocalization.ApplyFont(label);
        label.text = text;
        label.fontSize = label.fontSizeMax = fontSize;
        label.fontSizeMin = 18;
        label.enableAutoSizing = true;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }
}
