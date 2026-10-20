using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace BudeStudio.Json
{
    /// <summary>
    /// Minimal, dependency-free JSON parser/serializer (public-domain pattern).
    /// Handles the JSON the Bigfoot chat API returns, defensively.
    /// </summary>
    public static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            int pos = 0;
            object result = ParseValue(json, ref pos);
            SkipWhitespace(json, ref pos);
            return result;
        }

        public static string Serialize(object obj)
        {
            var sb = new StringBuilder();
            SerializeValue(obj, sb);
            return sb.ToString();
        }

        // ---- helpers for defensive access ----
        public static string GetStr(IDictionary<string, object> d, string key, string fallback = "")
        {
            if (d != null && d.ContainsKey(key) && d[key] is string s) return s;
            return fallback;
        }

        public static IList GetList(IDictionary<string, object> d, string key)
        {
            if (d != null && d.ContainsKey(key) && d[key] is IList l) return l;
            return null;
        }

        public static IDictionary<string, object> GetDict(object o)
        {
            return o as IDictionary<string, object>;
        }

        // ---- parser ----
        private static void SkipWhitespace(string json, ref int pos)
        {
            while (pos < json.Length && (json[pos] == ' ' || json[pos] == '\t' || json[pos] == '\n' || json[pos] == '\r')) pos++;
        }

        private static object ParseValue(string json, ref int pos)
        {
            SkipWhitespace(json, ref pos);
            if (pos >= json.Length) return null;
            char c = json[pos];
            switch (c)
            {
                case '{': return ParseObject(json, ref pos);
                case '[': return ParseArray(json, ref pos);
                case '"': return ParseString(json, ref pos);
                case 't': case 'f':
                    bool val = c == 't';
                    pos += val ? 4 : 5;
                    return val;
                case 'n':
                    pos += 4;
                    return null;
                default:
                    return ParseNumber(json, ref pos);
            }
        }

        private static object ParseObject(string json, ref int pos)
        {
            var dict = new Dictionary<string, object>();
            pos++; // {
            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == '}') { pos++; return dict; }
            while (pos < json.Length)
            {
                SkipWhitespace(json, ref pos);
                string key = ParseString(json, ref pos);
                SkipWhitespace(json, ref pos);
                pos++; // :
                object value = ParseValue(json, ref pos);
                dict[key] = value;
                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ',') { pos++; continue; }
                if (pos < json.Length && json[pos] == '}') { pos++; }
                break;
            }
            return dict;
        }

        private static object ParseArray(string json, ref int pos)
        {
            var list = new List<object>();
            pos++; // [
            SkipWhitespace(json, ref pos);
            if (pos < json.Length && json[pos] == ']') { pos++; return list; }
            while (pos < json.Length)
            {
                object value = ParseValue(json, ref pos);
                list.Add(value);
                SkipWhitespace(json, ref pos);
                if (pos < json.Length && json[pos] == ',') { pos++; continue; }
                if (pos < json.Length && json[pos] == ']') { pos++; }
                break;
            }
            return list;
        }

        private static string ParseString(string json, ref int pos)
        {
            var sb = new StringBuilder();
            pos++; // opening quote
            while (pos < json.Length)
            {
                char c = json[pos];
                if (c == '"') { pos++; break; }
                if (c == '\\' && pos + 1 < json.Length)
                {
                    pos++;
                    char esc = json[pos];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (pos + 4 < json.Length)
                            {
                                string hex = json.Substring(pos + 1, 4);
                                sb.Append((char)Convert.ToInt32(hex, 16));
                                pos += 4;
                            }
                            break;
                        default: sb.Append(esc); break;
                    }
                    pos++;
                }
                else
                {
                    sb.Append(c);
                    pos++;
                }
            }
            return sb.ToString();
        }

        private static object ParseNumber(string json, ref int pos)
        {
            int start = pos;
            while (pos < json.Length && (char.IsDigit(json[pos]) || json[pos] == '-' || json[pos] == '+' ||
                   json[pos] == '.' || json[pos] == 'e' || json[pos] == 'E')) pos++;
            string num = json.Substring(start, pos - start);
            if (num.Contains(".") || num.Contains("e") || num.Contains("E"))
                return double.TryParse(num, System.Globalization.CultureInfo.InvariantCulture, out double d) ? d : 0.0;
            return long.TryParse(num, out long l) ? l : 0L;
        }

        private static void SerializeValue(object obj, StringBuilder sb)
        {
            if (obj == null) { sb.Append("null"); return; }
            if (obj is string s) { SerializeString(s, sb); return; }
            if (obj is bool b) { sb.Append(b ? "true" : "false"); return; }
            if (obj is int || obj is long || obj is float || obj is double)
            {
                sb.Append(Convert.ToString(obj, System.Globalization.CultureInfo.InvariantCulture));
                return;
            }
            if (obj is IDictionary<string, object> dict)
            {
                sb.Append('{');
                bool first = true;
                foreach (var kv in dict)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    SerializeString(kv.Key, sb);
                    sb.Append(':');
                    SerializeValue(kv.Value, sb);
                }
                sb.Append('}');
                return;
            }
            if (obj is IEnumerable list)
            {
                sb.Append('[');
                bool first = true;
                foreach (var item in list)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    SerializeValue(item, sb);
                }
                sb.Append(']');
                return;
            }
            SerializeString(obj.ToString(), sb);
        }

        private static void SerializeString(string s, StringBuilder sb)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32) sb.Append("\\u" + ((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
