// -----------------------------------------------------------------------------
// Project:		EXTENDED LIFE SIMULATION CAPSTONE ASSIGNMENT
// Item:		Shared button font + rounded chrome per scene
// Requirement:	Main menu, configuration, simulation editor, score summary, leaderboard
// -----------------------------------------------------------------------------

using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Applies Roboto-Bold TMP to button labels and rounded UISprite backgrounds where configured.
/// </summary>
static class LifeSimUIStyleBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string n = scene.name;
        if (n != "MainMenu" && n != "Configuration" && n != "ScoreSummary" && n != "Leaderboard" &&
            n != "Simulation")
            return;

        TMP_FontAsset font = LifeSimUI.ButtonFont;
        Transform editorPanel = null;
        if (n == "Simulation")
            editorPanel = FindTransformInScene(scene, "EditorPanel");

        foreach (Button b in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b == null || b.gameObject.scene != scene)
                continue;

            if (font != null)
            {
                foreach (TMP_Text t in b.GetComponentsInChildren<TMP_Text>(true))
                    t.font = font;
            }

            bool rounded =
                n == "MainMenu" || n == "Configuration" || n == "ScoreSummary" ||
                (n == "Simulation" && editorPanel != null && b.transform.IsChildOf(editorPanel));

            if (!rounded || !b.TryGetComponent(out Image img))
                continue;

            Sprite s = LifeSimUI.BuiltinRoundedUISprite;
            if (s == null)
                continue;
            img.sprite = s;
            img.type = Image.Type.Sliced;
        }
    }

    static Transform FindTransformInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] ts = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in ts)
            {
                if (t.name == objectName)
                    return t;
            }
        }

        return null;
    }
}
