using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace Frankensteiner
{
    public class Mercenary
    {
        // Mercenary Properties
        public string Name { get; set; }
        public string Gear { get; set; }
        public string Appearance { get; set; }
        public string Face { get; set; }
        public string Skills { get; set; }
        public List<uint> FaceValues = new List<uint>();
        // Original Values
        public string OriginalName { get; set; }
        public List<uint> OriginalFaceValues = new List<uint>();
        public string OriginalConfigEntry { get; set; }
        // Logic Fields
        public bool importedMercenary = false;
        public bool isHordeMercenary = false;
        public bool isOriginal = true;
        public string importCode { get; set; }
        // ListBox Related Properties
        public SolidColorBrush BackgroundColor { get; set; }
        public string ItemText { get; set; }


        public Mercenary(string characterCode)
        {
            OriginalConfigEntry = characterCode;
        }

        public bool ProcessFaceValues()
        {
            Regex rx = new Regex(@"(\d+)");
            MatchCollection rxMatches = rx.Matches(Face);
            if(rxMatches.Count > 0)
            {
                foreach (Match match in rxMatches)
                {
                    FaceValues.Add(UInt16.Parse(match.Value));
                    OriginalFaceValues.Add(UInt16.Parse(match.Value));
                }
                //
                if (FaceValues.Count == 147)
                {
                    return true;
                } else {
                    return false;
                }
            } else {
                return false;
            }
        }

        public bool ValidateMercenaryCode()
        {
            // This is just a nasty copy and paste because I'm lazy. See ConfigParser.cs for info
            try
            {
                Regex rx = new Regex(@"("".+"")"); //new Regex("\"(.+)\"");
                if(rx.IsMatch(OriginalConfigEntry))
                {
                    Name = rx.Match(OriginalConfigEntry).Value.Replace("\"", "");
                    if (!String.IsNullOrWhiteSpace(Name))
                    {
                        OriginalName = Name;
                        rx = new Regex(@"GearCustomization=\(.+\)\)\)");
                        Gear = rx.Match(OriginalConfigEntry).Value;
                        rx = new Regex(@"AppearanceCustomization=\(.+\),F");
                        Appearance = rx.Match(OriginalConfigEntry).Value.Replace("),F", ")");
                        rx = new Regex(@"FaceCustomization=\(.+\)\),");
                        Face = rx.Match(OriginalConfigEntry).Value.Replace(")),", "))");
                        rx = new Regex(@"SkillsCustomization=\(.+\)\)");
                        Skills = rx.Match(OriginalConfigEntry).Value;
                        if (ProcessFaceValues())
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
                    if (rx.IsMatch(OriginalConfigEntry))
                    {
                        OriginalName = "Horde/BR";
                        Name = "Horde/BR";
                        ItemText = "Horde/BR - Unsaved Changes!";
                        isHordeMercenary = true;
                        //rx = new Regex(@"FaceCustomization=\(.+\)\),");
                        Face = rx.Match(OriginalConfigEntry).Value.Replace(")),", "))");
                        if (ProcessFaceValues())
                        {
                            return true;
                        } else {
                            return false;
                        }
                    } else {
                        return false;
                    }
                }
            } catch (Exception eggseption) {
                return false;
            }            
        }

        public void SetNewAsOriginal()
        {
            OriginalName = Name;
            ItemText = Name;
            OriginalConfigEntry = ToString();
            OriginalFaceValues.Clear();
            OriginalFaceValues.AddRange(FaceValues);
            SolidColorBrush newColor = (Properties.Settings.Default.appTheme == "Dark") ? new SolidColorBrush(Color.FromRgb(69, 69, 69)) : new SolidColorBrush(Color.FromRgb(245, 245, 245));
            BackgroundColor = newColor;
            importedMercenary = false;
            isOriginal = true;
        }

        public void RevertCurrentChanges()
        {
            if(!importedMercenary)
            {
                Name = OriginalName;
                ItemText = Name;
                FaceValues = OriginalFaceValues;
                SolidColorBrush newColor = (Properties.Settings.Default.appTheme == "Dark") ? new SolidColorBrush(Color.FromRgb(69, 69, 69)) : new SolidColorBrush(Color.FromRgb(245, 245, 245));
                BackgroundColor = newColor;
                isOriginal = true;
            } else {
                Name = OriginalName;
                ItemText = String.Format("{0} - Unsaved Imported Mercenary", OriginalName);
                FaceValues = OriginalFaceValues;
                SolidColorBrush newColor = (Properties.Settings.Default.appTheme == "Dark") ? new SolidColorBrush(Color.FromRgb(69, 69, 69)) : new SolidColorBrush(Color.FromRgb(245, 245, 245));
                BackgroundColor = newColor;
                isOriginal = true;
            }
        }

        public override string ToString()
        {
            if(isHordeMercenary)
            {
                return String.Format("DefaultCharacterFace=(Translate=({0}),Rotate=({1}),Scale=({2}))", string.Join(",", FaceValues.Select(x => x.ToString()).ToArray(), 0, 49), string.Join(",", FaceValues.Select(x => x.ToString()).ToArray(), 49, 49), string.Join(",", FaceValues.Select(x => x.ToString()).ToArray(), 98, 49));
            } else {
                return String.Format("CharacterProfiles=(Name=INVTEXT(\"{0}\"),{1},{2},FaceCustomization=(Translate=({3}),Rotate=({4}),Scale=({5})),{6}", Name, Gear, Appearance, string.Join(",", FaceValues.Select(x => x.ToString()).ToArray(), 0, 49), string.Join(",", FaceValues.Select(x => x.ToString()).ToArray(), 49, 49), string.Join(",", FaceValues.Select(x => x.ToString()).ToArray(), 98, 49), Skills);
            }
        }
    }
}
