using UnityEngine;

public class HitBoxScript : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<ObstacleScript>())
            PlayerController.getInstance().OnHitBoxCollided(other, gameObject);
    }
}
