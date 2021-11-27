using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace MonitorIotaNode
{
    public class NodeEndpoint
    {
        public Uri Uri { get; private set; }

        private RestClient client;
        private RestRequest request;

        public NodeEndpoint(Uri uri)
        {
            this.Uri = uri;
            client = new RestClient(Uri);
            request = new RestRequest("", DataFormat.None);
        }

        public NodeInfo RetrieveNodeInfo()
        {
            IRestResponse response = client.Get(request);
            if (!response.IsSuccessful) throw new Exception($"Error requesting {Uri.AbsoluteUri}");
            string json = response.Content;
            return JsonConvert.DeserializeObject<NodeInfo>(json);
        }

        public override bool Equals(object obj)
        {
            Uri otherUri = (Uri)obj;
            return Uri.Equals(otherUri);
        }

        public override int GetHashCode()
        {
            return Uri.GetHashCode();
        }
    }
}
