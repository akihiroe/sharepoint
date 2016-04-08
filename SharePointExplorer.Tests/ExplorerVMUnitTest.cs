using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharePointExplorer.Models;

namespace SharePointExplorer.Tests
{
    [TestClass]
    public class ExplorerVMUnitTest
    {
        static ExplorerVM testVm;
        static SPSiteItem testSite;
        static SPDocumentLibraryItem testLib;
        static Models.SPFolderItem testFolder;

        static private string TestFileText = "Testファイル.txt";

        [ClassInitialize]
        public static void SetupTestFolder(TestContext context)
        {
            AppViewModel.ExecuteActionAsyncMode = false;
            testVm = new ExplorerVM();
            testSite = (SPSiteItem)testVm.Children.Where(x => x.Name.Trim('/') == TestConfig.SiteUrl.Trim('/')).First();
            testSite.EnsureChildren().Wait();
            testLib = (SPDocumentLibraryItem)testSite.Children.Where(x => x.Name == "Shared Documents").First();
            testLib.EnsureChildren().Wait();
            testFolder = (Models.SPFolderItem)testLib.Children.Where(x => x.Name == "TEST").First();
            testFolder.EnsureChildren().Wait();
            System.IO.File.WriteAllText(TestFileText, "TEST キーワード " + DateTime.Now.ToString("yyyy/DD/mm HH:mm:ss"), System.Text.Encoding.UTF8);
            testFolder.UploadCommand.Execute(new string[] { TestFileText });
        }

        [TestMethod]
        public void SelectedItemChangedCommandTest()
        {
            testVm.SelectedItemChangedCommand.Execute(testFolder);
            Assert.IsNotNull(testVm.CurrentContent);
            Assert.IsTrue(testVm.SelectedItem == testFolder);
        }

        [TestMethod]
        public void SearchCommandTest()
        {
            testVm.SelectedItem = testLib;
            testVm.SearchCommand.Execute("test");
            var searchVM = testVm.Children.Where(x => x.Name == Properties.Resources.MsgSearchResults).FirstOrDefault() as SPSearchResultsItem;
            Assert.IsNotNull(searchVM);
            Assert.IsTrue(searchVM.Items.Count > 0);
        }

        [TestMethod]
        public void ClearCacheCommandTest()
        {
            testVm.SelectedItem = testLib;
            testVm.ClearCacheCommand.Execute(null);

            //CONFIRM NO ERROR
        }

        [TestMethod]
        public void CancelCommandTest()
        {
            testVm.SelectedItem = testLib;
            testVm.CancelCommand.Execute(null);

            //CONFIRM NO ERROR
        }

    }
}
