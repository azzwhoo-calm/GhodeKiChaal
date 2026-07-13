// GameBootstrap.cs — the one script that makes pressing Play "just work".
// It wakes up by itself in ANY scene (even an empty one), builds the whole
// uGUI world in code — EventSystem, Canvas, safe area, all four screens —
// wires everything to the GameController, and lands on the Main Menu.
// Nothing needs to be dragged, clicked or saved in the Editor first.

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Ghode.Audio;
using Ghode.UI;
using AppScreen = Ghode.Core.Screen; // our enum; alias avoids UnityEngine.Screen clashes

namespace Ghode.Game
{
    /// <summary>
    /// Self-booting entry point. A [RuntimeInitializeOnLoadMethod] creates one
    /// of these automatically right after any scene loads, so the game runs
    /// with zero manual scene setup — our whole "testable from an empty scene"
    /// strategy hangs off this class.
    /// </summary>
    // Scene-first with a code-built fallback. When this component lives in the
    // Boot scene it reuses the managers wired there (see _controller / _audio),
    // and only the UI is still built in code; in a bare scene (an EditMode test,
    // or an empty SampleScene) it auto-creates everything, so "press Play from
    // nothing" keeps working exactly as before.
    //
    // TODO(azzwhoo): finish the migration in-Editor — turn the four code-built
    // screens into prefabs, drop them under a Canvas in Boot.unity, and wire the
    // roots here so BuildUi() can reuse them. The manager layer is already
    // scene-authored (Boot.unity → Bootstrap → GameManager / AudioManager).
    public class GameBootstrap : MonoBehaviour
    {
        // Managers authored in the Boot scene and assigned in the Inspector.
        // Left null in a bare scene, where ComposeManagers() creates them on the
        // fly. Unity's fake-null means an unassigned reference reads as == null
        // here — which is exactly the cue to fall back to code.
        [Header("Wired in Boot.unity — auto-built when absent")]
        [SerializeField] GameController _controller;
        [SerializeField] AudioManager _audio;

        // Guards against building the game twice (e.g. scene reloads).
        static bool _booted;

        // In plain words: if "Enter Play Mode Options" skips domain reload,
        // static fields survive between plays — this puts the flag back.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _booted = false;
        }

        /// <summary>
        /// Runs automatically after the first scene loads. If no bootstrap
        /// exists yet (nobody placed one in the scene), it creates itself.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (_booted) return;
            if (FindAnyObjectByType<GameBootstrap>(FindObjectsInactive.Include) != null) return; // scene already has one

            var go = new GameObject("~GhodeGame (auto-built)");
            go.AddComponent<GameBootstrap>(); // Awake below does the real work
        }

        void Awake()
        {
            if (_booted)
            {
                // A second copy (duplicate in scene, or scene reload) is unwanted.
                Destroy(gameObject);
                return;
            }
            _booted = true;
            DontDestroyOnLoad(gameObject); // survive any future scene loads
            BuildWorld();
        }

        // Builds and wires everything, in dependency order:
        // crash capture first (nothing may crash unrecorded), then managers
        // (reused from the scene when present), then the UI.
        void BuildWorld()
        {
            Ghode.Analytics.CrashReporting.Init();
            var controller = ComposeManagers();
            BuildUi(controller);
        }

        // --- The brains and the sound desk ---------------------------------
        // Reuse the GameController and AudioManager authored in the Boot scene;
        // create whichever one is missing so a bare scene still boots. Returns
        // the initialised controller for the UI step.
        GameController ComposeManagers()
        {
            if (_audio == null) _audio = FindOrCreate<AudioManager>();
            if (_controller == null) _controller = FindOrCreate<GameController>();
            _controller.Init(_audio); // loads saved settings + records
            _controller.ConnectServices(); // store + leaderboards (async, optional)
            return _controller;
        }

        // Finds a manager already living in the scene, or adds a fresh one to the
        // bootstrap object as a fallback. Includes inactive objects so a disabled
        // manager in the scene is reused rather than silently duplicated.
        T FindOrCreate<T>() where T : Component
        {
            var existing = FindAnyObjectByType<T>(FindObjectsInactive.Include);
            return existing != null ? existing : gameObject.AddComponent<T>();
        }

        // Builds the code-driven uGUI world (Canvas, safe area, the four screens)
        // and hands the screen switcher to the controller. This is the part still
        // awaiting the in-Editor prefab pass; until then it runs identically in
        // the Boot scene and in a bare test scene.
        void BuildUi(GameController controller)
        {
            // --- Input plumbing --------------------------------------------
            EnsureEventSystem();

            // An empty scene has no camera, hence no AudioListener — add one so
            // Unity does not log "no audio listener" warnings every frame.
            if (FindAnyObjectByType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }

            // --- The Canvas (portrait phone, 1080×1920 reference) -----------
            var canvasGo = new GameObject("Canvas (built by GameBootstrap)");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f); // design in portrait phone pixels
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f; // balance width/height so odd aspect ratios stay sane

            canvasGo.AddComponent<GraphicRaycaster>();

            // Full-bleed backdrop so notch/safe-area gutters look intentional
            // instead of showing random camera background.
            var backdrop = UiFactory.CreatePanel("Backdrop", canvasGo.transform, UiFactory.Palette.WalnutDeep);
            UiFactory.Fill(backdrop.rectTransform);

            // Everything interactive lives inside the phone's safe area
            // (dodges notches, punch-holes and gesture bars).
            var safeArea = UiFactory.CreateRect("SafeArea", canvasGo.transform);
            UiFactory.Fill(safeArea);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            // --- The four screens -------------------------------------------
            var screens = gameObject.AddComponent<ScreenManager>();
            screens.Register(AppScreen.Menu, MainMenuScreen.Build(safeArea, controller).gameObject);
            screens.Register(AppScreen.Instructions, InstructionsScreen.Build(safeArea, controller).gameObject);
            screens.Register(AppScreen.Playing, GameScreen.Build(safeArea, controller).gameObject);
            screens.Register(AppScreen.Result, ResultScreen.Build(safeArea, controller).gameObject);

            // --- Lights on ---------------------------------------------------
            controller.AttachScreens(screens); // lands on the Main Menu
        }

        // Makes sure the scene has an EventSystem WITH a working input module.
        // In plain words: an EventSystem without an input module looks perfectly
        // fine in the Hierarchy but silently ignores every tap and click. Some
        // of our scenes contain placeholder EventSystem objects exactly like
        // that — so we never trust an existing one blindly. If the module is
        // missing, we bolt it on right here.
        void EnsureEventSystem()
        {
            var existing = FindAnyObjectByType<EventSystem>();
            GameObject host;
            if (existing != null)
            {
                host = existing.gameObject; // reuse it — two EventSystems fight
            }
            else
            {
                host = new GameObject("EventSystem (built by GameBootstrap)");
                host.transform.SetParent(transform, false);
                host.AddComponent<EventSystem>();
            }

            // Drag starts after 12 dp of movement (the spec'd threshold):
            // shorter gestures stay taps. dp → px via the device's dpi, with
            // 160 (= 1 dp per px) as the fallback when dpi is unknown.
            float dpi = UnityEngine.Screen.dpi > 0f ? UnityEngine.Screen.dpi : 160f;
            host.GetComponent<EventSystem>().pixelDragThreshold =
                Mathf.RoundToInt(12f * dpi / 160f);

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            // This project uses the new Input System exclusively
            // (ProjectSettings → activeInputHandler = 1). Only an
            // InputSystemUIInputModule can feed uGUI here — a legacy
            // StandaloneInputModule would just log an error and disable itself.
            var module = host.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (module == null)
            {
                module = host.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // A module added from code starts with NO actions asset (the editor
            // normally assigns one via its Reset callback, which never runs at
            // runtime). Assign the defaults explicitly: they bind mouse, pen AND
            // touch — touch is what the Device Simulator feeds when you click
            // with the mouse, so this line is what makes the Simulator work.
            if (module.actionsAsset == null)
            {
                module.AssignDefaultActions();
            }
#else
            if (host.GetComponent<StandaloneInputModule>() == null)
            {
                host.AddComponent<StandaloneInputModule>();
            }
#endif
        }
    }
}
