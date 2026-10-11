using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

public class NavigationBarBuilder
{
    [MenuItem("Yeyu/UI/Create Navigation Buttons")]
    public static void CreateNavigationBar()
    {
        // 1. 이미 있으면 중단
        if (Object.FindFirstObjectByType<NavigationBar>() != null)
        {
            Debug.Log("[NavigationBarBuilder] NavigationBar가 이미 있음");
            return;
        }

        // 2. Canvas 찾기
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[NavigationBarBuilder] Canvas가 없음");
            return;
        }

        // 3. NavigationBar 오브젝트 (왼쪽 위)
        GameObject bar = new GameObject("NavigationBar", typeof(RectTransform));
        bar.transform.SetParent(canvas.transform, false);
        Undo.RegisterCreatedObjectUndo(bar, "Create NavigationBar");

        RectTransform barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0, 1);
        barRect.anchorMax = new Vector2(0, 1);
        barRect.pivot = new Vector2(0, 1);
        barRect.anchoredPosition = new Vector2(24, -24);
        barRect.sizeDelta = new Vector2(208, 96);

        // 4. 버튼 2개 (← 와 ⌂)
        Button back = MakeButton(bar.transform, "BackButton", 0, "←");
        Button home = MakeButton(bar.transform, "HomeButton", 112, "⌂");

        // 5. NavigationBar 스크립트 붙이고 버튼 연결
        NavigationBar nav = bar.AddComponent<NavigationBar>();
        SerializedObject so = new SerializedObject(nav);
        SerializedProperty p1 = so.FindProperty("_backButton");
        SerializedProperty p2 = so.FindProperty("_homeButton");
        if (p1 != null) p1.objectReferenceValue = back;
        else Debug.LogWarning("[NavigationBarBuilder] _backButton 필드를 못 찾음 (이름 확인)");
        if (p2 != null) p2.objectReferenceValue = home;
        else Debug.LogWarning("[NavigationBarBuilder] _homeButton 필드를 못 찾음 (이름 확인)");
        so.ApplyModifiedProperties();

        Debug.Log("[NavigationBarBuilder] 생성 완료");
    }

    private static Button MakeButton(Transform parent, string name, float x, string label)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, 0);
        rect.sizeDelta = new Vector2(96, 96);

        obj.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(obj.transform, false);

        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 48;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        return obj.GetComponent<Button>();
    }
}