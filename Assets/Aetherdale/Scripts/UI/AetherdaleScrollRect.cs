using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AetherdaleScrollbar : ScrollRect, IPointerEnterHandler, IPointerExitHandler
{
    bool mouseOverThis = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouseOverThis = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (eventData.fullyExited)
        {
            mouseOverThis = false;
        }
    }

    protected override void LateUpdate()
    {
        if (mouseOverThis && InputSystem.actions.FindAction("ScrollWheel").ReadValue<Vector2>().magnitude != 0)
        {
            PointerEventData pointerEventData = new(EventSystem.current)
            {
                scrollDelta = InputSystem.actions.FindAction("ScrollWheel").ReadValue<Vector2>()
            };

            OnScroll(pointerEventData);
        }
    }

    public override void OnScroll(PointerEventData data)
    {
        if (data.scrollDelta.y < -Mathf.Epsilon)
            data.scrollDelta = new Vector2(0f, -scrollSensitivity);
        else if (data.scrollDelta.y > Mathf.Epsilon)
            data.scrollDelta = new Vector2(0f, scrollSensitivity);

        base.OnScroll(data);

    }
}
