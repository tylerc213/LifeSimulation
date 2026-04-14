// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Editor Settings Utility
// Requirement:	Leaderboard HTTP
// Author:		Benjamin Jones
// Date:		04/14/2026
// Version:		0.0.0
//
// Description:
//    Ensures Unity Player setting "Allow downloads over HTTP" stays enabled
//    for Nakama HTTP integration during development.
// -----------------------------------------------------------------------------

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary> Forces Allow downloads over HTTP to Always Allowed. </summary>
public static class EnforceHttpDownloadsSetting
{
    [InitializeOnLoadMethod]
    private static void ApplyOnEditorLoad()
    {
        ApplySetting();
    }

    [MenuItem("Tools/Leaderboard/Enable HTTP Downloads")]
    private static void ApplyFromMenu()
    {
        ApplySetting();
        Debug.Log("Player setting updated: Allow downloads over HTTP = Always Allowed.");
    }

    private static void ApplySetting()
    {
        if (PlayerSettings.insecureHttpOption != InsecureHttpOption.AlwaysAllowed)
        {
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
