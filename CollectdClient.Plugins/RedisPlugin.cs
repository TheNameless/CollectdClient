﻿using System.Collections.Generic;
using System.Threading.Tasks;
using BookSleeve;
using CollectdClient.Core;
using CollectdClient.Core.Plugins;
using Newtonsoft.Json.Linq;

namespace CollectdClient.Plugins
{
    [Plugin("redis", 5)]
    public class RedisPlugin : IReadInterface, IConfigInterface, IInitInterface
    {

        private IDictionary<string, RedisConnection> connections;

        public RedisPlugin()
        {
            connections = new Dictionary<string, RedisConnection>();
        }

        public void Register(PluginContext context)
        {
        }

        public bool Init()
        {
            foreach (var con in connections.Values)
            {
                con.Open();
            }
            return true;
        }

        public void Config(JToken config)
        {
            foreach (var child in config.Children())
            {
                string hostName = ((JProperty) child).Name;
                string host = child.First["Host"].Value<string>();
                int port = child.First["Port"].Value<int>();
                int timeout = child.First["Timeout"].Value<int>();

                connections.Add(hostName, ConnectionUtils.Connect(string.Format("{0}:{1}", host, port), false));
            }
        }

        public async Task<bool> Read()
        {   
            foreach (var con in connections)
            {
                var info = await con.Value.Server.GetInfo();
                Submit(con.Key, "memory", "used", double.Parse(info["used_memory"]));
                Submit(con.Key, "current_connections", "clients", double.Parse(info["connected_clients"]));
                Submit(con.Key, "current_connections", "slaves", double.Parse(info["connected_slaves"]));
                //Submit(con.Key, "volatile_changes", null, double.Parse(info["changes_since_last_save"]));
            }


            return true;
        }

        private void Submit(string name, string type, string typeInstance, double value)
        {
            var vl = ValueList.Build()
                              .Plugin("redis")
                              .PluginInstance(name)
                              .Type(type)
                              .TypeInstance(typeInstance)
                              .AddValue(value)
                              .Build();

            Collectd.DispatchValues(vl);
            
        }
    }
}
