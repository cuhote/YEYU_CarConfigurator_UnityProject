using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

// UI-06 스마트키 화면: [전진] [후진]은 누르고 있는 동안만 차를 움직이고,
// [탑승]은 오른쪽(적용) 차가 탑승 가능 상태일 때만 보인다 (FR-31, FR-32, FR-64, FR-65).
public class SmartKeyUI : MonoBehaviour
{
    [SerializeField] private Button _forwardButton;
    [SerializeField] private Button _reverseButton;
    // 오른쪽 화면에 놓는 [탑승] 버튼. 탑승 가능 상태에서만 켠다
    [SerializeField] private Button _boardButton;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private RemoteParking _remoteParking;

    private bool _forwardPressed = false;
    private bool _reversePressed = false;

    private void Start()
    {
        ValidateComponents();

        if (_forwardButton != null)
        {
            EventTrigger forwardTrigger = _forwardButton.GetComponent<EventTrigger>();
            if (forwardTrigger == null)
                forwardTrigger = _forwardButton.gameObject.AddComponent<EventTrigger>();

            AddEventTrigger(forwardTrigger, EventTriggerType.PointerDown, () => OnForwardDown());
            AddEventTrigger(forwardTrigger, EventTriggerType.PointerUp, () => OnForwardUp());
        }

        if (_reverseButton != null)
        {
            EventTrigger reverseTrigger = _reverseButton.GetComponent<EventTrigger>();
            if (reverseTrigger == null)
                reverseTrigger = _reverseButton.gameObject.AddComponent<EventTrigger>();

            AddEventTrigger(reverseTrigger, EventTriggerType.PointerDown, () => OnReverseDown());
            AddEventTrigger(reverseTrigger, EventTriggerType.PointerUp, () => OnReverseUp());
        }

        if (_boardButton != null)
        {
            _boardButton.onClick.AddListener(OnBoardPressed);
        }

        Refresh();
    }

    // 장면이 다시 시작되어 화면이 켜질 때 버튼을 처음 상태로 맞춘다
    private void OnEnable()
    {
        _forwardPressed = false;
        _reversePressed = false;
        Refresh();
    }

    // 버튼을 누른 채로 화면이 꺼지면 차가 계속 가지 않게 세운다
    private void OnDisable()
    {
        bool wasPressed = _forwardPressed || _reversePressed;
        _forwardPressed = false;
        _reversePressed = false;

        if (wasPressed && _remoteParking != null)
            _remoteParking.Release();
    }

    private void ValidateComponents()
    {
        if (_remoteParking == null)
            Debug.LogError("[SmartKeyUI] _remoteParking이 null입니다");
        if (_statusText == null)
            Debug.LogError("[SmartKeyUI] _statusText가 null입니다");
        if (_forwardButton == null)
            Debug.LogError("[SmartKeyUI] _forwardButton이 null입니다");
        if (_reverseButton == null)
            Debug.LogError("[SmartKeyUI] _reverseButton이 null입니다");
        if (_boardButton == null)
            Debug.LogError("[SmartKeyUI] _boardButton이 null입니다");
    }

    private void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, System.Action callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = eventType;
        entry.callback.AddListener((data) => callback());
        trigger.triggers.Add(entry);
    }

    private void Update()
    {
        if (_remoteParking == null)
            return;

        // 탑승 가능 위치에 오면 차가 스스로 멈춘다. 그 뒤에는 누르고 있어도 다시 움직이지 않게 입력을 끊는다
        RemoteParking.RemoteState state = _remoteParking.State;
        if (state == RemoteParking.RemoteState.Boardable || state == RemoteParking.RemoteState.Boarded)
        {
            _forwardPressed = false;
            _reversePressed = false;
        }

        if (_forwardPressed)
            _remoteParking.Press(1);
        else if (_reversePressed)
            _remoteParking.Press(-1);

        Refresh();
    }

    private void OnForwardDown()
    {
        _forwardPressed = true;
    }

    private void OnForwardUp()
    {
        _forwardPressed = false;
        if (_remoteParking != null)
            _remoteParking.Release();
    }

    private void OnReverseDown()
    {
        _reversePressed = true;
    }

    private void OnReverseUp()
    {
        _reversePressed = false;
        if (_remoteParking != null)
            _remoteParking.Release();
    }

    private void OnBoardPressed()
    {
        if (_remoteParking != null)
        {
            _remoteParking.Board();
        }
    }

    public void SetBoardEnabled(bool enabled)
    {
        if (_boardButton != null)
        {
            _boardButton.gameObject.SetActive(enabled);
        }
    }

    public void SetStatus(string status)
    {
        if (_statusText != null)
        {
            _statusText.text = status;
        }
    }

    // RemoteParking 상태로 문구와 버튼 표시를 맞춘다
    private void Refresh()
    {
        if (_remoteParking == null)
            return;

        RemoteParking.RemoteState state = _remoteParking.State;

        string status = state switch
        {
            RemoteParking.RemoteState.Idle => "준비 완료",
            RemoteParking.RemoteState.Moving => "원격 이동 중",
            RemoteParking.RemoteState.Boardable => "탑승 가능",
            RemoteParking.RemoteState.Boarded => "탑승 완료",
            _ => "알 수 없음"
        };
        // 앞이나 뒤가 막혀 멈춘 동안에는 경고 문구를 먼저 보여 준다 (FR-33)
        if (_remoteParking.BlockedByObstacle && state != RemoteParking.RemoteState.Boarded)
            status = "장애물 감지, 정지";
        SetStatus(status);

        // [탑승]은 탑승 가능 상태에서만 보인다 (FR-64)
        SetBoardEnabled(state == RemoteParking.RemoteState.Boardable);

        // 탑승한 뒤에는 [전진] [후진]을 숨긴다 (FR-65)
        bool canMove = state != RemoteParking.RemoteState.Boarded;
        if (_forwardButton != null) _forwardButton.gameObject.SetActive(canMove);
        if (_reverseButton != null) _reverseButton.gameObject.SetActive(canMove);
    }
}
