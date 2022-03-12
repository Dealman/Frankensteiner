using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Frankensteiner
{
	class FaceValueSliderItem : ObservableObject
	{
		public String Description { get; private set; }
		public String Foreground { get; private set; }

		#region Translation, Rotation and Scale Properties
		private uint _translation = 0;
		public uint Translation
		{
			get => _translation;
			set => SetField(ref _translation, value);
		}
		private uint _rotation = 0;
		public uint Rotation
		{
			get => _rotation;
			set => SetField(ref _rotation, value);
		}
		private uint _scale = 0;
		public uint Scale
		{
			get => _scale;
			set => SetField(ref _scale, value);
		}
		private SolidColorBrush _bg = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
		public SolidColorBrush BackgroundColour
		{
			get => _bg;
			set => SetField(ref _bg, value);
		}
		#endregion

		public FaceValueSliderItem()
		{

		}

		public FaceValueSliderItem(uint newTrans, uint newRot, uint newScale)
		{
			Translation = newTrans;
			Rotation = newRot;
			Scale = newScale;
		}

		public void UpdateValues(uint newTrans, uint newRot, uint newScale)
		{
			Translation = newTrans;
			Rotation = newRot;
			Scale = newScale;
		}

		public void UpdateDescription(string newText, string foreground = "White")
		{
			if (!String.IsNullOrWhiteSpace(newText))
			{
				Description = newText;
				Foreground = foreground;
				OnPropertyChanged(nameof(Description));
			}
		}
	}
}
