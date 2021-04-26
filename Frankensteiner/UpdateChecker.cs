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
		private static string VERSION;
		private static string GITHUB_FILE = "https://raw.githubusercontent.com/Halen84/Frankensteiner/master/version.txt"; // Be sure to change Halen84 to Dealman
		private string latestVersion;
		private int Timeout = 500;

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
					if (latestVersion.CompareTo(VERSION) <= 0)
					{
						if (showMessage)
						{
							MessageBox.Show("You have the latest version avaiable.", "Frankensteiner", MessageBoxButtons.OK, MessageBoxIcon.Information);
						}
					}
					else
					{
						var result = MessageBox.Show("A new update is avaiable! Would you like to install it?", "Frankensteiner", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
						if (result == DialogResult.Yes)
						{
							Process.Start("https://github.com/Dealman/Frankensteiner/releases/latest");
						}
					}
				}
				else
				{
					var result = MessageBox.Show("Failed to fetch latest update. Retry?", "Frankensteiner", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
					if (result == DialogResult.Retry)
					{
						CheckLatestVersion(Timeout);
					}
					else if(result == DialogResult.Cancel)
					{
						latestVersionFetchThread.Abort();
					}

				}
			}
			catch (WebException ex)
			{
				MessageBox.Show(ex.ToString(), "Frankensteiner", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			catch (ThreadInterruptedException ex)
			{
				MessageBox.Show(ex.ToString(), "Frankensteiner", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void DownloadUrlSynchronously(string url)
		{
			try
			{
				ServicePointManager.Expect100Continue = true;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
				ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

				WebClient client = new WebClient();
				string version = client.DownloadString(url);
				latestVersion = version.Trim();
			}
			catch (WebException ex)
			{
				// I should probably add a retry here too but idk
				MessageBox.Show(ex.ToString(), "Frankensteiner", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void SetVersion()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
			VERSION = fvi.FileVersion;
		}

		public static string GetVersion()
		{
			return VERSION;
		}
	}
}
