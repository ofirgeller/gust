using FluentAssertions;
using Gust.Metadata;
using GustEfcConsumer.Model;
using NUnit.Framework;
using System;
using System.Linq;

namespace GustEfcConsumer
{
    [TestFixture]
    public class MetadataTests
    {
        MetadataExtractor.Metadata _metadata;
        string _metadataAsJson;

        [OneTimeSetUp]
        public void Setup()
        {
            var ctx = BloggerContext.CreateWithSqliteProvider("EFPersistManager");
            _metadata = MetadataExtractor.GetMetadata(ctx);
            _metadataAsJson = MetadataSerielizer.ToJson(_metadata, true);
            Console.WriteLine(_metadataAsJson);
        }

        [Test]
        public void Metadata_Test()
        {
            _metadata.StructuralTypes.Count.Should().Be(5);
            _metadata.ResourceEntityTypeMap.Count.Should().Be(5);

            /// ResourceEntityTypeMap maps backend entity types to backend endpoints and breeze expects both keys and 
            /// values to be PascalCase
            _metadata.ResourceEntityTypeMap.Keys.Should().OnlyContain(k => char.IsUpper(k.First()));
            _metadata.ResourceEntityTypeMap.Values.Should().OnlyContain(k => char.IsUpper(k.First()));

            var associationNames = _metadata.StructuralTypes
                .SelectMany(t => t.NavigationProperties.Select(np => np.AssociationName)).ToList();

            var associationNameGroups = associationNames.GroupBy(an => an).ToList();

            foreach (var group in associationNameGroups)
            {
                group.Count().Should().Be(2, $"expected group { group.First()} to appear two times");
            }

        }

        [Test]
        public void Metadata_Print_Test()
        {
            Console.WriteLine(_metadataAsJson);
        }

    }
}
