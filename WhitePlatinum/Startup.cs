using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace WhitePlatinum {
	public class Startup {
		/*IConfiguration Config;

		public Startup(IConfiguration Configuration) {
			Config = Configuration;
		}*/

		public void ConfigureServices(IServiceCollection Services) {
			Services.AddMvc((MvcOptions) => {
				MvcOptions.EnableEndpointRouting = false;
			}).AddNewtonsoftJson((Opt) => {

			});

			Services.Configure<IISServerOptions>(Opt => {
				Opt.AllowSynchronousIO = true;
			});

			Services.Configure<KestrelServerOptions>(Opt => {
				Opt.AllowSynchronousIO = true;
			});

			Services.AddCors(Opt => {
				Opt.AddDefaultPolicy(Builder => Builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
			});

			//Configuration.
			//Services.Configure<PlatinumConfig>(Config.GetSection(nameof(PlatinumConfig)));
		}

		public void Configure(IApplicationBuilder App, IWebHostEnvironment Env) {
			if (Env.IsDevelopment())
				App.UseDeveloperExceptionPage();

			//App.UseHttpsRedirection();
			App.UseMvc();
			App.UseCors();
		}
	}
}
