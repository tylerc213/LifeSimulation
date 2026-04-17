// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		UI theme access
// Requirement:	Simulation user interface
// Author:		Benjamin Jones
// Date:		04/14/2026
// Version:		0.0.0
//
// Description:
//    Resolves the one active theme for simulation UI so every screen pulls the
//    same palette and typography instead of hard-coding look-and-feel per view.
// -----------------------------------------------------------------------------

using TMPro;
using UnityEngine;

/// <summary> Active LifeSim UI theme (Resources asset or runtime defaults). </summary>
public static class LifeSimUI
{
    static LifeSimUITheme _theme;
    static TMP_FontAsset _buttonFont;
    static TMP_FontAsset _scoreSummaryCategoryFont;
    static Sprite _builtinUiSprite;
    static bool _builtinUiSpriteResolved;

    /// <summary> TMP asset from TextMesh Pro Examples Roboto-Bold (matches Roboto-Bold.ttf). </summary>
    public static TMP_FontAsset ButtonFont
    {
        get
        {
            if (_buttonFont == null)
                _buttonFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Roboto-Bold SDF");
            return _buttonFont;
        }
    }

    /// <summary> TMP asset from Electronic Highway Sign.TTF (Examples & Extras). Score summary title, category rows, name field text and placeholder. </summary>
    public static TMP_FontAsset ScoreSummaryCategoryFont
    {
        get
        {
            if (_scoreSummaryCategoryFont == null)
                _scoreSummaryCategoryFont =
                    Resources.Load<TMP_FontAsset>("Fonts & Materials/Electronic Highway Sign SDF");
            return _scoreSummaryCategoryFont;
        }
    }

    /// <summary> Sliced sprite for uGUI buttons (procedural 9-slice; avoids missing built-in UISprite on Unity 6+). </summary>
    public static Sprite BuiltinRoundedUISprite
    {
        get
        {
            if (_builtinUiSpriteResolved)
                return _builtinUiSprite;

            _builtinUiSpriteResolved = true;

            // Avoid GetBuiltinResource("UI/Skin/UISprite.psd"): missing on many Unity 6+ runtimes and logs an error.
            _builtinUiSprite = CreateProceduralSlicedWhiteSprite();
            return _builtinUiSprite;
        }
    }

    static Sprite CreateProceduralSlicedWhiteSprite()
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "LifeSimUI_ProceduralButtonSprite",
            hideFlags = HideFlags.DontSave
        };
        var white = new Color32(255, 255, 255, 255);
        var pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = white;
        tex.SetPixels32(pixels);
        tex.Apply();
        const float ppu = 100f;
        var border = new Vector4(10f, 10f, 10f, 10f);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect,
            border);
    }

    /// <summary> Active theme; never null. </summary>
    public static LifeSimUITheme Theme
    {
        get
        {
            if (_theme == null)
                _theme = LoadOrCreateTheme();
            return _theme;
        }
    }

    static LifeSimUITheme LoadOrCreateTheme()
    {
        LifeSimUITheme asset = Resources.Load<LifeSimUITheme>("LifeSimUITheme");
        if (asset != null)
            return asset;

        LifeSimUITheme runtime = ScriptableObject.CreateInstance<LifeSimUITheme>();
        runtime.name = "LifeSimUITheme_RuntimeDefaults";
        runtime.ApplyEmbeddedDefaults();
        return runtime;
    }
}
