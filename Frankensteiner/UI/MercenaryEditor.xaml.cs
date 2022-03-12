using MahApps.Metro.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Frankensteiner
{
	/// <summary>
	/// Interaction logic for MercenaryEditor.xaml
	/// </summary>
	public partial class MercenaryEditor : MetroWindow
	{
		private MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;
		private ObservableCollection<FaceValueSliderItem> _sliders = new ObservableCollection<FaceValueSliderItem>();
		private MercenaryItem mercenary;

		public MercenaryEditor(MercenaryItem selectedMerc)
		{
			InitializeComponent();
			mercenary = selectedMerc;
			tbNewName.Text = mercenary.Name;

			Owner = Application.Current.MainWindow; // Set MainWindow as owner, so it'll make proper use of WindowStartupLocation.Owner
			#region Update colours to fit the active theme + alternating colors for readability
			if (Properties.Settings.Default.appTheme == "Dark")
			{
				gBackground.Background = new SolidColorBrush(Color.FromRgb(55, 55, 55));
				dgValueList.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(55, 55, 55));
			} else {
				gBackground.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
				dgValueList.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(235, 235, 235));
			}
			#endregion

			/*
				I had a talk with marox, and he said that some of the bones are just "virtual parents" and don't do anything.
				"They're virtual groupings of bones that don't otherwise exist"
				I've tested these unknown values for quite some time, and have come to the conclusion that they're all "virtual parents"
			*/

			string[] boneNames =
				{"Mouth - Middle", "Mouth - Edges", "Maxilla", "Left Eyebrow", "Virtual Parent", "Right Eyebrow", "Right Eye",
				"Virtual Parent", "Virtual Parent", "Virtual Parent", "Mouth", "Left Eye", "Lips - Left Edge", "Lips - Right Edge",
				"Chin", "Jaw - Left", "Jaw - Right", "Lower Lip - Middle", "Lower Lip - Left", "Lower Lip - Right", "Infraorbital Margin - Left",
				"Upper Lip - Middle", "Upper Lip - Left", "Upper Lip - Right", "Philtrum", "Nose - Tip", "Nose Bridge", "Nose Bridge - Top",
				"Infraorbital Margin - Right", "Cheek - Left", "Cheek - Right", "Left Eyebrow - Inner", "Right Eyebrow - Inner", "Left Eyebrow - Middle", "Right Eyebrow - Middle",
				"Left Eyebrow - Outter", "Right Eyebrow - Outter", "Ear - Left", "Ear - Right", "Virtual Parent", "Virtual Parent", "Left Eyelid - Top",
				"Left Eyelid - Bottom", "Virtual Parent", "Virtual Parent", "Right Eyelid - Top", "Right Eyelid - Bottom", "Cheekbone - Left", "Cheekbone - Right"};

			// Create 49 sliders, it consists of 3 separate sliders - one for each value
			dgValueList.ItemsSource = _sliders;
			for (int i=0; i < 49; i++)
			{
				var newSlider = new FaceValueSliderItem();
				newSlider.Translation = selectedMerc.FaceValues[i].Translation;
				newSlider.Rotation = selectedMerc.FaceValues[i].Rotation;
				newSlider.Scale = selectedMerc.FaceValues[i].Scale;

				if (boneNames[i] == "Virtual Parent")
				{
					// Make "Virtual Parent" bone labels red so the user might figure out that it doesn't do anything
					newSlider.UpdateDescription(boneNames[i], "Red");
				} else {
					newSlider.UpdateDescription(boneNames[i]);
				}
				
				_sliders.Add(newSlider);
			}
			tbNewName.IsEnabled = !mercenary.isHordeMercenary;
		}

		private void BOpenRandomizer_Click(object sender, RoutedEventArgs e)
		{
			settingsFlyout.IsOpen = true;
		}

		private void BRandomize_Click(object sender, RoutedEventArgs e)
		{
			int minValue = Convert.ToUInt16(sliderRandomRange.LowerValue);
			int maxValue = Convert.ToUInt16(sliderRandomRange.UpperValue);
			Random rnd = new Random();
			for(int i=0; i < _sliders.Count(); i++)
			{
				uint newTrans = (uint)rnd.Next(minValue, maxValue);
				uint newRot = (uint)rnd.Next(minValue, maxValue);
				uint newScale = (uint)rnd.Next(minValue, maxValue);

				_sliders[i].Translation = newTrans;
				_sliders[i].Rotation = newRot;
				_sliders[i].Scale = newScale;
			}
		}

		private void BMinimizeAll_Click(object sender, RoutedEventArgs e)
		{
			for (int i = 0; i < _sliders.Count(); i++)
			{
				_sliders[i].Translation = 0;
				_sliders[i].Rotation = 0;
				_sliders[i].Scale = 0;
			}
		}

		private void BMaximizeAll_Click(object sender, RoutedEventArgs e)
		{
			for (int i = 0; i < _sliders.Count(); i++)
			{
				_sliders[i].Translation = 65535;
				_sliders[i].Rotation = 65535;
				_sliders[i].Scale = 65535;
			}
		}

		private void BFrankenstein_Click(object sender, RoutedEventArgs e)
		{
			Random rnd = new Random();
			for (int i = 0; i < _sliders.Count(); i++)
			{
				_sliders[i].Translation = (uint)(rnd.Next(0, 2) == 1 ? 0 : 65535);
				_sliders[i].Rotation = (uint)(rnd.Next(0, 2) == 1 ? 0 : 65535);
				_sliders[i].Scale = (uint)(rnd.Next(0, 2) == 1 ? 0 : 65535);
			}
		}

		private void BSave_Click(object sender, RoutedEventArgs e)
		{
			for (int i = 0; i < _sliders.Count(); i++)
			{
				var faceSlider = _sliders[i];
				mercenary.FaceValues[i] = new MercenaryItem.FaceValue(Convert.ToUInt16(faceSlider.Translation), Convert.ToUInt16(faceSlider.Rotation), Convert.ToUInt16(faceSlider.Scale));
			}

			if (tbNewName.Text != null)
			{
				string stripped = tbNewName.Text.Replace("\"", ""); // If name contains double quotes, remove them
				if (!mainWindow.IsMercenaryNameTaken(stripped))
				{
					mercenary.Name = String.IsNullOrWhiteSpace(stripped) ? mercenary.Name : stripped;
				}
			}

			if(mercenary.Name == mercenary.OriginalName)
			{
				if (!mercenary.OriginalFaceValues.SequenceEqual(mercenary.FaceValues))
				{
					mercenary.isOriginal = false;
					mercenary.UpdateItemText();
					this.DialogResult = true;
				} else {
					mercenary.UpdateItemText();
					mercenary.isOriginal = true;
					this.DialogResult = false;
				}
			} else {
				mercenary.isOriginal = false;
				mercenary.UpdateItemText();
				this.DialogResult = true;
			}
		}

		private void TbNewName_TextChanged(object sender, TextChangedEventArgs e)
		{
			if(!String.IsNullOrWhiteSpace(tbNewName.Text) && !bSave.IsEnabled)
			{
				//bSave.IsEnabled = true;
			}
		}
	}
}
