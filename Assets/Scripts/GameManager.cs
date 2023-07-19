using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;
using Cinemachine;
using UnityEngine.Events;

public enum GameState
{
    START,
    START_TRANSITION,
    PLAYING,
    PAUSED,
    GAME_OVER,
}

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    private GameState _currentState = GameState.START;
    private float _tileBackwardOffset;
    private UIDocument _uiDocument;
    private Label _currentScoreLabel;
    private int _startZ;
    private int started2xScore;
    private int total2xScore;
    private PowerDetails currentPowerTween;

    public Vector3 startOffset = Vector3.zero;
    public float continuePlayingOffset = 1.0f;
    public VisualTreeAsset gameOverDocument;
    public VisualTreeAsset mainMenuDocument;
    public VisualTreeAsset gameOverlayDocument;
    public VisualTreeAsset settingsDocument;
    public VisualTreeAsset pausedMenuDocument;
    public VisualTreeAsset playTransitionDocument;
    public VisualTreeAsset adTemplateDocument;
    public VisualTreeAsset helpTemplateDocument;


    public AnimationCurve transitionRunCurve;
    public AudioMixer audioMixer;
    public float backgroundThemeVolume;

    public int powerUpIncrease = 2;
    public Sprite[] helpSprites;


    public static GameManager getInstance()
    {
        return _instance ? _instance : null;
    }
    void Awake()
    {
        _instance = this;
    }

    void Start()
    {
        if (!PlayerPrefs.HasKey("2x"))
            PlayerPrefs.SetInt("2x", powerUpIncrease);
        if (!PlayerPrefs.HasKey("invulnerability"))
            PlayerPrefs.SetInt("invulnerability", powerUpIncrease);

        _uiDocument = GetComponent<UIDocument>();
        _tileBackwardOffset = TileManager.getInstance().backwardOffsetFromPlayer;
        audioMixer.SetFloat("MusicVolume", PlayerPrefs.GetFloat("MusicVolume"));
        audioMixer.SetFloat("SFXVolume", PlayerPrefs.GetFloat("SFXVolume"));
        QualitySettings.SetQualityLevel(PlayerPrefs.HasKey("Quality") ? PlayerPrefs.GetInt("Quality") : 2, true);
        SetState(GameState.START);
    }

    void Update()
    {
        if (GetState() == GameState.PLAYING && _currentScoreLabel != null)
        {
            var playerScore = GetPlayerScore();
            var additionalScore = started2xScore > 0 ? playerScore - started2xScore : 0;
            _currentScoreLabel.text = (playerScore + additionalScore + total2xScore).ToString();
        }
    }

    public int GetPlayerScore()
    {
        return (int)PlayerController.getInstance().transform.position.z - _startZ;
    }

    public void SetupGameOverScreen()
    {
        ZombieScript.getInstance().DeactivateZombie();
        TileManager.getInstance().ChangeStateAllSpawned(false);
        if (currentPowerTween != null)
        {
            currentPowerTween.tween.Kill();
            currentPowerTween = null;
        }
        var chaseThemeSource = AudioManager.getInstance().GetSource("ChaseTheme");
        DOTween.To(() => backgroundThemeVolume, x => chaseThemeSource.volume = x, 0f, 2f).SetEase(Ease.Linear).OnComplete(() => { chaseThemeSource.Stop(); });
        var tree = gameOverDocument.CloneTree();
        tree.StretchToParentSize();

        var container = tree.Q<VisualElement>("container");
        var scoreText = tree.Q<Label>("score-text");
        DOTween.To(() => 0f, x => container.style.opacity = x, 1f, 2f).SetEase(Ease.Linear);
        total2xScore += started2xScore > 0 ? GetPlayerScore() - started2xScore : 0;
        started2xScore = 0;
        PlayerController.getInstance().EnableDisableInvulnerability(false);
        var score = GetPlayerScore() + total2xScore;
        DOTween.To(() => 0, x => scoreText.text = x.ToString(), score, 3f).SetEase(Ease.OutSine);
        PlayGames.getInstance().AddScoreToLeaderboard(score);

        var continueButton = tree.Q<Button>("continue-button");
        var mainmenuButton = tree.Q<Button>("mainmenu-button");
        continueButton.clicked += () =>
        {
            AdsManager.getInstance().ShowRewardAd(() =>
            {
                var player = PlayerController.getInstance().gameObject;
                player.SetActive(true);
                var currentPosition = player.transform.position;
                currentPosition.z += continuePlayingOffset;
                player.transform.position = currentPosition;
                FindObjectOfType<CinemachineBrain>().ManualUpdate();
                player.SetActive(false);
                ShowPlayingTransitionUI(true);
            });
        };
        mainmenuButton.clicked += () =>
        {
            SetState(GameState.START);
        };

        _uiDocument.rootVisualElement.Clear();
        _uiDocument.rootVisualElement.Add(tree);

        AdsManager.getInstance().ShowAd();
    }

    public void SetupStart()
    {
        AudioManager.getInstance().GetSource("ChaseTheme").Stop();
        var tileManagerInstance = TileManager.getInstance();
        tileManagerInstance.DeleteAllSpawned();
        PlayerController.getInstance().gameObject.transform.position = startOffset;
        PlayerController.getInstance().gameObject.SetActive(true);
        PlayerController.getInstance().avatar.SetFloat("Running", 0.5f);
        tileManagerInstance.backwardOffsetFromPlayer = tileManagerInstance.frontOffsetFromPlayer + 20;
        tileManagerInstance.CreateTile();
        total2xScore = 0;
        started2xScore = 0;
        ShowMainUI();
    }

    public void SetupPlaying()
    {
        var chaseThemeSource = AudioManager.getInstance().GetSource("ChaseTheme");
        if (!chaseThemeSource.isPlaying)
        {
            chaseThemeSource.Play();
            DOTween.To(() => 0f, x => chaseThemeSource.volume = x, backgroundThemeVolume, 2f).SetEase(Ease.Linear);
        }
        TileManager.getInstance().ChangeStateAllSpawned(true);
        PlayerController.getInstance().gameObject.SetActive(true);
        PlayerController.getInstance().avatar.SetFloat("Running", 1f);
        ShowPlayUI();
        TileManager.getInstance().backwardOffsetFromPlayer = _tileBackwardOffset;
    }

    public void SetupStartTransition()
    {
        var chaseThemeSource = AudioManager.getInstance().GetSource("ChaseTheme");
        chaseThemeSource.Play();
        DOTween.To(() => 0f, x => chaseThemeSource.volume = x, backgroundThemeVolume, 2f).SetEase(Ease.Linear);

        _uiDocument.rootVisualElement.Clear();
        var player = PlayerController.getInstance().gameObject;
        var endPos = new Vector3(
            player.transform.position.x,
            player.transform.position.y,
            player.transform.position.z + 10
            );
        Sequence sequence = DOTween.Sequence();
        sequence.Append(DOTween.To(() => player.transform.position, x => player.transform.position = x, endPos, 5f).SetEase(transitionRunCurve))
        .Insert(1f, DOTween.To(() => Vector3.zero, x => player.transform.rotation = Quaternion.Euler(x.x, x.y, x.z), new Vector3(0f, 180f, 0f), 1f).SetEase(Ease.InOutQuart))
        .InsertCallback(1f, () => { PlayerController.getInstance().avatar.SetFloat("Running", 0f); })
        .InsertCallback(1.5f, () =>
        {
            BendingManager.getInstance().frontOffset *= -1;
            StartCoroutine(ZombieScript.getInstance().ActivateZombie());
        })
        .Insert(4f, DOTween.To(() => new Vector3(0f, 180f, 0f), x => player.transform.rotation = Quaternion.Euler(x.x, x.y, x.z), Vector3.zero, 1f).SetEase(Ease.InOutQuart))
        .InsertCallback(4.5f, () => { BendingManager.getInstance().frontOffset *= -1; })
        .OnComplete(() =>
        {
            _startZ = (int)PlayerController.getInstance().gameObject.transform.position.z;
            SetState(GameState.PLAYING);
        });
    }

    public void SetupPaused()
    {
        PlayerController.getInstance().gameObject.SetActive(false);
        ZombieScript.getInstance().DeactivateZombie();
        if (currentPowerTween != null) currentPowerTween.tween.Pause();
        ShowPauseMenuUI();
    }

    public void SetState(GameState gs)
    {
        _currentState = gs;
        switch (gs)
        {
            case GameState.GAME_OVER:
                SetupGameOverScreen();
                break;
            case GameState.START:
                SetupStart();
                break;
            case GameState.START_TRANSITION:
                SetupStartTransition();
                break;
            case GameState.PLAYING:
                SetupPlaying();
                break;
            case GameState.PAUSED:
                SetupPaused();
                break;
            default:
                break;
        }
    }

    public GameState GetState()
    {
        return _currentState;
    }

    public void ShowMainUI()
    {
        var tree = mainMenuDocument.CloneTree();
        tree.StretchToParentSize();
        var mainTitle = tree.Q<Label>("main-title");
        DOTween.To(() => 0f, x => mainTitle.style.opacity = x, 1f, 1f).SetEase(Ease.Linear);
        var invulnerableCountLabel = tree.Q<Label>("invulnerable-count-label");
        var x2CountLabel = tree.Q<Label>("2x-count-label");
        invulnerableCountLabel.text = PlayerPrefs.GetInt("invulnerability").ToString();
        x2CountLabel.text = PlayerPrefs.GetInt("2x").ToString();
        tree.Q<Button>("invulnerable-button").RegisterCallback<ClickEvent>((evt) =>
        {
            evt.StopPropagation();
            ShowAdTemplate("Watch an Ad to Get " + powerUpIncrease.ToString() + " invulnerability tokens", () =>
             {
                 AdsManager.getInstance().ShowRewardAd(() =>
                 {
                     var newValue = PlayerPrefs.GetInt("invulnerability") + powerUpIncrease;
                     PlayerPrefs.SetInt("invulnerability", Mathf.Min(newValue, 9));
                     ShowMainUI();
                 });
             }, () => { ShowMainUI(); });
        }, TrickleDown.NoTrickleDown);
        tree.Q<Button>("2x-button").RegisterCallback<ClickEvent>((evt) =>
        {
            evt.StopPropagation();
            ShowAdTemplate("Watch an Ad to Get " + powerUpIncrease.ToString() + " 2x tokens", () =>
            {
                AdsManager.getInstance().ShowRewardAd(() =>
                {
                    var newValue = PlayerPrefs.GetInt("2x") + powerUpIncrease;
                    PlayerPrefs.SetInt("2x", Mathf.Min(newValue, 9));
                    ShowMainUI();
                });
            }, () => { ShowMainUI(); });
        }, TrickleDown.NoTrickleDown);
        tree.Q<Button>("settings-button").RegisterCallback<ClickEvent>((evt) =>
        {
            evt.StopPropagation();
            ShowSettingsUI();
        }, TrickleDown.NoTrickleDown);
        tree.Q<Button>("leaderboard-button").RegisterCallback<ClickEvent>((evt) =>
        {
            evt.StopPropagation();
            PlayGames.getInstance().ShowLeaderboard();
        }, TrickleDown.NoTrickleDown);
        tree.Q<Button>("help-button").RegisterCallback<ClickEvent>((evt) =>
        {
            evt.StopPropagation();
            ShowHelpUI();
        }, TrickleDown.NoTrickleDown);
        tree.Q<VisualElement>("container").RegisterCallback<ClickEvent>((evt) =>
        {
            SetState(GameState.START_TRANSITION);
        }, TrickleDown.NoTrickleDown);
        _uiDocument.rootVisualElement.Clear();
        _uiDocument.rootVisualElement.Add(tree);
    }

    public void ShowPlayUI()
    {
        var tree = gameOverlayDocument.CloneTree();
        _currentScoreLabel = tree.Q<Label>("score-label");
        tree.Q<Label>("power-label").style.display = DisplayStyle.None;
        tree.Q<ProgressBar>("power-progress").style.display = DisplayStyle.None;
        var invulnerableCountLabel = tree.Q<Label>("invulnerable-count-label");
        var x2CountLabel = tree.Q<Label>("2x-count-label");
        invulnerableCountLabel.text = PlayerPrefs.GetInt("invulnerability").ToString();
        x2CountLabel.text = PlayerPrefs.GetInt("2x").ToString();
        tree.Q<Button>("invulnerable-button").clicked += () =>
        {
            var tokenCount = PlayerPrefs.GetInt("invulnerability");
            if (tokenCount > 0 && currentPowerTween == null)
            {
                PlayerPrefs.SetInt("invulnerability", tokenCount - 1);
                invulnerableCountLabel.text = (tokenCount - 1).ToString();
                ShowPowerUI("Invulnerability", 8f, () =>
                {
                    PlayerController.getInstance().EnableDisableInvulnerability(true);
                }, () =>
                {
                    PlayerController.getInstance().EnableDisableInvulnerability(false);
                });
            }
        };
        tree.Q<Button>("2x-button").clicked += () =>
        {
            var tokenCount = PlayerPrefs.GetInt("2x");
            if (tokenCount > 0 && currentPowerTween == null)
            {
                PlayerPrefs.SetInt("2x", tokenCount - 1);
                x2CountLabel.text = (tokenCount - 1).ToString();
                var playerControllerInstance = PlayerController.getInstance();
                ShowPowerUI("2x", 20f, () =>
                {
                    playerControllerInstance.ChangeFlashlightColor(playerControllerInstance.x2FlashlightColor);
                    started2xScore = GetPlayerScore();
                }, () =>
                {
                    playerControllerInstance.ChangeFlashlightColor(playerControllerInstance.normalFlashlightColor);
                    total2xScore += GetPlayerScore() - started2xScore;
                    started2xScore = 0;
                    PlayerController.getInstance().EnableDisableInvulnerability(false);
                });
            }
        };
        tree.Q<Button>("pause-button").clicked += () =>
        {
            SetState(GameState.PAUSED);
        };
        tree.StretchToParentSize();

        _uiDocument.rootVisualElement.Clear();
        _uiDocument.rootVisualElement.Add(tree);
        if (currentPowerTween != null) 
            ShowPowerUI(currentPowerTween.powerName, currentPowerTween.powerSeconds, currentPowerTween.before, currentPowerTween.after);
    }

    public void ShowSettingsUI()
    {
        var tree = settingsDocument.CloneTree();
        tree.StretchToParentSize();
        int qualityLevel = QualitySettings.GetQualityLevel();
        var qualitySlider = tree.Q<Slider>("quality-slider");
        qualitySlider.value = qualityLevel;
        qualitySlider.RegisterValueChangedCallback((evt) =>
        {
            var roundValue = Mathf.RoundToInt(evt.newValue);
            if (evt.newValue == roundValue)
            {
                QualitySettings.SetQualityLevel(roundValue, true);
            }
            else
            {
                qualitySlider.value = roundValue;
            }
        });
        var musicSlider = tree.Q<Slider>("music-slider");
        audioMixer.GetFloat("MusicVolume", out var musicValue);
        musicSlider.value = musicValue;
        musicSlider.RegisterValueChangedCallback((evt) =>
        {
            audioMixer.SetFloat("MusicVolume", evt.newValue);
        });
        var sfxSlider = tree.Q<Slider>("sfx-slider");
        audioMixer.GetFloat("SFXVolume", out var sfxValue);
        sfxSlider.value = sfxValue;
        sfxSlider.RegisterValueChangedCallback((evt) =>
        {
            audioMixer.SetFloat("SFXVolume", evt.newValue);
        });
        tree.Q<Button>("back-button").clicked += () =>
        {
            PlayerPrefs.SetInt("Quality", (int)qualitySlider.value);
            PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
            PlayerPrefs.SetFloat("SFXVolume", sfxSlider.value);
            if (GetState() == GameState.PAUSED)
                ShowPauseMenuUI();
            else
                ShowMainUI();
        };
        _uiDocument.rootVisualElement.Clear();
        _uiDocument.rootVisualElement.Add(tree);
    }

    public void ShowPauseMenuUI()
    {
        var tree = pausedMenuDocument.CloneTree();
        tree.Q<Button>("continue-button").clicked += () =>
        {
            ShowPlayingTransitionUI(false);
        };
        tree.Q<Button>("settings-button").clicked += () =>
        {
            ShowSettingsUI();
        };
        tree.Q<Button>("mainmenu-button").clicked += () =>
        {
            SetState(GameState.START);
        };
        tree.StretchToParentSize();

        _uiDocument.rootVisualElement.Clear();
        _uiDocument.rootVisualElement.Add(tree);
    }

    public void ShowHelpUI()
    {
        var tree = helpTemplateDocument.CloneTree();
        var helpImage = tree.Q<IMGUIContainer>("help-image");
        var currentVal = 0;
        helpImage.style.backgroundImage = new StyleBackground(helpSprites[currentVal]);
        tree.Q<Button>("back-button").clicked += () =>
        {
            currentVal -= 1;
            currentVal = currentVal >= 0 ? currentVal : helpSprites.Length - 1;
            helpImage.style.backgroundImage = new StyleBackground(helpSprites[currentVal]);
        };
        tree.Q<Button>("close-button").clicked += () =>
        {
            ShowMainUI();
        };
        tree.Q<Button>("next-button").clicked += () =>
        {
            currentVal += 1;
            currentVal = helpSprites.Length > currentVal ? currentVal : 0;
            helpImage.style.backgroundImage = new StyleBackground(helpSprites[currentVal]);
        };
        tree.StretchToParentSize();

        _uiDocument.rootVisualElement.Clear();
        _uiDocument.rootVisualElement.Add(tree);
    }

    public void ShowPlayingTransitionUI(bool isContinuePlaying)
    {
        var tree = playTransitionDocument.CloneTree();
        var countdownLabel = tree.Q<Label>("countdown-label");
        tree.StretchToParentSize();

        TileManager.getInstance().ChangeStateAllSpawned(true);
        DOTween.To(() => 3f, x => countdownLabel.text = Mathf.Ceil(x).ToString(), 0f, 3f).SetEase(Ease.Linear).OnComplete(() =>
        {
            SetState(GameState.PLAYING);
            if (isContinuePlaying) ShowPowerUI("Invulnerability", 8f, () =>
            {
                PlayerController.getInstance().EnableDisableInvulnerability(true);
            }, () =>
            {
                PlayerController.getInstance().EnableDisableInvulnerability(false);
            });
        });
        _uiDocument.rootVisualElement.Clear();
        _uiDocument.rootVisualElement.Add(tree);
    }

    public void ShowPowerUI(string powerName, float powerSeconds, UnityAction before, UnityAction after)
    {
        var tree = _uiDocument.rootVisualElement;
        var powerLabel = tree.Q<Label>("power-label");
        var powerBar = tree.Q<ProgressBar>("power-progress");
        tree.StretchToParentSize();
        powerLabel.style.display = DisplayStyle.Flex;
        powerBar.style.display = DisplayStyle.Flex;
        powerLabel.text = powerName;
        if(currentPowerTween == null) before();
        var elapsed = currentPowerTween != null ? currentPowerTween.elapsed + currentPowerTween.tween.Elapsed() : 0 ;
        var startValue = currentPowerTween == null ? 100f : (powerSeconds - elapsed) * 100 / powerSeconds;
        var startSeconds = currentPowerTween == null ? powerSeconds : powerSeconds - elapsed;
        var powerTween = DOTween.To(() => startValue, x => powerBar.value = x, 0f, startSeconds).SetEase(Ease.Linear).OnComplete(() =>
        {
            powerLabel.style.display = DisplayStyle.None;
            powerBar.style.display = DisplayStyle.None;
            after();
            currentPowerTween = null;
        });
        if (currentPowerTween != null) currentPowerTween.tween.Kill();
        currentPowerTween = new PowerDetails() {
            powerName = powerName,
            powerSeconds = powerSeconds,
            before = before,
            after = after,
            tween = powerTween,
            elapsed = elapsed
        };
    }

    public void ShowAdTemplate(string header, UnityAction onSuccess, UnityAction onCancelled)
    {
        var tree = adTemplateDocument.CloneTree();
        tree.Q<Label>("header").text = header;
        tree.StretchToParentSize();
        tree.Q<Button>("no-button").clicked += () =>
        {
            onCancelled();
        };
        tree.Q<Button>("yes-button").clicked += () =>
        {
            onSuccess();
        };
        _uiDocument.rootVisualElement.Clear();
        _uiDocument.rootVisualElement.Add(tree);
    }
}

public class PowerDetails
{
    public string powerName;
    public float powerSeconds;
    public UnityAction before;
    public UnityAction after;
    public Tween tween;
    public float elapsed;
}
