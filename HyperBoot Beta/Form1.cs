using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HyperBoot_Beta
{
    public partial class Form1 : Form
    {
        bool downloading = false;
        bool downloading_ventoy = false;

        private static readonly HttpClient _httpclient = new HttpClient();

        public string appdir = AppDomain.CurrentDomain.BaseDirectory;
        public string configdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
        public string utilitiesdirventoy = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "utilities", "ventoy-1.1.17");
        public string ventoydir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "utilities", "ventoy-1.1.17.zip");
        public string utilitiesdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "utilities");
        public string configfile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "config.json");
        public string catalogfile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "catalog.json");
        public string ISOdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ISO Files");
        public string rawfileUri = "https://raw.githubusercontent.com/YahyaEmir2345/HyperBoot-OS-Catalog/refs/heads/main/OSCatalog.json";

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
            await File.WriteAllBytesAsync(targetpath, data);
        }

        private async Task ConfigureApp()
        {
            try
            {
                long? contentLength;
                using (var headRequest = new HttpRequestMessage(HttpMethod.Head, "https://github.com/ventoy/Ventoy/releases/download/v1.1.17/ventoy-1.1.17-windows.zip"))
                using (var headResponse = await _httpclient.SendAsync(headRequest))
                {
                    headResponse.EnsureSuccessStatusCode();
                    contentLength = headResponse.Content.Headers.ContentLength;
                }

                var dirventoyfolder = new DirectoryInfo(utilitiesdirventoy);
                long utilitiesdirventoylength = dirventoyfolder.TakeLength();

                if (!Directory.Exists(utilitiesdir))
                {
                    Directory.CreateDirectory(utilitiesdir);
                }

                bool needsDownload = false;
                bool needsExtraction = false;

                if (!File.Exists(ventoydir))
                {
                    needsDownload = true;
                    needsExtraction = true;
                }
                else if (contentLength.HasValue && new FileInfo(ventoydir).Length != contentLength.Value)
                {
                    needsDownload = true;
                    needsExtraction = true;
                }
                else if (!Directory.Exists(utilitiesdirventoy) || utilitiesdirventoylength == 0)
                {
                    needsExtraction = true;
                }

                if (needsDownload)
                {
                    downloading = true;
                    downloading_ventoy = true;
                    label17.Text = "Downloading";

                    await DownloadFileStreamAsync(_httpclient, "https://github.com/ventoy/Ventoy/releases/download/v1.1.17/ventoy-1.1.17-windows.zip", ventoydir);
                }

                if (needsExtraction)
                {
                    label17.Text = "Extracting";
                    await Task.Run(() =>
                    {
                        if (Directory.Exists(utilitiesdirventoy)) Directory.Delete(utilitiesdirventoy, true);
                        ZipFile.ExtractToDirectory(ventoydir, utilitiesdirventoy);
                    });
                }

                downloading_ventoy = false;
                downloading = false;
                label17.Text = "Available";
                cuiComboBox3.AddItem("Ventoy 1.1.17");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error configuring or downloading the Ventoy utilities: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            try
            {
                if (!Directory.Exists(configdir))
                {
                    label4.Text = "Creating configuration directory...";
                    Directory.CreateDirectory(configdir);
                    label4.Text = "Configuration directory created.";
                }

                if (!File.Exists(configfile))
                {
                    label4.Text = "Creating configuration file...";
                    File.WriteAllText(configfile, JsonConvert.SerializeObject(new Config()));
                    label4.Text = "Configuration file created.";
                }

                if (!File.Exists(catalogfile))
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
        }

        public static async Task DownloadFileStreamAsync(
        HttpClient httpClient,
        string fileUri,
        string outputPath,
        IProgress<int> progress = null)
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
            if (label17.Text == "Avaliable")
            {
                cuiComboBox3.AddItem(label17.Text);
            }
            if (File.Exists(catalogfile))
            {
                string jsondata = await File.ReadAllTextAsync(catalogfile);
                Catalog catalog = JsonConvert.DeserializeObject<Catalog>(jsondata);

                if (catalog != null)
                {
                    label11.Text = catalog.LastUpdated;
                    label7.Text = catalog.CatalogVersion;

                    if (catalog.Systems != null)
                    {
                        var cardTexts = catalog.Systems.Select(sys =>
                            $"{sys.OsName} {sys.OsVersion} ({sys.OsArchitecture})"
                        );

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
        }

        private void cuiButton4_Click(object sender, EventArgs e)
        {
            cuiPanel4.Visible = true;
            flowLayoutPanel1.Visible = false;
            cuiPanel10.Visible = false;
            cuiPanel24.Visible = false;
        }

        private void cuiButton7_Click(object sender, EventArgs e)
        {
            flowLayoutPanel1.Visible = false;
            cuiPanel4.Visible = false;
            cuiPanel10.Visible = true;
            cuiPanel24.Visible = false;
        }

        private void cuiButton6_Click(object sender, EventArgs e)
        {
            flowLayoutPanel1.Visible = false;
            cuiPanel4.Visible = false;
            cuiPanel10.Visible = false;
            cuiPanel24.Visible = true;
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

                if (!File.Exists(catalogfile))
                {
                    File.WriteAllText(catalogfile, content);
                    label7.Text = webCatalog.CatalogVersion;
                    label11.Text = webCatalog.LastUpdated;
                    MessageBox.Show("Catalog downloaded successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var configCatalog = JsonConvert.DeserializeObject<Catalog>(await File.ReadAllTextAsync(catalogfile));

                if (configCatalog == null || webCatalog.CatalogVersion != configCatalog.CatalogVersion)
                {
                    MessageBox.Show($"New catalog version available: {webCatalog.CatalogVersion}. Updating...", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    File.WriteAllText(catalogfile, content);
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
            if (downloading)
            {
                DialogResult dialog = MessageBox.Show("A download is in progress. Are you sure you want to exit?", "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialog == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void cuiComboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(cuiComboBox3.SelectedItem == "Ventoy 1.1.17")
            {
                cuiPanel6.Visible = false;
                label3.Visible = false;
                cuiComboBox2.Visible = false;
            } else 
            {
                cuiPanel6.Visible = true;
                label3.Visible = true;
                cuiComboBox2.Visible = true;
            }
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