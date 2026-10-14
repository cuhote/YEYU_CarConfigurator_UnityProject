using System.Collections;
using UnityEngine;

public class RemoteParking : MonoBehaviour, ICarFeature
{
    public enum RemoteState { Idle, Moving, Boardable, Boarded }

    [SerializeField] private CarController _car;
    [SerializeField] private ProximityStop _proximity;
    [SerializeField] private DoorSwingDrawer _doorDrawer;
    [SerializeField] private float _speedKmh = 3f;
    [SerializeField] private float _stopTime = 0.2f;
    [SerializeField] private float _obstacleDecel = 50f;   // 장애물을 만났을 때 세우는 감속도 (m/s²). 거의 그 자리에 선다
    // 처음 주차된 자리보다 뒤로는 가지 않는다. 뒤쪽 벽에 콜라이더가 없어도 벽에 닿지 않게 하는 한계다.
    // 값은 처음 자리 앞쪽으로 남겨 두는 여유 (m)
    [SerializeField] private float _reverseMargin = 0.05f;

    public RemoteState State { get; private set; } = RemoteState.Idle;
    public string FeatureId => "remoteParking";
    // 앞이나 뒤가 막혀 원격 이동을 멈춘 상태 (FR-33). 손을 떼거나 반대로 움직이면 풀린다
    public bool BlockedByObstacle { get; private set; }

    private CameraSwitcher _cameraSwitcher;
    private float _stopTimer = 0f;
    private bool _isMoving = false;
    private int _moveDir;   // 지금 움직이는 방향 (1 전진, -1 후진)

    // 장면을 시작한 뒤 처음 버튼을 눌렀을 때의 자리와 방향 (후진 한계의 기준)
    private bool _hasStart;
    private Vector3 _startPosition;
    private Vector3 _startForward;

    // [탑승] 뒤에 오른쪽 카메라가 옮겨 갈 이 차의 운전석 자리
    [SerializeField] private Transform _driverAnchor;
    [SerializeField] private float _boardDoorHold = 0.4f;   // 문이 다 열린 뒤 운전석 시점으로 바꿀 때까지 (초)
    [SerializeField] private float _boardDoorTimeout = 3f;  // 문이 다 열리기를 기다리는 최대 시간 (초)

    private DoorBlockedIndicator _doorIndicator;   // 운전석 문을 여닫는다. 없으면 문 연출 없이 바로 탑승한다
    private Coroutine _boardRoutine;

    private void Start()
    {
        _cameraSwitcher = FindObjectOfType<CameraSwitcher>();
        if (_cameraSwitcher == null)
        {
            Debug.LogWarning("[RemoteParking] CameraSwitcher를 찾을 수 없습니다");
        }

        ValidateComponents();
        if (_doorDrawer != null)
            _doorIndicator = _doorDrawer.GetComponent<DoorBlockedIndicator>();
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

        // 움직이는 방향이 막히면 바로 세운다. 버튼 입력이 끊겨도 벽에 닿지 않게 매 프레임 본다
        if (_isMoving && IsBlocked(_moveDir))
            StopForObstacle(_moveDir);

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

        if (State == RemoteState.Boarded || _boardRoutine != null)
        {
            Debug.Log("[RemoteParking] Press 취소: 탑승 중이거나 Boarded 상태에서는 조작 불가");
            return;
        }

        if (_car == null)
        {
            Debug.LogError("[RemoteParking] Press: _car가 null입니다");
            return;
        }

        // 차가 처음 자리로 되돌려진 뒤의 첫 입력에서 기준 자리를 잡는다
        if (!_hasStart)
        {
            _hasStart = true;
            _startPosition = _car.transform.position;
            _startForward = _car.transform.forward;
        }

        // 가려는 쪽이 막혀 있으면 움직이지 않는다. 반대쪽으로는 빠져나갈 수 있다 (FR-33)
        if (IsBlocked(dir))
        {
            StopForObstacle(dir);
            return;
        }
        BlockedByObstacle = false;

        // Idle → Moving 또는 Boardable → Moving
        if (State == RemoteState.Idle || State == RemoteState.Boardable)
        {
            State = RemoteState.Moving;
            Debug.Log($"[RemoteParking] State: Idle/Boardable → Moving");
        }

        Debug.Log($"[RemoteParking] Press: {(dir > 0 ? "전진" : "후진")} {_speedKmh}km/h 시작");
        _isMoving = true;
        _moveDir = dir > 0 ? 1 : -1;
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
        BlockedByObstacle = false;
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

    private bool IsBlocked(int dir)
    {
        if (dir < 0 && IsAtReverseLimit())
            return true;
        return _proximity != null && _proximity.IsBlockedInDirection(dir);
    }

    // 처음 주차된 자리까지 물러났는지 (그보다 뒤는 벽이다)
    private bool IsAtReverseLimit()
    {
        if (!_hasStart || _car == null)
            return false;

        float travelled = Vector3.Dot(_car.transform.position - _startPosition, _startForward);
        return travelled <= _reverseMargin;
    }

    // 장애물 앞에서 차를 그 자리에 세운다. 같은 장애물로 계속 막혀 있는 동안에는 로그를 한 번만 남긴다
    private void StopForObstacle(int dir)
    {
        _isMoving = false;
        _stopTimer = 0f;
        if (_car != null)
            _car.Brake(_obstacleDecel);

        if (State == RemoteState.Moving)
            State = RemoteState.Idle;

        if (!BlockedByObstacle)
        {
            BlockedByObstacle = true;
            Collider obstacle = _proximity != null ? _proximity.FindObstacle(dir) : null;
            string obstacleName = obstacle != null ? obstacle.name : (dir < 0 && IsAtReverseLimit() ? "처음 주차 자리 한계" : "?");
            Debug.Log($"[RemoteParking] {(dir > 0 ? "전방" : "후방")} 장애물({obstacleName}) 감지, 정지");
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

        if (_boardRoutine != null)
        {
            Debug.Log("[RemoteParking] Board 취소: 이미 탑승 중");
            return;
        }

        _boardRoutine = StartCoroutine(BoardRoutine());
    }

    // 운전석 문을 연 다음 운전석 시점으로 바꾸고, 문을 닫는다
    private IEnumerator BoardRoutine()
    {
        if (_doorIndicator != null && _doorIndicator.isActiveAndEnabled)
        {
            _doorIndicator.OpenForBoarding();

            float deadline = Time.time + _boardDoorTimeout;
            while (!_doorIndicator.IsOpenForBoarding && Time.time < deadline)
                yield return null;

            yield return new WaitForSeconds(_boardDoorHold);
        }

        State = RemoteState.Boarded;
        Debug.Log("[RemoteParking] State: Boarded");
        _boardRoutine = null;

        if (_doorIndicator != null)
            _doorIndicator.CloseDoor();

        if (_cameraSwitcher != null)
        {
            Debug.Log("[RemoteParking] Board: SetRightDriverView() 호출");
            _cameraSwitcher.SetRightDriverView(_driverAnchor);
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
        BlockedByObstacle = false;
        _hasStart = false;

        // 탑승 연출 중에 장면이 바뀌면 그만두고 문을 닫는다
        if (_boardRoutine != null)
        {
            StopCoroutine(_boardRoutine);
            _boardRoutine = null;
        }
        if (_doorIndicator != null)
            _doorIndicator.CloseDoor();

        if (_car != null)
        {
            _car.Brake(5f);
            Debug.Log("[RemoteParking] ResetFeature: 차량 정지");
        }
    }
}
