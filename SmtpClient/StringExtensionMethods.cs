using System;
using System.ComponentModel;

namespace TakeAsh {
    
    /// <summary>
    /// [C# - TryParse系のメソッドで一時変数を用意したくない… - Qiita](http://qiita.com/Temarin_PITA/items/9aac6c1f569fc2113e0d)
    /// </summary>
    public static class StringExtensionMethods {

        // string to T
        public static T TryParse<T>(this string text)
            where T : struct {
            
            return text.TryParse(default(T));
        }

        // string to T
        public static T TryParse<T>(this string text, T defaultValue)
            where T : struct {

            // コンバーターを作成
            var converter = TypeDescriptor.GetConverter(typeof(T));

            // 変換不可能な場合は規定値を返す
            if (!converter.CanConvertFrom(typeof(string))) {
                return defaultValue;
            }

            try {
                // 変換した値を返す
                return (T)converter.ConvertFrom(text);
            }
            catch {
                // 変換に失敗したら規定値を返す
                return defaultValue;
            }
        }
    }
}
