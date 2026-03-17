using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Strategy pattern interface for in-app purchase platforms.
    /// Game code sets platform-specific handlers via callbacks.
    /// </summary>
    public class IAPProvider
    {
        /// <summary>Fired when a purchase completes. (productId, receipt)</summary>
        public event Action<string, Dictionary<string, object>> PurchaseCompleted;

        /// <summary>Fired when a purchase fails. (productId, error)</summary>
        public event Action<string, string> PurchaseFailed;

        /// <summary>Injected: get available products.</summary>
        public Func<List<Dictionary<string, object>>> OnGetProducts { get; set; }

        /// <summary>Injected: initiate purchase. (productId)</summary>
        public Action<string> OnPurchase { get; set; }

        /// <summary>Injected: restore previous purchases.</summary>
        public Action OnRestorePurchases { get; set; }

        public List<Dictionary<string, object>> GetProducts()
        {
            return OnGetProducts?.Invoke() ?? new List<Dictionary<string, object>>();
        }

        public void Purchase(string productId)
        {
            if (OnPurchase == null)
            {
                PurchaseFailed?.Invoke(productId, "No IAP provider configured");
                return;
            }
            OnPurchase.Invoke(productId);
        }

        public void RestorePurchases() => OnRestorePurchases?.Invoke();

        /// <summary>Call from platform-specific code when purchase succeeds.</summary>
        public void NotifyPurchaseSuccess(string productId, Dictionary<string, object> receipt = null)
        {
            PurchaseCompleted?.Invoke(productId, receipt ?? new Dictionary<string, object>());
        }

        /// <summary>Call from platform-specific code when purchase fails.</summary>
        public void NotifyPurchaseError(string productId, string error)
        {
            PurchaseFailed?.Invoke(productId, error);
        }
    }
}
