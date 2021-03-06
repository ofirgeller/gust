﻿using Gust.Keys;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using static Gust.EntityNameUtil;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gust.Metadata
{
    /// <summary>
    /// This extractor creates metadata with the schame the js breeze client uses. the docs say
    /// this schame will also work if the server returns it. if it does not we can still import it
    /// by hand and that was tested and works.
    /// The subclasses are only meant to be used as DTO to be serialized and sent to the frontend.
    /// </summary>
    public static class MetadataExtractor
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

        public class Validator
        {
            /// <summary>
            ///  A name of a predefined breeze frontend validator. seems to be decided based on both
            ///  the type of the property and relevent attribuates it has.
            ///  Examples: "required", "maxLength", "bool", "int", "string", "int32"
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// If the validator name is MaxLength this property should specify the length
            /// </summary>
            public int? MaxLength { get; set; }
        }

        public class PropertyMetadata
        {
            /// <summary>
            /// Example: "id" "age"
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Should be ommitted if not true
            /// </summary>
            public bool? IsPartOfKey { get; set; }

            /// <summary>
            /// Short data type name.
            /// examples: Int16, Int32, String,
            /// </summary>
            public string DataType { get; set; }

            /// <summary>
            /// Not sure how much this is needed or how breeze decides what it is. maybe just the default
            /// clr value if the type is not nullable, so all numbers are 0, strings are an empty string
            /// and booleans are false
            /// </summary>
            public object DefaultValue { get; set; }

            /// <summary>
            /// defaults to true on the client
            /// </summary>
            public bool? IsNullable { get; set; }

            public int? MaxLength { get; set; }

            public Validator[] Validators { get; set; }

            /// <summary>
            /// Example: "Edm.Self.LearnSetId"
            /// When the property type is an enum on the server the DataType needs to be "String"
            /// </summary>
            public string EnumType { get; set; }
        }

        public class NavigationPropertyMetadata
        {
            /// <summary>
            /// Name of the navigation property (pascalCase)
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Long name of the entity type this property navigates to (PascalCase)
            /// Example "Fact:#LZDataBase.Model"
            /// </summary>
            public string EntityTypeName { get; set; }

            /// <summary>
            /// Is the navigation property itself a scalar value. 
            /// In a one to many reletionship the dependent has a scalar prop. in all other cases
            /// the property is not scalar
            /// </summary>
            public bool IsScalar { get; set; }

            /// <summary>
            /// A name for the reletionship, seems to be arbitrery as long as the two entities use the same name.
            /// Example: the relationship between facts and aspects should be named "Fact_Aspects" on both sides
            /// </summary>
            public string AssociationName { get; set; }

            /// <summary>
            /// The names of the properties that indicate the reletionship to the principal entity. (camelCase)
            /// if this entity is the principel this should be omitted 
            /// </summary>
            public string[] ForeignKeyNames { get; set; }

            /// <summary>
            /// The names of the properties indicating the connection on the related entity.
            /// If the entity is not the principal this should be omitted
            /// </summary>
            public string[] InvForeignKeyNames { get; set; }
        }

        public class TypeMetadata
        {
            public string ShortName { get; set; }

            public string Namespace { get; set; }

            /// <summary>
            /// Identity means this key will be created by the database, None means the client need to set it
            /// </summary>
            public AutoGeneratedKeyType AutoGeneratedKeyType { get; set; }

            /// <summary>
            /// Indicates the name of the endpoint that would return a collection of this type of entity.
            /// can also be the name of a collection of such entity on the client (after the client camelCases the name)
            /// Example for the Aspect type it's "Aspects"
            /// </summary>
            public string DefaultResourceName { get; set; }

            public PropertyMetadata[] DataProperties { get; set; }

            public NavigationPropertyMetadata[] NavigationProperties { get; set; }
        }

        public class Metadata
        {
            /// <summary>
            /// Example Key-value pair: "Aspects": "Aspect:#LZDataBase.Model",
            /// </summary>
            public Dictionary<string, string> ResourceEntityTypeMap { get; set; }

            public List<TypeMetadata> StructuralTypes { get; set; }
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
            var type = prop.ClrType;
            var nullable = prop.IsNullable;
            var isPartOfKey = prop.IsKey();

            var underliningType = Nullable.GetUnderlyingType(type);

            if (underliningType != null)
            {
                type = underliningType;
            }

            var isEnum = type.IsEnum;
            var numeric = TypeFns.IsNumericType(type);

            var dataType = isEnum ? "String" : ShortTypeNameFromLongName(type.Name);

            object defaultValue = null;
            if (nullable != true && numeric)
            {
                defaultValue = 0;
            }

            if (nullable != true && type == typeof(string))
            {
                defaultValue = "";
            }

            return new PropertyMetadata
            {
                Name = ToCamelCase(prop.Name),
                EnumType = isEnum ? type.FullName : null,
                IsNullable = nullable,
                DefaultValue = defaultValue,
                DataType = dataType,
                IsPartOfKey = isPartOfKey
            };
        }

        public static NavigationPropertyMetadata[] GetNavigationPropertiesMetadata(IEntityType entityType)
        {
            var dataModelTypeName = entityType.ClrType.Namespace;

            return entityType.GetNavigations().ToList().Select(n =>
              {
                  var name = n.Name;

                  var targetEntityType = n.GetTargetType();
                  var isDependent = n.IsDependentToPrincipal();

                  string[] foreignKeyNames = null;
                  string[] invForeignKeyNames = null;

                  var targetEntityName = ShortTypeNameFromLongName(targetEntityType.Name);
                  var decleringEntityName = ShortTypeNameFromLongName(n.DeclaringEntityType.Name);
                  var propertyNameOfDependent = isDependent ? n.Name : n.FindInverse()?.Name;

                  if (propertyNameOfDependent == null)
                  {
                      throw new Exception
                      ($"unable to find the inverse navigation property of the {n.Name} navigation property of of type {entityType.Name}");
                  }

                  var associationName = string.Empty;

                  if (isDependent)
                  {
                      foreignKeyNames = n.ForeignKey.Properties.Select(p => ToCamelCase(p.Name)).ToArray();
                      associationName = targetEntityName + "_" + decleringEntityName + "__" + propertyNameOfDependent;
                  }
                  else
                  {
                      invForeignKeyNames = n.FindInverse().ForeignKey.Properties.Select(p => ToCamelCase(p.Name)).ToArray();
                      associationName = decleringEntityName + "_" + targetEntityName + "__" + propertyNameOfDependent;
                  }

                  var baseAndTargetType = new[] { targetEntityName, decleringEntityName }
                  .OrderBy(i => i).ToArray();

                  return new NavigationPropertyMetadata
                  {
                      Name = ToCamelCase(n.Name),
                      EntityTypeName = $"{ ShortTypeNameFromLongName(targetEntityType.Name)}:#{dataModelTypeName}",
                      IsScalar = isDependent,
                      ForeignKeyNames = foreignKeyNames,
                      InvForeignKeyNames = invForeignKeyNames,
                      AssociationName = associationName

                  };
              }).ToArray();
        }

        public static TypeMetadata GetTypeMetadata(IEntityType type)
        {
            var name = ShortTypeNameFromLongName(type.Name);

            var properties = type.GetProperties().Select(p => GetPropertyMetadata(p)).ToArray();
            var navProperties = GetNavigationPropertiesMetadata(type);

            var pk = type.FindPrimaryKey();
            var keyMetaData = GetKeyMetadata(pk);

            var keysAreAllForeignkeysOrMarkedNone = pk.Properties.All((p) =>
            {
                var markedAsNone = false;

                if (Attribute.GetCustomAttribute(p.PropertyInfo, typeof(DatabaseGeneratedAttribute)) is DatabaseGeneratedAttribute attrib)
                {
                    markedAsNone = attrib.DatabaseGeneratedOption == DatabaseGeneratedOption.None;
                }

                return p.IsForeignKey() || markedAsNone;
            });

            Console.WriteLine();

            return new TypeMetadata
            {
                ShortName = name,
                Namespace = type.ClrType.Namespace,
                AutoGeneratedKeyType = keysAreAllForeignkeysOrMarkedNone ? AutoGeneratedKeyType.None : AutoGeneratedKeyType.Identity,
                DefaultResourceName = Pluralize(name),

                DataProperties = properties,
                NavigationProperties = navProperties

            };
        }

        public static List<TypeMetadata> GetTypesMetadata(List<IEntityType> entityTypes)
        {
            var typesMetadata = entityTypes.Select(t => GetTypeMetadata(t)).ToList();
            return typesMetadata;
        }

        public static Metadata GetMetadata(DbContext ctx)
        {
            var entityTypes = ctx.Model.GetEntityTypes().ToList();

            var resourceEntityTypeMap = entityTypes
                .Select(et => et.ClrType)
                .ToDictionary(i => Pluralize(i.Name), JsTypeNameFromType);

            var typesMetadate = GetTypesMetadata(entityTypes);

            return new Metadata
            {
                StructuralTypes = typesMetadate,
                ResourceEntityTypeMap = resourceEntityTypeMap
            };

        }
    }
}
