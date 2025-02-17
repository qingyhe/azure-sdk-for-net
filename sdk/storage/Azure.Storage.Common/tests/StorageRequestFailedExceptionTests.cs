﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for
// license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Core.Testing;
using NUnit.Framework;

namespace Azure.Storage.Common.Test
{
    [TestFixture]
    public class StorageRequestFailedExceptionTests
    {
        [Test]
        public void CreateFromResponse()
        {
            var response = new MockResponse(400, "reason");
            var ex = new StorageRequestFailedException(response);

            Assert.AreEqual(400, ex.Status);
            StringAssert.StartsWith("reason", ex.Message);
        }

        [Test]
        public void CreateFromResponseAndMessage()
        {
            var response = new MockResponse(400, "reason");
            var ex = new StorageRequestFailedException(response, "message");

            Assert.AreEqual(400, ex.Status);
            StringAssert.StartsWith("message", ex.Message);
            StringAssert.Contains("reason", ex.Message);
        }

        [Test]
        public void CreateFromResponseMessageAndException()
        {
            var response = new MockResponse(400, "reason");
            var inner = new Exception("Boom!");
            var ex = new StorageRequestFailedException(response, "message", inner);

            Assert.AreEqual(400, ex.Status);
            StringAssert.StartsWith("message", ex.Message);
            StringAssert.Contains("reason", ex.Message);
            Assert.AreEqual(inner, ex.InnerException);
        }

        [Test]
        public void CreateFromResponseAndException()
        {
            var response = new MockResponse(400, "reason");
            var inner = new Exception("Boom!");
            var ex = new StorageRequestFailedException(response, null, inner);

            Assert.AreEqual(400, ex.Status);
            StringAssert.StartsWith("reason", ex.Message);
            Assert.AreEqual(inner, ex.InnerException);
        }

        [Test]
        public void CreateFromResponseMessageExceptionAndErrorCode()
        {
            var response = new MockResponse(400, "reason");
            var inner = new Exception("Boom!");
            var ex = new StorageRequestFailedException(response, "message", inner, "storagecode");

            Assert.AreEqual(400, ex.Status);
            StringAssert.StartsWith("message", ex.Message);
            StringAssert.Contains("reason", ex.Message);
            Assert.AreEqual(inner, ex.InnerException);
            Assert.AreEqual("storagecode", ex.ErrorCode);
            StringAssert.Contains("storagecode", ex.Message);
        }

        [Test]
        public void CreateFromEverything()
        {
            var response = new MockResponse(400, "reason");
            var inner = new Exception("Boom!");
            var additional = new Dictionary<string, string>
            {
                { "foo", "bar" },
                { "qux", "quux" }
            };
            var ex = new StorageRequestFailedException(response, "message", inner, "storagecode", additional);

            Assert.AreEqual(400, ex.Status);
            StringAssert.StartsWith("message", ex.Message);
            StringAssert.Contains("reason", ex.Message);
            Assert.AreEqual(inner, ex.InnerException);
            Assert.AreEqual("storagecode", ex.ErrorCode);
            StringAssert.Contains("storagecode", ex.Message);
            StringAssert.Contains("foo: bar", ex.Message);
            StringAssert.Contains("qux: quux", ex.Message);
        }

        [Test]
        public void CreateFromResponseAndErrorCode()
        {
            var response = new MockResponse(400, "reason");
            var ex = new StorageRequestFailedException(response, null, null, "storagecode");

            Assert.AreEqual(400, ex.Status);
            StringAssert.StartsWith("reason", ex.Message);
            Assert.AreEqual("storagecode", ex.ErrorCode);
            StringAssert.Contains("storagecode", ex.Message);
        }

        [Test]
        public void MessageIncludesStatus()
        {
            var ex = new StorageRequestFailedException(new MockResponse(400, "reason"));

            Assert.AreEqual(400, ex.Status);
            StringAssert.Contains("400", ex.Message);
        }

        [Test]
        public void AdditionalInfoOnlyWhenPresent()
        {
            var ex = new StorageRequestFailedException(new MockResponse(400, "reason"), null, null, null, new Dictionary<string, string> { { "foo", "bar" } });
            StringAssert.Contains("Additional Information", ex.Message);

            ex = new StorageRequestFailedException(new MockResponse(400, "reason"));
            StringAssert.DoesNotContain("Additional Information", ex.Message);
        }

        [Test]
        public void UsesResponseErrorCode()
        {
            var response = new MockResponse(400, "reason");
            response.AddHeader(new HttpHeader("x-ms-error-code", "storagecode"));
            var ex = new StorageRequestFailedException(response);

            Assert.AreEqual(400, ex.Status);
            StringAssert.StartsWith("reason", ex.Message);
            Assert.AreEqual("storagecode", ex.ErrorCode);
            StringAssert.Contains("storagecode", ex.Message);
        }

        [Test]
        public void NullResponseThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new StorageRequestFailedException(null));
            Assert.Throws<ArgumentNullException>(() => new StorageRequestFailedException(null, "message"));
            Assert.Throws<ArgumentNullException>(() => new StorageRequestFailedException(null, null, new Exception()));
            Assert.Throws<ArgumentNullException>(() => new StorageRequestFailedException(null, null, null, "storagecode"));
        }
    }
}
