using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Athame.PluginAPI;
using Athame.PluginAPI.Downloader;
using Athame.PluginAPI.Service;
using GoogleMusicApi.Common;
using GoogleMusicApi.Structure;
using Album = Athame.PluginAPI.Service.Album;
using Artist = Athame.PluginAPI.Service.Artist;
using Playlist = Athame.PluginAPI.Service.Playlist;
using SearchResult = Athame.PluginAPI.Service.SearchResult;
using Track = Athame.PluginAPI.Service.Track;

namespace AthamePlugin.GooglePlayMusic
{
    public class PlayMusicService : MusicService, IUsernamePasswordAuthenticationAsync
    {
        public override int ApiVersion => 3;

        public override PluginInfo Info => new PluginInfo
        {
            Name = "Google Play Music",
            Description = "Plugin for the Google Play Music service.",
            Author = "svbnet",
            Website = new Uri("https://github.com/svbnet/AthamePlugin.GooglePlayMusic")
        };

        private const string GooglePlayHost = "play.google.com";
        private MobileClient client = new MobileClient();
        private PlayMusicServiceSettings settings = new PlayMusicServiceSettings();


        public override async Task<TrackFile> GetDownloadableTrackAsync(Track track)
        {
            // Only property we need to set is Track.StoreId (see Google.Music/GoogleMusicApi.UWP/Requests/Data/StreamUrlGetRequest.cs:32)
            var streamUrl = await client.GetStreamUrlAsync(new GoogleMusicApi.Structure.Track { StoreId = track.Id });
            if (streamUrl == null)
            {
                throw new InvalidSessionException("Play Music: Stream URL unavailable. Check your subscription is active then try again.");
            }
            // Unfortunately I have forgotten the various stream qualities available on Play Music because my subscription ran out,
            // so I will set the bitrate to -1, i.e. unknown
            // What is known is that all streams are MP3, so this should work.
            return new TrackFile
            {
                BitRate = -1,
                DownloadUri = streamUrl,
                FileType = MediaFileTypes.Mpeg3Audio,
                Track = track
            };
        }

        public override async Task<Playlist> GetPlaylistAsync(string playlistId)
        {
            // Shitty URL decode implementation because honestly,
            // playlist ID handling is a mess
            playlistId = playlistId.Replace("%3D", "=");
            var playlistResponse = await client.ListPlaylistsAsync();
            var playlists = playlistResponse.Data.Items;
            var playlistInfo = (from playlist in playlists
                where playlist.ShareToken == playlistId
                select playlist).FirstOrDefault();
            if (playlistInfo == null)
            {
                throw new Exception("Playlist not found, or is not a user playlist. Only playlists the user owns can be downloaded at present.");
            }
            var items = await client.ListTracksFromPlaylist(playlistInfo);
            return new Playlist
            {
                Id = playlistInfo.Id,
                Title = playlistInfo.Name,
                Tracks = (from track in items where track != null select track.ToAthameTrack()).ToList()
            };

        }

        public override PagedMethod<Track> GetPlaylistItems(string playlistId, int itemsPerPage)
        {
            throw new NotImplementedException();
        }

        public override UrlParseResult ParseUrl(Uri url)
        {
            if (url.Host != GooglePlayHost)
            {
                return null;
            }
            var hashParts = url.Fragment.Split('/');

            if (hashParts.Length <= 2)
            {
                return null;
            }
            var type = hashParts[1];
            var id = hashParts[2];
            var result = new UrlParseResult { Id = id, Type = MediaType.Unknown, OriginalUri = url };
            switch (type)
            {
                case "album":
                    result.Type = MediaType.Album;
                    break;

                case "artist":
                    result.Type = MediaType.Artist;
                    break;

                // Will auto-playlists actually be interchangeable with user-generated playlists?
                case "pl":
                case "ap":
                    result.Type = MediaType.Playlist;
                    break;

                default:
                    result.Type = MediaType.Unknown;
                    break;
            }
            return result;
        }

        public override SearchResult Search(string searchText, MediaType typesToRetrieve, int itemsPerPage)
        {
            throw new NotImplementedException();
        }

        public override Task<Artist> GetArtistInfoAsync(string artistId)
        {
            throw new NotImplementedException();
        }

        public override PagedMethod<Track> GetArtistTopTracks(string artistId, int itemsPerPage)
        {
            throw new NotImplementedException();
        }

        public override PagedMethod<Album> GetArtistAlbums(string artistId, int itemsPerPage)
        {
            throw new NotImplementedException();
        }

        public override async Task<Album> GetAlbumAsync(string albumId, bool withTracks)
        {
            try
            {
                // Album should always have tracks
                var album = await client.GetAlbumAsync(albumId, includeDescription: false);
                return album.ToAthameAlbum();
            }
            catch (HttpRequestException ex)
            {
                // Just uhhhh
                throw new ResourceNotFoundException(ex.Message);
            }
        }

        public override async Task<Track> GetTrackAsync(string trackId)
        {
            var track = await client.GetTrackAsync(trackId);
            return track.ToAthameTrack();
        }

        public void Reset()
        {
            client = new MobileClient();

        }

        public PagedMethod<Track> GetUserSavedTracks(int itemsPerPage)
        {
            throw new NotImplementedException();
        }

        public PagedMethod<Artist> GetUserSavedArtists(int itemsPerPage)
        {
            throw new NotImplementedException();
        }

        public PagedMethod<Album> GetUserSavedAlbums(int itemsPerPage)
        {
            throw new NotImplementedException();
        }

        public PagedMethod<Playlist> GetUserSavedPlaylists(int itemsPerPage)
        {
            throw new NotImplementedException();
        }

        public AccountInfo Account { get; private set; }

        public bool IsAuthenticated => client.Session != null && client.Session.IsAuthenticated;

        public override Control GetSettingsControl()
        {
            return new PlayMusicSettingsControl(settings);
        }

        public override object Settings
        {
            get
            {
                return settings;
            }
            set
            {
                settings = (PlayMusicServiceSettings)value ?? new PlayMusicServiceSettings();
                Account = new AccountInfo
                {
                    DisplayId = settings.Email
                };
                if (settings.DisplayName != null) Account.DisplayName = settings.DisplayName;
            }
        }

        public override Uri[] BaseUri => new[] { new Uri("http://" + GooglePlayHost), new Uri("https://" + GooglePlayHost) };




        public override void Init(AthameApplication application, PluginContext pluginContext)
        {

        }

        public async Task<bool> AuthenticateAsync(string username, string password, bool rememberUser)
        {
            if (!await client.LoginAsync(username, password))
            {
                return false;
            }
            Account = new AccountInfo
            {
                DisplayId = username,
                DisplayName = client.Session.FirstName + " " + client.Session.LastName
            };
            // ReSharper disable once InvertIf
            if (rememberUser)
            {
                settings.SessionToken = client.Session.MasterToken;
                settings.Email = username;
                settings.DisplayName = Account.DisplayName;

            }
            return true;
        }

        public string SignInHelpText
            =>
            "Enter your Google account email and password. If you use two-factor authentication, you must set an app password:"
            ;

        public IReadOnlyCollection<SignInLink> SignInLinks => new[]
        {
            new SignInLink
            {
                DisplayName = "Set an app password",
                Link = new Uri("https://security.google.com/settings/security/apppasswords")
            },
            new SignInLink
            {
                DisplayName = "Forgot your password?",
                Link = new Uri("https://accounts.google.com/signin/recovery")
            }
        };

        public bool HasSavedSession => settings.SessionToken != null;

        public async Task<bool> RestoreAsync()
        {
            if (settings.SessionToken == null) return false;
            if (!await client.LoginWithToken(settings.Email, settings.SessionToken)) return false;

            Account = new AccountInfo
            {
                DisplayId = settings.Email,
                DisplayName = client.Session.FirstName + " " + client.Session.LastName
            };
            return true;
        }
    }
}
