// -----------------------------------------------------------------------------
// Resolves the active UI theme: Resources/LifeSimUITheme, else embedded defaults.
// -----------------------------------------------------------------------------

using UnityEngine;

public static class LifeSimUI
{
    static LifeSimUITheme _theme;

    /// <summary>Active theme; never null.</summary>
    public static LifeSimUITheme Theme
    {
        get
        {
            if (_theme == null)
            {
                _theme = Resources.Load<LifeSimUITheme>("LifeSimUITheme");
                if (_theme == null)
                {
                    _theme = ScriptableObject.CreateInstance<LifeSimUITheme>();
                    _theme.name = "LifeSimUITheme_RuntimeDefaults";
                    _theme.ApplyEmbeddedDefaults();
                }
            }

            return _theme;
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>For tests or domain reload.</summary>
    public static void ClearThemeCache()
    {
        _theme = null;
    }
#endif
}
