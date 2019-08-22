using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ImageUploadDemo
{
    public class ConfigHelper
    {
        public string ConnectionString { get; set; }
        public string ContainerName { get; set; }
        public string AccessKey { get; set; }


        public ConfigHelper(IConfiguration config)
        {
            var apiConfig = config.GetSection("BlobStorage");

            ConnectionString = apiConfig["ConnectionString"];
            ContainerName = apiConfig["ContainerName"];
            AccessKey = apiConfig["AccessKey"];
        }
    }
}
