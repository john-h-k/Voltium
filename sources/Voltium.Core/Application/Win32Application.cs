using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using TerraFX.Interop;
using Voltium.Core.Devices;
using static TerraFX.Interop.Windows;

namespace Voltium.Core
{
    /// <summary>
    /// Used for creation and execution of Win32 backed applications
    /// </summary>
    public static unsafe class Win32Application
    {
        private static readonly delegate* stdcall<IntPtr, uint, nuint, nint, nint> WindowProcHandle =
            (delegate* stdcall<IntPtr, uint, nuint, nint, nint>)(delegate*<IntPtr, uint, nuint, nint, nint>)&WindowProc;

        private static bool _isResizing = false;
        //private static bool _isPaused = false;
        //private static bool _isMaximized = false;
        private static HWND Hwnd;

        private static Size _screenData;

        /// <summary>
        /// Run a <see cref="Application"/> on Win32
        /// </summary>
        /// <param name="application">The <see cref="Application"/> to run</param>
        /// <param name="width">The width, in pixels, of the screen</param>
        /// <param name="height">The height, in pixels, of the screen</param>
        /// <returns>The exit code of the app</returns>
        public static int Run(Application application, uint width, uint height)
        {
            var hInstance = GetModuleHandleW(null);
            _application = application;

            fixed (char* name = "Voltium.Interactive")
            fixed (char* windowsTitle = application.Title)
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

            application.Init(_screenData, IOutputOwner.FromHwnd(Hwnd));

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
                    Thread.Sleep(10);
                }
            }

            application.Dispose();

            // Return this part of the WM_QUIT message to Windows.
            return (int)msg.wParam;
        }

        private static void RunApp()
        {
            fixed (char* pFps = $"Voltium - FPS: {_timer.FramesPerSeconds}")
            {
                _ = SetWindowTextW(Hwnd, (ushort*)pFps);

                _timer.Tick(() =>
                {
                    _application.Update(_timer);
                    _application.Render();
                });
            }
        }

        private static ApplicationTimer _timer = null!;
        private static Application _application = null!;
        private const int ScrollResolution = 120;
        private static bool _isPaused;

        // Main message handler
        [UnmanagedCallersOnly(CallingConvention = CallingConvention.StdCall)]
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
                    _application.OnKeyDown((ConsoleKey)wParam);
                    return 0;
                }

                case WM_KEYUP:
                {
                    _application.OnKeyUp((ConsoleKey)wParam);
                    return 0;
                }

                case WM_MOUSEMOVE:
                {
                    return 0;
                }

                case WM_MOUSEWHEEL:
                {
                    var delta = GET_WHEEL_DELTA_WPARAM(wParam) / ScrollResolution;
                    _application.OnMouseScroll(delta);
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

                    if (!_isResizing)
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
