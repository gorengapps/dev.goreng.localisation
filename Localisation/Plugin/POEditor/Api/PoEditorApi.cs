#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using Http;
using Http.Transformers;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Localisation.Plugin.POEditor.API
{
    /// <summary>
    /// Client for interacting with the POEditor API.
    /// Provides methods to authenticate, list projects, export translations, and download files.
    /// </summary>
    public class PoEditorApi
    {
        private const string _baseUrl = "https://api.poeditor.com/v2";

        private string _apiKey;

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
            var bytes = await HttpEngine
                .Make(url)
                .SetByteOutput()
                .Send();

            await File.WriteAllBytesAsync(path, bytes.rawResponse);
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

            var result = await HttpEngine.Make(_baseUrl + path)
                .SetMethod(HttpMethod.Post)
                .SetTransformer(FormEncodedTransformer.Transform)
                .SetHeader("Content-Type", "application/x-www-form-urlencoded")
                .SetBody(new PoeEditorToken { api_token = _apiKey })
                .Send();

            return result
                .To<PoEditorResponseWrapper<PoEditorProjectResponse>>()
                .result;
        }

        /// <summary>
        /// Exports translations for a specific language from a POEditor project.
        /// </summary>
        /// <param name="language">The language code to export (e.g., "en", "fr").</param>
        /// <param name="projectId">The unique identifier of the POEditor project.</param>
        /// <returns>
        /// A task that completes with an <see cref="ExportResponse"/>
        /// containing download information and metadata.
        /// </returns>
        public async Task<Result<ExportResponse, Exception>> ExportLanguage(string language, string projectId)
        {
            var path = $"/projects/export";

            var arguments = new PoEditorExportParameters
            {
                api_token = _apiKey,
                id = projectId,
                language = language,
                type = "xliff_1_2",
            };

            var result = await HttpEngine.Make(_baseUrl + path)
                .SetMethod(HttpMethod.Post)
                .SetTransformer(FormEncodedTransformer.Transform)
                .SetHeader("Content-Type", "application/x-www-form-urlencoded")
                .SetBody(arguments)
                .Send();
            
            try
            {
                var data = result.To<PoEditorResponseWrapper<ExportResponse>>();

                return data.response.status == "fail" ? 
                    Result<ExportResponse, Exception>.Fail(new InvalidDataException(data.response.message)) : 
                    Result<ExportResponse, Exception>.Success(data.result);
            }
            catch (Exception ex)
            {
                return Result<ExportResponse, Exception>.Fail(ex);
            }
        }
    }
}
