using MandalaLogics.SurfaceTerminal;

namespace surfImg;

internal static partial class Program
{
    static Program()
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
    }

    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        SurfaceTerminal.Stop((Exception)e.ExceptionObject);
    }
}