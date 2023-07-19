using UnityEngine;
using GooglePlayGames;
using GooglePlayGames.BasicApi;


public class PlayGames : MonoBehaviour
{
    private static PlayGames _instance;
    public static PlayGamesPlatform platform;

    public static PlayGames getInstance()
    {
        return _instance ? _instance : null;
    }
    void Awake()
    {
        _instance = this;
    }

    void Start()
    {
        if (platform == null)
        {
            PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder().Build();
            PlayGamesPlatform.InitializeInstance(config);
            PlayGamesPlatform.DebugLogEnabled = true;
            platform = PlayGamesPlatform.Activate();
        }

        Social.Active.localUser.Authenticate(success =>
        {
            if (success)
            {
                Debug.Log("Logged in successfully");
            }
            else
            {
                Debug.Log("Login Failed");
            }
        });
    }

    public void AddScoreToLeaderboard(int playerScore)
    {
        if (Social.Active.localUser.authenticated)
        {
            Social.ReportScore(playerScore, GPGSIds.leaderboard_high_score, success => { });
        }
    }

    public void ShowLeaderboard()
    {
        if (Social.Active.localUser.authenticated)
        {
            platform.ShowLeaderboardUI();
        }
    }

}