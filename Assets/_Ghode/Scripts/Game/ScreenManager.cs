// ScreenManager.cs — the stage curtain operator.
// The game has four full-screen pages (Menu, Instructions, Playing, Result).
// This tiny class makes sure exactly ONE of them is visible at a time —
// nobody else ever calls SetActive on a screen root directly.

using System.Collections.Generic;
using UnityEngine;
using AppScreen = Ghode.Core.Screen; // our enum; alias avoids UnityEngine.Screen clashes

namespace Ghode.Game
{
    /// <summary>
    /// Keeps exactly one screen root GameObject active at a time.
    /// GameBootstrap registers the four roots at startup; GameController then
    /// calls <see cref="Show"/> whenever the player navigates.
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        // One entry per page: which enum value owns which root GameObject.
        readonly Dictionary<AppScreen, GameObject> _roots = new Dictionary<AppScreen, GameObject>();

        /// <summary>
        /// Tell the manager about a screen root. The root starts hidden;
        /// nothing shows until someone calls <see cref="Show"/>.
        /// </summary>
        public void Register(AppScreen screen, GameObject root)
        {
            _roots[screen] = root;
            root.SetActive(false);
        }

        /// <summary>Show this screen and hide every other registered one.</summary>
        public void Show(AppScreen screen)
        {
            // In plain words: walk every page and switch on only the chosen one.
            foreach (var pair in _roots)
            {
                pair.Value.SetActive(pair.Key == screen);
            }
        }
    }
}
