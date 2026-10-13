using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// UI-01 외관 선택 화면 프리팹을 목업 치수(1920×1080 기준)대로 조립한다.
// 메뉴: Yeyu > UI > Build Exterior Panel Prefab / Add Exterior Panel To Scene
// 조립 도우미(CreateRect, Place 등)는 _FeaturePanelBuilder의 것을 같이 쓴다.
public static class _ExteriorPanelBuilder
{
    private const string PrefabPath = "Assets/_Project/Prefabs/UI/ExteriorPanel.prefab";
    // 휠 버튼 그림. 에셋에 들어 있는 휠 사진을 그대로 쓴다 (CustomizationDock의 WheelDesign_01·02와 같은 그림)
    private const string WheelIconDir = "Assets/ThirdParty/URPLabStudio/Textures/UI/WheelIcons/";

    // 레이아웃 (px, 1920×1080 기준). 세로 값은 화면 위에서 잰 거리
    private const float SideMargin = 56f;
    private const float ColumnWidth = 732f;
    private const float TitleTop = 146f;
    private const float TabTop = 290f;
    private const float TabHeight = 84f;
    private const float SwatchCenterY = 516f;
    private const float SwatchRing = 142f;      // 선택 고리 지름
    private const float SwatchSize = 124f;      // 색 동그라미 지름
    private const float SwatchSpacing = 182f;   // 동그라미 중심 사이 거리
    private const float NameTop = 610f;
    private const float SummaryTop = 688f;
    private const float SummaryHeight = 197f;
    private const float SummaryValueLeft = 282f;
    private const float NextBottom = 83f;

    private static readonly Color Accent = new Color32(0x28, 0x66, 0xF2, 0xFF);
    private static readonly Color CardFill = new Color32(0x0A, 0x0E, 0x18, 0xB8);
    private static readonly Color IdleBorder = new Color(1f, 1f, 1f, 0.22f);
    private static readonly Color RingGap = new Color32(0x0B, 0x0E, 0x14, 0xFF);
    private static readonly Color HintText = new Color32(0x9A, 0xA0, 0xAC, 0xFF);

    private static readonly string[] TabNames = { "Color", "Wheel" };
    private static readonly string[] TabTitles = { "차체 색상", "휠" };

    // Car 프리팹 CarAppearance의 Body Materials 순서(화이트, 실버, 다크 그레이, 레드)와 같아야 한다
    private static readonly string[] ColorNames = { "화이트", "실버", "다크 그레이", "레드" };
    private static readonly Color[] ColorSwatches =
    {
        new Color32(0xF2, 0xF2, 0xF2, 0xFF),
        new Color32(0xA8, 0xAD, 0xB4, 0xFF),
        new Color32(0x4A, 0x53, 0x60, 0xFF),
        new Color32(0xB8, 0x10, 0x0F, 0xFF),
    };

    // Car 프리팹 CarAppearance의 Wheels 순서(Tires_A, Tires_B)와 같아야 한다
    private static readonly string[] WheelNames = { "휠 A", "휠 B" };
    private static readonly string[] WheelIcons = { "Wheel_A_Icon_Photoreal", "Wheel_B_Icon_Photoreal" };

    private static TMP_FontAsset _regular;
    private static TMP_FontAsset _bold;

    [MenuItem("Yeyu/UI/Build Exterior Panel Prefab")]
    public static void BuildPrefab()
    {
        GameObject prefab = Build();
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"ExteriorPanel prefab saved: {PrefabPath}", prefab);
    }

    // 프리팹을 씬의 FeaturePanel 옆에 놓고, 씬에서만 연결할 수 있는 것(쇼룸 영역, UIManager, [다음])을 연결한다
    [MenuItem("Yeyu/UI/Add Exterior Panel To Scene")]
    public static void AddToScene()
    {
        ExteriorPanel existing = Object.FindAnyObjectByType<ExteriorPanel>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.LogWarning("ExteriorPanel is already in the scene. Delete it first to add a new one.", existing);
            return;
        }

        // 놓을 자리: Hierarchy에서 고른 UI 오브젝트 → FeaturePanel의 부모 → 영역(주행·주차)에 속하지 않은 Canvas
        FeaturePanel feature = Object.FindAnyObjectByType<FeaturePanel>(FindObjectsInactive.Include);
        Transform parent = null;
        Transform selected = Selection.activeTransform;
        if (selected != null && selected.GetComponentInParent<Canvas>(true) != null)
            parent = selected;
        if (parent == null && feature != null)
            parent = feature.transform.parent;
        if (parent == null)
        {
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (canvas.isRootCanvas && canvas.GetComponentInParent<AreaBase>(true) == null)
                {
                    parent = canvas.transform;
                    break;
                }
            }
        }
        if (parent == null)
        {
            Debug.LogError("No place for ExteriorPanel. Select the parent UI object in the Hierarchy and try again.");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            prefab = Build();

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(instance, "Add Exterior Panel");
        if (feature != null && feature.transform.parent == parent)
            instance.transform.SetSiblingIndex(feature.transform.GetSiblingIndex());

        var panel = instance.GetComponent<ExteriorPanel>();
        var so = new SerializedObject(panel);
        ConfiguratorArea area = Object.FindAnyObjectByType<ConfiguratorArea>(FindObjectsInactive.Include);
        so.FindProperty("_area").objectReferenceValue = area;
        so.ApplyModifiedPropertiesWithoutUndo();
        if (area == null)
            Debug.LogWarning("ConfiguratorArea not found. Assign ExteriorPanel > Area by hand.", panel);

        UIManager uiManager = Object.FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
        if (uiManager != null)
        {
            UnityEventTools.AddVoidPersistentListener(panel.NextClicked, uiManager.ShowFeature);

            var uiSo = new SerializedObject(uiManager);
            uiSo.FindProperty("_exteriorPanel").objectReferenceValue = instance;
            uiSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(uiManager);
        }
        else
        {
            Debug.LogWarning("UIManager not found. Connect On Next and UIManager > Exterior Panel by hand.", panel);
        }

        EditorUtility.SetDirty(panel);
        PrefabUtility.RecordPrefabInstancePropertyModifications(panel);
        EditorSceneManager.MarkSceneDirty(instance.scene);
        Selection.activeGameObject = instance;
    }

    private static GameObject Build()
    {
        _regular = _FeaturePanelBuilder.LoadOrCreateFont("Pretendard-Regular");
        _bold = _FeaturePanelBuilder.LoadOrCreateFont("Pretendard-Bold");

        RectTransform root = _FeaturePanelBuilder.CreateRect("ExteriorPanel", null);
        _FeaturePanelBuilder.Stretch(root, 0f, 0f, 0f, 0f);
        var panel = root.gameObject.AddComponent<ExteriorPanel>();

        // 오른쪽 선택 영역이 3D 미리보기 위에서도 읽히도록 깔아 두는 어두운 그라데이션
        Image shade = _FeaturePanelBuilder.CreateImage("RightShade", root, LoadSprite("UI_GradientH"), new Color(0f, 0f, 0f, 0.6f), false);
        shade.rectTransform.anchorMin = new Vector2(0.45f, 0f);
        shade.rectTransform.anchorMax = Vector2.one;
        shade.rectTransform.offsetMin = Vector2.zero;
        shade.rectTransform.offsetMax = Vector2.zero;

        // 오른쪽 세로 한 줄. 안의 것은 모두 이 줄의 왼쪽 위를 기준으로 놓는다
        RectTransform column = _FeaturePanelBuilder.CreateRect("Column", root);
        column.anchorMin = new Vector2(1f, 0f);
        column.anchorMax = Vector2.one;
        column.pivot = new Vector2(1f, 1f);
        column.offsetMin = new Vector2(-(SideMargin + ColumnWidth), 0f);
        column.offsetMax = new Vector2(-SideMargin, 0f);

        TMP_Text title = MakeText("Title", column, "차량 컨피규레이터", _bold, 64f, Color.white, TextAlignmentOptions.Center);
        _FeaturePanelBuilder.PlaceRow(title.rectTransform, 0f, TitleTop, 76f);

        var tabs = new Button[2];
        var tabLabels = new TMP_Text[2];
        var tabUnderlines = new Graphic[2];
        BuildTabBar(column, tabs, tabLabels, tabUnderlines);

        var pages = new GameObject[2];

        // 차체 색상 탭
        RectTransform colorPage = _FeaturePanelBuilder.CreateRect("Page_Color", column);
        _FeaturePanelBuilder.Stretch(colorPage, 0f, 0f, 0f, 0f);
        pages[0] = colorPage.gameObject;

        var colorButtons = new Button[ColorNames.Length];
        var colorBorders = new Graphic[ColorNames.Length];
        float firstX = (ColumnWidth - SwatchSpacing * (ColorNames.Length - 1)) * 0.5f;
        for (int i = 0; i < ColorNames.Length; i++)
        {
            RectTransform swatch = BuildSwatch(colorPage, i, out colorButtons[i], out colorBorders[i]);
            _FeaturePanelBuilder.Place(swatch, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(firstX + i * SwatchSpacing, -SwatchCenterY), new Vector2(SwatchRing, SwatchRing));
        }

        TMP_Text colorName = MakeText("SelectedName", colorPage, ColorNames[0], _bold, 34f, Color.white, TextAlignmentOptions.Left);
        _FeaturePanelBuilder.PlaceRow(colorName.rectTransform, 12f, NameTop, 46f);

        // 휠 탭
        RectTransform wheelPage = _FeaturePanelBuilder.CreateRect("Page_Wheel", column);
        _FeaturePanelBuilder.Stretch(wheelPage, 0f, 0f, 0f, 0f);
        pages[1] = wheelPage.gameObject;

        var wheelButtons = new Button[WheelNames.Length];
        var wheelBorders = new Graphic[WheelNames.Length];
        for (int i = 0; i < WheelNames.Length; i++)
        {
            // 색 동그라미의 앞쪽 칸과 같은 자리에 놓는다
            RectTransform wheel = BuildWheelButton(wheelPage, i, out wheelButtons[i], out wheelBorders[i]);
            _FeaturePanelBuilder.Place(wheel, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(firstX + i * SwatchSpacing, -SwatchCenterY), new Vector2(SwatchRing, SwatchRing));
        }

        TMP_Text wheelName = MakeText("SelectedName", wheelPage, WheelNames[0], _bold, 34f, Color.white, TextAlignmentOptions.Left);
        _FeaturePanelBuilder.PlaceRow(wheelName.rectTransform, 12f, NameTop, 46f);

        wheelPage.gameObject.SetActive(false);

        RectTransform summary = BuildSummaryCard(column, out Graphic summaryDot, out TMP_Text summaryColor, out TMP_Text summaryWheel);
        _FeaturePanelBuilder.PlaceRow(summary, 0f, SummaryTop, SummaryHeight);

        Button next = BuildNextButton(root);

        var so = new SerializedObject(panel);
        so.FindProperty("_config").objectReferenceValue = _FeaturePanelBuilder.FindConfig();
        _FeaturePanelBuilder.SetArray(so, "_tabs", tabs);
        _FeaturePanelBuilder.SetArray(so, "_tabLabels", tabLabels);
        _FeaturePanelBuilder.SetArray(so, "_tabUnderlines", tabUnderlines);
        _FeaturePanelBuilder.SetArray(so, "_tabPages", pages);
        _FeaturePanelBuilder.SetArray(so, "_colorButtons", colorButtons);
        _FeaturePanelBuilder.SetArray(so, "_colorBorders", colorBorders);
        SetStrings(so, "_colorNames", ColorNames);
        SetColors(so, "_colorSwatches", ColorSwatches);
        so.FindProperty("_colorNameText").objectReferenceValue = colorName;
        _FeaturePanelBuilder.SetArray(so, "_wheelButtons", wheelButtons);
        _FeaturePanelBuilder.SetArray(so, "_wheelBorders", wheelBorders);
        SetStrings(so, "_wheelNames", WheelNames);
        so.FindProperty("_wheelNameText").objectReferenceValue = wheelName;
        // 휠 버튼도 색 동그라미처럼 선택된 것에만 고리가 보인다
        so.FindProperty("_borderIdleColor").colorValue = Color.clear;
        so.FindProperty("_summaryColorDot").objectReferenceValue = summaryDot;
        so.FindProperty("_summaryColorText").objectReferenceValue = summaryColor;
        so.FindProperty("_summaryWheelText").objectReferenceValue = summaryWheel;
        so.FindProperty("_nextButton").objectReferenceValue = next;
        so.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        Object.DestroyImmediate(root.gameObject);
        AssetDatabase.SaveAssets();
        return prefab;
    }

    // 글자 탭 2개와 그 아래 줄. 선택된 탭 아래에만 파란 줄이 보인다
    private static void BuildTabBar(RectTransform column, Button[] tabs, TMP_Text[] labels, Graphic[] underlines)
    {
        RectTransform bar = _FeaturePanelBuilder.CreateRect("TabBar", column);
        _FeaturePanelBuilder.PlaceRow(bar, 0f, TabTop, TabHeight);

        Image baseLine = _FeaturePanelBuilder.CreateImage("BaseLine", bar, null, IdleBorder, false);
        baseLine.rectTransform.anchorMin = Vector2.zero;
        baseLine.rectTransform.anchorMax = new Vector2(1f, 0f);
        baseLine.rectTransform.pivot = new Vector2(0.5f, 0f);
        baseLine.rectTransform.offsetMin = Vector2.zero;
        baseLine.rectTransform.offsetMax = new Vector2(0f, 2f);

        for (int i = 0; i < 2; i++)
        {
            // 눌리는 범위만 잡는 투명 이미지
            Image tab = _FeaturePanelBuilder.CreateImage("Tab_" + TabNames[i], bar, null, Color.clear, false);
            tab.raycastTarget = true;
            tab.rectTransform.anchorMin = new Vector2(i * 0.5f, 0f);
            tab.rectTransform.anchorMax = new Vector2((i + 1) * 0.5f, 1f);
            tab.rectTransform.offsetMin = Vector2.zero;
            tab.rectTransform.offsetMax = Vector2.zero;

            tabs[i] = tab.gameObject.AddComponent<Button>();
            tabs[i].targetGraphic = tab;
            tabs[i].transition = Selectable.Transition.None;

            labels[i] = MakeText("Label", tab.transform, TabTitles[i], _bold, 36f, i == 0 ? Color.white : HintText, TextAlignmentOptions.Center);
            _FeaturePanelBuilder.Stretch(labels[i].rectTransform, 0f, 0f, 0f, 10f);

            Image underline = _FeaturePanelBuilder.CreateImage("Underline", tab.transform, null, i == 0 ? Accent : Color.clear, false);
            underline.rectTransform.anchorMin = Vector2.zero;
            underline.rectTransform.anchorMax = new Vector2(1f, 0f);
            underline.rectTransform.pivot = new Vector2(0.5f, 0f);
            underline.rectTransform.offsetMin = Vector2.zero;
            underline.rectTransform.offsetMax = new Vector2(0f, 5f);
            underlines[i] = underline;
        }
    }

    // 색 동그라미 하나: 선택 고리(파랑) → 어두운 틈 → 색
    private static RectTransform BuildSwatch(RectTransform page, int index, out Button button, out Graphic border)
    {
        RectTransform rect = _FeaturePanelBuilder.CreateRect("ColorButton_" + index, page);
        Sprite circle = LoadSprite("UI_Circle");
        float inset = (SwatchRing - SwatchSize) * 0.5f;

        Image ring = _FeaturePanelBuilder.CreateImage("Border", rect, circle, index == 0 ? Accent : Color.clear, false);
        _FeaturePanelBuilder.Stretch(ring.rectTransform, 0f, 0f, 0f, 0f);
        border = ring;

        Image gap = _FeaturePanelBuilder.CreateImage("Gap", rect, circle, RingGap, false);
        _FeaturePanelBuilder.Stretch(gap.rectTransform, inset - 4f, inset - 4f, inset - 4f, inset - 4f);

        Image fill = _FeaturePanelBuilder.CreateImage("Swatch", rect, circle, ColorSwatches[index], false);
        fill.raycastTarget = true;
        _FeaturePanelBuilder.Stretch(fill.rectTransform, inset, inset, inset, inset);

        button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = fill;
        TintOnPress(button);
        return rect;
    }

    // 휠 버튼 하나: 선택 고리(파랑) → 어두운 틈 → 휠 사진
    private static RectTransform BuildWheelButton(RectTransform page, int index, out Button button, out Graphic border)
    {
        RectTransform rect = _FeaturePanelBuilder.CreateRect("WheelButton_" + index, page);
        Sprite circle = LoadSprite("UI_Circle");
        float inset = (SwatchRing - SwatchSize) * 0.5f;

        Image ring = _FeaturePanelBuilder.CreateImage("Border", rect, circle, index == 0 ? Accent : Color.clear, false);
        _FeaturePanelBuilder.Stretch(ring.rectTransform, 0f, 0f, 0f, 0f);
        border = ring;

        Image gap = _FeaturePanelBuilder.CreateImage("Gap", rect, circle, RingGap, false);
        _FeaturePanelBuilder.Stretch(gap.rectTransform, inset - 4f, inset - 4f, inset - 4f, inset - 4f);

        var icon = AssetDatabase.LoadAssetAtPath<Sprite>(WheelIconDir + WheelIcons[index] + ".png");
        if (icon == null)
            Debug.LogError($"Wheel icon not found: {WheelIconDir}{WheelIcons[index]}.png");

        Image thumbnail = _FeaturePanelBuilder.CreateImage("Thumbnail", rect, icon, Color.white, false);
        thumbnail.preserveAspect = true;
        thumbnail.raycastTarget = true;
        _FeaturePanelBuilder.Stretch(thumbnail.rectTransform, inset, inset, inset, inset);

        button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = thumbnail;
        TintOnPress(button);
        return rect;
    }

    // "선택한 옵션" 카드: 차체 색상(색 점 + 이름), 휠(이름)
    private static RectTransform BuildSummaryCard(RectTransform column, out Graphic colorDot, out TMP_Text colorText, out TMP_Text wheelText)
    {
        RectTransform card = _FeaturePanelBuilder.CreateRect("SummaryCard", column);

        Image background = _FeaturePanelBuilder.CreateImage("Background", card, LoadSprite("UI_RoundRect"), CardFill, true);
        _FeaturePanelBuilder.Stretch(background.rectTransform, 0f, 0f, 0f, 0f);
        Image outline = _FeaturePanelBuilder.CreateImage("Border", card, LoadSprite("UI_RoundRectOutline"), IdleBorder, true);
        _FeaturePanelBuilder.Stretch(outline.rectTransform, 0f, 0f, 0f, 0f);

        TMP_Text title = MakeText("Title", card, "선택한 옵션", _bold, 30f, Color.white, TextAlignmentOptions.Left);
        _FeaturePanelBuilder.PlaceRow(title.rectTransform, 34f, 26f, 40f);

        const float colorRowTop = 81f;
        const float wheelRowTop = 134f;
        const float rowHeight = 40f;
        const float dotSize = 24f;

        TMP_Text colorLabel = MakeText("ColorLabel", card, "차체 색상", _regular, 26f, HintText, TextAlignmentOptions.Left);
        _FeaturePanelBuilder.PlaceRow(colorLabel.rectTransform, 34f, colorRowTop, rowHeight);

        Image dot = _FeaturePanelBuilder.CreateImage("ColorDot", card, LoadSprite("UI_Circle"), ColorSwatches[0], false);
        _FeaturePanelBuilder.Place(dot.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
            new Vector2(SummaryValueLeft, -(colorRowTop + rowHeight * 0.5f)), new Vector2(dotSize, dotSize));
        colorDot = dot;

        colorText = MakeText("ColorValue", card, ColorNames[0], _bold, 30f, Color.white, TextAlignmentOptions.Left);
        _FeaturePanelBuilder.PlaceRow(colorText.rectTransform, SummaryValueLeft + dotSize + 18f, colorRowTop, rowHeight);

        TMP_Text wheelLabel = MakeText("WheelLabel", card, "휠", _regular, 26f, HintText, TextAlignmentOptions.Left);
        _FeaturePanelBuilder.PlaceRow(wheelLabel.rectTransform, 34f, wheelRowTop, rowHeight);

        wheelText = MakeText("WheelValue", card, WheelNames[0], _bold, 30f, Color.white, TextAlignmentOptions.Left);
        _FeaturePanelBuilder.PlaceRow(wheelText.rectTransform, SummaryValueLeft, wheelRowTop, rowHeight);

        return card;
    }

    // 오른쪽 아래 [다음]: 파란 알약 버튼과 뒤쪽 불빛
    private static Button BuildNextButton(RectTransform root)
    {
        RectTransform rect = _FeaturePanelBuilder.CreateRect("NextButton", root);
        _FeaturePanelBuilder.Place(rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-SideMargin, NextBottom), new Vector2(326f, 68f));

        Image glow = _FeaturePanelBuilder.CreateImage("Glow", rect, LoadSprite("UI_Glow"), new Color(Accent.r, Accent.g, Accent.b, 0.55f), true);
        _FeaturePanelBuilder.Stretch(glow.rectTransform, -40f, -40f, -40f, -40f);

        Image background = _FeaturePanelBuilder.CreateImage("Background", rect, LoadSprite("UI_Circle"), Accent, true);
        background.raycastTarget = true;
        _FeaturePanelBuilder.Stretch(background.rectTransform, 0f, 0f, 0f, 0f);

        TMP_Text label = MakeText("Label", rect, "다음", _bold, 28f, Color.white, TextAlignmentOptions.Center);
        _FeaturePanelBuilder.Stretch(label.rectTransform, 0f, 0f, 0f, 0f);

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        TintOnPress(button);
        return button;
    }

    // ---------- 도우미 ----------

    private static Sprite LoadSprite(string name)
    {
        Sprite sprite = _FeaturePanelBuilder.LoadSprite(name);
        if (sprite == null)
            Debug.LogError($"Sprite not found: {name}. Run Yeyu > UI > Build Feature Panel Prefab once to import the UI sprites.");
        return sprite;
    }

    private static TMP_Text MakeText(string name, Transform parent, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
    {
        return _FeaturePanelBuilder.CreateText(name, parent, text, font, size, color, alignment);
    }

    private static void TintOnPress(Button button)
    {
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
    }

    private static void SetStrings(SerializedObject so, string propertyName, string[] values)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).stringValue = values[i];
    }

    private static void SetColors(SerializedObject so, string propertyName, Color[] values)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).colorValue = values[i];
    }
}
