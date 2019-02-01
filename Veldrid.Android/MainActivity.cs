using Android.App;
using Android.Content.PM;
using Android.OS;
using System.Diagnostics;
using Veldrid;

namespace mellinoe.VeldridActivityExample
{
    [Activity(Label = "VeldridActivityExample", MainLauncher = true, Icon = "@mipmap/icon", ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainActivity : Activity
    {
        private GraphicsDeviceOptions _options;
        private VeldridSurfaceView _view;
        private ResourceFactory _disposeFactory;
        private Stopwatch _sw;

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

        private readonly int _frameRepeatCount = 20;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            _options = new GraphicsDeviceOptions(false, PixelFormat.R16_UNorm, false);
            //GraphicsBackend backend = GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan) ? GraphicsBackend.Vulkan : GraphicsBackend.OpenGLES;
            GraphicsBackend backend = GraphicsBackend.OpenGLES;


            _view = new VeldridSurfaceView(this, backend, _options);
            _view.Rendering += OnViewRendering;
            _view.DeviceCreated += OnViewCreatedDevice;
            _sw = Stopwatch.StartNew();

            SetContentView(_view);
        }

        protected override void OnPause()
        {
            base.OnPause();
            _view.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            _view.OnResume();
        }

        private void OnViewCreatedDevice()
        {
            _disposeFactory = _view.GraphicsDevice.ResourceFactory;
            _cl = _disposeFactory.CreateCommandList();
            _view.RunContinuousRenderLoop();
        }

        private void OnViewRendering()
        {
            // Each frame, we clear the Swapchain's color target.
            // Several different colors are cycled.
            _cl.Begin();
            _cl.SetFramebuffer(_view.MainSwapchain.Framebuffer);
            _cl.ClearColorTarget(0, _clearColors[(_frameIndex / _frameRepeatCount)]);
            _cl.End();
            _view.GraphicsDevice.SubmitCommands(_cl);
            _view.GraphicsDevice.SwapBuffers(_view.MainSwapchain);

            // Do some math to loop our color picker index.
            _frameIndex = (_frameIndex + 1) % (_clearColors.Length * _frameRepeatCount);
        }
    }
}

