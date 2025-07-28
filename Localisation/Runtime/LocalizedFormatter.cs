using System;
using Framework.Events;
using Framework.Events.Extensions;
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
        private readonly TextMeshProUGUI _text;
        private readonly LocaleString _localeString;

        private readonly DisposeBag _disposeBag = new();
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
            
            _localeString= new LocaleString(entry, args)
                .AddToDisposables(_disposeBag);
            
            _localeString.onStringRefreshed
                .Subscribe(UpdateText)
                .AddToDisposables(_disposeBag);
        }

        /// <summary>
        /// Releases resources and unsubscribes from the StringChanged event.
        /// </summary>
        public void Dispose()
        {
            _disposeBag.Dispose();
        }
        
        /// <summary>
        /// Updates the arguments to inject into the localized string and refreshes the displayed text.
        /// </summary>
        /// <param name="args">The new array of objects to replace placeholders.</param>
        public void SetArguments(params object[] args)
        {
            _localeString.SetArguments(args);
        }

        // Internal callback: replaces each "%@" in the raw localized string with indexed placeholders
        // and applies string.Format with the current arguments.
        private void UpdateText(object sender, string text)
        {
            _text.text = text;
            _onStringRefreshedEventProducer.Publish(this, true);
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
