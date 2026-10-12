using UnityEngine;

public class RemoteParking : MonoBehaviour, ICarFeature
{
    public enum RemoteState { Idle, Moving, Boardable, Boarded }

    [SerializeField] private CarController _car;
    [SerializeField] private ProximityStop _proximity;
    [SerializeField] private DoorSwingDrawer _doorDrawer;
    [SerializeField] private float _speedKmh = 3f;
    [SerializeField] private float _stopTime = 0.2f;

    public RemoteState State { get; private set; } = RemoteState.Idle;
    public string FeatureId => "remoteParking";

    private CameraSwitcher _cameraSwitcher;
    private float _stopTimer = 0f;
    private bool _isMoving = false;

    private void Start()
    {
        _cameraSwitcher = FindObjectOfType<CameraSwitcher>();
        if (_cameraSwitcher == null)
        {
            Debug.LogWarning("[RemoteParking] CameraSwitcher를 찾을 수 없습니다");
        }

        ValidateComponents();
    }

    private void ValidateComponents()
    {
        if (_car == null)
            Debug.LogError("[RemoteParking] _car가 null입니다");
        if (_proximity == null)
            Debug.LogError("[RemoteParking] _proximity가 null입니다");
        if (_doorDrawer == null)
            Debug.LogError("[RemoteParking] _doorDrawer가 null입니다");
    }

    private void Update()
    {
        if (State == RemoteState.Idle || State == RemoteState.Boarded)
            return;

        // 정지 타이머 업데이트
        if (_stopTimer > 0)
        {
            _stopTimer -= Time.deltaTime;
        }
        else if (_isMoving)
        {
            _isMoving = false;
            if (_car != null)
                _car.SetSpeed(0f);
        }

        // Boardable 체크
        if (State == RemoteState.Moving && _doorDrawer != null && _doorDrawer.CanOpen())
        {
            State = RemoteState.Boardable;
            Debug.Log("[RemoteParking] State: Boardable");

            // Boardable이 되는 순간 자동으로 Brake
            _isMoving = false;
            if (_car != null)
            {
                _car.Brake(5f);
                Debug.Log("[RemoteParking] Boardable 진입: 자동 Brake 적용");
            }
        }
    }

    public void Press(int dir)
    {
        Debug.Log($"[RemoteParking] Press({dir}) 호출됨, 현재 State: {State}");

        if (State == RemoteState.Boarded)
        {
            Debug.Log("[RemoteParking] Press 취소: Boarded 상태에서는 조작 불가");
            return;
        }

        if (_car == null)
        {
            Debug.LogError("[RemoteParking] Press: _car가 null입니다");
            return;
        }

        if (_proximity != null && _proximity.IsBlocked)
        {
            Debug.Log("[RemoteParking] Press 취소: 전방/후방에 장애물이 있습니다 (IsBlocked=true)");
            return;
        }

        // Idle → Moving 또는 Boardable → Moving
        if (State == RemoteState.Idle || State == RemoteState.Boardable)
        {
            State = RemoteState.Moving;
            Debug.Log($"[RemoteParking] State: Idle/Boardable → Moving");
        }

        Debug.Log($"[RemoteParking] Press: {(dir > 0 ? "전진" : "후진")} {_speedKmh}km/h 시작");
        _isMoving = true;
        _stopTimer = _stopTime;
        _car.SetSpeed(dir > 0 ? _speedKmh : -_speedKmh);
    }

    public void Release()
    {
        Debug.Log($"[RemoteParking] Release() 호출됨, 현재 State: {State}");

        if (_car == null)
        {
            Debug.LogError("[RemoteParking] Release: _car가 null입니다");
            return;
        }

        Debug.Log("[RemoteParking] Release: 정지 신호 (5m/s² 감속)");
        _stopTimer = _stopTime;
        _isMoving = false;
        _car.Brake(5f);

        // Moving → Idle, Boardable 유지
        if (State == RemoteState.Moving)
        {
            State = RemoteState.Idle;
            Debug.Log("[RemoteParking] State: Moving → Idle");
        }
        else if (State == RemoteState.Boardable)
        {
            Debug.Log("[RemoteParking] State: Boardable 유지");
        }
    }

    public void Board()
    {
        Debug.Log("[RemoteParking] Board() 호출됨");

        if (State != RemoteState.Boardable)
        {
            Debug.Log($"[RemoteParking] Board 취소: 상태가 {State}임 (Boardable 필요)");
            return;
        }

        State = RemoteState.Boarded;
        Debug.Log("[RemoteParking] State: Boarded");

        if (_cameraSwitcher != null)
        {
            Debug.Log("[RemoteParking] Board: SetRightDriverView() 호출");
            _cameraSwitcher.SetRightDriverView();
        }
        else
        {
            Debug.LogError("[RemoteParking] Board: CameraSwitcher가 없습니다");
        }
    }

    public void SetActive(bool active)
    {
        Debug.Log($"[RemoteParking] SetActive({active})");
        gameObject.SetActive(active);
        if (!active)
        {
            ResetFeature();
        }
    }

    public void ResetFeature()
    {
        Debug.Log("[RemoteParking] ResetFeature() 호출됨");
        State = RemoteState.Idle;
        _stopTimer = 0f;
        _isMoving = false;

        if (_car != null)
        {
            _car.Brake(5f);
            Debug.Log("[RemoteParking] ResetFeature: 차량 정지");
        }
    }
}
