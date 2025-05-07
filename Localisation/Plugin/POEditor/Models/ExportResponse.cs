namespace UnityEngine.Networking
{
    public class PoEditorResponse
    {
        public string status;
        public string code;
        public string message;
    }
    
    public class PoEditorResponseWrapper<T>
    {
        public PoEditorResponse response;
        public T result;
    }
    
    public class ExportResponse
    {
        public string url;
    }
}