using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Frankensteiner
{
	/// <summary>
	/// Interaction logic for ImportWindow.xaml
	/// </summary>
	public partial class ImportWindow : MetroWindow
	{
		private List<MercenaryItem> _mercenaryList = new List<MercenaryItem>();
		private MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

		public ImportWindow()
		{
			InitializeComponent();
			Owner = Application.Current.MainWindow;
		}

		#region Code Validation
		private void BValidate_Click(object sender, RoutedEventArgs e)
		{
			List<string> parsedMercenaries = new List<string>();
			List<MercenaryItem> _invalidMercs = new List<MercenaryItem>();

			if (!String.IsNullOrWhiteSpace(tbMercenaryCode.Text))
			{
				_mercenaryList.Clear();

				// Remove leading and trailing whitespace
				tbMercenaryCode.Text = tbMercenaryCode.Text.TrimStart().TrimEnd();

				Regex profile = new Regex(@"^CharacterProfiles=\(.+\)", RegexOptions.Multiline);
				Regex horde = new Regex(@"(DefaultCharacterFace=\(.+)");
				foreach (Match match in profile.Matches(tbMercenaryCode.Text))
				{
					parsedMercenaries.Add(match.Value);
				}
				if (horde.IsMatch(tbMercenaryCode.Text))
				{
					parsedMercenaries.Add(horde.Match(tbMercenaryCode.Text).Value);
				}

				foreach (string merc in parsedMercenaries)
				{
					MercenaryItem mercenary = new MercenaryItem(merc);
					if (mercenary.ValidateMercenaryCode())
					{
						if (!mainWindow.IsMercenaryNameTaken(mercenary.Name) || mercenary.isHordeMercenary)
						{
							_mercenaryList.Add(mercenary);
							bSave.IsEnabled = true;
						} else {
							_invalidMercs.Add(mercenary);
							MessageBox.Show($"A mercenary with the name \"{mercenary.Name}\" already exists!\nThis mercenary will not be imported.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
						}
					}
					else
					{
						_invalidMercs.Add(mercenary);
					}
				}

				_mercenaryList.Reverse(); // Reverse the list so it appears in the order it was pasted in
				if (_mercenaryList.Count == 0 && _invalidMercs.Count == 0)
				{
					MessageBox.Show("Invalid mercenary code! Make sure you copied the code correctly and try again.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
				} else
				{
					MessageBox.Show($"{_mercenaryList.Count.ToString()} mercenaries successfully validated!\n\n{_invalidMercs.Count.ToString()} mercenaries failed to validate!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}
			else
			{
				MessageBox.Show("Cannot validate empty code, you dummy!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
		#endregion

		private void BCancel_Click(object sender, RoutedEventArgs e)
		{
			mainWindow.Focus();
			this.Close();
		}

		private void BSave_Click(object sender, RoutedEventArgs e)
		{
			foreach (MercenaryItem mercenary in _mercenaryList)
			{
				mercenary.ItemText = $"{mercenary.Name} - Unsaved Imported Mercenary!";
				SolidColorBrush newColor = (Properties.Settings.Default.appTheme == "Dark") ? new SolidColorBrush(Color.FromRgb(69, 69, 69)) : new SolidColorBrush(Color.FromRgb(245, 245, 245));
				mercenary.BackgroundColour = newColor;
				mercenary.isOriginal = true;
				mercenary.isImportedMercenary = true;
				mainWindow.AddImportedMercenary(mercenary);
			}
			mainWindow.Focus();
			this.Close();
		}

		// Force revalidation of merc code after editing
		private void tbMercenaryCode_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox textBox = sender as TextBox;
			if (string.IsNullOrWhiteSpace(textBox.Text))
			{
				textBox.Text = string.Empty;
			}

			if (bSave.IsEnabled)
			{
				bSave.IsEnabled = false;
			}
		}
	}
}
