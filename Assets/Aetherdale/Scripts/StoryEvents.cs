using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;


public class StoryEvent
{
    [Client]
    public static void Send(string message, bool reevaluateConditionalObjects = true)
    {
        Debug.Log("Send story event " + message);
        foreach (IStoryEventHandler target in GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IStoryEventHandler>())
        {
            ExecuteEvents.Execute<IStoryEventHandler>(((MonoBehaviour) target).gameObject, null, (x,y) => x.StoryEvent(message));
        }

        if (reevaluateConditionalObjects)
        {
            ConditionalObject.ReevaluateAll();
        }
    }
}


public interface IStoryEventHandler : IEventSystemHandler
{
    GameObject gameObject { get ; }
    public void TargetStoryEvent(string storyEventID);
    public void StoryEvent(string storyEventID);
}