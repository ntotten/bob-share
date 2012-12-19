using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
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
using System.Windows.Forms;
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
        OpenFileDialog fileDialog;

        public MainWindow()
        {
            InitializeComponent();
            prgUploadProgress.Minimum = 0;
            prgUploadProgress.Maximum = 100;
            fileDialog = new OpenFileDialog();
            btnBrowse.Click += btnBrowse_Click;
            btnSend.Click += btnSend_Click;
            btnReset.Click += btnReset_Click;
            btnSettings.Click += btnSettings_Click;
        }

        void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            var window = new UserSettings();
            window.Show();
            this.Close();
        }

        void btnReset_Click(object sender, RoutedEventArgs e)
        {
            txtDownloadUrl.Text = "";
            txtFilePath.Text = "";
            btnBrowse.IsEnabled = true;
            btnSend.IsEnabled = false;
            btnReset.IsEnabled = false;
        }

        void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            DialogResult result = fileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                txtFilePath.Text = fileDialog.FileName;
                btnSend.IsEnabled = true;
                btnReset.IsEnabled = true;
            }
        }

        void btnSend_Click(object sender, RoutedEventArgs e)
        {
            btnBrowse.IsEnabled = false;
            UploadFile(fileDialog.FileName);
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
            bt.TransferProgressChanged += bt_TransferProgressChanged;
            bt.TransferCompleted += bt_TransferCompleted;
            bt.UploadBlobAsync(blockBlob, filePath, url);
        }

        async void bt_TransferCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            txtFilePath.Text = "";
            btnSend.IsEnabled = false;
            btnBrowse.IsEnabled = false;
            btnReset.IsEnabled = true;
            var url = (string)e.UserState;
            txtDownloadUrl.Text = await ShortenUrl(url);

            System.Windows.MessageBox.Show("File Upload Complete.");
        }

        void bt_TransferProgressChanged(object sender, BlobTransfer.BlobTransferProgressChangedEventArgs e)
        {
            prgUploadProgress.Value = e.ProgressPercentage;
        }

        async Task<string> ShortenUrl(string longUrl)
        {
            var key = Properties.Settings.Default.BitlyKey;
            var url = String.Format("https://api-ssl.bitly.com/v3/shorten?access_token={0}&longUrl={1}", key, longUrl);
            HttpClient client = new HttpClient();
            var result = await client.GetAsync(url);
            var json = await result.Content.ReadAsStringAsync();
            dynamic data = SimpleJson.DeserializeObject(json);
            string shortUrl = data["data"]["url"];
            return shortUrl;
        }

    }
}
