using UnityEngine;

public class ProximityStop : MonoBehaviour
{
    [SerializeField] private float _stopDistance = 0.3f;
    private CarController _car;
    private float _carLength = 4.5f;

    private void Start()
    {
        _car = GetComponent<CarController>();
        if (_car == null)
        {
            Debug.LogError("[ProximityStop] CarController를 찾을 수 없습니다");
        }
    }

    public bool IsBlocked
    {
        get
        {
            if (_car == null) return false;

            Transform carTransform = _car.transform;
            Vector3 carPos = carTransform.position;
            Vector3 carForward = carTransform.forward;

            // 전방 검사: 차 앞 끝에서 _stopDistance만큼 앞
            Vector3 frontPos = carPos + carForward * (_carLength / 2f);
            if (Physics.Raycast(frontPos, carForward, _stopDistance, LayerMask.GetMask("Obstacle")))
            {
                return true;
            }

            // 후방 검사: 차 뒤 끝에서 _stopDistance만큼 뒤
            Vector3 rearPos = carPos - carForward * (_carLength / 2f);
            if (Physics.Raycast(rearPos, -carForward, _stopDistance, LayerMask.GetMask("Obstacle")))
            {
                return true;
            }

            return false;
        }
    }
}
