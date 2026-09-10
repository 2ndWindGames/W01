using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace SWGUnity2DCore.Manager
{
    public sealed class IAPManager
    {
        public const string RemoveAdsProductId = "com.secondwindgames.violettap.removeads";
        private const string RemoveAdsPlayerPrefsKey = "VioletTap.IAP.RemoveAds";

        private readonly W01AdsManager m_AdsManager;
        private StoreController m_StoreController;
        private bool m_Initialized;
        private bool m_Initializing;
        private bool m_Purchasing;

        public bool IsNoAds { get; private set; }
        public bool IsStoreReady { get; private set; }
        public bool IsPurchasing => m_Purchasing;
        public string RemoveAdsPrice { get; private set; } = string.Empty;

        public event Action<bool> NoAdsChanged;
        public event Action<bool> StoreReadyChanged;
        public event Action<string> PurchaseFailed;

        public IAPManager(W01AdsManager adsManager)
        {
            m_AdsManager = adsManager ?? throw new ArgumentNullException(nameof(adsManager));
            IsNoAds = PlayerPrefs.GetInt(RemoveAdsPlayerPrefsKey, 0) == 1;
            m_AdsManager.SetAdsDisabled(IsNoAds);
        }

        public void Init()
        {
            if (m_Initialized || m_Initializing) return;
            m_Initializing = true;
            InitializeStore();
        }

        private async void InitializeStore()
        {
            try
            {
                m_StoreController = UnityIAPServices.StoreController();
                m_StoreController.OnStoreConnected += OnStoreConnected;
                m_StoreController.OnStoreDisconnected += OnStoreDisconnected;
                m_StoreController.OnProductsFetched += OnProductsFetched;
                m_StoreController.OnProductsFetchFailed += OnProductsFetchFailed;
                m_StoreController.OnPurchasePending += OnPurchasePending;
                m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
                m_StoreController.OnPurchaseFailed += OnPurchaseFailed;
                m_StoreController.OnPurchaseDeferred += OnPurchaseDeferred;
                m_StoreController.OnCheckEntitlement += OnCheckEntitlement;
                await m_StoreController.Connect();
                m_Initialized = true;
            }
            catch (Exception exception)
            {
                m_Initializing = false;
                ReportFailure("스토어 연결에 실패했습니다: " + exception.Message);
            }
        }

        private void OnStoreConnected()
        {
            m_StoreController.FetchProducts(new List<ProductDefinition>
            {
                new ProductDefinition(RemoveAdsProductId, ProductType.NonConsumable)
            });
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            SetStoreReady(false);
            Debug.LogWarning("[IAP] 스토어 연결 해제: " + failure.message);
        }

        private void OnProductsFetched(List<Product> products)
        {
            var product = products.FirstOrDefault(item => item.definition.id == RemoveAdsProductId);
            if (product == null || !product.availableToPurchase)
            {
                ReportFailure("광고 제거 상품을 스토어에서 찾을 수 없습니다.");
                return;
            }

            RemoveAdsPrice = product.metadata.localizedPriceString;
            SetStoreReady(true);
            m_StoreController.CheckEntitlement(product);
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            SetStoreReady(false);
            ReportFailure("상품 정보를 가져오지 못했습니다: " + failure.FailureReason);
        }

        public bool PurchaseRemoveAds()
        {
            if (IsNoAds) return false;
            if (!IsStoreReady || m_Purchasing || m_StoreController == null)
            {
                ReportFailure("스토어가 아직 준비되지 않았습니다. 잠시 후 다시 시도해 주세요.");
                return false;
            }

            m_Purchasing = true;
            m_StoreController.PurchaseProduct(RemoveAdsProductId);
            return true;
        }

        public void RestorePurchases(Action<bool, string> callback = null)
        {
            if (!IsStoreReady || m_StoreController == null)
            {
                callback?.Invoke(false, "스토어가 아직 준비되지 않았습니다.");
                return;
            }

            m_StoreController.RestoreTransactions((success, message) =>
                callback?.Invoke(success, message ?? string.Empty));
        }

        private void OnPurchasePending(PendingOrder order)
        {
            var product = GetFirstProduct(order);
            if (product == null || product.definition.id != RemoveAdsProductId) return;
            GrantRemoveAds();
            m_StoreController.ConfirmPurchase(order);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            m_Purchasing = false;
            if (order is FailedOrder failedOrder)
                ReportFailure("구매 확정에 실패했습니다: " + failedOrder.Details);
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            m_Purchasing = false;
            ReportFailure($"구매에 실패했습니다: {order.FailureReason} ({order.Details})");
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            m_Purchasing = false;
            Debug.Log("[IAP] 구매 승인이 보류되었습니다.");
        }

        private void OnCheckEntitlement(Entitlement entitlement)
        {
            if (entitlement.Product == null || entitlement.Product.definition.id != RemoveAdsProductId) return;
            if (entitlement.Status == EntitlementStatus.FullyEntitled ||
                entitlement.Status == EntitlementStatus.EntitledButNotFinished)
                GrantRemoveAds();
        }

        private void GrantRemoveAds()
        {
            PlayerPrefs.SetInt(RemoveAdsPlayerPrefsKey, 1);
            PlayerPrefs.Save();
            if (IsNoAds) return;
            IsNoAds = true;
            m_AdsManager.SetAdsDisabled(true);
            NoAdsChanged?.Invoke(true);
            Debug.Log("[IAP] 광고 제거 권한이 적용되었습니다.");
        }

        private static Product GetFirstProduct(Order order) =>
            order.CartOrdered.Items().FirstOrDefault()?.Product;

        private void SetStoreReady(bool ready)
        {
            if (IsStoreReady == ready) return;
            IsStoreReady = ready;
            StoreReadyChanged?.Invoke(ready);
        }

        private void ReportFailure(string message)
        {
            m_Purchasing = false;
            Debug.LogWarning("[IAP] " + message);
            PurchaseFailed?.Invoke(message);
        }
    }
}
