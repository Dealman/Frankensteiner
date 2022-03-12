using ControlzEx.Theming;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;


namespace Frankensteiner
{
	public partial class MainWindow : MetroWindow
	{
		private string gameConfigPath;
		private ConfigParser config;
		private ObservableCollection<MercenaryItem> _loadedMercenaries = new ObservableCollection<MercenaryItem>();
		private MercenaryItem _copiedMercenary;
		private UpdateChecker updater = new UpdateChecker();

		public MainWindow()
		{
			InitializeComponent();

			#region Auto-find Game.ini + Set Default Backup Folder
			if (String.IsNullOrWhiteSpace(Properties.Settings.Default.cfgConfigPath))
			{
				if(System.Windows.MessageBox.Show("Would you like to automatically try and find the game configuration file? If you choose not to, you can do it manually in the settings.", "Information", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
				{
					try
					{
						string localDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
						if(File.Exists(localDataPath+@"\Mordhau\Saved\Config\WindowsClient\Game.ini"))
						{

							gameConfigPath = localDataPath + @"\Mordhau\Saved\Config\WindowsClient\Game.ini";

							tbConfigPath.Text = gameConfigPath;
							tbBackupPath.Text = gameConfigPath.Replace("Game.ini", "");
							Properties.Settings.Default.cfgConfigPath = gameConfigPath;
							Properties.Settings.Default.cfgBackupPath = gameConfigPath.Replace("Game.ini", "");
							Properties.Settings.Default.Save();
							System.Windows.MessageBox.Show("Successfully located the configuration file!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
						}
					} catch(Exception eggseption) {
						System.Windows.MessageBox.Show(String.Format("An error occured whilst trying to automagically find the configuration file! Error Message:\n\n{0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
			} else {
				gameConfigPath = Properties.Settings.Default.cfgConfigPath;
				tbConfigPath.Text = gameConfigPath;
			}
			#endregion

			#region Fetch & Validate Saved Mordhau Folder
			try
			{
				if (!String.IsNullOrWhiteSpace(Properties.Settings.Default.cfgMordhauPath))
				{
					// Just in case, let's also make sure it's still valid!
					string mordhauPath = Properties.Settings.Default.cfgMordhauPath;
					if (File.Exists(mordhauPath + @"\Mordhau.exe") && Directory.Exists(mordhauPath + @"\Mordhau\Content\Movies"))
					{
						tbMordhauPath.Text = mordhauPath;
					} else {
						tbMordhauPath.Text = "";
						Properties.Settings.Default.cfgMordhauPath = "";
						Properties.Settings.Default.Save();
						System.Windows.MessageBox.Show("The path to Mordhau's folder does not seem to valid anymore. Please verify and re-set it in the settings!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
					}
				}
			} catch (Exception eggseption) {
				System.Windows.MessageBox.Show(String.Format("An error occured whilst trying to browse to the game's folder! Error Message:\n\n{0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			#endregion

			#region Fetch Saved Backup Folder
			try
			{
				string backupPath = Properties.Settings.Default.cfgBackupPath;
				tbBackupPath.Text = backupPath;
			} catch (Exception eggseption) {
				System.Windows.MessageBox.Show(String.Format("An error occured whilst trying to browse to the last saved backup folder! Error Message:\n\n{0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			#endregion

			#region Fetch Settings
			// ZIP Backup
			cfgZIPBackup.IsChecked = Properties.Settings.Default.cfgZIPBackup;
			// ZIP Limiter
			nudZipLimit.Value = Properties.Settings.Default.cfgZIPLimit;
			// ZIP Limit Active
			cfgZIPLimitActive.IsChecked = Properties.Settings.Default.cfgZIPLimitActive;
			// Disable Startup Movies
			cbDisableMovies.IsChecked = Properties.Settings.Default.cfgDisableMovies;
			// Check Conflcits
			cfgCheckConflict.IsChecked = Properties.Settings.Default.cfgCheckConflict;
			// Conflict Warnings
			cfgConflictWarnings.IsChecked = Properties.Settings.Default.cfgConflictWarnings;
			// Auto Restart Mordhau
			cbAutoCloseGame.IsChecked = Properties.Settings.Default.cfgAutoClose;
			cfgRestartMordhau.IsChecked = Properties.Settings.Default.cfgRestartMordhau;
			cfgRestartMordhauMode.IsChecked = Properties.Settings.Default.cfgRestartMordhauMode;
			// Shortcuts
			cfgShortcutsEnabled.IsChecked = Properties.Settings.Default.cfgShortcutsEnabled;
			// Check for updates
			cfgUpdateOnStartup.IsChecked = Properties.Settings.Default.cfgUpdateOnStartup;
			#endregion

			#region Set Application Theme & Accent
			string[] appThemes = { "Dark", "Light" };
			string[] appAccents = { "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald", "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta", "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna" };

			try
			{
				cbAppThemes.SelectedItem = cbAppThemes.Items.OfType<ComboBoxItem>().FirstOrDefault(x => x.Content.ToString() == Properties.Settings.Default.appTheme);
				cbAppAccents.SelectedItem = cbAppAccents.Items.OfType<ComboBoxItem>().FirstOrDefault(x => x.Content.ToString() == Properties.Settings.Default.appAccent);
				if (appThemes.Contains(Properties.Settings.Default.appTheme) && appAccents.Contains(Properties.Settings.Default.appAccent))
				{
					ThemeManager.Current.ChangeTheme(System.Windows.Application.Current, Properties.Settings.Default.appTheme + "." + Properties.Settings.Default.appAccent);
				} else {
					ThemeManager.Current.ChangeTheme(System.Windows.Application.Current, "Dark.Cyan");
					Properties.Settings.Default.appTheme = "Dark";
					Properties.Settings.Default.appAccent = "Cyan";
					Properties.Settings.Default.Save();
					System.Windows.MessageBox.Show("Frankensteiner was unable to set the app theme. Default style has been applied instead.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			} catch (Exception eggseption) {
				System.Windows.MessageBox.Show(String.Format("An error occured while trying to set the app theme. Error Message:\n\n{0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			#endregion

			#region Set Application Startup Pos & Size
			// TODO: Make this if statement less unintuitive (if X or Y is exactly 0, it won't set position)
			if (Properties.Settings.Default.appStartupPos.X != 0 && Properties.Settings.Default.appStartupPos.Y != 0)
			{
				// Position
				metroWindow.Left = Properties.Settings.Default.appStartupPos.X;
				metroWindow.Top = Properties.Settings.Default.appStartupPos.Y;
				// Size
				metroWindow.Width = Properties.Settings.Default.appStartupSize.X;
				metroWindow.Height = Properties.Settings.Default.appStartupSize.Y;
			}

			// Set WindowState
			if (Properties.Settings.Default.isWindowMaximized == true)
			{
				this.WindowState = WindowState.Maximized;
			} else {
				this.WindowState = WindowState.Normal;
			}
			#endregion

			#region Version Stuff
			runVersion.Text = $"{updater.Version} | Made by Dealman | Maintained by TuffyTown";
			if (Properties.Settings.Default.cfgUpdateOnStartup == true)
			{
				updater.CheckLatestVersion(updater.Timeout, false);
			}
			#endregion

			lbCharacterList.ItemsSource = _loadedMercenaries;
			RefreshMercenaries(); // Automatically load mercenaries on app startup
		}

		#region Events & Methods for Changing of Theme/Accent
		// TODO: Try and get this to work via XAML binding. For some reason couldn't get it to work, so said fuck it.
		private void UpdateGridBackgrounds()
		{
			Grid[] grids = { gWindowBG, gMainBG, gSettingsBG, gConfigBG, gBackupsBG, gBackupBoxBG, gOtherBG, gAboutBG };
			if (Properties.Settings.Default.appTheme == "Light")
			{
				foreach (Grid grid in grids)
				{
					grid.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
				}
			} else {
				foreach (Grid grid in grids)
				{
					grid.Background = new SolidColorBrush(Color.FromRgb(69, 69, 69));
				}
			}
		}

		private void CbAppThemes_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBoxItem cbi = (ComboBoxItem)cbAppThemes.SelectedItem;
			ThemeManager.Current.ChangeTheme(System.Windows.Application.Current, cbi.Content.ToString() + "." + Properties.Settings.Default.appAccent);
			Properties.Settings.Default.appTheme = cbi.Content.ToString();
			Properties.Settings.Default.Save();
			UpdateGridBackgrounds();
			foreach (MercenaryItem mercenary in _loadedMercenaries)
			{
				SolidColorBrush newColor = (Properties.Settings.Default.appTheme == "Dark") ? new SolidColorBrush(Color.FromRgb(69, 69, 69)) : new SolidColorBrush(Color.FromRgb(245, 245, 245));
				mercenary.BackgroundColour = newColor;
			}
		}

		private void CbAppAccents_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBoxItem cbi = (ComboBoxItem)cbAppAccents.SelectedItem;
			ThemeManager.Current.ChangeTheme(System.Windows.Application.Current, Properties.Settings.Default.appTheme + "." + cbi.Content.ToString());
			Properties.Settings.Default.appAccent = cbi.Content.ToString();
			Properties.Settings.Default.Save();
			UpdateGridBackgrounds();
		}
		#endregion

		#region Metro Window Closing
		private void MetroWindow_Closing(object sender, CancelEventArgs e)
		{
			List<MercenaryItem> _modifiedMercs = GetModifiedMercenaries();

			Properties.Settings.Default.appStartupPos = new System.Drawing.Point(Convert.ToInt16(metroWindow.Left), Convert.ToInt16(metroWindow.Top));
			Properties.Settings.Default.appStartupSize = new System.Drawing.Point(Convert.ToInt16(metroWindow.ActualWidth), Convert.ToInt16(metroWindow.ActualHeight));

			if (this.WindowState == WindowState.Maximized)
			{
				Properties.Settings.Default.isWindowMaximized = true;
			} else {
				Properties.Settings.Default.isWindowMaximized = false;
			}
			Properties.Settings.Default.Save();

			if (bTitleSave.IsVisible == true)
			{
				DialogResult response = System.Windows.Forms.MessageBox.Show($"Warning!\n\nYou have {_modifiedMercs.Count.ToString()} unsaved mercenaries!\n\nWould you like to save them before exiting?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
				if (response == System.Windows.Forms.DialogResult.Yes)
				{
					SaveAllMercenaries(_modifiedMercs);
				} else if(response == System.Windows.Forms.DialogResult.Cancel)
				{
					e.Cancel = true;
				}
			}
		}
		#endregion

		#region Mercenary List Logic
		private void BRefreshCharacters_Click(object sender, RoutedEventArgs e)
		{
			RefreshMercenaries();
		}

		private void RefreshMercenaries()
		{
			try
			{
				if (!String.IsNullOrWhiteSpace(gameConfigPath) && File.Exists(gameConfigPath)) // Make sure it exists so we don't potentially run into problems
				{
					config = new ConfigParser(gameConfigPath);
					if (config.ParseConfig())
					{
						bTitleSave.Visibility = Visibility.Collapsed;
						if (_loadedMercenaries.Count > 0)
						{
							_loadedMercenaries.Clear();
						}
						config.ProcessMercenaries();
						for (int i = 0; i < config.Mercenaries.Count; i++)
						{
							_loadedMercenaries.Add(config.Mercenaries[i]);
						}
						for (int i = 0; i < _loadedMercenaries.Count; i++)
						{
							if (i % 2 == 0)
							{
								_loadedMercenaries[i].BackgroundColour = (Properties.Settings.Default.appTheme == "Dark") ? new SolidColorBrush(Color.FromRgb(69, 69, 69)) : new SolidColorBrush(Color.FromRgb(245, 245, 245));
							}
							else
							{
								_loadedMercenaries[i].BackgroundColour = (Properties.Settings.Default.appTheme == "Dark") ? new SolidColorBrush(Color.FromRgb(49, 49, 49)) : new SolidColorBrush(Color.FromRgb(225, 225, 225));
							}
						}
						lbCharacterList.IsEnabled = true;
						bShowTools.IsEnabled = true;
					}
				}
			}
			catch (Exception eggseption)
			{
				System.Windows.MessageBox.Show(String.Format("An error has occured whilst trying to parse the configuration file! Error Message:\n\n{0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void LbCharacterList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if(lbCharacterList.SelectedItems.Count == 1 && lbCharacterList.SelectedIndex >= 0)
			{
				MercenaryItem selectedMerc = lbCharacterList.SelectedItem as MercenaryItem;
				if (selectedMerc != null)
				{
					MercenaryEditor mercEditor = new MercenaryEditor(selectedMerc);
					mercEditor.Title = $"Mercenary Editor - Editing {selectedMerc.Name}";
					if (mercEditor.ShowDialog().Value)
					{
						// TODO: Check for changes here, if yes, enable titlebar save
						if(!selectedMerc.isOriginal)
						{
							bTitleSave.Visibility = Visibility.Visible;
						}
					} else {

					}
				}
			}
		}
		#endregion

		#region Backup files
		private void CreateBackup()
		{
			try
			{
				if (Properties.Settings.Default.cfgNormalBackup && cfgNormalBackup.IsChecked.Value)
				{
					if (Properties.Settings.Default.cfgBackupPath != Properties.Settings.Default.cfgConfigPath)
					{
						File.Copy(gameConfigPath, Properties.Settings.Default.cfgBackupPath + @"\Game.bak", true);
					} else {
						File.Copy(gameConfigPath, gameConfigPath.Replace("Game.ini", "Game.bak"), true);
					}
				}
			} catch (Exception eggseption) {
				System.Windows.MessageBox.Show(String.Format("An error occured whilst trying to write to the config file. Do not worry, nothing was written to the file! Error Message:\n\n{0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void CreateZIPBackup()
		{
			try
			{
				if (Properties.Settings.Default.cfgZIPBackup && cfgZIPBackup.IsChecked.Value)
				{
					if (File.Exists(Properties.Settings.Default.cfgBackupPath + @"\FrankensteinerBackups.zip"))
					{
						using (ZipArchive zip = ZipFile.Open(Properties.Settings.Default.cfgBackupPath + @"\FrankensteinerBackups.zip", ZipArchiveMode.Update))
						{
							if (cfgZIPLimitActive.IsChecked.Value && (int)nudZipLimit.Value > 0)
							{
								IReadOnlyCollection<ZipArchiveEntry> zipEntries = zip.Entries;
								if (zipEntries.Count >= nudZipLimit.Value)
								{
									// TODO: This doesn't work properly, needs fixing
									int fileDifference = (int)(zipEntries.Count - nudZipLimit.Value.Value);
									for (int i = 0; i < fileDifference; i++)
									{
										zip.Entries[0].Delete();
									}
								} else {
									zip.CreateEntryFromFile(gameConfigPath, "Game (" + DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss") + ").ini", CompressionLevel.Optimal);
								}
							} else {
								zip.CreateEntryFromFile(gameConfigPath, "Game (" + DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss") + ").ini", CompressionLevel.Optimal);
							}
						}
					} else {
						using (ZipArchive zip = ZipFile.Open(Properties.Settings.Default.cfgBackupPath + @"\FrankensteinerBackups.zip", ZipArchiveMode.Create))
						{
							zip.CreateEntryFromFile(gameConfigPath, "Game (" + DateTime.Now.ToString("yyyy-MM-dd--HH-mm-ss") + ").ini");
						}
					}
				}
			} catch (Exception eggseption) {
				System.Windows.MessageBox.Show(String.Format("An error has occured whilst trying to create/modify the ZIP backup file. Error Message:\n\n{0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		#endregion

		private List<MercenaryItem> GetModifiedMercenaries()
		{
			List<MercenaryItem> _modifiedMercs = new List<MercenaryItem>();

			if(_loadedMercenaries.Count > 0)
			{
				for(int i=0; i < _loadedMercenaries.Count; i++)
				{
					if(!_loadedMercenaries[i].isOriginal || _loadedMercenaries[i].isNewMercenary || _loadedMercenaries[i].isImportedMercenary || _loadedMercenaries[i].isBeingDeleted || _loadedMercenaries[i].isDuplicated)
					{
						_modifiedMercs.Add(_loadedMercenaries[i]);
					}
				}
				return _modifiedMercs;
			} else {
				return _modifiedMercs;
			}
		}

		private bool CheckForMercenaryConflict(MercenaryItem merc)
		{
			if(File.Exists(gameConfigPath))
			{
				string configContents = File.ReadAllText(gameConfigPath);
				if(configContents.Contains(merc.OriginalEntry))
				{
					return false;
				} else {
					return true;
				}
			}
			return true;
		}

		private bool ResolveAllMercenaryConflicts(List<MercenaryItem> _invalidMercs)
		{
			if (File.Exists(gameConfigPath))
			{
				// We need to re-read the Config to get the new mercenaries
				ConfigParser configParser = new ConfigParser(gameConfigPath);
				configParser.ParseConfig();
				// Create the Compare Window
				CompareWindow compareWindow = new CompareWindow(_invalidMercs, configParser.GetParsedMercenariesList());
				if (compareWindow.ShowDialog().Value)
				{
					return true;
				} else {
					return false;
				}
			}
			return false;
		}

		private void SaveAllMercenaries(List<MercenaryItem> _modifiedMercs)
		{
			// Prepare some lists
			List<MercenaryItem> _validMercs = new List<MercenaryItem>();
			List<MercenaryItem> _invalidMercs = new List<MercenaryItem>();
			List<MercenaryItem> _importedMercs = new List<MercenaryItem>();
			List<MercenaryItem> _failedToSave = new List<MercenaryItem>();
			List<MercenaryItem> _deleteMercs = new List<MercenaryItem>();
			bool wasMordhauKilled = false;

			#region Auto Close Mordhau Check
			if (IsMordhauRunning())
			{
				if (Properties.Settings.Default.cfgAutoClose)
				{
					KillMordhau();
					wasMordhauKilled = true;
				} else {
					DialogResult response = System.Windows.Forms.MessageBox.Show("Mordhau is running! Saving now may result in Mordhau overwriting your changes when you close it.\n\nWould you like to close Mordhau before proceeding?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
					if (response == System.Windows.Forms.DialogResult.Yes)
					{
						KillMordhau();
						wasMordhauKilled = true;
					} else if (response == System.Windows.Forms.DialogResult.Cancel)
					{
						response = System.Windows.Forms.DialogResult.Cancel;
						return;
					}
				}
			}
			#endregion

			#region Fetch To-Be-Deleted Mercenaries
			for (int i=0; i < _modifiedMercs.Count; i++)
			{
				if(_modifiedMercs[i].isBeingDeleted)
				{
					_deleteMercs.Add(_modifiedMercs[i]);
				}
			}

			if(_deleteMercs.Count > 0)
			{
				if(System.Windows.MessageBox.Show($"Warning!\n\n {_deleteMercs.Count.ToString()} mercenaries will be deleted!\n\nThis action cannot be undone! Are you sure you want to proceed?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes) // string.Join("\n", _deleteMercs.Select(x => x.Name).ToArray())
				{
					for (int i = 0; i < _deleteMercs.Count; i++)
					{
						_modifiedMercs.Remove(_deleteMercs[i]);
					}
				} else {
					return;
				}
			}
			#endregion

			#region Fetch Imported Mercenaries
			for (int i = 0; i < _modifiedMercs.Count; i++)
			{
				if(_modifiedMercs[i].isImportedMercenary || _modifiedMercs[i].isNewMercenary)
				{
					_importedMercs.Add(_modifiedMercs[i]);
				}
			}
			// If we found any imports, remove them from the main list (or else they will be seen as a conflict)
			if(_importedMercs.Count > 0)
			{
				for(int i = 0; i < _importedMercs.Count; i++)
				{
					_modifiedMercs.Remove(_importedMercs[i]);
				}
			}
			#endregion

			#region Conflict Checking & Resolving
			// Check for conflicts, separate them into their corresponding list for easier managing
			for (int i = 0; i < _modifiedMercs.Count; i++)
			{
				if(CheckForMercenaryConflict(_modifiedMercs[i]))
				{
					_invalidMercs.Add(_modifiedMercs[i]);
				} else {
					_validMercs.Add(_modifiedMercs[i]);
				}
			}
			// Try and resolve invalid mercenaries first
			if (_invalidMercs.Count > 0)
			{
				if(System.Windows.MessageBox.Show(String.Format("Conflicts were detected for these mercenaries:\n\n{0}\n\nWould you like to try and resolve them manually? Choosing \"No\" will leave them unsaved.", string.Join("\n", _modifiedMercs.Select(x => String.Format("{0} [{1}]", x.Name, x.OriginalName)).ToArray())), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
				{
					// Users wants to resolve, try to resolve
					if(ResolveAllMercenaryConflicts(_invalidMercs))
					{
						// Conflicts were successfully resolved
						for(int i = 0; i < _invalidMercs.Count; i++)
						{
							_validMercs.Add(_invalidMercs[i]);
						}
					} else {
						// Conflicts were not resolved
						for (int i = 0; i < _invalidMercs.Count; i++)
						{
							_failedToSave.Add(_invalidMercs[i]);
						}
					}
				} else {
					// User does not want to resolve
					for (int i = 0; i < _invalidMercs.Count; i++)
					{
						_failedToSave.Add(_invalidMercs[i]);
					}
				}
			}
			#endregion

			#region New Config Prepartion
			// Read the config and prepare the new contents
			string configContents = File.ReadAllText(gameConfigPath);
			for(int i = 0; i < _validMercs.Count; i++)
			{
				if(configContents.Contains(_validMercs[i].OriginalEntry))
				{
					configContents = configContents.Replace(_validMercs[i].OriginalEntry, _validMercs[i].ToString());
					_validMercs[i].SetAsOriginal();
				} else {
					_failedToSave.Add(_validMercs[i]);
				}
			}
			// And the imported mercenaries
			if(_importedMercs.Count > 0)
			{
				Regex rx = new Regex("(SingletonVersion=.+)");
				if(rx.IsMatch(configContents))
				{
					string oldSingleton = rx.Match(configContents).Value;
					string newSingleton = rx.Match(configContents).Value;
					for(int i = 0; i < _importedMercs.Count; i++)
					{
						newSingleton = String.Format("{0}\n{1}", newSingleton, _importedMercs[i].ToString());
						_validMercs.Add(_importedMercs[i]);
						_importedMercs[i].SetAsOriginal();
					}
					configContents = configContents.Replace(oldSingleton, newSingleton);
				} else {
					System.Windows.MessageBox.Show("Something went wrong when trying to save imported mercenaries.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
			// Deletes
			if(_deleteMercs.Count > 0)
			{
				for (int i = 0; i < _deleteMercs.Count; i++)
				{
					if (configContents.Contains(_deleteMercs[i].OriginalEntry))
					{
						configContents = configContents.Remove(configContents.IndexOf(_deleteMercs[i].OriginalEntry), _deleteMercs[i].OriginalEntry.Length+1);
						_loadedMercenaries.Remove(_deleteMercs[i]);
					} else {
						if(_deleteMercs[i].isNewMercenary)
						{
							_loadedMercenaries.Remove(_deleteMercs[i]);
						} else {
							System.Windows.MessageBox.Show($"Unable to delete mercenary {_deleteMercs[i].Name}. Original entry was not found in the config!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
						}
					}
				}
			}
			#endregion

			#region Create Backup(s) Before Writing
			CreateBackup();
			CreateZIPBackup();
			#endregion

			#region Write to Config
			if(_validMercs.Count > 0 || _deleteMercs.Count > 0)
			{
				File.WriteAllText(gameConfigPath, configContents);
				if(_validMercs.Count > 0 && _deleteMercs.Count == 0)
				{
					System.Windows.MessageBox.Show($"Successfully saved {_validMercs.Count.ToString()} mercenaries!", "Information", MessageBoxButton.OK, MessageBoxImage.Information); // string.Join("\n", _validMercs.Select(x => x.Name).ToArray())
				} else if(_validMercs.Count == 0 && _deleteMercs.Count > 0) {
					System.Windows.MessageBox.Show($"Successfully deleted {_deleteMercs.Count.ToString()} mercenaries!", "Information", MessageBoxButton.OK, MessageBoxImage.Information); // string.Join("\n", _deleteMercs.Select(x => x.Name).ToArray())
				}
				bTitleSave.Visibility = Visibility.Collapsed;
			}
			#endregion

			#region Alert User of Unsaved Mercenaries
			if (_failedToSave.Count > 0)
			{
				System.Windows.MessageBox.Show(String.Format("{0} mercenaries were unable to save for an unknown reason.\n\n{1}", _failedToSave.Count, string.Join("\n", _failedToSave.Select(x => x.Name).ToArray())), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
			#endregion

			#region Auto Restart Mordhau
			if (Properties.Settings.Default.cfgRestartMordhau && cfgRestartMordhau.IsChecked.Value)
			{
				if (Properties.Settings.Default.cfgRestartMordhauMode && cfgRestartMordhauMode.IsChecked.Value)
				{
					if (wasMordhauKilled)
					{
						string mordhauExe = tbMordhauPath.Text + @"\Mordhau\Binaries\Win64\Mordhau-Win64-Shipping.exe";
						if (File.Exists(mordhauExe))
						{
							Process.Start(mordhauExe);
						}
						else
						{
							if (string.IsNullOrWhiteSpace(tbMordhauPath.Text))
							{
								System.Windows.MessageBox.Show("Did not restart Mordhau because the path is not set.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
							} else {
								System.Windows.MessageBox.Show($"Unable to restart Mordhau. Path searched:\n\n{mordhauExe}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
							}
						}
					}
				}
				else
				{
					string mordhauExe = tbMordhauPath.Text + @"\Mordhau\Binaries\Win64\Mordhau-Win64-Shipping.exe";
					if (File.Exists(mordhauExe))
					{
						Process.Start(mordhauExe);
					}
					else
					{
						if (string.IsNullOrWhiteSpace(tbMordhauPath.Text))
						{
							System.Windows.MessageBox.Show("Did not restart Mordhau because the path is not set.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
						} else {
							System.Windows.MessageBox.Show($"Unable to restart Mordhau. Path searched:\n\n{mordhauExe}", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
						}
					}
				}
			}
			#endregion
		}

		public void AddImportedMercenary(MercenaryItem importedMerc)
		{
			if(importedMerc != null)
			{
				if(importedMerc.isHordeMercenary)
				{
					for(int i=0; i < _loadedMercenaries.Count; i++)
					{
						if(_loadedMercenaries[i].isHordeMercenary)
						{
							_loadedMercenaries[i].FaceValues = importedMerc.FaceValues;
							_loadedMercenaries[i].ItemText = "Horde Mercenary - Unsaved Changes!";
							_loadedMercenaries[i].isOriginal = false;
						}
					}
				} else {
					_loadedMercenaries.Insert(0, importedMerc);
					importedMerc.index = _loadedMercenaries.Count + 1;
				}
				importedMerc.UpdateItemText();
			}
			CheckForModifiedMercenaries();
		}

		private void ToggleStartupMovies(bool toggle)
		{
			if(!String.IsNullOrWhiteSpace(tbMordhauPath.Text))
			{
				string moviesPath = tbMordhauPath.Text + @"\Mordhau\Content\Movies";
				string logoMoviePath = moviesPath + @"\LogoSplash.webm";
				string ue4MoviePath = moviesPath + @"\UE4_Logo.webm";

				if(toggle)
				{
					// Rename .webm into .bak
					if (File.Exists(logoMoviePath) && File.Exists(ue4MoviePath))
					{
						File.Move(logoMoviePath, logoMoviePath.Replace(".webm", ".bak"));
						File.Move(ue4MoviePath, ue4MoviePath.Replace(".webm", ".bak"));
						Properties.Settings.Default.cfgDisableMovies = true;
						Properties.Settings.Default.Save();
					} else {
						if (File.Exists(logoMoviePath.Replace(".webm", ".bak")) && File.Exists(ue4MoviePath.Replace(".webm", ".bak")))
						{
							Properties.Settings.Default.cfgDisableMovies = true;
							Properties.Settings.Default.Save();
						} else {
							System.Windows.MessageBox.Show("Unable to locate startup movies.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
						}
					}
				} else {
					// Rename .bak into .webm
					if (File.Exists(logoMoviePath.Replace(".webm", ".bak")) && File.Exists(ue4MoviePath.Replace(".webm", ".bak")))
					{
						File.Move(logoMoviePath.Replace(".webm", ".bak"), logoMoviePath);
						File.Move(ue4MoviePath.Replace(".webm", ".bak"), ue4MoviePath);
						Properties.Settings.Default.cfgDisableMovies = false;
						Properties.Settings.Default.Save();
					} else {
						if (File.Exists(logoMoviePath) && File.Exists(ue4MoviePath))
						{
							Properties.Settings.Default.cfgDisableMovies = false;
							Properties.Settings.Default.Save();
						} else {
							System.Windows.MessageBox.Show("Unable to locate startup movies.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
						}
					}
				}
			} else {
				System.Windows.MessageBox.Show("Failed to disable Mordhau startup movies - path to Mordhau has not been set.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		#region Mordhau Process Checking
		private bool IsMordhauRunning()
		{
			try
			{
				Process[] processes = Process.GetProcessesByName("Mordhau-Win64-Shipping");
				if(processes.Length > 0)
				{
					return true;
				}
				return false;
			} catch(Exception eggseption) {
				System.Windows.MessageBox.Show(String.Format("An error has occured whilst trying to fetch the Mordhau process.\n\nError Message: {0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return false;
			}
		}

		private void KillMordhau()
		{
			try
			{
				Process[] processes = Process.GetProcessesByName("Mordhau-Win64-Shipping");
				foreach (Process process in processes)
				{
					process.Kill();
					process.WaitForExit();
				}
			} catch (Exception eggseption) {
				System.Windows.MessageBox.Show(String.Format("An error has occured whilst trying to fetch and/or kill the Mordhau process.\n\nError Message: {0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		#endregion

		private void CheckForModifiedMercenaries()
		{
			if(_loadedMercenaries.Count > 0)
			{
				for (int i = 0; i < _loadedMercenaries.Count; i++)
				{
					if (!_loadedMercenaries[i].isOriginal || _loadedMercenaries[i].isImportedMercenary || _loadedMercenaries[i].isNewMercenary || _loadedMercenaries[i].isBeingDeleted || _loadedMercenaries[i].isDuplicated)
					{
						bTitleSave.Visibility = Visibility.Visible;
						//changeDetected = true;
						break;
					} else {
						bTitleSave.Visibility = Visibility.Collapsed;
						//changeDetected = false;
					}
				}
			}
		}

		private void CreateMercenary(string name, int faceType, bool isMassCreation)
		{
			if (!String.IsNullOrWhiteSpace(name))
			{
				if (IsMercenaryNameTaken(name) && !isMassCreation)
				{
					System.Windows.MessageBox.Show($"A mercenary with the name \"{name}\" already exists!\nPlease choose a different name.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}

				// 0 = Default, 1 = Random, 2 = Frankenstein
				string defaultCode = $"CharacterProfiles=(Name=INVTEXT(\"{name}\"),GearCustomization=(Wearables=((),(),(ID=30),(),(),(),(),(ID=15),(ID=8)),Equipment=((),(),())),AppearanceCustomization=(Emblem=0,EmblemColors=(0,0),MetalRoughnessScale=0,MetalTint=0,Age=0,Voice=2,VoicePitch=157,bIsFemale=False,Fat=85,Skinny=85,Strong=85,SkinColor=0,Face=0,EyeColor=0,HairColor=0,Hair=0,FacialHair=0,Eyebrows=0),FaceCustomization=(Translate=(15360,15360,15840,12656,15364,12653,15862,0,15385,0,16320,15847,15855,15855,384,8690,8683,480,480,480,31700,480,480,480,15360,15840,18144,15840,31690,15850,15860,11471,11471,12463,12463,11471,11471,15840,15840,0,0,0,0,0,0,0,0,7665,7660),Rotate=(0,0,0,0,0,0,16,0,0,0,0,14,0,0,12288,591,367,0,15855,15855,18976,0,0,0,0,18432,0,0,18816,0,0,0,0,0,0,0,0,655,335,0,0,15840,15840,0,0,15840,15840,0,0),Scale=(14351,14351,0,15360,0,15360,0,15855,0,15855,14336,0,0,0,14350,0,0,15855,15855,15855,15840,0,15855,15855,0,15914,6,0,15840,0,0,0,0,0,0,0,0,15855,15855,0,0,0,0,0,0,0,0,0,0)),SkillsCustomization=(Perks=0),Category=\"\")";
				MercenaryItem newMercenary = new MercenaryItem(defaultCode);
				if (newMercenary.ValidateMercenaryCode())
				{
					if (faceType == 1)
					{
						newMercenary.Randomize();
					}
					else if (faceType == 2)
					{
						newMercenary.Frankenstein();
					}
					newMercenary.index = _loadedMercenaries.Count + 1;
					newMercenary.isNewMercenary = true;
					newMercenary.UpdateItemText();
					_loadedMercenaries.Insert(0, newMercenary);
					tbMercenaryName.Text = "";
					if (!isMassCreation)
					{
						System.Windows.MessageBox.Show($"Successfully created {newMercenary.Name}", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
					}
					CheckForModifiedMercenaries();
				}
				else
				{
					System.Windows.MessageBox.Show("Failed to parse face values. Could not create new mercenary!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}

		public bool IsMercenaryNameTaken(string name)
		{
			bool match = false;
			for (int i = 0; i < _loadedMercenaries.Count; i++)
			{
				if (_loadedMercenaries[i].Name == name)
				{
					match = true;
					break;
				}
			}
			return match;
		}

		#region UpdateContextItem Methods
		private void UpdateContextItem(System.Windows.Controls.MenuItem item, string header)
		{
			if(item != null)
			{
				item.Header = header;
			}
		}
		private void UpdateContextItem(System.Windows.Controls.MenuItem item, string header, bool enabled)
		{
			if(item != null)
			{
				item.Header = header;
				item.IsEnabled = enabled;
			}
		}
		private void UpdateContextItem(System.Windows.Controls.MenuItem item, string header, bool enabled, Visibility visibility)
		{
			if (item != null)
			{
				item.Header = header;
				item.IsEnabled = enabled;
				item.Visibility = visibility;
			}
		}
		#endregion

		#region File & Folder Browsing (Config, Mordhau and Backup)
		private void BBrowseConfigPath_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				OpenFileDialog fileDialog = new OpenFileDialog();
				fileDialog.Filter = "Configuration Settings|*.ini";
				if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					string fileContents = File.ReadAllText(fileDialog.FileName);
					if (fileContents.Contains(@"[/Game/Mordhau/Blueprints/BP_MordhauSingleton.BP_MordhauSingleton_C]"))
					{
						gameConfigPath = fileDialog.FileName;
						tbConfigPath.Text = gameConfigPath;
						tbBackupPath.Text = gameConfigPath.Replace(@"\Game.ini", "");
						Properties.Settings.Default.cfgConfigPath = gameConfigPath;
						Properties.Settings.Default.cfgBackupPath = gameConfigPath.Replace(@"\Game.ini", "");
						Properties.Settings.Default.Save();
						System.Windows.MessageBox.Show("Successfully validated configuration file!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
						RefreshMercenaries(); // We'll also refresh mercenaries here
					} else {
						System.Windows.MessageBox.Show("Failed to verify the configuration file! Are you sure this is the correct file? Please, try again.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
					}
				}
			} catch (Exception eggseption) {
				System.Windows.MessageBox.Show(String.Format("An error occured whilst trying to set the configuration file. Error Message: {0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void BBrowseMordhauPath_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				FolderBrowserDialog folderDialog = new FolderBrowserDialog();
				folderDialog.ShowNewFolderButton = false;
				if(folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					if(File.Exists(folderDialog.SelectedPath+@"\Mordhau.exe") && Directory.Exists(folderDialog.SelectedPath+@"\Mordhau\Content\Movies"))
					{
						tbMordhauPath.Text = folderDialog.SelectedPath;
						Properties.Settings.Default.cfgMordhauPath = folderDialog.SelectedPath;
						Properties.Settings.Default.Save();
						System.Windows.MessageBox.Show("Mordhau folder was successfully set!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
					} else {
						System.Windows.MessageBox.Show("Folder does not seem to be valid! You should select the root folder of Mordhau. For example:\n\"C:/Program Files (x86)/Steam/steamapps/common/Mordhau\".\n\nPlease, try again!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
					}
				}
			} catch (Exception eggseption) {
				System.Windows.MessageBox.Show(String.Format("An error occured whilst trying to set the Mordhau folder. Error Message: {0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void BBrowseBackupPath_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				FolderBrowserDialog folderDialog = new FolderBrowserDialog();
				if(folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					string folderPath = folderDialog.SelectedPath;
					try
					{
						System.Security.AccessControl.DirectorySecurity canWrite = Directory.GetAccessControl(folderPath);
					} catch(UnauthorizedAccessException) {
						System.Windows.MessageBox.Show("Unable to use the selected folder, insuffiecent permissions - does not have write permissions.", "Error", MessageBoxButton.YesNo, MessageBoxImage.Error);
					}
					tbBackupPath.Text = folderPath;
					Properties.Settings.Default.cfgBackupPath = folderPath;
					Properties.Settings.Default.Save();
				}
			} catch (Exception eggseption) {
				System.Windows.MessageBox.Show(String.Format("An error occured whilst trying to set the Backup folder. Error Message: {0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		#endregion

		#region TextBox Events
		private void TbBackupPath_TextChanged(object sender, TextChangedEventArgs e)
		{
			if(!String.IsNullOrWhiteSpace(tbBackupPath.Text))
			{
				if(Directory.Exists(tbBackupPath.Text))
				{
					cfgNormalBackup.IsEnabled = true;
					cfgZIPBackup.IsEnabled = true;
				}
			}
		}
		private void TbConfigPath_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!String.IsNullOrWhiteSpace(tbConfigPath.Text))
			{
				bRefreshCharacters.IsEnabled = true;
			}
		}
		private void TbMordhauPath_TextChanged(object sender, TextChangedEventArgs e)
		{
			if(!String.IsNullOrWhiteSpace(tbMordhauPath.Text))
			{
				cbAutoCloseGame.IsEnabled = true;
				cbDisableMovies.IsEnabled = true;
			}
		}
		private void TbMercenaryName_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!String.IsNullOrWhiteSpace(tbMercenaryName.Text))
			{
				bCreateMercenary.IsEnabled = true;
			}
			else
			{
				bCreateMercenary.IsEnabled = false;
			}
		}
		#endregion

		#region Right-Click Events for Browse Buttons (Opens Folder)
		private void BBrowseConfigPath_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			if(!String.IsNullOrWhiteSpace(tbConfigPath.Text))
			{
				try
				{
					Process.Start(System.IO.Path.GetDirectoryName(tbConfigPath.Text));
				} catch (Exception eggseption) {
					System.Windows.MessageBox.Show(String.Format("An error has occured whilst trying to open the selected folder. Error Message:\n\n{0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void BBrowseMordhauPath_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			if(!String.IsNullOrWhiteSpace(tbMordhauPath.Text))
			{
				try
				{
					Process.Start(System.IO.Path.GetFullPath(tbMordhauPath.Text));
				} catch (Exception eggseption) {
					System.Windows.MessageBox.Show(String.Format("An error has occured whilst trying to open the selected folder. Error Message:\n\n{0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void BBrowseBackupPath_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
		{
			if(!String.IsNullOrWhiteSpace(tbBackupPath.Text))
			{
				try
				{
					Process.Start(System.IO.Path.GetFullPath(tbBackupPath.Text));
				} catch (Exception eggseption) {
					System.Windows.MessageBox.Show(String.Format("An error has occured whilst trying to open the selected folder. Error Message:\n\n{0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
		#endregion

		#region CheckBox Events
		private void CbDisableMovies_Click(object sender, RoutedEventArgs e)
		{
			ToggleStartupMovies(cbDisableMovies.IsChecked.Value);
		}
		private void CbAutoCloseGame_Click(object sender, RoutedEventArgs e)
		{
			Properties.Settings.Default.cfgAutoClose = cbAutoCloseGame.IsChecked.Value;
			cfgRestartMordhau.IsEnabled = cbAutoCloseGame.IsChecked.Value;
			cfgRestartMordhauMode.IsEnabled = cbAutoCloseGame.IsChecked.Value;
		}
		private void CbCheckBox_Click(object sender, RoutedEventArgs e)
		{
			System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
			Properties.Settings.Default[checkBox.Name] = checkBox.IsChecked.Value;
		}
		#endregion

		#region ListBox Context Menu Logic
		// Context Menu Opened
		private void lbContextMenu_Opened(object sender, RoutedEventArgs e)
		{
			if (lbCharacterList.SelectedIndex != -1)
			{
				if(lbCharacterList.SelectedItems.Count == 1)
				{
					MercenaryItem selectedMerc = lbCharacterList.SelectedItem as MercenaryItem;
					if (selectedMerc != null)
					{
						Regex category = new Regex("\"(.*?)\"");
						if (category.IsMatch(selectedMerc.CategoryString))
						{
							lbChangeCategory.Text = category.Match(selectedMerc.CategoryString).Value.Replace("\"", "");
						}
						bool changedOrImported = (!selectedMerc.isOriginal || selectedMerc.isOriginal && selectedMerc.isImportedMercenary) ? true : false;
						// Edit
						UpdateContextItem(lbContextEdit, $"Edit", true);
						UpdateContextItem(lbContextQuickEdit, $"Open in Mercenary Editor", true);
						UpdateContextItem(lbContextQuickFrankenstein, $"Frankenstein", true);
						UpdateContextItem(lbContextQuickRandomize, $"Randomize", true);
						// Save
						UpdateContextItem(lbContextSave, $"Save", changedOrImported);
						// Revert
						UpdateContextItem(lbContextRevert, $"Revert", changedOrImported);
						// Export
						UpdateContextItem(lbContextExport, $"Export to Clipboard", true);
						// Copy Face
						UpdateContextItem(lbContextCopyFace, $"Copy Face Values", true);
						UpdateContextItem(lbContextCopyFormat, "Copy", true);
						UpdateContextItem(lbContextCopyHordeFormat, "Copy as Horde Format", true);
						// Paste Face
						if (selectedMerc != _copiedMercenary && _copiedMercenary != null)
						{
							UpdateContextItem(lbContextPasteFace, $"Paste Face Values From {_copiedMercenary.Name} to {selectedMerc.Name}", true);
						} else {
							UpdateContextItem(lbContextPasteFace, "No Copied Face Values to Paste", false);
						}
						// Delete
						UpdateContextItem(lbContextDelete, $"Mark for Deletion", true);

						//TODO: Replace with Revert - alert user before
						//UpdateContextItem(lbContextDelete, String.Format("Delete {0}", selectedMerc.Name), selectedMerc.importedMercenary, Visibility.Visible);
					}
				} else {
					bool changesDetected = false;
					// Get a List of the selected mercenaries
					List<MercenaryItem> selectedMercs = new List<MercenaryItem>();
					foreach(var merc in lbCharacterList.SelectedItems)
					{
						selectedMercs.Add((MercenaryItem)merc);
					}
					// Check for Changes
					for(int i=0; i < selectedMercs.Count; i++)
					{
						if(!selectedMercs[i].isOriginal)
						{
							changesDetected = true;
							break;
						}
					}
					// Check if all CategoryString's are the same
					string compareString = selectedMercs[0].CategoryString;
					bool isEqual = selectedMercs.Skip(1).All(s => string.Equals(compareString, s.CategoryString, StringComparison.InvariantCultureIgnoreCase));
					if (isEqual)
					{
						Regex category = new Regex("\"(.*?)\"");
						if (category.IsMatch(compareString))
						{
							lbChangeCategory.Text = category.Match(compareString).Value.Replace("\"", "");
						}
					}
					// Edit
					UpdateContextItem(lbContextEdit, "Edit (Multiple Selected)", true);
					UpdateContextItem(lbContextQuickEdit, "Open in Mercenary Editor", false);
					UpdateContextItem(lbContextQuickFrankenstein, "Frankenstein (Multiple Selected)", true);
					UpdateContextItem(lbContextQuickRandomize, "Randomize (Multiple Selected)", true);
					// Save
					UpdateContextItem(lbContextSave, "Save (Multiple Selected)", changesDetected);
					// Revert
					UpdateContextItem(lbContextRevert, "Revert (Multiple Selected)", changesDetected);
					// Export
					UpdateContextItem(lbContextExport, "Export Selected Mercenaries to Clipboard", true);
					// Copy Face
					UpdateContextItem(lbContextCopyFace, "Copy Face Values from (Multiple Selected)", false);
					// Paste Face
					UpdateContextItem(lbContextPasteFace, "Paste Face Values to (Multiple Selected)", _copiedMercenary != null);
					// Delete
					UpdateContextItem(lbContextDelete, "Mark for Deletion (Multiple Selected)", true);
				}
			}
		}
		private void lbContextMenu_Closed(object sender, RoutedEventArgs e)
		{
			CheckForModifiedMercenaries();
			lbChangeCategory.Text = "";
		}
		// Context Option: Save
		private void LbContextSave_Click(object sender, RoutedEventArgs e)
		{
			List<MercenaryItem> _modifiedMercs = new List<MercenaryItem>();
			for (int i = 0; i < lbCharacterList.SelectedItems.Count; i++)
			{
				MercenaryItem _selectedMerc = lbCharacterList.SelectedItems[i] as MercenaryItem;
				if (_selectedMerc != null)
				{
					_modifiedMercs.Add(_selectedMerc);
				}
			}
			SaveAllMercenaries(_modifiedMercs);
		}
		// Context Option: Revert
		private void LbContextRevert_Click(object sender, RoutedEventArgs e)
		{
			for(int i=0; i < lbCharacterList.SelectedItems.Count; i++)
			{
				MercenaryItem merc = lbCharacterList.SelectedItems[i] as MercenaryItem;
				if (merc != null)
				{
					if (merc.isImportedMercenary && merc.isOriginal)
					{
						if (System.Windows.MessageBox.Show($"{merc.Name} is an imported mercenary without any changes. Reverting now will delete this mercenary.\n\nAre you sure?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
						{
							_loadedMercenaries.Remove(merc);
						}
					} else if (merc.isImportedMercenary && !merc.isOriginal) {
						merc.RevertCurrentChanges();
					} else if (!merc.isImportedMercenary && !merc.isOriginal) {
						merc.RevertCurrentChanges();
					}
				}
			}
			CheckForModifiedMercenaries();
		}
		//Context Option: Export
		private void LbContextExport_Click(object sender, RoutedEventArgs e)
		{
			StringBuilder sb = new StringBuilder();
			for(int i=0; i < lbCharacterList.SelectedItems.Count; i++)
			{
				MercenaryItem merc = lbCharacterList.SelectedItems[i] as MercenaryItem;
				if (merc != null)
				{
					sb.Append(String.Format("{0}{1}", merc.ToString(), (lbCharacterList.SelectedItems.Count > 1 && i != lbCharacterList.SelectedItems.Count-1) ? "\n" : ""));
				}
			}
			System.Windows.Clipboard.SetText(sb.ToString());
		}
		// Context Option: Quick Edit
		private void LbContextQuickEdit_Click(object sender, RoutedEventArgs e)
		{
			MercenaryItem selectedMerc = lbCharacterList.SelectedItem as MercenaryItem;
			if (selectedMerc != null)
			{
				MercenaryEditor mercEditor = new MercenaryEditor(selectedMerc);
				mercEditor.Title = $"Mercenary Editor - Editing {selectedMerc.Name}";
				if (mercEditor.ShowDialog().Value)
				{
					CheckForModifiedMercenaries();
				}
			}
		}
		// Context Option: Frankenstein
		private void LbContextQuickFrankenstein_Click(object sender, RoutedEventArgs e)
		{
			for(int i=0; i < lbCharacterList.SelectedItems.Count; i++)
			{
				MercenaryItem merc = lbCharacterList.SelectedItems[i] as MercenaryItem;
				if(merc != null)
				{
					merc.Frankenstein();
				}
			}
			CheckForModifiedMercenaries();
		}
		// Context Option: Randomize
		private void LbContextQuickRandomize_Click(object sender, RoutedEventArgs e)
		{
			for (int i = 0; i < lbCharacterList.SelectedItems.Count; i++)
			{
				MercenaryItem merc = lbCharacterList.SelectedItems[i] as MercenaryItem;
				if (merc != null)
				{
					merc.Randomize();
				}
			}
			CheckForModifiedMercenaries();
		}
		// Context Option: Copy Face
		private void LbContextCopyFace_Click(object sender, RoutedEventArgs e)
		{
			MercenaryItem selectedMerc = lbCharacterList.SelectedItem as MercenaryItem;
			if (selectedMerc != null)
			{
				_copiedMercenary = selectedMerc;
			}
		}
		// Context Option: Paste Face
		private void LbContextPasteFace_Click(object sender, RoutedEventArgs e)
		{
			for(int i=0; i < lbCharacterList.SelectedItems.Count; i++)
			{
				MercenaryItem selectedMerc = lbCharacterList.SelectedItems[i] as MercenaryItem;
				if(selectedMerc != null && _copiedMercenary != null)
				{
					selectedMerc.FaceValues = _copiedMercenary.FaceValues;
					selectedMerc.isOriginal = false;
					selectedMerc.UpdateItemText();
				}
			}
			CheckForModifiedMercenaries();
		}
		// Context Option: Delete
		private void LbContextDelete_Click(object sender, RoutedEventArgs e)
		{
			for(int i=0; i < lbCharacterList.SelectedItems.Count; i++)
			{
				MercenaryItem selectedMerc = lbCharacterList.SelectedItems[i] as MercenaryItem;
				if(selectedMerc != null)
				{
					selectedMerc.isBeingDeleted = true;
					selectedMerc.UpdateItemText();
				}
			}
			CheckForModifiedMercenaries();
		}
		// Context Option: Copy as Horde Format
		private void LbContextCopyHordeFormat_Click(object sender, RoutedEventArgs e)
		{
			MercenaryItem selectedMerc = lbCharacterList.SelectedItem as MercenaryItem;
			if (selectedMerc != null)
			{
				System.Windows.Clipboard.SetText(selectedMerc.GetHordeFormat());
			}
			CheckForModifiedMercenaries();
		}
		// Context Option: Copy Face Values
		private void LbContextCopyFormat_Click(object sender, RoutedEventArgs e)
		{
			MercenaryItem selectedMerc = lbCharacterList.SelectedItem as MercenaryItem;
			if (selectedMerc != null)
			{
				System.Windows.Clipboard.SetText(selectedMerc.FaceString);
			}
			CheckForModifiedMercenaries();
		}
		// Context Option: Change Category
		private void lbChangeCategory_TextChanged(object sender, TextChangedEventArgs e)
		{
			System.Windows.Controls.TextBox text = sender as System.Windows.Controls.TextBox;
			if (lbChangeCategory.IsFocused)
			{
				for (int i = 0; i < lbCharacterList.SelectedItems.Count; i++)
				{
					MercenaryItem selectedMerc = lbCharacterList.SelectedItems[i] as MercenaryItem;
					if (selectedMerc != null && !selectedMerc.isHordeMercenary)
					{
						selectedMerc.CategoryString = "Category=" + "\"" + text.Text.Replace("\"", "") + "\"";
						selectedMerc.isOriginal = false;
						selectedMerc.UpdateItemText();
					}
				}
			}
		}
		#endregion

		#region Button Events
		private void NudZipLimit_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
		{
			Properties.Settings.Default.cfgZIPLimit = e.NewValue.Value;
			Properties.Settings.Default.Save();
		}
		private void BGithub_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("https://github.com/Dealman/Frankensteiner");
		}
		private void BTitleSave_Click(object sender, RoutedEventArgs e)
		{
			List<MercenaryItem> _modifiedMercs = GetModifiedMercenaries();

			if(_modifiedMercs.Count > 0)
			{
				SaveAllMercenaries(_modifiedMercs);
			} else {
				// Shouldn't be possible to reach this, but leave a message just in case.
				System.Windows.MessageBox.Show("Something went wrong! No modified mercenaries were found - unable to save.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
		private void BShowTools_Click(object sender, RoutedEventArgs e)
		{
			toolsFlyout.IsOpen = true;
		}
		private void BCreateMercenary_Click(object sender, RoutedEventArgs e)
		{
			int faceType = 0;
			if (rbMassRandom.IsChecked.Value)
			{
				faceType = 1;
			}
			else if (rbMassFrankenstein.IsChecked.Value)
			{
				faceType = 2;
			}
			CreateMercenary(tbMercenaryName.Text, faceType, false);
		}
		private void BMassCreate_Click(object sender, RoutedEventArgs e)
		{
			int faceType = 0;
			if (rbMassRandom.IsChecked.Value)
			{
				faceType = 1;
			}
			else if (rbMassFrankenstein.IsChecked.Value)
			{
				faceType = 2;
			}

			for (int i = 0; i < (int)nudMassNumber.Value; i++)
			{
				int i2 = i + 1;

				while (IsMercenaryNameTaken($"MassCreation {i2.ToString()}"))
				{
					// If MassCreation [i] already exists, just keep adding 1 until it doesnt
					i2 += 1;
				}

				CreateMercenary($"MassCreation {i2.ToString()}", faceType, true);
			}
			System.Windows.MessageBox.Show($"Successfully mass created {nudMassNumber.Value.ToString()} mercenaries!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
		}
		private void BImportMercs_Click(object sender, RoutedEventArgs e)
		{
			ImportWindow importer = new ImportWindow();
			importer.Show();
		}
		#endregion

		#region Keybind Handler
		private void LbCharacterList_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{
			// Enter - Open Mercenary Editor
			if (e.Key == Key.Enter && lbContextMenu.IsVisible == false)
			{
				if (lbCharacterList.SelectedItems.Count == 1)
				{
					MercenaryItem _selectedMerc = lbCharacterList.SelectedItem as MercenaryItem;
					if (_selectedMerc != null)
					{
						MercenaryEditor mercEditor = new MercenaryEditor(_selectedMerc);
						mercEditor.Title = $"Mercenary Editor - Editing {_selectedMerc.Name}";
						if (mercEditor.ShowDialog().Value)
						{
							CheckForModifiedMercenaries();
						}
					}
				}
			}

			if (Properties.Settings.Default.cfgShortcutsEnabled)
			{
				// Save Keybind
				if (e.Key == Key.S && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
				{
					if (lbCharacterList.SelectedItems.Count == 0 && GetModifiedMercenaries().Count > 0)
					{
						SaveAllMercenaries(GetModifiedMercenaries());
					} else {
						List<MercenaryItem> _modifiedMercs = new List<MercenaryItem>();
						for (int i = 0; i < lbCharacterList.SelectedItems.Count; i++)
						{
							MercenaryItem _selectedMerc = lbCharacterList.SelectedItems[i] as MercenaryItem;
							if (_selectedMerc != null)
							{
								_modifiedMercs.Add(_selectedMerc);
							}
						}
						SaveAllMercenaries(_modifiedMercs);
					}
				}
				// Revert Keybind
				if (e.Key == Key.Z && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
				{
					for(int i = 0; i < lbCharacterList.SelectedItems.Count; i++)
					{
						MercenaryItem _selectedMerc = lbCharacterList.SelectedItems[i] as MercenaryItem;
						if (_selectedMerc != null)
						{
							// Normal or Horde Mercenary With Changes - Revert
							if (!_selectedMerc.isOriginal && !_selectedMerc.isImportedMercenary || !_selectedMerc.isOriginal && _selectedMerc.isHordeMercenary)
							{
								_selectedMerc.RevertCurrentChanges();
							}
							// Imported & Changed Mercenary - Revert
							if (_selectedMerc.isImportedMercenary && !_selectedMerc.isOriginal)
							{
								_selectedMerc.RevertCurrentChanges();
							}
							// Imported & Unchanged Mercenary - Delete
							if (_selectedMerc.isImportedMercenary && _selectedMerc.isOriginal)
							{
								if (System.Windows.MessageBox.Show($"{_selectedMerc.Name} is an imported mercenary without any changes. Reverting now will delete this mercenary.\n\nAre you sure?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
								{
									_loadedMercenaries.Remove(_selectedMerc);
								}
							}
						}
					}
				}
				// Frankenstein Keybind
				if (e.Key == Key.F && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
				{
					if (lbCharacterList.SelectedItems.Count > 0)
					{
						for (int i = 0; i < lbCharacterList.SelectedItems.Count; i++)
						{
							MercenaryItem _selectedMerc = lbCharacterList.SelectedItems[i] as MercenaryItem;
							if (_selectedMerc != null)
							{
								_selectedMerc.Frankenstein();
							}
						}
					}
				}
				// Randomize Keybind
				if (e.Key == Key.R && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
				{
					if (lbCharacterList.SelectedItems.Count > 0)
					{
						for (int i = 0; i < lbCharacterList.SelectedItems.Count; i++)
						{
							MercenaryItem _selectedMerc = lbCharacterList.SelectedItems[i] as MercenaryItem;
							if (_selectedMerc != null)
							{
								_selectedMerc.Randomize();
							}
						}
					}
				}
				// Export Mercenary
				if (e.Key == Key.E && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
				{
					StringBuilder sb = new StringBuilder();
					for (int i = 0; i < lbCharacterList.SelectedItems.Count; i++)
					{
						MercenaryItem merc = lbCharacterList.SelectedItems[i] as MercenaryItem;
						if (merc != null)
						{
							sb.Append(String.Format("{0}{1}", merc.ToString(), (lbCharacterList.SelectedItems.Count > 1 && i != lbCharacterList.SelectedItems.Count - 1) ? "\n" : ""));
						}
					}
					System.Windows.Clipboard.SetText(sb.ToString());
				}
				// Copy Face Values
				if(e.Key == Key.C && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
				{
					MercenaryItem selectedMerc = lbCharacterList.SelectedItem as MercenaryItem;
					if (selectedMerc != null)
					{
						_copiedMercenary = selectedMerc;
					}
				}
				// Paste Face Values
				if (e.Key == Key.V && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
				{
					for (int i = 0; i < lbCharacterList.SelectedItems.Count; i++)
					{
						MercenaryItem selectedMerc = lbCharacterList.SelectedItems[i] as MercenaryItem;
						if (selectedMerc != null && _copiedMercenary != null)
						{
							selectedMerc.FaceValues = _copiedMercenary.FaceValues;
							selectedMerc.isOriginal = false;
							selectedMerc.UpdateItemText();
						}
					}
					CheckForModifiedMercenaries();
				}
				// Duplicate Mercenary
				/*if (e.Key == Key.D && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
				{
					for (int i = 0; i < lbCharacterList.SelectedItems.Count; i++)
					{
						MercenaryItem merc = lbCharacterList.SelectedItems[i] as MercenaryItem;
						if (merc != null && !merc.isHordeMercenary)
						{
							string code = merc.ToString().Replace(merc.Name, merc.Name + " (2)");
							MercenaryItem newMercenary = new MercenaryItem(code);
							if (newMercenary.ValidateMercenaryCode() && newMercenary != merc)
							{
								newMercenary.index = _loadedMercenaries.Count + 1;
								newMercenary.isNewMercenary = true;
								newMercenary.isDuplicated = true;
								newMercenary.UpdateItemText();
								_loadedMercenaries.Insert(0, newMercenary);
								tbMercenaryName.Text = "";
								CheckForModifiedMercenaries();
							}
						}
					}
				}*/
				CheckForModifiedMercenaries();
			} else {
				if ((e.Key == Key.S || e.Key == Key.Z || e.Key == Key.F || e.Key == Key.R || e.Key == Key.E || e.Key == Key.C || e.Key == Key.V) && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
				{
					System.Windows.Forms.MessageBox.Show("Keyboard shortcuts are not enabled. Turn them on in Settings > Other", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
		}
		#endregion
	}
}
