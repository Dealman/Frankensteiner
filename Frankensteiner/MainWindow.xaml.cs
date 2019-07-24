using MahApps.Metro;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
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

namespace Frankensteiner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        string[] appThemes = { "Dark", "Light" };
        string[] appAccents = { "Red", "Green", "Blue", "Purple", "Orange", "Lime", "Emerald", "Teal", "Cyan", "Cobalt", "Indigo", "Violet", "Pink", "Magenta", "Crimson", "Amber", "Yellow", "Brown", "Olive", "Steel", "Mauve", "Taupe", "Sienna" };
        public List<Mercenary> loadedMercenaries = new List<Mercenary>();
        private ConfigParser config;
        private FileSystemWatcher watcher = new FileSystemWatcher();
        private DateTime lastParsed;
        private string lastMD5;
        private string processPath;
        public bool conflictDetected = false;

        private string gameConfigPath;

        public MainWindow()
        {
            InitializeComponent();
            // TODO: Read Settings When Initializing - Remember Theme/Accents
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
                            System.Windows.MessageBox.Show("Successfully located the configuration file! You may now create", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
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
            foreach (Mercenary mercenary in loadedMercenaries)
            {
                SolidColorBrush newColor = (Properties.Settings.Default.appTheme == "Dark") ? new SolidColorBrush(Color.FromRgb(69, 69, 69)) : new SolidColorBrush(Color.FromRgb(245, 245, 245));
                mercenary.BackgroundColor = newColor;
            }
            lbCharacterList.Items.Refresh();
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

        #region Save Window Position + Size
        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.appStartupPos = new System.Drawing.Point(Convert.ToInt16(metroWindow.Left), Convert.ToInt16(metroWindow.Top));
            Properties.Settings.Default.appStartupSize = new System.Drawing.Point(Convert.ToInt16(metroWindow.ActualWidth), Convert.ToInt16(metroWindow.ActualHeight));
            Properties.Settings.Default.Save();
        }
        #endregion

        #region Mercenary List Logic
        private void BRefreshCharacters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(gameConfigPath))
                {
                    config = new ConfigParser(gameConfigPath);
                    if (config.ParseConfig())
                    {
                        config.ProcessMercenaries();
                        loadedMercenaries = config.Mercenaries;
                        lbCharacterList.ItemsSource = loadedMercenaries;
                        foreach (Mercenary mercenary in loadedMercenaries)
                        {
                            SolidColorBrush newColor = (Properties.Settings.Default.appTheme == "Dark") ? new SolidColorBrush(Color.FromRgb(69, 69, 69)) : new SolidColorBrush(Color.FromRgb(245, 245, 245));
                            mercenary.BackgroundColor = newColor;
                        }
                        lbCharacterList.Items.Refresh();
                        lbCharacterList.IsEnabled = true;

                        lastParsed = DateTime.UtcNow.ToLocalTime();
                        lastMD5 = GetConfigMD5();
                    }
                }
            } catch (Exception eggseption) {
                System.Windows.MessageBox.Show(String.Format("An error has occured whilst trying to parse the configuration file! Error Message:\n\n{0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LbCharacterList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Mercenary selectedMerc = lbCharacterList.SelectedItem as Mercenary;
            if(selectedMerc != null)
            {
                MercenaryEditor mercEditor = new MercenaryEditor(selectedMerc);
                mercEditor.Title = String.Format("Mercenary Editor - Editing {0}", selectedMerc.Name);
                if(mercEditor.ShowDialog().Value)
                {
                    lbCharacterList.Items.Refresh();
                } else {
                    lbCharacterList.Items.Refresh();
                }
            }
        }
        #endregion

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

        private bool SaveMercenaryToConfig(Mercenary mercenary)
        {
            if(!String.IsNullOrWhiteSpace(gameConfigPath))
            {
                #region Check for Mordhau Process
                if (!Properties.Settings.Default.cfgAutoClose)
                {
                    if(IsMordhauRunning())
                    {
                        if (System.Windows.MessageBox.Show("Mordhau is running!\n\nIf you save to config now, some changes may be overwritten when you restart Mordhau. Do you wish to automagically restart Mordhau before saving?\n\n", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            KillMordhau();
                        }
                    }
                } else {
                    if(IsMordhauRunning())
                    {
                        KillMordhau();
                    }
                }
                #endregion
                #region Check for Conflicts
                if(Properties.Settings.Default.cfgConflictWarnings)
                {
                    string newMD5 = GetConfigMD5();
                    if(newMD5 != lastMD5)
                    {
                        FileInfo fi = new FileInfo(gameConfigPath);
                        if (System.Windows.MessageBox.Show(String.Format("Conflicts Detected!\n\nConfig Parsed: {0}\nLast Modified: {1}\nPrevious MD5: {2}\nNew MD5: {3}\n\nProgram might be unable to save certain mercenaries if they were modified. If this is the case, you can export them and overwrite manually.\n\nDo you wish to continue?\n\nThis warning can be disabled in the settings.", lastParsed.ToLocalTime(), fi.LastWriteTime.ToLocalTime(), lastMD5, newMD5), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                        {
                            // TODO: Window to compare differences using DiffMatchPatch. Also method to try and check if the mercenary exists via its OriginalName. If true, just replace the OriginalConfigEntry propery
                            return false;
                        }
                    }
                }
                #endregion
                CreateBackup();
                CreateZIPBackup();

                string configContents = File.ReadAllText(gameConfigPath);
                if (configContents.Contains(mercenary.OriginalConfigEntry))
                {
                    //System.Windows.MessageBox.Show(mercenary.OriginalConfigEntry.ToString());
                    //System.Windows.MessageBox.Show(mercenary.ToString());
                    configContents = configContents.Replace(mercenary.OriginalConfigEntry, mercenary.ToString());
                    File.WriteAllText(gameConfigPath, configContents);
                    mercenary.SetNewAsOriginal();
                    return true;
                } else {
                    if(mercenary.importedMercenary)
                    {
                        Regex rx = new Regex("(SingletonVersion=.+)");
                        string foundMatch = rx.Match(configContents).Value;
                        configContents = configContents.Replace(foundMatch, foundMatch+"\n"+mercenary.ToString());
                        File.WriteAllText(gameConfigPath, configContents);
                        mercenary.SetNewAsOriginal();
                        return true;
                    } else {
                        if(System.Windows.MessageBox.Show(String.Format("Unable to save mercenary - original config entry for {0} was not found.\n\nDo you want to copy this mercenary to the clipboard?", mercenary.OriginalName), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            System.Windows.Clipboard.SetText(mercenary.ToString());
                        }
                        return false;
                    }                    
                }
            } else {
                System.Windows.MessageBox.Show("Unable to save mercenary - unable to find Game.ini. Are you sure the path is correct?", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        private void SaveAllMercenariesToConfig()
        {
            if (!String.IsNullOrWhiteSpace(gameConfigPath))
            {
                #region Check for Mordhau Process
                if (!Properties.Settings.Default.cfgAutoClose)
                {
                    if (IsMordhauRunning())
                    {
                        if (System.Windows.MessageBox.Show("Mordhau is running!\n\nIf you save to config now, some changes may be overwritten when you restart Mordhau. Do you wish to automagically restart Mordhau before saving?\n\n", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            KillMordhau();
                        }
                    }
                }
                else
                {
                    if (IsMordhauRunning())
                    {
                        KillMordhau();
                    }
                }
                #endregion
                #region Check for Conflicts
                if (Properties.Settings.Default.cfgConflictWarnings)
                {
                    string newMD5 = GetConfigMD5();
                    if (newMD5 != lastMD5)
                    {
                        FileInfo fi = new FileInfo(gameConfigPath);
                        if (System.Windows.MessageBox.Show(String.Format("Conflicts Detected!\n\nConfig Parsed: {0}\nLast Modified: {1}\nPrevious MD5: {2}\nNew MD5: {3}\n\nProgram might be unable to save certain mercenaries if they were modified. If this is the case, you can export them and overwrite manually.\n\nDo you wish to continue?\n\nThis warning can be disabled in the settings.", lastParsed.ToLocalTime(), fi.LastWriteTime.ToLocalTime(), lastMD5, newMD5), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                        {
                            // TODO: Window to compare differences using DiffMatchPatch. Also method to try and check if the mercenary exists via its OriginalName. If true, just replace the OriginalConfigEntry propery
                            return;
                        }
                    }
                }
                #endregion
                CreateBackup();
                CreateZIPBackup();

                string configContents = File.ReadAllText(gameConfigPath);
                List<Mercenary> importedMercenaries = new List<Mercenary>();

                for (int i=0; i < loadedMercenaries.Count; i++)
                {
                    if (!loadedMercenaries[i].importedMercenary)
                    {
                        if(!loadedMercenaries[i].isOriginal && !loadedMercenaries[i].isHordeMercenary)
                        {
                            if(configContents.Contains(loadedMercenaries[i].OriginalConfigEntry))
                            {
                                configContents = configContents.Replace(loadedMercenaries[i].OriginalConfigEntry, loadedMercenaries[i].ToString());
                                loadedMercenaries[i].SetNewAsOriginal();
                            } else {
                                if (System.Windows.MessageBox.Show(String.Format("Unable to save mercenary - original config entry for {0} was not found.\n\nDo you want to copy this mercenary to the clipboard?", loadedMercenaries[i].OriginalName), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                                {
                                    System.Windows.Clipboard.SetText(loadedMercenaries[i].ToString());
                                }
                            }
                        }
                    } else {
                        importedMercenaries.Add(loadedMercenaries[i]);
                    }
                }
                
                // We make use of SingletonVersion=## to insert the imported mercs at the top
                if(importedMercenaries.Count > 0)
                {
                    Regex rx = new Regex("(SingletonVersion=.+)");
                    if(rx.IsMatch(configContents))
                    {
                        string oldSingleton = rx.Match(configContents).Value;
                        string newSingleton = rx.Match(configContents).Value;
                        for(int i=0; i < importedMercenaries.Count; i++)
                        {
                            newSingleton = String.Format(newSingleton + "\n{0}", importedMercenaries[i]);
                            importedMercenaries[i].SetNewAsOriginal();
                        }
                        configContents = configContents.Replace(oldSingleton, newSingleton);
                    }
                }
                File.WriteAllText(gameConfigPath, configContents);
                if(Properties.Settings.Default.cfgAutoClose && !String.IsNullOrWhiteSpace(processPath))
                {
                    Process.Start(processPath);
                }
            }
        }

        public void AddImportedMercenary(Mercenary importedMerc)
        {
            if(importedMerc != null)
            {
                if(importedMerc.isHordeMercenary)
                {
                    for(int i=0; i < loadedMercenaries.Count; i++)
                    {
                        if(loadedMercenaries[i].isHordeMercenary)
                        {
                            loadedMercenaries[i].FaceValues = importedMerc.FaceValues;
                            loadedMercenaries[i].ItemText = "Horde/BR - Unsaved Changes!";
                            loadedMercenaries[i].isOriginal = false;
                            lbCharacterList.Items.Refresh();
                        }
                    }
                } else {
                    loadedMercenaries.Insert(0, importedMerc);
                    lbCharacterList.Items.Refresh();
                }
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

        private string GetConfigMD5()
        {
            if(Properties.Settings.Default.cfgCheckConflict)
            {
                if(!String.IsNullOrWhiteSpace(gameConfigPath) && File.Exists(gameConfigPath))
                {
                    MD5 md5 = MD5.Create();
                    byte[] fileContents = File.ReadAllBytes(gameConfigPath);
                    byte[] hash = md5.ComputeHash(fileContents);

                    StringBuilder sb = new StringBuilder();
                    for(int i=0; i < hash.Length; i++)
                    {
                        sb.Append(hash[i].ToString("X2"));
                    }
                    return sb.ToString();
                }
            }
            return "";
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
                    processPath = process.MainModule.FileName; // Kinda ugly this, but CBA to re-do the save methods atm so will just assume it works tee-hee
                    process.Kill();
                    process.WaitForExit();
                }
            } catch (Exception eggseption) {
                System.Windows.MessageBox.Show(String.Format("An error has occured whilst trying to fetch and/or kill the Mordhau process.\n\nError Message: {0}", eggseption.Message.ToString()), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
            //Properties.Settings.Default.Save();
        }
        private void CbAutoCloseGame_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.cfgAutoClose = cbAutoCloseGame.IsChecked.Value;
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
        #endregion

        #region ListBox Context Menu Logic
        // Context Menu Opened
        private void lbContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (lbCharacterList.SelectedIndex != -1)
            {
                Mercenary selectedMerc = lbCharacterList.SelectedItem as Mercenary;
                if(selectedMerc != null)
                {
                    lbContextEdit.Header = String.Format("Edit {0}", selectedMerc.Name);
                        lbContextEdit.IsEnabled = true;
                    lbContextSave.Header = String.Format("Save {0} Changes", selectedMerc.Name);
                        lbContextSave.IsEnabled = (!selectedMerc.isOriginal || selectedMerc.isOriginal && selectedMerc.importedMercenary) ? true : false;
                    lbContextRevert.Header = String.Format("Revert {0} Changes", selectedMerc.Name);
                        lbContextRevert.IsEnabled = (!selectedMerc.isOriginal || !selectedMerc.isOriginal && selectedMerc.importedMercenary) ? true : false;
                    lbContextExport.Header = String.Format("Export {0} to Clipboard", selectedMerc.Name);
                        lbContextExport.IsEnabled = true;
                    lbContextDelete.Header = String.Format("Delete {0}", selectedMerc.Name);
                        lbContextDelete.Visibility = (selectedMerc.importedMercenary) ? Visibility.Visible : Visibility.Collapsed;
                    // Check for any changes
                    lbContextSaveAll.IsEnabled = false;
                    lbContextRevertAll.IsEnabled = false;
                    for (int i=0; i < loadedMercenaries.Count; i++)
                    {
                        if(!loadedMercenaries[i].isOriginal || loadedMercenaries[i].importedMercenary)
                        {
                            lbContextSaveAll.IsEnabled = true;
                            lbContextRevertAll.IsEnabled = true;
                            break;
                        }
                    }
                }
            }
        }
        // Context Option: Edit
        private void LbContextEdit_Click(object sender, RoutedEventArgs e)
        {
            Mercenary selectedMerc = lbCharacterList.SelectedItem as Mercenary;
            if (selectedMerc != null)
            {
                MercenaryEditor mercEditor = new MercenaryEditor(selectedMerc);
                mercEditor.Title = String.Format("Mercenary Editor - Editing {0}", selectedMerc.Name);
                if (mercEditor.ShowDialog().Value)
                {
                    lbCharacterList.Items.Refresh();
                }
            }
        }
        // Context Option: Save
        private void LbContextSave_Click(object sender, RoutedEventArgs e)
        {
            Mercenary selectedMerc = lbCharacterList.SelectedItem as Mercenary;
            if (selectedMerc != null)
            {
                if(SaveMercenaryToConfig(selectedMerc) && !String.IsNullOrWhiteSpace(processPath))
                {
                    Process.Start(processPath);
                }
                lbCharacterList.Items.Refresh();
            }
        }
        // Context Option: Revert
        private void LbContextRevert_Click(object sender, RoutedEventArgs e)
        {
            Mercenary selectedMerc = lbCharacterList.SelectedItem as Mercenary;
            if (selectedMerc != null)
            {
                if (!selectedMerc.isOriginal)
                {
                    selectedMerc.RevertCurrentChanges();
                    lbCharacterList.Items.Refresh();
                }
            }
        }
        //Context Option: Export
        private void LbContextExport_Click(object sender, RoutedEventArgs e)
        {
            Mercenary selectedMerc = lbCharacterList.SelectedItem as Mercenary; // TODO: Try/Catch this?
            if (selectedMerc != null)
            {
                if(selectedMerc.isOriginal)
                {
                    System.Windows.Clipboard.SetText(selectedMerc.OriginalConfigEntry);
                } else {
                    System.Windows.Clipboard.SetText(selectedMerc.ToString());
                }
            }
        }
        // Context Option: Import
        private void LbContextImport_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Open Import Window Here
            ImportWindow importer = new ImportWindow();
            if(importer.ShowDialog().Value)
            {
                // Save
            }
        }
        // Context Option: Save All
        private void LbContextSaveAll_Click(object sender, RoutedEventArgs e)
        {
            SaveAllMercenariesToConfig();
            lbCharacterList.Items.Refresh();
        }
        // Context Option: Revert All
        private void LbContextRevertAll_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Optimization? use the first for loop to check which mercenaries need to be reverted. Instead of looping them all a 2nd time.
            int counter = 0;
            for(int i=0; i < loadedMercenaries.Count; i++)
            {
                if(!loadedMercenaries[i].isOriginal)
                {
                    counter++;
                }
            }

            if(counter > 0)
            {
                if (System.Windows.MessageBox.Show(String.Format("Are you sure you want to revert all changes? This will restore all modified mercenaries to their original name and values. Can't be undone.\n\nYou'll be reverting {0} mercenaries to their original.", counter), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    for (int i = 0; i < loadedMercenaries.Count; i++)
                    {
                        if (!loadedMercenaries[i].isOriginal)
                        {
                            loadedMercenaries[i].RevertCurrentChanges();
                            lbCharacterList.Items.Refresh();
                        }
                    }
                }
            }
        }
        // Context Option: Delete Import
        private void LbContextDelete_Click(object sender, RoutedEventArgs e)
        {
            Mercenary selectedMerc = lbCharacterList.SelectedItem as Mercenary; // TODO: Try/Catch this?
            if(selectedMerc != null)
            {
                if (System.Windows.MessageBox.Show(String.Format("Are you sure you want to delete {0}? This action can't be undone.", selectedMerc.Name), "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    loadedMercenaries.Remove(selectedMerc);
                    lbCharacterList.Items.Refresh();
                }
            }            
        }
        #endregion

        private void NudZipLimit_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double?> e)
        {
            Properties.Settings.Default.cfgZIPLimit = e.NewValue.Value;
            Properties.Settings.Default.Save();
        }
    }
}
