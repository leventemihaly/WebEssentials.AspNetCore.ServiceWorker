using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace WebEssentials.AspNetCore.Pwa
{
    /// <summary>
    /// IApplicationBuilder extensions to register routes for manifest.webmanifest, serviceworker.js and offline.html
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Register routes for manifest.webmanifest, serviceworker.js and offline.html
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder AddPwaRoutes(this IApplicationBuilder app)
        {
            return app.UseEndpoints(endpoints =>
            {
                var options = endpoints.ServiceProvider.GetRequiredService<PwaOptions>();
                var customServiceworker = endpoints.ServiceProvider.GetRequiredService<RetrieveCustomServiceworker>();

                endpoints.Map(Constants.ServiceworkerRoute, context => ServiceWorkerHandlerAsync(context, options, customServiceworker));
                endpoints.Map(Constants.Offlineroute, OfflineHandlerAsync);
                endpoints.Map(Constants.WebManifestRoute, context => WebManifestHandlerAsync(context, options));
            });
        }

        private static async Task ServiceWorkerHandlerAsync(HttpContext context, PwaOptions options, RetrieveCustomServiceworker customServiceworker)
        {
            context.Response.ContentType = "application/javascript; charset=utf-8";
            context.Response.Headers[HeaderNames.CacheControl] = $"max-age={options.ServiceWorkerCacheControlMaxAge}";

            if (options.Strategy == ServiceWorkerStrategy.CustomStrategy)
            {
                string js = customServiceworker.GetCustomServiceworker(options.CustomServiceWorkerStrategyFileName);
                await context.Response.WriteAsync(InsertStrategyOptions(js, options));
            }
            else
            {
                string fileName = options.Strategy + ".js";
                Assembly assembly = typeof(ApplicationBuilderExtensions).Assembly;
                Stream resourceStream = assembly.GetManifestResourceStream($"WebEssentials.AspNetCore.Pwa.ServiceWorker.Files.{fileName}");

                using (var reader = new StreamReader(resourceStream))
                {
                    string js = await reader.ReadToEndAsync();
                    await context.Response.WriteAsync(InsertStrategyOptions(js, options));
                }
            }
        }

        private static async Task OfflineHandlerAsync(HttpContext context)
        {
            context.Response.ContentType = "text/html";

            Assembly assembly = typeof(ApplicationBuilderExtensions).Assembly;
            Stream resourceStream = assembly.GetManifestResourceStream("WebEssentials.AspNetCore.Pwa.ServiceWorker.Files.offline.html");

            using (var reader = new StreamReader(resourceStream))
            {
                await context.Response.WriteAsync(await reader.ReadToEndAsync());
            }
        }

        private static async Task WebManifestHandlerAsync(HttpContext context, PwaOptions options)
        {
            var wm = context.RequestServices.GetService<WebManifest>();
            if (wm == null)
            {
                throw new BadHttpRequestException("", 404);
            }

            context.Response.ContentType = "application/manifest+json; charset=utf-8";

            context.Response.Headers[HeaderNames.CacheControl] = $"max-age={options.WebManifestCacheControlMaxAge}";

            await context.Response.WriteAsync(wm.RawJson);
        }

        private static string InsertStrategyOptions(string javascriptString, PwaOptions options)
        {
            return javascriptString
                .Replace("{version}", options.CacheId + "::" + options.Strategy)
                .Replace("{routes}", string.Join(",", options.RoutesToPreCache.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(r => "'" + r.Trim() + "'")))
                .Replace("{offlineRoute}", options.BaseRoute + options.OfflineRoute)
                .Replace("{ignoreRoutes}", string.Join(",", options.RoutesToIgnore.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(r => "'" + r.Trim() + "'")));
        }
    }
}
