using Kaiyuanshe.OpenHackathon.Server.Helpers;
using NUnit.Framework;
using System;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Helpers
{
    internal class EnumHelperTest
    {
        [AttributeUsage(AttributeTargets.Field)]
        public class EnumFieldTestAttribute : Attribute
        {
            public string? Name { get; set; }
        }

        public enum CustomFieldAttr
        {
            [EnumFieldTest(Name = "zero")]
            Zero,
            One,
        }


        [Test]
        public void GetCustomAttribute()
        {
            Assert.AreEqual("zero", EnumHelper.GetCustomAttribute<EnumFieldTestAttribute>(CustomFieldAttr.Zero)?.Name);
            Assert.IsNull(EnumHelper.GetCustomAttribute<EnumFieldTestAttribute>(CustomFieldAttr.One));
        }
    }
}
