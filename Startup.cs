using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ACServerStatusCheck
{
	public class Startup
	{
		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapGet("/ping", async context => await context.Response.WriteAsync("pong"));
				endpoints.MapGet("/status/{server:required}", async context =>
				{
					var serverName = context.Request.RouteValues["server"];
					if (string.Equals(serverName, "ptr"))
					{
						var up = await serverCheckAsync("play.metaverse.ac", 9222);
						context.Response.StatusCode = up ? 200 : 504;
					}
					else
					{
						context.Response.StatusCode = 404;
					}
				});
			});
		}

		/* Implementation adapted from Thwargle:
		 * https://github.com/Thwargle/ThwargLauncher/tree/master/ThwargLauncher/ThwargLauncher/ServerMonitor
		 */
		private async Task<bool> serverCheckAsync(string endpoint, int port, int timeoutSec = 5)
		{
			using UdpClient udpClient = new UdpClient();
			try
			{
				udpClient.Connect(endpoint, port);
				IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
				byte[] sendBytes = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x93, 0x00, 0xd0, 0x05, 0x00, 0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x04, 0x00, 0x31, 0x38, 0x30, 0x32, 0x00, 0x00, 0x34, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3e, 0xb8, 0xa8, 0x58, 0x1c, 0x00, 0x61, 0x63, 0x73, 0x65, 0x72, 0x76, 0x65, 0x72, 0x74, 0x72, 0x61, 0x63, 0x6b, 0x65, 0x72, 0x3a, 0x6a, 0x6a, 0x39, 0x68, 0x32, 0x36, 0x68, 0x63, 0x73, 0x67, 0x67, 0x63, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
				udpClient.Send(sendBytes, sendBytes.Length);
				var receiveTask = udpClient.ReceiveAsync();
				var tsk = await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(timeoutSec)));
				if (tsk == receiveTask)
				{
					return true;
				}
			}
			catch (Exception) { }

			return false;
		}
	}
}
