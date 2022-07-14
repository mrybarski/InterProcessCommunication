using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterProcessCommunication
{
    public class InterProcessFileStream
    {
        private readonly string filePath;
        EventWaitHandle waitHandle;

        public InterProcessFileStream(string filePath)
        {
            this.filePath = filePath;
            waitHandle = new EventWaitHandle(true, EventResetMode.AutoReset, $"shared_file_{filePath}");
        }

        public void CreateFile()
        {
            waitHandle.WaitOne();

            if (!File.Exists(filePath))
            {
                using FileStream fs = System.IO.File.Create(filePath);
            }

            waitHandle.Set();
        }

        public void AppendToFile(Span<byte> data)
        {
            waitHandle.WaitOne();

            using FileStream fs = new(filePath, FileMode.Append);
            fs.Write(data);
            fs.Flush();

            waitHandle.Set();
        }

        public void ClearFile()
        {
            waitHandle.WaitOne();

            using FileStream fs = new(filePath, FileMode.Open);
            fs.SetLength(0);
            fs.Flush();

            waitHandle.Set();
        }
    }
}
