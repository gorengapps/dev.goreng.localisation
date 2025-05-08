using System.Linq;
using Localisation.Plugin;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace Localisation
{
    /// <summary>
    /// Custom inspector for localised text mesh pros
    /// </summary>
    [CustomEditor(typeof(TextMeshProLocalised))]
    public class LocalisedTextMeshProEditor : TMP_EditorPanelUI
    {
        private SerializedProperty _localisationProperty;
        
        private StringTableCollection _stringTableCollection;
        private string _selectedOption;
        
        static readonly GUIContent _localisationContent = new GUIContent(
            "Localisation", 
            "Apply a key that will resolve during runtime"
        );

        private void Awake()
        {
            _stringTableCollection = LocalizationEditorSettings.GetStringTableCollection("Strings");
        }

        /// <summary>
        /// Draw the standard custom inspector
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            _localisationProperty = serializedObject.FindProperty("_localisationKey");

            var table = _stringTableCollection.GetTable("en") as StringTable;

            if (table == null)
            {
                Debug.LogError("No table `en` found");
                return;
            }
            
            var keys = table
                .Select(x => x.Value.Key)
                .ToList();
            
            var selectedIndex = keys.IndexOf(_localisationProperty.stringValue);
            
            EditorGUILayout.HelpBox("Fill in the correct key to apply localisation", MessageType.None);
            var newIndex = EditorGUILayout.Popup(_localisationContent, selectedIndex, keys.ToArray());

            if (selectedIndex != newIndex)
            {
                m_HavePropertiesChanged = true;
                _selectedOption = keys[newIndex];
                _localisationProperty.stringValue = _selectedOption;

                var entry = table
                    .FirstOrDefault(x => x.Value.Key == _selectedOption).Value;

                if (entry != null)
                {
                    m_TextProp.stringValue = entry.GetLocalizedString();
                }
            }
            
            EditorGUILayout.Space();
            
            DrawTextInput();

            DrawMainSettings();

            DrawExtraSettings();

            EditorGUILayout.Space();

            if (!serializedObject.ApplyModifiedProperties() && !m_HavePropertiesChanged)
            {
                return;
            }
            
            m_TextComponent.havePropertiesChanged = true;
            m_HavePropertiesChanged = false;
            EditorUtility.SetDirty(target);
        }
    }
}
