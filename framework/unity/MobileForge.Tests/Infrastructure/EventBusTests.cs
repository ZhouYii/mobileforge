using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Infrastructure;

namespace MobileForge.Tests.Infrastructure
{
    [TestFixture]
    public class EventBusTests
    {
        private EventBus _bus;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
        }

        [Test]
        public void Subscribe_And_Emit_Calls_Callback()
        {
            int callCount = 0;
            Dictionary<string, object> received = null;

            _bus.Subscribe("test_event", payload =>
            {
                callCount++;
                received = payload;
            });

            _bus.Emit("test_event", new Dictionary<string, object> { { "key", "value" } });

            Assert.AreEqual(1, callCount);
            Assert.IsNotNull(received);
            Assert.AreEqual("value", received["key"]);
        }

        [Test]
        public void Multiple_Subscribers_All_Called()
        {
            int firstCount = 0;
            int secondCount = 0;

            _bus.Subscribe("multi", _ => firstCount++);
            _bus.Subscribe("multi", _ => secondCount++);

            _bus.Emit("multi", new Dictionary<string, object>());

            Assert.AreEqual(1, firstCount);
            Assert.AreEqual(1, secondCount);
        }

        [Test]
        public void Unsubscribe_Prevents_Callback()
        {
            int callCount = 0;
            void Callback(Dictionary<string, object> _) => callCount++;

            _bus.Subscribe("unsub_event", Callback);
            _bus.Unsubscribe("unsub_event", Callback);
            _bus.Emit("unsub_event", new Dictionary<string, object> { { "should", "not arrive" } });

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Emit_With_No_Subscribers_Does_Not_Throw()
        {
            Assert.DoesNotThrow(() =>
            {
                _bus.Emit("nobody_listening", new Dictionary<string, object> { { "data", 42 } });
            });
        }

        [Test]
        public void SubscriberCount_Returns_Correct_Count()
        {
            Assert.AreEqual(0, _bus.SubscriberCount("counted"));

            void Cb1(Dictionary<string, object> _) { }
            void Cb2(Dictionary<string, object> _) { }

            _bus.Subscribe("counted", Cb1);
            Assert.AreEqual(1, _bus.SubscriberCount("counted"));

            _bus.Subscribe("counted", Cb2);
            Assert.AreEqual(2, _bus.SubscriberCount("counted"));

            _bus.Unsubscribe("counted", Cb1);
            Assert.AreEqual(1, _bus.SubscriberCount("counted"));

            _bus.Unsubscribe("counted", Cb2);
            Assert.AreEqual(0, _bus.SubscriberCount("counted"));
        }

        [Test]
        public void ClearAll_Removes_All_Subscriptions()
        {
            _bus.Subscribe("a", _ => { });
            _bus.Subscribe("b", _ => { });

            _bus.ClearAll();

            Assert.AreEqual(0, _bus.SubscriberCount("a"));
            Assert.AreEqual(0, _bus.SubscriberCount("b"));
        }

        [Test]
        public void Duplicate_Subscribe_Only_Fires_Once()
        {
            int callCount = 0;
            void Callback(Dictionary<string, object> _) => callCount++;

            _bus.Subscribe("dup", Callback);
            _bus.Subscribe("dup", Callback); // duplicate

            Assert.AreEqual(1, _bus.SubscriberCount("dup"));

            _bus.Emit("dup", new Dictionary<string, object>());

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Different_Events_Are_Independent()
        {
            int countA = 0;
            int countB = 0;

            _bus.Subscribe("event_a", _ => countA++);
            _bus.Subscribe("event_b", _ => countB++);

            _bus.Emit("event_a", new Dictionary<string, object>());

            Assert.AreEqual(1, countA);
            Assert.AreEqual(0, countB);
        }

        [Test]
        public void Unsubscribe_During_Emit_Does_Not_Crash()
        {
            int secondCallCount = 0;
            void SelfUnsubscribe(Dictionary<string, object> _) =>
                _bus.Unsubscribe("self_unsub", SelfUnsubscribe);
            void SecondCallback(Dictionary<string, object> _) => secondCallCount++;

            _bus.Subscribe("self_unsub", SelfUnsubscribe);
            _bus.Subscribe("self_unsub", SecondCallback);

            Assert.DoesNotThrow(() =>
            {
                _bus.Emit("self_unsub", new Dictionary<string, object>());
            });

            // Second subscriber should still be called because emit iterates a copy
            Assert.AreEqual(1, secondCallCount);
            // Self-unsubscriber should have been removed
            Assert.AreEqual(1, _bus.SubscriberCount("self_unsub"));
        }

        [Test]
        public void Emit_Default_Payload_Is_Empty_Dict()
        {
            Dictionary<string, object> received = null;

            _bus.Subscribe("empty_payload", payload => received = payload);
            _bus.Emit("empty_payload"); // no payload argument

            Assert.IsNotNull(received);
            Assert.AreEqual(0, received.Count);
        }

        [Test]
        public void Payload_Data_Is_Passed_Correctly()
        {
            Dictionary<string, object> received = null;

            _bus.Subscribe("payload_test", payload => received = payload);

            var sent = new Dictionary<string, object>
            {
                { "int_val", 42 },
                { "str_val", "hello" },
                { "bool_val", true }
            };

            _bus.Emit("payload_test", sent);

            Assert.IsNotNull(received);
            Assert.AreEqual(42, received["int_val"]);
            Assert.AreEqual("hello", received["str_val"]);
            Assert.AreEqual(true, received["bool_val"]);
            Assert.AreEqual(3, received.Count);
        }
    }
}
