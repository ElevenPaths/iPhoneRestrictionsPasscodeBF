using FirstFloor.ModernUI.Windows.Controls;
using iPhoneRestrictionsPasscodeBFLib;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace iPhoneRestrictionsPasscodeBFTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        private string devicesFolder;
        public MainWindow()
        {
            InitializeComponent();
            this.devicesFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Apple Computer\MobileSync\Backup\");
        }

        private void ModernWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (System.IO.Directory.Exists(devicesFolder))
            {
                var directories = System.IO.Directory.GetDirectories(devicesFolder);
                this.deviceList.ItemsSource = directories.Where(q => System.IO.Directory.GetFiles(q).Any()).Select(p => System.IO.Path.GetFileName(p));
            }
        }

        private void breakButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.deviceList.SelectedValue != null)
            {
                loadingBar.Visibility = Visibility.Visible;
                this.breakButton.IsEnabled = false;
                this.passcodeValue.Text = String.Empty;
                this.deviceList.IsEnabled = false;
                string backupFolder = System.IO.Path.Combine(this.devicesFolder, (string)this.deviceList.SelectedValue);
                Task.Factory.StartNew(() =>
                {
                    bool fileFound = true;
                    string passCode = String.Empty;
                    try
                    {
                        passCode = PasscodeBreaker.BreakPassCode(backupFolder);
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        fileFound = false;
                    }

                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        this.loadingBar.Visibility = Visibility.Hidden;
                        this.breakButton.IsEnabled = this.deviceList.SelectedIndex >= 0;
                        if (fileFound)
                        {
                            if (String.IsNullOrEmpty(passCode))
                            {
                                MessageBox.Show("No passcode found :(", "Error breaking passcode", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            else
                            {
                                this.passcodeValue.Text = passCode;
                            }
                        }
                        else
                        {
                            MessageBox.Show("No restrictionspassword file found :(", "File not found", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        this.deviceList.IsEnabled = true;
                    }));
                });
            }
        }

        private void deviceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.breakButton.IsEnabled = this.deviceList.SelectedIndex >= 0;
        }
    }
}
