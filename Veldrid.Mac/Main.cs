using AppKit;

namespace VeldridNSViewExample
{
    static class MainClass
    {
        static void Main(string[] args)
        {
            NSApplication.Init();
            AppGlobals.InitDevice();
            NSApplication.Main(args);
            AppGlobals.DisposeDevice();
        }
    }
}
