﻿using Kaiyuanshe.OpenHackathon.Server.Kubernetes;
using Kaiyuanshe.OpenHackathon.Server.Storage;
using Kaiyuanshe.OpenHackathon.Server.Storage.BlobContainers;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaiyuanshe.OpenHackathon.ServerTests.Kubernetes
{
    class KubenetesConfigProviderTests
    {
        #region GetDefaultConfigAsync
        [Test]
        public async Task GetDefaultConfigAsync()
        {
            string testConfig = @"
apiVersion: v1
clusters:
- cluster:
    certificate-authority-data: certificate-authority-data
    server: https://10.0.0.1:8443
  name: kubernetes
contexts:
- context:
    cluster: kubernetes
    user: kubernetes-admin
  name: kubernetes-admin@kubernetes
current-context: kubernetes-admin@kubernetes
kind: Config
preferences: {}
users:
- name: kubernetes-admin
  user:
    client-certificate-data: client-certificate-data 
    client-key-data: client-key-data
";

            var kubeContainer = new Mock<IKubernetesBlobContainer>();
            kubeContainer.Setup(k => k.DownloadBlockBlobAsync("default/kubeconfig.yaml", default)).ReturnsAsync(testConfig);
            var storage = new Mock<IStorageContext>();
            storage.SetupGet(s => s.KubernetesBlobContainer).Returns(kubeContainer.Object);

            var kubeConfigProvider = new KubenetesConfigProvider
            {
                StorageContext = storage.Object,
            };
            var config = await kubeConfigProvider.GetDefaultConfigAsync(default);

            Assert.AreEqual("v1", config.ApiVersion);
            Assert.AreEqual("certificate-authority-data", config.Clusters.Single().ClusterEndpoint.CertificateAuthorityData);
            Assert.AreEqual("https://10.0.0.1:8443", config.Clusters.Single().ClusterEndpoint.Server);
            Assert.AreEqual("kubernetes", config.Clusters.Single().Name);
            Assert.AreEqual("kubernetes", config.Contexts.Single().ContextDetails.Cluster);
            Assert.AreEqual("kubernetes-admin", config.Contexts.Single().ContextDetails.User);
            Assert.AreEqual("kubernetes-admin@kubernetes", config.Contexts.Single().Name);
            Assert.AreEqual("kubernetes-admin@kubernetes", config.CurrentContext);
            Assert.AreEqual("Config", config.Kind);
            Assert.AreEqual(0, config.Preferences.Count);
            Assert.AreEqual("kubernetes-admin", config.Users.First().Name);
            Assert.AreEqual("client-certificate-data", config.Users.First().UserCredentials.ClientCertificateData);
            Assert.AreEqual("client-key-data", config.Users.First().UserCredentials.ClientKeyData);
        }
        #endregion
    }
}
