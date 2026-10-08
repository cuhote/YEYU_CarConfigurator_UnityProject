using UnityEngine;

public class CameraAnchors : MonoBehaviour
{
    [SerializeField]
    private Transform _driver;
    [SerializeField]
    private Transform _birdEye;
    [SerializeField]
    private Transform _fixed;

    public Transform Driver => _driver;

    public Transform BirdEye => _birdEye;

    public Transform Fixed => _fixed;

    // 앵커 3개가 Inspector에서 모두 연결됐는지 검사
    private void Awake()
    {
        if (_driver == null)
            Debug.LogError($"[CameraAnchors] {gameObject.name}: Driver 앵커가 비어 있음", this);
        if (_birdEye == null)
            Debug.LogError($"[CameraAnchors] {gameObject.name}: BirdEye 앵커가 비어 있음", this);
        if (_fixed == null)
            Debug.LogError($"[CameraAnchors] {gameObject.name}: Fixed 앵커가 비어 있음", this);
    }
}
