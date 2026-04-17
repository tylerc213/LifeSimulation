// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Button / strip styling
// Requirement:	Simulation user interface
// Author:		Benjamin Jones
// Date:		04/14/2026
// Version:		0.0.0
//
// Description:
//    Maps the shared theme onto runtime-built buttons so toolbar and modal
//    controls read as one product rather than one-off styling at each call site.
// -----------------------------------------------------------------------------

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary> Applies <see cref="LifeSimUITheme"/> to strip-style uGUI buttons. </summary>
public static class LifeSimUIButtonStyle
{
    /// <summary> Toolbar look on a root with Image + Button + child TMP. </summary>
    public static void ApplyStripButton(GameObject root, LifeSimUITheme theme, bool primaryStripAction)
    {
        if (root == null || theme == null)
            return;

        Color background = primaryStripAction ? theme.toolbarPrimaryBackground : theme.toolbarButtonBackground;
        ApplyStripVisuals(root, theme, background, DefaultInteractionColors(), theme.toolbarButtonLabel);
    }

    /// <summary> Danger / reset strip styling. </summary>
    public static void ApplyStripDangerButton(GameObject root, LifeSimUITheme theme)
    {
        if (root == null || theme == null)
            return;

        ApplyStripVisuals(root, theme, theme.toolbarDangerBackground, DangerInteractionColors(), theme.toolbarDangerLabel);
    }

    static void ApplyStripVisuals(GameObject root, LifeSimUITheme theme, Color background, InteractionColors interaction,
        Color labelColor)
    {
        Image img = root.GetComponent<Image>();
        if (img != null)
        {
            img.color = background;
            img.type = Image.Type.Sliced;
        }

        Button button = root.GetComponent<Button>();
        if (button != null)
        {
            ColorBlock cb = button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = interaction.Highlighted;
            cb.pressedColor = interaction.Pressed;
            cb.selectedColor = interaction.Selected;
            cb.disabledColor = interaction.Disabled;
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
            tmp.color = labelColor;
        }
    }

    static InteractionColors DefaultInteractionColors()
    {
        return new InteractionColors(
            new Color(0.92f, 0.92f, 0.92f, 1f),
            new Color(0.82f, 0.82f, 0.82f, 1f),
            Color.white,
            new Color(0.78f, 0.78f, 0.78f, 0.5f));
    }

    static InteractionColors DangerInteractionColors()
    {
        return new InteractionColors(
            new Color(0.95f, 0.88f, 0.88f, 1f),
            new Color(0.85f, 0.75f, 0.75f, 1f),
            Color.white,
            new Color(0.78f, 0.78f, 0.78f, 0.5f));
    }

    readonly struct InteractionColors
    {
        public readonly Color Highlighted;
        public readonly Color Pressed;
        public readonly Color Selected;
        public readonly Color Disabled;

        public InteractionColors(Color highlighted, Color pressed, Color selected, Color disabled)
        {
            Highlighted = highlighted;
            Pressed = pressed;
            Selected = selected;
            Disabled = disabled;
        }
    }
}
