using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Infrastructure;

namespace MobileForge.Tests.Infrastructure
{
    [TestFixture]
    public class SaveMigratorTests
    {
        private SaveMigrator _migrator;

        [SetUp]
        public void SetUp()
        {
            _migrator = new SaveMigrator();
        }

        [Test]
        public void Migrate_NoSteps_ReturnsOriginal()
        {
            var data = new Dictionary<string, object> { { "key", "value" } };
            var result = _migrator.Migrate(data, 1, 1);
            Assert.AreSame(data, result);
        }

        [Test]
        public void Migrate_SingleStep_TransformsData()
        {
            _migrator.Register(1, 2, d =>
            {
                d["migrated"] = true;
                return d;
            });

            var data = new Dictionary<string, object> { { "key", "value" } };
            var result = _migrator.Migrate(data, 1, 2);
            Assert.IsTrue((bool)result["migrated"]);
        }
    }
}
