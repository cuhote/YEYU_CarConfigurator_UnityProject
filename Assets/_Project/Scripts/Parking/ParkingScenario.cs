using UnityEngine;
using System.Collections;

// 주차 시나리오: 발렛 → 스마트키 순서로 장면을 실행한다 (FR-57).
// 옵션이 꺼진 장면은 ScenarioBase가 건너뛴다 (FR-62).
public class ParkingScenario : ScenarioBase
{
    [SerializeField] private ParkingArea _area;
    // 발렛 차량. 배열 순서대로 출발하고 예약한다 (주차장 A·B 모두)
    [SerializeField] private ValetAgent[] _agents;
    // 스마트키 장면에서 조작하는 적용 쪽 차
    [SerializeField] private RemoteParking _remote;
    // 스마트키 장면을 다시 시작할 때 처음 자리로 되돌릴 차 (A·B의 SmartKeyCar)
    [SerializeField] private CarController[] _smartKeyCars;
    // 비워 두면 씬에서 찾는다
    [SerializeField] private CameraSwitcher _switcher;
    // 스마트키 장면에서 시야를 가려 숨길 오브젝트 (예: Car_NPC2)
    [SerializeField] private GameObject[] _hideInSmartKey;
    // 발렛 장면에서 숨길 오브젝트 (스마트키 차와 그 옆 차. 예: 각 주차장의 NarrowSlotSet)
    [SerializeField] private GameObject[] _hideInValet;
    // 스마트키 화면(UI-06). 스마트키 장면에서만 켠다
    [SerializeField] private GameObject _smartKeyUI;

    [SerializeField] private float _valetTimeout = 120f;   // 발렛 장면 최대 시간 (초). 교착 상태로 끝나지 않을 때 넘어간다
    [SerializeField] private float _resultHold = 2f;      // 장면이 끝난 뒤 결과를 보여 주는 시간 (초)

    public const int SceneValet = ParkingArea.SceneValet;
    public const int SceneSmartKey = ParkingArea.SceneSmartKey;

    private Pose[] _smartKeyStartPoses;

    protected override int SceneCount => 2;

    private void Awake()
    {
        // 스마트키 차의 처음 자리를 기억해 둔다
        _smartKeyStartPoses = new Pose[_smartKeyCars.Length];
        for (int i = 0; i < _smartKeyCars.Length; i++)
        {
            if (_smartKeyCars[i] != null)
                _smartKeyStartPoses[i] = new Pose(_smartKeyCars[i].transform.position, _smartKeyCars[i].transform.rotation);
        }

        if (_switcher == null)
            _switcher = FindAnyObjectByType<CameraSwitcher>();

        SetSmartKeyUI(false);
    }

    protected override bool IsSceneEnabled(int index, CarConfig config)
    {
        if (config == null)
            return true;
        return index == SceneValet ? config.ValetParking : config.RemoteParking;
    }

    // 차·주차면·예약 테이블·에이전트·스마트키·카메라를 처음 상태로 (FR-39)
    protected override void PrepareScene(int index)
    {
        StopVehicles();

        if (_area != null)
            _area.ResetArea(_config);

        // 장면에 나오지 않는 차는 숨긴다. 스마트키 차는 다시 켠 다음에 자리를 되돌린다
        bool smartKey = index == SceneSmartKey;
        SetActive(_hideInValet, smartKey);
        SetActive(_hideInSmartKey, !smartKey);
        SetSmartKeyUI(smartKey);

        for (int i = 0; i < _smartKeyCars.Length; i++)
        {
            if (_smartKeyCars[i] != null && _smartKeyCars[i].gameObject.activeInHierarchy)
                _smartKeyCars[i].ResetPose(_smartKeyStartPoses[i]);
        }

        if (_switcher != null)
        {
            _switcher.SetFixedView(smartKey);
            // 발렛 장면은 주차장 전체가 보이는 버드뷰로 시작한다 (고정 시점을 푼 다음에 바꿔야 한다)
            if (!smartKey)
                _switcher.Switch(CameraView.BirdEye);
        }
    }

    protected override IEnumerator RunScene(int index)
    {
        if (index == SceneValet)
            yield return RunValet();
        else
            yield return RunSmartKey();
    }

    private IEnumerator RunValet()
    {
        // 먼저 모든 차가 서 있는 자리를 잡은 뒤에 출발시킨다.
        // 안 그러면 먼저 출발한 차가 아직 서 있는 차의 자리를 예약해 버린다
        foreach (ValetAgent agent in _agents)
            if (agent != null) agent.HoldPosition();
        foreach (ValetAgent agent in _agents)
            if (agent != null) agent.StartGo();

        // 모든 차가 주차를 마치거나(만차 포함) 제한 시간이 지날 때까지
        float deadline = Time.time + _valetTimeout;
        while (!AllFinished() && Time.time < deadline)
            yield return null;

        if (!AllFinished())
            Debug.Log($"[ParkingScenario] 발렛 장면이 {_valetTimeout}초 안에 끝나지 않아 다음으로 넘어감");

        yield return new WaitForSeconds(_resultHold);
    }

    private bool AllFinished()
    {
        foreach (ValetAgent agent in _agents)
        {
            if (agent == null)
                continue;
            if (agent.State != ValetAgent.AgentState.Parked && agent.State != ValetAgent.AgentState.Full)
                return false;
        }
        return true;
    }

    private IEnumerator RunSmartKey()
    {
        if (_remote == null)
        {
            Debug.LogError("[ParkingScenario] _remote가 비어 있어 스마트키 장면을 건너뜀 (Inspector 연결 확인)");
            yield break;
        }

        _remote.SetActive(true);

        // 사용자가 [탑승]을 누를 때까지 (FR-65)
        while (_remote.State != RemoteParking.RemoteState.Boarded)
            yield return null;

        yield return new WaitForSeconds(_resultHold);
    }

    protected override void OnStopped()
    {
        StopVehicles();
        SetSmartKeyUI(false);
    }

    // 달리던 발렛 차와 스마트키 차를 세우고 상태를 처음으로 돌린다
    private void StopVehicles()
    {
        foreach (ValetAgent agent in _agents)
            if (agent != null) agent.ResetAgent();
        if (_remote != null)
            _remote.ResetFeature();
    }

    private void SetSmartKeyUI(bool active)
    {
        if (_smartKeyUI != null)
            _smartKeyUI.SetActive(active);
    }

    private static void SetActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject obj in objects)
            if (obj != null) obj.SetActive(active);
    }
}
