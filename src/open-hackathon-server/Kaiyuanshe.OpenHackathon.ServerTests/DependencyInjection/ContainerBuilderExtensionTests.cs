using Autofac;
using Kaiyuanshe.OpenHackathon.Server.DependencyInjection;
using NUnit.Framework;
using System;

namespace Kaiyuanshe.OpenHackathon.ServerTests.DependencyInjection
{
    class ContainerBuilderExtensionTests
    {
        [Test]
        public void RegisterSubTypes()
        {
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterSubTypes(typeof(ITestA));

            var container = builder.Build();
            Assert.IsFalse(container.IsRegistered(typeof(ITestA)));
            Assert.IsFalse(container.IsRegistered(typeof(TestABase)));
            Assert.IsTrue(container.IsRegistered(typeof(TestASubA)));
            Assert.IsTrue(container.IsRegistered(typeof(TestASubB)));
        }

        [Test]
        public void RegisterSubTypesAsDirectInterfaces()
        {
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterSubTypesAsDirectInterfaces(typeof(ITestB));

            var container = builder.Build();
            Assert.IsFalse(container.IsRegistered(typeof(ITestB)));
            Assert.IsFalse(container.IsRegistered(typeof(TestBBase)));

            // no interface
            Assert.IsFalse(container.IsRegistered(typeof(TestBSubC)));

            // single
            AssertResolvable(container, typeof(ITestBSubB), typeof(TestBSubB));

            // multiple
            AssertResolvable(container, typeof(ITestBSubA1), typeof(TestBSubA));
            AssertResolvable(container, typeof(ITestBSubA2), typeof(TestBSubA));
        }

        [Test]
        public void RegisterGenericSubTypesAsDirectInterfaces()
        {
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterSubTypesAsDirectInterfaces(typeof(ITestC<>));

            var container = builder.Build();
            Assert.IsFalse(container.IsRegistered(typeof(ITestC<>)));
            Assert.IsFalse(container.IsRegistered(typeof(TestCBase<>)));

            // no interface
            Assert.IsFalse(container.IsRegistered(typeof(TestCSubA)));

            // single interface
            AssertResolvable(container, typeof(ITestCSubB), typeof(TestCSubB));

            // multiple interfaces
            AssertResolvable(container, typeof(ITestCSubC1), typeof(TestCSubC));
            AssertResolvable(container, typeof(ITestCSubC2), typeof(TestCSubC));

            // sub-sub
            AssertResolvable(container, typeof(ITestCSubD), typeof(TestCSubDSub));

            // generic sub
            AssertResolvable(container, typeof(IWLogSubTable), typeof(WlogSubTable));
        }

        private void AssertResolvable(IContainer container, Type resolveType, Type expectedInstanceType)
        {
            Assert.IsTrue(container.IsRegistered(resolveType));
            Assert.IsTrue(container.IsRegistered(expectedInstanceType));
            var resolved = container.Resolve(resolveType);
            Assert.IsNotNull(resolved);
            Assert.AreEqual(expectedInstanceType, resolved.GetType());
        }
    }
}
