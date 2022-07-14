using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace InterProcessCommunication.Extensions
{
    public static class StringExtension
    {
        public static string CreateSHA512(this string strData)
        {
            var message = Encoding.UTF8.GetBytes(strData);
            using var alg = SHA512.Create();
            StringBuilder builder = new();

            var hashValue = alg.ComputeHash(message);
            foreach (byte x in hashValue)
            {
                builder.Append(String.Format("{0:x2}", x));
            }
            return builder.ToString();
        }

        public static bool Contains(this string? @string, params string[] elements)
        {
            if (string.IsNullOrEmpty(@string)) return false;

            return elements.Any(e => @string.Contains(e));
        }

        public static bool HasReplyForQuestion(this string? @string, params string[] replies)
        {
            if (string.IsNullOrEmpty(@string)) return false;

            return replies.Any(e => @string.IndexOf(e) == 0);
        }
    }
}
