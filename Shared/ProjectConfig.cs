using UnityEditor;
using UnityEngine;

#nullable enable
namespace SphereKit
{
    public class ProjectConfig : ScriptableObject
    {
        public const string ConfigPath = "SphereKitConfig";

        [SerializeField]
        public string clientID = "";

        [SerializeField]
        public string projectID = "";
        
        [SerializeField]
        public string serverURL = ""; // TODO: Set server URL close to launch

        [SerializeField]
        public string deepLinkScheme = "";

        public static ProjectConfig GetOrCreateConfig()
        {
#if UNITY_EDITOR
            var config = AssetDatabase.LoadAssetAtPath<ProjectConfig>("Assets/Resources/" + ConfigPath + ".asset");
            if (config == null)
            {
                config = CreateInstance<ProjectConfig>();
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateAsset(config, "Assets/Resources/" + ConfigPath + ".asset");
                AssetDatabase.SaveAssets();
            }
            return config;
#else
            return Resources.Load<ProjectConfig>(ConfigPath);
#endif
        }

        public static
#if UNITY_EDITOR
        SerializedObject
#else
        object
#endif
         GetSerializedConfig()
        {
#if UNITY_EDITOR
            return new SerializedObject(GetOrCreateConfig());
#else
            return null;
#endif
        }
    }
}