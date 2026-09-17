using HartUI.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using static HyperBoot_Beta.HyperBootAIService;
using static System.Net.WebRequestMethods;

namespace HyperBoot_Beta
{
    public partial class Form1 : Form
    {
        bool downloading = false;
        bool downloading_ventoy = false;
        bool downloading_balena = false;

        private static readonly HttpClient _httpclient = new HttpClient();

        public string appdir = AppDomain.CurrentDomain.BaseDirectory;
        public string configdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
        public string utilitiesdirventoy = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "utilities", "ventoy-1.1.17");
        public string utilitiesdirbalena = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "utilities", "balena");
        public string ventoydir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "utilities", "ventoy-1.1.17.zip");
        public string balenaclidir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "utilities", "balena-cli-v25.2.0-windows-x64-standalone.tar.gz");
        public string utilitiesdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "utilities");
        public string configfile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "config.json");
        public string catalogfile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "catalog.json");
        public string ISOdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ISO Files");
        public string rawfileUri = "https://raw.githubusercontent.com/YahyaEmir2345/HyperBoot-OS-Catalog/refs/heads/main/OSCatalog.json";
        public string balenacliUri = "https://github.com/balena-io/balena-cli/releases/download/v25.2.0/balena-cli-v25.2.0-windows-x64-standalone.tar.gz";
        public string filetowrite;

        class Config
        {
            public string language = "en";
        }

        public class SystemItem
        {
            [JsonProperty("osName")]
            public string OsName { get; set; }

            [JsonProperty("osVersion")]
            public string OsVersion { get; set; }

            [JsonProperty("osArchitecture")]
            public string OsArchitecture { get; set; }

            [JsonProperty("officialSource")]
            public string OfficialSource { get; set; }

            [JsonProperty("downloadUrl")]
            public string DownloadUrl { get; set; }

            [JsonProperty("sha256Hash")]
            public string Sha256Hash { get; set; }
        }

        public class Catalog
        {
            [JsonProperty("catalogVersion")]
            public string CatalogVersion { get; set; }

            [JsonProperty("lastUpdated")]
            public string LastUpdated { get; set; }

            [JsonProperty("systems")]
            public List<SystemItem> Systems { get; set; }
        }

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        public Form1()
        {
            InitializeComponent();
        }

        public static async Task DownloadFile(string targetpath, string downloadUri)
        {
            byte[] data = await _httpclient.GetByteArrayAsync(downloadUri);
            await System.IO.File.WriteAllBytesAsync(targetpath, data);
        }

        private async Task ConfigureApp()
        {
            try
            {
                if (!Directory.Exists(configdir))
                {
                    label4.Text = "Creating configuration directory...";
                    Directory.CreateDirectory(configdir);
                    label4.Text = "Configuration directory created.";
                }

                if (!System.IO.File.Exists(configfile))
                {
                    label4.Text = "Creating configuration file...";
                    await System.IO.File.WriteAllTextAsync(configfile, JsonConvert.SerializeObject(new Config(), Formatting.Indented));
                    label4.Text = "Configuration file created.";
                }

                if (!System.IO.File.Exists(catalogfile))
                {
                    label4.Text = "Downloading and configuring the catalog file...";
                    await DownloadFile(catalogfile, rawfileUri);
                    label4.Text = "Catalog file configured.";
                }

                if (!Directory.Exists(ISOdir))
                {
                    label4.Text = "Creating ISO Files directory...";
                    Directory.CreateDirectory(ISOdir);
                    label4.Text = "ISO Files directory created.";
                }

                label4.Text = "All Systems are operational.";
            }
            catch (Exception ex)
            {
                label4.Text = "An error occurred: " + ex.Message;
            }
            // Ventoy Configure Code
            try
            {
                label4.Text = "Checking Ventoy";
                long? contentLength = null;
                using (var headRequest = new HttpRequestMessage(HttpMethod.Head, "https://github.com/ventoy/Ventoy/releases/download/v1.1.17/ventoy-1.1.17-windows.zip"))
                {
                    using (var headResponse = await _httpclient.SendAsync(headRequest))
                    {
                        if (headResponse.IsSuccessStatusCode)
                        {
                            contentLength = headResponse.Content.Headers.ContentLength;
                        }
                    }
                }

                var dirventoyfolder = new DirectoryInfo(utilitiesdirventoy);
                long utilitiesdirventoylength = dirventoyfolder.TakeLength();

                if (!Directory.Exists(utilitiesdir))
                {
                    Directory.CreateDirectory(utilitiesdir);
                }

                bool VentoyneedsDownload = false;
                bool VentoyneedsExtraction = false;

                if (!System.IO.File.Exists(ventoydir))
                {
                    VentoyneedsDownload = true;
                    VentoyneedsExtraction = true;
                }
                else if (contentLength.HasValue && new FileInfo(ventoydir).Length != contentLength.Value)
                {
                    VentoyneedsDownload = true;
                    VentoyneedsExtraction = true;
                }
                else if (!Directory.Exists(utilitiesdirventoy) || utilitiesdirventoylength == 0)
                {
                    VentoyneedsExtraction = true;
                }

                if (VentoyneedsDownload)
                {
                    label4.Text = "Downloading Ventoy";
                    downloading = true;
                    downloading_ventoy = true;
                    label17.Text = "Downloading";

                    await DownloadFileStreamAsync(_httpclient, "https://github.com/ventoy/Ventoy/releases/download/v1.1.17/ventoy-1.1.17-windows.zip", ventoydir);
                }

                if (VentoyneedsExtraction)
                {
                    label4.Text = "Extracting Ventoy";
                    label17.Text = "Extracting";
                    await Task.Run(() =>
                    {
                        if (Directory.Exists(utilitiesdirventoy))
                        {
                            Directory.Delete(utilitiesdirventoy, true);
                        }
                        ZipFile.ExtractToDirectory(ventoydir, utilitiesdirventoy);
                    });
                }

                downloading_ventoy = false;
                downloading = false;
                label17.Text = "Available";

                if (!comboBox3.Items.Contains("Ventoy 1.1.17"))
                {
                    comboBox3.Items.Add("Ventoy 1.1.17");
                }
                label4.Text = "Ventoy utilities configured successfully.";
                Task.Delay(500).Wait();
                label4.Text = "All Systems are operational.";
            }
            catch (Exception ex)
            {
                downloading_ventoy = false;
                downloading = false;
                label17.Text = "Unavailable";
                MessageBox.Show("Error configuring or downloading the Ventoy utilities: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                label4.Text = "An error occurred while configuring Ventoy.";
            }

            try
            {
                long? contentLength = null;
                using (var headRequest = new HttpRequestMessage(HttpMethod.Head, balenacliUri))
                {
                    using (var headResponse = await _httpclient.SendAsync(headRequest))
                    {
                        if (headResponse.IsSuccessStatusCode)
                        {
                            contentLength = headResponse.Content.Headers.ContentLength;
                        }
                    }
                }

                var dirventoyfolder = new DirectoryInfo(utilitiesdirbalena);

                long utilitiesdirbalenalength = dirventoyfolder.Exists ? dirventoyfolder.TakeLength() : 0;

                bool needsDownload = false;
                bool needsExtraction = false;

                if (!System.IO.File.Exists(balenaclidir))
                {
                    needsDownload = true;
                    needsExtraction = true;
                }
                else if (contentLength.HasValue && new FileInfo(balenaclidir).Length != contentLength.Value)
                {
                    needsDownload = true;
                    needsExtraction = true;
                }
                else if (!Directory.Exists(utilitiesdirbalena) || utilitiesdirbalenalength == 0)
                {
                    needsExtraction = true;
                }

                if (needsDownload)
                {
                    downloading = true;
                    downloading_balena = true;
                    label18.Text = "Downloading...";
                    await DownloadFileStreamAsync(_httpclient, balenacliUri, balenaclidir);
                }

                if (needsExtraction)
                {
                    downloading = true;
                    label18.Text = "Extracting...";
                    await Task.Run(() =>
                    {
                        if (Directory.Exists(utilitiesdirbalena))
                        {
                            Directory.Delete(utilitiesdirbalena, true);
                        }
                        using (FileStream originalFileStream = System.IO.File.OpenRead(balenaclidir))
                        using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                        {
                            TarFile.ExtractToDirectory(decompressionStream, utilitiesdir, overwriteFiles: true);
                        }
                    });
                }

                downloading = false;
                downloading_balena = false;
                label18.Text = "Unavailable in testing.";

                if (comboBox3.Items.Contains("Balena CLI v25.2.0"))
                {
                    comboBox3.Items.Remove("Balena CLI v25.2.0");
                }
            }
            catch (Exception ex)
            {
                downloading = false;
                downloading_balena = false;
                label18.Text = "Unavailable";

                System.Diagnostics.Debug.WriteLine("Balena Kurulum Hatası: " + ex.Message);
            }

        }

        public static async Task DownloadFileStreamAsync(HttpClient httpClient, string fileUri, string outputPath, IProgress<int> progress = null)
        {
            using (var response = await httpClient.GetAsync(fileUri, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                long? totalBytes = response.Content.Headers.ContentLength;

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    byte[] buffer = new byte[8192];
                    long totalReadBytes = 0;
                    int readBytes;

                    while ((readBytes = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, readBytes);
                        totalReadBytes += readBytes;

                        if (totalBytes.HasValue && totalBytes.Value > 0)
                        {
                            int progressPercentage = (int)((double)totalReadBytes / totalBytes.Value * 100);
                            progress?.Report(progressPercentage);
                        }
                    }
                }
            }
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await ConfigureApp();
            if (System.IO.File.Exists(catalogfile))
            {
                string jsondata = await System.IO.File.ReadAllTextAsync(catalogfile);
                Catalog catalog = JsonConvert.DeserializeObject<Catalog>(jsondata);

                if (catalog != null)
                {
                    label11.Text = string.IsNullOrWhiteSpace(catalog.LastUpdated) ? "Unknown" : catalog.LastUpdated;
                    label7.Text = string.IsNullOrWhiteSpace(catalog.CatalogVersion) ? "Unknown" : catalog.CatalogVersion;

                    if (catalog.Systems != null)
                    {
                        var cardTexts = catalog.Systems.Select(sys => $"{sys.OsName} {sys.OsVersion} ({sys.OsArchitecture})");

                        InfoCardClass infoCard = new InfoCardClass(flowLayoutPanel1, cardTexts);

                        infoCard.CardClicked += (s, args) =>
                        {
                            var selectedSystem = catalog.Systems[args.Index];
                            İnstall install = new İnstall(selectedSystem.OsName, selectedSystem.OsArchitecture, selectedSystem.OsVersion, selectedSystem.DownloadUrl);
                            install.Show();
                        };
                    }
                }
            }
        }

        private void cuiButton1_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void cuiPanel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, 0xA1, 0x2, 0);
            }
        }

        private void cuiButton2_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void cuiButton3_Click(object sender, EventArgs e)
        {
            flowLayoutPanel1.Visible = true;
            cuiPanel4.Visible = false;
            cuiPanel10.Visible = false;
            cuiPanel24.Visible = false;
            cuiPanel16.Visible = false;
        }

        private void cuiButton4_Click(object sender, EventArgs e)
        {
            cuiPanel4.Visible = true;
            flowLayoutPanel1.Visible = false;
            cuiPanel10.Visible = false;
            cuiPanel24.Visible = false;
            cuiPanel16.Visible = false;
        }

        private void cuiButton7_Click(object sender, EventArgs e)
        {
            flowLayoutPanel1.Visible = false;
            cuiPanel4.Visible = false;
            cuiPanel10.Visible = true;
            cuiPanel24.Visible = false;
            cuiPanel16.Visible = false;
        }

        private void cuiButton6_Click(object sender, EventArgs e)
        {
            flowLayoutPanel1.Visible = false;
            cuiPanel4.Visible = false;
            cuiPanel10.Visible = false;
            cuiPanel24.Visible = true;
            cuiPanel16.Visible = false;
        }

        private async void cuiButton10_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Checking Catalog Updates...", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            try
            {
                var response = await _httpclient.GetAsync(rawfileUri);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                var webCatalog = JsonConvert.DeserializeObject<Catalog>(content);

                if (webCatalog == null)
                {
                    MessageBox.Show("Failed to parse the catalog from the server.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!System.IO.File.Exists(catalogfile))
                {
                    await System.IO.File.WriteAllTextAsync(catalogfile, content);
                    label7.Text = webCatalog.CatalogVersion;
                    label11.Text = webCatalog.LastUpdated;
                    MessageBox.Show("Catalog downloaded successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var configCatalog = JsonConvert.DeserializeObject<Catalog>(await System.IO.File.ReadAllTextAsync(catalogfile));

                if (configCatalog == null || webCatalog.CatalogVersion != configCatalog.CatalogVersion)
                {
                    MessageBox.Show($"New catalog version available: {webCatalog.CatalogVersion}. Updating...", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await System.IO.File.WriteAllTextAsync(catalogfile, content);
                    label7.Text = webCatalog.CatalogVersion;
                    label11.Text = webCatalog.LastUpdated;
                    MessageBox.Show("Catalog updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Catalog is up to date.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating catalog: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (downloading | downloading_ventoy | downloading_balena)
            {
                DialogResult dialog = MessageBox.Show("A download is in progress. Are you sure you want to exit?", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialog == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox3.SelectedItem == null)
            {
                return;
            }

            if (comboBox3.SelectedItem.ToString() == "Ventoy 1.1.17")
            {
                comboBox1.Text = "";
                comboBox1.Items.Clear();

                foreach (string ventoyDrive in VentoyFinder.GetVentoyDrives())
                {
                    if (!comboBox1.Items.Contains(ventoyDrive))
                    {
                        comboBox1.Items.Add(ventoyDrive);
                    }
                }
            }

        }

        private void cuiButton9_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Select an ISO File";
            openFileDialog1.InitialDirectory = ISOdir;
            openFileDialog1.FileName = "";
            openFileDialog1.Filter = "ISO Files|*.iso";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (!string.IsNullOrWhiteSpace(openFileDialog1.FileName))
                {
                    label8.Text = openFileDialog1.FileName;
                }
            }
        }

        private void cuiButton5_Click(object sender, EventArgs e)
        {
            if (comboBox3.SelectedItem != null && comboBox3.SelectedItem.ToString() == "Ventoy 1.1.17")
            {
                comboBox1.Items.Clear();
                comboBox1.SelectedItem = null;

                foreach (string ventoyDrive in VentoyFinder.GetVentoyDrives())
                {
                    if (!comboBox1.Items.Contains(ventoyDrive))
                    {
                        comboBox1.Items.Add(ventoyDrive);
                    }
                }
            }
            else
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (drive.DriveType == DriveType.Removable && drive.IsReady)
                        {
                            string driveIdentifier = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? drive.Name : $"{drive.Name} [{drive.VolumeLabel}]";

                            if (!comboBox1.Items.Contains(driveIdentifier))
                            {
                                comboBox1.Items.Add(driveIdentifier);
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
        }

        private async void cuiButton8_Click(object sender, EventArgs e)
        {
            if (label8.Text == "Unknown" || string.IsNullOrWhiteSpace(label8.Text))
            {
                MessageBox.Show("Please select an ISO file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(comboBox1.Text))
            {
                MessageBox.Show("Please select a drive first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (string.IsNullOrWhiteSpace(comboBox3.Text))
            {
                MessageBox.Show("Please select a write engine first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            DialogResult dialog = MessageBox.Show($"Are you sure you want to write the ISO file '{label8.Text}' to the drive '{comboBox1.Text}' using '{comboBox3.Text}'? This will erase all data on the selected drive.", "Confirm Write", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dialog == DialogResult.Yes)
            {
                try
                {
                    string ResourcePath = label8.Text;
                    string FileName = System.IO.Path.GetFileName(ResourcePath);
                    string cleanDrivePath = comboBox1.Text.Split(' ')[0];
                    string hedefYolu = System.IO.Path.Combine(cleanDrivePath, FileName);
                    using FileStream kaynakAkisi = System.IO.File.OpenRead(ResourcePath);
                    using FileStream hedefAkisi = System.IO.File.Create(hedefYolu);

                    await kaynakAkisi.CopyToAsync(hedefAkisi);
                    MessageBox.Show("Successfully Completed", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void cuiButton11_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
            "You are about to be redirected to Ventoy.\n\n" +
            "Please note: Installing Ventoy will erase all data on the selected removable drive. You will be able to review your drive choice inside the Ventoy app before proceeding.\n\n" +
            "Would you like to continue?",
            "Redirect Confirmation",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);
            if (result == DialogResult.Yes)
            {
                Process.Start(Path.Combine(utilitiesdirventoy, "ventoy-1.1.17", "Ventoy2Disk.exe"));
            }
        }

        private void cuiButton14_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/YahyaEmir2345/HyperBoot-Beta/tree/master",
                UseShellExecute = true
            });
        }

        private void cuiButton12_Click(object sender, EventArgs e)
        {
            flowLayoutPanel1.Visible = false;
            cuiPanel4.Visible = false;
            cuiPanel10.Visible = false;
            cuiPanel24.Visible = false;
            cuiPanel16.Visible = true;
        }

        private void cuiPanel22_Paint(object sender, PaintEventArgs e)
        {

        }

        private async void cuiButton13_Click(object sender, EventArgs e)
        {
            var recommendation = await HyperBootAIService.GetOsRec(cuiTextBox2.Text, cuiTextBox1.Text, cuiTextBox3.Text, cuiTextBox4.Text, richTextBox1.Text);
            if (recommendation != null)
            {
                string message = $"Recommended OS: {recommendation.RecommendedOs}\n" +
                                 $"Compliance Rate: %{recommendation.MatchPercentage}\n\n" +
                                 $"Description: {recommendation.Reason}";

                MessageBox.Show(message, "AI Suggestion", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    "The AI could not be answered. Check your internet connection or proxy server.",
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        private void cuiButton15_Click(object sender, EventArgs e)
        {

        }
    }
}

public static class DirectoryExtensions
{
    public static long TakeLength(this DirectoryInfo di)
    {
        if (!di.Exists) return 0;
        try
        {
            return di.EnumerateFiles("*", SearchOption.AllDirectories).Sum(f => f.Length);
        }
        catch
        {
            return 0;
        }
    }
}