using System;
using UnityEngine;

// CarConfig의 외관 값(색상·휠)을 차 한 대에 적용한다 (FR-01, FR-03, FR-10).
// Car 프리팹에 붙이고, 선택을 반영할 차(쇼룸 미리보기, Car_E 등)에서만 Apply를 부른다.
public class CarAppearance : MonoBehaviour
{
    // 차체 색이 들어가는 머티리얼 칸 하나. 렌더러에 칸이 여러 개면 도장 칸만 골라 넣는다
    [Serializable]
    private struct PaintSlot
    {
        public Renderer Renderer;
        public int MaterialIndex;
    }

    [Header("Body Color")]
    [SerializeField] private PaintSlot[] _paintSlots;      // 차체 본체, 보닛, 도어, 트렁크, 손잡이, 윙
    [SerializeField] private Material[] _bodyMaterials;    // 색상 4종 (CarConfig.BodyColor 순서)

    [Header("Wheels")]
    [SerializeField] private GameObject[] _wheels;         // 휠 세트 2종 (CarConfig.WheelIndex 순서. 예: Tires_A, Tires_B)

    public void Apply(CarConfig config)
    {
        if (config == null)
        {
            Debug.LogError($"[CarAppearance] Apply: {name}에 넘어온 CarConfig가 null입니다", this);
            return;
        }

        ApplyBodyColor(config.BodyColor);
        ApplyWheels(config.WheelIndex);
    }

    private void ApplyBodyColor(int index)
    {
        if (_bodyMaterials == null || _bodyMaterials.Length == 0)
            return;
        if (index < 0 || index >= _bodyMaterials.Length || _bodyMaterials[index] == null)
        {
            Debug.LogError($"[CarAppearance] ApplyBodyColor: 색상 {index}번 머티리얼이 없음 (Inspector 연결 확인)", this);
            return;
        }

        foreach (PaintSlot slot in _paintSlots)
        {
            if (slot.Renderer == null)
                continue;

            // sharedMaterials는 복사본을 돌려주므로 칸을 바꾼 뒤 다시 넣어야 반영된다.
            // 머티리얼 에셋 자체는 건드리지 않아 다른 차의 색은 그대로다
            Material[] materials = slot.Renderer.sharedMaterials;
            if (slot.MaterialIndex < 0 || slot.MaterialIndex >= materials.Length)
            {
                Debug.LogError($"[CarAppearance] ApplyBodyColor: {slot.Renderer.name}에 머티리얼 칸 {slot.MaterialIndex}번이 없음", this);
                continue;
            }

            materials[slot.MaterialIndex] = _bodyMaterials[index];
            slot.Renderer.sharedMaterials = materials;
        }
    }

    private void ApplyWheels(int index)
    {
        if (_wheels == null || _wheels.Length == 0)
            return;
        if (index < 0 || index >= _wheels.Length || _wheels[index] == null)
        {
            Debug.LogError($"[CarAppearance] ApplyWheels: 휠 {index}번이 없음 (Inspector 연결 확인)", this);
            return;
        }

        for (int i = 0; i < _wheels.Length; i++)
        {
            if (_wheels[i] != null)
                _wheels[i].SetActive(i == index);
        }
    }
}
