using UnityEngine;
using System.Collections;

public class CarController : MonoBehaviour
{
    [SerializeField]
    private float _speedKmh;

    public float SpeedKmh => _speedKmh;

    public void SetSpeed(float kmh){}
    
    public void SetSteer(float degrees){}

    public void SetRearSteer(float degrees){}

    public void Brake(float decel){}

    public IEnumerator Follow(Vector3[] points, float kmh, bool reverse)
    {
        yield break;
    }

    public void ResetPose(Pose pose){}

}
