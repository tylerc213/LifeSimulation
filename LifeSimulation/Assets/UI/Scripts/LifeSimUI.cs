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

/// <summary> Active LifeSim UI theme (Resources asset or runtime defaults). </summary>
public static class LifeSimUI
{
    static LifeSimUITheme _theme;

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
