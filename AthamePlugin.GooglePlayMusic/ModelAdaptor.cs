using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Athame.PluginAPI.Service;
using GoogleMusicApi.Structure.Enums;

namespace AthamePlugin.GooglePlayMusic
{
    public static class ModelAdaptor
    {
        public static Artist GetAthameArtist(this GoogleMusicApi.Structure.Track googlePlayTrack)
        {
            return new Artist
            {
                Id = googlePlayTrack.ArtistIds[0],
                Name = googlePlayTrack.Artist
            };
        }

        public static Artist GetAthameAlbumArtist(this GoogleMusicApi.Structure.Track googlePlayTrack)
        {
            return new Artist
            {
                Name = googlePlayTrack.AlbumArtist
            };
        }

        public static Artist GetAthameArtist(this GoogleMusicApi.Structure.Album googlePlayAlbum)
        { 
            return new Artist
            {
                Id = googlePlayAlbum.ArtistId[0],
                Name = googlePlayAlbum.AlbumArtist
            };
        }

        public static Album AlbumFromTrack(this GoogleMusicApi.Structure.Track googlePlayTrack)
        {
            return new Album
            {
                Id = googlePlayTrack.AlbumId,
                Artist = googlePlayTrack.GetAthameAlbumArtist(),
                CoverPicture = new GooglePlayPicture(googlePlayTrack.AlbumArtReference[0].Url),
                Title = googlePlayTrack.Album,
                Type = AlbumType.Album
            };
        }

        public static Track ToAthameTrack(this GoogleMusicApi.Structure.Track googlePlayTrack)
        {
            return new Track
            {
                Artist = googlePlayTrack.GetAthameArtist(),
                Album = googlePlayTrack.AlbumFromTrack(),
                DiscNumber = googlePlayTrack.DiscNumber,
                Genre = googlePlayTrack.Genre,
                Title = googlePlayTrack.Title,
                Year = googlePlayTrack.Year,
                TrackNumber = googlePlayTrack.TrackNumber,
                Id = googlePlayTrack.StoreId,
                Duration = new TimeSpan(0, 0, 0, 0, (int)googlePlayTrack.DurationMillis),
                Composer = googlePlayTrack.Composer,
                CustomMetadata = new List<Metadata>
                {
                    new Metadata
                    {
                        Name = "Explicit",
                        CanDisplay = true,
                        IsFlag = true,
                        Value = (googlePlayTrack.ExplicitType == ExplicitType.Explicit).ToString()
                    }
                },
                // AFAIK tracks returned will always be downloadable or else the server will give a 404/403/400
                IsDownloadable = true
            };
        }

        public static Album ToAthameAlbum(this GoogleMusicApi.Structure.Album googlePlayAlbum)
        {
            var a = new Album
            {
                Artist = googlePlayAlbum.GetAthameArtist(),
                CoverPicture = new GooglePlayPicture(googlePlayAlbum.AlbumArtRef),
                Title = googlePlayAlbum.Name,
                Tracks = new List<Track>(),
                Id = googlePlayAlbum.AlbumId,
                Year = googlePlayAlbum.Year
            };
            if (googlePlayAlbum.Tracks != null)
            {
                foreach (var track in googlePlayAlbum.Tracks)
                {
                    var athameTrack = track.ToAthameTrack();
                    athameTrack.Album = a;
                    a.Tracks.Add(athameTrack);
                }
            }
            return a;
        }
    }
}
