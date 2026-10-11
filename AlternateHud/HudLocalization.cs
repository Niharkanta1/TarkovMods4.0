using System;
using System.Reflection;
using EFT;

namespace AlternateHud
{
    internal static class HudLocalization
    {
        private static readonly MethodInfo LocalizedMethod = FindLocalizedMethod();

        public static string Localized(string key)
        {
            if (string.IsNullOrEmpty(key) || LocalizedMethod == null) return key ?? "";

            try
            {
                var translated = LocalizedMethod.Invoke(null, new object[] { key, null }) as string;
                return string.IsNullOrEmpty(translated) ? key : translated;
            }
            catch (Exception e)
            {
                AlternateHudPlugin.Log.LogWarning("Could not localize HUD text '" + key + "': " + e.Message);
                return key;
            }
        }

        private static MethodInfo FindLocalizedMethod()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            foreach (var type in typeof(Player).Assembly.GetTypes())
            {
                var methods = type.GetMethods(flags);
                var hasParser = false;
                foreach (var candidate in methods)
                {
                    if (candidate.Name == "ParseLocalization")
                    {
                        hasParser = true;
                        break;
                    }
                }

                if (!hasParser) continue;
                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    if (method.Name == "Localized" && parameters.Length == 2 &&
                        parameters[0].ParameterType == typeof(string) &&
                        parameters[1].ParameterType == typeof(string))
                        return method;
                }
            }

            AlternateHudPlugin.Log.LogWarning("EFT localization method was not found; HUD will show raw localization keys.");
            return null;
        }
    }
}
