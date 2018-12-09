using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Veldrid;
using Veldrid.OpenGL;

namespace Veldrid.Forms
{
    public class VeldridControl : UserControl
    {
        private readonly GraphicsBackend _backend;
        protected GraphicsDeviceOptions DeviceOptions { get; }
        protected IntPtr HWND { get; }
        protected IntPtr HInstance { get; }

        private bool _paused;
        private bool _enabled;
        
        public GraphicsDevice GraphicsDevice { get; protected set; }

        public Swapchain MainSwapchain { get; protected set; }

        public event Action Rendering;
        public event Action Resized;
        
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            const double dpiScale = 1;
            uint width = (uint)(Width < 0 ? 0 : Math.Ceiling(Width * dpiScale));
            uint height = (uint)(Height < 0 ? 0 : Math.Ceiling(Height * dpiScale));

            NativeMethods.MoveWindow(HWND, 0, 0, Width, Height, true);
            MainSwapchain.Resize(width, height);

            Resized?.Invoke();
        }

        public VeldridControl(GraphicsBackend backend, GraphicsDeviceOptions deviceOptions)
        {
            if (!(backend == GraphicsBackend.Vulkan || backend == GraphicsBackend.OpenGL || backend == GraphicsBackend.Direct3D11))
            {
                throw new NotSupportedException($"{backend} is not supported on windows.");
            }

            if (backend == GraphicsBackend.OpenGL)
            {
                throw new NotSupportedException($"{backend} is not currently implemented in this demo.");
            }

            _backend = backend;
            DeviceOptions = deviceOptions;

            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = false;

            NativeMethods.CreateWindow(Handle, MessageHandler, Width, Height, out var hwnd, out var hinstance);

            HWND = hwnd;
            HInstance = hinstance;

            if (_backend == GraphicsBackend.Vulkan)
            {
                GraphicsDevice = GraphicsDevice.CreateVulkan(deviceOptions);
            }
            else
            {
                GraphicsDevice = GraphicsDevice.CreateD3D11(deviceOptions);
            }

            const double dpiScale = 1;
            uint width = (uint)(Width < 0 ? 0 : Math.Ceiling(Width * dpiScale));
            uint height = (uint)(Height < 0 ? 0 : Math.Ceiling(Height * dpiScale));

            SwapchainSource swapchainSource = SwapchainSource.CreateWin32(HWND, HInstance);
            SwapchainDescription swapchainDescription = new SwapchainDescription(swapchainSource, width, height, PixelFormat.R32_Float, true);
            MainSwapchain = GraphicsDevice.ResourceFactory.CreateSwapchain(swapchainDescription);
    
            Disposed += OnDisposed;
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            base.OnMouseDown(e);
        }

        public void Start()
        {
            Task.Factory.StartNew(RenderLoop, TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            _enabled = false;
        }

        private void RenderLoop()
        {
            _enabled = true;
            while (_enabled)
            {
                try
                {
                    if (_paused)
                    {
                        continue;
                    }
                    if (GraphicsDevice != null)
                    {
                        Rendering?.Invoke();
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Encountered an error while rendering: " + e);
                    throw;
                }
            }
        }

        private void OnDisposed(object sender, EventArgs e)
        {
            Disposed -= OnDisposed;
            Stop();             
            NativeMethods.DestroyWindow(HWND);
        }
        
        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
        }

        private bool _mouseInClient;

        private IntPtr MessageHandler(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            Point point = new Point(lParam.ToInt32());
            Point wheel = new Point(wParam.ToInt32());

            switch (msg)
            {
                case NativeMethods.WM_LBUTTONDOWN:
                    base.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, point.X, point.Y, 0));
                    break;
                case NativeMethods.WM_LBUTTONUP:
                    base.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, point.X, point.Y, 0));
                    break;
                case NativeMethods.WM_RBUTTONDOWN:
                    base.OnMouseDown(new MouseEventArgs(MouseButtons.Right, 1, point.X, point.Y, 0));
                    break;
                case NativeMethods.WM_RBUTTONUP:
                    base.OnMouseUp(new MouseEventArgs(MouseButtons.Right, 1, point.X, point.Y, 0));
                    break;
                case NativeMethods.WM_MOUSEWHEEL:
                    base.OnMouseWheel(new MouseEventArgs(MouseButtons.Right, 1, point.X, point.Y, wheel.Y));
                    break;
                case NativeMethods.WM_MOUSEMOVE:
                {
                    if (!_mouseInClient)
                    {
                        base.OnMouseEnter(new EventArgs());
                    }
                    _mouseInClient = true;
                    base.OnMouseMove(new MouseEventArgs(MouseButtons.None, 1, point.X, point.Y, 0));
                    break;
                }
                case NativeMethods.WM_MOUSELEAVE:
                {
                    _mouseInClient = false;
                    base.OnMouseLeave(new EventArgs());
                    break;
                }
            }

            return NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
        }

        internal class NativeMethods
        {

            public static void CreateWindow(IntPtr hwndParent, WndProc handler, int width, int height, out IntPtr hwnd, out IntPtr hinstance)
            {                
                const string WindowClass = "VeldridHwndWrapper";
                hinstance = GetModuleHandle(null);
                var wndClass = new WndClassEx();
                wndClass.cbSize = (uint)Marshal.SizeOf(wndClass);
                wndClass.hInstance = hinstance;
                wndClass.lpfnWndProc = handler;
                wndClass.lpszClassName = WindowClass;
                wndClass.hCursor = LoadCursor(IntPtr.Zero, IDC_ARROW);
                RegisterClassEx(ref wndClass);
                hwnd = CreateWindowEx(0, WindowClass, "", WS_CHILD | WS_VISIBLE, 0, 0, width, height, hwndParent, IntPtr.Zero, IntPtr.Zero, 0);
            }



            public const int WS_CHILD = 0x40000000;
            public const int WS_VISIBLE = 0x10000000;
            public const int WM_LBUTTONDOWN = 0x0201;
            public const int WM_LBUTTONUP = 0x0202;
            public const int WM_RBUTTONDOWN = 0x0204;
            public const int WM_RBUTTONUP = 0x0205;
            public const int WM_MOUSEWHEEL = 0x020A;
            public const int WM_MOUSELEAVE = 0x02A3;
            public const int WM_MOUSEMOVE = 0x0200;

            public const int IDC_ARROW = 32512;
            public const int IDC_CROSS = 32515;



            //public enum Cursor
            //{
            //    AppStarting = 32650,
            //    Arrow = 32512,
            //    Cross = 32515,
            //    Hand = 32649,
            //    Help = 32651,
            //    IBeam = 32513,
            //    No = 32648,
            //    SizeAll = 32646,
            //    SizeSW = 32643,
            //    SizeNS = 32645,
            //    SizeWSE = 32642,
            //    SizeWE = 32644,
            //    UpArrow = 32516,
            //    Wait = 32514
            //}

            

            [StructLayout(LayoutKind.Sequential)]
            public struct WndClassEx
            {
                public uint cbSize;
                public uint style;
                [MarshalAs(UnmanagedType.FunctionPtr)]
                public WndProc lpfnWndProc;
                public int cbClsExtra;
                public int cbWndExtra;
                public IntPtr hInstance;
                public IntPtr hIcon;
                public IntPtr hCursor;
                public IntPtr hbrBackground;
                public string lpszMenuName;
                public string lpszClassName;
                public IntPtr hIconSm;
            }
            
            [DllImport("user32.dll")]
            public static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);
            public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
      
            [DllImport("user32.dll")]
            public static extern IntPtr SetCursor(IntPtr handle);

            [DllImport("user32.dll")]
            public static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

            [DllImport("user32.dll", EntryPoint = "CreateWindowEx", CharSet = CharSet.Auto)]
            public static extern IntPtr CreateWindowEx(int exStyle, string className, string windowName, int style, int x, int y, int width, int height, IntPtr hwndParent, IntPtr hMenu, IntPtr hInstance, [MarshalAs(UnmanagedType.AsAny)] object pvParam);
            
            [DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Auto)]
            public static extern bool DestroyWindow(IntPtr hwnd);
     
            [DllImport("kernel32.dll")]
            public static extern IntPtr GetModuleHandle(string module);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.U2)]
            public static extern short RegisterClassEx([In] ref WndClassEx lpwcx);

            [DllImport("user32.dll")]
            public static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);
        }

    }
}
