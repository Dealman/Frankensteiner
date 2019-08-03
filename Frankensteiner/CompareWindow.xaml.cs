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

namespace Frankensteiner
{
    public partial class CompareWindow : MetroWindow
    {
        private List<MercenaryItem> ModifiedMercs = new List<MercenaryItem>();
        private List<string> NewMercs = new List<string>();
        private string TestString { get; set; }

        public CompareWindow(List<MercenaryItem> modifiedMercs, List<string> newMercs)
        {
            InitializeComponent();

            Owner = Application.Current.MainWindow;

            ModifiedMercs = modifiedMercs;
            NewMercs = newMercs;

            lbConflictedMercenaries.ItemsSource = ModifiedMercs;
            lbNewMercenaries.ItemsSource = NewMercs;

            SolidColorBrush newColor = (Properties.Settings.Default.appTheme == "Dark") ? new SolidColorBrush(Color.FromRgb(69, 69, 69)) : new SolidColorBrush(Color.FromRgb(245, 245, 245));
            gBackground.Background = newColor;
        }

        public CompareWindow(MercenaryItem modifiedMerc, List<string> newMercs)
        {
            InitializeComponent();

            Owner = Application.Current.MainWindow;

            ModifiedMercs.Add(modifiedMerc);
            NewMercs = newMercs;

            lbConflictedMercenaries.ItemsSource = ModifiedMercs;
            lbNewMercenaries.ItemsSource = NewMercs;

            SolidColorBrush newColor = (Properties.Settings.Default.appTheme == "Dark") ? new SolidColorBrush(Color.FromRgb(69, 69, 69)) : new SolidColorBrush(Color.FromRgb(245, 245, 245));
            gBackground.Background = newColor;
        }

        private void LbConflictedMercenaries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(lbConflictedMercenaries.SelectedIndex != -1 && lbNewMercenaries.SelectedIndex != -1)
            {
                bReplace.IsEnabled = true;
            } else {
                bReplace.IsEnabled = false;
            }
        }

        private void LbNewMercenaries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbConflictedMercenaries.SelectedIndex != -1 && lbNewMercenaries.SelectedIndex != -1)
            {
                bReplace.IsEnabled = true;
            }
            else
            {
                bReplace.IsEnabled = false;
            }
        }

        private void BReplace_Click(object sender, RoutedEventArgs e)
        {
            MercenaryItem selectedMerc = lbConflictedMercenaries.SelectedItem as MercenaryItem;
            if(selectedMerc != null)
            {
                ListBoxItem item1 = (ListBoxItem)lbConflictedMercenaries.ItemContainerGenerator.ContainerFromItem(lbConflictedMercenaries.SelectedItem);
                ListBoxItem item2 = (ListBoxItem)lbNewMercenaries.ItemContainerGenerator.ContainerFromItem(lbNewMercenaries.SelectedItem);

                //MessageBox.Show(String.Format("Replace\n{0}\n\nWith\n{1}", selectedMerc.OriginalEntry, item2.Content));
                selectedMerc.OriginalEntry = item2.Content.ToString();

                lbConflictedMercenaries.SelectedIndex = -1;
                lbNewMercenaries.SelectedIndex = -1;

                item1.IsEnabled = false;
                item2.IsEnabled = false;
            }
            // Check if any Mercs left
            for(int i=0; i < lbConflictedMercenaries.Items.Count; i++)
            {
                ListBoxItem item = (ListBoxItem)lbConflictedMercenaries.ItemContainerGenerator.ContainerFromIndex(i);
                if(item.IsEnabled)
                {
                    return;
                }
            }
            bSave.IsEnabled = true;
        }

        private void BSave_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void BCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void BHowUse_Click(object sender, RoutedEventArgs e)
        {
            if(!flyoutHowUse.IsOpen)
            {
                flyoutHowUse.IsOpen = true;
            }
        }
    }
}
