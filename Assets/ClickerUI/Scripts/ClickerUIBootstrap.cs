using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecondWind.ClickerUI
{
    public sealed class ClickerUIBootstrap : MonoBehaviour
    {
        private const string LayoutPath = "ClickerUI/ClickerMain";

        private float currency = 1250;
        private int tapLevel = 1;
        private int idleLevel = 1;
        private float tapValue = 5;
        private float incomePerSecond = 12;

        private Label currencyLabel;
        private Label perTapLabel;
        private Label perSecondLabel;
        private Label tapLevelLabel;
        private Label idleLevelLabel;
        private Label tapCostLabel;
        private Label idleCostLabel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForScene()
        {
            if (FindAnyObjectByType<ClickerUIBootstrap>() != null)
                return;

            new GameObject("Clicker UI").AddComponent<ClickerUIBootstrap>();
        }

        private void Awake()
        {
            var layout = Resources.Load<VisualTreeAsset>(LayoutPath);
            if (layout == null)
            {
                Debug.LogError($"Clicker UI layout was not found at Resources/{LayoutPath}.");
                enabled = false;
                return;
            }

            var document = gameObject.AddComponent<UIDocument>();
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.name = "Clicker UI Runtime Panel";
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(540, 960);
            panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panelSettings.match = 0.5f;
            document.panelSettings = panelSettings;
            document.visualTreeAsset = layout;

            Bind(document.rootVisualElement);
            Refresh();
            StartCoroutine(GenerateIdleIncome());
        }

        private void Bind(VisualElement root)
        {
            currencyLabel = root.Q<Label>("currency-label");
            perTapLabel = root.Q<Label>("per-tap-label");
            perSecondLabel = root.Q<Label>("per-second-label");
            tapLevelLabel = root.Q<Label>("tap-level");
            idleLevelLabel = root.Q<Label>("idle-level");
            tapCostLabel = root.Q<Label>("tap-cost");
            idleCostLabel = root.Q<Label>("idle-cost");

            root.Q<Button>("click-target").clicked += () =>
            {
                currency += tapValue;
                Refresh();
            };
            root.Q<Button>("tap-upgrade").clicked += BuyTapUpgrade;
            root.Q<Button>("idle-upgrade").clicked += BuyIdleUpgrade;
        }

        private IEnumerator GenerateIdleIncome()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                currency += incomePerSecond;
                Refresh();
            }
        }

        private void BuyTapUpgrade()
        {
            var cost = TapUpgradeCost();
            if (currency < cost) return;
            currency -= cost;
            tapLevel++;
            tapValue += 3;
            Refresh();
        }

        private void BuyIdleUpgrade()
        {
            var cost = IdleUpgradeCost();
            if (currency < cost) return;
            currency -= cost;
            idleLevel++;
            incomePerSecond += 8;
            Refresh();
        }

        private int TapUpgradeCost() => 100 * tapLevel;
        private int IdleUpgradeCost() => 250 * idleLevel;

        private void Refresh()
        {
            currencyLabel.text = Mathf.FloorToInt(currency).ToString("N0");
            perTapLabel.text = $"+{tapValue:N0} each tap";
            perSecondLabel.text = $"+{incomePerSecond:N0} / sec";
            tapLevelLabel.text = $"Level {tapLevel} | +{tapValue:N0} per tap";
            idleLevelLabel.text = $"Level {idleLevel} | +{incomePerSecond:N0} per sec";
            tapCostLabel.text = $"{TapUpgradeCost():N0} G";
            idleCostLabel.text = $"{IdleUpgradeCost():N0} G";
        }
    }
}
