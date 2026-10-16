using UnityEngine;

// Appels au SDK Luna. L'URL du store et l'appel MRAID par réseau sont gérés par Luna
// (configurés dans le Playground), pas dans le code.
public static class PlayableCta
{
    public static void OpenStore()
    {
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        Luna.Unity.Playable.InstallFullGame();
#else
        Debug.Log("[Playable] InstallFullGame");
#endif
    }

    public static void GameEnd()
    {
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        Luna.Unity.LifeCycle.GameEnded();
#else
        Debug.Log("[Playable] GameEnded");
#endif
    }
}
