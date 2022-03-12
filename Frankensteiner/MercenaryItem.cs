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
            get => _originalName;
            set => SetField(ref _originalName, value);
        }
        #endregion
        #region Original Face Values
        private List<FaceValue> _originalFaceValues = new List<FaceValue>();
        public List<FaceValue> OriginalFaceValues
        {
            get => _originalFaceValues;
            set => SetField(ref _originalFaceValues, value);
        }
        #endregion
        #region Face Values
        private List<FaceValue> _faceValues = new List<FaceValue>();
        public List<FaceValue> FaceValues
        {
            get => _faceValues;
            set => SetField(ref _faceValues, value);
        }
        #endregion
        #region Original Config Entry
        private string _originalEntry = "";
        public string OriginalEntry
        {
            get => _originalEntry;
            set => SetField(ref _originalEntry, value);
        }
        #endregion
        #region Import Code
        private string _importCode = "";
        public string ImportCode
        {
            get => _importCode;
            set => SetField(ref _importCode, value);
        }
        #endregion
        // ListBoxItem Appearance Properties
        #region Item Text
        private string _itemText = "";
        public string ItemText
        {
            get => _itemText;
            set => SetField(ref _itemText, value);
        }
        #endregion
        #region Background Colour
        private SolidColorBrush _bgColour;
        public SolidColorBrush BackgroundColour
        {
            get => _bgColour;
            set => SetField(ref _bgColour, value);
        }
        #endregion
        // Mercenary Parsed Strings
        #region Name
        private string _name = "";
        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }
        #endregion
        #region Gear String
        private string _gearString = "";
        public string GearString
        {
            get => _gearString;
            set => SetField(ref _gearString, value);
        }
        #endregion
        #region Appearance String
        private string _appearanceString = "";
        public string AppearanceString
        {
            get => _appearanceString;
            set => SetField(ref _appearanceString, value);
        }
        #endregion
        #region Face String
        private string _faceString = "";
        public string FaceString
        {
            get => _faceString;
            set => SetField(ref _faceString, value);
        }
        #endregion
        #region Skill String
        private string _skillString = "";
        public string SkillString
        {
            get => _skillString;
            set => SetField(ref _skillString, value);
        }
        #endregion
        #region Category String
        private string _categoryString = "";
        public string CategoryString
        {
            get => _categoryString;
            set => SetField(ref _categoryString, value);
        }
        #endregion

        // Logic Fields
        public int index = 0;
        public bool isImportedMercenary = false;
        public bool isNewMercenary = false;
        public bool isHordeMercenary = false;
        public bool isOriginal = true;
        public bool isBeingDeleted = false;
        public bool isDuplicated = false;

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
                MessageBox.Show($"Unable to process face values for \"{Name}\".\n\nCount mismatch. Expected a total of 147 values, got {rxMatches.Count.ToString()}.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        public bool ValidateMercenaryCode()
        {
            // This is just a nasty copy and paste because I'm lazy. See ConfigParser.cs for info
            try
            {
                Regex rx = new Regex("\"(.*?)\"");
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
                        CategoryString = rx.Match(OriginalEntry).Value.Replace(")", "");
                        if (ParseFaceValues())
                        {
                          if (!Name.Contains(GearString))
                          {
                              return true;
                          } else {
                              MessageBox.Show("Lost a closing quote while trying to validate a mercenary!\n\nMake sure you have double quotes (\") around the mercenary name and category via mercenary code!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                              return false;
                          }
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
                    ItemText = String.Format("{0} - Imported Mercenary - UNSAVED", ((Name != OriginalName) ? String.Format("{0} --> {1}", OriginalName, Name) : Name));
                } else {
                    ItemText = String.Format("{0} - Imported & Modified Mercenary - UNSAVED", ((Name != OriginalName) ? String.Format("{0} --> {1}", OriginalName, Name) : Name));
                }
            }
            // New Mercenaries
            if (isNewMercenary)
            {
                if (isOriginal)
                {
                    ItemText = String.Format("{0} - New Mercenary - UNSAVED", ((Name != OriginalName) ? String.Format("{0} --> {1}", OriginalName, Name) : Name));
                } else {
                    ItemText = String.Format("{0} - New & Modified Mercenary - UNSAVED", ((Name != OriginalName) ? String.Format("{0} --> {1}", OriginalName, Name) : Name));
                }
            }
            // Horde Mercenary
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
                    ItemText = String.Format("{0}", ((Name != OriginalName) ? String.Format("{0} --> {1}", OriginalName, Name) : Name));
                } else {
                    ItemText = String.Format("{0} - Modified Mercenary - UNSAVED", ((Name != OriginalName) ? String.Format("{0} --> {1}", OriginalName, Name) : Name));
                }
            }
            // Deletion
            if(isBeingDeleted)
            {
                ItemText = String.Format("{0} - MARKED FOR DELETION", ((Name != OriginalName) ? String.Format("{0} --> {1}", OriginalName, Name) : Name));
            }
            // Duplicated
            /*if (isDuplicated)
            {
                if (isOriginal)
                {
                    ItemText = String.Format("{0} - Duplicated Mercenary - UNSAVED", ((Name != OriginalName) ? String.Format("{0} --> {1}", OriginalName, Name) : Name));
                } else {
                    ItemText = String.Format("{0} - Duplicated & Modified Mercenary - UNSAVED", ((Name != OriginalName) ? String.Format("{0} --> {1}", OriginalName, Name) : Name));
                }
            }*/
        }

        public void SetAsOriginal()
        {
            OriginalName = Name;
            OriginalEntry = ToString();
            OriginalFaceValues.Clear();
            OriginalFaceValues.AddRange(FaceValues);
            isImportedMercenary = false;
            isNewMercenary = false;
            //isDuplicated = false;
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
