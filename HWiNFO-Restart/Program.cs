using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Threading;

namespace HWiNFO_Restart
{
    class Program
    {
        private const string HWiNFO64Path = @"C:\Program Files\HWiNFO64\HWiNFO64.EXE";
        private static readonly TimeSpan RestartInterval = TimeSpan.FromHours(11);
        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

        static void Main(string[] args)
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                {
                    Log("Administrator privileges are required. Start the configured Task Scheduler task with Run with highest privileges; do not launch the executable directly. No elevation prompt will be requested.");
                    Environment.ExitCode = 1;
                    return;
                }
            }

            try
            {
                bool createdNew;
                using (var instance = new Mutex(true, @"Local\HWiNFO-Restart", out createdNew))
                {
                    if (!createdNew)
                    {
                        return;
                    }

                    var restartTimer = Stopwatch.StartNew();
                    while (true)
                    {
                        if (StartHWiNFO64IfNeeded(restartTimer.Elapsed >= RestartInterval))
                        {
                            restartTimer.Restart();
                        }

                        TimeSpan remaining = RestartInterval - restartTimer.Elapsed;
                        Thread.Sleep(remaining > TimeSpan.Zero && remaining < CheckInterval
                            ? remaining
                            : CheckInterval);
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Log("Cannot access the single-instance mutex: " + ex);
            }
        }

        private static bool StartHWiNFO64IfNeeded(bool restart)
        {
            Process[] processes = new Process[0];
            try
            {
                processes = Process.GetProcessesByName("HWiNFO64");
                if (!restart && processes.Length > 0)
                {
                    return false;
                }

                foreach (Process process in processes)
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        if (!process.WaitForExit(30000))
                        {
                            Log("Timed out waiting for HWiNFO64 to exit. Will retry in one minute.");
                            return false;
                        }
                    }
                }

                // HWiNFO64 requests uiAccess; even an elevated caller must use the shell launcher.
                var startInfo = new ProcessStartInfo
                {
                    FileName = HWiNFO64Path,
                    WorkingDirectory = Path.GetDirectoryName(HWiNFO64Path),
                    UseShellExecute = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        Log("Starting HWiNFO64 returned no process. Will retry in one minute.");
                        return false;
                    }
                }

                Log("Started HWiNFO64. Next scheduled restart is in 11 hours.");
                return true;
            }
            catch (Win32Exception ex)
            {
                Log("Cannot start or restart HWiNFO64. Will retry in one minute. " + ex);
                return false;
            }
            catch (InvalidOperationException ex)
            {
                Log("HWiNFO64 changed state during maintenance. Will retry in one minute. " + ex);
                return false;
            }
            finally
            {
                foreach (Process process in processes)
                {
                    process.Dispose();
                }
            }
        }

        private static void Log(string message)
        {
            try
            {
                string directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "HWiNFO-Restart");
                Directory.CreateDirectory(directory);
                File.AppendAllText(Path.Combine(directory, "restart.log"),
                    DateTimeOffset.Now.ToString("o") + " " + message + Environment.NewLine);
            }
            catch (IOException ex)
            {
                Trace.TraceError("{0}{1}Cannot write the HWiNFO-Restart log: {2}", message, Environment.NewLine, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Trace.TraceError("{0}{1}Cannot write the HWiNFO-Restart log: {2}", message, Environment.NewLine, ex);
            }
        }
    }
}

