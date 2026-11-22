using UnityEngine.EventSystems;

public sealed class HoverButton : UnityEngine.UI.Button
{
    private bool pointerInside;

    public override void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        base.OnPointerEnter(eventData);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        base.OnPointerExit(eventData);
    }

    protected override void OnDisable()
    {
        pointerInside = false;
        base.OnDisable();
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        if (state == SelectionState.Selected && pointerInside)
            state = SelectionState.Highlighted;

        base.DoStateTransition(state, instant);
    }
}
