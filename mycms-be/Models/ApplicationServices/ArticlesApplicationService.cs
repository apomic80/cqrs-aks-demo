using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.ServiceBus;
using mycms_shared.Entities;
using mycms_shared.Infrastructure;
using mycms.Models.ViewModels.Articles;
using mycms_shared.Events;

namespace mycms.Models.ApplicationServices
{
    public class ArticlesApplicationService : IArticlesApplicationService
    {
        private readonly IRepository<Article, int> repository = null;
        private readonly ITopicClient topicClient = null;

        public ArticlesApplicationService(
            IRepository<Article, int> repository,
            ITopicClient topicClient)
        {
            this.repository = repository;
            this.topicClient = topicClient;
        }
        
        public IEnumerable<ArticleListItemViewModel> GetAll()
        {
            return this.repository.GetAll()
                .Select(x => new ArticleListItemViewModel() 
                {
                    Id = x.Id,
                    Title = x.Title,
                    Author = x.Author
                }).ToList();
        }

        public ArticleViewModel Get(int id)
        {
            var article = this.repository.Get(id);
            return new ArticleViewModel() 
            {
                Id = article.Id,
                Title = article.Title,
                Subtitle = article.Subtitle,
                Content = article.Content,
                Author = article.Author
            };
        }

        public void Create(ArticleViewModel model)
        {
            var article = this.getArticle(model);
            this.repository.Create(article);
            this.repository.SaveChanges();
            this.raiseEvent(CRUDOperation.CREATE, article);
        }

        public void Update(ArticleViewModel model)
        {
            var article = this.getArticle(model);
            this.repository.Update(article);
            this.repository.SaveChanges();
            this.raiseEvent(CRUDOperation.UPDATE, article);
        }

        public void Delete(ArticleViewModel model)
        {
            var article = this.getArticle(model);
            this.repository.Delete(article);
            this.repository.SaveChanges();
            this.raiseEvent(CRUDOperation.DELETE, article);
        }

        private Article getArticle(ArticleViewModel model)
        {
            return new Article()
            {
                Id = model.Id,
                Title = model.Title,
                Subtitle = model.Subtitle,
                Content = model.Content,
                Author = model.Author
            };
        }

        private void raiseEvent(CRUDOperation operation, Article entity)
        {
            var crudEvent = new ArticleCRUDEvent() 
            {
                Entity = entity,
                Operation = operation
            };
            var json = JsonSerializer.Serialize(crudEvent);
            this.topicClient.SendAsync(new Message(Encoding.UTF8.GetBytes(json)));
        }
    }
}