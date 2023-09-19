using System;
using TerraFX.Interop;
using TerraFX.Interop.Windows;

namespace Voltium.Core.Devices
{
    /// <summary>
    /// Indicates the type owns an unknown output type
    /// </summary>
    public interface IOutputOwner
    {
        /// <summary>
        /// The <see cref="OutputType"/> for this output
        /// </summary>
        public OutputType Type { get; }


        /// <summary>
        /// Gets the <see cref="IntPtr"/> for the type. The meaning of this changes based on <see cref="Type"/>
        /// </summary>
        /// <returns>The <see cref="IntPtr"/> for the type</returns>
        public IntPtr GetOutput();

        /// <summary>
        /// Creates a new <see cref="IOutputOwner"/> to an HWND
        /// </summary>
        public static IOutputOwner FromHwnd(HWND hwnd)
            => new HwndOwner(hwnd);

        /// <summary>
        /// Creates a new <see cref="IOutputOwner"/> to an ICoreWindow
        /// </summary>
        public static unsafe IOutputOwner FromICoreWindow(void* window)
            => new CoreWindowOwner(window);


        /// <summary>
        /// Creates a new <see cref="IOutputOwner"/> to a XAML SwapChainPanel
        /// </summary>
        public static unsafe IOutputOwner FromSwapChainPanel(void* swapChainPanel)
            => new SwapChainPanelOwner(swapChainPanel);

        private sealed class HwndOwner : IOutputOwner
        {
            private IntPtr _hwnd;

            public HwndOwner(IntPtr hwnd)
                => _hwnd = hwnd;

            public OutputType Type => OutputType.Hwnd;

            public IntPtr GetOutput() => _hwnd;
        }

        private sealed unsafe class CoreWindowOwner : IOutputOwner
        {
            private void* _coreWindow;

            public CoreWindowOwner(void* coreWindow)
                => _coreWindow = coreWindow;

            public OutputType Type => OutputType.ICoreWindow;

            public IntPtr GetOutput() => (IntPtr)_coreWindow;
        }

        private sealed unsafe class SwapChainPanelOwner : IOutputOwner
        {
            private void* _panel;

            public SwapChainPanelOwner(void* panel)
                => _panel = panel;

            public OutputType Type => OutputType.SwapChainPanel;

            public IntPtr GetOutput() => (IntPtr)_panel;
        }
    }
}
