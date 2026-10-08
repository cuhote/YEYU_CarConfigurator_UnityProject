using UnityEditor;
using UnityEngine;


// 씬 뷰에서 지금 보고 있는 시점을 카메라에 한 번 복사한다 (게임 뷰가 씬 뷰와 같아진다).
// 메뉴: Yeyu > Camera > Align Camera To Scene View (Ctrl+Alt+Shift+F)
//       Yeyu > Camera > Align Selected To Scene View (Ctrl+Alt+Shift+G)
public static class SceneViewCameraAligner
{
    [MenuItem("Yeyu/Camera/Align Camera To Scene View %&#f")]
    public static void AlignCameraToSceneView()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            Debug.LogWarning("씬 뷰가 열려 있지 않습니다.");
            return;
        }

        Camera target = FindTargetCamera();
        if (target == null)
        {
            Debug.LogWarning("맞출 카메라가 없습니다. 카메라를 선택하거나 MainCamera 태그를 붙이세요.");
            return;
        }

        Transform view = sceneView.camera.transform;
        Undo.RecordObject(target.transform, "Align Camera To Scene View");
        target.transform.SetPositionAndRotation(view.position, view.rotation);
        Debug.Log($"'{target.name}' 카메라를 씬 뷰 시점에 맞췄습니다.", target);
    }

    // 선택한 오브젝트(카메라 앵커 등)를 씬 뷰 시점과 같은 위치·회전으로 옮긴다.
    // 부모가 있어도 월드 기준으로 맞추므로, 차량 자식 앵커는 로컬 값이 자동으로 계산된다.
    [MenuItem("Yeyu/Camera/Align Selected To Scene View %&#g")]
    public static void AlignSelectedToSceneView()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            Debug.LogWarning("씬 뷰가 열려 있지 않습니다.");
            return;
        }

        Transform target = Selection.activeTransform;
        if (target == null)
        {
            Debug.LogWarning("Hierarchy에서 맞출 오브젝트를 먼저 선택하세요.");
            return;
        }

        Transform view = sceneView.camera.transform;
        Undo.RecordObject(target, "Align Selected To Scene View");
        target.SetPositionAndRotation(view.position, view.rotation);
        Debug.Log($"'{target.name}'을(를) 씬 뷰 시점에 맞췄습니다.", target);
    }

    // 선택한 오브젝트에 카메라가 있으면 그 카메라, 없으면 MainCamera 태그가 붙은 카메라.
    private static Camera FindTargetCamera()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected != null && selected.TryGetComponent(out Camera selectedCamera))
            return selectedCamera;

        return Camera.main;
    }
}
