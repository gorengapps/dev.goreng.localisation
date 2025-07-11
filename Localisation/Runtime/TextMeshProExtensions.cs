using System;
using Framework.Events;
using TMPro;
using UnityEngine.Localization;

namespace Localisation.Plugin
{
    /// <summary>
    /// Provides formatting and binding of localized strings with positional placeholders ("%@")
    /// to a TextMeshProUGUI component, allowing dynamic argument updates at runtime.
    /// </summary>
    public class LocalizedFormatter : IDisposable
    {
        // Static field to hold the selected table collection for all formatters.
        private static string _stringTableCollection;
        
        private readonly TextMeshProUGUI _text;
        private readonly LocalizedString _localized;
        private object[] _args;

        private readonly BaseEventProducer<bool> _onStringRefreshedEventProducer = new();
        public IEventListener<bool> onStringRefreshed => _onStringRefreshedEventProducer.listener;
        
        /// <summary>
        /// Creates a new formatter for the given TextMeshProUGUI, binding it to the specified entry key
        /// in the static string table collection. The provided arguments will be injected into the
        /// localized string at each "%@" placeholder.
        /// </summary>
        /// <param name="text">The TextMeshProUGUI component to update with formatted text.</param>
        /// <param name="entry">The key of the entry in the string table collection.</param>
        /// <param name="args">The initial array of objects to inject into the placeholders.</param>
        public LocalizedFormatter(TextMeshProUGUI text, string entry, object[] args)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _args = args ?? Array.Empty<object>();
            _localized = new LocalizedString(_stringTableCollection, entry ?? throw new ArgumentNullException(nameof(entry)));
            _localized.StringChanged += UpdateText;
            _localized.RefreshString();
        }

        /// <summary>
        /// Releases resources and unsubscribes from the StringChanged event.
        /// </summary>
        public void Dispose()
        {
            _localized.StringChanged -= UpdateText;
        }

        /// <summary>
        /// Sets the static string table collection name to use for all new formatters.
        /// Must be called before creating any LocalizedFormatter instances.
        /// </summary>
        /// <param name="tableCollection">The name of the string table collection to use.</param>
        public static void SetStringTableCollection(string tableCollection)
        {
            if (string.IsNullOrEmpty(tableCollection))
                throw new ArgumentException("Table collection name cannot be null or empty.", nameof(tableCollection));
            _stringTableCollection = tableCollection;
        }
        
        /// <summary>
        /// Updates the arguments to inject into the localized string and refreshes the displayed text.
        /// </summary>
        /// <param name="args">The new array of objects to replace placeholders.</param>
        public void SetArguments(params object[] args)
        {
            _args = args ?? Array.Empty<object>();
            _localized.RefreshString();
        }

        // Internal callback: replaces each "%@" in the raw localized string with indexed placeholders
        // and applies string.Format with the current arguments.
        private void UpdateText(string raw)
        {
            if (raw == null)
            {
                return;
            }
            
            var pattern = raw;
            for (int i = 0; i < _args.Length; i++)
                pattern = ReplaceFirst(pattern, "%@", "{" + i + "}");

            _text.text = string.Format(pattern, _args);
            _onStringRefreshedEventProducer.Publish(this, true);
        }

        // Replaces the first occurrence of 'search' in 'text' with 'replace'.
        private static string ReplaceFirst(string text, string search, string replace)
        {
            int idx = text.IndexOf(search, StringComparison.Ordinal);
            return idx < 0
                ? text
                : text.Substring(0, idx) + replace + text.Substring(idx + search.Length);
        }
    }

    /// <summary>
    /// Extension methods for TextMeshProUGUI to bind localized formatted strings.
    /// </summary>
    public static class TextMeshProLocalizationExtensions
    {
        /// <summary>
        /// Binds a "%@"-formatted entry from the currently set string table collection
        /// to this TextMeshProUGUI, returning a LocalizedFormatter to update arguments.
        /// </summary>
        /// <param name="tmp">The TextMeshProUGUI component to bind.</param>
        /// <param name="entry">The key of the entry in the string table collection.</param>
        /// <param name="args">Initial placeholder values to inject.</param>
        /// <returns>A LocalizedFormatter instance for runtime argument updates.</returns>
        public static LocalizedFormatter BindLocalizedFormat(this TextMeshProUGUI tmp,
            string entry,
            params object[] args)
        {
            if (tmp == null) throw new ArgumentNullException(nameof(tmp));
            return new LocalizedFormatter(tmp, entry, args);
        }
    }
}
