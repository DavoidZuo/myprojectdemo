using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSAJNanJing
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //CreateHostBuilder(args).Build().Run();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
             //Host.CreateDefaultBuilder(args)
             //    .ConfigureWebHostDefaults(webBuilder =>
             //    {
             //        webBuilder.UseStartup<Startup>();
             //    });

             Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(serverOptions =>
                    {
                        // Set properties and call methods on options
                    }).UseUrls("http://*:7600")
                    .UseIIS()
                    //.UseStartup<Startup>().ConfigureAppConfiguration((host, config) =>
                    //{
                    //    config.AddJsonFile($"RateLimitConfig.json", optional: true, reloadOnChange: true);
                    //})
                    .UseStartup<Startup>();
                });
    }
}
