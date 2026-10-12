using UnityEngine;
using System;

public class NightVisionSensor : MonoBehaviour,ICarFeature
{
    [SerializeField] private CarController _car;
    [SerializeField] private VirtualDriver _driver;
    [SerializeField] private Pedestrian _pedestrian;
    [SerializeField] private float _detectRange = 120f; // 감지 거리 (m)
    [SerializeField] private float _ttcWarn =4.0f; // 경고 시간 (초)
    [SerializeField] private float _ttcBrake = 2.5f; // 제동 시간 (초)
    [SerializeField] private float _brakeDeceleration = 5.0f; // 요청할 감속도(m/s²)
    [SerializeField] private float _frontOffset = 3.6f; //차 원점 (뒷바퀴 축)에서 앞범퍼까지 거리 

    private bool _active; //가능이 켜져 있는지
    private bool _warning; //경고 중인지
    private bool _brakeRequested; //제동 요청 중인지

    public string FeatureId => "nightVision";
    public Pedestrian Target {get; private set;} // 감지 대상 보행자
    public float CurrentDistance {get; private set;} // 현재 거리 (m)
    public float CurrentTTC {get; private set;} // 현재 TTC (초)
    public bool IsWarning => _warning;
    public event Action<bool> WarningChanged; // 경고 상태가 바뀌면 호출됨 (true: 경고 시작, false: 경고 종료)

    public float CalcTTC(float distance, float speedMps)
    {
        if(speedMps <= 0.1f) return float.PositiveInfinity;
        return distance / speedMps;
    }
    private void Update()
    {
        if(!_active) return;
        if(_car == null || _pedestrian == null) return;

        Vector3 toPedestrian = _pedestrian.transform.position - _car.transform.position;

        float dot = Vector3.Dot(toPedestrian, _car.transform.forward);
        dot -= _frontOffset;
        if(dot <= 0f || dot >_detectRange)
        {
            Target = null;
            CurrentTTC = float.PositiveInfinity;
            return;
        }
        Target = _pedestrian;
        CurrentDistance = dot; 
        float speedMps = _car.SpeedKmh / 3.6f; // m/s
        CurrentTTC = CalcTTC(dot, speedMps);
        if(CurrentTTC <= _ttcWarn && _warning == false)
        {
            _warning = true;
            WarningChanged?.Invoke(true);
        }
        if(CurrentTTC <= _ttcBrake && !_brakeRequested)
        {
            _brakeRequested = true;
            if(_driver != null)
            {
                _driver.RequestBrake(_brakeDeceleration);
            }
        }
    }
    public void SetActive(bool active)
    {
        _active = active;
        if (!active)ResetFeature();
    }

    public void ResetFeature()
    {
      if(_warning)
      {
        WarningChanged?.Invoke(false);
      }
        _warning = false;
        _brakeRequested = false;
        Target = null;
        CurrentTTC = float.PositiveInfinity;
      }
    
    
}