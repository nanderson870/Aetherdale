using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Aetherdale
{
    public class MenuTab : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] Color selectedColor;
        [SerializeField] Color unselectedColor;

        [SerializeField] GameObject controlledContent;
        [SerializeField] GameObject contentSelectedObject;

        [SerializeField] UnityEvent OnClick;

        bool selected = false;

        void Start()
        {
            if (selected)
            {
                GetComponent<Image>().color = selectedColor;
            }
            else
            {
                GetComponent<Image>().color = unselectedColor;
            }
        }

        public void Select()
        {
            GetComponent<Image>().color = selectedColor;

            if (controlledContent != null)
            {
                controlledContent.SetActive(true);
            }

            selected = true;

            if (contentSelectedObject != null)
            {
                EventSystem.current.SetSelectedGameObject(contentSelectedObject);
            }
        }

        public void Unselect()
        {
            GetComponent<Image>().color = unselectedColor;

            if (controlledContent != null)
            {
                controlledContent.SetActive(false);
            }

            selected = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke();
        }
    }
}
