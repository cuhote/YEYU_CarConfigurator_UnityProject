using UnityEngine;

public class ConfiguratorArea : AreaBase
{
    // 쇼룸에 세워 둔 미리보기 차량
    [SerializeField]
    private CarAppearance _previewCar;

    // 영역에 들어올 때 ModeManager가 부른다. 저장된 선택을 미리보기 차에 적용 (FR-55, FR-59)
    public override void ResetArea(CarConfig config)
    {
        ApplyAppearance(config);
    }

    //외관 선택 화면에서 버튼을 누를 때마다 부른다 (FR-01)
    public void ApplyAppearance(CarConfig config){
        if(_previewCar == null){
            Debug.LogError("[ConfiguratirArea] ApplyAppearance: _ previewCar가 비어있음 (Inspector 연결 확인)");
            return;
        }
        _previewCar.Apply(config);
    }

    protected override void SetSplitCameras(bool enabled)
        {
            if(_leftCam != null)
            {
                _leftCam.rect = new Rect(0,0,1,1);
                _leftCam.enabled = enabled;
            }

        }
}
