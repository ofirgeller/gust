using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Gust.PersistEFC
{
    /// <summary>
    /// This extractor creates metadata that looks like the response the EF6 code returns
    /// We known that schame will work for sure.
    /// </summary>
    public static class MetadataExtractor1
    {
        public class PropertyRef
        {
            public string Name { get; set; }
        }

        /// <summary>
        ///  Empty interface used becuse the key can be a single property or an array of properties
        /// </summary>
        public interface IKeyMetadata
        {

        }

        public class SimpleKeyMetadata : IKeyMetadata
        {
            public PropertyRef PropertyRef { get; set; }
        }

        public class MultiKeyMetadata : IKeyMetadata
        {
            public PropertyRef[] PropertyRef { get; set; }
        }

        public class PropertyMetadata
        {
            public string Name { get; set; }

            public string Type { get; set; }

            /// <summary>
            /// Can this property be null. can be omitted if false.
            /// </summary>
            public bool? Nullable { get; set; }

            public int? MaxLength { get; set; }
        }

        public class NavigationPropertyMetadata
        {
            public string Name { get; set; }
            public string Relationship { get; set; }
            public string FromRole { get; set; }
            public string ToRole { get; set; }
            //"name": "Aspects",
            // "relationship": "Self.Fact_Aspects",
            // "fromRole": "Fact_Aspects_Source",
            // "toRole": "Fact_Aspects_Target"
        }

        public class TypeMetadata
        {
            public string Name { get; set; }

            public IKeyMetadata Key { get; set; }

            /// <summary>
            /// Singular name for compatability with breeze js.
            /// </summary>
            public List<PropertyMetadata> Property { get; set; }

            /// <summary>
            /// Singular name for compatability with breeze js.
            /// Breeze was using a simple object when there was only one navigation property
            /// but I think we can always have an array.
            /// </summary>
            public List<NavigationPropertyMetadata> NavigationProperty { get; set; }
        }

        public class Metadata
        {
            public string Alias => "Self";

            public string Namespace { get; set; }
            /// <summary>
            /// Singular name for compatability with breeze js.
            /// </summary>
            public List<TypeMetadata> EntityType { get; set; }
        }

        static string ShortNameFromlongName(string longName)
        {
            return longName.Split('.').Last();
        }

        public static IKeyMetadata GetKeyMetadata(IKey key)
        {
            if (key.Properties.Count == 1)
            {
                return new SimpleKeyMetadata
                {
                    PropertyRef = new PropertyRef
                    {
                        Name = key.Properties.First().Name
                    }
                };
            }

            var propertyRefs = key.Properties
                .Select(p => new PropertyRef { Name = p.Name })
                .ToArray();

            return new MultiKeyMetadata
            {
                PropertyRef = propertyRefs
            };
        }

        public static PropertyMetadata GetPropertyMetadata(IProperty prop)
        {
            return new PropertyMetadata
            {
                Name = prop.Name,
                Nullable = prop.IsNullable ? true : default(bool?),
                Type = ShortNameFromlongName(prop.ClrType.Name)
            };
        }

        public static TypeMetadata GetTypeMetadata(IEntityType type)
        {
            var name = ShortNameFromlongName(type.Name);

            var propertiesMetadata = type.GetProperties().Select(p => GetPropertyMetadata(p)).ToList();

            var pk = type.FindPrimaryKey();
            var keyMetaData = GetKeyMetadata(pk);

            type.GetNavigations().ToList().Select(n =>
            {
                return new NavigationPropertyMetadata
                {
                    Name = n.Name
                };
            });

            return new TypeMetadata
            {
                Name = name,
                Key = keyMetaData,
                Property = propertiesMetadata
            };
        }

        public static List<TypeMetadata> GetTypesMetadata(IModel model)
        {
            var entityTypes = model.GetEntityTypes().ToList();
            var typesMetadata = entityTypes.Select(t => GetTypeMetadata(t)).ToList();
            return typesMetadata;
        }

        public static List<TypeMetadata> GetTypesMetadata(DbContext ctx)
        {
            return GetTypesMetadata(ctx.Model);
        }

        public static Metadata GetMetadata(DbContext ctx)
        {
            return new Metadata
            {
                EntityType = GetTypesMetadata(ctx),

                Namespace = ctx.GetType().Namespace
            };
        }
    }
}
