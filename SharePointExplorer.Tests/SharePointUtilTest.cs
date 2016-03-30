using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace SharePointExplorer.Tests
{
    [TestClass]
    public class SharePointUtilTest
    {


        [TestMethod]
        public void DocumentLibraryTests()
        {
            using (var util = new SharePointUtil(TestConfig.SiteUrl, TestConfig.User, TestConfig.Pass))
            {
                var lists = util.ListDocumentLibrary();
                foreach (var item in lists)
                {
                    Debug.WriteLine(item.Title);
                }
                Assert.IsTrue(lists.Any(x => x.Title == "Shared Documents"));
            }
        }
    }
}
