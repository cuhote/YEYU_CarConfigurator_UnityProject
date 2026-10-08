using UnityEngine;

public class ModeManager : MonoBehaviour
{
    [SerializeField] private AreaBase[] _areas;
    [SerializeField] private CarConfig _configAsset;
    private AreaBase _currentArea;
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
        _currentArea =target; 
               
    }

    public void StartConfigurator(){SwitchTo(AppMode.Configurator);}
    public void StartDriving(){SwitchTo(AppMode.Driving);}
    public void StartParking(){SwitchTo(AppMode.Parking);}
}
