using UnityEditor;
using UnityEngine;

namespace SphereKit
{
    public class ProjectConfig : ScriptableObject
    {
        public const string ConfigPath = "SphereKitConfig";

        [SerializeField]
        public string clientID;

        [SerializeField]
        public string serverURL;

        public static ProjectConfig GetOrCreateConfig()
        {
#if UNITY_EDITOR
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<ProjectConfig>("Assets/Resources/" + ConfigPath + ".asset");
            if (config == null)
            {
                config = CreateInstance<ProjectConfig>();
                config.clientID = "";
                config.serverURL = ""; // TODO: Set server URL close to launch
                if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                }
                UnityEditor.AssetDatabase.CreateAsset(config, "Assets/Resources/" + ConfigPath + ".asset");
                UnityEditor.AssetDatabase.SaveAssets();
            }
            return config;
#else
            return Resources.Load<ProjectConfig>(ConfigPath);
#endif
        }

        public static SerializedObject GetSerializedConfig()
        {
#if UNITY_EDITOR
            return new SerializedObject(GetOrCreateConfig());
#else
            return null;
#endif
        }
    }
}