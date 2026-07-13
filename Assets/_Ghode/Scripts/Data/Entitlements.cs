// Entitlements.cs — what the player OWNS (as opposed to what they chose).
// Today that is exactly one thing: the "Royal Stable" purchase, which
// unlocks the Ebony + Marble themes and removes ads forever.
// EntitlementsStore saves/loads this as entitlements.v1.json. The store
// (Google Play Billing via Unity IAP) is AUTHORITATIVE — this file is only
// the offline cache so an airplane-mode launch still honors the purchase.

using System;

namespace Ghode.Data
{
    /// <summary>
    /// The player's purchases, cached locally. BillingService refreshes this
    /// from the real store whenever it connects; the game reads it freely.
    /// </summary>
    [Serializable]
    public class Entitlements
    {
        /// <summary>The save-file format this class writes. Bump + migrate on change.</summary>
        public const int CurrentSchema = 1;

        /// <summary>Format stamp carried inside the save file (see SaveService).</summary>
        public int schemaVersion = CurrentSchema;

        /// <summary>
        /// The one-time "Royal Stable" purchase: Ebony + Marble themes
        /// unlocked, ads gone forever.
        /// </summary>
        public bool royalStable = false;
    }
}
