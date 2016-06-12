using System.Web.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebApiRouteHelper.Tests
{
    public class FakeController : ApiController
    {
        [Route("uppercase")]
        public string GetUpperrcase(string str)
        {
            return str.ToUpper();
        }

        [Route("lowercase/{str}")]
        public string GetLowercase(string str)
        {
            return str.ToLower();
        }

        [Route("person/name")]
        public string GetPersonName(Person person)
        {
            return person.Name;
        }

        [Route("get-values")]
        public string[] GetValues([FromUri]string[] values)
        {
            return values;
        }
    }

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    [RoutePrefix("api")]
    public class FakeControllerWithPrefix : FakeController {}

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
        public void GetRouteUrl_returns_correct_Url_for_attribute_routes()
        {
            var url = _helper.GetRouteUrl((FakeController c) => c.GetUpperrcase("test"));
            Assert.AreEqual("uppercase?str=test", url);
            url = _helper.GetRouteUrl((FakeController c) => c.GetLowercase("test"));
            Assert.AreEqual("lowercase/test", url);
            url = _helper.GetRouteUrl((FakeController c) => c.GetValues(new[] {"test1", "test2"}));
            url = _helper.GetRouteUrl((FakeController c) => c.GetPersonName(new Person{Name = "User", Age = 23}));
        }
    }
}
