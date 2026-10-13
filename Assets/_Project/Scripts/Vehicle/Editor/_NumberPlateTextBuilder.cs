using TMPro;
using UnityEditor;
using UnityEngine;

// Car 프리팹의 앞뒤 번호판 위에 3D 글자를 올린다.
// 다시 실행하면 이전 글자를 고쳐 쓴다 (글자나 크기를 바꾸려면 아래 상수를 고치고 다시 실행)
public static class NumberPlateTextBuilder
{
    private const string PrefabPath = "Assets/_Project/Prefabs/Vehicle/Car.prefab";
    private const string FontPath = "Assets/_Project/UI/Fonts/Pretendard-Bold SDF.asset";

    private const string PlateText = "YEYU_0204";
    private const string TextObjectName = "PlateText";
    private const float SurfaceGap = 0.003f;   // 판 표면에서 띄우는 거리 (m). 붙이면 판과 겹쳐 깜빡인다
    private const float WidthRatio = 0.95f;    // 판 너비 중 글자 영역이 쓰는 비율
    private const float HeightRatio = 0.85f;   // 판 높이 중 글자 영역이 쓰는 비율
    private const float FontSize = 0.7f;       // 글자 크기. 글자 수를 바꾸면 판에 들어오는지 보고 맞춘다

    private static readonly Color TextColor = Color.black;

    // 흰 판(_02)에 글자를 올린다. _01은 판을 두르는 검은 틀이다
    private static readonly string[] PlateNames =
    {
        "Lab_Body_F_NumberPlate_02_M",
        "Lab_Body_Rear_NumberPlate_02_M",
    };

    [MenuItem("Yeyu/Vehicle/Add Number Plate Text")]
    public static void AddNumberPlateText()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
            Debug.LogWarning($"[NumberPlateTextBuilder] 글꼴을 찾지 못해 기본 글꼴을 씀 ({FontPath})");

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        int made = 0;

        foreach (string plateName in PlateNames)
        {
            Transform plate = FindChild(root.transform, plateName);
            if (plate == null)
            {
                Debug.LogError($"[NumberPlateTextBuilder] {plateName}을 찾을 수 없음");
                continue;
            }

            MeshFilter filter = plate.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                Debug.LogError($"[NumberPlateTextBuilder] {plateName}에 메시가 없음");
                continue;
            }

            // 차 기준 좌표에서 판이 차지하는 상자. 차의 앞은 +Z
            Bounds bounds = BoundsInRoot(root.transform, plate, filter.sharedMesh.bounds);
            bool front = plateName.Contains("_F_");

            // 이전에 만든 글자가 있으면 그대로 고쳐 쓴다.
            // 지우고 새로 만들면 TextMeshPro가 지워진 글자를 계속 찾아 MissingReferenceException이 난다
            Transform existing = plate.Find(TextObjectName);
            GameObject go = existing != null ? existing.gameObject : new GameObject(TextObjectName);
            go.layer = plate.gameObject.layer;
            TextMeshPro text = go.GetComponent<TextMeshPro>();
            if (text == null) text = go.AddComponent<TextMeshPro>();

            // 판의 바깥쪽 면 바로 앞에 세운다. 3D 글자는 -Z 쪽에서 읽히므로 앞 번호판은 뒤로 돌린다
            Transform t = go.transform;
            t.SetParent(root.transform, false);
            t.localScale = Vector3.one;
            float z = front ? bounds.max.z + SurfaceGap : bounds.min.z - SurfaceGap;
            t.localPosition = new Vector3(bounds.center.x, bounds.center.y, z);
            t.localRotation = front ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            t.SetParent(plate, true);

            text.rectTransform.sizeDelta = new Vector2(bounds.size.x * WidthRatio, bounds.size.y * HeightRatio);
            if (font != null) text.font = font;
            text.text = PlateText;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            // 글자 크기는 고정한다. 자동 맞춤을 쓰면 차를 놓은 곳마다(씬, 영역 프리팹)
            // 계산된 크기가 오버라이드로 쌓인다
            text.enableAutoSizing = false;
            text.fontSize = FontSize;

            // TextMeshPro가 켜질 때 스스로 고쳐 쓰는 값도 미리 넣어, 오버라이드가 생기지 않게 한다
            SerializedObject serialized = new SerializedObject(text);
            serialized.FindProperty("m_fontSizeBase").floatValue = FontSize;
            serialized.FindProperty("m_fontColor32").colorValue = TextColor;
            serialized.FindProperty("m_TextStyleHashCode").intValue = TMP_Style.NormalStyle.hashCode;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"[NumberPlateTextBuilder] {plateName}: 판 크기 {bounds.size.x:F3} x {bounds.size.y:F3} m, 두께 {bounds.size.z:F3} m");
            made++;
        }

        if (made > 0)
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);

        Debug.Log($"[NumberPlateTextBuilder] 번호판 글자 {made}개를 Car 프리팹에 넣음");
    }

    private static Transform FindChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name) return child;
        }
        return null;
    }

    // 메시 상자의 꼭짓점 8개를 차 기준 좌표로 옮겨 다시 상자로 묶는다
    private static Bounds BoundsInRoot(Transform root, Transform plate, Bounds meshBounds)
    {
        Bounds result = new Bounds();
        bool first = true;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = meshBounds.center + Vector3.Scale(meshBounds.extents,
                new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
            Vector3 point = root.InverseTransformPoint(plate.TransformPoint(corner));
            if (first)
            {
                result = new Bounds(point, Vector3.zero);
                first = false;
            }
            else
            {
                result.Encapsulate(point);
            }
        }
        return result;
    }
}
