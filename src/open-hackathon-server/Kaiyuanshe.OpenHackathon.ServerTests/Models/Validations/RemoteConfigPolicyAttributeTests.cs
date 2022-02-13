using Kaiyuanshe.OpenHackathon.Server.Models;
using Kaiyuanshe.OpenHackathon.Server.Models.Validations;
using NUnit.Framework;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Models.Validations
{
    class RemoteConfigPolicyAttributeTests
    {
        [Test]
        public void IsValidTest()
        {
            var attribute = new RemoteConfigPolicyAttribute();
            Assert.AreEqual(true, attribute.IsValid(null));

            // no protocol
            Template a = new Template();
            Assert.AreEqual(true, attribute.IsValid(a));

            // rdp
            Template a2 = new Template { ingressProtocol = IngressProtocol.rdp };
            Assert.AreEqual(true, attribute.IsValid(a));

            // vnc null
            Template b = new Template { ingressProtocol = IngressProtocol.vnc };
            Assert.AreEqual(false, attribute.IsValid(b));

            // username null
            Template c = new Template
            {
                ingressProtocol = IngressProtocol.vnc,
                vnc = new VncSettings
                {
                    password = "pw"
                }
            };
            Assert.AreEqual(false, attribute.IsValid(c));

            // password null
            Template d = new Template
            {
                ingressProtocol = IngressProtocol.vnc,
                vnc = new VncSettings
                {
                    userName = "un"
                }
            };
            Assert.AreEqual(false, attribute.IsValid(d));

            // valid
            Template e = new Template
            {
                ingressProtocol = IngressProtocol.vnc,
                vnc = new VncSettings
                {
                    password = "pw",
                    userName = "un"
                }
            };
            Assert.AreEqual(true, attribute.IsValid(e));
        }
    }
}
