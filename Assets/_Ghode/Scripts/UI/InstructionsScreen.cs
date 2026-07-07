// InstructionsScreen.cs — the how-to-play page.
// Plain friendly text explaining the horse's L-shaped hop, the goal, getting
// stuck, hints, and the three difficulties — plus a Back button. That's it.

using UnityEngine;
using UnityEngine.UI;
using Ghode.Game;

namespace Ghode.UI
{
    /// <summary>
    /// A static page of rules text with a Back button. Reachable from the
    /// main menu; Back returns there via <see cref="GameController.CloseInstructions"/>.
    /// </summary>
    public class InstructionsScreen : MonoBehaviour
    {
        // The whole rulebook, written so a curious 10-year-old gets it.
        const string RulesText =
            "The horse hops in an L shape: two squares one way, then one square " +
            "sideways — the famous \"ghode ki chaal\".\n\n" +
            "1. Tap any square to set the horse down. That square becomes move 1.\n\n" +
            "2. Hop again and again. Every hop must land on a square you have NOT " +
            "visited yet — stamped squares are locked.\n\n" +
            "3. Stamp every square on the board to win!\n\n" +
            "Stuck with no hops left? The board stays exactly as it is, so you can " +
            "study your trail — Undo to hop backwards, or Restart to try fresh.\n\n" +
            "Hint marks the smartest hop (it follows Warnsdorff's rule: visit the " +
            "cramped squares first) and counts how many hints you used.\n\n" +
            "Apprentice shows every legal hop AND the smartest one.  " +
            "Knight shows every legal hop.  " +
            "Master shows nothing at all — you spot the L-shapes yourself.";

        GameController _gc;

        /// <summary>Create the instructions page under the safe area.</summary>
        public static InstructionsScreen Build(RectTransform parent, GameController gc)
        {
            var root = UiFactory.CreatePanel("InstructionsScreen", parent, UiFactory.Palette.WalnutDeep);
            UiFactory.Fill(root.rectTransform);

            var screen = root.gameObject.AddComponent<InstructionsScreen>();
            screen._gc = gc;

            UiFactory.VStack(root.rectTransform, 24f, new RectOffset(90, 90, 60, 60));

            var title = UiFactory.CreateText("Title", root.transform, "How to Play", 72,
                UiFactory.Palette.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.Layout(title, preferredHeight: 100f);

            // TODO(azzwhoo): add the little knight-move diagram here — a mini 5×5
            // grid image with the L-shaped hop drawn on it (art pending).

            var body = UiFactory.CreateText("Rules", root.transform, RulesText, 40,
                UiFactory.Palette.Parchment, TextAnchor.UpperLeft);
            UiFactory.Layout(body, flexibleHeight: 1f); // soak up the middle of the page

            var back = UiFactory.CreateButton("BackButton", root.transform, "Back", 46, gc.CloseInstructions);
            UiFactory.Layout(back, preferredHeight: 110f);

            return screen;
        }
    }
}
