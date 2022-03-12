using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Text;
using System.Windows.Forms;

namespace Frankensteiner
{
    public partial class UpdateChecker
    {
        public string Version { get; private set; }
        private static string GITHUB_FILE = "https://raw.githubusercontent.com/Dealman/Frankensteiner/master/version.txt"; // Be sure to change Halen84 to Dealman
        private string latestVersion;
        public readonly int Timeout = 500;

        public UpdateChecker()
        {
            SetVersion();
        }

        public void CheckLatestVersion(int timeout, bool showMessage = true)
        {
            try
            {
                Thread latestVersionFetchThread;
                latestVersionFetchThread = new Thread(() => DownloadUrlSynchronously(GITHUB_FILE));
                latestVersionFetchThread.Start();
                latestVersionFetchThread.Join(timeout);

                if (!latestVersionFetchThread.IsAlive)
                {
                    if (latestVersion.CompareTo(Version) <= 0)
                    {
                        if (showMessage)
                        {
                            MessageBox.Show("You have the latest version avaiable.", "Frankensteiner", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        if (MessageBox.Show("A new update is available! Would you like to install it?", "Frankensteiner", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            Process.Start("https://github.com/Dealman/Frankensteiner/releases/latest");
                            //Process.Start("https://github.com/Halen84/Frankensteiner/releases/latest");
                        }
                    }
                }
                else
                {
                    DialogResult result = MessageBox.Show("Failed to fetch latest update. Retry?", "Frankensteiner", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
                    if (result == DialogResult.Retry)
                    {
                        CheckLatestVersion(Timeout);
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        latestVersionFetchThread.Abort();
                    }

                }
            }
            catch (WebException)
            {
                if (MessageBox.Show("Failed to fetch latest update. Retry?", "Frankensteiner", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) == DialogResult.Retry)
                {
                    CheckLatestVersion(Timeout);
                }
            }
            catch (ThreadInterruptedException)
            {
                if (MessageBox.Show("Failed to fetch latest update. Retry?", "Frankensteiner", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning) == DialogResult.Retry)
                {
                    CheckLatestVersion(Timeout);
                }
            }
        }

        private void DownloadUrlSynchronously(string url)
        {
            try
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                // Fetch version text file
                WebClient client = new WebClient();
                string version = client.DownloadString(url);
                latestVersion = version.Trim();
            }
            catch (WebException)
            {
                // I should probably add a retry here too but idk
                MessageBox.Show("Failed to fetch update version.", "Frankensteiner", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetVersion()
        {
            // Set Version to the assembly version (Frankensteiner's version)
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            Version = fvi.FileVersion;
        }
    }
}
