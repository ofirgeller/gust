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

        [Test]
        public void UnmappedProperties_WillBeAddedToDictionary()
        {
            var entityJson = @"         
       {
    ""Id"": -1,
    ""Accent"": null,
    ""Attribution"": null,
    ""DeletedAt"": null,
    ""Duration"": null,
    ""FileAfix"": ""wav"",
    ""Formats"": null,
    ""Gender"": ""masculine"",
    ""InternalComments"": null,
    ""IsARecording"": true,
    ""License"": null,
    ""MediaType"": ""Audio"",
    ""Path"": null,
    ""SourceId"": ""ccd9128e-0516-4cd9-b937-54c4e8358cdb"",
    ""StorageURL"": null,
    ""Tags"": null,
    ""__unmapped"": {
                ""FileBinary"": ""/9//3/A/8H/wf/C/8L/w//D/8T/xP/G/8b/yf/J/8z/zP/P/8//0f/R/9L/0v/T/9P/1P/U/9T/1P/U/9T/1P/U/9P/0//T/9P/0v/S/9L/""
    },
    ""entityAspect"": {
        ""entityTypeName"": ""Blog:#GustEfcConsumer.Model"",
        ""defaultResourceName"": ""Blogs"",
        ""entityState"": ""Added"",
        ""originalValuesMap"": { },
        ""autoGeneratedKey"": {
                    ""propertyName"": ""Id"",
            ""autoGeneratedKeyType"": ""Identity""
        }
            }
        }
";

            var entityJToken = JToken.Parse(entityJson);

            var uut = new PersistManager<BloggerContext>();

            var entityInfo = uut.EntityInfoFromJsonToken(entityJToken);

            entityInfo.UnmappedValuesMap.Should().HaveCount(1);

        }
    }
}
