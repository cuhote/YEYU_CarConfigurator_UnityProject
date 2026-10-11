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
        }
    }

    public void Press(int dir)
    {
        if (State != RemoteState.Moving && State != RemoteState.Boardable)
            return;

        if (_car == null)
        {
            Debug.LogError("[RemoteParking] Press: _car가 null입니다");
            return;
        }

        if (_proximity != null && _proximity.IsBlocked)
        {
            Debug.LogWarning("[RemoteParking] Press: 전방/후방에 장애물이 있어 이동할 수 없습니다");
            return;
        }

        _isMoving = true;
        _stopTimer = _stopTime;
        _car.SetSpeed(dir > 0 ? _speedKmh : -_speedKmh);
    }

    public void Release()
    {
        if (_car == null)
            return;

        _stopTimer = _stopTime;
        _isMoving = false;
        _car.Brake(5f);
    }

    public void Board()
    {
        if (State != RemoteState.Boardable)
        {
            Debug.LogWarning("[RemoteParking] Board: 현재 탑승할 수 없습니다 (State: " + State + ")");
            return;
        }

        State = RemoteState.Boarded;
        Debug.Log("[RemoteParking] State: Boarded");

        if (_cameraSwitcher != null)
        {
            _cameraSwitcher.SetRightDriverView();
        }
        else
        {
            Debug.LogError("[RemoteParking] Board: CameraSwitcher가 없습니다");
        }
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
        if (!active)
        {
            ResetFeature();
        }
    }

    public void ResetFeature()
    {
        State = RemoteState.Idle;
        _stopTimer = 0f;
        _isMoving = false;

        if (_car != null)
        {
            _car.Brake(5f);
        }
    }
}
