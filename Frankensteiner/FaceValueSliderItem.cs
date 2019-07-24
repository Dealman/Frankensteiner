using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frankensteiner
{
    class FaceValueSliderItem : ObservableObject
    {
        public String Description { get; private set; }
        #region Translation, Rotation and Scale Properties
        private uint _translation = 0;
        public uint Translation
        {
            get { return _translation; }
            set
            {
                if (value != _translation)
                {
                    _translation = value;
                    OnPropertyChanged(nameof(Translation));
                }
            }
        }
        private uint _rotation = 0;
        public uint Rotation
        {
            get { return _rotation; }
            set
            {
                if (value != _rotation)
                {
                    _rotation = value;
                    OnPropertyChanged(nameof(Rotation));
                }
            }
        }
        private uint _scale = 0;
        public uint Scale
        {
            get { return _scale; }
            set
            {
                if (value != _scale)
                {
                    _scale = value;
                    OnPropertyChanged(nameof(Scale));
                }
            }
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

        public void UpdateDescription(string newText)
        {
            if (!String.IsNullOrWhiteSpace(newText))
            {
                Description = newText;
                OnPropertyChanged(nameof(Description));
            }
        }
    }
}
