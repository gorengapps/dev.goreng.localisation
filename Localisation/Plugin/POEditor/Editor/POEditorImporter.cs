#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.XLIFF;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Localisation.Editor.Plugin
{
    public static class PoEditorImporter
    {
        /// <summary>
        /// A helper class to store the result of a download operation.
        /// </summary>
        private class LocaleDownloadResult
        {
            public bool Success { get; set; }
            public string LanguageCode { get; set; } = "";
            public string TempPath { get; set; } = "";
            public string ErrorMessage { get; set; } = "";
        }
        
        public static void ImportAllLanguages(Dictionary<string, string> validatedOptions)
        {
            Debug.Log("Starting CLI import process with Coroutines...");
            var coroutine = ImportAllFromCliCoroutine(validatedOptions);
            bool isRunning = true;

            // This loop manually drives the coroutine's execution.
            while (isRunning)
            {
                try
                {
                    // MoveNext() executes the coroutine until the next 'yield'.
                    // It returns false when the coroutine has finished.
                    if (!coroutine.MoveNext())
                    {
                        isRunning = false;
                        Debug.Log("CLI import process completed successfully.");
                    }
                }
                catch (Exception e)
                {
                    // If the coroutine throws an exception, it will be caught here.
                    isRunning = false;
                    Debug.LogError("CLI import process failed with an exception.");
                    Debug.LogException(e);
                }
                // Sleep to prevent the loop from consuming 100% CPU.
                Thread.Sleep(100);
            }
        }
        
        private static IEnumerator ImportAllFromCliCoroutine(Dictionary<string, string> validatedOptions)
        {
            var apiKey = validatedOptions["poeditor-api-key"];
            var projectId = validatedOptions["poeditor-project-id"];
            var stringTableName = "Strings";

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(projectId))
            {
                Debug.LogError("Missing required arguments. Use -poeditor-api-key and -poeditor-project-id");
                yield break; // End the coroutine
            }

            Debug.Log($"API Key found, Project ID: {projectId}");

            var api = new PoEditorCliApi();
            api.SetCredentials(apiKey);

            var instance = LocalizationSettings.Instance;
            var initializationTask = instance.GetInitializationOperation().Task;
            // Wait for the Task to complete by polling its status.
            while (!initializationTask.IsCompleted)
            {
                yield return null;
            }
            if (initializationTask.IsFaulted) throw initializationTask.Exception;

            var locales = instance.GetAvailableLocales().Locales;

            Debug.Log("Starting sequential import for all locales...");
            foreach (var locale in locales)
            {
                // Start the async download operation, which returns a Task.
                var downloadTask = DownloadLocaleAsync(api, locale.Identifier.Code, projectId);

                // Yield control until the Task is complete.
                while (!downloadTask.IsCompleted)
                {
                    yield return null;
                }

                // Check for exceptions inside the completed task.
                if (downloadTask.IsFaulted)
                {
                    if (downloadTask.Exception != null) throw downloadTask.Exception;
                }
                
                var result = downloadTask.Result;

                if (!result.Success)
                {
                    Debug.LogError(result.ErrorMessage);
                    continue;
                }
                
                var collection = LocalizationEditorSettings.GetStringTableCollection(stringTableName);
                if (collection == null)
                {
                    throw new InvalidOperationException($"StringTableCollection '{stringTableName}' not found.");
                }

                var table = collection.GetTable(result.LanguageCode) as StringTable;
                if (table == null)
                {
                    Debug.LogWarning($"String Table for '{result.LanguageCode}' not found in '{stringTableName}'. Skipping.");
                    continue;
                }

                table.Clear();
                Debug.Log($"Importing strings into table for language '{result.LanguageCode}'.");
                Xliff.ImportFileIntoTable(result.TempPath, table);
                EditorUtility.SetDirty(table);
                Debug.Log($"Successfully imported {table.Count} entries for '{result.LanguageCode}'.");

                try { File.Delete(result.TempPath); }
                catch (Exception e) { Debug.LogWarning($"Could not delete temp file {result.TempPath}: {e.Message}"); }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("All locales processed.");
        }

        /// <summary>
        /// This method remains async Task because it uses HttpClient, which is Task-based.
        /// The coroutine will wait for the returned Task to complete.
        /// </summary>
        private static async Task<LocaleDownloadResult> DownloadLocaleAsync(PoEditorCliApi api, string languageCode, string projectId)
        {
            try
            {
                var exportResult = await api.ExportLanguage(languageCode, projectId).ConfigureAwait(false);
                if (exportResult.hasError)
                {
                    return new LocaleDownloadResult { Success = false, ErrorMessage = $"Failed to export '{languageCode}': {exportResult.error?.Message}" };
                }
                
                var downloadUrl = exportResult.result.url;
                var tempPath = Path.Combine(Path.GetTempPath(), $"file-{languageCode}.xliff");
                await api.DownloadFile(downloadUrl, tempPath).ConfigureAwait(false);
                return new LocaleDownloadResult { Success = true, LanguageCode = languageCode, TempPath = tempPath };
            }
            catch (Exception e)
            {
                return new LocaleDownloadResult { Success = false, ErrorMessage = $"Exception while downloading '{languageCode}': {e.Message}" };
            }
        }

        private static string GetArgument(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}
