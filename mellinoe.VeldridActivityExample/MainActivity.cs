using Android.App;
using Android.OS;
using Veldrid;
using System;
using Android.Views;
using Android.Runtime;
using Java.Util;

namespace mellinoe.VeldridActivityExample
{
    public class RenderTimer : TimerTask
    {
        private readonly Action _action;

        public RenderTimer(Action action)
        {
            _action = action;
        }

        public override void Run()
        {
            _action?.Invoke();
        }
    }

    [Activity(Label = "mellinoe.VeldridActivityExample", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity, View.IOnLayoutChangeListener
    {
        private GraphicsDevice _gd;
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

        public MainActivity()
        {
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            AppGlobals.InitDevice();
            _frameIndex = new System.Random().Next(0, _clearColors.Length * _frameRepeatCount);
            _gd = AppGlobals.Device;

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            SurfaceView surfaceView = FindViewById<SurfaceView>(Resource.Id.surfaceView);
            surfaceView.AddOnLayoutChangeListener(this);

            var ss = SwapchainSource.CreateAndroidSurface(surfaceView.Handle, JNIEnv.Handle);
            SwapchainDescription scDesc = new SwapchainDescription(
                ss, (uint)surfaceView.Width, (uint)surfaceView.Height, null, true, true);
            _sc = AppGlobals.Device.ResourceFactory.CreateSwapchain(scDesc);
            _cl = AppGlobals.Device.ResourceFactory.CreateCommandList();

            // To render at a steady rate, we create a timer which will invoke our Render function.
            Timer timer = new Timer();
            timer.ScheduleAtFixedRate(new RenderTimer(Render), 0, 1000 / 30);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            AppGlobals.DisposeDevice();
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

        public void OnLayoutChange(View v, int left, int top, int right, int bottom, int oldLeft, int oldTop, int oldRight, int oldBottom)
        {
            _resized = true;
            _size = ((uint)v.Width, (uint)v.Height);
        }
    }
}

