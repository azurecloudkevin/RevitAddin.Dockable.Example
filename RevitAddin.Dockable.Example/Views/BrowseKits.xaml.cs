using Autodesk.Revit.UI;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using TextBox = System.Windows.Controls.TextBox;
using System.Collections.Generic;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.IO;
using Autodesk.Revit.DB;

namespace RevitAddin.Dockable.Example.Views
{
    public partial class BrowseKits : Page, IDockablePaneProvider
    {
        public static Guid Guid => new Guid("F1F1F1F1-1F1F-1F1F-1F0F-1F1F1F1F1F1F");
        static string kustoConnectionString = "Data Source=https://wsp-kits.kusto.windows.net;Initial Catalog=kits;AAD Federated Security=True;";
        static string blobConnectionString = "DefaultEndpointsProtocol=https;AccountName=wspkits;AccountKey=your_account_key;EndpointSuffix=core.windows.net";
        static string blobContainerName = "kits";
        private readonly UIApplication _uiApp;

        public static List<List<string>> KitsArray { get; set; } = new List<List<string>>();
        // Create a Kusto client
        static ICslQueryProvider kustoClient = KustoClientFactory.CreateCslQueryProvider(kustoConnectionString);

        public BrowseKits()
        {
            InitializeComponent();
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.VisibleByDefault = true;
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Bottom,
            };
        }

        // create a container for the kits
        private void CreateKitsContainer()
        {
            StackPanel kitsContainer = new StackPanel();
            kitsContainer.Name = "KitsContainer";
            kitsContainer.Margin = new Thickness(5);
        }

        // create a text box to search kits
        private void CreateSearchBox()
        {
            TextBox searchBox = new TextBox();
            searchBox.Name = "SearchBox";
            searchBox.Margin = new Thickness(5);
            searchBox.TextChanged += SearchBox_TextChanged;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // use text from the search box to filter the kits
            var searchText = (sender as TextBox).Text;
            // create a query string to filter the kits
            AddKitsToContainer(searchText);
        }

        // read database and add kits to the container
        private void AddKitsToContainer(string queryString = "take 10")
        {
            var kitItem = new StackPanel();
            kitItem.Name = "KitItem";
            kitItem.Margin = new Thickness(5);
            // read from Azure Data Explorer
            var query = "Kits | " + queryString;
            var reader = kustoClient.ExecuteQuery(query);
            while (reader.Read())
            {
                // create a kit item
                kitItem.Children.Add(new TextBlock { Text = reader["Name"].ToString() });
                kitItem.Children.Add(new Button { Content = "Select" });

                // add callback function for button
                var button = kitItem.Children[1] as Button;
                button.Click += (s, e) =>
                {
                    // do something with the kit
                    OpenKit(reader["Location"].ToString());
                };

                // add Kit data from ADX to a list
                KitsArray.Add(new List<string> { reader["Name"].ToString(), reader["Location"].ToString() });
            }

        }

        private async void OpenKit(string blobName)
        {
            // Create a BlobServiceClient
            BlobServiceClient blobServiceClient = new BlobServiceClient(blobConnectionString);

            // Get the container client
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(blobContainerName);

            // Get the blob client
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            // Download the blob's content
            BlobDownloadInfo download = await blobClient.DownloadAsync();

            // Save the content to a temporary file
            string tempFilePath = Path.GetTempFileName();
            using (var fileStream = File.OpenWrite(tempFilePath))
            {
                await download.Content.CopyToAsync(fileStream);
            }

            // Open the file in Revit
            Document doc = _uiApp.Application.OpenDocumentFile(tempFilePath);

            // Optionally, you can delete the temporary file after opening it
            File.Delete(tempFilePath);
        }

        public int Number { get; set; }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            button.Content = Number++;
        }
    }
}