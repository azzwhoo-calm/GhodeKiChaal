// BillingService.cs — the game's one door to real-money purchases (M4).
// One product exists: the non-consumable "Royal Stable" (royal_stable),
// which unlocks the Ebony + Marble themes and removes ads forever.
// Built on Unity IAP v5 (StoreController): connect → fetch products →
// fetch purchases; buying uses the mandatory two-step flow (OnPurchasePending
// → grant → ConfirmPurchase). In the editor Unity IAP runs its fake store,
// so the whole purchase pipeline is exercisable without a device.
//
// The game itself never talks to this class about THEME rules — it only
// hears "royal stable owned: yes/no" via OnEntitlementChanged; the
// GameController persists that to entitlements.v1.json and repaints.
//
// TODO(azzwhoo): before RC — add receipt validation (CrossPlatformValidator
// with GooglePlayTangle from the Receipt Validation Obfuscator) inside
// GrantIfRoyalStable, and create the royal_stable product (with its price)
// in the Play Console.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Ghode.Monetization
{
    /// <summary>
    /// Wraps Unity IAP for our single product. Lifecycle: construct, hook
    /// <see cref="OnEntitlementChanged"/>, then <see cref="Connect"/> once
    /// (GameBootstrap does this in play mode only — EditMode tests never
    /// touch the store). All events arrive on the main thread.
    /// </summary>
    public class BillingService
    {
        /// <summary>The one product id (same on every store).</summary>
        public const string RoyalStableId = "royal_stable";

        /// <summary>Fired with true when the Royal Stable is (re)delivered.</summary>
        public event Action<bool> OnEntitlementChanged;

        /// <summary>Human-readable store state for the shop UI.</summary>
        public string Status { get; private set; } = "Not connected";

        /// <summary>Can we start a purchase right now?</summary>
        public bool ReadyToBuy { get; private set; }

        /// <summary>Localized price ("₹150.00") once products are fetched.</summary>
        public string RoyalStablePrice { get; private set; } = "";

        StoreController _store;

        /// <summary>
        /// Connect to the platform store (fake store in the editor). Safe to
        /// call once; failures leave the game fully playable on the cached
        /// entitlement.
        /// </summary>
        public async void Connect()
        {
            if (_store != null) return;
            Status = "Connecting…";

            try
            {
                _store = UnityIAPServices.StoreController();

                // Subscribe to EVERYTHING before Connect — a pending purchase
                // from a crashed session can fire the moment we reconnect.
                _store.OnPurchasePending += HandlePurchasePending;
                _store.OnPurchaseConfirmed += HandlePurchaseConfirmed;
                _store.OnPurchaseFailed += failed =>
                    Status = "Purchase failed: " + failed.FailureReason;
                _store.OnPurchaseDeferred += _ =>
                    Status = "Purchase awaiting approval"; // e.g. parental approval

                _store.OnProductsFetched += HandleProductsFetched;
                _store.OnProductsFetchFailed += failure =>
                    Status = "Product fetch failed: " + failure.FailureReason;

                _store.OnPurchasesFetched += HandlePurchasesFetched;
                _store.OnPurchasesFetchFailed += failure =>
                    Status = "Purchase fetch failed: " + failure.Message;

                _store.OnStoreConnected += HandleStoreConnected;
                _store.OnStoreDisconnected += failure =>
                {
                    ReadyToBuy = false;
                    Status = "Store offline: " + failure.Message;
                };

                await _store.Connect();
            }
            catch (Exception e)
            {
                // No store, no crash: the cached entitlement carries the day.
                ReadyToBuy = false;
                Status = "Store unavailable (" + e.Message + ")";
                Debug.LogWarning("BillingService: " + Status);
            }
        }

        /// <summary>Start the Royal Stable purchase (opens the store dialog).</summary>
        public void BuyRoyalStable()
        {
            if (_store == null || !ReadyToBuy)
            {
                Status = "Store not ready — try again in a moment";
                return;
            }
            Status = "Purchasing…";
            _store.PurchaseProduct(RoyalStableId);
        }

        /// <summary>
        /// Re-deliver old purchases (required UX on every store; each one
        /// arrives through the normal OnPurchasePending path).
        /// </summary>
        public void RestorePurchases(Action<bool> done = null)
        {
            if (_store == null)
            {
                Status = "Store not ready — try again in a moment";
                done?.Invoke(false);
                return;
            }
            Status = "Restoring…";
            _store.RestoreTransactions((success, error) =>
            {
                Status = success ? "Restore complete" : "Restore failed: " + error;
                done?.Invoke(success);
            });
        }

        // ------------------------------------------------------------------
        // Store event handlers
        // ------------------------------------------------------------------

        void HandleStoreConnected()
        {
            Status = "Fetching products…";
            _store.FetchProducts(new List<ProductDefinition>
            {
                new ProductDefinition(RoyalStableId, ProductType.NonConsumable)
            });
        }

        void HandleProductsFetched(List<Product> products)
        {
            var royal = products.FirstOrDefault(p => p.definition.id == RoyalStableId);
            if (royal != null)
            {
                RoyalStablePrice = royal.metadata != null ? royal.metadata.localizedPriceString : "";
                ReadyToBuy = true;
                Status = "Ready";
            }
            else
            {
                Status = "Product missing from store";
            }

            // Now learn about purchases from previous sessions/devices.
            // (Pending orders from a crashed session re-deliver automatically.)
            _store.FetchPurchases();
        }

        void HandlePurchasesFetched(Orders orders)
        {
            // A confirmed royal_stable from any earlier session = still owned.
            foreach (var confirmed in orders.ConfirmedOrders)
            {
                if (OrderContainsRoyalStable(confirmed)) RaiseEntitled();
            }
        }

        void HandlePurchasePending(PendingOrder pending)
        {
            // Two-step flow: grant FIRST, then confirm — if we die in between,
            // the store re-delivers this pending order on the next launch.
            if (OrderContainsRoyalStable(pending))
            {
                RaiseEntitled();
            }
            _store.ConfirmPurchase(pending);
        }

        void HandlePurchaseConfirmed(Order order)
        {
            switch (order)
            {
                case ConfirmedOrder _:
                    Status = "Purchase complete — enjoy!";
                    break;
                case FailedOrder failed:
                    // The grant already happened (and re-delivery will retry
                    // the confirmation) — just surface the store's complaint.
                    Status = "Confirmation failed: " + failed.FailureReason;
                    break;
            }
        }

        static bool OrderContainsRoyalStable(Order order)
        {
            return order.CartOrdered.Items()
                .Any(item => item.Product != null
                    && item.Product.definition.id == RoyalStableId);
        }

        void RaiseEntitled()
        {
            OnEntitlementChanged?.Invoke(true);
        }
    }
}
