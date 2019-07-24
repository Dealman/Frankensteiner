using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DiffMatchPatch;

namespace Frankensteiner
{
    public partial class CompareWindow : MetroWindow
    {
        public CompareWindow()
        {
            InitializeComponent();
        }

        public void StartCompare(string originalText)
        {
            // TODO
            var dmp = DiffMatchPatchModule.Default;
            List<Diff> diffs = dmp.DiffMain(originalText, originalText.Replace("Stormwind guard", "Orgrimmar guard"));
            dmp.DiffCleanupSemantic(diffs);

            for(int i=0; i < diffs.Count; i++)
            {
                Console.WriteLine(diffs[i]);
            }
        }
    }
}
