using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Hosting;
using System.Web.Http.Routing;

namespace WebApiRouteHelper
{
    /// <summary>
    /// This class is providing strngrly typed api to generate WebAPI routes in order to be able to generate
    /// relative routes to controller actions without hardcoding Url strings
    /// Inspired by the library HyperLinkr (https://github.com/ploeh/Hyprlinkr) but addresses the use case when
    /// there is no meaningfull HttpRequest available (in unit-tests or during static code generation). Instead it's
    /// based on Routes Collecton provided 
    /// <example>
    /// In order to generate relative URL for action GetValus in the following controller:
    /// <code>
    /// [RoutePrefix("api/foo")]
    /// public class FooController: ApiController
    /// {
    ///     [Route("uppercase")]
    ///     public string GetUppercase(string str)
    ///     {
    ///         return str.ToUpper();
    ///     }
    /// }
    /// </code>
    /// You need create intance of RouteHelper passing <see cref="HttpRouteCollection"/>
    /// and calling method GetRouteUrl with lambda expression containing only your controller action with
    /// all argumens. Like so:
    /// <code>
    /// var helper = new RouteHelper(HttpConfiguration.Routes);
    /// var url = helper.GetRouteUrl( (FooController f) => f.GetUpperCase("test")); 
    /// </code>
    /// Whoch will return `/api/foo/uppercase?str=test`
    /// </example>
    /// </summary>
    public class RouteHelper
    {
        private readonly HttpRouteCollection _routes;
        private const string ActionsKey = "actions";
        public RouteHelper(HttpRouteCollection routes)
        {
            _routes = routes;
        }

        public string GetRouteUrl<TController>(Expression<Action<TController>> actionExpression)
        {
            return GetRelativeUrl(actionExpression.Body);
        }

        public string GetRouteUrl<TController, TResult>(Expression<Func<TController, TResult>> actionExpression)
        {
            return GetRelativeUrl(actionExpression.Body);
        }

        public string GetRouteUrl<TController>(Expression<Action<TController, Task>> actionExpression)
        {
            return GetRelativeUrl(actionExpression.Body);
        }

        public string GetRouteUrl<TController, TResult>(Expression<Func<TController, Task<TResult>>> actionExpression)
        {
            return GetRelativeUrl(actionExpression.Body);
        }

        private string GetRelativeUrl(Expression expr)
        {
            var methodExpr = expr as MethodCallExpression;
            if (methodExpr == null)
            {
                throw new ArgumentException("Expression is not of type MethodCallExpression. It must be of format 'controller => controller.Action(param1, const2)'");
            }
            var routeValues = GetRouteValues(methodExpr);
            var route = FindRoute(methodExpr.Method, _routes);
            var fakeReq = GetFakeRequest();
            return route.GetVirtualPath(fakeReq, routeValues).VirtualPath;
        }

        private static IDictionary<string, object> GetRouteValues(MethodCallExpression methodExpr)
        {
            var routeValues = new Dictionary<string, object>();
            var parameters = methodExpr.Method.GetParameters();
            foreach (var parameter in parameters)
            {
                var arg = methodExpr.Arguments[parameter.Position];
                var lambda = Expression.Lambda(arg);
                var value = lambda.Compile().DynamicInvoke();
                routeValues.Add(parameter.Name, value);
            }
            // In order to be processed internally by HttpRoute.GetVirtualPath we need to add HttRouteKey attribute 
            if (!routeValues.ContainsKey(HttpRoute.HttpRouteKey))
            {
                routeValues.Add(HttpRoute.HttpRouteKey, true);
            }
            return routeValues;
        }

        private static IHttpRoute FindRoute(MethodInfo methodInfo, IEnumerable<IHttpRoute> routes)
        {
            if (methodInfo.ReflectedType == null)
            {
                throw new ArgumentException("method must be member of the type");
            }
            var controllerName = methodInfo.ReflectedType.Name;
            foreach (var route in routes)
            {
                var routeCollection = route as IEnumerable<IHttpRoute>;
                if (routeCollection != null)
                {
                    return FindRoute(methodInfo, routeCollection);
                }
                var routeData = route.DataTokens;
                if (routeData != null && routeData.ContainsKey(ActionsKey))
                {
                    var actions = routeData[ActionsKey] as IEnumerable<HttpActionDescriptor>;
                    if (actions == null)
                    {
                        continue;
                    }
                    if (actions.Any(action => action.ActionName == methodInfo.Name &&
                                              action.ControllerDescriptor.ControllerType.Name == controllerName))
                    {
                        return route;
                    }
                }
            }
            return null;
        }

        private static HttpRequestMessage GetFakeRequest()
        {
            var fakeReq = new HttpRequestMessage();
            var context = new HttpRequestContext { Url = new UrlHelper(fakeReq) };
            fakeReq.Properties.Add(HttpPropertyKeys.RequestContextKey, context);
            return fakeReq;
        }
    }
}
