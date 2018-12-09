using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Veldrid;

namespace Veldrid.Forms
{
    public partial class MainForm : Form
    {
        private VeldridControl _veldridControl;
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

        public MainForm()
        {
            InitializeComponent();

            GraphicsDeviceOptions options = new GraphicsDeviceOptions(
                debug: false,
                swapchainDepthFormat: null,
                syncToVerticalBlank: false,
                resourceBindingModel: ResourceBindingModel.Improved,
                preferDepthRangeZeroToOne: true,
                preferStandardClipSpaceYDirection: true);

            _veldridControl = new VeldridControl(GraphicsBackend.Direct3D11, options)
            {
                Width = 300,
                Height = 300,
                BackColor = Color.Red,
                Location = new Point(8, 8)
            };
            _veldridControl.Rendering += VeldridControlOnRendering;
            Controls.Add(_veldridControl);
            
            _veldridControl.MouseDown += (sender, args) =>
            {
                var a = 1;
            };

            _frameIndex = new Random().Next(0, _clearColors.Length * _frameRepeatCount);
            _cl = _veldridControl.GraphicsDevice.ResourceFactory.CreateCommandList();
            _veldridControl.Start();
        }

        private void VeldridControlOnRendering()
        {
            _cl.Begin();
            _cl.SetFramebuffer(_veldridControl.MainSwapchain.Framebuffer);
            _cl.ClearColorTarget(0, _clearColors[_frameIndex / _frameRepeatCount]);
            _cl.ClearDepthStencil(1);
            _cl.End();
            _veldridControl.GraphicsDevice.SubmitCommands(_cl);
            _veldridControl.GraphicsDevice.SwapBuffers(_veldridControl.MainSwapchain);

            // Do some math to loop our color picker index.
            _frameIndex = (_frameIndex + 1) % (_clearColors.Length * _frameRepeatCount);
        }
        
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _veldridControl.Dispose();
        }
    }
}
