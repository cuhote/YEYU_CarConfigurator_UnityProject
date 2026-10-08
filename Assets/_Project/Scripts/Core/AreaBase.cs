using UnityEngine;



public abstract class AreaBase : MonoBehaviour
{
    //인스펙터에서 오브젝트 연결(필드)
    [SerializeField] private GameObject _root;
    [SerializeField] protected Camera _leftCam;
    [SerializeField] protected Camera _rightCam;

    // 값을 밖에서 읽을 수 있게 내보내줌 (프로퍼티)
    public GameObject Root => _root;
    public Camera LeftCam => _leftCam;
    public Camera RightCam => _rightCam; 


    public abstract void ResetArea(CarConfig config);

    public virtual void OnEnter()
    {

    }

    public virtual void OnExit()
    {

    }
    protected virtual void ApplyLighting()
    {

    }
    protected virtual void SetSplitCameras(bool enabled)
    {

    }
}
