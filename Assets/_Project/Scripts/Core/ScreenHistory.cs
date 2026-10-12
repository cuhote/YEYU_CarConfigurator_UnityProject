using UnityEngine;
using System.Collections.Generic;

public class ScreenHistory : MonoBehaviour
{
    public enum ScreenId { Exterior, Feature, DrivingScene, ParkingScene }

    private Stack<ScreenId> _history = new Stack<ScreenId>();
    private ScreenId _lastPushed = ScreenId.Exterior;

    public void Push(ScreenId screenId)
    {
        // 같은 화면이 연속으로 Push되면 무시
        if (_lastPushed == screenId)
            return;

        _history.Push(screenId);
        _lastPushed = screenId;
    }

    public ScreenId Pop()
    {
        if (_history.Count == 0)
            return ScreenId.Exterior;

        ScreenId popped = _history.Pop();
        _lastPushed = _history.Count > 0 ? _history.Peek() : ScreenId.Exterior;
        return popped;
    }

    public ScreenId Peek()
    {
        if (_history.Count == 0)
            return ScreenId.Exterior;

        return _history.Peek();
    }

    public void Clear()
    {
        _history.Clear();
        _lastPushed = ScreenId.Exterior;
    }
}
