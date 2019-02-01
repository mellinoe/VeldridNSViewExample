using System;
using System.Diagnostics;
using CoreAnimation;
using CoreVideo;
using Foundation;
using UIKit;
using Veldrid;

namespace VeldridUIViewExample
{
    public class VeldridView : UIView
    {
        private readonly GraphicsBackend _backend;
        private readonly GraphicsDeviceOptions _deviceOptions;

        private CADisplayLink _displayLink;
        private bool _paused;
        private bool _resized;
        private uint _width;
        private uint _height;
        private bool _disposed;

        public GraphicsDevice GraphicsDevice { get; protected set; }
        public Swapchain MainSwapchain { get; protected set; }

        public event Action DeviceReady;
        public event Action Rendering;
        public event Action Resized;

        public VeldridView(GraphicsBackend backend, GraphicsDeviceOptions deviceOptions)
        {
            if (!(backend == GraphicsBackend.Metal || backend == GraphicsBackend.OpenGLES))
            {
                throw new NotSupportedException($"{backend} is not supported on windows.");
            }

            _backend = backend;
            _deviceOptions = deviceOptions;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if (disposing)
            {
                _displayLink.RemoveFromRunLoop(NSRunLoop.Current, NSRunLoopMode.Default);
                GraphicsDevice.Dispose();
            }
            _disposed = true;
            base.Dispose(disposing);
        }

        public override void MovedToWindow()
        {
            base.MovedToWindow();

            var swapchainSource = SwapchainSource.CreateUIView(Handle);
            var swapchainDescription = new SwapchainDescription(swapchainSource, (uint)Frame.Width, (uint)Frame.Height, null, true, true);

            if (_backend == GraphicsBackend.Metal)
            {
                GraphicsDevice = GraphicsDevice.CreateMetal(_deviceOptions);
            }

            MainSwapchain = GraphicsDevice.ResourceFactory.CreateSwapchain(swapchainDescription);

            DeviceReady?.Invoke();

            _displayLink = CADisplayLink.Create(HandleDisplayLinkOutputCallback);
            _displayLink.AddToRunLoop(NSRunLoop.Current, NSRunLoopMode.Default);
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            _resized = true;

            const double dpiScale = 1;
            _width = (uint)(Frame.Width < 0 ? 0 : Math.Ceiling(Frame.Width * dpiScale));
            _height = (uint)(Frame.Height < 0 ? 0 : Math.Ceiling(Frame.Height * dpiScale));
        }

        private void HandleDisplayLinkOutputCallback()
        {
            try
            {
                if (_paused)
                {
                    return;
                }
                if (GraphicsDevice != null)
                {
                    if (_resized)
                    {
                        _resized = false;
                        MainSwapchain.Resize(_width / 2, _height);
                        Resized?.Invoke();
                    }
                    Rendering?.Invoke();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Encountered an error while rendering: " + e);
                throw;
            }
        }

        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
        }
    }
}
