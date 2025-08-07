using System;
using Framework.Events;
using UnityEngine.Localization;

namespace Localisation.Plugin
{
 public class LocaleString: IDisposable
    {
        private static string _stringTableCollection;
        private string _key;
        private object[] _args;
        private readonly LocalizedString _localizedString;
        private readonly BaseEventProducer<string> _onStringRefreshedEventProducer = new(true);
        
        public IEventListener<string> onStringRefreshed => _onStringRefreshedEventProducer.listener;
        
        /// <summary>
        /// Sets the static string table collection name to use for all new formatters.
        /// Must be called before creating any LocalizedFormatter instances.
        /// </summary>
        /// <param name="tableCollection">The name of the string table collection to use.</param>
        public static void SetStringTableCollection(string tableCollection)
        {
            if (string.IsNullOrEmpty(tableCollection))
            {
                throw new ArgumentException("Table collection name cannot be null or empty.", nameof(tableCollection));
            }
            
            _stringTableCollection = tableCollection;
        }
        
        public LocaleString(string key, params object[] args)
        {
            _args = args;
            _localizedString = new LocalizedString(_stringTableCollection, key ?? throw new ArgumentNullException(nameof(key)));
            _localizedString.StringChanged += UpdateText;
            _localizedString.RefreshString();
        }

        public void RefreshString()
        {
            _localizedString.RefreshString();
        }

        /// <summary>
        /// Updates the arguments to inject into the localized string and refreshes the displayed text.
        /// </summary>
        /// <param name="args">The new array of objects to replace placeholders.</param>
        public void SetArguments(params object[] args)
        {
            _args = args ?? Array.Empty<object>();
            _localizedString.RefreshString();
        }
        
        private void UpdateText(string raw)
        {
            var pattern = raw;
            
            for (var i = 0; i < _args.Length; i++)
            {
                pattern = ReplaceFirst(pattern, "%@", "{" + i + "}");
            }

            _onStringRefreshedEventProducer.Publish(
                this,
                string.Format(pattern, _args)
            );
        }
        
        // Replaces the first occurrence of 'search' in 'text' with 'replace'.
        private static string ReplaceFirst(string text, string search, string replace)
        {
            int idx = text.IndexOf(search, StringComparison.Ordinal);
            return idx < 0
                ? text
                : text.Substring(0, idx) + replace + text.Substring(idx + search.Length);
        }
        
        public void Dispose()
        {
            _localizedString.StringChanged -= UpdateText;
        }
    }
}