using System;
using UnityEngine;
using UnityEngine.Advertisements;
using UnityEngine.Events;

public class AdsManager : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsListener
{
    private static AdsManager _instance;

    [SerializeField] string _androidGameId;
    [SerializeField] string _iOsGameId;
    [SerializeField] bool _testMode = true;
    [SerializeField] bool _enablePerPlacementMode = true;
    private string _gameId;

    [SerializeField] string _androidInterstitialAdUnitId = "Interstitial_Android";
    [SerializeField] string _iOsInterstitialAdUnitId = "Interstitial_iOS";
    string _interstitialAdUnitId;

    [SerializeField] string _androidRewardedAdUnitId = "Rewarded_Android";
    [SerializeField] string _iOsRewardedAdUnitId = "Rewarded_iOS";
    string _rewardedAdUnitId;

    private DateTime? lastAdShowed = null;

    private UnityAction rewardAction;

    public static AdsManager getInstance()
    {
        return _instance ? _instance : null;
    }
    
    void Awake()
    {
        _instance = this;
        InitializeAds();
        _interstitialAdUnitId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? _iOsInterstitialAdUnitId
            : _androidInterstitialAdUnitId;
        _rewardedAdUnitId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? _iOsRewardedAdUnitId
            : _androidRewardedAdUnitId;
    }

    public void InitializeAds()
    {
        _gameId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? _iOsGameId
            : _androidGameId;
        Advertisement.Initialize(_gameId, _testMode, _enablePerPlacementMode, this);
        Advertisement.AddListener(this);
    }

    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads initialization complete.");
        LoadAd();
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
    }

    public void LoadAd()
    {
        Debug.Log("Loading Ad: " + _interstitialAdUnitId);
        Advertisement.Load(_interstitialAdUnitId, this);
        Debug.Log("Loading Ad: " + _rewardedAdUnitId);
        Advertisement.Load(_rewardedAdUnitId, this);
    }

    public void ShowAd()
    {
        if (lastAdShowed == null || lastAdShowed.GetValueOrDefault().AddMinutes(5) < DateTime.Now)
        {
            Debug.Log("Showing Ad: " + _interstitialAdUnitId);
            Advertisement.Show(_interstitialAdUnitId);
            lastAdShowed = DateTime.Now;
        }
    }

    public void ShowRewardAd(UnityAction action){
        Debug.Log("Showing Ad: " + _rewardedAdUnitId);
        Advertisement.Show(_rewardedAdUnitId);
        rewardAction = action;
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.Log($"Error loading Ad Unit: {adUnitId} - {error.ToString()} - {message}");
    }

    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.Log($"Error showing Ad Unit {adUnitId}: {error.ToString()} - {message}");
    }

    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Debug.Log("Ad Loaded: " + adUnitId);
    }

    public void OnUnityAdsReady(string placementId)
    {
    }

    public void OnUnityAdsDidError(string message)
    {
    }

    public void OnUnityAdsDidStart(string placementId)
    {
    }

    public void OnUnityAdsDidFinish(string adUnitId, ShowResult showResult)
    {
        Debug.Log("OnUnityAdsDidFinish: "+ adUnitId);
        if (adUnitId.Equals(_rewardedAdUnitId) && showResult == ShowResult.Finished)
        {
            Debug.Log("Unity Ads Rewarded Ad Completed");
            rewardAction();
            Advertisement.Load(_rewardedAdUnitId, this);
        }
    }
}
