using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharePointExplorer.Models;
using System.Diagnostics;

namespace SharePointExplorer.Tests
{
    [TestClass]
    public class SPSiteListUnitTest
    {
        [TestMethod]
        public void SPListItem生成テスト()
        {
            using (var site = new SPSiteItem(null, TestConfig.SiteUrl, TestConfig.User, TestConfig.Pass))
            {
                site.EnsureChildren().Wait();
                var list = site.Children.FirstOrDefault(x => x.Name == "Shared Documents");
                list.EnsureChildren().Wait();
                foreach (var item in list.Children)
                {
                    Debug.WriteLine(item.Name);
                }
                Assert.IsTrue(list.Children.Any(x => x.Name == "TEST"));
            }
        }
    }
}
