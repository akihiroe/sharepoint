using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharePointExplorer.Models;
using System.Windows;

namespace SharePointExplorer.Tests
{
    [TestClass]
    public class SPFolderItemUnitTest
    {
        private class MyExploreVM : ExplorerVM
        {
            protected override bool Confirm(string title, string message)
            {
                return true;
            }
        }

        static ExplorerVM testVm;
        static SPSiteItem testSite;
        static SPDocumentLibraryItem testLib;
        static Models.SPFolderItem testFolder;

        static private string TestFileText = "Testファイル.txt";

        [ClassInitialize]
        public static void SetupTestFolder(TestContext context)
        {
            AppViewModel.ExecuteActionAsyncMode = false;
            testVm = new MyExploreVM();
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
        public void ExecuteFileCommandTest()
        {
            testFolder.SelectedFile = (SPFileItem)testFolder.Items.Where(x => x.Name == TestFileText).FirstOrDefault();
            testFolder.ExecuteFileCommand.Execute(null);

            Assert.AreEqual("", testFolder.Message);
        }

        [TestMethod]
        public void UploadCommandTest()
        {
            testFolder.UploadCommand.Execute(TestFileText);
            Assert.AreEqual("", testFolder.Message);
        }

        [TestMethod]
        public void DeleteCommandTest()
        {
            testFolder.SelectedFile = (SPFileItem)testFolder.Items.Where(x => x.Name == TestFileText).FirstOrDefault();
            testFolder.DeleteCommand.Execute(TestFileText);
            Assert.AreEqual("", testFolder.Message);
        }

        [TestMethod]
        public void OpenCommand()
        {
            testFolder.SelectedFile = (SPFileItem)testFolder.Items.Where(x => x.Name == TestFileText).FirstOrDefault();
            testFolder.OpenCommand.Execute(TestFileText);
            Assert.AreEqual("", testFolder.Message);
        }

        [TestMethod]
        public void CopyUrlToClipboardCommandTest()
        {
            testFolder.SelectedFile = (SPFileItem)testFolder.Items.Where(x => x.Name == TestFileText).FirstOrDefault();
            testFolder.CopyUrlToClipboardCommand.Execute(TestFileText);
            Assert.AreEqual(testFolder.SelectedFile.SPUrl, Clipboard.GetText());
            Assert.AreEqual("", testFolder.Message);
        }

        [TestMethod]
        public void CheckoutCommandTest()
        {
            testFolder.SelectedFile = (SPFileItem)testFolder.Items.Where(x => x.Name == TestFileText).FirstOrDefault();
            testFolder.CheckoutCommand.Execute(null);
            Assert.IsNotNull(testFolder.SelectedFile.CheckedOut);
            Assert.AreEqual("", testFolder.Message);
        }

        [TestMethod]
        public void CheckinCommandTest()
        {
            testFolder.SelectedFile = (SPFileItem)testFolder.Items.Where(x => x.Name == TestFileText).FirstOrDefault();
            testFolder.CheckoutCommand.Execute(null);
            Assert.IsNotNull(testFolder.SelectedFile.CheckedOut);
            testFolder.CheckinCommand.Execute(null);
            Assert.IsNull(testFolder.SelectedFile.CheckedOut);
            Assert.AreEqual("", testFolder.Message);
        }

        [TestMethod]
        public void CancelCheckoutCommandTest()
        {
            testFolder.SelectedFile = (SPFileItem)testFolder.Items.Where(x => x.Name == TestFileText).FirstOrDefault();
            testFolder.CheckoutCommand.Execute(null);
            Assert.IsNotNull(testFolder.SelectedFile.CheckedOut);
            testFolder.CancelCheckoutCommand.Execute(null);
            Assert.IsNull(testFolder.SelectedFile.CheckedOut);
            Assert.AreEqual("", testFolder.Message);
        }


        [TestMethod]
        public void CopyCommandTest()
        {
            testFolder.SelectedFile = (SPFileItem)testFolder.Items.Where(x => x.Name == TestFileText).FirstOrDefault();
            testFolder.CopyCommand.Execute(null);
            Assert.AreEqual("", testFolder.Message);
        }

        [TestMethod]
        public void PasteCommandTest()
        {
            testFolder.SelectedFile = (SPFileItem)testFolder.Items.Where(x => x.Name == TestFileText).FirstOrDefault();
            testFolder.PasteCommand.Execute(null);
            Assert.AreEqual("", testFolder.Message);
        }

    }
}
