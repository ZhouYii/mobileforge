using System;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Strategy pattern for rewarded ad networks.
    /// Pure C# — delegates to platform-specific provider via callbacks.
    /// </summary>
    public class RewardedAds
    {
        public event Action<string> AdCompleted;
        public event Action<string, string> AdFailed;
        public event Action<string> AdSkipped;

        /// <summary>Injected: check if ad is ready. (placementId) -> bool</summary>
        public Func<string, bool> OnIsReady { get; set; }

        /// <summary>Injected: show ad. (placementId)</summary>
        public Action<string> OnShow { get; set; }

        /// <summary>Injected: preload ad. (placementId)</summary>
        public Action<string> OnLoadAd { get; set; }

        public bool IsReady(string placementId) => OnIsReady?.Invoke(placementId) ?? false;

        public void Show(string placementId)
        {
            if (OnShow == null)
            {
                AdFailed?.Invoke(placementId, "No ad provider configured");
                return;
            }
            OnShow.Invoke(placementId);
        }

        public void LoadAd(string placementId) => OnLoadAd?.Invoke(placementId);

        public void NotifyCompleted(string placementId) => AdCompleted?.Invoke(placementId);
        public void NotifyFailed(string placementId, string error) => AdFailed?.Invoke(placementId, error);
        public void NotifySkipped(string placementId) => AdSkipped?.Invoke(placementId);
    }
}
