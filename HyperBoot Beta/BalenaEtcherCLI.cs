using System;
using System.Diagnostics;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace HyperBoot_Beta
{
    internal class BalenaEtcherCLI
    {
        public static async Task FlashToUSB(string CLIPath, string FlashFilePath, string USBPath, IProgress<string> progress)
        {
            string drivepath = GetPhysicalDrivePath(USBPath);

            if (string.IsNullOrEmpty(drivepath))
            {
                throw new Exception("USB Path is not valid or not found.");
            }

            string arguments = $"local flash \"{FlashFilePath}\" --drive \"{drivepath}\" --yes";

            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = CLIPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            StringBuilder errorBuilder = new StringBuilder();

            try
            {
                using (Process process = new Process { StartInfo = processInfo })
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            progress?.Report(e.Data);
                            Console.WriteLine(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            errorBuilder.AppendLine(e.Data);
                            progress?.Report("Error: " + e.Data);
                            Console.WriteLine("Error: " + e.Data);
                        }
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"BalenaEtcherCLI exited with code {process.ExitCode}. Error: {errorBuilder.ToString()}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while flashing the USB drive: {ex.Message}", ex);
            }
        }

        private static string GetPhysicalDrivePath(string driveLetter)
        {
            try
            {
                driveLetter = driveLetter.TrimEnd('\\');

                using (var searcher = new ManagementObjectSearcher(
                    $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{driveLetter}'}} WHERE ResultClass=Win32_DiskPartition"))
                {
                    foreach (ManagementObject partition in searcher.Get())
                    {
                        uint diskIndex = (uint)partition["DiskIndex"];
                        return $@"\\.\PhysicalDrive{diskIndex}";
                    }
                }
            }
            catch
            {
            }

            return null;
        }
    }
}