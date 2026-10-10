using UnityEngine;

public class DrivingArea : AreaBase
{
    
    [SerializeField] private CarController _carA; // 기능 미적용 차
    [SerializeField] private CarController _carB; // 기능 적용 차
    [SerializeField] private Transform [] _nightVisionStarts;
    [SerializeField] private Transform [] _rearSteerStarts;

    public const int SceneNightVision = 0;
    public const int SceneRearSteer = 1;

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
        int scene = SceneNightVision;
        if(config != null && ! config.NightVision && config.RearWheelSteering)
        {
            scene = SceneRearSteer;
        } 
    
        if(_carA == null)
        {
            Debug.LogError("[DrivingArea] ResetArea: _carA가 비어 있음 (Inspector 연결 확인)");
        }
        else
        {
            _carA.ResetPose(GetStartPose(scene, 0));
        }

        if(_carB == null)
        {
            Debug.LogError("[DrivingArea] ResetArea: _carB가 비어 있음 (Inspector 연결 확인)");
        }
        else
        {
            _carB.ResetPose(GetStartPose(scene, 1));
        }
              // TODO 보행자 초기화 (Pedestrian.ResetPose)
        // TODO config에 따라 차 B의 기능 켜기/끄기 (NightVisionSensor.SetActive)
    }
}