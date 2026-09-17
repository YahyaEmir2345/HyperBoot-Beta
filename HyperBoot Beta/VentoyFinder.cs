using System;
using System.Collections.Generic;
using System.IO;
using System.Management; // NuGet: System.Management

namespace HyperBoot_Beta
{
    public class VentoyFinder
    {
        public static List<string> GetVentoyDrives()
        {
            List<string> ventoyDrives = new List<string>();

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Removable && drive.IsReady)
                {
                    if (IsVentoyDrive(drive))
                    {
                        ventoyDrives.Add(drive.RootDirectory.FullName);
                    }
                }
            }

            return ventoyDrives;
        }

        public static bool IsVentoyDrive(DriveInfo drive)
        {
            try
            {
                if (string.Equals(drive.VolumeLabel, "Ventoy", StringComparison.OrdinalIgnoreCase) ||
                    Directory.Exists(Path.Combine(drive.RootDirectory.FullName, "ventoy")))
                {
                    return true;
                }

                string driveLetter = drive.Name.TrimEnd('\\'); 

                using (var searcher = new ManagementObjectSearcher(
                    $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{driveLetter}'}} WHERE ResultClass=Win32_DiskPartition"))
                {
                    foreach (ManagementObject partition in searcher.Get())
                    {
                        uint diskIndex = (uint)partition["DiskIndex"];

                        using (var partSearcher = new ManagementObjectSearcher(
                            $"SELECT * FROM Win32_DiskPartition WHERE DiskIndex={diskIndex}"))
                        {
                            foreach (ManagementObject part in partSearcher.Get())
                            {
                                string partName = part["Name"]?.ToString() ?? "";

                                if (partName.IndexOf("VTOYEFI", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return false;
        }
    }
}