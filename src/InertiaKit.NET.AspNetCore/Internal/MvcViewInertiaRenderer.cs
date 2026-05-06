using Microsoft.Extensions.DependencyInjection;

namespace InertiaKit.AspNetCore.Internal;

internal sealed class MvcViewInertiaRenderer : IInertiaRenderer
{
    public int Priority => -100;

    public bool CanRender(InertiaRenderContext context) =>
        context.HttpContext.RequestServices.GetService<
            Microsoft.AspNetCore.Mvc.Infrastructure.IActionResultExecutor<
                Microsoft.AspNetCore.Mvc.ViewResult>>() is not null;

    public Task RenderAsync(InertiaRenderContext context)
    {
        var services = context.HttpContext.RequestServices;
        var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(
            context.HttpContext,
            context.HttpContext.Features.Get<Microsoft.AspNetCore.Routing.IRoutingFeature>()?.RouteData
                ?? new Microsoft.AspNetCore.Routing.RouteData(),
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

        var viewName = ResolveMvcRootViewName(actionContext, services, context.RootView);
        var viewResult = new Microsoft.AspNetCore.Mvc.ViewResult { ViewName = viewName };

        return viewResult.ExecuteResultAsync(actionContext);
    }

    private static string ResolveMvcRootViewName(
        Microsoft.AspNetCore.Mvc.ActionContext actionContext,
        IServiceProvider services,
        string configuredViewName)
    {
        var viewEngine = services.GetService<Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine>();
        if (viewEngine is null || ViewExists(actionContext, viewEngine, configuredViewName))
            return configuredViewName;

        if (IsExplicitViewPath(configuredViewName))
            return configuredViewName;

        var folderIndexViewName = $"{configuredViewName}/Index";
        return ViewExists(actionContext, viewEngine, folderIndexViewName)
            ? folderIndexViewName
            : configuredViewName;
    }

    private static bool ViewExists(
        Microsoft.AspNetCore.Mvc.ActionContext actionContext,
        Microsoft.AspNetCore.Mvc.ViewEngines.ICompositeViewEngine viewEngine,
        string viewName)
    {
        var result = IsExplicitViewPath(viewName)
            ? viewEngine.GetView(executingFilePath: null, viewPath: viewName, isMainPage: true)
            : viewEngine.FindView(actionContext, viewName, isMainPage: true);

        return result.Success;
    }

    private static bool IsExplicitViewPath(string viewName) =>
        viewName.StartsWith("~/", StringComparison.Ordinal)
        || viewName.StartsWith("/", StringComparison.Ordinal)
        || viewName.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase);
}