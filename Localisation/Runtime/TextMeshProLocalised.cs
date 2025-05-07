using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace Localisation.Plugin
{
    public class TextMeshProLocalised: TextMeshProUGUI
    {
        [SerializeField] private string _localisationKey;
        
        protected override void Start()
        {
            base.Start();
            
            LocalizedString key = new LocalizedString(
                "Strings", 
                _localisationKey
            );
            
            key.StringChanged += KeyOnStringChanged;
            key.RefreshString();
        }

        private void KeyOnStringChanged(string value)
        {
            text = value;
        }
    }
}