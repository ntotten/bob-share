using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BobShare
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Windows.Forms.OpenFileDialog fileDialog;

        public MainWindow()
        {
            InitializeComponent();
            prgUploadProgress.Minimum = 0;
            prgUploadProgress.Maximum = 100;
            fileDialog = new System.Windows.Forms.OpenFileDialog();
            btnBrowse.Click += ButtonBrowseClick;
            btnUpload.Click += ButtonUploadClick;
            btnReset.Click += ButtonResetClick;
            btnSave.Click += ButtonSaveClick;
            btnCopy.Click += ButtonCopyClick;
            if (!SettingsHaveValues())
            {
                tabUpload.IsEnabled = false;
                tabControlMain.SelectedIndex = 1;
            }
        }

        void ButtonCopyClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtDownloadUrl.Text, System.Windows.TextDataFormat.Text);
        }

        void ButtonResetClick(object sender, RoutedEventArgs e)
        {
            txtDownloadUrl.Text = "";
            txtFilePath.Text = "";
            btnBrowse.IsEnabled = true;
            btnUpload.IsEnabled = false;
            btnReset.IsEnabled = false;
            btnUpload.IsEnabled = false;
            btnCopy.IsEnabled = false;
            btnUpload.Content = "Upload";
            prgUploadProgress.Value = 0;
        }

        private void ButtonBrowseClick(object sender, RoutedEventArgs e)
        {
            var result = fileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                txtFilePath.Text = fileDialog.FileName;
                btnUpload.IsEnabled = true;
                btnReset.IsEnabled = true;
            }
        }

        private void ButtonUploadClick(object sender, RoutedEventArgs e)
        {
            btnUpload.IsEnabled = false;
            btnUpload.Content = "Uploading...";
            btnBrowse.IsEnabled = false;
            UploadFile(fileDialog.FileName);
        }

        private void ButtonSaveClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            if (SettingsHaveValues())
            {
                tabUpload.IsEnabled = true;
                tabControlMain.SelectedIndex = 0;
            }
        }

        private async void TransferCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            btnReset.IsEnabled = true;
            btnCopy.IsEnabled = true;
            btnUpload.Content = "Complete";
            var url = (string)e.UserState;
            txtDownloadUrl.Text = await ShortenUrl(url);

            MessageBox.Show("File Upload Complete.");
        }

        private void TransferProgressChanged(object sender, BlobTransfer.BlobTransferProgressChangedEventArgs e)
        {
            prgUploadProgress.Value = e.ProgressPercentage;
        }

        private bool SettingsHaveValues()
        {
            return Properties.Settings.Default.StorageName != "" &&
                   Properties.Settings.Default.StorageKey != "";
        }

        private void UploadFile(string filePath)
        {
            var credentials = new StorageCredentials(Properties.Settings.Default.StorageName, Properties.Settings.Default.StorageKey);
            var storageAccount = new CloudStorageAccount(credentials, true);

            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("shared");

            var extension = System.IO.Path.GetExtension(filePath);

            string blobName = Guid.NewGuid() + extension;
            var blockBlob = container.GetBlockBlobReference(blobName);

            var url = blockBlob.Uri.ToString();

            BlobTransfer bt = new BlobTransfer();
            bt.TransferProgressChanged += TransferProgressChanged;
            bt.TransferCompleted += TransferCompleted;
            bt.UploadBlobAsync(blockBlob, filePath, url);
        }

        private async Task<string> ShortenUrl(string longUrl)
        {
            var key = Properties.Settings.Default.BitlyKey;
            if (!String.IsNullOrWhiteSpace(key))
            {
                var url = String.Format("https://api-ssl.bitly.com/v3/shorten?access_token={0}&longUrl={1}", key, longUrl);
                HttpClient client = new HttpClient();
                var result = await client.GetAsync(url);
                var json = await result.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(json);
                string shortUrl = data.data.url;
                return shortUrl;
            }
            return longUrl;
        }

    }
}
