using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ProgressionGraphScrollRect : UnityEngine.UI.ScrollRect
{
    private const float DefaultMinimumZoom = 0.25f;
    private const float MaximumZoom = 1.65f;
    private const float WheelDeltaPerTick = 6f;
    private float minimumZoom = DefaultMinimumZoom;

    internal bool FrameGraph()
    {
        Vector2 viewportSize = viewport.rect.size;
        if (viewportSize.x <= 0f || viewportSize.y <= 0f)
            return false;

        Vector2 graphSize = content.rect.size;
        float fitZoom = Mathf.Min(1f, Mathf.Min(viewportSize.x / graphSize.x, viewportSize.y / graphSize.y));
        minimumZoom = Mathf.Min(DefaultMinimumZoom, fitZoom);
        SetZoom(fitZoom);
        SetContentAnchoredPosition(Vector2.zero);
        return true;
    }

    public override void OnScroll(PointerEventData eventData)
    {
        if (!IsActive() || eventData.scrollDelta.y == 0f)
            return;

        float previousZoom = content.localScale.x;
        float wheelDelta = Mathf.Clamp(eventData.scrollDelta.y / WheelDeltaPerTick, -3f, 3f);
        float nextZoom = Mathf.Clamp(previousZoom * Mathf.Pow(1.14f, wheelDelta), minimumZoom, MaximumZoom);
        if (Mathf.Approximately(previousZoom, nextZoom) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                content, eventData.position, eventData.enterEventCamera, out Vector2 point))
            return;

        StopMovement();
        SetZoom(nextZoom);
        Vector2 position = content.anchoredPosition + point * (previousZoom - nextZoom);
        Vector2 overflow = content.rect.size * nextZoom - viewport.rect.size;
        float horizontalLimit = Mathf.Max(0f, overflow.x * 0.5f);
        float verticalLimit = Mathf.Max(0f, overflow.y);
        SetContentAnchoredPosition(new Vector2(
            Mathf.Clamp(position.x, -horizontalLimit, horizontalLimit),
            Mathf.Clamp(position.y, 0f, verticalLimit)));
        eventData.Use();
    }

    private void SetZoom(float zoom)
    {
        content.localScale = new Vector3(zoom, zoom, 1f);
    }
}
