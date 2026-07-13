namespace CsvDesktopAnalyzer;

static class Program
{
    [STAThread]
    static void Main()
    {
        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
        catch (Exception ex)
        {
            string logPath = Path.Combine(AppContext.BaseDirectory, "startup-error.log");
            File.WriteAllText(logPath, ex.ToString());
            MessageBox.Show(ex.ToString(), "CsvDesktopAnalyzer startup error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
