using UnityEngine;

public enum CameraView { Driver, BirdEye, Fixed } 

public class CameraSwitcher : MonoBehaviour
{
    private Camera _left;
    private Camera _right;

    private CameraAnchors _leftAnchors;
    private CameraAnchors _rightAnchors;

    // 카메라마다 처음 봤을 때의 nearClipPlane (버드뷰에서 바꾼 값을 되돌릴 때 쓴다)
    private readonly System.Collections.Generic.Dictionary<Camera, float> _defaultNearClips = new System.Collections.Generic.Dictionary<Camera, float>();

    public CameraView CurrentView { get;private set; } = CameraView.Driver;

    // 영역에 들어올 때 카메라 2대를 받아서 보관 = 주차에 카메라2개, 주행에 카메라2대 등이 있음
    // AreaBase.OnEnter() 뒤에서 Bind(주행왼카메라,주행오카메라) 하면 주행 카메라들을 다뤄
    // 영역 전환 할 때마다 호출됨
    
    public void SwitchToDriver(){Switch(CameraView.Driver);}
    public void SwitchToBirdView(){Switch(CameraView.BirdEye);}
    
    public void Bind(Camera left, Camera right)
    {
        // 1. 이전 영역의 상태 지우기
        CurrentView = CameraView.Driver;
        _left = null;
        _right = null;
        _leftAnchors = null;
        _rightAnchors = null;

        // 2. 카메라가 비어 있으면 로그 남기고 종료
        if (left == null || right == null)
        {
            Debug.LogError("[CameraSwitcher] Bind: 카메라가 비어 있음");
            return;
        }

        CameraAnchors leftAnchors = left.GetComponent<CameraAnchors>();
        CameraAnchors rightAnchors = right.GetComponent<CameraAnchors>();

        if (leftAnchors == null || rightAnchors == null)
        {
            Debug.LogError($"[CameraSwitcher] Bind: CameraAnchors가 없음 (left: {left.name}, right: {right.name})");
            return;
        }

        // 4. 둘 다 정상일 때만 저장
        _left = left;
        _right = right;
        _leftAnchors = leftAnchors;
        _rightAnchors = rightAnchors;

        Switch(CameraView.Driver);
    }

    //(운전석 ↔ 버드뷰) 양쪽 카메라를 같은 시점으로 동시에 전환 
    public void Switch(CameraView view)
    {
        
        if(_left ==null || _right == null)
        {
            Debug.LogError("[CameraSwitcher] Switch: 카메라가 연결되지 않음 (Bind 실패 또는 미호출)");
            return;
        }
        if (CurrentView == CameraView.Fixed) return;

        ApplyView(_left,_leftAnchors,view);
        ApplyView(_right, _rightAnchors, view);

        CurrentView = view;
    }

    //(고정/해제) 스마트키 장면 진입 시 고정 시점으로 강제 전환 / 해제
    public void SetFixedView(bool isOn)
    {
        if (_left == null || _right == null)
        {
            Debug.LogError("[CameraSwitcher] SetFixedView: 카메라가 연결되지 않음 (Bind 실패 또는 미호출)");
            return;
        }

        CameraView target = isOn ? CameraView.Fixed : CameraView.Driver;

        CurrentView = target;
        ApplyView(_left, _leftAnchors, target);
        ApplyView(_right, _rightAnchors, target);

    }

    //[탑승] 누르면 오른쪽만 운전석 시점으로
    public void SetRightDriverView()
    {
        if(_right == null)
        {
            Debug.LogError("[CameraSwitcher] SetRightDriverView: Bind 실패 또는 미호출");
            return;
        }
        ApplyView(_right, _rightAnchors, CameraView.Driver);
    }

    // (지원) 카메라를 어떤 시점 앵커로 옮기는 작업
    private void ApplyView(Camera cam, CameraAnchors anchors, CameraView view)
    {
        Transform target = null;
        int layer = LayerMask.NameToLayer("Visualization");


        switch (view)
        {
            case CameraView.Driver:
                target = anchors.Driver;
                break;
            case CameraView.BirdEye:
                target = anchors.BirdEye;
                break;
            case CameraView.Fixed:
                target = anchors.Fixed;
                break;
        }

        if (target == null)
        {
            Debug.LogError($"[CameraSwitcher] ApplyView: {cam.name}의 {view} 앵커가 비어 있음");
            return;
        }

        cam.transform.SetParent(target, false);
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;

        // 버드뷰에서는 가까운 쪽 절단면을 내려서 천장 쪽 부품을 잘라 낸다. 다른 시점에서는 원래 값으로 되돌린다
        if (!_defaultNearClips.TryGetValue(cam, out float defaultNear))
        {
            defaultNear = cam.nearClipPlane;
            _defaultNearClips[cam] = defaultNear;
        }
        float nearClip = defaultNear;
        if (view == CameraView.BirdEye && anchors.BirdEyeCutHeight > 0f)
            nearClip = Mathf.Max(defaultNear, cam.transform.position.y - anchors.BirdEyeCutHeight);
        cam.nearClipPlane = nearClip;

        //Visualization 레이어 처리
        if (layer == -1)
        {
            Debug.LogError("[CameraSwitcher] ApplyView:'Visualization' 레이어가 없음 (Layers 메뉴 확인)");
            return;
        }
        if (view != CameraView.Driver)
        {
            cam.cullingMask |= (1 << layer); //켜기
        }
        else
        {
            cam.cullingMask &= ~(1 << layer); //끄기
        }
    }
}