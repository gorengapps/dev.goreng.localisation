
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Localisation
{
    /// <summary>
    /// Client for interacting with the POEditor API.
    /// Provides methods to authenticate, list projects, export translations, and download files.
    /// </summary>
    ///
    public class PoEditorCliApi
    {
        private const string _baseUrl = "https://api.poeditor.com/v2";
        private string? _apiKey;

        /// <summary>
        /// Sets the API key for authenticating with the POEditor API.
        /// </summary>
        /// <param name="apiKey">The API key provided by POEditor.</param>
        public void SetCredentials(string apiKey)
        {
            _apiKey = apiKey;
        }

        /// <summary>
        /// Downloads a file from the specified URL and saves it to the given local path.
        /// </summary>
        /// <param name="url">The full URL of the file to download.</param>
        /// <param name="path">Local filesystem path where the downloaded file will be saved.</param>
        /// <returns>A task representing the asynchronous download and file write operation.</returns>
        public async Task DownloadFile(string url, string path)
        {
            // Create a new client for this request, mirroring the working test case.
            using var httpClient = new HttpClient();
            var fileBytes = await httpClient.GetByteArrayAsync(url).ConfigureAwait(false);
            await File.WriteAllBytesAsync(path, fileBytes).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves a list of POEditor projects accessible with the current API key.
        /// </summary>
        /// <returns>
        /// A task that completes with a <see cref="PoEditorProjectResponse"/>
        /// containing the project list result.
        /// </returns>
        public async Task<PoEditorProjectResponse> ListProjects()
        {
            var path = "/projects/list";

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("api_token", _apiKey ?? string.Empty)
            });

            // Create a new client for this request.
            using var httpClient = new HttpClient();
            var response =
                await httpClient.PostAsync(_baseUrl + path, formContent).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var data = JsonConvert
                .DeserializeObject<PoEditorResponseWrapper<PoEditorProjectResponse>>(jsonResponse);

            return data.result;
        }

        /// <summary>
        /// Exports translations for a specific language from a POEditor project.
        /// </summary>
        /// <param name="language">The language code to export (e.g., "en", "fr").</param>
        /// <param name="projectId">The unique identifier of the POEditor project.</param>
        /// <returns>
        /// A task that completes with a <see cref="Result{TSuccess,TFailure}"/>
        /// containing download information or an exception.
        /// </returns>
        public async Task<Result<ExportResponse, Exception>> ExportLanguage(string language, string projectId)
        {
            var path = "/projects/export";

            var parameters = new Dictionary<string, string>
            {
                { "api_token", _apiKey ?? string.Empty },
                { "id", projectId },
                { "language", language },
                { "type", "xliff_1_2" }
            };

            var formContent = new FormUrlEncodedContent(parameters);

            try
            {
                // Create a new client for this request.
                using var httpClient = new HttpClient();
                
                Debug.Log("Before sending request to POEditor API");
                
                var response = await httpClient
                    .PostAsync(_baseUrl + path, formContent)
                    .ConfigureAwait(false);
                
                Debug.Log("After sending request to POEditor API");
                
                var jsonResponse = await response.Content
                    .ReadAsStringAsync()
                    .ConfigureAwait(false);

                Debug.Log("After reading string");
                
                var data = JsonConvert.DeserializeObject<PoEditorResponseWrapper<ExportResponse>>(jsonResponse);
                
                Debug.Log($"After deserializing response {data.response.status}");
                
                if (data.response.status == "fail")
                {
                    return Result<ExportResponse, Exception>.Fail(new InvalidDataException(data.response.message));
                }

                return Result<ExportResponse, Exception>.Success(data.result);
            }
            catch (Exception ex)
            {
                return Result<ExportResponse, Exception>.Fail(ex);
            }
        }
    }
}
#endif