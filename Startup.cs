using ATBPM.Standard.DBHelper;
using Common;
using DockingModel;
using Hangfire;
using JSAJ.Core.Common.DataUtil;
using JSAJ.Core.Common.QRCodes;
using JSAJ.Core.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSwag;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JSAJNanJing
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // ���ÿ���
            services.AddCors(options =>
            {
                options.AddPolicy("AllowSameDomain", build =>
                {
                    build.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
                });
            });
             services.Configure<JwtSettings>(Configuration.GetSection("JwtSettings"));
            JwtSettings setting = new JwtSettings();
            Configuration.Bind("JwtSettings", setting);


            services.Configure<ForbidChangeRole>(Configuration.GetSection("Roles"));
            ForbidChangeRole changeRole = new ForbidChangeRole();
            Configuration.Bind("Roles", changeRole);
            services.AddAuthentication(option =>
            {
                option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(config =>
            {
                config.SecurityTokenValidators.Clear();
                config.SecurityTokenValidators.Add(new TokenValidate());
                config.Events = new JwtBearerEvents()
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Headers["accessToken"];
                        context.Token = token.FirstOrDefault();
                        return Task.CompletedTask;
                    }
                };
            });

            services
              .AddSwaggerDocument(config =>
              {
                  config.Title = "�Ͼ��а�ȫʩ������ϵͳ �ӿ�";
                  config.Description = "�Ͼ��а�ȫʩ������ϵͳ";
                  config.Version = "1.0.0";
                  config.PostProcess = document =>
                  {
                      document.Info.Contact = new OpenApiContact
                      {
                            //Name = "My Company",
                            //Email = "info@mycompany.com",
                            //Url = "https://www.mycompany.com"
                        };
                  };
              });


            // ����Ŀ¼���
            services.AddDirectoryBrowser();
            services.AddHttpClient();


            services.AddSingleton<ICustomWebSocketFactory, CustomWebSocketFactory>();
            services.AddSingleton<ICustomWebSocketMessageHandler, CustomWebSocketMessageHandler>();

            // ���ɶ�ά������
            services.AddSingleton<IQRCode, RaffQRCode>();
            var conn = Configuration.GetConnectionString("JssAnjianmanager");
            // ʡվ���ݿ�
            services.AddDbContext<JssanjianmanagerContext>(
                opt => opt.UseSqlServer(conn,
                // o=>o.UseRelationalNulls(true)
                o => o.UseRowNumberForPaging(false) // sqlserver 2008+��Ҫȡ������Ȼ��ҳЧ����
                ));
            // ʡվ�Խ����ݿ�
            services.AddDbContext<JSAJDokingDbContext>(
                opt => opt.UseSqlServer(Configuration.GetConnectionString("JSAJDokingDbContext"),
                // o=>o.UseRelationalNulls(true)
                o => o.UseRowNumberForPaging(false) // sqlserver 2008+��Ҫȡ������Ȼ��ҳЧ����
                ));
            SqlServerHelper.default_connection_str = conn;
            // redis����
            var csredis = new CSRedis.CSRedisClient(Configuration.GetConnectionString("Redis"));
            RedisHelper.Initialization(csredis);

            // ��ʱ��
            services.AddHangfire(x => x.UseSqlServerStorage(Configuration.GetConnectionString("JssAnjianmanager")));

            services.AddSignalR();
            services.AddControllers()
                .AddNewtonsoftJson(option =>
                {
                    // ͳһ��ʽ������
                    //option.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
                });
        }



        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("AllowSameDomain");

            //app.UseAuthentication();
            app.UseOpenApi();
            app.UseSwaggerUi3();


            //app.UseHttpsRedirection();

            // websocket


            app.UseRouting();

            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024
            };
            //webSocketOptions.AllowedOrigins.Add(Configuration.GetConnectionString("WebSocket"));
            app.UseWebSockets(webSocketOptions);
            app.UseCustomWebSocketManager();
            // ����Ŀ¼���
            //app.UseDirectoryBrowser();
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();
            //�ļ�����������
            app.UseFileMiddleware();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            //hangfire��ʱ��
            //app.UseHangfireServer();
            //app.UseHangfireDashboard("/hangfire", new DashboardOptions
            //{
            //    Authorization = new[] { new HangfireAuthorizationFilter() }
            //});
        }
    }
}
