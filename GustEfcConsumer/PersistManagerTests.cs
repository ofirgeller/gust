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
        [Test]
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

        [Test]
        public void PersistManager_Test()
        {
            var uut = new PersistManager<BloggerContext>();

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

            var blogs = uut.Context.Blogs.ToList();

            saveResult.KeyMappings.Count.Should().Be(2);

        }
    }
}
