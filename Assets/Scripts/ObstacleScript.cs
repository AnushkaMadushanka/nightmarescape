using UnityEngine;
using System.Linq;

public class ObstacleScript : MonoBehaviour
{
    public ColliderAnimationKeyCombo[] colliderAnimationKeys;

    public string GetKey(Collider collider)
    {
        return colliderAnimationKeys.First(i => i.collider == collider).dyingAnimationKey;
    }
}

[System.Serializable]
public class ColliderAnimationKeyCombo
{
    public string dyingAnimationKey = "Instant";
    public Collider collider;
}
