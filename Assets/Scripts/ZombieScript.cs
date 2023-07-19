using System.Collections;
using UnityEngine;

public class ZombieScript : MonoBehaviour
{
    private static ZombieScript _instance;
    private Vector3 distanceToPlayer;
    public float distanceOffset = 20f;
    public static ZombieScript getInstance()
    {
        return _instance ? _instance : null;
    }

    void Awake()
    {
        _instance = this;
    }

    void Start()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        var position = transform.position;
        position.x = 0;
        transform.position = position;
        if (GameManager.getInstance().GetState() == GameState.PLAYING)
        {
            transform.position = PlayerController.getInstance().transform.position - distanceToPlayer;
        }
    }

    public IEnumerator ActivateZombie()
    {
        gameObject.SetActive(true);
        var postion = PlayerController.getInstance().transform.position;
        postion.z -= distanceOffset;
        transform.position = postion;
        GetComponent<Animator>().SetBool("Running", true);
        yield return new WaitForSeconds(3f);
        postion = PlayerController.getInstance().transform.position;
        distanceToPlayer = postion - transform.position;

    }

    public void DeactivateZombie()
    {
        GetComponent<Animator>().SetBool("Running", false);
        gameObject.SetActive(false);
    }

    public void PlayFootstep()
    {
        AudioManager.getInstance().GetSource("MonsterFootstep").Play();
    }
}
