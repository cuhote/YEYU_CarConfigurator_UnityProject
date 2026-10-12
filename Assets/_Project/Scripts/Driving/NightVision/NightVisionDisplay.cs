using UnityEngine;

public class NightVisionDisplay : MonoBehaviour
{
    [SerializeField] private NightVisionSensor _sensor;
    [SerializeField] private Camera _thermalCamera;
    [SerializeField] private RectTransform _screen;
    [SerializeField] private RectTransform _targetBox;
    [SerializeField] private GameObject _warningIcon;
    [SerializeField] private float _pedestrianHeight =1.7f;

    private CameraSwitcher _cameraSwitcher;

    private void Awake()
    {
        _cameraSwitcher = FindAnyObjectByType<CameraSwitcher>();
    }

    private void OnEnable()
    {
        if (_sensor != null) _sensor.WarningChanged += SetWarning;
    }

    private void OnDisable()
    {
        if (_sensor != null) _sensor.WarningChanged -= SetWarning;
    }

    public void SetWarning(bool on)
    {
        if (_warningIcon != null) _warningIcon.SetActive(on);
    }

    private void Update()
    {
        if (_sensor == null || _screen == null) return;
        //1 보일 조건 :센서가 켜져있고 , 운ㅇ전석 시점 
        bool driverView = _cameraSwitcher == null ||_cameraSwitcher.CurrentView == CameraView.Driver;
        bool show = _sensor.IsActive && driverView;
        // 2 조건 대로 화면을 켜거나 끔
        _screen.gameObject.SetActive(show);
        if(!show)return;

        //3 감지한 보행자가 없으면 박스를 숨김 
        if(_sensor.Target == null)
        {
            _targetBox.gameObject.SetActive(false);
            return;
        }
        ShowTarget(_sensor.Target, _sensor.CurrentDistance, _sensor.CurrentTTC);

    }

    public void ShowTarget(Pedestrian target, float distance, float ttc)
    {
        // 보행자의 3D 위치를 열화상 카메라 화면의 0~1 좌표로 바꾼다
        Vector3 vp = _thermalCamera.WorldToViewportPoint(target.transform.position);
        if (vp.z <= 0f)                      // 카메라 뒤에 있으면 숨김
        {
            _targetBox.gameObject.SetActive(false);
            return;
        }

        float w = _screen.rect.width;
        float h = _screen.rect.height;

        // 화면 속 보행자 키(픽셀) = 실제 키 ÷ 그 거리에서 화면이 담는 높이 × 화면 높이
        float viewHeight = 2f * vp.z * Mathf.Tan(_thermalCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float boxH = Mathf.Max(_pedestrianHeight / viewHeight * h * 1.3f, 16f);
        float boxW = Mathf.Max(boxH * 0.5f, 10f);

        _targetBox.gameObject.SetActive(true);
        _targetBox.anchoredPosition = new Vector2(vp.x * w, vp.y * h);
        _targetBox.sizeDelta = new Vector2(boxW, boxH);
    }
} 



