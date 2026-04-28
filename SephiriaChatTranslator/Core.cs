using FMOD.Studio;
using FMODUnity;
using HarmonyLib;
using MelonLoader;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[assembly: MelonInfo(typeof(SephiriaChatTranslator.Core), "ChatTranslator", "1.0.0", "Mira", null)]
[assembly: MelonGame("TEAMHORAY", "Sephiria")]

namespace SephiriaChatTranslator
{
    public class Core : MelonMod
    {
        public List<List<string>> ModOptions => [["UI_ModSetting_English", "UI_ModSetting_Korean", "UI_ModSetting_Japanese", "UI_ModSetting_Chinese"],
        ["UI_ModSetting_None", "UI_ModSetting_English", "UI_ModSetting_Korean", "UI_ModSetting_Japanese", "UI_ModSetting_Chinese"]];
        public List<string> ModOptionsDescription => ["UI_ModSetting_Language", "UI_ModSetting_LanguageSecond"];
        public List<int> ModOptionsDefault => [0, 0];
        public bool UseLocalizedSring => true;

        public int TranslateMode = 0;

        public int TranslateSecondMode = 0;

        public void OnModOptionChanged(int index, int value)
        {
            if (index == 0)
                TranslateMode = value;
            else if (index == 1)
                TranslateSecondMode = value;
            Melon<Core>.Logger.Msg("Changed: " + new LocalizedString(ModOptions[index][value]).ToString());
        }
        public void OnModOptionLoaded(int index, int value)
        {
            if (index == 0)
                TranslateMode = value;
            else if (index == 1)
                TranslateSecondMode = value;
            Melon<Core>.Logger.Msg("Loaded: " + new LocalizedString(ModOptions[index][value]).ToString());
        }
        public string GetTargetLanguage()
        {
            return TranslateMode switch
            {
                0 => "en",
                1 => "ko",
                2 => "ja",
                3 => "zh-TW",
                _ => "en"
            };
        }
        public string GetSecondTargetLanguage()
        {
            return TranslateSecondMode switch
            {
                1 => "en",
                2 => "ko",
                3 => "ja",
                4 => "zh-TW",
                _ => string.Empty
            };
        }

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");
        }

        [HarmonyPatch(typeof(AddOnLoader), nameof(AddOnLoader.LoadAll))]
        public static class OnAddOnLoadPatch
        {
            static void Postfix()
            {
                HorayModAPI.OnLocalizationReady += OnLocalizationReady;
            }
            private static void OnLocalizationReady(HorayModLocalizationContext context)
            {
                var enUS = "en-US";
                var jaJP = "ja-JP";
                var koKR = "ko-KR";
                context.AddText(enUS, "UI_ModSetting_Language", "Language used for translation");
                context.AddText(enUS, "UI_ModSetting_LanguageSecond", "Language used for retranslation");
                context.AddText(enUS, "UI_ModSetting_None", "None");
                context.AddText(enUS, "UI_ModSetting_English", "English");
                context.AddText(enUS, "UI_ModSetting_Korean", "Korean");
                context.AddText(enUS, "UI_ModSetting_Japanese", "Japanese");
                context.AddText(enUS, "UI_ModSetting_Chinese", "Chinese");
                context.AddText(enUS, "UI_ChatTranslator_Error", "(It seems the translation failed...)");

                context.AddText(jaJP, "UI_ModSetting_Language", "翻訳に使用する言語");
                context.AddText(jaJP, "UI_ModSetting_LanguageSecond", "再翻訳に使用する言語");
                context.AddText(jaJP, "UI_ModSetting_None", "なし");
                context.AddText(jaJP, "UI_ModSetting_English", "英語");
                context.AddText(jaJP, "UI_ModSetting_Korean", "韓国語");
                context.AddText(jaJP, "UI_ModSetting_Japanese", "日本語");
                context.AddText(jaJP, "UI_ModSetting_Chinese", "中国語");
                context.AddText(jaJP, "UI_ChatTranslator_Error", "(翻訳に失敗したようです...)");

                context.AddText(koKR, "UI_ModSetting_Language", "번역에 사용되는 언어");
                context.AddText(koKR, "UI_ModSetting_LanguageSecond", "재번역에 사용되는 언어");
                context.AddText(koKR, "UI_ModSetting_None", "없음");
                context.AddText(koKR, "UI_ModSetting_English", "영어");
                context.AddText(koKR, "UI_ModSetting_Korean", "한국어");
                context.AddText(koKR, "UI_ModSetting_Japanese", "일본어");
                context.AddText(koKR, "UI_ModSetting_Chinese", "중국어");
                context.AddText(koKR, "UI_ChatTranslator_Error", "(번역에 실패한 것 같습니다 ...)");
            }
        }

        [HarmonyPatch(typeof(DungeonManager), "UserCode_RpcChat__PlayerAvatar__String__String", new Type[] { typeof(PlayerAvatar), typeof(string), typeof(string) })]
        public static class DungeonManagerChatPatch
        {
            public static readonly string Url = "https://script.google.com/macros/s/AKfycbzNx6hm5JzdXNx6a_P3zQaF1eTflxaZ9YId0dwq_uO_QkaS_Uw4y01cJYrBX0tJp_Yj/exec";
            static void Postfix(PlayerAvatar avatar, string name, string message, ref DungeonManager __instance)
            {
                __instance.StartCoroutine(Enumerator(avatar, name, message));
            }
            static IEnumerator Enumerator(PlayerAvatar avatar, string name, string message)
            {
                var url = Url + "?text=" + message + "&source=&target=" + Melon<Core>.Instance.GetTargetLanguage();


                using (UnityWebRequest req = UnityWebRequest.Get(url))
                {
                    yield return req.SendWebRequest();

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning(req.error);
                        if ((bool)avatar)
                        {
                            avatar.CreateChatBubble(new LocalizedString("UI_ChatTranslator_Error").ToString());
                        }
                        yield break;
                    }

                    TranslationResult result = null;
                    try
                    {
                        var translated = req.downloadHandler.text;
                        Debug.Log(translated);
                        result = JsonUtility.FromJson<TranslationResult>(translated);

                        if (result.text == message)
                        {
                            yield break;
                        }

                        string text = name + " : " + result.text;
                        Debug.Log(text);
                        GameLogWriter.Instance.WriteLog(text, new Color(0, 1f, 0.5f));
                        if ((bool)avatar)
                        {
                            avatar.CreateChatBubble(result.text);
                        }
                    }
                    catch(Exception ex)
                    {
                        Debug.LogWarning(ex);
                        Melon<Core>.Logger.Warning(ex);
                        if ((bool)avatar)
                        {
                            avatar.CreateChatBubble(new LocalizedString("UI_ChatTranslator_Error").ToString());
                        }
                        yield break;
                    }

                    if (result != null)
                        yield return EnumeratorSecond(avatar, name, result.text);
                }
            }
            static IEnumerator EnumeratorSecond(PlayerAvatar avatar, string name, string message)
            {
                var second = Melon<Core>.Instance.GetSecondTargetLanguage();
                if (string.IsNullOrEmpty(second))
                    yield break;
                var url = Url + "?text=" + message + "&source=" + Melon<Core>.Instance.GetTargetLanguage() + "&target=" + second;

                using (UnityWebRequest req = UnityWebRequest.Get(url))
                {
                    yield return req.SendWebRequest();

                    if (req.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning(req.error);
                        if ((bool)avatar)
                        {
                            avatar.CreateChatBubble(new LocalizedString("UI_ChatTranslator_Error").ToString());
                        }
                        yield break;
                    }

                    try
                    {
                        var translated = req.downloadHandler.text;
                        Debug.Log(translated);
                        var result = JsonUtility.FromJson<TranslationResult>(translated);

                        string text = name + " : " + result.text;
                        Debug.Log(text);
                        GameLogWriter.Instance.WriteLog(text, new Color(0f, 0.75f, 0.5f));
                        if ((bool)avatar)
                        {
                            avatar.CreateChatBubble(result.text);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(ex);
                        Melon<Core>.Logger.Warning(ex);
                        if ((bool)avatar)
                        {
                            avatar.CreateChatBubble(new LocalizedString("UI_ChatTranslator_Error").ToString());
                        }
                        yield break;
                    }

                }
            }
        }
        [System.Serializable]
        public class TranslationResult
        {
            public int code;
            public string text;
        }
    }
}