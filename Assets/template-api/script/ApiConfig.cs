using UnityEngine;

namespace TemplateApiNodeJS
{
    // The CreateAssetMenu attribute enables the creation of instances of this ScriptableObject
    // directly from the Unity editor. This provides an easy way to create API configuration files
    // without manually coding them. It helps streamline setting up the API configuration across
    // different environments (development, staging, production).
    [CreateAssetMenu(fileName = "ApiConfig", menuName = "MyApi/ApiConfig", order = 1)]
    public class ApiConfig : ScriptableObject
    {
        // The 'baseUrl' variable is used to store the base URL of your API for the development environment.
        // This value typically includes the protocol (http), domain (localhost), and the API entry point (usually '/api/').
        //
        // Example usage for development:
        // - 'http://localhost:3000/api/'
        [Tooltip("Base URL of the API for Development, e.g., http://localhost:3000/api/")]
        public string developmentBaseUrl = "http://localhost:3000/api/";

        // The 'stagingBaseUrl' variable stores the base URL for the staging environment.
        // The staging environment is typically used for testing intermediate versions of your API before deployment.
        [Tooltip("Base URL of the API for Staging, e.g., https://staging.myapi.com/api/")]
        public string stagingBaseUrl = "https://staging.myapi.com/api/";

        // The 'productionBaseUrl' variable stores the base URL for the production environment.
        // The production environment is where the live API runs, typically using HTTPS.
        [Tooltip("Base URL of the API for Production, e.g., https://api.production.com/api/")]
        public string productionBaseUrl = "https://api.productiondomain.com/api/";

        // This enum allows the user to switch between environments directly from the Unity Inspector.
        // Options are Development, Staging, and Production.
        public enum Environment
        {
            Development,
            Staging,
            Production
        }

        // The current environment for the API.
        // The base URL will change dynamically depending on this setting.
        [Tooltip("Choose the environment. The correct base URL will be used based on the selected environment.")]
        public Environment currentEnvironment = Environment.Development;

        // This method returns the appropriate base URL depending on the environment setting.
        // It checks if the current environment is Development, Staging, or Production and returns the appropriate URL.
        public string GetBaseUrl()
        {
            // Switch case to determine the correct base URL based on the environment
            switch (currentEnvironment)
            {
                case Environment.Production:
                    return productionBaseUrl;
                case Environment.Staging:
                    return stagingBaseUrl;
                case Environment.Development:
                default:
                    return developmentBaseUrl;
            }
        }

        // Method to validate the base URLs (optional but recommended).
        // This method can be called to ensure the URLs are correctly set for each environment.
        public void ValidateUrls()
        {
            if (string.IsNullOrEmpty(developmentBaseUrl))
            {
                Debug.LogWarning("Development base URL is not set!");
            }
            if (string.IsNullOrEmpty(stagingBaseUrl))
            {
                Debug.LogWarning("Staging base URL is not set!");
            }
            if (string.IsNullOrEmpty(productionBaseUrl))
            {
                Debug.LogWarning("Production base URL is not set!");
            }
        }

        // This method can be used to print the current environment and URL to the console for debugging purposes.
        public void PrintCurrentEnvironment()
        {
            string currentUrl = GetBaseUrl();
            Debug.Log("Current Environment: " + currentEnvironment + " | Base URL: " + currentUrl);
        }
    }
}