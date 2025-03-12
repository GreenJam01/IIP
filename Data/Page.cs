namespace Data
{
    public class Page 
    {
        public string? Url { get; set; }

        public string? Title { get; set; }
        public string? Text { get; set; }

        public List<string> Category { get; set; } = new List<string>();

        public List<string> Images { get; set; } = new List<string>();

        public List<float> ImageEmbedding { get; set; }
        public List<float> TextEmbedding { get; set; }
    }
}
