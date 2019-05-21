using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            CheckOnlineVkfaces.Program.Manager manager = new CheckOnlineVkfaces.Program.Manager();
            manager.Add("sg_muwa");
            Assert.AreEqual(1, manager.Count);
            manager.Add("id324842960");
            Assert.AreEqual(2, manager.Count);
        }
    }
}
