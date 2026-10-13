using UnityEngine;


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

    [SerializeField] private DrivingScenario _scenario;
   

    public const int SceneNightVision = 0;
    public const int SceneRearSteer = 1;

    private int _scene;



    private CarConfig _config;


    public override void OnEnter()
    {
        base.OnEnter();
        if (_scenario != null) _scenario.StartFirstScene(_config);
    }
    public override void OnExit()
    {
        if (_scenario != null) _scenario.StopScenario();
        base.OnExit();
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

        _scene = (!nightVision && rearSteer) ? SceneRearSteer : SceneNightVision;

        SetupScene();
    }
    public void SetScene(int scene)
    {
        _scene = scene;
        SetupScene();
        ApplyLighting();
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
            _drawer.ShowTurn(!night);
        }

    }
           
}