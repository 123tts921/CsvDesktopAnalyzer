using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

internal static class InstallerProgram
{
    private const string AppName = "CsvDesktopAnalyzer";
    private const string PayloadResourceName = "CsvDesktopAnalyzerPayload.exe";

    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        try
        {
            EnsureAdministrator();
            InstallApplication();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "安装失败：\n" + ex.Message,
                "CsvDesktopAnalyzer 安装程序",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void EnsureAdministrator()
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                return;
        }

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = Application.ExecutablePath,
            UseShellExecute = true,
            Verb = "runas"
        };

        Process.Start(psi);
        Environment.Exit(0);
    }

    private static void InstallApplication()
    {
        string installRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            AppName);

        string appExePath = Path.Combine(installRoot, AppName + ".exe");
        string desktopShortcutPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            AppName + ".lnk");
        string startMenuDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
            "Programs",
            AppName);
        string startMenuShortcutPath = Path.Combine(startMenuDir, AppName + ".lnk");
        string uninstallShortcutPath = Path.Combine(startMenuDir, "卸载 " + AppName + ".lnk");
        string uninstallPs1Path = Path.Combine(installRoot, "uninstall.ps1");
        string uninstallCmdPath = Path.Combine(installRoot, "卸载 CsvDesktopAnalyzer.cmd");

        Directory.CreateDirectory(installRoot);
        Directory.CreateDirectory(startMenuDir);

        WritePayloadExe(appExePath);
        File.WriteAllText(uninstallPs1Path, BuildUninstallPs1(), new UTF8Encoding(true));
        File.WriteAllText(
            uninstallCmdPath,
            "@echo off\r\npowershell.exe -NoProfile -ExecutionPolicy Bypass -File \"%~dp0uninstall.ps1\"\r\n",
            Encoding.ASCII);

        CreateShortcut(desktopShortcutPath, appExePath, installRoot, appExePath);
        CreateShortcut(startMenuShortcutPath, appExePath, installRoot, appExePath);
        CreateShortcut(uninstallShortcutPath, uninstallCmdPath, installRoot, appExePath);

        MessageBox.Show(
            "安装完成。\n安装位置：\n" + installRoot,
            "CsvDesktopAnalyzer 安装程序",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static void WritePayloadExe(string destinationPath)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string resourceName = null;

        foreach (string name in assembly.GetManifestResourceNames())
        {
            if (name.EndsWith(PayloadResourceName, StringComparison.OrdinalIgnoreCase))
            {
                resourceName = name;
                break;
            }
        }

        if (resourceName == null)
            throw new InvalidOperationException("未找到内嵌主程序资源。");

        using (Stream input = assembly.GetManifestResourceStream(resourceName))
        {
            if (input == null)
                throw new InvalidOperationException("无法读取内嵌主程序资源。");

            using (FileStream output = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                input.CopyTo(output);
            }
        }
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string iconLocation)
    {
        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
        object shell = Activator.CreateInstance(shellType);
        object shortcut = shellType.InvokeMember(
            "CreateShortcut",
            BindingFlags.InvokeMethod,
            null,
            shell,
            new object[] { shortcutPath });

        Type shortcutType = shortcut.GetType();
        shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
        shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workingDirectory });
        shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { iconLocation });
        shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
    }

    private static string BuildUninstallPs1()
    {
        return
@"Add-Type -AssemblyName System.Windows.Forms
$ErrorActionPreference = ""Stop""

$appName = ""CsvDesktopAnalyzer""
$installRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$desktopShortcutPath = Join-Path ([Environment]::GetFolderPath(""Desktop"")) ""$appName.lnk""
$startMenuDir = Join-Path ([Environment]::GetFolderPath(""CommonStartMenu"")) ""Programs\$appName""

function Ensure-Administrator {
    $currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentIdentity)
    if ($principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        return
    }

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = ""powershell.exe""
    $psi.Arguments = ""-NoProfile -ExecutionPolicy Bypass -File `""$PSCommandPath`""""
    $psi.Verb = ""runas""
    [Diagnostics.Process]::Start($psi) | Out-Null
    exit 0
}

Ensure-Administrator

$result = [System.Windows.Forms.MessageBox]::Show(
    ""确定要卸载 CsvDesktopAnalyzer 吗？"",
    ""卸载确认"",
    [System.Windows.Forms.MessageBoxButtons]::OKCancel,
    [System.Windows.Forms.MessageBoxIcon]::Question
)

if ($result -ne [System.Windows.Forms.DialogResult]::OK) {
    exit 0
}

if (Test-Path $desktopShortcutPath) {
    Remove-Item -LiteralPath $desktopShortcutPath -Force
}

if (Test-Path $startMenuDir) {
    Remove-Item -LiteralPath $startMenuDir -Recurse -Force
}

if (Test-Path $installRoot) {
    Remove-Item -LiteralPath $installRoot -Recurse -Force
}

[System.Windows.Forms.MessageBox]::Show(
    ""CsvDesktopAnalyzer 已卸载。"",
    ""卸载完成"",
    [System.Windows.Forms.MessageBoxButtons]::OK,
    [System.Windows.Forms.MessageBoxIcon]::Information
) | Out-Null
";
    }
}
