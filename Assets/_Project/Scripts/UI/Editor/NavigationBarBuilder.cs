using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
public class NavigationBarBuilder
{
    [MenuItem("Yeyu/UI/Create Navigation Buttons")]
    public static void CreateNavigationBar()
    {
        // 이미 존재하는지 확인
        NavigationBar existingBar = FindObjectOfType<NavigationBar>();
        if (existingBar != null)
        {
            EditorUtility.DisplayDialog("NavigationBar 생성",
                "NavigationBar가 이미 씬에 존재합니다. 중복 생성을 방지하였습니다.",
                "확인");
            Debug.Log("[NavigationBarBuilder] NavigationBar가 이미 존재합니다. 중복 생성을 방지하였습니다.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        // Canvas 찾기
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            // MainCanvas 찾기
            foreach (Canvas c in FindObjectsOfType<Canvas>())
            {
                if (c.gameObject.name == "MainCanvas")
                {
                    canvas = c;
                    break;
                }
            }
        }

        if (canvas == null)
        {
            EditorUtility.DisplayDialog("에러", "씬에 Canvas 또는 MainCanvas가 없습니다.", "확인");
            Debug.LogError("[NavigationBarBuilder] Canvas를 찾을 수 없습니다.");
            return;
        }

        // NavigationBar 오브젝트 생성
        GameObject navBarObj = new GameObject("NavigationBar");
        navBarObj.transform.SetParent(canvas.transform, false);
        Undo.RegisterCreatedObjectUndo(navBarObj, "Create NavigationBar");

        // RectTransform 설정 (좌상단 앵커, 여백 24px)
        RectTransform navBarRect = navBarObj.AddComponent<RectTransform>();
        navBarRect.anchorMin = Vector2.zero;
        navBarRect.anchorMax = Vector2.zero;
        navBarRect.pivot = Vector2.zero;
        navBarRect.anchoredPosition = new Vector2(24, -24);
        navBarRect.sizeDelta = new Vector2(216, 96); // (96 + 16 + 96)px 폭, 96px 높이

        // NavigationBar 컴포넌트 추가
        NavigationBar navBar = navBarObj.AddComponent<NavigationBar>();

        // 뒤로가기 버튼 생성
        GameObject backBtnObj = CreateButton(navBarObj.transform, "BackButton",
            0, "←", false);
        Button backButton = backBtnObj.GetComponent<Button>();

        // 홈 버튼 생성
        GameObject homeBtnObj = CreateButton(navBarObj.transform, "HomeButton",
            112, "🏠", true);
        Button homeButton = homeBtnObj.GetComponent<Button>();

        // NavigationBar에 버튼 자동 연결
        Undo.RecordObject(navBar, "Connect buttons to NavigationBar");
        navBar._backButton = backButton;
        navBar._homeButton = homeButton;
        EditorUtility.SetDirty(navBar);

        // UIManager에 NavigationBar 자동 연결
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            SerializedObject so = new SerializedObject(uiManager);
            SerializedProperty navBarProp = so.FindProperty("_navigationBar");
            if (navBarProp != null)
            {
                Undo.RecordObject(uiManager, "Connect NavigationBar to UIManager");
                navBarProp.objectReferenceValue = navBar;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(uiManager);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("[NavigationBarBuilder] NavigationBar가 생성되었습니다.");
    }

    private static GameObject CreateButton(Transform parent, string name, float xPos, string buttonText, bool isHomeButton)
    {
        // 버튼 오브젝트 생성
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        RectTransform btnRect = btnObj.AddComponent<RectTransform>();
        btnRect.anchorMin = Vector2.zero;
        btnRect.anchorMax = Vector2.zero;
        btnRect.pivot = Vector2.zero;
        btnRect.anchoredPosition = new Vector2(xPos, 0);
        btnRect.sizeDelta = new Vector2(96, 96);

        // 배경 이미지 (반투명 어두운 둥근 사각형)
        Image bgImage = btnObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.5f); // 반투명 검은색

        // 둥근 모서리 (Image.raycastTarget는 유지)
        bgImage.raycastTarget = true;

        // 버튼 컴포넌트
        Button button = btnObj.AddComponent<Button>();
        button.targetGraphic = bgImage;

        // 텍스트 오브젝트 생성
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();

        // 이모지 처리
        if (isHomeButton)
        {
            // 🏠 이모지 지원 확인
            TMP_FontAsset currentFont = textComponent.font;
            if (!SupportsEmoji(currentFont, "🏠"))
            {
                // 이모지 미지원 시 대체문자 사용
                textComponent.text = "⌂"; // U+2302
                Debug.Log("[NavigationBarBuilder] 홈 버튼: TMP 기본 폰트가 🏠 이모지를 지원하지 않습니다. ⌂(U+2302)로 대체하였습니다.");
            }
            else
            {
                textComponent.text = "🏠";
            }
        }
        else
        {
            textComponent.text = buttonText;
        }

        textComponent.color = Color.white;
        textComponent.fontSize = 48;
        textComponent.alignment = TextAlignmentOptions.Center;

        // ColorTransition 설정 (클릭 시 시각적 피드백)
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0, 0, 0, 0.5f);
        colors.highlightedColor = new Color(0, 0, 0, 0.7f);
        colors.pressedColor = new Color(0, 0, 0, 0.3f);
        button.colors = colors;

        return btnObj;
    }

    private static bool SupportsEmoji(TMP_FontAsset font, string emoji)
    {
        if (font == null)
            return false;

        // TMP 폰트가 이모지를 지원하는지 간단히 체크
        // Liberation Sans는 대부분의 이모지를 지원하지 않음
        if (font.name.Contains("Liberation") || font.name.Contains("Arial"))
        {
            return false;
        }

        return true;
    }
}
#endif
