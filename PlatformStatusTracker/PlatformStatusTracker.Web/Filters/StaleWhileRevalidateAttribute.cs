using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace PlatformStatusTracker.Web.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class StaleWhileRevalidateAttribute : Attribute, IFilterFactory
{
    bool IFilterFactory.IsReusable => true;

    public string CacheProfileName { get; init; }
    public int StaleWhileRevalidateDuration { get; init; }

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var responseCacheAttr = new ResponseCacheAttribute() { CacheProfileName = CacheProfileName };
        var responseCacheFilter = responseCacheAttr.CreateInstance(serviceProvider);
        var mvcOptions = serviceProvider.GetRequiredService<IOptions<MvcOptions>>();
        var cacheProfile = responseCacheAttr.GetCacheProfile(mvcOptions.Value);

        return new StaleWhileRevalidateFilter(cacheProfile, StaleWhileRevalidateDuration, responseCacheFilter);
    }

    public class StaleWhileRevalidateFilter : IFilterMetadata, IActionFilter
    {
        private readonly IActionFilter _cacheFilter;
        private readonly CacheProfile _cacheProfile;
        private readonly int _staleWhileRevalidateDuration;

        public StaleWhileRevalidateFilter(CacheProfile cacheProfile, int staleWhileRevalidateDuration, IFilterMetadata cacheFilter)
        {
            _cacheProfile = cacheProfile;
            _staleWhileRevalidateDuration = staleWhileRevalidateDuration;
            _cacheFilter = cacheFilter as IActionFilter ?? throw new InvalidOperationException("The inner filter must be IActionFilter.");
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            _cacheFilter.OnActionExecuted(context);
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            _cacheFilter.OnActionExecuting(context);

            if (_cacheProfile.NoStore ?? false) return;
            if (_cacheProfile.Location is ResponseCacheLocation.None) return;

            context.HttpContext.Response.Headers.CacheControl += $",stale-while-revalidate={_staleWhileRevalidateDuration}";
        }
    }
}