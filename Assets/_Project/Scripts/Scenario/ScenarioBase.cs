using UnityEngine;
using System.Collections;

// 시나리오 공통 흐름: 장면을 순서대로 실행하고, 꺼진 옵션의 장면은 건너뛰고,
// 장면이 끝나면 잠깐 쉬었다가 다음 장면으로 넘어간다 (FR-57, FR-62, FR-63, FR-48).
// 자식은 장면 수, 장면이 켜져 있는지, 장면 준비와 실행을 채운다.
public abstract class ScenarioBase : MonoBehaviour
{
    [SerializeField] private float _fadeTime = 0.5f;      // 장면 사이 간격 (초)
    [SerializeField] private CanvasGroup _fade;           // 검은 화면. 비워 두면 간격만 둔다

    protected int _currentIndex = -1;
    protected CarConfig _config;
    private Coroutine _running;

    public bool IsRunning => _running != null;
    public int CurrentIndex => _currentIndex;

    protected abstract int SceneCount { get; }
    protected abstract bool IsSceneEnabled(int index, CarConfig config);
    // 장면을 처음 상태로 되돌린다 (차 위치, 카메라 등). 장면 사이의 어두운 화면에서 불린다
    protected virtual void PrepareScene(int index) { }
    // 장면을 실행한다. 장면이 끝나면 반환한다
    protected abstract IEnumerator RunScene(int index);
    // 시나리오가 중간에 멈출 때 차 등을 세우는 곳
    protected virtual void OnStopped() { }

    // 켜진 장면 중 첫 번째부터 시작한다
    public void StartFirstScene(CarConfig config)
    {
        _config = config;
        StartFrom(0);
    }

    // 옵션 토글이 바뀌었을 때: 현재 장면을 처음부터 다시. 현재 장면이 꺼졌으면 켜져 있는 다른 장면부터
    public void RestartCurrentScene(CarConfig config)
    {
        _config = config;
        StartFrom(Mathf.Max(_currentIndex, 0));
    }

    public void GoToNextScene()
    {
        StartFrom(_currentIndex + 1);
    }

    public void StopScenario()
    {
        if (_running != null)
        {
            StopCoroutine(_running);
            _running = null;
            OnStopped();
        }
        SetFade(0f);
    }

    private void StartFrom(int index)
    {
        if (_running != null)
        {
            StopCoroutine(_running);
            OnStopped();
        }
        _running = StartCoroutine(Play(index));
    }

    private IEnumerator Play(int index)
    {
        int first = FindEnabled(index);
        if (first < 0)
        {
            Debug.LogWarning($"[{GetType().Name}] 켜진 장면이 없어 시작하지 않음");
            _running = null;
            yield break;
        }

        // 영역 진입 처리(ModeManager의 카메라 연결)가 끝난 다음 프레임에 시작한다
        yield return null;

        for (int i = first; i < SceneCount; i++)
        {
            if (!IsSceneEnabled(i, _config))
                continue;   // 꺼진 옵션의 장면은 건너뜀

            if (i != first)
                yield return Fade(1f);

            _currentIndex = i;
            Debug.Log($"[{GetType().Name}] 장면 {i} 시작");

            // 화면이 어두운 동안 장면을 처음 상태로 되돌린다
            PrepareScene(i);
            if (i != first)
                yield return Fade(0f);

            yield return RunScene(i);

            Debug.Log($"[{GetType().Name}] 장면 {i} 종료");
        }

        Debug.Log($"[{GetType().Name}] 모든 장면 종료");
        _running = null;
    }

    // from부터 켜진 장면을 찾고, 없으면 앞쪽에서 찾는다. 하나도 없으면 -1
    private int FindEnabled(int from)
    {
        for (int i = from; i < SceneCount; i++)
            if (IsSceneEnabled(i, _config)) return i;
        for (int i = 0; i < from && i < SceneCount; i++)
            if (IsSceneEnabled(i, _config)) return i;
        return -1;
    }

    // 검은 화면을 target(0 투명, 1 검정)까지 _fadeTime의 절반 동안 바꾼다
    private IEnumerator Fade(float target)
    {
        float half = _fadeTime * 0.5f;
        if (_fade == null || half <= 0f)
        {
            yield return new WaitForSeconds(half);
            yield break;
        }

        float start = _fade.alpha;
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            _fade.alpha = Mathf.Lerp(start, target, elapsed / half);
            yield return null;
        }
        _fade.alpha = target;
    }

    private void SetFade(float alpha)
    {
        if (_fade != null)
            _fade.alpha = alpha;
    }
}
