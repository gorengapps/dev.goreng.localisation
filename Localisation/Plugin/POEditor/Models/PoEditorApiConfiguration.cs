namespace Localisation
{
    public class PoEditorApiConfiguration
    {
        public string apiKey;

        public bool isValid => !string.IsNullOrEmpty(apiKey);
    }
}