// SegmentedControl.cs — a "pick exactly one" row of buttons, like 5×5 | 6×6 | 8×8.
// The main menu uses it for board size and difficulty. Tapping an option
// highlights it and reports the chosen index; setting it from code only
// repaints (no callback), so saved settings can be shown without echo loops.

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ghode.UI
{
    /// <summary>
    /// A row of equally-sized option buttons where exactly one is selected.
    /// Build it with <see cref="Build"/>; show a saved choice with
    /// <see cref="SetSelected"/> (repaint only — it never fires the callback).
    /// </summary>
    public class SegmentedControl : MonoBehaviour
    {
        Image[] _faces;
        Text[] _labels;
        int _selected = -1;
        Action<int> _onSelect;

        /// <summary>Create the control with one button per option string.</summary>
        public static SegmentedControl Build(RectTransform parent, string name, string[] options, Action<int> onSelect)
        {
            var rt = UiFactory.CreateRect(name, parent);
            UiFactory.Layout(rt, preferredHeight: 110f);
            UiFactory.HStack(rt, 12f, new RectOffset(0, 0, 0, 0));

            var control = rt.gameObject.AddComponent<SegmentedControl>();
            control._onSelect = onSelect;
            control._faces = new Image[options.Length];
            control._labels = new Text[options.Length];

            for (int i = 0; i < options.Length; i++)
            {
                int index = i; // capture a fresh copy for the click closure
                var button = UiFactory.CreateButton("Option " + options[i], rt, options[i], 40,
                    () => control.HandleTap(index));
                UiFactory.Layout(button, flexibleWidth: 1f); // equal widths
                control._faces[i] = button.GetComponent<Image>();
                control._labels[i] = button.GetComponentInChildren<Text>();
            }

            control.SetSelected(0);
            return control;
        }

        // A button was tapped: repaint AND tell the owner (unless nothing changed).
        void HandleTap(int index)
        {
            if (index == _selected) return;
            SetSelected(index);
            _onSelect?.Invoke(index);
        }

        /// <summary>
        /// Highlight one option WITHOUT firing the callback — used to display
        /// saved settings. (Selected = gold face; others = plain walnut.)
        /// </summary>
        public void SetSelected(int index)
        {
            _selected = index;
            for (int i = 0; i < _faces.Length; i++)
            {
                bool selected = i == index;
                _faces[i].color = selected ? UiFactory.Palette.Accent : UiFactory.Palette.ButtonFace;
                _labels[i].color = selected ? UiFactory.Palette.Walnut : UiFactory.Palette.Parchment;
                _labels[i].fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
            }
        }
    }
}
