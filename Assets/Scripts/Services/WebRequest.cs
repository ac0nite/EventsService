using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GameplayServices.Common
{
    public interface IWebRequest
    {
        UniTask<bool> PostAsync(string url, string jsonData);
    }
    
    public class WebRequest : IWebRequest
    {
        private readonly TimeSpan _timeout;

        public WebRequest(float timeout = float.MaxValue)
        {
            _timeout = TimeSpan.FromSeconds(timeout);
        }
        public async UniTask<bool> PostAsync(string url, string jsonData)
        {
            using var cancellationTokenSource = new CancellationTokenSource(_timeout);
            var request = WebRequestPattern.Post(url, jsonData);
            try
            {
                await request.SendWebRequest().WithCancellation(cancellationTokenSource.Token);
                return RequestHandler(request);
            }
            catch (OperationCanceledException)
            {
                Debug.LogError($"Request to {url} was canceled due to timeout.");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred while sending the request: {ex.Message}");
                return false;
            }
        }

        private bool RequestHandler(UnityWebRequest request)
        {
            if (request.isDone && 
                request.result == UnityWebRequest.Result.Success && 
                request.responseCode == WebRequestPattern.ResponseCode.Success)
                return true;
            
            if (!request.isDone)
                request.Abort();
            
#if UNITY_EDITOR
            if (!request.isDone)
                Debug.LogError($"Request timeout: {request.url}");
            else
                Debug.LogError($"{request.error} Code:{request.responseCode} Handler:{request.downloadHandler.text}");
#endif

            return false;
        }
    }

    public static class WebRequestPattern
    {
        private static readonly string nameHeader = "Content-Type";
        private static readonly string valueHeaderJson = "application/json";
        private static readonly string postMethod = "POST";
        
        public static UnityWebRequest Post(string url, string jsonData)
        {
            var request = new UnityWebRequest(url, postMethod);
            var bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader(nameHeader, valueHeaderJson);
            return request;
        }

        public static class ResponseCode
        {
            public const long Success = 200;
        }
    }
}