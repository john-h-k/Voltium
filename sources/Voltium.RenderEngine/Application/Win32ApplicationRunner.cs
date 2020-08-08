using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using TerraFX.Interop;
using Voltium.Core.Devices;
using Voltium.Input;
using static TerraFX.Interop.Windows;

namespace Voltium.Core
{
    /// <summary>
    /// Used for creation and execution of Win32 backed applications
    /// </summary>
    internal unsafe static class Win32ApplicationRunner
    {
        private static readonly delegate* stdcall<IntPtr, uint, nuint, nint, nint> WindowProcHandle =
            (delegate* stdcall<IntPtr, uint, nuint, nint, nint>)(delegate*<IntPtr, uint, nuint, nint, nint>)&WindowProc;

        private static bool _isResizing = false;
        private static bool _isPaused = false;
        private static HWND Hwnd;

        private static Size _screenData;

        public static int Run(Application application, uint width, uint height)
        {
            var hInstance = GetModuleHandleW(null);
            _application = application;

            fixed (char* name = application.Name)
            fixed (char* windowsTitle = application.WindowTitle)
            {
                // Initialize the window class.
                var windowClass = new WNDCLASSEXW
                {
                    cbSize = (uint)sizeof(WNDCLASSEXW),
                    style = CS_HREDRAW | CS_VREDRAW,
                    lpfnWndProc = WindowProcHandle,
                    hInstance = hInstance,
                    hCursor = LoadCursorW(IntPtr.Zero, (ushort*)IDC_ARROW),
                    lpszClassName = (ushort*)name
                };
                _ = RegisterClassExW(&windowClass);

                var windowRect = new Rectangle(0, 0, (int)width, (int)height);
                _ = AdjustWindowRect((RECT*)&windowRect, WS_OVERLAPPEDWINDOW, FALSE);

                height = (uint)(windowRect.Bottom - windowRect.Top);
                width = (uint)(windowRect.Right - windowRect.Left);
                // Create the window and store a handle to it.
                Hwnd = CreateWindowExW(
                    0,
                    windowClass.lpszClassName,
                    (ushort*)windowsTitle,
                    WS_OVERLAPPEDWINDOW,
                    CW_USEDEFAULT,
                    CW_USEDEFAULT,
                    (int)width,
                    (int)height,
                    HWND.NULL,                              // We have no parent window.
                    HMENU.NULL,                             // We aren't using menus.
                    hInstance,
                    (void*)GCHandle.ToIntPtr(GCHandle.Alloc(application))
                );
            }

            _screenData = new Size((int)height, (int)width);

            application.Initialize(_screenData, IOutputOwner.FromHwnd(Hwnd));

            _ = ShowWindow(Hwnd, SW_SHOWDEFAULT);

            _timer = ApplicationTimer.StartNew();
            _timer.TargetElapsedSeconds = 1 / 500d;
            _timer.IsFixedTimeStep = false;

            // Main sample loop.
            MSG msg = default;

            // Process any messages in the queue.
            while (msg.message != WM_QUIT)
            {
                if (PeekMessageW(&msg, HWND.NULL, 0, 0, PM_REMOVE) != 0)
                {
                    _ = TranslateMessage(&msg);
                    _ = DispatchMessageW(&msg);
                }
                else if (!_isPaused)
                {
                    RunApp();
                }
                else
                {
                    Thread.Sleep(100);
                }
            }

            application.Dispose();

            // Return this part of the WM_QUIT message to Windows.
            return (int)msg.wParam;
        }

        private static double _lastUpdatedTitle = -1;
        private static void RunApp()
        {
            // Update window title approx every second
            if (_timer.TotalSeconds - _lastUpdatedTitle >= 1)
            {
                _lastUpdatedTitle = _timer.TotalSeconds;
                fixed (char* pTitle = _application.WindowTitle + " FPS: " + _timer.FramesPerSeconds.ToString())
                {
                    int result = SetWindowTextW(Hwnd, (ushort*)pTitle);
                    Debug.Assert(result != 0 /* nonzero is success */);
                }
            }

            _timer.Tick(_application, static (timer, app) =>
            {
                app.Update(timer);
                app.Render();
            });
        }

        private static ApplicationTimer _timer = null!;
        private static Application _application = null!;
        private const int ScrollResolution = 120;

        // Main message handler
        // Uncomment when JIT bug is fixed
        //[UnmanagedCallersOnly(CallingConvention = CallingConvention.StdCall)]
        private static nint WindowProc(IntPtr hWnd, uint message, nuint wParam, nint lParam)
        {
            switch (message)
            {
                case WM_ACTIVATE:
                {
                    if (LOWORD(wParam) == WA_INACTIVE)
                    {
                        _isPaused = true;
                    }
                    else
                    {
                        _isPaused = false;
                    }
                    return 0;
                }


                case WM_KEYDOWN:
                {
                    if (TryGetModifier(wParam, out var modifier))
                    {
                        KeyboardHandler.SetModifierState(modifier, true);
                    }
                    else
                    {
                        KeyboardHandler.SetKeyState((ConsoleKey)wParam, true);
                    }
                    return 0;
                }

                case WM_KEYUP:
                {
                    if (TryGetModifier(wParam, out var modifier))
                    {
                        KeyboardHandler.SetModifierState(modifier, false);
                    }
                    else
                    {
                        KeyboardHandler.SetKeyState((ConsoleKey)wParam, false);
                    }
                    return 0;
                }

                // Can't just blindly cast as the values don't line up. 
                static bool TryGetModifier(nuint wParam, out ConsoleModifiers modifier)
                {
                    if (wParam == VK_SHIFT)
                    {
                        modifier = ConsoleModifiers.Shift;
                        return true;
                    }
                    if (wParam == /* weird name, but it is ALT key */ VK_MENU)
                    {
                        modifier = ConsoleModifiers.Alt;
                        return true;
                    }
                    if (wParam == VK_CONTROL)
                    {
                        modifier = ConsoleModifiers.Control;
                        return true;
                    }
                    modifier = 0;
                    return false;
                }

                case WM_MOUSEMOVE:
                {
                    return 0;
                }

                case WM_MOUSEWHEEL:
                {
                    var delta = GET_WHEEL_DELTA_WPARAM(wParam) / ScrollResolution;
                    return 0;
                }

                case WM_ENTERSIZEMOVE:
                {
                    _isResizing = true;
                    return 0;
                }

                case WM_EXITSIZEMOVE:
                {
                    _isResizing = false;
                    _application.OnResize(_screenData);
                    return 0;
                }

                case WM_SIZE:
                {
                    var sz = (uint)lParam;
                    _screenData = new Size(LOWORD(sz), HIWORD(sz));

                    if (!_isResizing && wParam != SIZE_MINIMIZED)
                    {
                        _application.OnResize(_screenData);
                    }

                    return 0;
                }

                case WM_DESTROY:
                {
                    PostQuitMessage(0);
                    return 0;
                }
            }

            // Handle any messages the switch statement didn't.
            return DefWindowProcW(hWnd, message, wParam, lParam);
        }
    }
}
