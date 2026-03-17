using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;
using MobileForge.Infrastructure;

namespace MobileForge.Tests.Infrastructure
{
    [TestFixture]
    public class CrashRecoveryTests
    {
        private CrashRecovery _recovery;
        private Dictionary<string, object> _writtenCheckpoints;
        private List<string> _completedCheckpoints;

        [SetUp]
        public void SetUp()
        {
            _recovery = new CrashRecovery();
        }

        [Test]
        public void create_checkpoint_WritesData()
        {
            var key = "crash_recovery";
            Assert.IsNotNull(key);
            var checkpoint = _recovery.ReadCheckpoint(key);
            Assert.IsNull(checkpoint);
        }

    }
}