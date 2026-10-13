using UnityEngine;

public class CameraAnchors : MonoBehaviour
{
    [SerializeField]
    private Transform _driver;
    [SerializeField]
    private Transform _birdEye;
    [SerializeField]
    private Transform _fixed;

    // 버드뷰에서 이 높이(월드 Y, m)보다 위에 있는 것은 그리지 않는다 (천장·배관 숨기기). 0이면 끔.
    // 버드뷰 앵커가 바로 아래를 내려다볼 때만 높이가 정확히 맞는다
    [SerializeField]
    private float _birdEyeCutHeight = 0f;

    public Transform Driver => _driver;

    public float BirdEyeCutHeight => _birdEyeCutHeight;

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
