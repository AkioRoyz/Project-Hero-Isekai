using UnityEngine;
using UnityEngine.Localization.Settings;
using System.Collections;

public class LanguageSwitch : MonoBehaviour
{
    public void SetEnglish()
    {
        StartCoroutine(SetLocale(0));
    }

    public void SetRussian()
    {
        StartCoroutine(SetLocale(1));
    }

    private IEnumerator SetLocale(int localeIndex)
    {
        yield return LocalizationSettings.InitializationOperation;

        LocalizationSettings.SelectedLocale =
            LocalizationSettings.AvailableLocales.Locales[localeIndex];
    }

}
