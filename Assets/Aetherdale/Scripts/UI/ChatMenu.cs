using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ChatMenu : MonoBehaviour // does not inherit from Menu because it is potentially always open
{
    [SerializeField] int maxLines = 10;
    [SerializeField] float activeDurationOnChange = 7.0F;
    [SerializeField] Transform log;
    [SerializeField] TMP_InputField inputField;

    [SerializeField] TextMeshProUGUI  npcChatMessagePrefab;
    [SerializeField] TextMeshProUGUI environmentMessagePrefab;
    [SerializeField] TextMeshProUGUI playerMessagePrefab;
    [SerializeField] TextMeshProUGUI systemMessagePrefab;

    [SerializeField] RectMask2D gradientMask;

    public delegate void DeactivateAction();
    public event DeactivateAction OnDeactivate;

    PlayerUI playerUI;
    bool open = false;

    int currentLines = 0;

    float lastChangeTime;


    void Start()
    {
        inputField.interactable = false;
        playerUI = GetComponentInParent<PlayerUI>();
        if (playerUI == null)
        {
            Debug.LogError("Could not get parent UI for chat window");
        }
    }

    void LateUpdate()
    {   
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!IsActive())
            {
                Activate();
            }
            else
            {
                Send();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Slash) && !IsActive() && !Player.GetLocalPlayer().GetControlledEntity().InGUI())
        {
            Activate();
            inputField.text = "/";
            inputField.caretPosition = inputField.text.Length;
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && IsActive())
        {
            Deactivate();
            playerUI.GetOwningPlayer().GetControlledEntity().SetInGUI(false);
        }

        if (!IsActive() && Time.time - lastChangeTime > activeDurationOnChange)
        {
            log.gameObject.SetActive(false);
        }
    }

    public bool IsActive()
    {
        return open;
    }

    public void Activate()
    {
        playerUI.GetOwningPlayer().GetControlledEntity().SetInGUI(true);

        log.gameObject.SetActive(true);

        inputField.interactable = true;
        open = true;
        inputField.Select();

        lastChangeTime = Time.time;

        gradientMask.enabled = false;
    }

    public void Deactivate()
    {
        inputField.text = string.Empty;
        inputField.interactable = false;
        open = false;

        OnDeactivate?.Invoke();

        lastChangeTime = Time.time;

        gradientMask.enabled = true;
    }


    public void Send()
    {
        inputField.DeactivateInputField();

        if (Input.GetKeyDown(KeyCode.Return) && inputField.text != string.Empty) // only actually send message if enter pressed
        {
            playerUI.GetOwningPlayer().Chat(inputField.text);
        }

        lastChangeTime = Time.time;

        // Clear field either way
        Deactivate();
        playerUI.GetOwningPlayer().GetControlledEntity().SetInGUI(false);
    }

    public void Receive(ChatMessageType chatMessageType, string message)
    {
        TextMeshProUGUI messageTMP;
        switch (chatMessageType)
        {
            case ChatMessageType.NpcMessage:
                messageTMP = Instantiate(npcChatMessagePrefab, log);
                break;

            case ChatMessageType.PlayerMessage:
                messageTMP = Instantiate(playerMessagePrefab, log);
                break;
            
            case ChatMessageType.EnvironmentMessage:
                messageTMP = Instantiate(environmentMessagePrefab, log);
                break;
            
            case ChatMessageType.SystemMessage:
            default:
                messageTMP = Instantiate(systemMessagePrefab, log);
                break;
        }
        
        messageTMP.text = message;

        StartCoroutine(RefreshChatLog(messageTMP));

        log.gameObject.SetActive(true);
        lastChangeTime = Time.time;

    }

    IEnumerator RefreshChatLog(TextMeshProUGUI messageTMP)
    {
        yield return null;

        currentLines += messageTMP.textInfo.lineCount;
        while (currentLines > maxLines)
        {
            currentLines -= log.GetChild(0).GetComponent<TextMeshProUGUI>().textInfo.lineCount;
            Destroy(log.GetChild(0).gameObject);
        }
    }
}
