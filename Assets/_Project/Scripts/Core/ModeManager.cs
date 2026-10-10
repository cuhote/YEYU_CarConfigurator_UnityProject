using UnityEngine;

public class ModeManager : MonoBehaviour
{
    [SerializeField] private AreaBase[] _areas;
    [SerializeField] private CarConfig _configAsset;
    private AreaBase _currentArea;
    [SerializeField] private CameraSwitcher _cameraSwitcher;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SwitchTo(AppMode.Configurator);
    }

    public void SwitchTo(AppMode mode)
    {
        if (_currentArea != null){_currentArea.OnExit();}
        foreach (AreaBase area in _areas)
        {
            area.Root.SetActive(false);
           
        }

        AreaBase target =_areas[(int)mode];
        
        target.Root.SetActive(true);
        target.ResetArea(_configAsset);
        target.OnEnter();
        // 좌우 카메라가 둘다 연결되어있는(주행, 주차)에서만 
        if (target.LeftCam != null && target.RightCam != null)
        {
            if(_cameraSwitcher == null)
            {
                Debug.LogError("[ModeManager] SwitchTo: _cameraSwitcher가 비어 있음 (Inspector 연결 확인)");
            }
            else
            {
                _cameraSwitcher.Bind(target.LeftCam, target.RightCam);
            }
               
        }
        _currentArea =target; 
    }
    public void StartConfigurator(){SwitchTo(AppMode.Configurator);}
    public void StartDriving(){SwitchTo(AppMode.Driving);}
    public void StartParking(){SwitchTo(AppMode.Parking);}
    
}