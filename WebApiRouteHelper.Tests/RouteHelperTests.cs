using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin.Hosting;
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
        private const string BaseUri = "http://localhpst:333";

        [TestInitialize]
        public void TestInitialize()
        {
            HttpConfiguration config = null;
            WebApp.Start(BaseUri, builder =>
            {
                config = new HttpConfiguration();
                config.MapHttpAttributeRoutes();
                config.EnsureInitialized();
            });

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
            var x = Get<string[]>(url);
            url = _helper.GetRouteUrl((FakeController c) => c.GetPersonName(new Person{Name = "User", Age = 23}));
        }

        public async Task<TResponse> Get<TResponse>(string relativeUri) where TResponse : class
        {
            using (var httpClient = new HttpClient())
            {
                var uri = new Uri(new Uri(BaseUri), relativeUri);
                var message = await httpClient.GetAsync(uri);
                return await message.Content.ReadAsAsync<TResponse>();
            }
        }
    }
}
