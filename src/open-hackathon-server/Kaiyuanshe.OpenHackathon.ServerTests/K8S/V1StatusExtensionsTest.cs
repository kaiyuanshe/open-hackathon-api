using k8s.Models;
using Kaiyuanshe.OpenHackathon.Server.K8S;
using NUnit.Framework;

namespace Kaiyuanshe.OpenHackathon.ServerTests.K8S
{
    class V1StatusExtensionsTest
    {
        [Test]
        public void IsFailed()
        {
            Assert.IsFalse(V1StatusExtensions.IsFailed(null));
            Assert.IsFalse(V1StatusExtensions.IsFailed(new V1Status { }));
            Assert.IsTrue(V1StatusExtensions.IsFailed(new V1Status { Code = 400 }));
            Assert.IsTrue(V1StatusExtensions.IsFailed(new V1Status { Code = 409 }));
            Assert.IsTrue(V1StatusExtensions.IsFailed(new V1Status { Code = 422 }));
            Assert.IsTrue(V1StatusExtensions.IsFailed(new V1Status { Code = 500 }));
            Assert.IsFalse(V1StatusExtensions.IsFailed(new V1Status { Code = 200 }));
            Assert.IsFalse(V1StatusExtensions.IsFailed(new V1Status { Code = 201 }));
            Assert.IsFalse(V1StatusExtensions.IsFailed(new V1Status { Code = 202 }));
            Assert.IsFalse(V1StatusExtensions.IsFailed(new V1Status { Code = 204 }));
            Assert.IsFalse(V1StatusExtensions.IsFailed(new V1Status { Code = 307 }));
        }
    }
}
