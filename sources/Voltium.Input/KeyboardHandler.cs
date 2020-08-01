using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerraFX.Interop;

namespace Voltium.Input
{
    //public enum KeyState
    //{
    //    Up,
    //    UpReleased,
    //    Down,
    //    DownPressed,
    //}

    /// <summary>
    /// A utility class for monitoring keyboard keys
    /// </summary>
    public static class KeyboardHandler
    {
        private static bool[] _keys = new bool[byte.MaxValue];
        private static bool[] _modifiers = new bool[byte.MaxValue];

        /// <summary>
        /// Tests whether a given <see cref="ConsoleKey"/> is pressed
        /// </summary>
        /// <param name="key">The <see cref="ConsoleKey"/> to test for</param>
        /// <returns><see langword="true"/> if <paramref name="key"/> is pressed, else <see langword="false"/></returns>
        public static bool IsKeyDown(ConsoleKey key) => _keys[(int)key];


        /// <summary>
        /// Tests whether a given <see cref="ConsoleModifiers"/> is pressed
        /// </summary>
        /// <param name="modifier">The <see cref="ConsoleModifiers"/> to test for</param>
        /// <returns><see langword="true"/> if <paramref name="modifier"/> is pressed, else <see langword="false"/></returns>
        public static bool IsModifierDown(ConsoleModifiers modifier) => _modifiers[(int)modifier];


        /// <summary>
        /// Sets the state for a given <see cref="ConsoleKey"/>
        /// </summary>
        /// <param name="key">The <see cref="ConsoleKey"/> to set the state for</param>
        /// <param name="isKeyDown">The <see cref="bool"/> state of <paramref name="key"/></param>
        public static void SetKeyState(ConsoleKey key, bool isKeyDown) => _keys[(int)key] = isKeyDown;

        /// <summary>
        /// Sets the state for a given <see cref="ConsoleModifiers"/>
        /// </summary>
        /// <param name="modifier">The <see cref="ConsoleModifiers"/> to set the state for</param>
        /// <param name="isModifierDown">The <see cref="bool"/> state of <paramref name="modifier"/></param>
        public static void SetModifierState(ConsoleModifiers modifier, bool isModifierDown) => _modifiers[(int)modifier] = isModifierDown;
    }

    //public enum MouseButton
    //{
    //    LeftClick,
    //    RightClick,
    //    ScrollClick,
    //    XButton1,
    //    XButton2
    //}

    //public static class MouseHandler
    //{
    //    private static bool[] _buttons = new bool[byte.MaxValue];

    //    public static (int X, int Y) CursorPosition { get; }


    //    /// <summary>
    //    /// Tests whether a given <see cref="ConsoleKey"/> is pressed
    //    /// </summary>
    //    /// <param name="key">The <see cref="ConsoleKey"/> to test for</param>
    //    /// <returns><see langword="true"/> if <paramref name="key"/> is pressed, else <see langword="false"/></returns>
    //    public static bool MouseButton(MouseButton button) => _buttons[(int)button];
    //}
}
