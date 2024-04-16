namespace STOTool.Class
{
    public class NewsInfo
    {
        public string? Title { get; set; }
        public string? ImageUrl { get; set; }
        
        public string? NewsLink { get; set; }
        
        public override string ToString()
        {
            return Title + " " + NewsLink;
        }
    }
}