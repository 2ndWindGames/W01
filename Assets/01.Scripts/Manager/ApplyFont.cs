using TMPro;
using UnityEngine;

namespace SWGUnity2DCore.Manager
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ApplyFont : MonoBehaviour
    {
        [SerializeField] private GameFontRole fontCombination = GameFontRole.Body;

        public GameFontRole FontCombination => fontCombination;

        private void OnEnable() => Refresh();

        public void Refresh()
        {
            if (TryGetComponent(out TMP_Text text))
                GameLocalization.ApplyFont(text);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += RefreshIfPresent;
        }

        private void RefreshIfPresent()
        {
            if (this != null && isActiveAndEnabled) Refresh();
        }
#endif
    }
}
