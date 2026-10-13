using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// UI-06 스마트키 화면을 ParkingArea 프리팹 안에 목업 치수(1920×1080 기준)대로 조립한다.
// 메뉴: Yeyu > UI > Build Smart Key UI In Parking Area
// 오른쪽 아래 카드에 도형으로 그린 차키([전진] [후진])를 두고, [탑승] 버튼은 오른쪽 화면 가운데 아래에 둔다.
// 조립 도우미(CreateRect, Place 등)는 _FeaturePanelBuilder의 것을 같이 쓴다.
public static class _SmartKeyUIBuilder
{
    private const string AreaPrefabPath = "Assets/_Project/Prefabs/Parking/ParkingArea.prefab";
    private const string CanvasName = "SmartKeyCanvas";

    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    // 카드 (오른쪽 아래). 안의 것은 모두 카드 왼쪽 위에서 잰 거리로 놓는다
    private static readonly Vector2 CardSize = new Vector2(348f, 424f);
    private const float CardMargin = 20f;
    private const float HeaderHeight = 64f;

    // 차키 몸체와 버튼
    private static readonly Vector2 KeyPosition = new Vector2(64f, 107f);
    private static readonly Vector2 KeySize = new Vector2(105f, 264f);
    private static readonly Vector2 KeyButtonSize = new Vector2(60f, 56f);
    private const float ForwardTop = 69f;       // 몸체 위에서 [전진] 버튼까지
    private const float ReverseTop = 144f;      // 몸체 위에서 [후진] 버튼까지
    private const float LabelLeft = 212f;       // "전진" "후진" 글자의 왼쪽

    // [탑승] 버튼: 오른쪽 화면(가로 75% 지점)의 아래쪽
    private const float BoardBottom = 150f;
    private static readonly Vector2 BoardSize = new Vector2(326f, 76f);

    private static readonly Color Accent = new Color32(0x28, 0x66, 0xF2, 0xFF);
    private static readonly Color CardFill = new Color32(0x0A, 0x0E, 0x18, 0xE6);
    private static readonly Color IdleBorder = new Color(1f, 1f, 1f, 0.22f);
    private static readonly Color HintText = new Color32(0x9A, 0xA0, 0xAC, 0xFF);
    private static readonly Color KeyShell = new Color32(0xA9, 0xAE, 0xB5, 0xFF);
    private static readonly Color KeyPlate = new Color32(0x2B, 0x2F, 0x37, 0xFF);
    private static readonly Color KeyButtonIdle = new Color32(0x4A, 0x50, 0x5B, 0xFF);

    private static TMP_FontAsset _regular;
    private static TMP_FontAsset _bold;

    [MenuItem("Yeyu/UI/Build Smart Key UI In Parking Area")]
    public static void Build()
    {
        _regular = _FeaturePanelBuilder.LoadOrCreateFont("Pretendard-Regular");
        _bold = _FeaturePanelBuilder.LoadOrCreateFont("Pretendard-Bold");

        GameObject root = PrefabUtility.LoadPrefabContents(AreaPrefabPath);
        try
        {
            var scenario = root.GetComponentInChildren<ParkingScenario>(true);
            if (scenario == null)
            {
                Debug.LogError($"ParkingScenario not found in {AreaPrefabPath}");
                return;
            }

            // 다시 실행하면 예전 것을 지우고 새로 만든다
            Transform old = root.transform.Find(CanvasName);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            RectTransform canvasRect = _FeaturePanelBuilder.CreateRect(CanvasName, root.transform);
            var canvas = canvasRect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasRect.gameObject.AddComponent<GraphicRaycaster>();
            var ui = canvasRect.gameObject.AddComponent<SmartKeyUI>();

            RectTransform card = BuildCard(canvasRect, out TMP_Text status);
            RectTransform key = BuildKeyBody(card);

            Button forward = BuildKeyButton(key, "ForwardButton", ForwardTop, true);
            Button reverse = BuildKeyButton(key, "ReverseButton", ReverseTop, false);
            BuildButtonLabel(card, "전진", ForwardTop);
            BuildButtonLabel(card, "후진", ReverseTop);

            Button board = BuildBoardButton(canvasRect);

            var so = new SerializedObject(ui);
            so.FindProperty("_forwardButton").objectReferenceValue = forward;
            so.FindProperty("_reverseButton").objectReferenceValue = reverse;
            so.FindProperty("_boardButton").objectReferenceValue = board;
            so.FindProperty("_statusText").objectReferenceValue = status;

            // 조작할 차는 ParkingScenario가 스마트키 장면에서 쓰는 적용 쪽 차와 같다
            var scenarioSo = new SerializedObject(scenario);
            Object remote = scenarioSo.FindProperty("_remote").objectReferenceValue;
            so.FindProperty("_remoteParking").objectReferenceValue = remote;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (remote == null)
                Debug.LogWarning("ParkingScenario > Remote is empty. Assign SmartKeyUI > Remote Parking by hand.");

            scenarioSo.FindProperty("_smartKeyUI").objectReferenceValue = canvasRect.gameObject;
            scenarioSo.ApplyModifiedPropertiesWithoutUndo();

            // 스마트키 장면에서만 ParkingScenario가 켠다
            canvasRect.gameObject.SetActive(false);

            PrefabUtility.SaveAsPrefabAsset(root, AreaPrefabPath);
            Debug.Log($"Smart key UI saved in {AreaPrefabPath} ({CanvasName})");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // 오른쪽 아래 카드: 위에 상태 문구 한 줄, 아래 오른쪽에 "길게 누르기"
    private static RectTransform BuildCard(RectTransform canvasRect, out TMP_Text status)
    {
        RectTransform card = _FeaturePanelBuilder.CreateRect("KeyCard", canvasRect);
        _FeaturePanelBuilder.Place(card, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-CardMargin, CardMargin), CardSize);

        Image background = _FeaturePanelBuilder.CreateImage("Background", card, _FeaturePanelBuilder.LoadSprite("UI_RoundRect"), CardFill, true);
        _FeaturePanelBuilder.Stretch(background.rectTransform, 0f, 0f, 0f, 0f);
        Image outline = _FeaturePanelBuilder.CreateImage("Border", card, _FeaturePanelBuilder.LoadSprite("UI_RoundRectOutline"), IdleBorder, true);
        _FeaturePanelBuilder.Stretch(outline.rectTransform, 0f, 0f, 0f, 0f);

        // 상태 문구 앞의 신호 점
        Image dot = _FeaturePanelBuilder.CreateImage("SignalDot", card, _FeaturePanelBuilder.LoadSprite("UI_Circle"), Accent, false);
        _FeaturePanelBuilder.Place(dot.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(26f, -HeaderHeight * 0.5f), new Vector2(14f, 14f));

        status = _FeaturePanelBuilder.CreateText("Status", card, "준비 완료", _bold, 24f, Color.white, TextAlignmentOptions.Left);
        _FeaturePanelBuilder.PlaceRow(status.rectTransform, 52f, 0f, HeaderHeight);

        Image divider = _FeaturePanelBuilder.CreateImage("Divider", card, null, IdleBorder, false);
        _FeaturePanelBuilder.PlaceRow(divider.rectTransform, 22f, HeaderHeight, 2f);

        TMP_Text hint = _FeaturePanelBuilder.CreateText("Hint", card, "길게 누르기", _regular, 18f, HintText, TextAlignmentOptions.Right);
        _FeaturePanelBuilder.PlaceRow(hint.rectTransform, 24f, CardSize.y - 46f, 28f);

        return card;
    }

    // 차키 몸체: 밝은 회색 껍데기 → 어두운 판 → 위쪽 고리 구멍, 아래쪽 작은 동그라미
    private static RectTransform BuildKeyBody(RectTransform card)
    {
        Sprite roundRect = _FeaturePanelBuilder.LoadSprite("UI_RoundRect");
        Sprite circle = _FeaturePanelBuilder.LoadSprite("UI_Circle");

        Image shell = _FeaturePanelBuilder.CreateImage("Key", card, circle, KeyShell, true);
        _FeaturePanelBuilder.Place(shell.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(KeyPosition.x, -KeyPosition.y), KeySize);

        Image plate = _FeaturePanelBuilder.CreateImage("Plate", shell.transform, roundRect, KeyPlate, true);
        _FeaturePanelBuilder.Stretch(plate.rectTransform, 12f, 50f, 12f, 40f);

        Image ringHole = _FeaturePanelBuilder.CreateImage("RingHole", shell.transform, circle, KeyPlate, true);
        _FeaturePanelBuilder.Place(ringHole.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(46f, 16f));

        Image bottomDot = _FeaturePanelBuilder.CreateImage("BottomDot", shell.transform, circle, KeyPlate, false);
        _FeaturePanelBuilder.Place(bottomDot.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(14f, 14f));

        return shell.rectTransform;
    }

    // 차키 버튼 하나. 평소에는 어두운 회색, 누르고 있으면 파란색. 안에 위·아래 화살표를 그린다
    private static Button BuildKeyButton(RectTransform key, string name, float top, bool arrowUp)
    {
        RectTransform rect = _FeaturePanelBuilder.CreateRect(name, key);
        _FeaturePanelBuilder.Place(rect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -top), KeyButtonSize);

        Image background = _FeaturePanelBuilder.CreateImage("Background", rect, _FeaturePanelBuilder.LoadSprite("UI_RoundRect"), Color.white, true);
        background.raycastTarget = true;
        _FeaturePanelBuilder.Stretch(background.rectTransform, 0f, 0f, 0f, 0f);

        BuildArrow(rect, arrowUp);

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        ColorBlock colors = button.colors;
        colors.normalColor = KeyButtonIdle;
        colors.highlightedColor = Color.Lerp(KeyButtonIdle, Accent, 0.35f);
        colors.pressedColor = Accent;
        colors.selectedColor = KeyButtonIdle;
        colors.fadeDuration = 0.05f;
        button.colors = colors;
        return button;
    }

    // 막대 3개로 그린 화살표 (세로 막대 + 45도로 기운 머리 2개). 아래쪽 화살표는 통째로 뒤집는다
    private static void BuildArrow(RectTransform button, bool up)
    {
        RectTransform arrow = _FeaturePanelBuilder.CreateRect("Arrow", button);
        _FeaturePanelBuilder.Place(arrow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));
        if (!up)
            arrow.localRotation = Quaternion.Euler(0f, 0f, 180f);

        const float thickness = 4f;

        Image shaft = _FeaturePanelBuilder.CreateImage("Shaft", arrow, null, Color.white, false);
        _FeaturePanelBuilder.Place(shaft.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(thickness, 26f));

        // 머리: 꼭대기(0, 13)에서 양쪽 아래로 내려오는 막대. 위쪽 끝을 축으로 돌린다
        for (int side = -1; side <= 1; side += 2)
        {
            Image head = _FeaturePanelBuilder.CreateImage(side < 0 ? "HeadLeft" : "HeadRight", arrow, null, Color.white, false);
            _FeaturePanelBuilder.Place(head.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, 14f), new Vector2(thickness, 15f));
            head.rectTransform.localRotation = Quaternion.Euler(0f, 0f, side * 45f);
        }
    }

    // 버튼 오른쪽으로 뻗은 가는 줄과 "전진" "후진" 글자
    private static void BuildButtonLabel(RectTransform card, string label, float buttonTop)
    {
        float centerY = KeyPosition.y + buttonTop + KeyButtonSize.y * 0.5f;
        float lineLeft = KeyPosition.x + (KeySize.x + KeyButtonSize.x) * 0.5f + 6f;

        Image line = _FeaturePanelBuilder.CreateImage("Line_" + label, card, null, new Color(1f, 1f, 1f, 0.6f), false);
        _FeaturePanelBuilder.Place(line.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            new Vector2(lineLeft, -centerY), new Vector2(LabelLeft - lineLeft - 10f, 2f));

        TMP_Text text = _FeaturePanelBuilder.CreateText("Label_" + label, card, label, _bold, 24f, Color.white, TextAlignmentOptions.Left);
        _FeaturePanelBuilder.Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            new Vector2(LabelLeft, -centerY), new Vector2(110f, 36f));
    }

    // 오른쪽 화면 아래 가운데의 [탑승]: 파란 알약 버튼과 뒤쪽 불빛. SmartKeyUI가 탑승 가능할 때만 켠다
    private static Button BuildBoardButton(RectTransform canvasRect)
    {
        RectTransform rect = _FeaturePanelBuilder.CreateRect("BoardButton", canvasRect);
        _FeaturePanelBuilder.Place(rect, new Vector2(0.75f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, BoardBottom), BoardSize);

        Image glow = _FeaturePanelBuilder.CreateImage("Glow", rect, _FeaturePanelBuilder.LoadSprite("UI_Glow"), new Color(Accent.r, Accent.g, Accent.b, 0.55f), true);
        _FeaturePanelBuilder.Stretch(glow.rectTransform, -40f, -40f, -40f, -40f);

        Image background = _FeaturePanelBuilder.CreateImage("Background", rect, _FeaturePanelBuilder.LoadSprite("UI_Circle"), Accent, true);
        background.raycastTarget = true;
        _FeaturePanelBuilder.Stretch(background.rectTransform, 0f, 0f, 0f, 0f);

        TMP_Text label = _FeaturePanelBuilder.CreateText("Label", rect, "탑승", _bold, 30f, Color.white, TextAlignmentOptions.Center);
        _FeaturePanelBuilder.Stretch(label.rectTransform, 0f, 0f, 0f, 0f);

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;

        rect.gameObject.SetActive(false);
        return button;
    }
}
