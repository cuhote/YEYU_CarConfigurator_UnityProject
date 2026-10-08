using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;


// UI-02 기능 선택 화면 프리팹을 목업 치수(1920×1080 기준)대로 조립한다.
// 메뉴: Yeyu > UI > Build Feature Panel Prefab / Add Feature Panel To Scene
public static class FeaturePanelBuilder
{
    private const string IconDir = "Assets/_Project/UI/Icons/";
    private const string FontDir = "Assets/_Project/UI/Fonts/";
    private const string PrefabPath = "Assets/_Project/Prefabs/UI/FeaturePanel.prefab";

    private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

    // 레이아웃 (px, 1920×1080 기준)
    private const float SideMargin = 58f;
    private const float BottomMargin = 49f;
    private const float CardWidth = 388f;
    private const float CardHeight = 253f;
    private const float CardGap = 28f;
    private const float CardTop = 227f;
    private const float TitleTop = 160f;
    private const float ButtonHeight = 68f;

    private static readonly Color Accent = new Color32(0x28, 0x66, 0xF2, 0xFF);
    private static readonly Color CardFill = new Color32(0x0A, 0x0E, 0x18, 0xB8);
    private static readonly Color IdleBorder = new Color(1f, 1f, 1f, 0.22f);
    private static readonly Color TabBarFill = new Color32(0x3A, 0x3E, 0x48, 0xEB);
    private static readonly Color TabIdleText = new Color32(0xC9, 0xCD, 0xD6, 0xFF);
    private static readonly Color BodyText = new Color32(0xD5, 0xD8, 0xDF, 0xFF);
    private static readonly Color HintText = new Color32(0x9A, 0xA0, 0xAC, 0xFF);
    private static readonly Color DarkText = new Color32(0x11, 0x13, 0x18, 0xFF);

    private static readonly string[] TabNames = { "Driving", "Parking" };
    private static readonly string[] TabTitles = { "주행", "주차" };
    private static readonly string[] StartLabels = { "주행 시작", "주차 시작" };

    private static readonly string[] FeatureNames = { "NightVision", "RearWheelSteering", "Valet", "SmartKey" };
    private static readonly string[] FeatureTitles = { "나이트 비전", "후륜 조향", "주차장 연동 발렛", "스마트키 원격 주차" };
    private static readonly string[] FeatureDescriptions =
    {
        "야간 주행 시 전방의 열화상 이미지를\n제공하여 안전을 강화합니다.",
        "저속 주행 시 회전 반경을 줄이고\n고속 주행 시 조향 안정성을 높입니다.",
        "자동 주차 후 스마트폰으로 알림을\n보내고 다시 호출할 수 있습니다.",
        "스마트키로 주차 공간에 원격으로\n전후진하여 주차할 수 있습니다.",
    };

    private static TMP_FontAsset _regular;
    private static TMP_FontAsset _bold;

    [MenuItem("Yeyu/UI/Build Feature Panel Prefab")]
    public static void BuildPrefab()
    {
        GameObject prefab = Build();
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"FeaturePanel prefab saved: {PrefabPath}", prefab);
    }

    [MenuItem("Yeyu/UI/Add Feature Panel To Scene")]
    public static void AddToScene()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            prefab = Build();

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObject = new GameObject("MainCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Add Feature Panel");
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
            Undo.RegisterCreatedObjectUndo(eventSystem, "Add Feature Panel");
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
        Undo.RegisterCreatedObjectUndo(instance, "Add Feature Panel");
        Selection.activeGameObject = instance;
    }

    private static GameObject Build()
    {
        ImportSprites();
        _regular = LoadOrCreateFont("Pretendard-Regular");
        _bold = LoadOrCreateFont("Pretendard-Bold");

        RectTransform root = CreateRect("FeaturePanel", null);
        Stretch(root, 0f, 0f, 0f, 0f);
        var panel = root.gameObject.AddComponent<FeaturePanel>();

        // 오른쪽 카드 영역이 3D 미리보기 위에서도 읽히도록 깔아 두는 어두운 그라데이션
        Image shade = CreateImage("RightShade", root, LoadSprite("UI_GradientH"), new Color(0f, 0f, 0f, 0.55f), false);
        shade.rectTransform.anchorMin = new Vector2(0.4f, 0f);
        shade.rectTransform.anchorMax = Vector2.one;
        shade.rectTransform.offsetMin = Vector2.zero;
        shade.rectTransform.offsetMax = Vector2.zero;

        var tabs = new Button[2];
        var tabLabels = new TMP_Text[2];
        BuildTabBar(root, tabs, tabLabels);

        var pages = new GameObject[2];
        var startButtons = new Button[2];
        var startGlows = new Graphic[2];
        var toggles = new Toggle[4];
        var descriptions = new TMP_Text[4];
        var borders = new Graphic[4];
        var checkMarks = new GameObject[4];

        for (int tab = 0; tab < 2; tab++)
        {
            RectTransform page = CreateRect("Page_" + TabNames[tab], root);
            Stretch(page, 0f, 0f, 0f, 0f);
            pages[tab] = page.gameObject;

            float left = -(SideMargin + CardWidth * 2f + CardGap);

            TMP_Text title = CreateText("Title", page, TabTitles[tab], _bold, 38f, Color.white, TextAlignmentOptions.Left);
            Place(title.rectTransform, new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(left, -TitleTop), new Vector2(400f, 48f));

            for (int i = 0; i < 2; i++)
            {
                int feature = tab * 2 + i;
                RectTransform card = BuildCard(page, feature, out toggles[feature], out descriptions[feature], out borders[feature], out checkMarks[feature]);
                Place(card, new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(left + i * (CardWidth + CardGap), -CardTop), new Vector2(CardWidth, CardHeight));
            }

            startButtons[tab] = BuildStartButton(page, StartLabels[tab], out startGlows[tab]);
            pages[tab].SetActive(tab == 0);
        }

        TMP_Text hint = CreateText("Hint", root, "기능을 1개 이상 선택하면 시작할 수 있어요.", _regular, 20f, HintText, TextAlignmentOptions.Right);
        Place(hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-SideMargin, 131f), new Vector2(700f, 30f));

        Button previous = BuildPillButton("PreviousButton", root, "이전", Color.white, DarkText);
        Place((RectTransform)previous.transform, Vector2.zero, Vector2.zero, new Vector2(56f, BottomMargin), new Vector2(234f, ButtonHeight));

        var so = new SerializedObject(panel);
        so.FindProperty("_config").objectReferenceValue = FindConfig();
        SetArray(so, "_tabs", tabs);
        SetArray(so, "_tabLabels", tabLabels);
        SetArray(so, "_tabPages", pages);
        SetArray(so, "_startButtons", startButtons);
        SetArray(so, "_startGlows", startGlows);
        SetArray(so, "_toggles", toggles);
        SetArray(so, "_descriptionTexts", descriptions);
        SetArray(so, "_cardBorders", borders);
        SetArray(so, "_checkMarks", checkMarks);
        so.FindProperty("_previousButton").objectReferenceValue = previous;
        so.FindProperty("_hintText").objectReferenceValue = hint;
        so.ApplyModifiedPropertiesWithoutUndo();

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        Object.DestroyImmediate(root.gameObject);
        AssetDatabase.SaveAssets();
        return prefab;
    }

    private static void BuildTabBar(RectTransform root, Button[] tabs, TMP_Text[] labels)
    {
        Image bar = CreateImage("TabBar", root, LoadSprite("UI_Circle"), TabBarFill, true);
        Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -51f), new Vector2(322f, 56f));

        for (int i = 0; i < 2; i++)
        {
            Image tab = CreateImage("Tab_" + TabNames[i], bar.transform, LoadSprite("UI_Circle"), i == 0 ? Accent : Color.clear, true);
            tab.raycastTarget = true;
            tab.rectTransform.anchorMin = new Vector2(i * 0.5f, 0f);
            tab.rectTransform.anchorMax = new Vector2((i + 1) * 0.5f, 1f);
            tab.rectTransform.offsetMin = new Vector2(3f, 3f);
            tab.rectTransform.offsetMax = new Vector2(-3f, -3f);

            tabs[i] = tab.gameObject.AddComponent<Button>();
            tabs[i].targetGraphic = tab;
            tabs[i].transition = Selectable.Transition.None;

            labels[i] = CreateText("Label", tab.transform, TabTitles[i], _bold, 26f, i == 0 ? Color.white : TabIdleText, TextAlignmentOptions.Center);
            Stretch(labels[i].rectTransform, 0f, 0f, 0f, 0f);
        }
    }

    private static RectTransform BuildCard(RectTransform page, int feature, out Toggle toggle, out TMP_Text description, out Graphic border, out GameObject checkMark)
    {
        RectTransform card = CreateRect("Card_" + FeatureNames[feature], page);

        Image background = CreateImage("Background", card, LoadSprite("UI_RoundRect"), CardFill, true);
        background.raycastTarget = true;
        Stretch(background.rectTransform, 0f, 0f, 0f, 0f);

        Image outline = CreateImage("Border", card, LoadSprite("UI_RoundRectOutline"), Accent, true);
        Stretch(outline.rectTransform, 0f, 0f, 0f, 0f);
        border = outline;

        Image icon = CreateImage("Icon", card, LoadSprite("Icon_" + FeatureNames[feature]), Color.white, false);
        Place(icon.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -18f), new Vector2(84f, 84f));

        Image box = CreateImage("CheckBox", card, LoadSprite("UI_CheckBoxOutline"), IdleBorder, false);
        Place(box.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -26f), new Vector2(38f, 38f));

        Image boxFill = CreateImage("Checked", box.transform, LoadSprite("UI_CheckBox"), Accent, false);
        Stretch(boxFill.rectTransform, 0f, 0f, 0f, 0f);
        Image check = CreateImage("Check", boxFill.transform, LoadSprite("Icon_Check"), Color.white, false);
        Stretch(check.rectTransform, 3f, 3f, 3f, 3f);
        checkMark = boxFill.gameObject;

        TMP_Text title = CreateText("Title", card, FeatureTitles[feature], _bold, 32f, Color.white, TextAlignmentOptions.Left);
        PlaceRow(title.rectTransform, 28f, 112f, 42f);

        description = CreateText("Description", card, FeatureDescriptions[feature], _regular, 22f, BodyText, TextAlignmentOptions.TopLeft);
        description.lineSpacing = 35f;
        PlaceRow(description.rectTransform, 28f, 164f, 80f);

        toggle = card.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = null;
        toggle.isOn = true;

        ColorBlock colors = toggle.colors;
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = Color.white;
        toggle.colors = colors;
        return card;
    }

    private static Button BuildStartButton(RectTransform page, string label, out Graphic glow)
    {
        Button button = BuildPillButton("StartButton", page, label, Accent, Color.white);
        var rect = (RectTransform)button.transform;
        Place(rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-SideMargin, BottomMargin), new Vector2(324f, ButtonHeight));

        Image glowImage = CreateImage("Glow", rect, LoadSprite("UI_Glow"), new Color(Accent.r, Accent.g, Accent.b, 0.55f), true);
        Stretch(glowImage.rectTransform, -40f, -40f, -40f, -40f);
        glowImage.transform.SetAsFirstSibling();
        glow = glowImage;

        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.7f);
        button.colors = colors;
        return button;
    }

    private static Button BuildPillButton(string name, Transform parent, string label, Color fill, Color textColor)
    {
        RectTransform rect = CreateRect(name, parent);

        Image background = CreateImage("Background", rect, LoadSprite("UI_Circle"), fill, true);
        background.raycastTarget = true;
        Stretch(background.rectTransform, 0f, 0f, 0f, 0f);

        TMP_Text text = CreateText("Label", rect, label, _bold, 28f, textColor, TextAlignmentOptions.Center);
        Stretch(text.rectTransform, 0f, 0f, 0f, 0f);

        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        return button;
    }

    // ---------- 에셋 ----------

    private static void ImportSprites()
    {
        AssetDatabase.Refresh();
        SetSpriteImport("UI_Circle", 63);
        SetSpriteImport("UI_RoundRect", 24);
        SetSpriteImport("UI_RoundRectOutline", 24);
        SetSpriteImport("UI_CheckBox", 12);
        SetSpriteImport("UI_CheckBoxOutline", 12);
        SetSpriteImport("UI_Glow", 95);
        SetSpriteImport("UI_GradientH", 0);
        SetSpriteImport("Icon_Check", 0);
        foreach (string feature in FeatureNames)
            SetSpriteImport("Icon_" + feature, 0);
    }

    private static void SetSpriteImport(string name, int border)
    {
        string path = IconDir + name + ".png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Sprite not found: {path}");
            return;
        }

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteBorder = new Vector4(border, border, border, border);
        settings.spritePixelsPerUnit = 100f;
        settings.alphaIsTransparency = true;
        settings.mipmapEnabled = false;
        settings.wrapMode = TextureWrapMode.Clamp;
        importer.SetTextureSettings(settings);
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(IconDir + name + ".png");
    }

    // 한글은 글자 수가 많아 Dynamic 아틀라스로 만든다 (쓰인 글자만 런타임에 채워진다)
    private static TMP_FontAsset LoadOrCreateFont(string name)
    {
        string assetPath = FontDir + name + " SDF.asset";
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (fontAsset != null)
            return fontAsset;

        var font = AssetDatabase.LoadAssetAtPath<Font>(FontDir + name + ".ttf");
        if (font == null)
        {
            Debug.LogError($"Font not found: {FontDir}{name}.ttf");
            return null;
        }

        fontAsset = TMP_FontAsset.CreateFontAsset(font, 64, 6, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        AssetDatabase.CreateAsset(fontAsset, assetPath);

        fontAsset.atlasTextures[0].name = name + " Atlas";
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
        fontAsset.material.name = name + " Atlas Material";
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        return fontAsset;
    }

    private static CarConfig FindConfig()
    {
        string[] guids = AssetDatabase.FindAssets("t:CarConfig");
        return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<CarConfig>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
    }

    // ---------- UI 조립 도우미 ----------

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rect = (RectTransform)go.transform;
        if (parent != null)
            rect.SetParent(parent, false);
        return rect;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color, bool sliced)
    {
        var image = CreateRect(name, parent).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
    {
        var label = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        label.text = text;
        return label;
    }

    // anchor 한 점에 고정. position은 anchor 기준 pivot 위치.
    private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    // 부모 위쪽에서 top만큼 내려온 가로 한 줄 (좌우 inset)
    private static void PlaceRow(RectTransform rect, float inset, float top, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.offsetMin = new Vector2(inset, -top - height);
        rect.offsetMax = new Vector2(-inset, -top);
    }

    private static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void SetArray(SerializedObject so, string propertyName, Object[] values)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}

