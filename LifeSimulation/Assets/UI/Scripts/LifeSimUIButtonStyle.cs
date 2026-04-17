// -----------------------------------------------------------------------------
// Applies LifeSimUITheme visuals to strip buttons and shared Button+Image+TMP.
// -----------------------------------------------------------------------------

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class LifeSimUIButtonStyle
{
    /// <summary>Apply toolbar look to a root with Image + Button + child TMP.</summary>
    public static void ApplyStripButton(GameObject root, LifeSimUITheme theme, bool primaryStripAction)
    {
        if (root == null || theme == null)
            return;

        Image img = root.GetComponent<Image>();
        if (img != null)
        {
            img.color = primaryStripAction ? theme.toolbarPrimaryBackground : theme.toolbarButtonBackground;
            img.type = Image.Type.Sliced;
        }

        Button button = root.GetComponent<Button>();
        if (button != null)
        {
            ColorBlock cb = button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.1f;
            button.colors = cb;
            button.transition = Selectable.Transition.ColorTint;
            if (img != null)
                button.targetGraphic = img;
        }

        TextMeshProUGUI tmp = root.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.fontSize = theme.toolbarButtonFontSize;
            tmp.color = theme.toolbarButtonLabel;
        }
    }

    public static void ApplyStripDangerButton(GameObject root, LifeSimUITheme theme)
    {
        if (root == null || theme == null)
            return;

        Image img = root.GetComponent<Image>();
        if (img != null)
        {
            img.color = theme.toolbarDangerBackground;
            img.type = Image.Type.Sliced;
        }

        Button button = root.GetComponent<Button>();
        if (button != null)
        {
            ColorBlock cb = button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.95f, 0.88f, 0.88f, 1f);
            cb.pressedColor = new Color(0.85f, 0.75f, 0.75f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.5f);
            button.colors = cb;
            if (img != null)
                button.targetGraphic = img;
        }

        TextMeshProUGUI tmp = root.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.fontSize = theme.toolbarButtonFontSize;
            tmp.color = theme.toolbarDangerLabel;
        }
    }
}
