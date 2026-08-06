
namespace HyperBoot_Beta
{
    public class VentoyFinder
    {

        public static string GetVentoyDrive()
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.DriveType == DriveType.Removable && drive.IsReady)
                {
                    try
                    {
                        string ventoyFolderPath = Path.Combine(drive.RootDirectory.FullName, "ventoy");

                        if (Directory.Exists(ventoyFolderPath))
                        {
                            return drive.RootDirectory.FullName;
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            return null; // Bulunamadı
        }
    }
}





