using UnityEngine;
using System.Collections;

public class CarController : MonoBehaviour
{
    [SerializeField] private float _wheelBase =2.883f;     //축거
    [SerializeField] private float _maxSteerAngle = 35f; //앞바퀴 최대 조향각 (도)
    [SerializeField] private float _maxSteerRate = 30f; //조향각이 바뀌는 최대 속도 (도/초)
    [SerializeField] private float _accel = 3f;   // 가속도 (m/s²)


    private float _speedMps; //현재 속도 (m/s). 내부 계산용
    private float _frontSteerAngle; //현재 앞바퀴 조향각 (도)
    private float _rearSteerAngle; //현재 뒷바퀴 조향각 (도)
    private Rigidbody _rb;
    private float _targetSpeedMps;   // 목표 속도 (m/s)
    private float _decel = 3f;          // 지금 적용 중인 감속도 (m/s²)

    public float SpeedKmh => _speedMps * 3.6f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    { 
        float dt = Time.fixedDeltaTime;

        // 속도: 목표 속도를 향해 가감속
        float rate = (Mathf.Abs(_targetSpeedMps) > Mathf.Abs(_speedMps)) ? _accel : _decel; // 목표가 지금보다 빠르면 가속도 , 느리면 감속도 (후진도 가능) 
        _speedMps = Mathf.MoveTowards(_speedMps, _targetSpeedMps, rate * dt);

        //직진 
        float distance = _speedMps * dt; 

        Vector3 newPosition= _rb.position + transform.forward *distance;
        _rb.MovePosition(newPosition);

       //회전 
        float front =Mathf.Tan(_frontSteerAngle*Mathf.Deg2Rad); //도를 라디안
        float rear = Mathf.Tan(_rearSteerAngle*Mathf.Deg2Rad);
        float yawRate = _speedMps / _wheelBase * (front - rear);   // 라디안/초
        float yawDegrees = yawRate * Mathf.Rad2Deg * dt;           // 라디안을 도 , 이번 프레임에 돌 각도(도)

        Quaternion newRotation = _rb.rotation * Quaternion.Euler(0f, yawDegrees, 0f); // y축 기준으로 회전
        _rb.MoveRotation(newRotation);

        
    }
    // 속도를 순간적으로 바꾸기 (서서히 가속 )
    public void SetSpeed(float kmh)
    {
        _targetSpeedMps = kmh / 3.6f;
    }

    
    public void SetSteer(float degrees)
    {
        _frontSteerAngle = Mathf.Clamp(degrees, -_maxSteerAngle, _maxSteerAngle);
    }

    public void SetRearSteer(float degrees)
    {
         _rearSteerAngle = degrees;
    }
    // 정해진 감속도로 멈추기
    public void Brake(float decel)
    {
        _targetSpeedMps =0f;
        _decel = decel;
    }

    

    public IEnumerator Follow(Vector3[] points, float kmh, bool reverse)
    {
        const float lookAhead = 2.0f;    // 이 거리보다 가까운 점은 지나친 것으로 본다
        const float arriveDist = 0.3f;   // 마지막 점에 이만큼 가까워지면 도착

        SetSpeed(reverse ? -kmh : kmh);

        int index = 0;
        while (index < points.Length)
        {
            Vector3 toTarget = points[index] - _rb.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            bool isLast = (index == points.Length - 1);
            if (dist < (isLast ? arriveDist : lookAhead))
            {
                index++;
                continue;
            }

            // 후진일 때는 차의 뒤쪽이 진행 방향
            Vector3 heading = reverse ? -transform.forward : transform.forward;
            float angle = Vector3.SignedAngle(heading, toTarget, Vector3.up);
            SetSteer(reverse ? -angle : angle);

            yield return new WaitForFixedUpdate();
    }

    SetSteer(0f);
    Brake(_decel);
    }

    //차를 지정한 위치와 방향으로 되돌리고 멈춘 상태로 만듬 

    public void ResetPose(Pose pose)
    {
        _rb.position = pose.position;
        _rb.rotation = pose.rotation;
        transform.SetPositionAndRotation(pose.position, pose.rotation);

        _speedMps = 0f;
        _targetSpeedMps = 0f;
        _frontSteerAngle = 0f;
        _rearSteerAngle = 0f;
    }

}
