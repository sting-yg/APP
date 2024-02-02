using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace APP.Web
{
    public class WebServer
    {
        private WebApplication? app;

        public Task RunAsync(CancellationToken ct)
        {

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                
            });

            this.app = builder.Build();
            return this.app.StartAsync(ct);
        }


    }
}
