using GustEfcConsumer.Model;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GustEfcConsumer
{
    [TestFixture]
    public class BlogerContextTests
    {
        [Test]
        public void GetEntityTypeSaveOrder_Test()
        {
            var ctx = BloggerContext.CreateWithNpgsql();
            var saveOrder = DecideSaveOrder(ctx);
            saveOrder = DecideSaveOrder(ctx);
            saveOrder = DecideSaveOrder(ctx);
            saveOrder = DecideSaveOrder(ctx);

            saveOrder.ForEach(t => Console.WriteLine(t));
        }

        /// <summary>
        /// Returns a list of entity type names where each entity only depends on entites that
        /// come before it on the list.
        /// </summary>
        static List<string> DecideSaveOrder(DbContext ctx)
        {
            var entityTypes = ctx.Model.GetEntityTypes();

            /// Entities are already ordered by name but since it's not documented we make sure that's true
            entityTypes = entityTypes.OrderBy(et => et.Name).ToList();

            var typesAndDependencies = entityTypes.Select(et =>
            {
                var dependencies = et.GetNavigations()
                                              .Where(n => n.IsDependentToPrincipal())
                                              .Select(i => i.GetTargetType().Name)
                                              .ToHashSet();

                return (et, dependencies);

            }).ToList();

            var saveOrder = new List<string>();

            /// Will holt or throw since we are removing an item each time or throwing if we did not
            /// find an item
            while (typesAndDependencies.Count > 0)
            {
                var nextToSave = typesAndDependencies.First((t) =>
                {
                    return t.dependencies.Except(saveOrder).Count() == 0;
                });

                typesAndDependencies.Remove(nextToSave);
                saveOrder.Add(nextToSave.et.Name);
            }

            return saveOrder;
        }

    }
}
