using System;
using System.Globalization;
using System.Text;

namespace TcpUdpTool.Model.Parser
{
    public class HexParser : IParser
    {
        public byte[] Parse(string text, Encoding encoding = null)
        {
            string[] parts = text.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            byte[] data = new byte[parts.Length];

            for(int i = 0; i < parts.Length; i++)
            {
                try
                {
                    if (parts[i].Length > 2)
                        throw new FormatException();

                    data[i] = (byte)uint.Parse(parts[i], NumberStyles.AllowHexSpecifier);
                }
                catch(FormatException)
                {
                    throw new FormatException("序列错误，" + parts[i] + " 不是 8 位十六进制数。");
                }               
            }

            return data;
        }

    }
}
