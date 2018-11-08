﻿using FluentAssertions;
using Gust;
using Gust.Keys;
using GustEfcConsumer.Model;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using NodaTime.Serialization.JsonNet;

namespace GustEfcConsumer.Tests
{
    public class PersistManagerWithNoda : PersistManager<BloggerContextPg>
    {
        public override BloggerContextPg CreateContext()
        {
            return BloggerContextPg.CreateWithNpgsql();
        }

        protected override JsonSerializerSettings  GetSerializerSettings()
        {
            var settings = base.GetSerializerSettings();
            settings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
            return settings;
        }
    }

    /// <summary>
    /// Notice that the tests that depend on the data in the database (vs the scheme) 
    /// have an order attiribute, this allows us to not have to recreate the
    /// DB baseline state for each test
    /// </summary>
    [TestFixture]
    public class PersistManagerTests
    {
        PersistManager<BloggerContextPg> UUT;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            /// make sure w have a fresh  and empty DB .  
            using (var setupCtx = new BloggerContextPg())
            {
                setupCtx.Database.EnsureDeleted();
                setupCtx.Database.EnsureCreated();
            }

            UUT = new PersistManagerWithNoda();
        }

        SaveResult InsertTestDataBaseLineIntoDb(PersistManager<BloggerContextPg> uut)
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
                Title = "this is the title",
                CreatedAt = Instant.FromUtc(2002, 10, 8, 6, 4)
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
        public void GetEntitySetsInfo_Test()
        {
            var entitySetsInfo = UUT.GetEntitySetsInfo();

            entitySetsInfo.Should().HaveCount(6).And.OnlyHaveUniqueItems(esi => esi.JsName);
            entitySetsInfo.Should().HaveCount(6).And.OnlyHaveUniqueItems(esi => esi.ClrType);
            entitySetsInfo.Should().HaveCount(6).And.OnlyHaveUniqueItems(esi => esi.EntityType);
        }

        [Test]
        public void PersistManager_EntityInfoFromJsonToken_UnmappedProperties()
        {
            var entityJson = @"         
       {
    ""Id"": -1,
    ""Url"": ""https://example.com"",
    ""__unmapped"": {
                ""unmappedPropertyName"": ""unmapped value""
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

            var entityInfo = UUT.EntityInfoFromJsonToken(entityJToken);

            entityInfo.UnmappedValuesMap.Should().HaveCount(1);
            entityInfo.UnmappedValuesMap.Should().ContainKey("unmappedPropertyName")
                .And.ContainValue("unmapped value");
        }

        [Test]
        public void PersistManager_EntityInfoFromJsonToken_OriginalValues()
        {
            var entityJson = @"         
               {
            ""Id"": -1,
            ""Url"": ""https://example.com"",
            ""Subject"": ""sports"",
            ""entityAspect"": {
                ""entityTypeName"": ""Blog:#GustEfcConsumer.Model"",
                ""defaultResourceName"": ""Blogs"",
                ""entityState"": ""Modified "",
                ""originalValuesMap"": {
                    ""Url"": ""https://notExample.com"",
                    ""Subject"": ""Lifestyle""
                }
                    }
                }
                 ";

            var entityJToken = JToken.Parse(entityJson);

            var entityInfo = UUT.EntityInfoFromJsonToken(entityJToken);

            entityInfo.OriginalValuesMap.Should().HaveCount(2);

            entityInfo.OriginalValuesMap.Should().ContainKeys("Url", "Subject")
                .And.ContainValues("https://notExample.com", "Lifestyle");
        }

        [Test, Order(0)]
        public void PersistManager_Add_DependentEntity()
        {
            var saveResult = InsertTestDataBaseLineIntoDb(UUT);
            var blogs = UUT.Context.Blogs.ToList();
            blogs.Single().Posts.Should().HaveCount(1);
            saveResult.KeyMappings.Count.Should().Be(2);
        }

        [Test, Order(1)]
        public void PersistManager_Delete_DependentEntity()
        {
            var uut = new PersistManager<BloggerContextPg>();

            var ctx = new BloggerContextPg();

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

            var saveResultOfDelete = uut.SaveChanges(parsedSaveBundle.ToString());

            var postDeleteBlogs = uut.Context.Blogs.ToList();
            postDeleteBlogs.Count.Should().Be(0);
            saveResultOfDelete.DeletedKeys.Count.Should().Be(blogs.Count + posts.Count);
        }

        public class InheritingPersistManager : PersistManager<BloggerContextPg>
        {
            protected override void AfterSaveEntities(Dictionary<Type, List<EntityInfo>> saveMap, Dictionary<(Type, object), KeyMapping> keyMappings, List<EntityKey> deletedKeys)
            {
                throw new Exception();
            }
        }

        [Test, Order(1)]
        public void PersistManager_WhenAfterSaveEntitiesthrows_ChangesAreRolledBack()
        {
            var uut = new InheritingPersistManager();

            var blogUrl = "www.shouldNotBEsAVED.com";

            var blog = new Blog
            {
                Id = -1,
                Url = blogUrl
            };

            var blogEntityAspect = new EntityAspect(blog, EntityState.Added);

            var saveBundle0 = new ClientSaveBundle();

            saveBundle0.AddEntity(blogEntityAspect);

            var parsedSaveBundle = JObject.Parse(saveBundle0.ToJson());

            uut.Invoking((m) => m.SaveChanges(parsedSaveBundle.ToString()))
               .Should().Throw<Exception>();

            var ctx = new BloggerContextPg();
            ctx.Blogs.FirstOrDefault(b => b.Url == blogUrl).Should().BeNull();
        }

    }
}
