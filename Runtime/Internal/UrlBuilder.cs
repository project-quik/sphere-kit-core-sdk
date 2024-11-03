using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;

namespace SphereKit.Utils
{
    internal class UrlBuilder
    {
        private readonly UriBuilder _uriBuilder;
        private readonly NameValueCollection _query;

        private UrlBuilder(string url)
        {
            _uriBuilder = new UriBuilder(url);
            _query = HttpUtility.ParseQueryString("");
        }

        internal static UrlBuilder New(string url)
        {
            return new UrlBuilder(url);
        }

        internal UrlBuilder SetQueryParameters(Dictionary<string, string> parameters)
        {
            foreach (var p in parameters)
            {
                _query.Set(p.Key, p.Value);
            }

            return this;
        }

        public override string ToString()
        {
            _uriBuilder.Query = _query.ToString();
            return _uriBuilder.Uri.ToString();
        }
    }
}