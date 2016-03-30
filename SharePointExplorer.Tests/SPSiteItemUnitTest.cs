using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharePointExplorer.Models;
using System.Linq;

namespace SharePointExplorer.Tests
{
    [TestClass]
    public class SPSiteItemUnitTest
    {
        [TestMethod]
        public void SPSitem生成テスト()
        {
            using (var site = new SPSiteItem(null, TestConfig.SiteUrl, TestConfig.User, TestConfig.Pass))
            {
                site.EnsureChildren().Wait();
                Assert.IsTrue(site.Children.Any(x => x.Name == "Shared Documents"));

            }
        }
    }
}
