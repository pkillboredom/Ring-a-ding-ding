using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Http;
using Ring_a_ding_ding.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ring_a_ding_ding.Services.HueControl;

namespace Ring_a_ding_ding
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddHttpClient(); 
            builder.Services.RemoveAll<IHttpMessageHandlerBuilderFilter>();
            builder.Services.AddSingleton<SignalRLoggerConfiguration>();
            builder.Services.AddSingleton<HueControlService>();
            builder.Logging.AddSimpleConsole(options =>
            {
                options.IncludeScopes = true;
                options.SingleLine = true;
                options.TimestampFormat = "hh:mm:ss ";
            });
            builder.Services.AddSingleton<VideoExecutor>();

            builder.Configuration
                .AddJsonFile("huecontrol.json", optional: true, reloadOnChange: true);

#if !DEBUG
            builder.WebHost.UseUrls("http://0.0.0.0:5000");
#endif


            var app = builder.Build();

            var lf = app.Services.GetRequiredService<ILoggerFactory>();
            lf.AddProvider(new SignalRLoggerProvider(app.Services.GetRequiredService<SignalRLoggerConfiguration>()));

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");
            app.MapHub<BlazorChatSampleHub>(BlazorChatSampleHub.HubUrl);

            app.Run();
        }
    }
}