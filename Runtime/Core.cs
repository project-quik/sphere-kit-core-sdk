using UnityEngine;

namespace SphereKit
{
    public class Core
    {
        static public bool HasInitialized { get; private set; } = false;
        static string _clientId;

        public static void Initialize()
        {
            var config = ProjectConfig.GetOrCreateConfig();
            if (config == null || config.clientID == "")
            {
                throw new System.Exception("Sphere Kit has not been configured yet. Please configure Sphere Kit in Project Settings.");
            }

            _clientId = config.clientID;

            HasInitialized = true;
            Debug.Log($"Sphere Kit has been initialized. Client ID is {_clientId}");
        }

        static void CheckInitialized()
        {
            if (!HasInitialized)
            {
                throw new System.Exception("Sphere Kit has not been initialized. Please call SphereKit.Core.Initialize() before calling any other methods.");
            }
        }

        public static async void SignInWithSphere()
        {
            CheckInitialized();

            // TODO: Start OAuth2 flow
        }

        public static void SignOut()
        {
            CheckInitialized();

            // TODO: Sign out of Sphere Kit
        }
    }
}
