using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace TemplateApiNodeJS
{
    public class ApiManager : MonoBehaviour
    {
        // Public reference to an instance of the ApiConfig ScriptableObject.
        public ApiConfig apiConfig;

        // Optional debug flag to enable/disable logging in production environments.
        public bool debugMode = true;

        // This method initiates a GET request to the specified API endpoint.
        public void GetRequest(string endpoint, System.Action<string> callback)
        {
            // Validate configuration and URLs.
            if (!ValidateApiConfig()) return;

            // Construct the full URL.
            string fullUrl = apiConfig.GetBaseUrl() + endpoint;

            // Log the request URL if debugging is enabled.
            if (debugMode) Debug.Log("Making GET request to: " + fullUrl);

            // Start the coroutine to perform the GET request.
            StartCoroutine(RequestCoroutine(fullUrl, "GET", null, callback));
        }

        // This method initiates a POST request to the specified API endpoint.
        public void PostRequest(string endpoint, string jsonData, System.Action<string> callback)
        {
            // Validate configuration and URLs.
            if (!ValidateApiConfig()) return;

            // Construct the full URL.
            string fullUrl = apiConfig.GetBaseUrl() + endpoint;

            // Log the request URL and data if debugging is enabled.
            if (debugMode) Debug.Log("Making POST request to: " + fullUrl + " with data: " + jsonData);

            // Start the coroutine to perform the POST request.
            StartCoroutine(RequestCoroutine(fullUrl, "POST", jsonData, callback));
        }

        // This method can be extended to support PUT, DELETE, etc.
        public void SendCustomRequest(string endpoint, string method, string jsonData, System.Action<string> callback)
        {
            // Validate configuration and URLs.
            if (!ValidateApiConfig()) return;

            // Construct the full URL.
            string fullUrl = apiConfig.GetBaseUrl() + endpoint;

            // Log the request URL and method if debugging is enabled.
            if (debugMode) Debug.Log("Making " + method + " request to: " + fullUrl);

            // Start the coroutine to perform the custom request.
            StartCoroutine(RequestCoroutine(fullUrl, method, jsonData, callback));
        }

        // This private method handles all types of web requests (GET, POST, PUT, DELETE).
        private IEnumerator RequestCoroutine(string uri, string method, string jsonData, System.Action<string> callback)
        {
            UnityWebRequest webRequest;

            // If it's a POST or PUT request, configure the request to send JSON data.
            if (method == "POST" || method == "PUT")
            {
                webRequest = new UnityWebRequest(uri, method);
                byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
            }
            else
            {
                // For GET, DELETE, or other types of requests.
                webRequest = UnityWebRequest.Get(uri);
            }

            // Send the web request and wait for the response asynchronously.
            yield return webRequest.SendWebRequest();

            // Check for errors with the request.
            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error during request to " + uri + ": " + webRequest.error);
            }
            else
            {
                if (debugMode) Debug.Log("Received response: " + webRequest.downloadHandler.text);
                callback?.Invoke(webRequest.downloadHandler.text);
            }
        }

        // This method validates that the ApiConfig and URLs are correctly set.
        private bool ValidateApiConfig()
        {
            if (apiConfig == null)
            {
                Debug.LogError("ApiConfig is not assigned in the inspector! Please assign it.");
                return false;
            }

            // Validate the base URL depending on the environment.
            string baseUrl = apiConfig.GetBaseUrl();
            if (string.IsNullOrEmpty(baseUrl))
            {
                Debug.LogError("The base URL is not set in ApiConfig. Please ensure it is configured correctly.");
                return false;
            }

            return true;
        }
    }
}