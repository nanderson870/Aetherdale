using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using Mirror;
using UnityEngine.Networking;
using TMPro;

public class PauseMenu : Menu
{
    [SerializeField] Button returnToMenuButton;
    [SerializeField] Button returnToHubButton;

    [SerializeField] TMP_InputField feedbackInputField;
    
    public override void Open()
    {
        base.Open();
        
        if (NetworkServer.active)
        {
            returnToHubButton.gameObject.SetActive(AreaSequencer.GetAreaSequencer().IsSequenceRunning());
            returnToMenuButton.gameObject.SetActive(!AreaSequencer.GetAreaSequencer().IsSequenceRunning());

            if (Player.GetPlayers().Count == 1)
            {
                WorldManager.GetWorldManager().SetTimescale(0);
            }
        }
        else
        {
            returnToHubButton.gameObject.SetActive(false);
        }
    }

    public override void Close()
    {
        base.Close();

        if (NetworkServer.active)
        {
           WorldManager.GetWorldManager().SetTimescale(1.0F);
        }
    }

    public override void ProcessInput()
    {
    }

    public void InviteFriends()
    {
        CSteamID lobbyId = SteamLobby.singleton.currentLobbyID;
        SteamFriends.ActivateGameOverlayInviteDialog(lobbyId);
    }

    public void Join()
    {
        GameManager.JoinGame();
    }


    public void ExitToMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Close();

        GameManager.StopGame();
        
        //AetherdaleNetworkManager.singleton.ServerChangeScene("Main Menu");
        //AetherdaleNetworkManager.singleton.StopHost();
    }

    public void ReturnToHub()
    {
        Close();

        if (NetworkServer.active)
        {
            AreaSequencer.GetAreaSequencer().StopAreaSequence();
        }
    }

    public void ExitToDesktop()
    {
        // Stop game in editor if applicable
        #if UNITY_EDITOR
        if(EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }
        #endif

        Application.Quit();
    }


    public void SendFeedback()
    {
        StartCoroutine(SendFeedbackCoroutine(feedbackInputField.text));
    }
    
    IEnumerator SendFeedbackCoroutine(string feedback)
    {
        string feedbackFormURL = "https://docs.google.com/forms/u/0/d/e/1FAIpQLSdx-n2CLIIvE57xrmS0FnOOIGkOAPirDQ6mQcZ85GxzmVagTA/formResponse";

        WWWForm form = new();
        form.AddField("entry.2023191710", feedback);

        UnityWebRequest www = UnityWebRequest.Post(feedbackFormURL, form);

        yield return www.SendWebRequest();
    }

}
