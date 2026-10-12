using UnityEngine;
using System.Collections;

public class DrivingArea : AreaBase
{
    
    [SerializeField] private CarController _carA; // 기능 미적용 차
    [SerializeField] private CarController _carB; // 기능 적용 차
    [SerializeField] private Transform [] _nightVisionStarts;
    [SerializeField] private Transform [] _rearSteerStarts;

    [SerializeField] private NightVisionSensor _nightVisionSensor; //야간 시야 센서
    [SerializeField] private RearWheelSteering _rearSteer; //후륜 조향 센서

    [SerializeField] private VirtualDriver _driverA;
    [SerializeField] private VirtualDriver _driverB;
    [SerializeField] private DrivingDrawer _drawer;

    [Header("유턴 장면 조명")]
    [SerializeField] private Light _areaLight; // 영역 안의 Directional Light
    [SerializeField] private float _nightLightIntensity = 0.07f;
    [SerializeField] private float _turnLightIntensity = 0.8f;
    [SerializeField] private Color _turnAmbient = new Color(0.45f, 0.47f, 0.52f);

   

    public const int SceneNightVision = 0;
    public const int SceneRearSteer = 1;

    private int _scene;

    [SerializeField] private float _sceneGap = 3f;

    private CarConfig _config;
    private bool _playBoth; // 두 기능을 다 골랐는지 

    public override void OnEnter()
    {
        base.OnEnter();
        StopAllCoroutines();
        StartScene();
        if (_playBoth) StartCoroutine(PlayNextScene());
    }
    public override void OnExit()
    {
        StopAllCoroutines();
        base.OnExit();
    }

    private void StartScene()
    {
        if(_scene == SceneNightVision)
        {
            if(_driverA != null) _driverA.Drive();
            if(_driverB != null) _driverB.Drive();
        }
        else if(_scene == SceneRearSteer)
        {
            if(_driverA != null) _driverA.DriveUTurn();
            if(_driverB != null) _driverB.DriveUTurn();
        }
    }

    private IEnumerator PlayNextScene()
    {
        yield return new WaitForSeconds(2f); // 차가 출발할 시간을 준다

        while (Mathf.Abs(_carA.SpeedKmh) > 0.1f || Mathf.Abs(_carB.SpeedKmh) > 0.1f)
        {
            yield return null; // 두 차가 모두 멈출 때까지
        }
        yield return new WaitForSeconds(_sceneGap);

        _scene = SceneRearSteer;
        SetupScene();
        ApplyLighting();
        StartScene();
    }
    protected override void ApplyLighting()
    {
        base.ApplyLighting(); // 밤 조명을 먼저 건다

        bool turn = _scene == SceneRearSteer;
        if (turn) RenderSettings.ambientLight = _turnAmbient;
        if (_areaLight != null)
        {
            _areaLight.intensity = turn ? _turnLightIntensity : _nightLightIntensity;
        }
    }
    public Pose GetStartPose(int scene, int car)
    {
        Transform[] starts;
        if (scene == SceneNightVision) starts = _nightVisionStarts; 
        else if (scene == SceneRearSteer) starts = _rearSteerStarts;
        else
        {
            Debug.LogError($"[DrivingArea] GetStartPose: scene은 0 또는 1이어야 함 (받은 값: {scene})");
            return new Pose(Vector3.zero, Quaternion.identity);
        }
        if(starts == null)
        {
            Debug.LogError($"[DrivingArea] GetStartPose: starts가 비어 있음 (Inspector 연결 확인)");
            return new Pose(Vector3.zero, Quaternion.identity);
        }
        if (car < 0 || car >= starts.Length || starts[car] == null)
        {
            Debug.LogError($"[DrivingArea] GetStartPose: scene {scene}의 car {car} 시작 위치가 없음 (범위 또는 Inspector 연결 확인)");
            return new Pose(Vector3.zero, Quaternion.identity);
        }
        Transform start = starts[car];
        return new Pose(start.position, start.rotation);
    }
    public override void ResetArea(CarConfig config)
    {
         _config = config;
        bool nightVision = config != null && config.NightVision;
        bool rearSteer = config != null && config.RearWheelSteering;

        _playBoth = nightVision && rearSteer;
        _scene = (!nightVision && rearSteer) ? SceneRearSteer : SceneNightVision;

        SetupScene();
    }

    private void SetupScene()
    {
        bool night = _scene == SceneNightVision;

        if(_carA == null)
        {
            Debug.LogError("[DrivingArea] ResetArea: _carA가 비어 있음 (Inspector 연결 확인)");
        }
        else
        {
            _carA.ResetPose(GetStartPose(_scene, 0));
        }

        if(_carB == null)
        {
            Debug.LogError("[DrivingArea] ResetArea: _carB가 비어 있음 (Inspector 연결 확인)");
        }
        else
        {
            _carB.ResetPose(GetStartPose(_scene, 1));
        }

        if(_driverA != null) _driverA.ResetDriver();
        if(_driverB != null) _driverB.ResetDriver();

        if(_nightVisionSensor != null)
        {
            _nightVisionSensor.ResetFeature();
            _nightVisionSensor.SetActive(night&& _config != null &&_config.NightVision);
        }
        if(_rearSteer !=null)
        {
            _rearSteer.ResetFeature();
            _rearSteer.SetActive(_config != null && _config.RearWheelSteering);
        }
        
        if (_drawer != null)
        {
            _drawer.ShowCones(night);
            _drawer.ShowThermalCone(night && _config != null && _config.NightVision);
        }

    }
           
}