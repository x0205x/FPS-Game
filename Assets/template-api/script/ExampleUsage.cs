using UnityEngine;

namespace TemplateApiNodeJS
{
    public class ExampleUsage : MonoBehaviour
    {
        // Reference to the ApiManager prefab that will be instantiated at runtime.
        // This prefab should be assigned through the Unity Inspector. It should contain the ApiManager component.
        // The prefab must be set in the Inspector, or an error will occur.
        public GameObject apiManagerPrefab;

        // This private variable will hold the instance of the ApiManager after instantiating the prefab.
        private ApiManager apiManager;

        void Start()
        {
            // Step 1: Ensure that the prefab is assigned in the Inspector.
            // This check prevents runtime errors due to missing prefab references.
            if (apiManagerPrefab == null)
            {
                // Log an error if the prefab reference is missing. This message will help the user debug their setup.
                Debug.LogError("ApiManager prefab is not assigned in the inspector! Please assign the prefab before running the scene.");
                return;  // Stop execution to prevent further issues.
            }

            // Step 2: Instantiate the ApiManager prefab at runtime.
            // The prefab will be spawned as a new GameObject in the scene.
            GameObject apiManagerObject;
            try
            {
                // Instantiate the prefab and assign the new GameObject to 'apiManagerObject'.
                apiManagerObject = Instantiate(apiManagerPrefab);
            }
            catch (System.Exception e)
            {
                // In case the instantiation fails, log an error with the exception details.
                Debug.LogError("Failed to instantiate ApiManager prefab: " + e.Message);
                return;  // Stop execution to avoid further issues.
            }

            // Step 3: Check if the ApiManager component is present.
            // This ensures that the prefab has the correct script attached to it.
            // Without the component, the script can't function properly.
            if (!apiManagerObject.TryGetComponent<ApiManager>(out apiManager))
            {
                // Log an error if the ApiManager component is not found on the prefab.
                Debug.LogError("The instantiated ApiManager prefab does not contain an ApiManager component! Ensure that the prefab has the correct script attached.");
                return;  // Stop execution because we can't proceed without the ApiManager component.
            }

            // Step 4: Now that the ApiManager is instantiated and verified, we can make the API request.
            // Here, we make a GET request to the "users" endpoint and handle the response using a callback method.
            try
            {
                apiManager.GetRequest("users", OnApiResponse);
            }
            catch (System.Exception e)
            {
                // Log an error if the API request fails for any reason.
                Debug.LogError("Failed to make API request: " + e.Message);
            }
        }

        // Step 5: This is the callback method that handles the response from the API.
        // The 'response' parameter contains the data returned by the API.
        // In this case, we are simply logging the response, but you can modify this method to process the data further.
        void OnApiResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                // Log a warning if the response is empty or null, which might indicate an issue with the API.
                Debug.LogWarning("API Response is empty or null.");
            }
            else
            {
                // Log the API response to the console. This is where you can handle the data returned by the API.
                Debug.Log("API Response: " + response);
            }
        }
    }
}
