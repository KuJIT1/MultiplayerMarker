namespace MultiplayerMarker
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    using MultiplayerMarker.Core;
    using MultiplayerMarker.DbModel;
    using MultiplayerMarker.Hub;
    using MultiplayerMarker.Logger;

    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddSingleton<GameCore>();
            services.AddSingleton<Engine>();
            services.AddSingleton<DbUserActionLogger>();
            services.AddSignalR();

            //TODO:проблема с освобождением соединения при неожиданном завершении работы. Изменить жизненный цикл
            services.AddDbContext<ApplicationContext>(options =>
            {
                options.UseNpgsql(this.configuration.GetConnectionString("DefaultConnection"));
            }, ServiceLifetime.Singleton);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapHub<GameHub>("gamehub");
            });
        }
    }
}
