public interface ICarFeature
{
    string FeatureId {get ;}

    void SetActive(bool active);

    void ResetFeature();
}