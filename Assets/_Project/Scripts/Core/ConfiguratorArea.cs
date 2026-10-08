using UnityEngine;

public class ConfiguratorArea : AreaBase
{
    public override void ResetArea(CarConfig config)
    {
        
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
