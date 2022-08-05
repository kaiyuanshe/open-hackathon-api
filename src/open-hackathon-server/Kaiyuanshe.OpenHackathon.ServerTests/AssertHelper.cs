using Kaiyuanshe.OpenHackathon.Server.Models;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using System;
using System.Diagnostics;

namespace Kaiyuanshe.OpenHackathon.ServerTests
{
    public static class AssertHelper
    {
        public static void AssertObjectResult(object result, int expectedStatusCode, string expectedDetail)
        {
            AssertObjectResult(result, expectedStatusCode, (p) =>
            {
                Assert.AreEqual(expectedDetail, p.Detail);
            });
        }

        public static void AssertObjectResult(object result, int expectedStatusCode, Action<ProblemDetails>? extraAssert = null)
        {
            ObjectResult? objectResult = result as ObjectResult;
            Assert.IsNotNull(objectResult);
            Debug.Assert(objectResult != null);
            Assert.AreEqual(expectedStatusCode, objectResult.StatusCode);

            ProblemDetails? problemDetails = objectResult.Value as ProblemDetails;
            Assert.IsNotNull(problemDetails);
            Debug.Assert(problemDetails != null);
            Assert.IsTrue(problemDetails.Status.HasValue);
            Debug.Assert(problemDetails.Status.HasValue);
            Assert.AreEqual(expectedStatusCode, problemDetails.Status.Value);
            if (extraAssert != null)
            {
                extraAssert(problemDetails);
            }
        }

        public static T AssertOKResult<T>(object result)
        {
            Assert.IsTrue(result is OkObjectResult);
            OkObjectResult objectResult = (OkObjectResult)result;
            Assert.IsNotNull(objectResult.Value);
            Debug.Assert(objectResult.Value != null);
            Assert.IsTrue(objectResult.Value.GetType() == typeof(T));
            return (T)objectResult.Value;
        }

        public static void AssertNoContentResult(object result)
        {
            Assert.IsTrue(result is NoContentResult);
            NoContentResult noContent = (NoContentResult)result;
            Assert.AreEqual(204, noContent.StatusCode);
        }

        public static void AssertEqual(Pagination expected, Pagination actual)
        {
            if (expected == actual)
                return;

            Assert.IsNotNull(expected);
            Assert.IsNotNull(actual);
            Assert.AreEqual(expected.np, actual.np);
            Assert.AreEqual(expected.nr, actual.nr);
            Assert.AreEqual(expected.top, actual.top);
        }
    }
}
