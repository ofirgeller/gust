using FluentAssertions;
using GustEfc.Metadata;
using GustEfcConsumer.Model;
using NUnit.Framework;
using System;

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
        }

        [Test]
        public void Metadata_Print_Test()
        {
            Console.WriteLine(_metadataAsJson);
        }

    }
}
