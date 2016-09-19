using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharePointExplorer.Models
{
    public static class SharePointContextExtension
    {
        public static void ExecuteQueryWithIncrementalRetry(this ClientContext context, int retryCount = 5, int delay=10000)
        {
            int retryAttempts = 0;
            int backoffInterval = delay;
            if (retryCount <= 0)
                throw new ArgumentException("Provide a retry count greater than zero.");
            if (delay <= 0)
                throw new ArgumentException("Provide a delay greater than zero.");
            while (retryAttempts < retryCount)
            {
                try
                {
                    context.ExecuteQuery();
                    return;
                }
                catch (WebException wex)
                {
                    var response = wex.Response as HttpWebResponse;
                    if (response != null && response.StatusCode == (HttpStatusCode)429)
                    {
                        //Add delay.
                        System.Threading.Thread.Sleep(backoffInterval);
                        //Add to retry count and increase delay.
                        retryAttempts++;
                        backoffInterval = backoffInterval * 2;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            throw new Exception(string.Format("Maximum retry attempts {0}, have been attempted.", retryCount));
        }

    }
}
