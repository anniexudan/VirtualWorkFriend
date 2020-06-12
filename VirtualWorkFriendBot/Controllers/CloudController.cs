using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace VirtualWorkFriendBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CloudController : ControllerBase
    {
        private IServiceProvider _serviceProvider;
        private IWebHostEnvironment _env;
        private IConfiguration configuration;

        [HttpPost]
        public async Task<object> Index()
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("ContentRootPath", _env.ContentRootPath);

            
            return data;
        }
        public CloudController(IConfiguration config, IServiceProvider serviceProvider, IWebHostEnvironment env)
        {
            configuration = config;
            _serviceProvider = serviceProvider;
            _env = env;
        }
    }
}