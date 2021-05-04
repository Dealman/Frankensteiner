using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Frankensteiner
{
    public class MercenaryItem : ObservableObject
    {
        // Struct for Face Values
        #region Face Value Struct
        public struct FaceValue
        {
            public ushort Translation, Rotation, Scale;

            public FaceValue(ushort trans, ushort rot, ushort scale)
            {
                Translation = trans;
                Rotation = rot;
                Scale = scale;
            }
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
        private List<FaceValue> _originalFaceValues = new List<FaceValue>();
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
        private List<FaceValue> _faceValues = new List<FaceValue>();
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
        private SolidColorBrush _bgColour;
        public SolidColorBrush BackgroundColour
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
        #region Category String
        private string _categoryString = "";
        public string CategoryString
        {
            get { return _categoryString; }
            set
            {
                if (value != _categoryString)
                {
                    _categoryString = value;
                    OnPropertyChanged(nameof(CategoryString));
                }
            }
        }
        #endregion
        // Logic Fields
        public int index = 0;
        public bool isImportedMercenary = false;
        public bool isNewMercenary = false;
        public bool isHordeMercenary = false;
        public bool isOriginal = true;
        public bool isBeingDeleted = false;

        public MercenaryItem()
        {

        }
        public MercenaryItem(string originalCode)
        {
            OriginalEntry = originalCode;
        }

        public bool ParseFaceValues()
        {
            Regex rx = new Regex(@"(\d+)");
            MatchCollection rxMatches = rx.Matches(FaceString);
            if (rxMatches.Count == 147)
            {
                for(int i=0; i < 49; i++)
                {
                    FaceValue _faceValue = new FaceValue(UInt16.Parse(rxMatches[i].Value), UInt16.Parse(rxMatches[i + 49].Value), UInt16.Parse(rxMatches[i + 98].Value));
                    _originalFaceValues.Add(_faceValue);
                    _faceValues.Add(_faceValue);
                }
                return true;
            } else {
                MessageBox.Show(String.Format("Unable to process face values for mercenary. Count mismatch - expected a total of 147 values. Got {0}.", rxMatches.Count), "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }
        public bool ValidateMercenaryCode()
        {
            // This is just a nasty copy and paste because I'm lazy. See ConfigParser.cs for info
            try
            {
                //Regex rx = new Regex(@"("".+"")"); //new Regex("\"(.+)\"");
                Regex rx = new Regex("\\\"(.*?)\\\"");
                if (rx.IsMatch(OriginalEntry))
                {
                    Name = rx.Match(OriginalEntry).Value.Replace("\"", "");
                    if (!String.IsNullOrWhiteSpace(Name))
                    {
                        OriginalName = Name;
                        ItemText = Name;
                        rx = new Regex(@"GearCustomization=\(.+\)\)\)");
                        GearString = rx.Match(OriginalEntry).Value;
                        rx = new Regex(@"AppearanceCustomization=\(.+\),F");
                        AppearanceString = rx.Match(OriginalEntry).Value.Replace("),F", ")");
                        rx = new Regex(@"FaceCustomization=\(.+\)\),");
                        FaceString = rx.Match(OriginalEntry).Value.Replace(")),", "))");
                        rx = new Regex(@"SkillsCustomization=\(.+\),");
                        SkillString = rx.Match(OriginalEntry).Value.Replace("),", ")");
                        rx = new Regex(@"Category=.+\)");
                        CategoryString = rx.Match(OriginalEntry).Value.Replace(")", ""); // only temporary
                        if (ParseFaceValues())
                        {
                            return true;
                        } else {
                            return false;
                        }
                    } else {
                        return false;
                    }
                } else {
                    rx = new Regex(@"^DefaultCharacterFace.+\)\)");
                    if (rx.IsMatch(OriginalEntry))
                    {
                        OriginalName = "Horde Mercenary";
                        Name = "Horde Mercenary";
                        ItemText = "Horde Mercenary - Unsaved Changes!";
                        isHordeMercenary = true;
                        //rx = new Regex(@"FaceCustomization=\(.+\)\),");
                        FaceString = rx.Match(OriginalEntry).Value.Replace(")),", "))");
                        if (ParseFaceValues())
                        {
                            return true;
                        } else {
                            return false;
                        }
                    } else {
                        return false;
                    }
                }
            } catch (Exception) {
                return false;
            }
        }
        public void UpdateItemText()
        {
            // Imported Mercenaries
            if(isImportedMercenary)
            {
                if (isOriginal)
                {
                    ItemText = String.Format("{0} - Imported Mercenary - UNSAVED", ((Name != OriginalName) ? String.Format("{0}->{1}", OriginalName, Name) : Name));
                } else {
                    ItemText = String.Format("{0} - Imported & Modified Mercenary - UNSAVED", ((Name != OriginalName) ? String.Format("{0}->{1}", OriginalName, Name) : Name));
                }
            }
            // New Mercenaries
            if (isNewMercenary)
            {
                if (isOriginal)
                {
                    ItemText = String.Format("{0} - New Mercenary - UNSAVED", ((Name != OriginalName) ? String.Format("{0}->{1}", OriginalName, Name) : Name));
                } else {
                    ItemText = String.Format("{0} - New & Modified Mercenary - UNSAVED", ((Name != OriginalName) ? String.Format("{0}->{1}", OriginalName, Name) : Name));
                }
            }
            // Horde/BR Mercenary
            if (isHordeMercenary)
            {
                if (isOriginal)
                {
                    ItemText = String.Format("{0}", Name);
                } else {
                    ItemText = String.Format("{0} - Modified Mercenary - UNSAVED", Name);
                }
            }
            // Normal
            if(!isImportedMercenary && !isNewMercenary && !isHordeMercenary)
            {
                if (isOriginal)
                {
                    ItemText = String.Format("{0}", ((Name != OriginalName) ? String.Format("{0}->{1}", OriginalName, Name) : Name));
                } else {
                    ItemText = String.Format("{0} - Modified Mercenary - UNSAVED", ((Name != OriginalName) ? String.Format("{0}->{1}", OriginalName, Name) : Name));
                }
            }
            // Deletion
            if(isBeingDeleted)
            {
                ItemText = String.Format("{0} - MARKED FOR DELETION", ((Name != OriginalName) ? String.Format("{0}->{1}", OriginalName, Name) : Name));
            }
        }
        public void SetAsOriginal()
        {
            OriginalName = Name;
            OriginalEntry = ToString();
            OriginalFaceValues.Clear();
            OriginalFaceValues.AddRange(FaceValues);
            isImportedMercenary = false;
            isNewMercenary = false;
            isOriginal = true;
            UpdateItemText();
        }
        public void RevertCurrentChanges()
        {
            Name = OriginalName;
            FaceValues = OriginalFaceValues;
            isBeingDeleted = false;
            isOriginal = true;
            UpdateItemText();
        }
        public void Frankenstein()
        {
            Random rnd = new Random(Guid.NewGuid().GetHashCode());
            for(int i=0; i < FaceValues.Count; i++)
            {
                FaceValues[i] = new FaceValue((ushort)(rnd.Next(0, 2) == 1 ? 0 : 65535), (ushort)(rnd.Next(0, 2) == 1 ? 0 : 65535), (ushort)(rnd.Next(0, 2) == 1 ? 0 : 65535));
            }
            isOriginal = false;
            UpdateItemText();
        }
        public void Randomize()
        {
            Random rnd = new Random(Guid.NewGuid().GetHashCode());
            for(int i=0; i < FaceValues.Count; i++)
            {
                FaceValues[i] = new FaceValue((ushort)rnd.Next(0, 65535), (ushort)rnd.Next(0, 65535), (ushort)rnd.Next(0, 65535));
            }
            isOriginal = false;
            UpdateItemText();
        }

        public string GetHordeFormat()
        {
            return String.Format("DefaultCharacterFace=(Translate=({0}),Rotate=({1}),Scale=({2}))", string.Join(",", FaceValues.Select(x => x.Translation.ToString()).ToArray()), string.Join(",", FaceValues.Select(x => x.Rotation.ToString()).ToArray()), string.Join(",", FaceValues.Select(x => x.Scale.ToString()).ToArray()));
        }

        public override string ToString()
        {
            if (isHordeMercenary)
            {
                return String.Format("DefaultCharacterFace=(Translate=({0}),Rotate=({1}),Scale=({2}))", string.Join(",", FaceValues.Select(x => x.Translation.ToString()).ToArray()), string.Join(",", FaceValues.Select(x => x.Rotation.ToString()).ToArray()), string.Join(",", FaceValues.Select(x => x.Scale.ToString()).ToArray()));
            } else {
                return String.Format("CharacterProfiles=(Name=INVTEXT(\"{0}\"),{1},{2},FaceCustomization=(Translate=({3}),Rotate=({4}),Scale=({5})),{6},{7})", Name, GearString, AppearanceString, string.Join(",", FaceValues.Select(x => x.Translation.ToString()).ToArray()), string.Join(",", FaceValues.Select(x => x.Rotation.ToString()).ToArray()), string.Join(",", FaceValues.Select(x => x.Scale.ToString()).ToArray()), SkillString, CategoryString);
            }
        }
    }
}
