using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using TerraFX.Interop;
using Voltium.Core.Managers;
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

            application.Init(_screenData, Hwnd);

            _ = ShowWindow(Hwnd, SW_SHOWDEFAULT);

            _timer = ApplicationTimer.StartNew();
            _timer.TargetElapsedSeconds = 1 / 120d;
            _timer.IsFixedTimeStep = true;

            // Main sample loop.
            MSG msg;

            do
            {
                // Process any messages in the queue.
                if (PeekMessageW(&msg, IntPtr.Zero, 0, 0, PM_REMOVE) != 0)
                {
                    _ = TranslateMessage(&msg);
                    _ = DispatchMessageW(&msg);
                }
            }
            while (msg.message != WM_QUIT);

            application.Destroy();

            //// Return this part of the WM_QUIT message to Windows.
            return (int)msg.wParam;
        }

        private static ApplicationTimer _timer = null!;
        private static Application _application = null!;
        private const int ScrollResolution = 120;

        // Main message handler for the sample
        [UnmanagedCallersOnly(CallingConvention = CallingConvention.StdCall)]
        private static nint WindowProc(IntPtr hWnd, uint message, nuint wParam, nint lParam)
        {
            var handle = GetWindowLongPtrW(hWnd, GWLP_USERDATA);

            switch (message)
            {
                case WM_CREATE:
                {
                    // Save the Application* passed in to CreateWindow.
                    var pCreateStruct = (CREATESTRUCTW*)lParam;
                    _ = SetWindowLongPtrW(hWnd, GWLP_USERDATA, (IntPtr)pCreateStruct->lpCreateParams);
                    return 0;
                }

                case WM_KEYDOWN:
                {
                    _application.OnKeyDown((byte)wParam);
                    return 0;
                }

                case WM_KEYUP:
                {
                    _application.OnKeyUp((byte)wParam);
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

                    if (!_isResizing && lParam != 0) // why do we sometimes get zero size lParams?
                    {
                        _application.OnResize(_screenData);
                    }

                    return 0;
                }

                case WM_PAINT:
                {
                    if (_application != null)
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
