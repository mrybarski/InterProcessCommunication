using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterProcessCommunication
{
    public static class DateTimeExtension
    {
        public static int GetUnixTimeStamp(this DateTime dateTime)
        {
            return (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        }
    }
}
