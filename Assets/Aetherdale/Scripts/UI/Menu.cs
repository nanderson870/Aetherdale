using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public abstract class Menu : MonoBehaviour
{
    [SerializeField] protected GameObject firstSelectedObject;
    [SerializeField] protected GameObject closeSelectedObject;

    public UnityEvent OnOpened;
    public UnityEvent OnClosed;

    public virtual void Open()
    {
        if (IsOpen())
        {
            return;
        }

        gameObject.SetActive(true);

        EventSystem.current.SetSelectedGameObject(firstSelectedObject);

        OnOpened?.Invoke();
    }

    public virtual void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == null && InputSystem.actions.FindAction("Navigate").ReadValue<Vector2>() != Vector2.zero)
        {
            EventSystem.current.SetSelectedGameObject(firstSelectedObject);
        }
    }
    
    public virtual void Close()
    {
        gameObject.SetActive(false);

        if (GetOwningUI() != null)
        {
            GetOwningUI().UpdateMenuStack();
        }

        EventSystem.current.SetSelectedGameObject(closeSelectedObject);

        OnClosed?.Invoke();
    }
    
    public virtual void ProcessInput() {}

    public virtual bool IsOpen()
    {
        return gameObject.activeSelf;
    }

    public PlayerUI GetOwningUI()
    {
        return gameObject.GetComponentInParent<PlayerUI>();
    }

    public Player GetOwningPlayer()
    {
        return GetOwningUI().GetOwningPlayer();
    }
}