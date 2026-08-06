using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static HyperBoot_Beta.Form1;

namespace HyperBoot_Beta
{
    public partial class İnstall : Form
    {
        public string ISOdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ISO Files");
        public string osname;
        public string osversion;
        public string osarch;
        bool isDownloading = false;

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private string osdownloadlink;
        private HttpClient _httpClient = new HttpClient();
        private bool _isClosingPromptOpen = false;

        public İnstall(string osname, string osarch, string osversion, string downloadlink)
        {
            InitializeComponent();
            this.osname = osname;
            this.osarch = osarch;
            this.osversion = osversion;
            this.osdownloadlink = downloadlink;

            label13.Text = osname;
            cuiPanel16.Width = label13.Width + 50;
            label2.Text = osarch;
            label3.Text = osversion;
        }

        private void label2_Click(object sender, EventArgs e)
        {
        }

        private void İnstall_Load(object sender, EventArgs e)
        {
        }

        // Pencereyi sürüklemek için
        private void cuiPanel1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, 0xA1, 0x2, 0);
            }
        }

        // Kapatma butonu
        private void cuiButton1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // Simge durumuna küçültme butonu
        private void cuiButton2_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private async void cuiButton3_Click(object sender, EventArgs e)
        {
            if (isDownloading)
            {
                MessageBox.Show("A download is already in progress. Please wait for it to finish before starting a new one.", "Download in Progress", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                isDownloading = true;

                if (!Directory.Exists(ISOdir))
                {
                    Directory.CreateDirectory(ISOdir);
                }

                var progress = new Progress<int>(value =>
                {
                    if (!this.IsDisposed)
                    {
                        cuiProgressBarHorizontal1.Value = value;
                        label4.Text = $"{value}%";
                    }
                });

                string savePath = Path.Combine(ISOdir, $"{osname}_{osversion}_{osarch}.iso");

                await DownloadFileStreamAsync(_httpClient, osdownloadlink, savePath, progress);

                if (!this.IsDisposed)
                {
                    MessageBox.Show("Download completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during the download: {ex.Message}", "Download Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                isDownloading = false;
            }
        }

        private void Install_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isDownloading)
            {
                if (_isClosingPromptOpen)
                {
                    e.Cancel = true;
                    return;
                }

                _isClosingPromptOpen = true;

                DialogResult result = MessageBox.Show(
                    "Download is in progress.\nIf you exit, the process will be cancelled. Are you sure?",
                    "Confirm Exit",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                _isClosingPromptOpen = false;

                if (result == DialogResult.Yes)
                {
                    _httpClient?.CancelPendingRequests();
                    isDownloading = false;
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }
    }
}