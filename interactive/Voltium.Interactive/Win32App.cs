using System;
using System.Runtime.InteropServices;
using System.Threading;
using TerraFX.Interop;
using Voltium.Core;
using Voltium.Core.Managers;
using static TerraFX.Interop.Windows;

namespace Voltium.Interactive
{
    public static unsafe class Win32Application
    {
        private static readonly WNDPROC WndProc = (hwnd, message, wParam, lParam) => WindowProc(hwnd, message, wParam, lParam);
        private static readonly IntPtr WindowProcHandle = Marshal.GetFunctionPointerForDelegate(WndProc);

        private static bool _isResizing = false;
        //private static bool _isPaused = false;
        //private static bool _isMaximized = false;

        public static readonly int Height = 1080 / 2, Width = 1920 / 2;
        public static HWND Hwnd { get; private set; }

        private static ScreenData _screenData;

        public static int Run(Application application, HINSTANCE hInstance, int nCmdShow)
        {
            uint height;
            uint width;

            fixed (char* lpszClassName = "DXSampleClass")
            fixed (char* lpWindowName = application.Title)
            {
                // Initialize the window class.
                var windowClass = new WNDCLASSEXW
                {
                    cbSize = (uint)sizeof(WNDCLASSEXW),
                    style = CS_HREDRAW | CS_VREDRAW,
                    lpfnWndProc = WindowProcHandle,
                    hInstance = hInstance,
                    hCursor = LoadCursorW(IntPtr.Zero, (ushort*)IDC_ARROW),
                    lpszClassName = (ushort*)lpszClassName
                };
                _ = RegisterClassExW(&windowClass);

                var windowRect = new Rectangle(0, 0, Width, Height);
                _ = AdjustWindowRect((RECT*)&windowRect, WS_OVERLAPPEDWINDOW, FALSE);

                height = (uint)(windowRect.Bottom - windowRect.Top);
                width = (uint)(windowRect.Right - windowRect.Left);
                // Create the window and store a handle to it.
                Hwnd = CreateWindowExW(
                    0,
                    windowClass.lpszClassName,
                    (ushort*)lpWindowName,
                    WS_OVERLAPPEDWINDOW,
                    CW_USEDEFAULT,
                    CW_USEDEFAULT,
                    (int)width,
                    (int)height,
                    HWND.NULL,                              // We have no parent window.
                    HMENU.NULL,                             // We aren't using menus.
                    hInstance,
                    ((IntPtr)GCHandle.Alloc(application)).ToPointer()
                );
            }

            _screenData = new ScreenData(height, width, Hwnd);
            // Initialize the sample. OnInit is defined in each child-implementation of DXSample.
            application.Init(_screenData);

            _ = ShowWindow(Hwnd, nCmdShow);

            // Main sample loop.
            MSG msg;

            _timer = ApplicationTimer.StartNew();

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

            // Return this part of the WM_QUIT message to Windows.
            return (int)msg.wParam;
        }

        private static ApplicationTimer _timer = null!;

        // Main message handler for the sample
        private static IntPtr WindowProc(HWND hWnd, uint message, UIntPtr wParam, IntPtr lParam)
        {
            var handle = GetWindowLongPtrW(hWnd, GWLP_USERDATA);
            var pSample = (handle != IntPtr.Zero) ? (Application?)GCHandle.FromIntPtr(handle).Target : null;

            switch (message)
            {
                case WM_CREATE:
                {
                    // Save the Application* passed in to CreateWindow.
                    var pCreateStruct = (CREATESTRUCTW*)lParam;
                    _ = SetWindowLongPtrW(hWnd, GWLP_USERDATA, (IntPtr)pCreateStruct->lpCreateParams);
                    return IntPtr.Zero;
                }

                case WM_KEYDOWN:
                {
                    pSample?.OnKeyDown((byte)wParam);
                    return IntPtr.Zero;
                }

                case WM_KEYUP:
                {
                    pSample?.OnKeyUp((byte)wParam);
                    return IntPtr.Zero;
                }

                case WM_MOUSEWHEEL:
                {
                    var delta = GET_WHEEL_DELTA_WPARAM(wParam) / 120;
                    pSample?.OnMouseScroll(delta);
                    return IntPtr.Zero;
                }

                case WM_ENTERSIZEMOVE:
                {
                    _isResizing = true;
                    return IntPtr.Zero;
                }

                case WM_EXITSIZEMOVE:
                {
                    _isResizing = false;
                    goto case WM_SIZE;
                }

                case WM_SIZE:
                {
                    var sz = (uint)lParam;
                    _screenData = new ScreenData(HIWORD(sz), LOWORD(sz), hWnd);

                    if (_isResizing)
                    {
                        return IntPtr.Zero;
                    }

                    if (sz != 0) // why do we sometimes get zero size wParams?
                    {
                        pSample?.OnResize(_screenData);
                    }
                    return IntPtr.Zero;
                }

                case WM_PAINT:
                {
                    if (pSample != null)
                    {
                        _timer.Tick();
                        pSample.Update(_timer);
                        pSample.Render();
                    }

                    return IntPtr.Zero;
                }

                case WM_DESTROY:
                {
                    PostQuitMessage(0);
                    return IntPtr.Zero;
                }
            }

            // Handle any messages the switch statement didn't.
            return DefWindowProcW(hWnd, message, wParam, lParam);
        }
    }
}
