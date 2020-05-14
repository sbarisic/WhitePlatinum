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

namespace WhitePlatinum {
	public class Startup {
		public void ConfigureServices(IServiceCollection Services) {
			Services.AddMvc((MvcOptions) => {
				MvcOptions.EnableEndpointRouting = false;
			}).AddNewtonsoftJson((Opt) => { 

			});
		}

		public void Configure(IApplicationBuilder App, IWebHostEnvironment Env) {
			if (Env.IsDevelopment())
				App.UseDeveloperExceptionPage();

			App.UseMvc();
		}
	}
}
