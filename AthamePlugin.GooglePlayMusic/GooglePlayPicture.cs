using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Athame.PluginAPI.Downloader;
using Athame.PluginAPI.Service;

namespace AthamePlugin.GooglePlayMusic
{
    public class GooglePlayPicture : Picture
    {
        private readonly string pictureUrl;
        internal GooglePlayPicture(string pictureUrl)
        {
            this.pictureUrl = pictureUrl;
            FileType = MediaFileTypes.JpegImage;
        }

        public override async Task<byte[]> GetLargestVersionAsync()
        {
            return await new HttpClient().GetByteArrayAsync(pictureUrl);
        }

        public override Task<byte[]> GetThumbnailVersionAsync()
        {
            throw new NotImplementedException();
        }

        public override bool IsThumbnailAvailable => false;
        
    }
}
