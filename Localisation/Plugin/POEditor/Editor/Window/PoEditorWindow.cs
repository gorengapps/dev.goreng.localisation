using System;
using System.Collections.Generic;
using System.Linq;
using Localisation.Plugin.POEditor.API;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.XLIFF;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace DesignSystem.Editor.Window
{
    public class PoEditorWindow: EditorWindow
    {
        // UI Elements
        private TextField _apiField;
        
        private Label _actionLabel;
        private Button _importButton;
        
        private DropdownField _cultureSelector;
        private DropdownField _projectSelector;
        private DropdownField _tableSelector;
        
        // Configuration
        private const string _editorPrefsKey = "_PoEditorPlugin_Window";
        private const string _editorWindowPath =
            "Packages/dev.goreng.localisation/Localisation/Plugin/POEditor/Editor/Window/POEditorWindow.uxml";
        
        // Data
        private PoEditorApiConfiguration _configuration = new PoEditorApiConfiguration();
        
        private List<string> _locales;
        private List<PoEditorProject> _projects;
        private string _stringTableName;
        
        private readonly PoEditorApi _api = new PoEditorApi();
        
        [MenuItem("Framework/Import Localisation via PoEditor")]
        public static void ShowWindow()
        {
            PoEditorWindow window = GetWindow<PoEditorWindow>();
            window.titleContent = new GUIContent("POEditor Language Importer");
        }

        private void UpdateLocales()
        {
            LocalizationSettings.Instance
                .GetAvailableLocales();
            
            _locales = LocalizationSettings.Instance
                .GetAvailableLocales()
                .Locales
                .Select(x => x.Identifier.Code)
                .ToList();
        }

        private void OnDestroy()
        {
            _apiField.UnregisterCallback<ChangeEvent<string>>(OnConfigurationChanged);
            _projectSelector.UnregisterCallback<ChangeEvent<string>>(OnConfigurationChanged);
            _cultureSelector.UnregisterCallback<ChangeEvent<string>>(OnConfigurationChanged);
            _tableSelector.UnregisterCallback<ChangeEvent<string>>(OnConfigurationChanged);
        }

        private void OnEnable()
        {
            UpdateLocales();
            
            var configuration = EditorPrefs.GetString(_editorPrefsKey);
            
            if (string.IsNullOrEmpty(configuration))
            {
                return;
            }
            
            _configuration = JsonConvert.DeserializeObject<PoEditorApiConfiguration>(configuration);
            _api.SetCredentials(_configuration.apiKey);
            
            UpdateLocales();
            FetchProjects();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            
            // Import UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(_editorWindowPath);
            
            VisualElement uxml = visualTree.CloneTree();
            
            for(var i = 0; i < uxml.childCount; i++)
            {
                root.Add(uxml.ElementAt(i));
            }
            
            _importButton = root.Q<Button>("UpdateButton");
            _apiField = root.Q<TextField>("ApiTextField");
            _projectSelector = root.Q<DropdownField>("ProjectSelector");
            _cultureSelector = root.Q<DropdownField>("CultureSelector");
            _tableSelector = root.Q<DropdownField>("TableSelector");
            
            _actionLabel = root.Q<Label>("ActionLabel");
           
            
            // Update data
            _cultureSelector.choices = _locales;
            _importButton.clicked += OnImportButtonClicked;
            
            // Set Configuration
            _apiField.SetValueWithoutNotify(_configuration.apiKey);

            // Callbacks
            _apiField.RegisterCallback<ChangeEvent<string>>(OnConfigurationChanged);
            _projectSelector.RegisterCallback<ChangeEvent<string>>(OnConfigurationChanged);
            _cultureSelector.RegisterCallback<ChangeEvent<string>>(OnConfigurationChanged);
            _tableSelector.RegisterCallback<ChangeEvent<string>>(OnConfigurationChanged);
            
            // Verifications
            _importButton.SetEnabled(
                _configuration.isValid && 
                _cultureSelector.index >= 0 && 
                _projectSelector.index >= 0 && 
                _tableSelector.index >= 0
            );
        }

        private void OnConfigurationChanged(ChangeEvent<string> evt)
        {
            _configuration.apiKey = _apiField.value;
            _api.SetCredentials(_configuration.apiKey);
            _stringTableName = _tableSelector.value;
            
            EditorPrefs.SetString(_editorPrefsKey, JsonConvert.SerializeObject(_configuration));
            
            // Verifications
            _importButton.SetEnabled(
                _configuration.isValid && 
                _cultureSelector.index >= 0 && 
                _projectSelector.index >= 0 && 
                _tableSelector.index >= 0
            );
        }

        private void FetchTables()
        {
            // Find all assets of type StringTableCollection
            var guids = AssetDatabase.FindAssets("t:StringTableCollection");

            var strings = guids.Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<StringTableCollection>)
                .Select(x => x.TableCollectionName)
                .ToList();

            _tableSelector.choices = strings;
        }

        private async void FetchProjects()
        {
            try
            {
                var response = await _api.ListProjects();

                _projects = response.projects;
                
                _projectSelector.choices = _projects
                    .Select(x => x.name)
                    .ToList();
                
                FetchTables();
            }
            catch(Exception e)
            {
                Debug.LogError($"Failed to fetch projects from API {e.Message}");
            }
        }

        private async void OnImportButtonClicked()
        {
            try
            {
                _actionLabel.text = "Fetching language files from POEditor";
                _importButton.SetEnabled(false);
                
                var language = _locales[_cultureSelector.index];
                var selectedProject = _projects[_projectSelector.index];
                var value = await _api.ExportLanguage(language, selectedProject.id);

                if (value.hasError)
                {
                    throw value.error!;
                }
                
                var path = Application.temporaryCachePath + @"/file-" + language +".xliff";
                
                await _api.DownloadFile(value.result.url, path);
                
                var collection = LocalizationEditorSettings.GetStringTableCollection(_stringTableName);
                var table = collection.GetTable(language) as StringTable;

                if (table == null)
                {
                    return;
                }

                collection.ClearAllEntries();
                    
                _actionLabel.text = "Importing strings into table";
                Xliff.ImportFileIntoTable(path, table);
                    
                _actionLabel.text = "Waiting for action";
                _importButton.SetEnabled(true);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    "Error", 
                    $"Something went wrong while downloading languages {e.Message}", 
                    "Ok"
                );
                
                _importButton.SetEnabled(true);
            }
        }
    }
}