using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Shared;

public class RenderView
{
    private readonly IRazorViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceProvider _serviceProvider;

    public RenderView(IRazorViewEngine viewEngine, ITempDataProvider tempDataProvider, IServiceProvider serviceProvider)
    {
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
        _serviceProvider = serviceProvider;
    }

    public async Task<string> RenderToStringAsync<TModel>(TModel model)
    {

        var viewName = string.Concat(typeof(TModel).FullName.Replace("Model", "")).Replace("Web.Views.", "").Replace(".", "/");
        using var stringWriter = new StringWriter();

        var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        var viewResult = _viewEngine.FindView(actionContext, viewName, false);

        if (viewResult.View == null)
        {
            throw new ArgumentNullException($"View '{viewName}' not found.");
        }

        var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = model
        };

        var tempData = new TempDataDictionary(httpContext, provider: _tempDataProvider);
        var viewContext = new ViewContext(actionContext, viewResult.View, viewDictionary, tempData, stringWriter, new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);

        return stringWriter.ToString();
    }
}


//public class RenderView
//{
//    private readonly IRazorViewEngine _razorViewEngine;
//    private readonly ITempDataProvider _tempDataProvider;
//    private readonly IHttpContextAccessor _contextAccessor;

//    public RenderView(IRazorViewEngine razorViewEngine, ITempDataProvider tempDataProvider, IHttpContextAccessor contextAccessor)
//    {
//        _razorViewEngine = razorViewEngine;
//        _tempDataProvider = tempDataProvider;
//        _contextAccessor = contextAccessor;
//    }

//    public async Task<string> RenderToString<TModel>(TModel model)
//    {
//        var actionContext = new ActionContext(_contextAccessor.HttpContext, _contextAccessor.HttpContext.GetRouteData(), new ActionDescriptor());

//        var viewName = string.Concat(typeof(TModel).FullName.Replace("Model", "")).Replace("Web.Views.", "").Replace(".", "/");

//        await using var sw = new StringWriter();
//        var viewResult = FindView(actionContext, viewName);

//        if (viewResult == null)
//            throw new ArgumentNullException($"{viewName} does not match any available view");

//        var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary());
//        viewDictionary.Model = model;

//        var viewContext = new ViewContext(
//            actionContext,
//            viewResult,
//            viewDictionary,
//            new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
//            sw,
//            new HtmlHelperOptions()
//        );

//        await viewResult.RenderAsync(viewContext);
//        return sw.ToString();
//    }


//    private IView FindView(ActionContext actionContext, string viewName)
//    {
//        var findPageResult = _razorViewEngine.FindPage(actionContext, viewName);

//        var asd = findPageResult.Page?.BodyContent.ToString();

//        var getViewResult = _razorViewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: true);
//        if (getViewResult.Success)
//        {
//            return getViewResult.View;
//        }

//        var findViewResult = _razorViewEngine.FindView(actionContext, viewName, isMainPage: true);
//        if (findViewResult.Success)
//        {
//            return findViewResult.View;
//        }

//        var searchedLocations = getViewResult.SearchedLocations.Concat(findViewResult.SearchedLocations);
//        var errorMessage = string.Join(
//            Environment.NewLine,
//            new[] { $"Unable to find view '{viewName}'. The following locations were searched:" }.Concat(searchedLocations));

//        throw new InvalidOperationException(errorMessage);
//    }
//}
