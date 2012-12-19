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

namespace BobShare
{
    /// <summary>
    /// Interaction logic for UserSettings.xaml
    /// </summary>
    public partial class UserSettings : Window
    {
        public UserSettings()
        {
            InitializeComponent();
            if (SettingsHaveValues())
            {
                //LoadMain();
            }
            btnSave.Click += btnSave_Click;
        }

        void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            if (SettingsHaveValues())
            {
                LoadMain();
            }

        }

        private bool SettingsHaveValues()
        {
            return Properties.Settings.Default.StorageName != "" &&
                   Properties.Settings.Default.StorageKey != "" &&
                   Properties.Settings.Default.BitlyKey != "";
        }

        private void LoadMain()
        {
            var main = new MainWindow();
            main.Show();
            this.Close();
        }
    }
}
