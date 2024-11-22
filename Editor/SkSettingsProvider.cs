using UnityEditor;
using UnityEngine.UIElements;

namespace SphereKit.Editor
{
    internal class SkSettingsProvider : SettingsProvider
    {
        private SerializedObject _projectConfig;
        internal const string ConfigPath = ProjectConfig.ConfigPath;

        private SkSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _projectConfig = ProjectConfig.GetSerializedConfig();
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.PropertyField(_projectConfig.FindProperty("clientID"));
            EditorGUILayout.PropertyField(_projectConfig.FindProperty("projectID"));
            EditorGUILayout.PropertyField(_projectConfig.FindProperty("serverURL"));
            EditorGUILayout.PropertyField(_projectConfig.FindProperty("deepLinkScheme"));
            _projectConfig.ApplyModifiedPropertiesWithoutUndo();
        }

        [SettingsProvider]
        internal static SettingsProvider CreateCustomSettingsProvider()
        {
            return new SkSettingsProvider("Project/Sphere Kit");
        }
    }
}