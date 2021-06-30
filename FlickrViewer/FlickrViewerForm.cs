using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace FlickrViewer
{
    public partial class FlickrViewerForm : Form
    {
        private const string KEY = "fea1ba257a70522c6a3c27585f70664a";

        private static HttpClient flickrClient = new HttpClient();
        Task<string> flickrTask = null;
        public FlickrViewerForm()
        {
            InitializeComponent();
        }

        private async void searchButton_Click(object sender, EventArgs e)
        {
            if (flickrTask?.Status != TaskStatus.RanToCompletion)
            {
                var result = MessageBox.Show(
                    "Cancel the current Flickr search?",
                    "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    return;
                }
                else
                {
                    flickrClient.CancelPendingRequests();
                }
            }
            var flickrURL = "https://api.flickr.com/services/rest/?method=" +
                $"flickr.photos.search&api_key={KEY}&" +
                $"tags={inputTextBox.Text.Replace(" ", ",")}" +
                "&tag_mode=all&per_page=500&privacy_filter=1";

            imagesListBox.DataSource = null;
            imagesListBox.Items.Clear();
            pictureBox.Image = null;
            imagesListBox.Items.Add("Loading...");

            flickrTask = flickrClient.GetStringAsync(flickrURL);
            XDocument flickrXML = XDocument.Parse(await flickrTask);

            var flickrPhotos =
                from photo in flickrXML.Descendants("photo")
                let id = photo.Attribute("id").Value
                let title = photo.Attribute("title").Value
                let secret = photo.Attribute("secret").Value
                let server = photo.Attribute("server").Value
                let farm = photo.Attribute("farm").Value
                select new FlickrResult

                {
                    Title = title,
                    URL = $"https://farm{farm}.staticflickr.com/" +
                    $"{server}/{id}_{secret}.jpg"
                };
            imagesListBox.Items.Clear();

            if (flickrPhotos.Any())
            {
                imagesListBox.DataSource = flickrPhotos.ToList();
                imagesListBox.DisplayMember = "Title";
            }
            else
            {
                imagesListBox.Items.Add("No Matches");
            }

        }

        private async void imagesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (imagesListBox.SelectedItem != null)
            {
                string selectedURL =
                    ((FlickrResult)imagesListBox.SelectedItem).URL;
                byte[] imageBytes =
                    await flickrClient.GetByteArrayAsync(selectedURL);

                MemoryStream memoryStream = new MemoryStream(imageBytes);
                pictureBox.Image = Image.FromStream(memoryStream);
            }
        }
    }
}
