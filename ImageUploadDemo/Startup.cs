using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ImageUploadDemo.Hubs;

namespace ImageUploadDemo
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
            services.AddCors(options => options.AddPolicy("CorsPolicy",
                builder =>
                {
                    builder.AllowAnyMethod().AllowAnyHeader()
                        .WithOrigins(
                            "http://localhost:3000", 
                            "https://kaslide.azurewebsites.net", 
                            "https://ka-upload.azurewebsites.net",
                            "http://kineogandreas.com",
                            "https://kineogandreas.com",
                            "http://andreasogkine.no",
                            "https://andreasogkine.no"
                            )
                        .AllowCredentials();
                })
            );

            services.AddMvc();
            services.AddTransient<ConfigHelper>();
            services.AddTransient<NotifyService>();

            services.AddSignalR();
        }
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}

            app.UseCors("CorsPolicy");

            app.UseSignalR(routes =>
            {
                routes.MapHub<ImageHub>("/imageHub");
            });
            app.UseMvc();
        }
    }
}
