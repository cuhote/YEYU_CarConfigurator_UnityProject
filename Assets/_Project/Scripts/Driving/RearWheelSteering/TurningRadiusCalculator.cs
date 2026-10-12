using UnityEngine;

public static class TurningRadiusCalculator 
{
    public static float Radius(float wheelBase, float frontDeg, float rearDeg)
    {
        //축거와 앞 뒤 바퀴 각도를 받아 회전 반경(m)을 계산
        //항상 0 이상의 값 

        float diff = Mathf.Tan(frontDeg*Mathf.Deg2Rad) - Mathf.Tan(rearDeg*Mathf.Deg2Rad);
        if(Mathf.Abs(diff) < 0.001f) return float.PositiveInfinity; //앞뒤 바퀴가 평행하면 회전 반경은 무한대
        return wheelBase / Mathf.Abs(diff);
    }
    public static Vector3 Icr(Transform car, float wheelBase, float frontDeg, float rearDeg)
    {
        //회전 중심의 월드 좌표를 돌려줌
        //차는 이 점을 중심으로 원을 그리며 돔 
        float diff = Mathf.Tan(frontDeg*Mathf.Deg2Rad) - Mathf.Tan(rearDeg*Mathf.Deg2Rad);
        if(Mathf.Abs(diff) < 0.001f) return car.position;
        float radius = wheelBase /diff;
        return car.position + car.right * radius;
    }


    
}