using System;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Filtering;
using App.Metrics.Formatters.InfluxDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppMetricsTest
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var filter = new MetricsFilter().WhereType(MetricType.Timer);
            var metrics = AppMetrics.CreateDefaultBuilder()                
                        .Report.ToInfluxDb(options => {
                            options.InfluxDb.BaseUri = new Uri("http://hassio.local:8086");
                            options.InfluxDb.Database = "metricsdatabase";
                            options.InfluxDb.UserName = "monitor";
                            options.InfluxDb.Password = "monitor";
                            options.InfluxDb.RetentionPolicy = "rp";
                            options.InfluxDb.CreateDataBaseIfNotExists = true;
                            options.HttpPolicy.BackoffPeriod = TimeSpan.FromSeconds(30);
                            options.HttpPolicy.FailuresBeforeBackoff = 5;
                            options.HttpPolicy.Timeout = TimeSpan.FromSeconds(10);
                            options.MetricsOutputFormatter = new MetricsInfluxDbLineProtocolOutputFormatter();
                            options.Filter = filter;
                            options.FlushInterval = TimeSpan.FromSeconds(20);
                        })
                .Build();
            
            

            Task.WhenAll(metrics.ReportRunner.RunAllAsync());

            services.AddMetrics(metrics);
            services.AddMetricsTrackingMiddleware();
            services.AddMetricsReportingHostedService();
            
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();            

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            //app.UseMetricsAllMiddleware();
            app.UseMetricsErrorTrackingMiddleware(); 
            app.UseMetricsActiveRequestMiddleware();
            app.UseMetricsRequestTrackingMiddleware(); 
        }
    }
}
