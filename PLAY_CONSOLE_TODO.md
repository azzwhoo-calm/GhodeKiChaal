# Account-Side Ledger — everything that needs a human with credentials

The codebase is COMPLETE up to the SDK drop-ins: every external service hides
behind a tested seam, and each seam's file header carries its own
`TODO(azzwhoo)` drop-in instructions. This file is the master checklist of
the account work, the ids to collect, and where each one plugs in.

Legend: ⏳ = has a real waiting period, start early.

---

## 1. Google Play Console (gates everything) ⏳

- [ ] Register the developer account at play.google.com/console ($25 one-time).
      Identity verification can take days.
- [ ] Create the app: "Ghode Ki Chaal" · en-US · Game → Puzzle · Free.
      Package id is already `com.azzwhoo.ghodekichaal` in the project.
- [ ] ⏳ **The 14-day clock:** personal accounts need a closed test with
      **12+ testers opted in for 14 continuous days** before production.
      Recruit 16–20 testers NOW; check the count DAILY (dips reset progress).
- [ ] Run `Tools/create-release-keystore.ps1` (local machine), back the
      keystore up twice, select it in Player Settings → Publishing Settings.
- [ ] Build the AAB (menu: **Ghode → Build Android AAB**, or the batchmode
      command in DEVELOPMENT.md §5) and upload to the closed track.
- [ ] Enroll in Play App Signing during the first upload.
- [ ] Verify on the FIRST installed build: R8 minification (one full game +
      one purchase — see Assets/Plugins/Android/proguard-user.txt), haptic
      patterns feel right, themes on OLED, 60 fps on the 8×8.

## 2. Merchant profile + the product (billing) ⏳

- [ ] Play Console → set up the payments/merchant profile (tax details;
      approval takes days — the plan puts this on Day 1).
- [ ] Create in-app product, id EXACTLY **`royal_stable`** (non-consumable),
      set the price, activate. The id is hardcoded in
      `Monetization/BillingService.cs` — nothing to change in code.
- [ ] Add testers as **license testers** (test purchases won't charge them).
- [ ] Monetization setup → copy the **licensing key** → hand to dev for
      receipt validation (TODO(azzwhoo) in BillingService: Receipt Validation
      Obfuscator → `GooglePlayTangle` → `CrossPlatformValidator`).

## 3. Play Games Services (leaderboards)

- [ ] Play Console → Grow → Play Games Services → create the configuration.
- [ ] Create FOUR leaderboards (best time per size: 5×5, 6×6, 7×7, 8×8),
      lower-is-better, time format.
- [ ] Collect: **GPGS app id** + four **leaderboard ids** (`CgkI…`).
- [ ] Dev drop-in: import the Play Games plugin, write
      `PlayGamesLeaderboardBackend : ILeaderboardBackend`, map the ids in
      `LeaderboardService.BoardIdFor` (instructions in that file's header),
      uncomment the GPGS block in proguard-user.txt.

## 4. AdMob + UMP (ads)

- [ ] Create the AdMob account, register the app, link it to the Play
      listing once live.
- [ ] Collect: **AdMob app id** (`ca-app-pub-…~…`) + one **interstitial ad
      unit id**. (Code ships with Google's TEST ids until the RC.)
- [ ] Set up the UMP consent form in AdMob (EEA messaging).
- [ ] Dev drop-in: import the Google Mobile Ads plugin, write
      `AdMobInterstitialProvider : IInterstitialProvider` incl. the UMP flow
      (instructions in `Monetization/AdsService.cs`), uncomment the AdMob
      block in proguard-user.txt. The caps/entitlement logic doesn't change.

## 5. Firebase (analytics + crashes)

- [ ] Create the Firebase project, add the Android app
      (`com.azzwhoo.ghodekichaal`), download **google-services.json**.
- [ ] Dev drop-in: import FirebaseAnalytics + FirebaseCrashlytics packages,
      put google-services.json under Assets/, write
      `FirebaseAnalyticsBackend : IAnalyticsBackend` (event schema is
      documented in `Analytics/AnalyticsService.cs` — names/params are
      already Firebase-shaped), uncomment the Firebase proguard block.
- [ ] Verify events in DebugView + one test crash symbolicated on a device.

## 6. Listing & compliance (RC gate — from the tracker's Release tab)

- [ ] Privacy policy page live (declare Analytics, Crashlytics, AdMob,
      Billing). A GitHub Pages page is fine.
- [ ] Store listing: title ≤30, short ≤80, full description, icon 512,
      feature graphic 1024×500, 6–8 screenshots.
- [ ] Content rating questionnaire (expect Everyone).
- [ ] Data safety form: Firebase + AdMob + purchase history.
- [ ] Ads declaration: yes · target audience 13+ · app access: no credentials.
- [ ] Day ~17: apply for production (answer the questionnaire with specifics
      from the closed test); on approval promote the RC with a staged
      rollout 20→50→100% watching vitals (crash <1.09%, ANR <0.47%).

---

## The hand-back table (what the dev needs, one line each)

| Id / artifact | From | Plugs into |
|---|---|---|
| Keystore selected in Unity | you, locally | first uploadable AAB |
| Licensing key | Console → Monetization setup | BillingService receipt validation |
| GPGS app id + 4 leaderboard ids | Console → Play Games Services | PlayGamesLeaderboardBackend |
| AdMob app id + interstitial unit id | AdMob | AdMobInterstitialProvider |
| google-services.json | Firebase console | Assets/ + FirebaseAnalyticsBackend |
| Privacy policy URL | you | listing, data safety, UMP |
