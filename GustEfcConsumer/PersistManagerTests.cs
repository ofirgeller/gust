using FluentAssertions;
using Gust;
using GustEfcConsumer.Model;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GustEfcConsumer
{
    [TestFixture]
    public class PersistManagerTests
    {
        [SetUp]
        public void GetEntitySetsInfo_Test()
        {
            var ctx = BloggerContext.CreateWithNpgsql();
            var entitySetsInfo = PersistManager<BloggerContext>.GetEntitySetsInfo(ctx);
        }

        [Test]
        public void ClientSaveBundle_EntityAndEntityAspectToJson_Test()
        {
            var blog = new Blog { Url = "www.example.com" };
            var blogEntityAspect = new EntityAspect(blog, EntityState.Added);

            {
                var uut = new ClientSaveBundle(pascalCase: false);
                var entityWithAspectAsJson = uut.EntityAndEntityAspectToJObject(blog, blogEntityAspect).ToString();
                entityWithAspectAsJson.Should().Contain("\"url\": \"www.example.com\",");
                Console.WriteLine(entityWithAspectAsJson);
            }

            {
                var uut = new ClientSaveBundle(pascalCase: true);
                var entityWithAspectAsJson = uut.EntityAndEntityAspectToJObject(blog, blogEntityAspect).ToString();
                entityWithAspectAsJson.Should().Contain("\"Url\": \"www.example.com\",");
            }
        }

        [Test]
        public void ClientSaveBundle_EntityAspect_ChangeValue_Test()
        {
            var originalUrl = "www.oldUrl.com";
            var newUrl = "www.newUrl.com";
            var blog = new Blog { Url = originalUrl };
            var blogEntityAspect = new EntityAspect(blog, EntityState.Added);

            blogEntityAspect.ChangeValue("Url", newUrl);
            blog.Url.Should().Be(newUrl);
            blogEntityAspect.OriginalValuesMap.Should()
                .Contain(KeyValuePair.Create("Url", originalUrl as object));
        }

        public SaveResult SaveBlogAndPost(PersistManager<BloggerContext> uut)
        {
            var blog = new Blog
            {
                Id = -1,
                Url = "www.example.com"
            };

            var blogEntityAspect = new EntityAspect(blog, EntityState.Added);

            var post = new Post
            {
                BlogId = -1,
                Content = "I am content",
                Id = -2,
                Title = "this is the title"
            };

            var postEntityAspect = new EntityAspect(post, EntityState.Added);

            var saveBundle0 = new ClientSaveBundle();

            saveBundle0.AddEntity(blogEntityAspect);
            saveBundle0.AddEntity(postEntityAspect);

            var parsedSaveBundle = JObject.Parse(saveBundle0.ToJson());

            var saveResult = uut.SaveChanges(parsedSaveBundle.ToString());

            return saveResult;
        }

        [Test]
        public void PersistManager_Test_Add_DependentEntity()
        {
            var uut = new PersistManager<BloggerContext>();
            var saveResult = SaveBlogAndPost(uut);
            var blogs = uut.Context.Blogs.ToList();
            saveResult.KeyMappings.Count.Should().Be(2);
        }


        [Test]
        public void PersistManager_Test_Delete_DependentEntity()
        {
            var uut1 = new PersistManager<BloggerContext>();
            var saveResult = SaveBlogAndPost(uut1);

            var uut2 = new PersistManager<BloggerContext>();


            var ctx = new BloggerContext();

            var blogs = ctx.Blogs.ToList();
            var posts = ctx.Posts.ToList();
            blogs.Count.Should().BeGreaterThan(0);
            posts.Count.Should().BeGreaterThan(0);

            var blogsToDelete = blogs.Select(b => new EntityAspect(b, EntityState.Deleted)).ToList();
            var postsToDelete = posts.Select(p => new EntityAspect(p, EntityState.Deleted)).ToList();

            var saveBundle0 = new ClientSaveBundle();

            blogsToDelete.ForEach(b => saveBundle0.AddEntity(b));
            postsToDelete.ForEach(p => saveBundle0.AddEntity(p));

            var parsedSaveBundle = JObject.Parse(saveBundle0.ToJson());

            var saveResultOfDelete = uut2.SaveChanges(parsedSaveBundle.ToString());

            var postDeleteBlogs = uut2.Context.Blogs.ToList();
            postDeleteBlogs.Count.Should().Be(0);
            saveResultOfDelete.DeletedKeys.Count.Should().Be(blogs.Count + posts.Count);
        }
    }
}
