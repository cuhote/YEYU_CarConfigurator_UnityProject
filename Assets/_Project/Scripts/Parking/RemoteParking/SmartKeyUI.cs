using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class SmartKeyUI : MonoBehaviour
{
    [SerializeField] private Button _forwardButton;
    [SerializeField] private Button _reverseButton;
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

        UpdateStatus();
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
        if (_forwardPressed && _remoteParking != null)
        {
            _remoteParking.Press(1);
        }

        if (_reversePressed && _remoteParking != null)
        {
            _remoteParking.Press(-1);
        }

        UpdateStatus();
        UpdateBoardButtonState();
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
            _boardButton.interactable = enabled;
        }
    }

    public void SetStatus(string status)
    {
        if (_statusText != null)
        {
            _statusText.text = status;
        }
    }

    private void UpdateStatus()
    {
        if (_remoteParking == null || _statusText == null)
            return;

        string status = _remoteParking.State switch
        {
            RemoteParking.RemoteState.Idle => "준비 완료",
            RemoteParking.RemoteState.Moving => "원격 이동 중",
            RemoteParking.RemoteState.Boardable => "탑승 가능",
            RemoteParking.RemoteState.Boarded => "탑승 완료",
            _ => "알 수 없음"
        };

        _statusText.text = status;
    }

    private void UpdateBoardButtonState()
    {
        if (_boardButton == null || _remoteParking == null)
            return;

        bool canBoard = _remoteParking.State == RemoteParking.RemoteState.Boardable;
        _boardButton.interactable = canBoard;
    }
}
