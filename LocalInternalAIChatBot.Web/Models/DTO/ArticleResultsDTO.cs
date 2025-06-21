#nullable disable
namespace LocalInternalAIChatBot.Models.DTO
{
    public class ArticleResultsDTO
    {
        public string Article { get; set; }
        public int Sequence { get; set; }
        public string Contents { get; set; }
        public double Match { get; set; }
    }
}
