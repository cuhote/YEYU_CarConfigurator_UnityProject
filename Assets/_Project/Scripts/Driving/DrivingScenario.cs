using UnityEngine;
using System.Collections;

public class DrivingScenario : ScenarioBase
{
    [SerializeField] private DrivingArea _area;
    [SerializeField] private VirtualDriver _driverA;
    [SerializeField] private VirtualDriver _driverB;
    [SerializeField] private float _resultHold = 3f; // 장면이 끝난 뒤 결과를 보여 주는 시간 (초)

     protected override int SceneCount => 2;

    protected override bool IsSceneEnabled(int index, CarConfig config)
    {
        if (config == null) return true;
        return index == DrivingArea.SceneNightVision ? config.NightVision : config.RearWheelSteering;
    }

    // 차 위치, 센서, 조명을 그 장면의 처음 상태로 (FR-39)
    protected override void PrepareScene(int index)
    {
        if (_area != null) _area.SetScene(index);
    }

    protected override IEnumerator RunScene(int index)
    {
        if (index == DrivingArea.SceneNightVision)
            yield return RunNightVision();
        else
            yield return RunUTurn();
    }

    private IEnumerator RunNightVision()
    {
        _driverA.Drive();
        _driverB.Drive();

        yield return new WaitForSeconds(2f); // 차가 출발할 시간을 준다
        while (!_driverA.IsStopped || !_driverB.IsStopped)
            yield return null; // 두 차가 모두 멈출 때까지

        yield return new WaitForSeconds(_resultHold);
    }

    private IEnumerator RunUTurn()
    {
        _driverA.DriveUTurn();
        _driverB.DriveUTurn();

        while (!_driverA.IsDone || !_driverB.IsDone)
            yield return null; // 두 차가 모두 유턴을 마칠 때까지

        yield return new WaitForSeconds(_resultHold);
    }

    protected override void OnStopped()
    {
        if (_driverA != null) _driverA.Stop();
        if (_driverB != null) _driverB.Stop();
    }
}
