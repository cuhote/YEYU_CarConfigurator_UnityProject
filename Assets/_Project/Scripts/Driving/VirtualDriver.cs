using UnityEngine;
using System.Collections;

public class VirtualDriver : MonoBehaviour
{
    [SerializeField] private CarController _car; //이 운전자가 모는 차

    [SerializeField] private Pedestrian _pedestrian; //보행자

    [SerializeField] private float _cruiseKmh= 60f; //주행 속도

    [SerializeField] private float _headlampRange =50f; // 눈으로 보이는 거리

    [SerializeField] private float _reactionTime = 1.5f; //반응 시간

    [SerializeField] private float _brakeDeceleration = 6f; //제동 감속도

    [SerializeField] private float _frontOffset = 3.6f;

    [Header("유턴")]
    [SerializeField] private float[] _uTurnAngles = { 180f};
    [SerializeField] private float _uTurnKmh = 8f;
    [SerializeField] private float _uTurnSteer = 35f;
    [SerializeField] private float _exitDistance= 15f; //다 돌고 나서 더 가는 거리 

    [SerializeField] private Transform _uTurnPoint; // 여기까지 직진한 뒤 유턴 시작. 비워 두면 바로 유턴
    [SerializeField] private float _approachKmh = 20f; // 직진 속도
    [SerializeField] private float _slowDistance = 6f; // 이만큼 남으면 유턴 속도로 줄인다

    [SerializeField] private float _reverseKmh = 4f; // 후진 속도
    [SerializeField] private float _shiftPause = 1.2f; // 멈춘 뒤 기어를 바꾸는 시간(초)

    private float _turned; //지금 까지 돈 각도 
    private float _lastYaw; 

    private bool _driving; //주행 중인지 여부
    private bool _braking;  //제동 중인지 여부
    private float _reactionTimer; //발견한 뒤 흐른 시간 
    private bool _pedestrianDetected; //보행자 발견 여부

    public void Drive()
    {
        _driving = true;
        _car.SetSpeed(_cruiseKmh);
    }

    public void RequestBrake(float decel)
    {
        if(_braking) return;
        _braking = true;
        _car.Brake(decel);
    }

    public void ResetDriver()
    {
        StopAllCoroutines();
        _driving = false;
        _braking = false;
        _reactionTimer = 0f;
        _pedestrianDetected = false;
    }

    public void DriveUTurn()
    {
        StopAllCoroutines();
        StartCoroutine(UTurnRoutine());
    }

    private IEnumerator UTurnRoutine()
    {
        if (_uTurnPoint != null)
        {
            _car.SetSteer(0f);
            _car.SetSpeed(_approachKmh);
            while (DistanceToUTurnPoint() > _slowDistance)
            {
                yield return new WaitForFixedUpdate();
            }

            _car.SetSpeed(_uTurnKmh);
            while (DistanceToUTurnPoint() > 0f)
            {
                yield return new WaitForFixedUpdate();
            }
        }
        _turned = 0f;
        _lastYaw = _car.transform.eulerAngles.y;

        for(int i =0; i < _uTurnAngles.Length; i++)
        {
            bool reverse = i % 2 == 1;
            _car.SetSteer(reverse ? _uTurnSteer : -_uTurnSteer);
            _car.SetSpeed(reverse ? -_reverseKmh : _uTurnKmh);

            while (_turned < _uTurnAngles[i])
            {
                yield return new WaitForFixedUpdate();
                TrackTurn();
            }

            if (i < _uTurnAngles.Length - 1)   // 다음 구간이 있으면 완전히 선 뒤 방향을 바꾼다
            {
                _car.Brake(_brakeDeceleration);
                while (Mathf.Abs(_car.SpeedKmh) > 0.1f)
                {
                    yield return new WaitForFixedUpdate();
                    TrackTurn();
                }
                yield return new WaitForSeconds(_shiftPause);
            }
        }

        _car.SetSteer(0f);
        Vector3 from = _car.transform.position;
        while (Vector3.Distance(from, _car.transform.position) < _exitDistance)
        {
            yield return new WaitForFixedUpdate();
        }
        _car.Brake(3f);
    }

    private float DistanceToUTurnPoint()
    {
        Vector3 toPoint = _uTurnPoint.position - _car.transform.position;
        return Vector3.Dot(toPoint, _car.transform.forward);
    }


    private void TrackTurn()
    {
        float yaw = _car.transform.eulerAngles.y;
        _turned += Mathf.Abs(Mathf.DeltaAngle(_lastYaw, yaw));
        _lastYaw = yaw;
    }

    private void Update()
    {
        //매 프레임 보행자가 보이나 확인
        if(!_driving || _braking ) return;
        if(_car == null || _pedestrian == null) return;
        if(!_pedestrianDetected)
        {
            Vector3 toPedestrian = _pedestrian.transform.position - _car.transform.position;
            float dot = Vector3.Dot(toPedestrian, _car.transform.forward);
            dot -= _frontOffset;
            if (dot > 0f && dot <= _headlampRange)
            {
                _pedestrianDetected = true;
                _reactionTimer = 0f;
            }
        }
        if(_pedestrianDetected)
        {
            _reactionTimer += Time.deltaTime;
            if(_reactionTimer >= _reactionTime)
            {
                RequestBrake(_brakeDeceleration);
            }
        }
        
    }
}