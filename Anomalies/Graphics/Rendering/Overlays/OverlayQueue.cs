namespace Anomalies.Visuals.Overlays;

/// <summary>
/// 数值越大越晚绘制，覆盖较低优先级的遮罩；仅比较同一 All 阶段内的提交。
/// </summary>
public enum OverlayPriority
{
    Background = -100,
    Default = 0,
    Attack = 100,
    ArenaMask = 200,
    Foreground = 300
}

/// <summary>
/// 在绘制阶段提交当前有效的遮罩，不依赖实体 PreDraw 或屏幕裁剪。
/// </summary>
public interface IOverlaySource
{
    void SubmitOverlays(OverlayQueue overlays);
}

/// <summary>
/// 绘制对象可由实体本身实现。调用前后 SpriteBatch 均须处于未开始状态。
/// </summary>
public interface IOverlayDrawable
{
    void DrawOverlay(SpriteBatch spriteBatch);
}

/// <summary>
/// 单次 All 阶段的提交队列。按优先级、提交顺序绘制，不跨帧保留实体或委托。
/// </summary>
public sealed class OverlayQueue
{
    private readonly record struct Entry(IOverlayDrawable Drawable, OverlayPriority Priority, int Order);
    private readonly List<Entry> _Entries = [];
    private readonly HashSet<IOverlayDrawable> _Submitted = new(ReferenceEqualityComparer.Instance);
    private bool _Collecting;

    /// <summary>
    /// 仅能在 SubmitOverlays 回调内提交；同一绘制对象的重复提交被忽略，首次优先级生效。
    /// </summary>
    public void Submit(IOverlayDrawable drawable, OverlayPriority priority = OverlayPriority.Default)
    {
        ArgumentNullException.ThrowIfNull(drawable);
        if (!_Collecting)
            throw new InvalidOperationException("Overlays may only be submitted during collection.");
        if (!_Submitted.Add(drawable))
            return;
        _Entries.Add(new(drawable, priority, _Entries.Count));
    }

    internal void BeginCollection()
    {
        Clear();
        _Collecting = true;
    }

    internal void Draw(SpriteBatch spriteBatch)
    {
        _Collecting = false;
        _Entries.Sort(static (left, right) =>
        {
            int priority = left.Priority.CompareTo(right.Priority);
            return priority != 0 ? priority : left.Order.CompareTo(right.Order);
        });
        foreach (Entry entry in _Entries)
            entry.Drawable.DrawOverlay(spriteBatch);
    }

    internal void Clear()
    {
        _Collecting = false;
        _Entries.Clear();
        _Submitted.Clear();
    }
}
