using UnityEngine;
using System.Linq;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{
    private static PlayerController _instance;

    public Animator avatar;
    public float initialSpeed = 1.0f;
    public float acceleration=1.0f;
    public float leftRightOffset = 1.8f;

    public float walkSpeed = 3f;

    public BoxCollider headCollider;
    public BoxCollider bodyCollider;

    public DyingAnimationObject[] dyingAnimObjects;

    public Light flashlight;
    public Color invulnerabilityFlashlightColor;
    public Color x2FlashlightColor;

    private int currentOffset;
    private float curSpeed=0.0f;
    private bool isAboutToDie;
    [HideInInspector]
    public Color normalFlashlightColor;
    private bool isInvulnerabilityEnabled;

    public static PlayerController getInstance(){
        return _instance ? _instance : null;
    }

    void Awake(){
        _instance = this;
    }

    void Start()
    {
        curSpeed = initialSpeed;
        normalFlashlightColor = flashlight.color;
        AnimationEventCatcher("DEFAULT");
    }

    void Update(){
        var swipeControls = Swipe.getInstance();
        if (GameManager.getInstance().GetState() == GameState.PLAYING && !isAboutToDie)
        {
            transform.Translate(Vector3.forward * curSpeed * Time.deltaTime);
            curSpeed += acceleration * Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.RightArrow) || swipeControls.SwipeRight)
                currentOffset = Mathf.Clamp(currentOffset + 1, -1, 1);
            else if (Input.GetKeyDown(KeyCode.LeftArrow) || swipeControls.SwipeLeft)
                currentOffset = Mathf.Clamp(currentOffset - 1, -1, 1);
            var newSidePos = new Vector3(currentOffset * leftRightOffset, transform.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, newSidePos, Time.deltaTime * initialSpeed);
            if (Input.GetKeyDown(KeyCode.UpArrow) || swipeControls.SwipeUp)
                avatar.SetTrigger("Jump");
            else if (Input.GetKeyDown(KeyCode.DownArrow) || swipeControls.SwipeDown)
                avatar.SetTrigger("Slide");
        } else if (GameManager.getInstance().GetState() == GameState.START) {
            transform.Translate(Vector3.forward * walkSpeed * Time.deltaTime);
            curSpeed = initialSpeed;
        }
    }

    public void OnHitBoxCollided(Collider cObject, GameObject hitFrom){
        Debug.Log(cObject.name + " : " + hitFrom.name);
        gameObject.SetActive(false);
        var obstacleScript = cObject.GetComponent<ObstacleScript>();
        var animationKey = obstacleScript.GetKey(cObject);
        var dyingAnimObj = dyingAnimObjects.FirstOrDefault(i => i.key == animationKey);
        if(dyingAnimObj == null)
            GameManager.getInstance().SetState(GameState.GAME_OVER);
        else
        {
            dyingAnimObj.parentObj.transform.position = transform.position;
            dyingAnimObj.parentObj.SetActive(true);
            isAboutToDie = true;
            if (dyingAnimObj.dollyCart != null)
            {
                DOTween.To(() => 0f, x => dyingAnimObj.dollyCart.m_Position = x, 1f, 1f).SetEase(Ease.Linear).OnComplete(() =>
                {
                    GameManager.getInstance().SetState(GameState.GAME_OVER);
                    isAboutToDie = false;
                    dyingAnimObj.parentObj.SetActive(false);
                });
            } else
            {
                DOTween.To(() => 0f, x => { }, 1f, 1f).SetEase(Ease.Linear).OnComplete(() =>
                {
                    GameManager.getInstance().SetState(GameState.GAME_OVER);
                    isAboutToDie = false;
                    dyingAnimObj.parentObj.SetActive(false);
                });
            }
        }
    }

    public void AnimationEventCatcher(string animEvent){
        switch (animEvent)
        {
            case "JUMP_ON":
            case "SLIDE_ON":
                headCollider.enabled = true;
                bodyCollider.enabled = false;
                break;
            case "DEFAULT":
            default:
                headCollider.enabled = false;
                bodyCollider.enabled = true;
                break;
        }

        if (isInvulnerabilityEnabled) headCollider.enabled = bodyCollider.enabled = false;

        switch (animEvent)
        {
            case "FOOTSTEP":
                AudioManager.getInstance().GetSource("PlayerFootstep").Play();
                break;
            case "JUMP_ON":
                break;
            case "SLIDE_ON":
                break;
            default:
                break;
        }
    }

    public void EnableDisableInvulnerability(bool isEnabled)
    {
        Debug.Log("Invulnerability: " + isEnabled);
        ChangeFlashlightColor(isEnabled ? invulnerabilityFlashlightColor : normalFlashlightColor);
        headCollider.enabled = bodyCollider.enabled = !isEnabled;
        isInvulnerabilityEnabled = isEnabled;
    }

    public void ChangeFlashlightColor(Color color)
    {
        flashlight.color = color;
    }
}

[System.Serializable]
public class DyingAnimationObject
{
    public string key;
    public GameObject parentObj;
    public Cinemachine.CinemachineDollyCart dollyCart;
}
