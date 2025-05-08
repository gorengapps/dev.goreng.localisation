namespace UnityEngine.Networking
{
    internal class PoEditorApiConfiguration
    {
        public string apiKey;

        public bool isValid => !string.IsNullOrEmpty(apiKey);
    }
}