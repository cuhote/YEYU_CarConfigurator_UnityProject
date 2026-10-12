using UnityEngine;

public class RearWheelSteering : MonoBehaviour, ICarFeature
{
    [SerializeField] private CarController _car;
    [SerializeField] private float _counterAngle = 10f; //역위상 최대 각도(도) 
    [SerializeField] private float _counterBelowKmh = 30f; //이 속도 미만에서 역위상 
    [SerializeField] private float _sameAngle = 2f; //동위상 최대 각도(도)
    [SerializeField] private float _sameAboveKmh = 60f; // 이 속도 이상에서 동위상
    [SerializeField] private float _maxFrontAngle= 35f; //앞바퀴 최대 조향각 (도)


    private bool _active; //기능 활성화 여부
    public string FeatureId => "rearWheelSteering";
    public float GetRearAngle(float frontAngle, float speedKmh)
    {
        speedKmh = Mathf.Abs(speedKmh);
        float ratio = Mathf.Clamp01(Mathf.Abs(frontAngle) / _maxFrontAngle);
        float sign = Mathf.Sign(frontAngle);
        if(speedKmh < _counterBelowKmh)
        {
            return -sign * ratio * _counterAngle;
        }
        else if(speedKmh >= _sameAboveKmh)
        {
            return sign * ratio * _sameAngle;
        }
        else
        {
            return 0f;
        }
    }

    private void Update()
    {
        if (!_active || _car == null) return;
        _car.SetRearSteer(GetRearAngle(_car.SteerAngle, _car.SpeedKmh));
    }
    public void SetActive(bool active)
    {
        _active = active;
        if(!_active)
        {
            ResetFeature();
        }
    }
    public void ResetFeature()
    {
        if(_car !=null)
        {
            _car.SetRearSteer(0f);//뒷바퀴를 똑바로 되돌림
        } 
    }  
}