
using System.Collections.Generic;

namespace System
{
    public static class UriUtil
    {

        public static Dictionary<String, String> ParseQuery(this Uri uri)
        {
            Dictionary<String, String> result = new Dictionary<string, string>();
            var query = uri.Query;
            if (query[0] == '?') query = query.Substring(1);
            var segments = query.Split("&", StringSplitOptions.RemoveEmptyEntries);

            foreach (String item in segments)
            {
                var pairs = item.Split('=', StringSplitOptions.RemoveEmptyEntries);
                var key = Decode(pairs[0].AsMemory()).ToString();
                String value = null;
                if (pairs.Length > 2) throw new Exception("");
                if (pairs.Length == 2) value = Decode(pairs[1].AsMemory()).ToString();
                result.Add(key, value);
            }
            return result;
        }

        private static ReadOnlyMemory<char> Decode(ReadOnlyMemory<char> chars)
        {
            ReadOnlySpan<char> span = chars.Span;
            if (!span.ContainsAny('%', '+'))
            {
                return chars;
            }
            char[] array = new char[span.Length];
            span.Replace(array, '+', ' ');
            int length;
            Uri.TryUnescapeDataString(array, array, out length);
            return array.AsMemory(0, length);
        }



    }
}
