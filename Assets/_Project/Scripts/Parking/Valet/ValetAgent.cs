using UnityEngine;
using System.Collections;

public class ValetAgent : MonoBehaviour
{
    public enum AgentState { Idle, Moving, Reversing, Parked, Full }

    [SerializeField] private CarController _car;
    [SerializeField] private ParkingSlotAllocator _allocator;
    [SerializeField] private float _laneZ = 7.05f;

    public AgentState State { get; private set; } = AgentState.Idle;

    private Coroutine _currentCoroutine;
    private ParkingSlot _assignedSlot;

    public IEnumerator Go()
    {
        if (_car == null)
        {
            Debug.LogError("[ValetAgent] Go: _car가 null입니다");
            yield break;
        }
        if (_allocator == null)
        {
            Debug.LogError("[ValetAgent] Go: _allocator가 null입니다");
            yield break;
        }

        _assignedSlot = _allocator.AssignEmpty();
        if (_assignedSlot == null)
        {
            State = AgentState.Full;
            Debug.Log($"[ValetAgent] State: {State}");
            yield break;
        }

        State = AgentState.Moving;
        Debug.Log($"[ValetAgent] State: {State}");
        Vector3[] globalPath = CalculateGlobalPath(_assignedSlot);
        yield return _car.Follow(globalPath, 10f, false);

        Debug.Log("[ValetAgent] 전역 이동 완료");

        Pose entryPose = _assignedSlot.GetEntryPose();
        _car.ResetPose(entryPose);
        Debug.Log("[ValetAgent] 진입 자세 세팅 완료");

        yield return new WaitForFixedUpdate();
        _car.Brake(3f);
        yield return new WaitForSeconds(0.5f);

        Vector3[] reverseRoute = _assignedSlot.GetReverseRoute();
        if (reverseRoute == null || reverseRoute.Length < 2)
        {
            Debug.LogError("[ValetAgent] Go: 후진 경로가 없거나 점이 부족합니다");
            yield break;
        }

        State = AgentState.Reversing;
        Debug.Log($"[ValetAgent] State: {State}");
        yield return _car.Follow(reverseRoute, 5f, true);

        // Vector3 routeDir = (reverseRoute[0] - reverseRoute[1]).normalized;
        Vector3 routeDir = (reverseRoute[reverseRoute.Length-2] - reverseRoute[reverseRoute.Length-1]).normalized;
        Quaternion parkingRot = Quaternion.LookRotation(routeDir, Vector3.up);

        _car.ResetPose(new Pose(_assignedSlot.transform.position, parkingRot));
        _car.Brake(3f);
        Debug.Log("[ValetAgent] 후진 완료, 주차 위치에 스냅");

        State = AgentState.Parked;
        Debug.Log($"[ValetAgent] State: {State}");
    }

    private Vector3[] CalculateGlobalPath(ParkingSlot slot)
    {
        Vector3 carPos = _car.transform.position;
        Vector3 entryPos = slot.GetEntryPose().position;

        return new Vector3[]
        {
            carPos,
            new Vector3(carPos.x, carPos.y, _laneZ),
            new Vector3(entryPos.x, entryPos.y, _laneZ),
            entryPos
        };
    }

    public void ResetAgent()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
            _currentCoroutine = null;
        }

        State = AgentState.Idle;
        _assignedSlot = null;

        if (_car != null)
        {
            _car.Brake(3f);
        }
    }

    public void StartGo()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }
        _currentCoroutine = StartCoroutine(Go());
    }
}
