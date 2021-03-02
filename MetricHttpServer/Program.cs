using MetricHttpServer.Data;
using System;
using System.Net;
using System.Text;

namespace MetricHttpServer
{
    class Program
    {
        private static HttpListener httpListener;

        /// <summary>
        /// Main entry point for the application
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var port = System.Configuration.ConfigurationManager.AppSettings["metricsHttpServerPort"];

            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://127.0.0.1:{port}/");
            httpListener.Start();

            ProcessFirstOrNextRequest();
            Console.WriteLine("Press anny key to exit application...");
            Console.ReadKey();
        }

        /// <summary>
        /// Process http requests
        /// </summary>
        private static void ProcessFirstOrNextRequest()
        {
            // Start to get connection context
            var asyncResult = httpListener.BeginGetContext(OnGetContext, httpListener);
        }

        /// <summary>
        /// Called when request has been made
        /// </summary>
        /// <param name="asyncResult"></param>
        private static void OnGetContext(IAsyncResult asyncResult)
        {
            var context = httpListener.EndGetContext(asyncResult);
            var urlExists = false;

            // There are only two types of urls avaiable
            // Made simple check to get data for each of them
            if (context.Request.Url.Segments.Length == 2)
            {
                // If url is http://127.0.0.1:{port}/sensors
                if (context.Request.Url.Segments[1] == "sensors")
                {
                    context.Response.StatusCode = 200;
                    context.Response.StatusDescription = "OK";
                    context.Response.ContentType = "application/json";

                    try
                    {
                        var sensorList = MetricsData.GetSensorListWithLastMeasure();
                        var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(sensorList);
                        var byteData = Encoding.UTF8.GetBytes(jsonData);
                        context.Response.OutputStream.Write(byteData, 0, byteData.Length);
                    }
                    catch
                    {
                        context.Response.StatusCode = 500;
                        context.Response.StatusDescription = "Internal Server Error";
                    }
                    context.Response.Close();
                    urlExists = true;
                }
            }
            else if (context.Request.Url.Segments.Length == 3)
            {
                // If url is http://127.0.0.1:{port}/measures/2019-09-09
                if (context.Request.Url.Segments[1].Replace("/", string.Empty) == "measures")
                {
                    var date = context.Request.Url.Segments[2];
                    context.Response.StatusCode = 200;
                    context.Response.StatusDescription = "OK";
                    context.Response.ContentType = "application/json";

                    try
                    {
                        var measureList = MetricsData.GetSensorMeasuresForDate(date);
                        var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(measureList);
                        var byteData = Encoding.UTF8.GetBytes(jsonData);
                        context.Response.OutputStream.Write(byteData, 0, byteData.Length);
                    }
                    catch
                    {
                        context.Response.StatusCode = 500;
                        context.Response.StatusDescription = "Internal Server Error";
                    }

                    context.Response.Close();
                    urlExists = true;
                }
            }

            // If wrong url specified return 404 Not Found error
            if (!urlExists)
            {
                context.Response.StatusCode = 404;
                context.Response.StatusDescription = "Not Found";
                context.Response.Close();
            }

            // Process next request
            ProcessFirstOrNextRequest();
        }
    }
}
