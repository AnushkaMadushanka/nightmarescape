using UnityEngine;
using UnityEngine.Events;


public class AnimationEventTriggerScript : MonoBehaviour
{
    public UnityEvent<string> action;
    public void FireEvent(string eventName){
        action.Invoke(eventName);
    }
}
