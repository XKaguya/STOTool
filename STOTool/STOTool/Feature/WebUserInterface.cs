using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using STOTool.Controllers;
using STOTool.Enum;
using STOTool.Generic;
using STOTool.Settings;

namespace STOTool.Feature
{
    public class WebUserInterface
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(options =>
                    {
                        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                        options.LoginPath = "/Process/Authentication";
                        options.SlidingExpiration = true;
                    });
                services.AddAuthorization();
                
                services.AddCors(options =>
                {
                    options.AddPolicy("AllowAnyOrigin",
                        builder => builder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader());
                });

                services.AddRazorPages().AddRazorRuntimeCompilation();
                services.AddControllersWithViews();
                services.AddRouting();
                services.AddControllers().AddApplicationPart(typeof(ProcessController).Assembly);
            }

            public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
                if (System.Enum.Parse<ProgramLevel>(GlobalVariables.ProgramLevel) == ProgramLevel.Debug)
                {
                    app.UseDeveloperExceptionPage();
                }
                app.UseStaticFiles();
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseCors("AllowAnyOrigin");
                
                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });
                
                app.UseEndpoints(endpoints => 
                { 
                    endpoints.MapControllers();
                });
                
                app.Use((RequestDelegate next) => async (context) =>
                {
                    var path = context.Request.Path.Value;
                    
                    if (string.IsNullOrEmpty(path) || path.Equals("/"))
                    {
                        if (context.User.Identity.IsAuthenticated)
                        {
                            Logger.Error($"User is authenticated.");
                            context.Response.Redirect("/Process/Index");
                        }
                        else
                        {
                            Logger.Error($"User is not authenticated.");
                            context.Response.Redirect("/Process/Authentication");
                        }
                    }

                    var assembly = Assembly.GetExecutingAssembly();
                    string resourceName = GetResourceName(path);
                    if (resourceName != null)
                    {
                        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                        {
                            if (stream != null)
                            {
                                context.Response.ContentType = GetContentType(resourceName);
                                await stream.CopyToAsync(context.Response.Body);
                            }
                        }
                    }

                    context.Response.StatusCode = 404;
                });
            }

            private string GetResourceName(string requestPath)
            {
                var resourceName = "STOTool.WebUI" + requestPath.Replace("/", ".");
                if (requestPath == "/" || requestPath.EndsWith("/index.html"))
                {
                    resourceName = "STOTool.WebUI.index.html";
                }

                return resourceName;
            }

            private string GetContentType(string resourceName)
            {
                if (resourceName.EndsWith(".html"))
                    return "text/html";
                if (resourceName.EndsWith(".js"))
                    return "application/javascript";
                if (resourceName.EndsWith(".css"))
                    return "text/css";
                if (resourceName.EndsWith(".png"))
                    return "image/png";
                if (resourceName.EndsWith(".jpg") || resourceName.EndsWith(".jpeg"))
                    return "image/jpeg";
                if (resourceName.EndsWith(".gif"))
                    return "image/gif";

                return "application/octet-stream";
            }
        }
    }
}