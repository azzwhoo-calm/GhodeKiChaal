// ToggleControl.cs — a labeled on/off switch row, e.g. "Sound   [On]".
// The pause overlay uses three of these for live settings. Tapping flips the
// value and reports it; setting it from code only repaints (no callback),
// exactly like SegmentedControl, so there are no feedback loops.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ghode.UI
{
    /// <summary>
    /// A row with a text label on the left and an On/Off button on the right.
    /// Show a saved value with <see cref="SetValue"/> (repaint only); the
    /// callback fires only on real taps.
    /// </summary>
    public class ToggleControl : MonoBehaviour
    {
        Action<bool> _onChanged;
        bool _value;
        Image _face;
        Text _stateLabel;

        /// <summary>Create the row: "label ......... [On/Off]".</summary>
        public static ToggleControl Build(RectTransform parent, string name, string label, Action<bool> onChanged)
        {
            var rt = UiFactory.CreateRect(name, parent);
            UiFactory.Layout(rt, preferredHeight: 96f);
            UiFactory.HStack(rt, 16f, new RectOffset(0, 0, 0, 0));

            var control = rt.gameObject.AddComponent<ToggleControl>();
            control._onChanged = onChanged;

            // The wordy left side takes two thirds of the row...
            var caption = UiFactory.CreateText("Caption", rt, label, 42, UiFactory.Palette.Parchment, TextAnchor.MiddleLeft);
            UiFactory.Layout(caption, flexibleWidth: 2f);

            // ...and the tappable switch takes the rest. The explicit height
            // matters: the row's HStack CONTROLS child heights, and a plain
            // Image has no preferred size of its own — without this the
            // switch face collapses to 0 px tall (only its overflowing text
            // kept it findable at all).
            var button = UiFactory.CreateButton("Switch", rt, "Off", 40, control.HandleTap);
            UiFactory.Layout(button, preferredHeight: 80f, flexibleWidth: 1f);
            control._face = button.GetComponent<Image>();
            control._stateLabel = button.GetComponentInChildren<Text>();

            control.SetValue(false);
            return control;
        }

        // The switch was tapped: flip, repaint, and tell the owner.
        void HandleTap()
        {
            SetValue(!_value);
            _onChanged?.Invoke(_value);
        }

        /// <summary>Show a value WITHOUT firing the callback (for saved settings).</summary>
        public void SetValue(bool value)
        {
            _value = value;
            // In plain words: gold + "On" when active, plain wood + "Off" when not.
            _face.color = value ? UiFactory.Palette.Accent : UiFactory.Palette.ButtonFace;
            _stateLabel.text = value ? "On" : "Off";
            _stateLabel.color = value ? UiFactory.Palette.Walnut : UiFactory.Palette.ParchmentDim;
            _stateLabel.fontStyle = value ? FontStyle.Bold : FontStyle.Normal;
        }
    }
}
