using UnityEngine;
using UnityEngine.Rendering;



public abstract class AreaBase : MonoBehaviour
{
    //인스펙터에서 오브젝트 연결(필드)
    [SerializeField] private GameObject _root;
    [SerializeField] protected Camera _leftCam;
    [SerializeField] protected Camera _rightCam;
    [SerializeField] private AppMode _mode;
    [SerializeField] private Color _ambientColor;

    // 값을 밖에서 읽을 수 있게 내보내줌 (프로퍼티)
    public GameObject Root => _root;
    public Camera LeftCam => _leftCam;
    public Camera RightCam => _rightCam; 


    public abstract void ResetArea(CarConfig config);

    public virtual void OnEnter()
    {
        ApplyLighting();
        SetSplitCameras(true);
    }

    public virtual void OnExit()
    {
        SetSplitCameras(false);
    }
    protected virtual void ApplyLighting()
    {
        RenderSettings.ambientMode =AmbientMode.Flat;
        RenderSettings.ambientLight =_ambientColor;
    }
    protected virtual void SetSplitCameras(bool enabled)
    {   
        if(_leftCam != null)
        {
            _leftCam.rect= new Rect(0,0,0.5f,1);
            _leftCam.enabled = enabled;
        }

        if(_rightCam != null)
        {
            _rightCam.rect= new Rect(0.5f,0,0.5f,1);
            _rightCam.enabled = enabled;
        }
        
    }

}
