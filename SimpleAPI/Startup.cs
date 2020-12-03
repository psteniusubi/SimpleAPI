using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using SimpleAPI.OIDC;
using System.Net.Http;

namespace SimpleAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient<HttpClient>();
            services.AddSingleton<IntrospectionClient>();
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder => builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .WithHeaders(HeaderNames.Authorization)
                        .WithExposedHeaders(HeaderNames.WWWAuthenticate));
            });
            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseRouting();
            app.UseCors();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
