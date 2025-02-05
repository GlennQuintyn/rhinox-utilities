using System;
using System.Text;
using System.Threading.Tasks;
using Rhinox.Lightspeed;
using Rhinox.Perceptor;
using UnityEngine;
using UnityEngine.Networking;
using Utility = Rhinox.Lightspeed.Utility;

namespace Rhinox.Utilities
{
    public class ApiHelper
    {
        private string _baseUrl;
        
        public ApiHelper(string url)
        {
            _baseUrl = url;
        }
        
        public delegate void WebRequestAction(UnityWebRequest request);

        public async Task Get(string path, WebRequestAction handleRequest)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                await request.SendWebRequest();
                
                if (request.IsRequestValid(out string error))
                    handleRequest?.Invoke(request);
                else
                    PLog.Error<UtilityLogger>(error);
            }
        }

        public async Task<T> Get<T>(string path)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                await request.SendWebRequest();
                
                if (request.IsRequestValid(out string error))
                    return request.ParseJsonResult<T>();
                else
                    PLog.Error<UtilityLogger>(error);
                
                return default;
            }
        }

        public T GetSync<T>(string path)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = UnityWebRequest.Get(uri))
            {
                // Request and wait for the desired page.
                var op = request.SendWebRequest();
                while (!op.isDone) { }

                return request.ParseJsonResult<T>();
            }
        }

        public async Task Post(string path, string json, WebRequestAction handleRequest = null)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = new UnityWebRequest(uri, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Request and wait for the desired page.
                await request.SendWebRequest();

                if (request.IsRequestValid(out string error))
                    handleRequest?.Invoke(request);
                else
                    PLog.Error<UtilityLogger>(error);
            }
        }
        
        
        public async Task Post(string path, object o, WebRequestAction handleRequest = null)
            => await Post(path, Utility.ToJson(o, true), handleRequest);

        public async Task<T> Post<T>(string path, string json)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = new UnityWebRequest(uri, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Request and wait for the desired page.
                await request.SendWebRequest();

                return request.ParseJsonResult<T>();
            }
        }

        public async Task<TResult> Post<TResult>(string path, object o)
            => await Post<TResult>(path, Utility.ToJson(o, true));

        public TResult PostSync<TResult>(string path, string json)
        {
            string uri = $"{_baseUrl}{path}";
            using (var request = new UnityWebRequest(uri, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                // Request and wait for the desired page.
                var op = request.SendWebRequest();
                while (!op.isDone) { }

                return request.ParseJsonResult<TResult>();
            }
        }
        
        public TResult PostSync<TResult>(string path, object o)
            => PostSync<TResult>(path, Utility.ToJson(o, true));

    }
}