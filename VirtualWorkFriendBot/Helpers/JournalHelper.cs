using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VirtualWorkFriendBot.Helpers
{
    public static class JournalHelper
    {
        public static IConfiguration Configuration { get; set; }
        public static void Configure(IConfiguration configuration)
        {
            Configuration = configuration.GetSection("JournalSettings");
        }
        public static string DefaultNotebookName
        {
            get
            {
                return Configuration["DefaultNotebookName"];
            }
        }
    }
}
