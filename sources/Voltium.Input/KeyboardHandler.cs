using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;

namespace Voltium.Input
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public enum KeyState
    {
        Up,
        UpReleased,
        Down,
        DownPressed,
    }

    /// <summary>
    /// A utility class for monitoring keyboard keys
    /// </summary>
    public static class KeyboardHandler
    {
        private static KeyState[] _keys = new KeyState[byte.MaxValue];
        private static KeyState[] _modifiers = new KeyState[byte.MaxValue];

        /// <summary>
        /// Tests whether a given <see cref="ConsoleKey"/> is pressed
        /// </summary>
        /// <param name="key">The <see cref="ConsoleKey"/> to test for</param>
        /// <returns><see langword="true"/> if <paramref name="key"/> is pressed, else <see langword="false"/></returns>
        public static KeyState Key(ConsoleKey key) => _keys[(int)key];

        /// <summary>
        /// Tests whether a given <see cref="ConsoleModifiers"/> is pressed
        /// </summary>
        /// <param name="modifier">The <see cref="ConsoleModifiers"/> to test for</param>
        /// <returns><see langword="true"/> if <paramref name="modifier"/> is pressed, else <see langword="false"/></returns>
        public static KeyState Modifier(ConsoleModifiers modifier) => _modifiers[(int)modifier];

        /// <summary>
        /// Sets the state for a given <see cref="ConsoleKey"/>
        /// </summary>
        /// <param name="key">The <see cref="ConsoleKey"/> to set the state for</param>
        /// <param name="isKeyDown">The <see cref="bool"/> state of <paramref name="key"/></param>
        public static void SetKeyState(ConsoleKey key, bool isKeyDown)
        {
            int index = (int)key;
            if (isKeyDown)
            {
                _keys[index] = _keys[index] is KeyState.Down or KeyState.DownPressed ? KeyState.Down : KeyState.DownPressed;
            }
            else
            {
                _keys[index] = _keys[index] is KeyState.Up or KeyState.UpReleased ? KeyState.Up : KeyState.UpReleased;
            }
        }

        /// <summary>
        /// Sets the state for a given <see cref="ConsoleModifiers"/>
        /// </summary>
        /// <param name="modifier">The <see cref="ConsoleModifiers"/> to set the state for</param>
        /// <param name="isModifierDown">The <see cref="bool"/> state of <paramref name="modifier"/></param>
        public static void SetModifierState(ConsoleModifiers modifier, bool isModifierDown)
        {
            int index = (int)modifier;
            if (isModifierDown)
            {
                _modifiers[index] = _modifiers[index] is KeyState.Down or KeyState.DownPressed ? KeyState.Down : KeyState.DownPressed;
            }
            else
            {
                _modifiers[index] = _modifiers[index] is KeyState.Up or KeyState.UpReleased ? KeyState.Up : KeyState.UpReleased;
            }
        }
    }

    public enum MouseButton : byte
    {
        LeftClick,
        RightClick,
        ScrollClick,
        XButton1,
        XButton2
    }

    //public static class MouseHandler
    //{
    //    private static KeyState[] _buttons = new KeyState[byte.MaxValue];

    //    public static (int X, int Y) CursorPosition { get; private set; }

    //    public static (int X, int Y) CursorDelta { get; private set; }

    //    /// <summary>
    //    /// Tests whether a given <see cref="ConsoleKey"/> is pressed
    //    /// </summary>
    //    /// <param name="key">The <see cref="ConsoleKey"/> to test for</param>
    //    /// <returns><see langword="true"/> if <paramref name="key"/> is pressed, else <see langword="false"/></returns>
    //    public static KeyState MouseButton(MouseButton button) => _buttons[(int)button];

    //    public static void SetCursorPosition(int x, int y)
    //    {
    //        CursorDelta = (x - CursorPosition.X, y - CursorPosition.Y);
    //        CursorPosition = (x, y);
    //    }

    //    public static void SetButtonState(MouseButton button, bool isButtonDown)
    //    {
    //        int index = (int)button;
    //        if (isButtonDown)
    //        {
    //            _buttons[index] = _buttons[index] is KeyState.Down or KeyState.DownPressed ? KeyState.Down : KeyState.DownPressed;
    //        }
    //        else
    //        {
    //            _buttons[index] = _buttons[index] is KeyState.Up or KeyState.UpReleased ? KeyState.Up : KeyState.UpReleased;
    //        }
    //    }
    //}

    public static class KeyStateExtensions
    {
        public static bool IsDown(this KeyState state) => state is KeyState.Down or KeyState.DownPressed;
        public static bool IsUp(this KeyState state) => !state.IsDown();

        public static bool HasChangedState(this KeyState state) => state is KeyState.DownPressed or KeyState.UpReleased;
    }
}
