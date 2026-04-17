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
