using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;


namespace Unity.AI.MLAgents
{
    unsafe internal class BaseSharedMem
    {
        private const string k_Directory = "ml-agents";
        private MemoryMappedViewAccessor accessor;
        private IntPtr accessorPointer;
        private string filePath;

        public BaseSharedMem(string fileName, bool createFile, int size = 0)
        {
            var directoryPath = Path.Combine(Path.GetTempPath(), k_Directory);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            filePath = Path.Combine(directoryPath, fileName);
            if (createFile)
            {
                if (File.Exists(filePath))
                {
                    throw new MLAgentsException($"The file {filePath} already exists");
                }
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(new byte[size], 0, size);
                }
            }
            long length = new System.IO.FileInfo(filePath).Length;
            var mmf = MemoryMappedFile.CreateFromFile(
                File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite),
                null,
                length,
                MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None,
                false
            );
            accessor = mmf.CreateViewAccessor(0, length, MemoryMappedFileAccess.ReadWrite);
            mmf.Dispose();

            byte* ptr = (byte*)0;
            accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            accessorPointer = new IntPtr(ptr);
        }

        public int GetInt(ref int offset)
        {
            offset += 4;
            return accessor.ReadInt32(offset);
        }

        public float GetFloat(ref int offset)
        {
            offset += 4;
            return accessor.ReadSingle(offset);
        }

        public bool GetBool(ref int offset)
        {
            offset += 1;
            return accessor.ReadBoolean(offset);
        }

        public string GetString(ref int offset)
        {
            var length = accessor.ReadSByte(offset);
            var bytes = new byte[length];
            offset += 1;
            accessor.ReadArray(offset, bytes, 0, length);
            offset += length;
            return Encoding.ASCII.GetString(bytes);
        }

        public void GetArray(ref int offset, IntPtr dst, int length)
        {
            IntPtr src = IntPtr.Add(accessorPointer, offset);
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), length, length);
            offset += length;
        }

        public byte[] GetBytes(ref int offset, int length)
        {
            var result = new byte[length];
            accessor.ReadArray(offset, result, 0, length);
            offset += length;
            return result;
        }

        public int GetInt(int offset)
        {
            return accessor.ReadInt32(offset);
        }

        public float GetFloat(int offset)
        {
            return accessor.ReadSingle(offset);
        }

        public bool GetBool(int offset)
        {
            return accessor.ReadBoolean(offset);
        }

        public string GetString(int offset)
        {
            var length = accessor.ReadSByte(offset);
            var bytes = new byte[length];
            offset +=  1;
            accessor.ReadArray(offset, bytes, 0, length);
            return Encoding.ASCII.GetString(bytes);
        }

        public void GetArray(int offset, IntPtr dst, int length)
        {
            IntPtr src = IntPtr.Add(accessorPointer, offset);
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), length, length);
        }

        public byte[] GetBytes(int offset, int length)
        {
            var result = new byte[length];
            accessor.ReadArray(offset, result, 0, length);
            return result;
        }

        public int SetInt(int offset, int value)
        {
            accessor.Write(offset, value);
            return offset + 4;
        }

        public int SetFloat(int offset, float value)
        {
            accessor.Write(offset, value);
            return offset + 4;
        }

        public int SetBool(int offset, bool value)
        {
            accessor.Write(offset, value);
            return offset + 1;
        }

        public int SetString(int offset, string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            int length = bytes.Length;
            accessor.Write(offset, (sbyte)length);
            offset += 1;
            accessor.WriteArray(offset, bytes, 0, length);
            offset += length;
            return offset;
        }

        public int SetArray(int offset, IntPtr src, int length)
        {
            IntPtr dst = IntPtr.Add(accessorPointer, offset);
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), length, length);
            return offset + length;
        }

        public int SetBytes(int offset, byte[] value)
        {
            accessor.WriteArray(offset, value, 0, value.Length);
        }

        public void Close()
        {
            if (accessor.CanWrite)
            {
                accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                accessor.Dispose();
            }
        }

        public void Delete()
        {
            Close();
            File.Delete(filePath);
        }
    }
}
