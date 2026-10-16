using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Taquin 4x4 dont l'interface est construite par code : seul ce composant est posé dans la scène.
public class PuzzleGame : MonoBehaviour
{
    const int Size = 4;
    const int Count = Size * Size;
    const float SlideTime = 0.09f;

    static readonly Color BgColor = new Color32(0x1E, 0x22, 0x30, 0xFF);
    static readonly Color BoardColor = new Color32(0x2C, 0x32, 0x46, 0xFF);
    static readonly Color TileTextColor = new Color32(0x2A, 0x1A, 0x05, 0xFF);

    // board[index] = valeur de la tuile (0 = case vide), index = ligne * Size + colonne
    readonly int[] board = new int[Count];
    readonly RectTransform[] tiles = new RectTransform[Count];

    // Réglables depuis le Luna Playground pour tester des variantes.
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
    [LunaPlaygroundField("Coups de mélange", 1, "Gameplay")]
#endif
    [SerializeField] int shuffleMoves = 40;
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
    [LunaPlaygroundField("Couleur des tuiles", 2, "Apparence")]
#endif
    [SerializeField] Color tileColor = new Color32(0xF2, 0xA6, 0x3B, 0xFF);
#if LUNA_IS_PRESENT || LUNA_EDITOR_SOURCES
    [LunaPlaygroundField("Couleur du bouton", 3, "Apparence")]
#endif
    [SerializeField] Color ctaColor = new Color32(0x3B, 0xC1, 0x6E, 0xFF);

    [SerializeField] Font font;
    CanvasScaler scaler;
    RectTransform canvasRect, safeRect, boardRect, header, cta;
    RectTransform endCard, endSafeRect, endTextRect, install, replay;
    Text headerText, endText;
    Vector2 lastCanvasSize;
    Rect lastSafeArea;
    int lastScreenW, lastScreenH;
    float cell;
    int moves, replays;
    // Événements Luna déjà envoyés pendant la partie en cours (une seule fois chacun).
    readonly bool[] rowLogged = new bool[Size];
    bool invalidTapLogged, multiSlideLogged;
    bool busy, won;

    void Awake()
    {
        Application.targetFrameRate = 60;
        if (font == null) font = (Font)Resources.GetBuiltinResource(typeof(Font), "LegacyRuntime.ttf");
        BuildUI();
        NewGame();
    }

    void LateUpdate()
    {
        if (canvasRect.rect.size != lastCanvasSize || Screen.width != lastScreenW || Screen.height != lastScreenH)
            Layout();
    }

    // ---------- Jeu ----------

    void NewGame()
    {
        for (int i = 0; i < Count - 1; i++) board[i] = i + 1;
        board[Count - 1] = 0;

        // Mélange par coups valides depuis l'état résolu : la grille est toujours soluble.
        int empty = Count - 1, previous = -1;
        int[] neighbours = new int[4];
        for (int m = 0; m < shuffleMoves || IsSolved(); m++)
        {
            int n = 0, r = empty / Size, c = empty % Size;
            if (r > 0) neighbours[n++] = empty - Size;
            if (r < Size - 1) neighbours[n++] = empty + Size;
            if (c > 0) neighbours[n++] = empty - 1;
            if (c < Size - 1) neighbours[n++] = empty + 1;
            int next;
            do next = neighbours[Random.Range(0, n)]; while (next == previous);
            board[empty] = board[next];
            board[next] = 0;
            previous = empty;
            empty = next;
        }

        moves = 0;
        won = false;
        invalidTapLogged = multiSlideLogged = false;
        for (int r = 0; r < Size; r++) rowLogged[r] = false;
        busy = false;
        endCard.gameObject.SetActive(false);
        UpdateHeader();
        PlaceTiles();
    }

    void Replay()
    {
        PlayableCta.LevelRetry(++replays);
        NewGame();
    }

    void OnTile(int value)
    {
        if (!busy && !won)
            StartCoroutine(Slide(value));
    }

    // Fait glisser la tuile touchée (et celles entre elle et le trou) vers la case vide.
    IEnumerator Slide(int value)
    {
        int t = IndexOf(value), e = IndexOf(0);
        int tr = t / Size, tc = t % Size, er = e / Size, ec = e % Size;
        if (tr != er && tc != ec)
        {
            if (!invalidTapLogged) PlayableCta.InvalidTap(moves);
            invalidTapLogged = true;
            yield break;
        }

        busy = true;
        int step = tr == er ? (tc > ec ? 1 : -1) : (tr > er ? Size : -Size);
        int[] moved = new int[Size];
        Vector2[] from = new Vector2[Size];
        int count = 0;
        while (e != t)
        {
            int n = e + step;
            board[e] = board[n];
            board[n] = 0;
            moved[count] = board[e];
            from[count] = tiles[board[e]].anchoredPosition;
            count++;
            e = n;
        }
        if (moves++ == 0) PlayableCta.FirstMove();
        if (moves == 10 || moves == 20 || moves == 50) PlayableCta.MovesReached(moves);
        if (count > 1 && !multiSlideLogged) PlayableCta.MultiTileSlide(count);
        multiSlideLogged |= count > 1;
        UpdateHeader();

        for (float k = 0f; k < 1f; k += Time.deltaTime / SlideTime)
        {
            float s = 1f - (1f - k) * (1f - k);
            for (int i = 0; i < count; i++)
                tiles[moved[i]].anchoredPosition = Vector2.Lerp(from[i], CellPos(IndexOf(moved[i])), s);
            yield return null;
        }
        PlaceTiles();
        busy = false;

        LogCompletedRows();
        if (IsSolved()) Win();
    }

    void Win()
    {
        won = true;
        endText.text = "Bravo !\nRésolu en " + moves + " coups";
        endCard.gameObject.SetActive(true);
        PlayableCta.LevelWon(moves);
    }

    // "row_1_complete" etc. : envoyé la première fois qu'une ligne est entièrement en place.
    void LogCompletedRows()
    {
        for (int r = 0; r < Size - 1; r++)
        {
            if (rowLogged[r]) continue;
            bool done = true;
            for (int c = 0; c < Size; c++)
                if (board[r * Size + c] != r * Size + c + 1) done = false;
            if (!done) continue;
            rowLogged[r] = true;
            PlayableCta.RowComplete(r + 1, moves);
        }
    }

    void OnCtaInGame()
    {
        PlayableCta.CtaInGame(moves);
    }

    void OnCtaEndCard()
    {
        PlayableCta.CtaEndCard(moves);
    }

    int IndexOf(int value)
    {
        for (int i = 0; i < Count; i++)
            if (board[i] == value) return i;
        return -1;
    }

    bool IsSolved()
    {
        for (int i = 0; i < Count - 1; i++)
            if (board[i] != i + 1) return false;
        return true;
    }

    void UpdateHeader()
    {
        headerText.text = moves == 0 ? "Remets les chiffres\ndans l'ordre !" : "Coups : " + moves;
    }

    // ---------- Interface ----------

    void BuildUI()
    {
        if (FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 720);
        // Luna gère MatchWidthOrHeight (comme le tuto) : on suit la plus petite dimension.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasRect = (RectTransform)canvasGo.transform;

        Stretch(AddImage(NewRect("Background", canvasRect), BgColor).rectTransform);

        // Tout le contenu vit dans la safe area (encoches, barres système).
        safeRect = NewRect("SafeArea", canvasRect);
        boardRect = AddImage(NewRect("Board", safeRect), BoardColor).rectTransform;
        for (int v = 1; v < Count; v++)
        {
            int value = v;
            var button = MakeButton(boardRect, value.ToString(), tileColor, TileTextColor, () => OnTile(value));
            tiles[v] = (RectTransform)button.transform;
        }

        header = NewRect("Header", safeRect);
        headerText = AddText(header, "", Color.white);

        cta = (RectTransform)MakeButton(safeRect, "TÉLÉCHARGER", ctaColor, Color.white, OnCtaInGame).transform;

        endCard = NewRect("EndCard", canvasRect);
        Stretch(endCard);
        AddImage(endCard, new Color(0f, 0f, 0f, 0.8f));
        endSafeRect = NewRect("SafeArea", endCard);
        endTextRect = NewRect("EndText", endSafeRect);
        endText = AddText(endTextRect, "", Color.white);
        install = (RectTransform)MakeButton(endSafeRect, "TÉLÉCHARGER", ctaColor, Color.white, OnCtaEndCard).transform;
        replay = (RectTransform)MakeButton(endSafeRect, "Rejouer", BoardColor, Color.white, Replay).transform;
    }

    // Toutes les tailles sont proportionnelles à la safe area.
    // Portrait : titre / plateau / bouton empilés. Paysage : plateau à gauche, panneau à droite.
    void Layout()
    {
        lastCanvasSize = canvasRect.rect.size;
        lastScreenW = Screen.width;
        lastScreenH = Screen.height;
        lastSafeArea = Screen.safeArea;
        if (lastSafeArea.width <= 0f || lastSafeArea.height <= 0f)
            lastSafeArea = new Rect(0f, 0f, Screen.width, Screen.height);
        scaler.matchWidthOrHeight = Screen.height >= Screen.width ? 0f : 1f;
        ApplySafeArea(safeRect);
        ApplySafeArea(endSafeRect);

        Vector2 s = Vector2.Scale(lastCanvasSize, new Vector2(
            lastSafeArea.width / Screen.width, lastSafeArea.height / Screen.height));
        float side;

        if (s.y >= s.x)
        {
            float margin = s.x * 0.05f, gap = s.y * 0.03f;
            float headerH = s.y * 0.1f, ctaH = Mathf.Min(s.y * 0.09f, s.x * 0.2f);
            side = Mathf.Min(s.x - 2 * margin, s.y - headerH - ctaH - 4 * gap);
            float top = (headerH + gap + side + gap + ctaH) / 2;
            header.anchoredPosition = new Vector2(0, top - headerH / 2);
            header.sizeDelta = new Vector2(s.x - 2 * margin, headerH);
            boardRect.anchoredPosition = new Vector2(0, top - headerH - gap - side / 2);
            cta.anchoredPosition = new Vector2(0, boardRect.anchoredPosition.y - side / 2 - gap - ctaH / 2);
            cta.sizeDelta = new Vector2(s.x * 0.75f, ctaH);
        }
        else
        {
            float margin = s.y * 0.05f;
            side = Mathf.Min(s.y - 2 * margin, s.x * 0.6f - 2 * margin);
            float panelWidth = s.x - side - 3 * margin;
            float panelX = (margin + side) / 2;
            boardRect.anchoredPosition = new Vector2(-s.x / 2 + margin + side / 2, 0);
            header.anchoredPosition = new Vector2(panelX, s.y * 0.15f);
            header.sizeDelta = new Vector2(panelWidth, s.y * 0.3f);
            cta.anchoredPosition = new Vector2(panelX, -s.y * 0.15f);
            cta.sizeDelta = new Vector2(Mathf.Min(panelWidth, s.y * 0.8f), s.y * 0.16f);
        }

        boardRect.sizeDelta = new Vector2(side, side);
        cell = side / Size;
        PlaceTiles();

        float u = Mathf.Min(s.x, s.y);
        endTextRect.anchoredPosition = new Vector2(0, u * 0.3f);
        endTextRect.sizeDelta = new Vector2(s.x * 0.9f, u * 0.3f);
        install.anchoredPosition = Vector2.zero;
        install.sizeDelta = new Vector2(u * 0.75f, u * 0.17f);
        replay.anchoredPosition = new Vector2(0, -u * 0.2f);
        replay.sizeDelta = new Vector2(u * 0.4f, u * 0.11f);
    }

    void ApplySafeArea(RectTransform rt)
    {
        rt.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
        rt.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
        rt.sizeDelta = Vector2.zero;
    }

    void PlaceTiles()
    {
        if (cell <= 0f) return;
        float tileSize = cell * 0.92f;
        for (int i = 0; i < Count; i++)
        {
            if (board[i] == 0) continue;
            var t = tiles[board[i]];
            t.sizeDelta = new Vector2(tileSize, tileSize);
            t.anchoredPosition = CellPos(i);
        }
    }

    Vector2 CellPos(int index)
    {
        float half = (Size - 1) * 0.5f;
        return new Vector2((index % Size - half) * cell, (half - index / Size) * cell);
    }

    static RectTransform NewRect(string name, Transform parent)
    {
        var rt = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rt.SetParent(parent, false);
        // Valeurs explicites : les défauts de RectTransform ne sont pas les mêmes sous Luna.
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;
        return rt;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
    }

    static Image AddImage(RectTransform rt, Color color)
    {
        var image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    Text AddText(RectTransform parent, string value, Color color)
    {
        var rt = NewRect("Text", parent);
        Stretch(rt);
        var text = rt.gameObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = 300;
        text.raycastTarget = false;
        return text;
    }

    Button MakeButton(RectTransform parent, string label, Color color, Color labelColor, UnityEngine.Events.UnityAction onClick)
    {
        var rt = NewRect(label, parent);
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = AddImage(rt, color);
        button.onClick.AddListener(onClick);
        var text = AddText(rt, label, labelColor);
        ((RectTransform)text.transform).sizeDelta = new Vector2(-16, -16);
        return button;
    }
}
