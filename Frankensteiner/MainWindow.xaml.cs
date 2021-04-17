using MahApps.Metro;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

// Relevant File? C:\Program Files (x86)\Steam\steamapps\common\Mordhau\Mordhau\Content\Mordhau\Blueprints\Characters\BP_ControllableDummy.uasset
// TODO: Make mercenaries organizable by drag and drop or moving up and down - Make sure to move face values in Game.ini too!
// TODO: Make a "favorite mercenary" with a star that you can click so that mercenary will appear on top

namespace Frankensteiner
{
    public partial class MainWindow : MetroWindow
    {
        string[] appThemes = { "Dark", "Light" };
        string[] appAccents = { "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald", "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta", "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna" };
        private string gameConfigPath;
        private ConfigParser config;
        private ObservableCollection<MercenaryItem> _loadedMercenaries = new ObservableCollection<MercenaryItem>();
        private MercenaryItem _copiedMercenary;
        private bool changeDetected = false;

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
                            System.Windows.MessageBox.Show("Successfully located the configuration file! You may now create.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
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
            cbZipBackup.IsChecked = Properties.Settings.Default.cfgZIPBackup;
            // ZIP Limiter
            nudZipLimit.Value = Properties.Settings.Default.cfgZIPLimit;
            // ZIP Limit Active
            cbLimitZipSize.IsChecked = Properties.Settings.Default.cfgZIPLimitActive;
            // Disable Startup Movies
            cbDisableMovies.IsChecked = Properties.Settings.Default.cfgDisableMovies;
            // Check Conflcits
            cbCheckConflicts.IsChecked = Properties.Settings.Default.cfgCheckConflict;
            // Conflict Warnings
            cbConflictWarnings.IsChecked = Properties.Settings.Default.cfgConflictWarnings;
            // Auto Restart Mordhau
            cbAutoCloseGame.IsChecked = Properties.Settings.Default.cfgAutoClose;
            cbRestartMordhau.IsChecked = Properties.Settings.Default.cfgRestartMordhau;
            cbRestartMordhauMode.IsChecked = Properties.Settings.Default.cfgRestartMordhauMode;
            // Shortcuts
            cbShortcutKeys.IsChecked = Properties.Settings.Default.cfgShortcutsEnabled;
            #endregion
            #region Set Application Theme & Accent
            try
            {
                cbAppThemes.SelectedItem = cbAppThemes.Items.OfType<ComboBoxItem>().FirstOrDefault(x => x.Content.ToString() == Properties.Settings.Default.appTheme);
                cbAppAccents.SelectedItem = cbAppAccents.Items.OfType<ComboBoxItem>().FirstOrDefault(x => x.Content.ToString() == Properties.Settings.Default.appAccent);
                if (appThemes.Contains(Properties.Settings.Default.appTheme) && appAccents.Contains(Properties.Settings.Default.appAccent))
                {
                    ThemeManager.ChangeAppStyle(System.Windows.Application.Current, ThemeManager.GetAccent(Properties.Settings.Default.appAccent), ThemeManager.GetAppTheme("Base" + Properties.Settings.Default.appTheme));
                } else {
                    ThemeManager.ChangeAppStyle(System.Windows.Application.Current, ThemeManager.GetAccent("Blue"), ThemeManager.GetAppTheme("BaseDark"));
                    Properties.Settings.Default.appTheme = "BaseDark";
                    Properties.Settings.Default.appAccent = "Blue";
                    Properties.Settings.Default.Save();
                    System.Windows.MessageBox.Show("Application was unable to set the theme and/or accent! Setting default style instead.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            } catch (Exception eggseption) {
                System.Windows.MessageBox.Show(String.Format("An error occured whilst trying to update the applications theme/accent! Error Message:\n\n{0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            #endregion
            #region Set App Version in About Tab
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            runVersion.Text = String.Format("{0} | Made by Dealman", fvi.FileVersion);
            #endregion
            lbCharacterList.ItemsSource = _loadedMercenaries;
            RefreshMercenaries();   // Automatically load mercenaries on app startup
            #region Set WindowState
            if (Properties.Settings.Default.isWindowMaximized == true)
            {
                this.WindowState = WindowState.Maximized;
            }
            if (Properties.Settings.Default.isWindowMaximized == false)
            {
                this.WindowState = WindowState.Normal;
            }
            #endregion
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
            ThemeManager.ChangeAppTheme(System.Windows.Application.Current, "Base" + cbi.Content);
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
            Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(System.Windows.Application.Current);
            ThemeManager.ChangeAppStyle(System.Windows.Application.Current, ThemeManager.GetAccent(cbi.Content.ToString()), appStyle.Item1);
            Properties.Settings.Default.appAccent = cbi.Content.ToString();
            Properties.Settings.Default.Save();
            UpdateGridBackgrounds();
        }
        #endregion

        #region Save Window Position + Size + WindowState
        private void MetroWindow_Closing(object sender, CancelEventArgs e)
        {
            Properties.Settings.Default.appStartupPos = new System.Drawing.Point(Convert.ToInt16(metroWindow.Left), Convert.ToInt16(metroWindow.Top));
            Properties.Settings.Default.appStartupSize = new System.Drawing.Point(Convert.ToInt16(metroWindow.ActualWidth), Convert.ToInt16(metroWindow.ActualHeight));

            // Added new boolean property (isWindowMaximized) to fix weird behavior with appStartupSize and appStartupPos
            // There is an issue with saving a Minimized WindowState which is being worked on.
            if (this.WindowState == WindowState.Maximized)
            {
                Properties.Settings.Default.isWindowMaximized = true;
            }
            if (this.WindowState == WindowState.Normal)
            {
                Properties.Settings.Default.isWindowMaximized = false;
            }
            Properties.Settings.Default.Save();
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
                    mercEditor.Title = String.Format("Mercenary Editor - Editing {0}", selectedMerc.Name);
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

        private void ResetWindowSize_Click(object sender, EventArgs e) // This seems unnecessary to have but w/e. It might be useful.
        {
            this.WindowState = WindowState.Normal;
            this.Width = 600;   // Min Width
            this.Height = 450;  // Min Height
            Properties.Settings.Default.appStartupSize = new System.Drawing.Point(Convert.ToInt16(metroWindow.ActualWidth), Convert.ToInt16(metroWindow.ActualHeight));
            Properties.Settings.Default.Save();
            #region MainWindow.g.cs Code
            /*
                             case 62:
                    this.ResetWindowSize = ((System.Windows.Controls.Button)(target));

#line 182 "..\..\MainWindow.xaml"
                    this.ResetWindowSize.Click += new System.Windows.RoutedEventHandler(this.ResetWindowSize_Click);

#line default
#line hidden
                    return;
             */
            #endregion
        }

        #region Backup files
        private void CreateBackup()
        {
            try
            {
                if (Properties.Settings.Default.cfgNormalBackup && cbNormalBackup.IsChecked.Value)
                {
                    if (Properties.Settings.Default.cfgBackupPath != Properties.Settings.Default.cfgConfigPath)
                    {
                        File.Copy(gameConfigPath, Properties.Settings.Default.cfgBackupPath + @"\Game.bak", true);
                    } else {
                        File.Copy(gameConfigPath, gameConfigPath.Replace("Game.ini", "Game.bak"), true);
                    }
                }
            } catch (Exception eggseption) {
                System.Windows.MessageBox.Show(String.Format("An error occured whilst trying to write to config. Do not worry, nothing was actually writen to the config file yet! Error Message:\n\n{0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateZIPBackup()
        {
            try
            {
                if (Properties.Settings.Default.cfgZIPBackup && cbZipBackup.IsChecked.Value)
                {
                    if (File.Exists(Properties.Settings.Default.cfgBackupPath + @"\FrankensteinerBackups.zip"))
                    {
                        using (ZipArchive zip = ZipFile.Open(Properties.Settings.Default.cfgBackupPath + @"\FrankensteinerBackups.zip", ZipArchiveMode.Update))
                        {
                            if (cbLimitZipSize.IsChecked.Value && (int)nudZipLimit.Value > 0)
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
                    if(!_loadedMercenaries[i].isOriginal || _loadedMercenaries[i].isImportedMercenary || _loadedMercenaries[i].isBeingDeleted)
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
            if (Properties.Settings.Default.cfgAutoClose)
            {
                if(IsMordhauRunning())
                {
                    KillMordhau();
                    wasMordhauKilled = true;
                }
            } else {
                if (IsMordhauRunning())
                {
                    if (System.Windows.MessageBox.Show("Mordhau is running! Saving now may result in Mordhau overwriting changes when you close it.\n\nWould you like to close it automatically before proceeding?", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        KillMordhau();
                        wasMordhauKilled = true;
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
                if(System.Windows.MessageBox.Show(String.Format("Warning!\n\nThese mercenaries will be deleted:\n{0}\n\nThis action can not be undone! Are you sure you want to proceed?", string.Join("\n", _deleteMercs.Select(x => x.Name).ToArray())), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
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
                if(System.Windows.MessageBox.Show(String.Format("Conflicts were detected for those mercenaries:\n\n{0}\n\nWould you like to try and resolve those manually? Choosing no will leave them unsaved.", string.Join("\n", _modifiedMercs.Select(x => String.Format("{0} [{1}]", x.Name, x.OriginalName)).ToArray())), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
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
                            System.Windows.MessageBox.Show(String.Format("Unable to delete mercenary {0}. Original entry was not found in the config!", _deleteMercs[i].Name), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    System.Windows.MessageBox.Show(String.Format("Successfully saved these mercenaries:\n\n{0}", string.Join("\n", _validMercs.Select(x => x.Name).ToArray())), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                } else if(_validMercs.Count == 0 && _deleteMercs.Count > 0) {
                    System.Windows.MessageBox.Show(String.Format("Successfully deleted these mercenaries:\n\n{0}", string.Join("\n", _deleteMercs.Select(x => x.Name).ToArray())), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                bTitleSave.Visibility = Visibility.Collapsed;
            }
            #endregion

            #region Alert User of Unsaved Mercenaries
            if (_failedToSave.Count > 0)
            {
                System.Windows.MessageBox.Show(String.Format("{0} mercenaries were unable to save for an unknown reason. Those mercenaries are:\n\n{1}", _failedToSave.Count, string.Join("\n", _failedToSave.Select(x => x.Name).ToArray())), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            #endregion

            #region Auto Restart Mordhau
            if(Properties.Settings.Default.cfgRestartMordhau && cbRestartMordhau.IsChecked.Value)
            {
                if(Properties.Settings.Default.cfgRestartMordhauMode && cbRestartMordhauMode.IsChecked.Value)
                {
                    if(wasMordhauKilled)
                    {
                        string mordhauExe = tbMordhauPath.Text + @"\Mordhau\Binaries\Win64\Mordhau-Win64-Shipping.exe";
                        if (File.Exists(mordhauExe))
                        {
                            Process.Start(mordhauExe);
                        } else {
                            System.Windows.MessageBox.Show(String.Format("Unable to restart Mordhau, unable to find executable. This is where I looked:\n\n{0}", mordhauExe), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                } else {
                    string mordhauExe = tbMordhauPath.Text + @"\Mordhau\Binaries\Win64\Mordhau-Win64-Shipping.exe";
                    if (File.Exists(mordhauExe))
                    {
                        Process.Start(mordhauExe);
                    } else {
                        System.Windows.MessageBox.Show(String.Format("Unable to restart Mordhau, unable to find executable. This is where I looked:\n\n{0}", mordhauExe), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                            _loadedMercenaries[i].ItemText = "Horde/BR - Unsaved Changes!";
                            _loadedMercenaries[i].isOriginal = false;
                        }
                    }
                } else {
                    _loadedMercenaries.Insert(0, importedMerc);
                    importedMerc.index = _loadedMercenaries.Count + 1;
                }
                importedMerc.UpdateItemText();
            }
        }
        private void ToggleStartupMovies(bool toggle)
        {
            if(!String.IsNullOrWhiteSpace(tbMordhauPath.Text))
            {
                string moviesPath = tbMordhauPath.Text + @"\Mordhau\Content\Movies";
                string logoMoviePath = moviesPath + @"\LogoSplash.mp4";
                string ue4MoviePath = moviesPath + @"\UE4_Logo.mp4";

                if(toggle)
                {
                    // Rename .mp4 into .bak
                    if (File.Exists(logoMoviePath) && File.Exists(ue4MoviePath))
                    {
                        File.Move(logoMoviePath, logoMoviePath.Replace(".mp4", ".bak"));
                        File.Move(ue4MoviePath, ue4MoviePath.Replace(".mp4", ".bak"));
                        Properties.Settings.Default.cfgDisableMovies = true;
                        Properties.Settings.Default.Save();
                    } else {
                        if (File.Exists(logoMoviePath.Replace(".mp4", ".bak")) && File.Exists(ue4MoviePath.Replace(".mp4", ".bak")))
                        {
                            Properties.Settings.Default.cfgDisableMovies = true;
                            Properties.Settings.Default.Save();
                        } else {
                            System.Windows.MessageBox.Show("Unable to disable startup movies - files were not found!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                } else {
                    // Rename .bak into .mp4
                    if (File.Exists(logoMoviePath.Replace(".mp4", ".bak")) && File.Exists(ue4MoviePath.Replace(".mp4", ".bak")))
                    {
                        File.Move(logoMoviePath.Replace(".mp4", ".bak"), logoMoviePath);
                        File.Move(ue4MoviePath.Replace(".mp4", ".bak"), ue4MoviePath);
                        Properties.Settings.Default.cfgDisableMovies = false;
                        Properties.Settings.Default.Save();
                    } else {
                        if (File.Exists(logoMoviePath) && File.Exists(ue4MoviePath))
                        {
                            Properties.Settings.Default.cfgDisableMovies = false;
                            Properties.Settings.Default.Save();
                        } else {
                            System.Windows.MessageBox.Show("Unable to re-enable startup movies - files were not found!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            } else {
                System.Windows.MessageBox.Show("Failed to disable Mordhau startup movies - path to Mordhau has not been set.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
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
        private void CheckForModifiedMercenaries()
        {
            if(_loadedMercenaries.Count > 0)
            {
                for (int i = 0; i < _loadedMercenaries.Count; i++)
                {
                    if (!_loadedMercenaries[i].isOriginal || _loadedMercenaries[i].isImportedMercenary || _loadedMercenaries[i].isNewMercenary || _loadedMercenaries[i].isBeingDeleted)
                    {
                        bTitleSave.Visibility = Visibility.Visible;
                        changeDetected = true;
                        break;
                    } else {
                        bTitleSave.Visibility = Visibility.Collapsed;
                        changeDetected = false;
                    }
                }
            }
        }
        private void CreateMercenary(string name, int faceType, bool isMassCreation)
        {
            // TODO: Maybe prevent duplicate names. Mordhau still loads duplicates, however.
            if (!String.IsNullOrWhiteSpace(name))
            {
                // 0 = Default, 1 = Random, 2 = Frankenstein
                string defaultCode = String.Format("CharacterProfiles=(Name=INVTEXT(\"{0}\"),GearCustomization=(Wearables=((),(),(ID=30),(),(),(),(),(ID=15),(ID=8)),Equipment=((),(),())),AppearanceCustomization=(Emblem=0,EmblemColors=(0,0),MetalRoughnessScale=0,MetalTint=0,Age=0,Voice=2,VoicePitch=157,bIsFemale=False,Fat=85,Skinny=85,Strong=85,SkinColor=0,Face=0,EyeColor=0,HairColor=0,Hair=0,FacialHair=0,Eyebrows=0),FaceCustomization=(Translate=(15360,15360,15840,12656,15364,12653,15862,0,15385,0,16320,15847,15855,15855,384,8690,8683,480,480,480,31700,480,480,480,15360,15840,18144,15840,31690,15850,15860,11471,11471,12463,12463,11471,11471,15840,15840,0,0,0,0,0,0,0,0,7665,7660),Rotate=(0,0,0,0,0,0,16,0,0,0,0,14,0,0,12288,591,367,0,15855,15855,18976,0,0,0,0,18432,0,0,18816,0,0,0,0,0,0,0,0,655,335,0,0,15840,15840,0,0,15840,15840,0,0),Scale=(14351,14351,0,15360,0,15360,0,15855,0,15855,14336,0,0,0,14350,0,0,15855,15855,15855,15840,0,15855,15855,0,15914,6,0,15840,0,0,0,0,0,0,0,0,15855,15855,0,0,0,0,0,0,0,0,0,0)),SkillsCustomization=(Perks=0))", name);
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
                        System.Windows.MessageBox.Show(String.Format("Successfully created mercenary {0}", newMercenary.Name), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    CheckForModifiedMercenaries();
                }
                else
                {
                    System.Windows.MessageBox.Show("Failed to parse face values. Could not create new mercenary!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
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
                        System.Windows.MessageBox.Show("Configuration file validated successfully!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        RefreshMercenaries();   // We'll also refresh mercenaries here
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
                    cbNormalBackup.IsEnabled = true;
                    cbZipBackup.IsEnabled = true;
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
        private void CbNormalBackup_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.cfgNormalBackup = cbNormalBackup.IsChecked.Value;
            Properties.Settings.Default.Save();
        }
        private void CbZipBackup_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.cfgZIPBackup = cbZipBackup.IsChecked.Value;
            Properties.Settings.Default.Save();
        }
        private void CbLimitZipSize_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.cfgZIPLimitActive = cbLimitZipSize.IsChecked.Value;
            Properties.Settings.Default.Save();
        }
        private void CbDisableMovies_Click(object sender, RoutedEventArgs e)
        {
            ToggleStartupMovies(cbDisableMovies.IsChecked.Value);
        }
        private void CbAutoCloseGame_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.cfgAutoClose = cbAutoCloseGame.IsChecked.Value;
            cbRestartMordhau.IsEnabled = cbAutoCloseGame.IsChecked.Value;
            cbRestartMordhauMode.IsEnabled = cbAutoCloseGame.IsChecked.Value;
            Properties.Settings.Default.Save();
        }
        private void CbCheckConflicts_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.cfgCheckConflict = cbCheckConflicts.IsChecked.Value;
            Properties.Settings.Default.Save();
        }
        private void CbConflictWarnings_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.cfgConflictWarnings = cbConflictWarnings.IsChecked.Value;
            Properties.Settings.Default.Save();
        }
        private void CbRestartMordhau_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.cfgRestartMordhau = cbRestartMordhau.IsChecked.Value;
            Properties.Settings.Default.Save();
        }
        private void CbRestartMordhauMode_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.cfgRestartMordhauMode = cbRestartMordhauMode.IsChecked.Value;
            Properties.Settings.Default.Save();
        }
        private void CbShortcutKeys_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.cfgShortcutsEnabled = cbShortcutKeys.IsChecked.Value;
            Properties.Settings.Default.Save();
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
                        bool changedOrImported = (!selectedMerc.isOriginal || selectedMerc.isOriginal && selectedMerc.isImportedMercenary) ? true : false;
                        // Edit
                        UpdateContextItem(lbContextEdit, String.Format("Edit {0}", selectedMerc.Name), true);
                        UpdateContextItem(lbContextQuickEdit, String.Format("Open {0} in Mercenary Editor", selectedMerc.Name), true);
                        UpdateContextItem(lbContextQuickFrankenstein, String.Format("Frankenstein {0}", selectedMerc.Name), true);
                        UpdateContextItem(lbContextQuickRandomize, String.Format("Randomize {0}", selectedMerc.Name), true);
                        // Save
                        UpdateContextItem(lbContextSave, String.Format("Save {0}", selectedMerc.Name), changedOrImported);
                        // Revert
                        UpdateContextItem(lbContextRevert, String.Format("Revert {0}", selectedMerc.Name), changedOrImported);
                        // Export
                        UpdateContextItem(lbContextExport, String.Format("Export {0} to Clipboard", selectedMerc.Name), true);
                        // Copy Face
                        UpdateContextItem(lbContextCopyFace, String.Format("Copy Face Values from {0}", selectedMerc.Name), true);
                        UpdateContextItem(lbContextCopyFormat, "Copy as Horde/BR Format", true);
                        // Paste Face
                        if(selectedMerc != _copiedMercenary && _copiedMercenary != null)
                        {
                            UpdateContextItem(lbContextPasteFace, String.Format("Paste Face Values From {0} to {1}", _copiedMercenary.Name, selectedMerc.Name), true);
                        } else {
                            UpdateContextItem(lbContextPasteFace, String.Format("No Copied Face Values to Paste", selectedMerc.Name), false);
                        }
                        // Delete
                        UpdateContextItem(lbContextDelete, String.Format("Mark {0} for Deletion", selectedMerc.Name), true);

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
                    UpdateContextItem(lbContextPasteFace, "Paste Face Values to (Multiple Selected)", true);
                    // Delete
                    UpdateContextItem(lbContextDelete, "Mark for Deletion (Multiple Selected)", true);
                }
            }
        }
        // Context Option: Save
        private void LbContextSave_Click(object sender, RoutedEventArgs e)
        {
            if(lbCharacterList.SelectedItems.Count == 1)
            {
                MercenaryItem selectedMerc = lbCharacterList.SelectedItem as MercenaryItem;
                if(selectedMerc != null)
                {
                    List<MercenaryItem> _modifiedMercs = new List<MercenaryItem>();
                    _modifiedMercs.Add(selectedMerc);
                    SaveAllMercenaries(_modifiedMercs);
                }
            } else if(lbCharacterList.SelectedItems.Count > 1) {
                SaveAllMercenaries(GetModifiedMercenaries());
            }
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
                        if (System.Windows.MessageBox.Show(String.Format("{0} is an imported mercenary without any changes. Reverting now will delete this mercenary.\n\nAre you sure?", merc.Name), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
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
                mercEditor.Title = String.Format("Mercenary Editor - Editing {0}", selectedMerc.Name);
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
        // Context Option: Copy as Horde/BR Format
        private void LbContextCopyFormat_Click(object sender, RoutedEventArgs e)
        {
            MercenaryItem selectedMerc = lbCharacterList.SelectedItem as MercenaryItem;
            if (selectedMerc != null)
            {
                System.Windows.Clipboard.SetText(selectedMerc.GetHordeFormat());
            }
            CheckForModifiedMercenaries();
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
        private void BReddit_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.reddit.com/r/Mordhau/comments/cll3kl/release_frankensteiner_v1200/");
        }
        private void BMordhau_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://mordhau.com/forum/topic/19301/release-frankensteiner-create-asymmetric-faces/");
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
        private void BImportSingle_Click(object sender, RoutedEventArgs e)
        {
            ImportWindow importer = new ImportWindow();
            if (importer.ShowDialog().Value)
            {
                CheckForModifiedMercenaries();
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

            for (int i = (int)nudMassNumber.Value; i > 0; i--)
            {
                CreateMercenary(String.Format("MassCreation {0}", (i)), faceType, true);
            }
            System.Windows.MessageBox.Show(String.Format("Successfully mass created {0} mercenaries!", nudMassNumber.Value), "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void BImportMultiple_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement. Remember to enable once implemented!
        }
        #endregion

        private void LbCharacterList_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(Properties.Settings.Default.cfgShortcutsEnabled)
            {
                // Enter - Open Mercenary Editor
                if (e.Key == Key.Enter)
                {
                    if (lbCharacterList.SelectedItems.Count == 1)
                    {
                        MercenaryItem _selectedMerc = lbCharacterList.SelectedItem as MercenaryItem;
                        if (_selectedMerc != null)
                        {
                            MercenaryEditor mercEditor = new MercenaryEditor(_selectedMerc);
                            mercEditor.Title = String.Format("Mercenary Editor - Editing {0}", _selectedMerc.Name);
                            if (mercEditor.ShowDialog().Value)
                            {
                                CheckForModifiedMercenaries();
                            }
                        }
                    }
                }
                // Select All Keybind
                if(e.Key == Key.A && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                {
                    lbCharacterList.SelectAll();
                }
                // Save Keybind
                if (e.Key == Key.S && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                {
                    if (lbCharacterList.SelectedItems.Count == 1)
                    {
                        MercenaryItem _selectedMerc = lbCharacterList.SelectedItem as MercenaryItem;
                        if (_selectedMerc != null)
                        {
                            List<MercenaryItem> _modifiedMercs = new List<MercenaryItem>();
                            _modifiedMercs.Add(_selectedMerc);
                            SaveAllMercenaries(_modifiedMercs);
                        }
                    } else if (lbCharacterList.SelectedItems.Count > 1) {
                        SaveAllMercenaries(GetModifiedMercenaries());
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
                            // Normal or Horde/BR Mercenary With Changes - Revert
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
                                if (System.Windows.MessageBox.Show(String.Format("{0} is an imported mercenary without any changes. Reverting now will delete this mercenary.\n\nAre you sure?", _selectedMerc.Name), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
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
                // Import Mercenary
                if (e.Key == Key.I && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                {
                    ImportWindow importer = new ImportWindow();
                    if (importer.ShowDialog().Value)
                    {
                        //CheckForModifiedMercenaries();
                    }
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
                CheckForModifiedMercenaries();
            }
        }
    }
}
