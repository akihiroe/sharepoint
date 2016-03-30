using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharePointExplorer.Models;

namespace SharePointExplorer.Tests
{
    [TestClass]
    public class SPFileItemUnitTest
    {
        static ExplorerVM testVm;
        static SPSiteItem testSite;
        static SPDocumentLibraryItem testLib;
        static SPFolderItem testFolder;
        static SPFileItem testFile1;
        static SPFileItem testFile2;
        static SPFileItem testFile3;
        static SPFileItem testFile4;

        static private string TestFileText1 = "Testファイル1.txt";
        static private string TestFileText2 = "Testファイル2.txt";
        static private string TestFileText3 = "Testファイル3.txt";

        private class MyExploreVM : ExplorerVM
        {
            protected override bool Confirm(string title, string message)
            {
                return true;
            }
        }


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
            System.IO.File.WriteAllText(TestFileText1, "TEST キーワード " + DateTime.Now.ToString("yyyy/DD/mm HH:mm:ss"), System.Text.Encoding.UTF8);
            testFolder.UploadCommand.Execute(new string[] { TestFileText1 });
            testFile1 = (SPFileItem)testFolder.Items.Where(x => x.Name == TestFileText1).FirstOrDefault();
            System.IO.File.WriteAllText(TestFileText2, "TEST キーワード " + DateTime.Now.ToString("yyyy/DD/mm HH:mm:ss"), System.Text.Encoding.UTF8);
            testFolder.UploadCommand.Execute(new string[] { TestFileText2 });
            testFile2 = (SPFileItem)testFolder.Items.Where(x => x.Name == TestFileText2).FirstOrDefault();
            System.IO.File.WriteAllText(TestFileText3, "TEST キーワード " + DateTime.Now.ToString("yyyy/DD/mm HH:mm:ss"), System.Text.Encoding.UTF8);
            testFolder.UploadCommand.Execute(new string[] { TestFileText3 });
            testFile3 = (SPFileItem)testFolder.Items.Where(x => x.Name == TestFileText2).FirstOrDefault();

            testFile4 = (SPFileItem)testFolder.Items.Where(x => x.Name == "新しい名前.txt").FirstOrDefault();
            if (testFile4 != null) testFile4.DeleteCommand.Execute(null);
        }

        [TestMethod]
        public void RenameCommandTest()
        {
            testFile2.NewName = "新しい名前.txt";
            testFile2.RenameCommand.Execute(null);
            testFile4 = (SPFileItem)testFolder.Items.Where(x => x.Name == "新しい名前.txt").FirstOrDefault();
            Assert.IsNotNull(testFile4);
        }
    }
}
