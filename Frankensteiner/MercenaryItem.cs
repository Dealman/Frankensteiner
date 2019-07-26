using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frankensteiner
{
    class MercenaryItem : ObservableObject
    {
        // Struct for Face Values
        #region Face Value Struct
        public struct FaceValue
        {
            public uint translation;
            public uint rotation;
            public uint scale;
        }
        #endregion
        // Original Properties (Used for Reverting Etc)
        #region Original Name
        private string _originalName = "";
        public string OriginalName
        {
            get { return _originalName; }
            set
            {
                if (value != _originalName)
                {
                    _originalName = value;
                    OnPropertyChanged(nameof(OriginalName));
                }
            }
        }
        #endregion
        #region Original Face Values
        private List<FaceValue> _originalFaceValues;
        public List<FaceValue> OriginalFaceValues
        {
            get { return _originalFaceValues; }
            set
            {
                if (!_originalFaceValues.SequenceEqual(value))
                {
                    _originalFaceValues = value;
                    OnPropertyChanged(nameof(OriginalFaceValues));
                }
            }
        }
        #endregion
        #region Face Values
        private List<FaceValue> _faceValues;
        public List<FaceValue> FaceValues
        {
            get { return _faceValues; }
            set
            {
                if (!_faceValues.SequenceEqual(value))
                {
                    _faceValues = value;
                    OnPropertyChanged(nameof(FaceValues));
                }
            }
        }
        #endregion
        #region Original Config Entry
        private string _originalEntry = "";
        public string OriginalEntry
        {
            get { return _originalEntry; }
            set
            {
                if (value != _originalEntry)
                {
                    _originalEntry = value;
                    OnPropertyChanged(nameof(OriginalEntry));
                }
            }
        }
        #endregion
        #region Import Code
        private string _importCode = "";
        public string ImportCode
        {
            get { return _importCode; }
            set
            {
                if (value != _importCode)
                {
                    _importCode = value;
                    OnPropertyChanged(nameof(ImportCode));
                }
            }
        }
        #endregion
        // ListBoxItem Appearance Properties
        #region Item Text
        private string _itemText = "";
        public string ItemText
        {
            get { return _itemText; }
            set
            {
                if (value != _itemText)
                {
                    _itemText = value;
                    OnPropertyChanged(nameof(ItemText));
                }
            }
        }
        #endregion
        #region Background Colour
        private Brush _bgColour;
        public Brush BackgroundColour
        {
            get { return _bgColour; }
            set
            {
                if (value != _bgColour)
                {
                    _bgColour = value;
                    OnPropertyChanged(nameof(BackgroundColour));
                }
            }
        }
        #endregion
        // Mercenary Parsed Strings
        #region Name
        private string _name = "";
        public string Name
        {
            get { return _name; }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        #endregion
        #region Gear String
        private string _gearString = "";
        public string GearString
        {
            get { return _gearString; }
            set
            {
                if (value != _gearString)
                {
                    _gearString = value;
                    OnPropertyChanged(nameof(GearString));
                }
            }
        }
        #endregion
        #region Appearance String
        private string _appearanceString = "";
        public string AppearanceString
        {
            get { return _appearanceString; }
            set
            {
                if (value != _appearanceString)
                {
                    _appearanceString = value;
                    OnPropertyChanged(nameof(AppearanceString));
                }
            }
        }
        #endregion
        #region Face String
        private string _faceString = "";
        public string FaceString
        {
            get { return _faceString; }
            set
            {
                if (value != _faceString)
                {
                    _faceString = value;
                    OnPropertyChanged(nameof(FaceString));
                }
            }
        }
        #endregion
        #region Skill String
        private string _skillString = "";
        public string SkillString
        {
            get { return _skillString; }
            set
            {
                if (value != _skillString)
                {
                    _skillString = value;
                    OnPropertyChanged(nameof(SkillString));
                }
            }
        }
        #endregion
        // Logic Fields
        public bool isImportedMercenary = false;
        public bool isHordeMercenary = false;
        public bool isOriginal = true;

        public MercenaryItem()
        {

        }
    }
}
