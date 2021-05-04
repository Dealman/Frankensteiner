using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Frankensteiner
{
    public class ConfigParser
    {
        private string configFile;
        private string fileContents;
        private List<string> parsedMercenaries = new List<string>();

        public List<MercenaryItem> Mercenaries = new List<MercenaryItem>();

        public ConfigParser(string configPath)
        {
            configFile = configPath;
        }
        public bool ParseConfig()
        {
            if(File.Exists(configFile))
            {
                // Read the config entirely, then parse it and fetch only the mercenaries
                fileContents = File.ReadAllText(configFile);
                Regex rx = new Regex(@"^CharacterProfiles=\(.+\)", RegexOptions.Multiline);
                foreach (Match match in rx.Matches(fileContents))
                {
                    parsedMercenaries.Add(match.Value);
                }

                // Also fetch the Horde mercenary
                rx = new Regex(@"^DefaultCharacterFace.+\)\)", RegexOptions.Multiline);
                if (rx.IsMatch(fileContents))
                {
                    parsedMercenaries.Add(rx.Match(fileContents).Value);
                }

                // Make sure we actually got anything
                if(parsedMercenaries.Count > 0)
                {
                    return true;
                } else {
                    return false;
                }
            } else {
                return false;
            }
        }

        public List<string> GetParsedMercenariesList()
        {
            return parsedMercenaries;
        }

        public void ProcessMercenaries()
        {
            int counter = 0;
            foreach(string parsedMercenary in parsedMercenaries)
            {
                // Create a new mercenary
                MercenaryItem mercenary = new MercenaryItem(parsedMercenary);
                mercenary.index = counter;
                counter++;
                // Name + ItemText
                Regex rx = new Regex("\\\"(.*?)\\\""); // "\"(.+)\"" - Use Regex to find the mercenary's name
                mercenary.Name = rx.Match(parsedMercenary).Value.Replace("\"", ""); // Once found, remove the quotation marks - they were only used to help find the name
                // Check if the Name is empty, if true - that means it's the Horde/BR character and needs to be handled differently
                if(!String.IsNullOrWhiteSpace(mercenary.Name))
                {
                  /*
                  * We parse all of this to make re-writing the config file easier later. So instead of replacing only certain values which would be a headache
                  * we just replace the entire line instead.
                  */
                    mercenary.OriginalName = mercenary.Name;
                    mercenary.ItemText = mercenary.Name; // Set ItemText to be same as the name - this is what's actually shown in the ListBox
                    // Parse the Gear Customization
                    rx = new Regex(@"GearCustomization=\(.+\)\)\)");
                    mercenary.GearString = rx.Match(parsedMercenary).Value;
                    // Parse the Appearance Customization
                    rx = new Regex(@"AppearanceCustomization=\(.+\),F");
                    mercenary.AppearanceString = rx.Match(parsedMercenary).Value.Replace("),F", ")");
                    // Parse the Face Customization
                    rx = new Regex(@"FaceCustomization=\(.+\)\),");
                    mercenary.FaceString = rx.Match(parsedMercenary).Value.Replace(")),", "))");
                    // Parse the Skills
                    rx = new Regex(@"SkillsCustomization=\(.+\),");
                    mercenary.SkillString = rx.Match(parsedMercenary).Value.Replace("),", ")");
                    rx = new Regex(@"Category=.+\)");
                    mercenary.CategoryString = rx.Match(parsedMercenary).Value.Replace(")", ""); // only temporary. have to wait and see what category is. possibly related to armory rework.
                } else {
                    mercenary.OriginalName = "Horde Mercenary";
                    mercenary.Name = "Horde Mercenary";
                    mercenary.ItemText = mercenary.Name;
                    // Parse the Face Customization
                    rx = new Regex(@"(DefaultCharacterFace=\(.+)");
                    mercenary.FaceString = rx.Match(parsedMercenary).Value;
                    mercenary.isHordeMercenary = true;
                }
                if(mercenary.ParseFaceValues())
                {
                    Mercenaries.Add(mercenary);
                } else {
                    System.Windows.MessageBox.Show(String.Format("There was an error trying to parse mercenary: {0}.", mercenary.Name), "Warning", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                }
            }
        }
    }
}
