using System;
using System.Collections.Generic;
using MongoDB.Bson;

namespace VBin.AspNet
{
    [Serializable]
    public class AspNetVirtualSiteConfig
    {
        public AspNetVirtualSiteConfig()
        {
            AspNetAssemblies = new List<string>();
        }

        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public List<string> AspNetAssemblies { get; set; }
    }
}