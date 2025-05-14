namespace Localisation
{
    public struct PoEditorResponse
    {
        public string status;
        public string code;
        public string message;
    }
    
    public struct PoEditorResponseWrapper<T>
    {
        public PoEditorResponse response;
        public T result;
    }
    
    public struct ExportResponse
    {
        public string url;
    }
}