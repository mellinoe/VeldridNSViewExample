using System;
using UIKit;
using CoreVideo;
using Foundation;
using Veldrid;
using CoreAnimation;

namespace VeldridUIViewExample
{
    public partial class ViewController : UIViewController
    {
        private readonly GraphicsDevice _gd;
        private Swapchain _sc;
        private CommandList _cl;
        private int _frameIndex = 0;
        private RgbaFloat[] _clearColors =
        {
            RgbaFloat.Red,
            RgbaFloat.Orange,
            RgbaFloat.Yellow,
            RgbaFloat.Green,
            RgbaFloat.Blue,
            new RgbaFloat(0.8f, 0.1f, 0.3f, 1f),
            new RgbaFloat(0.8f, 0.1f, 0.9f, 1f),
        };
        private bool _resized;
        private (uint Width, uint Height) _size;
        private readonly int _frameRepeatCount = 20;

        public ViewController(IntPtr handle) : base(handle)
        {
            _frameIndex = new Random().Next(0, _clearColors.Length * _frameRepeatCount);
            _gd = AppGlobals.Device;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var ss = SwapchainSource.CreateUIView(View.Handle);
            SwapchainDescription scDesc = new SwapchainDescription(
                ss, (uint)View.Frame.Width, (uint)View.Frame.Height, null, true, true);
            _sc = AppGlobals.Device.ResourceFactory.CreateSwapchain(scDesc);
            _cl = AppGlobals.Device.ResourceFactory.CreateCommandList();

            // To render at a steady rate, we create a display link which will invoke our Render function.
            CADisplayLink displayLink = CADisplayLink.Create(HandleDisplayLinkOutputCallback);
            displayLink.AddToRunLoop(NSRunLoop.Current, NSRunLoopMode.Default);
        }

        private void Render()
        {
            // If we've detected a resize event, we handle it now.
            if (_resized)
            {
                _resized = false;
                _sc.Resize(_size.Width, _size.Height);
            }

            // Each frame, we clear the Swapchain's color target.
            // Several different colors are cycled.
            _cl.Begin();
            _cl.SetFramebuffer(_sc.Framebuffer);
            _cl.ClearColorTarget(0, _clearColors[(_frameIndex / _frameRepeatCount)]);
            _cl.End();
            _gd.SubmitCommands(_cl);
            _gd.SwapBuffers(_sc);

            // Do some math to loop our color picker index.
            _frameIndex = (_frameIndex + 1) % (_clearColors.Length * _frameRepeatCount);
        }

        private void HandleDisplayLinkOutputCallback()
        {
            Render();
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();
            _resized = true;
            _size = ((uint)View.Frame.Width, (uint)View.Frame.Height);
        }
    }
}
