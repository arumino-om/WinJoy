using HidApi;

namespace WinJoy;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        Hid.Init();
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        Hid.Exit();
    }
}