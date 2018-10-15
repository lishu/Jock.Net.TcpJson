using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Jock.Net.TcpJson
{
    class TcpJsonPackage
    {
        public TcpJsonPackageType Type { get; set; }
        public string DataType { get; set; }
        public string Data { get; set; }
        public Action Callback { get; set; }
        public byte[] DataBytes { get; set; }

        public byte[] ToBytes()
        {
            using (var ms = new MemoryStream())
            {
                using(var writer = new BinaryWriter(ms))
                {
                    writer.Write(DataType ?? string.Empty);
                    if (Type == TcpJsonPackageType.NamedStream)
                    {
                        writer.Write(DataBytes.Length);
                        writer.Write(DataBytes, 0, DataBytes.Length);
                    }
                    else
                    {
                        writer.Write(Data ?? string.Empty);
                    }
                    writer.Flush();
                    var buffer = ms.ToArray();
                    var fullBuffer = new byte[buffer.Length + 5];
                    Array.Copy(buffer, 0, fullBuffer, 5, buffer.Length);
                    fullBuffer[0] = (byte)Type;
                    buffer = BitConverter.GetBytes(buffer.Length);
                    Array.Copy(buffer, 0, fullBuffer, 1, 4);
                    return fullBuffer;
                }
            }            
        }
    }
}
