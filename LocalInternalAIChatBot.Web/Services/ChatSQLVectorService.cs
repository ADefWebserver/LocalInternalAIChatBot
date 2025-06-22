#nullable disable
using LocalInternalAIChatBot.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;

namespace LocalInternalAIChatBot
{
    public class ChatSQLVectorService
    {
        private readonly LocalInternalAIChatBotContext _context;
        public ChatSQLVectorService(
            LocalInternalAIChatBotContext context)
        {
            _context = context;
        }

        public async Task<List<Article>>
            GetArticlesAsync()
        {
            // Get Articles including ArticleDetail           
            return await _context.Articles
                .Include(a => a.ArticleDetails)
                .AsNoTracking().ToListAsync();
        }

        public async Task<Article> GetArticleByIdAsync(int id)
        {
            // Get Article including ArticleDetail
            return await _context.Articles
                .Include(a => a.ArticleDetails)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        // Add Article
        public async Task<Article> AddArticleAsync(Article article)
        {
            _context.Articles.Add(article);
            await _context.SaveChangesAsync();
            return article;
        }

        // Add ArticleDetail
        public async Task<ArticleDetail> AddArticleDetailAsync(
            ArticleDetail articleDetail, float[] embeddings)
        {
            _context.ArticleDetails.Add(articleDetail);

            await _context.SaveChangesAsync();

            // Insert all Embedding Vectors
            for (int i = 0; i < embeddings.Length; i++)
            {
                var embeddingVector = new ArticleVectorDatum
                {
                    ArticleDetailId = articleDetail.Id,
                    VectorValueId = i,
                    VectorValue = embeddings[i]
                };
                _context.ArticleVectorData.Add(embeddingVector);
            }

            await _context.SaveChangesAsync();

            return articleDetail;
        }

        // Delete Article
        public async Task<Article> DeleteArticleAsync(int id)
        {
            var article = await _context.Articles.FindAsync(id);
            _context.Articles.Remove(article);
            await _context.SaveChangesAsync();
            return article;
        }

        // GetSimilarContentArticles
        public List<SimilarContentArticlesResult>
            GetSimilarContentArticles(string vector)
        {
            return _context.SimilarContentArticles(vector).ToList();
        }
    }
}