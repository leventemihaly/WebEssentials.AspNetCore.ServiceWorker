﻿using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Hosting;
using System;

namespace WebEssentials.AspNetCore.Pwa
{
    internal class ServiceWorkerTagHelperComponent : TagHelperComponent
    {
        private string _script;

        private IWebHostEnvironment _env;
        private IHttpContextAccessor _accessor;
        private PwaOptions _options;

        public ServiceWorkerTagHelperComponent(IWebHostEnvironment env, IHttpContextAccessor accessor, PwaOptions options)
        {
            _env = env;
            _accessor = accessor;
            _options = options;

            _script = "\r\n\t<script" + (_options.EnableCspNonce ? Constants.CspNonce : string.Empty) + ">'serviceWorker'in navigator&&navigator.serviceWorker.register('" + options.BaseRoute + Constants.ServiceworkerRoute + "', { scope: '" + options.BaseRoute + "/' })</script>";
        }

        /// <inheritdoc />
        public override int Order => -1;

        /// <inheritdoc />
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (!_options.RegisterServiceWorker)
            {
                return;
            }

            if (string.Equals(context.TagName, "body", StringComparison.OrdinalIgnoreCase))
            {
                if ((_options.AllowHttp || _accessor.HttpContext.Request.IsHttps) || _env.IsDevelopment())
                {
                    output.PostContent.AppendHtml(_script);
                }
            }
        }
    }
}
