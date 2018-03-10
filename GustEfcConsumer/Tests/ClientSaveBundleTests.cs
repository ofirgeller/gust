using FluentAssertions;
using GustEfcConsumer.Model;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace GustEfcConsumer.Tests
{
    /// <summary>
    /// Tests to ensure out client bundle class used for testing is itself correct
    /// </summary>
    [TestFixture]
    public class ClientSaveBundleTests
    {
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
    }
}
