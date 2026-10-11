using UnityEngine;

public class VirtualDriver : MonoBehaviour
{
    [SerializeField] private CarController _car; //이 운전자가 모는 차

    [SerializeField] private Pedestrian _pedestrian; //보행자

    [SerializeField] private float _cruiseKmh= 60f; //주행 속도

    [SerializeField] private float _headlampRange =50f; // 눈으로 보이는 거리

    [SerializeField] private float _reactionTime = 1.5f; //반응 시간

    [SerializeField] private float _brakeDeceleration = 6f; //제동 감속도


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
        _driving = false;
        _braking = false;
        _reactionTimer = 0f;
        _pedestrianDetected = false;
    }

    void Start()
    {

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