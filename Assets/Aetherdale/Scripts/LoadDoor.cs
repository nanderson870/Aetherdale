using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class LoadDoor : MonoBehaviour, IInteractable
{
    [SerializeField] string destinationSceneName;
    [SerializeField] string doorName;
    [SerializeField] Transform placePlayerTransform;

    List<ControlledEntity> entitiesUsingThisDoor = new();

    [Server]
    public void Interact(ControlledEntity entity)
    {
        // Check if dest scene is loaded
        Scene destScene = SceneManager.GetSceneByName(destinationSceneName);

        if (!destScene.IsValid())
        {
            entity.connectionToClient.Send(new SceneMessage { sceneName = destinationSceneName, sceneOperation = SceneOperation.LoadAdditive, customHandling = true });
            StartCoroutine(LoadSceneAsync(entity));
        }
        else
        {
            StartCoroutine(TransportPlayer(entity));
        }
    }

    public bool IsInteractable(ControlledEntity interactingEntity)
    {
        return true;
    }

    public string GetInteractionPromptText(ControlledEntity interactingEntity)
    {
        return $"Go to {destinationSceneName}";
    }


    IEnumerator LoadSceneAsync(ControlledEntity travellingEntity)
    {
        AsyncOperation loadSceneOp = SceneManager.LoadSceneAsync(destinationSceneName, LoadSceneMode.Additive);

        while (!loadSceneOp.isDone)
        {
            yield return null;
        }

        if (travellingEntity != null)
        {
            yield return StartCoroutine(TransportPlayer(travellingEntity));
        }
    }

    [ServerCallback]
    IEnumerator TransportPlayer(ControlledEntity entity)
    {
        if (entity.TryGetComponent(out NetworkIdentity identity))
        {
            NetworkConnectionToClient conn = identity.connectionToClient;
            if (conn == null) yield break;

            Player player = entity.GetOwningPlayer();

            // Tell client to unload previous subscene. No custom handling for this.
            //conn.Send(new SceneMessage { sceneName = gameObject.scene.path, sceneOperation = SceneOperation.UnloadAdditive, customHandling = true });

            yield return new WaitForSeconds(0.2F);

            //NetworkServer.RemovePlayerForConnection(conn, false);

            // reposition player on server and client
            entity.TargetSetPosition(FindDestinationPosition());

            Scene destScene = SceneManager.GetSceneByName(destinationSceneName);

            // Move player to new subscene.
            SceneManager.MoveGameObjectToScene(entity.gameObject, destScene);


            Debug.Log("after move, object is " + player);
            player.SetControlledEntity(entity);
            // Tell client to load the new subscene with custom handling (see NetworkManager::OnClientChangeScene).
            
            //NetworkServer.AddPlayerForConnection(conn, player.gameObject);
        }

        //entity.RpcSceneChange(FindDestinationPosition(), destinationSceneName);
    }

    public string GetDestinationSceneName()
    {
        return destinationSceneName;
    }

    public string GetDoorName()
    {
        return doorName;
    }

    public Vector3 GetPlacePlayerPosition()
    {
        return placePlayerTransform.position;
    }

    public Vector3 FindDestinationPosition()
    {
        LoadDoor[] loadDoors = FindObjectsByType<LoadDoor>(FindObjectsSortMode.None);
        foreach (LoadDoor loadDoor in loadDoors)
        {
            if (loadDoor != this && loadDoor.doorName == doorName)
            {
                return loadDoor.GetPlacePlayerPosition();
            }
        }

        return Vector3.negativeInfinity;
    }

    public string GetTooltipText(ControlledEntity interactingEntity)
    {
        throw new System.NotImplementedException();
    }

    public string GetTooltipTitle(ControlledEntity interactingEntity)
    {
        throw new System.NotImplementedException();
    }

    public bool IsSelectable()
    {
        return true;
    }
}
