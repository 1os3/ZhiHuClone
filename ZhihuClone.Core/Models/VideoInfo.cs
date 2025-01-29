namespace ZhihuClone.Core.Models
{
    public class VideoInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public long Duration { get; set; }
        public string Format { get; set; } = string.Empty;
        public long Bitrate { get; set; }
    }
} 