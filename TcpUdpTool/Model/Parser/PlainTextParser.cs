using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TcpUdpTool.Model.Parser
{
    public class PlainTextParser : IParser
    {
 
        public byte[] Parse(string text, Encoding encoding = null)
        {
            if(encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            List<byte> res = new List<byte>(text.Length * 2);
            StringBuilder cache = new StringBuilder();
            
            bool escape = false;
            bool escapeHex = false;
            bool escapeUnicode = false;
            string hexStr = "";

            foreach (char c in text)
            {
                if (escape)
                {
                    if (c == '\\')
                        cache.Append('\\');
                    else if (c == '0')
                        cache.Append('\0');
                    else if (c == 'a')
                        cache.Append('\a');
                    else if (c == 'b')
                        cache.Append('\b');
                    else if (c == 'f')
                        cache.Append('\f');
                    else if (c == 'n')
                        cache.Append('\n');
                    else if (c == 'r')
                        cache.Append('\r');
                    else if (c == 't')
                        cache.Append('\t');
                    else if (c == 'v')
                        cache.Append('\v');
                    else if (c == 'x')
                        escapeHex = true;
                    else if (c == 'u')
                        escapeUnicode = true;
                    else
                        throw new FormatException("发现错误的转义序列，\\"
                            + c + " 不被允许。");

                    escape = false;
                }
                else if (escapeHex)
                {
                    hexStr += c;

                    if (hexStr.Length == 2)
                    {
                        try
                        {
                            // adding binary data that should not be character encoded,
                            // encode and move previous data to result and clear cache.
                            res.AddRange(encoding.GetBytes(cache.ToString()));
                            cache.Clear();
                            res.Add((byte)int.Parse(hexStr, NumberStyles.AllowHexSpecifier));
                        }
                        catch (FormatException)
                        {
                            throw new FormatException("发现错误的转义序列，\\x"
                                + hexStr + " 不是 8 位十六进制数。");
                        }

                        escapeHex = false;
                        hexStr = "";
                    }
                }
                else if (escapeUnicode)
                {
                    hexStr += c;

                    if (hexStr.Length == 4)
                    {
                        try
                        {
                            cache.Append(Convert.ToChar(int.Parse(hexStr, NumberStyles.AllowHexSpecifier)));
                        }
                        catch (FormatException)
                        {
                            throw new FormatException("发现错误的转义序列，\\u"
                               + hexStr + " 不是 16 位十六进制 Unicode 码点。");
                        }

                        escapeUnicode = false;
                        hexStr = "";
                    }
                }
                else
                {
                    if (c == '\\')
                    {
                        // next char is an escape sequence.
                        escape = true;
                    }
                    else
                    {
                        cache.Append(c);
                    }
                }
            }

            res.AddRange(encoding.GetBytes(cache.ToString()));

            return res.ToArray();
        }

    }

}
