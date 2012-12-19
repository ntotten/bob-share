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
        BlobTransfer blobTransfer;

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
            ResetUI();
        }

        void ButtonCopyClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtDownloadUrl.Text, System.Windows.TextDataFormat.Text);
        }

        void ButtonResetClick(object sender, RoutedEventArgs e)
        {
            if (blobTransfer != null && blobTransfer.IsBusy)
            {
                var result = MessageBox.Show("Are you sure you want to reset? This will cancel the current upload.", "Cancel Upload", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    blobTransfer.CancelAsync();
                    btnReset.Content = "Canceling...";
                    btnReset.IsEnabled = false;
                }
                return;
            }
            ResetUI();
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
            if (e.Cancelled)
            {
                ResetUI();
                return;
            }
            btnReset.IsEnabled = true;
            btnCopy.IsEnabled = true;
            btnUpload.Content = "Complete";
            var url = (string)e.UserState;
            txtDownloadUrl.Text = await ShortenUrl(url);

            MessageBox.Show("File Upload Complete.");

            blobTransfer = null;
        }

        private void TransferProgressChanged(object sender, BlobTransfer.BlobTransferProgressChangedEventArgs e)
        {
            prgUploadProgress.Value = e.ProgressPercentage;
            statusSpeed.Content = (int)(e.Speed / 1024) + "kb/sec";
            statusBytesTransfered.Content = (int)(e.BytesSent / 1024) + (int)(e.BytesSent % 1024) + "kb sent";
            statusBytesRemaining.Content = (int)((e.TotalBytesToSend - e.BytesSent) / 1024) + "kb remaining";
            string timeRemaining;
            if (e.TimeRemaining.TotalHours > 0)
            {
                timeRemaining = Math.Round(e.TimeRemaining.TotalHours, 2) + " hours";
            }
            else if (e.TimeRemaining.TotalMinutes > 0)
            {
                timeRemaining = Math.Round(e.TimeRemaining.TotalMinutes, 2) + " mins";
            }
            else
            {
                timeRemaining = Math.Round(e.TimeRemaining.TotalSeconds, 0) + " secs";
            }
            statusTimeRemaining.Content = timeRemaining;


        }

        private void ResetUI()
        {
            txtDownloadUrl.Text = "";
            txtFilePath.Text = "";
            btnBrowse.IsEnabled = true;
            btnUpload.IsEnabled = false;
            btnUpload.Content = "Upload";
            btnCopy.IsEnabled = false;
            btnReset.Content = "Reset";
            btnReset.IsEnabled = false;

            statusBytesRemaining.Content = "0kb remaining";
            statusBytesTransfered.Content = "0kb sent";
            statusSpeed.Content = "0kb/sec";
            statusTimeRemaining.Content = "0 secs remaining";

            prgUploadProgress.Value = 0;
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

            blobTransfer = new BlobTransfer();
            blobTransfer.TransferProgressChanged += TransferProgressChanged;
            blobTransfer.TransferCompleted += TransferCompleted;
            blobTransfer.UploadBlobAsync(blockBlob, filePath, url);
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
