using GoogleMusicApi.Structure.Enums;

namespace AthamePlugin.GooglePlayMusic
{
    public class PlayMusicServiceSettings
    {
        public StreamQuality StreamQuality { get; set; }

        public string Email { get; set; }
        public string SessionToken { get; set; }

        public PlayMusicServiceSettings()
        {
            StreamQuality = StreamQuality.High;
        }
    }
}
