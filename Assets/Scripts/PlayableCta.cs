using UnityEngine;

// Appels au SDK Luna. L'URL du store et l'appel MRAID par réseau sont gérés par Luna
// (configurés dans le Playground), pas dans le code.
// Les noms d'événements restent des littéraux : l'analyse du build Luna les compte dans le code.
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

    public static void FirstMove()
    {
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        Luna.Unity.Analytics.LogEvent(Luna.Unity.Analytics.EventType.TutorialComplete);
        Luna.Unity.Analytics.LogEvent("first_move", 0);
#endif
        Log("first_move", 0);
    }

    public static void InvalidTap(int moves)
    {
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        Luna.Unity.Analytics.LogEvent("invalid_tap", moves);
#endif
        Log("invalid_tap", moves);
    }

    public static void MultiTileSlide(int tiles)
    {
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        Luna.Unity.Analytics.LogEvent("multi_tile_slide", tiles);
#endif
        Log("multi_tile_slide", tiles);
    }

    public static void MovesReached(int moves)
    {
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        if (moves == 10) Luna.Unity.Analytics.LogEvent("moves_10", moves);
        else if (moves == 20) Luna.Unity.Analytics.LogEvent("moves_20", moves);
        else if (moves == 50) Luna.Unity.Analytics.LogEvent("moves_50", moves);
#endif
        Log("moves_" + moves, moves);
    }

    // row : 1 = première ligne. Le nombre de coups au moment où la ligne est terminée.
    public static void RowComplete(int row, int moves)
    {
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        if (row == 1) Luna.Unity.Analytics.LogEvent("row_1_complete", moves);
        else if (row == 2) Luna.Unity.Analytics.LogEvent("row_2_complete", moves);
        else if (row == 3) Luna.Unity.Analytics.LogEvent("row_3_complete", moves);
#endif
        Log("row_" + row + "_complete", moves);
    }

    public static void LevelWon(int moves)
    {
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        Luna.Unity.Analytics.LogEvent(Luna.Unity.Analytics.EventType.LevelWon);
        Luna.Unity.Analytics.LogEvent("puzzle_solved_moves", moves);
        Luna.Unity.Analytics.LogEvent(Luna.Unity.Analytics.EventType.EndCardShown);
        Luna.Unity.LifeCycle.GameEnded();
#endif
        Log("puzzle_solved_moves", moves);
    }

    public static void LevelRetry(int replays)
    {
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        Luna.Unity.Analytics.LogEvent(Luna.Unity.Analytics.EventType.LevelRetry);
        Luna.Unity.Analytics.LogEvent("replay", replays);
#endif
        Log("replay", replays);
    }

    public static void CtaInGame(int moves)
    {
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        Luna.Unity.Analytics.LogEvent("cta_click_ingame", moves);
#endif
        Log("cta_click_ingame", moves);
        OpenStore();
    }

    public static void CtaEndCard(int moves)
    {
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
        Luna.Unity.Analytics.LogEvent("cta_click_endcard", moves);
#endif
        Log("cta_click_endcard", moves);
        OpenStore();
    }

    static void Log(string name, int value)
    {
#if UNITY_EDITOR
        Debug.Log("[Playable] " + name + " = " + value);
#endif
    }
}
