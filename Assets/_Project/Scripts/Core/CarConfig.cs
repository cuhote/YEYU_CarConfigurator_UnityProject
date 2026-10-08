using UnityEngine;

namespace Yeyu.Core  
{
    [CreateAssetMenu(menuName = "Yeyu/CarConfig")]
    public class CarConfig : ScriptableObject
    {
        [SerializeField] private int _bodyColor;
        [SerializeField] private int _trimIndex;
        [SerializeField] private int _wheelIndex;
        [SerializeField] private bool _nightVision = true;
        [SerializeField] private bool _rearWheelSteering = true;
        [SerializeField] private bool _valetParking = true;
        [SerializeField] private bool _remoteParking = true;

        public bool NightVision { get => _nightVision; set => _nightVision = value; }
        public bool RearWheelSteering { get => _rearWheelSteering; set => _rearWheelSteering = value; }
        public bool ValetParking { get => _valetParking; set => _valetParking = value; }
        public bool RemoteParking { get => _remoteParking; set => _remoteParking = value; }

        public bool HasDrivingFeature() => _nightVision || _rearWheelSteering;
        public bool HasParkingFeature() => _valetParking || _remoteParking;
    }
}