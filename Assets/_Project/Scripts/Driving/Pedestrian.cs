using UnityEngine;

//도로 위에 서 있는 보행자표식 감지는 pedestrian 레이어의 collider 로 한다 
public class Pedestrian : MonoBehaviour
{
    [SerializeField] private GameObject _thermalBody; // 열화상 카메라에 밝게 찍히는 몸체

    public void ShowThermalBody(bool show)
    {
        if (_thermalBody != null) _thermalBody.SetActive(show);
    }
}