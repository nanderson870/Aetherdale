using TMPro;
using UnityEngine;

public class MessageBox : Menu
{
    [SerializeField] TextMeshProUGUI titleTMP;
    [SerializeField] TextMeshProUGUI messageTMP;

    public static void ShowMessage(string title, string message)
    {
        MessageBox mb = FindAnyObjectByType<MessageBox>(FindObjectsInactive.Include);
        
        mb.titleTMP.text = title;
        mb.messageTMP.text = message;
        
        mb.GetOwningUI().OpenAndPushMenu(mb);

    }
}
