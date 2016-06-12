using System.Web.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebApiRouteHelper.Tests
{
    [RoutePrefix("api/fake")]
    public class FakeController : ApiController
    {
        [Route("uppercase")]
        public string GetUpperrcase(string str)
        {
            return str.ToUpper();
        }
    }

    [TestClass]
    public class RouteHelperTests
    {
        private RouteHelper _helper;

        [TestInitialize]
        public void TestInitialize()
        {
            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();
            config.EnsureInitialized();

            _helper = new RouteHelper(config.Routes);
        }

        [TestMethod]
        public void GetRouteUrl_returns_correct_Url_for_attribute_rputes()
        {
            var url = _helper.GetRouteUrl((FakeController c) => c.GetUpperrcase("test"));
            Assert.AreEqual("api/fake/uppercase?str=test", url);
        }
    }
}
