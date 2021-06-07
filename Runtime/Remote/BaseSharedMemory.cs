using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.AI.MLAgents
{
    unsafe internal class BaseSharedMemory
    {
        private const string k_Directory = "ml-agents";
        private MemoryMappedViewAccessor m_Accessor;
        private IntPtr m_AccessorPointer;
        private string m_FilePath;

        /// <summary>
        /// A bare bone shared memory wrapper that opens or create a new shared
        /// memory file and connects to it.
        /// </summary>
        /// <param name="fileName"> The name of the file. The file must be present
        /// in the Temporary folder (depends on OS) in the directory <see cref="k_Directory"/></param>
        /// <param name="createFile"> If true, the file will be created by the constructor.
        /// And error will be thrown if the file already exists.</param>
        /// <param name="size"> The size of the file to be created (will be ignored if
        /// <see cref="createFile"/> is true.</param>
        public BaseSharedMemory(string fileName, bool createFile, int size = 0)
        {
            var directoryPath = Path.Combine(Path.GetTempPath(), k_Directory);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            m_FilePath = Path.Combine(directoryPath, fileName);
            if (createFile)
            {
                if (File.Exists(m_FilePath))
                {
                    throw new MLAgentsException($"The file {m_FilePath} already exists");
                }
                using (var fs = new FileStream(m_FilePath, FileMode.Create, FileAccess.Write))
                {
                    fs.Write(new byte[size], 0, size);
                }
            }
            long length = new System.IO.FileInfo(m_FilePath).Length;
            var mmf = MemoryMappedFile.CreateFromFile(
                File.Open(m_FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite),
                null,
                length,
                MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None,
                false
            );
            m_Accessor = mmf.CreateViewAccessor(0, length, MemoryMappedFileAccess.ReadWrite);
            mmf.Dispose();

            byte* ptr = (byte*)0;
            m_Accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            m_AccessorPointer = new IntPtr(ptr);
        }

        /// <summary>
        /// Returns the integer present at the specified offset. The <see cref="offset"/> must be
        /// passed by reference and will be incremented to the next value.
        /// </summary>
        /// <param name="offset"> Where to read the value</param>
        /// <returns></returns>
        public int GetInt(ref int offset)
        {
            var result =  m_Accessor.ReadInt32(offset);
            offset += 4;
            return result;
        }

        /// <summary>
        /// Returns the float present at the specified offset. The <see cref="offset"/> must be
        /// passed by reference and will be incremented to the next value.
        /// </summary>
        /// <param name="offset"> Where to read the value</param>
        /// <returns></returns>
        public float GetFloat(ref int offset)
        {
            var result = m_Accessor.ReadSingle(offset);
            offset += 4;
            return result;
        }

        /// <summary>
        /// Returns the boolean present at the specified offset. The <see cref="offset"/> must be
        /// passed by reference and will be incremented to the next value.
        /// </summary>
        /// <param name="offset"> Where to read the value</param>
        /// <returns></returns>
        public bool GetBool(ref int offset)
        {
            var result = m_Accessor.ReadBoolean(offset);
            offset += 1;
            return result;
        }

        /// <summary>
        /// Returns the string present at the specified offset. The <see cref="offset"/> must be
        /// passed by reference and will be incremented to the next value.
        /// Note, the strings must be represented with : a sbyte (8-bit unsigned)
        /// indicating the size of the string in bytes followed by the bytes of the
        /// string (in ASCII format).
        /// </summary>
        /// <param name="offset"> Where to read the value</param>
        /// <returns></returns>
        public string GetString(ref int offset)
        {
            var length = m_Accessor.ReadSByte(offset);
            var bytes = new byte[length];
            offset += 1;
            m_Accessor.ReadArray(offset, bytes, 0, length);
            offset += length;
            return Encoding.ASCII.GetString(bytes);
        }

        /// <summary>
        /// Copies the values in the memory file at offset to the NativeArray <see cref="array"/>
        /// The <see cref="offset"/> must be passed by reference and will be incremented to the next value.
        /// <param name="offset"> Where to read the value</param>
        /// <param name="array"> The destination array</param>
        /// <param name="length"> The number of bytes that must be copied, must be less or equal
        /// to the capacity of the array.</param>
        /// <typeparam name="T"> The type of the NativeArray. Must be a struct.</typeparam>
        /// </summary>
        public void GetArray<T>(ref int offset, NativeArray<T> array, int length) where T : struct
        {
            if (length <= 0){
                return;
            }
            IntPtr src = IntPtr.Add(m_AccessorPointer, offset);
            IntPtr dst = new IntPtr(array.GetUnsafePtr());
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), length, length);
            offset += length;
        }

        /// <summary>
        /// Returns the byte array present at the specified offset. The <see cref="offset"/> must be
        /// passed by reference and will be incremented to the next value.
        /// </summary>
        /// <param name="offset"> Where to read the value</param>
        /// <param name="length"> The size of the array in bytes</param>
        /// <returns></returns>
        public byte[] GetBytes(ref int offset, int length)
        {
            var result = new byte[length];
            m_Accessor.ReadArray(offset, result, 0, length);
            offset += length;
            return result;
        }

        /// <summary>
        /// Returns the integer present at the specified offset.
        /// </summary>
        /// <param name="offset"> Where to read the value.</param>
        /// <returns></returns>
        public int GetInt(int offset)
        {
            return m_Accessor.ReadInt32(offset);
        }

        /// <summary>
        /// Returns the float present at the specified offset.
        /// </summary>
        /// <param name="offset"> Where to read the value.</param>
        /// <returns></returns>
        public float GetFloat(int offset)
        {
            return m_Accessor.ReadSingle(offset);
        }

        /// <summary>
        /// Returns the boolean present at the specified offset.
        /// </summary>
        /// <param name="offset"> Where to read the value.</param>
        /// <returns></returns>
        public bool GetBool(int offset)
        {
            return m_Accessor.ReadBoolean(offset);
        }

        /// <summary>
        /// Returns the string present at the specified offset.
        /// Note, the strings must be represented with : a sbyte (8-bit unsigned)
        /// indicating the size of the string in bytes followed by the bytes of the
        /// string (in ASCII format).
        /// </summary>
        /// <param name="offset"> Where to read the value</param>
        /// <returns></returns>
        public string GetString(int offset)
        {
            var length = m_Accessor.ReadSByte(offset);
            var bytes = new byte[length];
            offset +=  1;
            m_Accessor.ReadArray(offset, bytes, 0, length);
            return Encoding.ASCII.GetString(bytes);
        }

        /// <summary>
        /// Copies the values in the memory file at offset to the NativeArray <see cref="array"/>
        /// <param name="offset"> Where to read the value</param>
        /// <param name="array"> The destination array</param>
        /// <param name="length"> The number of bytes that must be copied, must be less or equal
        /// to the capacity of the array.</param>
        /// <typeparam name="T"> The type of the NativeArray. Must be a struct.</typeparam>
        /// </summary>
        public void GetArray<T>(int offset, NativeArray<T> array, int length) where T : struct
        {
            IntPtr src = IntPtr.Add(m_AccessorPointer, offset);
            IntPtr dst = new IntPtr(array.GetUnsafePtr());
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), length, length);
        }

        /// <summary>
        /// Returns the byte array present at the specified offset.
        /// </summary>
        /// <param name="offset"> Where to read the value</param>
        /// <param name="length"> The size of the array in bytes</param>
        /// <returns></returns>
        public byte[] GetBytes(int offset, int length)
        {
            var result = new byte[length];
            if (length > 0)
            {
                m_Accessor.ReadArray(offset, result, 0, length);
            }
            return result;
        }

        /// <summary>
        /// Sets the integer at the specified offset in the shared memory.
        /// </summary>
        /// <param name="offset"> The position at which to write the value</param>
        /// <param name="value"> The value to be written</param>
        /// <returns> The offset right after the written value.</returns>
        public int SetInt(int offset, int value)
        {
            m_Accessor.Write(offset, value);
            return offset + 4;
        }

        /// <summary>
        /// Sets the float at the specified offset in the shared memory.
        /// </summary>
        /// <param name="offset"> The position at which to write the value</param>
        /// <param name="value"> The value to be written</param>
        /// <returns> The offset right after the written value.</returns>
        public int SetFloat(int offset, float value)
        {
            m_Accessor.Write(offset, value);
            return offset + 4;
        }

        /// <summary>
        /// Sets the boolean at the specified offset in the shared memory.
        /// </summary>
        /// <param name="offset"> The position at which to write the value</param>
        /// <param name="value"> The value to be written</param>
        /// <returns> The offset right after the written value.</returns>
        public int SetBool(int offset, bool value)
        {
            m_Accessor.Write(offset, value);
            return offset + 1;
        }

        /// <summary>
        /// Sets the string at the specified offset in the shared memory.
        /// Note that the string is represented with a sbyte (8-bit unsigned) number
        /// encoding the length of the string and the string bytes in ASCII.
        /// </summary>
        /// <param name="offset"> The position at which to write the value</param>
        /// <param name="value"> The value to be written</param>
        /// <returns> The offset right after the written value.</returns>
        public int SetString(int offset, string value)
        {
            var bytes = Encoding.ASCII.GetBytes(value);
            int length = bytes.Length;
            m_Accessor.Write(offset, (sbyte)length);
            offset += 1;
            m_Accessor.WriteArray(offset, bytes, 0, length);
            offset += length;
            return offset;
        }

        /// <summary>
        /// Copies the values present in a NativeArray to the shared memory file.
        /// </summary>
        /// <param name="offset"> The position at which to write the value</param>
        /// <param name="array"> The NativeArray containing the data to write</param>
        /// <param name="length"> The size of the array in bytes</param>
        /// <typeparam name="T"> The type of the NativeArray. Must be a struct.</typeparam>
        /// <returns> The offset right after the written value.</returns>
        public int SetArray<T>(int offset, NativeArray<T> array, int length) where T : struct
        {
            IntPtr dst = IntPtr.Add(m_AccessorPointer, offset);
            IntPtr src = new IntPtr(array.GetUnsafePtr());
            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), length, length);
            return offset + length;
        }

        /// <summary>
        /// Sets the byte array at the specified offset in the shared memory.
        /// </summary>
        /// <param name="offset"> The position at which to write the value</param>
        /// <param name="value"> The value to be written</param>
        /// <returns> The offset right after the written value.</returns>
        public int SetBytes(int offset, byte[] value)
        {
            m_Accessor.WriteArray(offset, value, 0, value.Length);
            return offset + value.Length;
        }

        /// <summary>
        /// Closes the shared memory reader and writter. This will not delete the file but
        /// remove access to it.
        /// </summary>
        public void Close()
        {
            if (m_Accessor.CanWrite)
            {
                m_Accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                m_Accessor.Dispose();
            }
        }

        /// <summary>
        /// Indicates wether the <see cref="BaseSharedMemory"/> has write access to the file.
        /// If it does not, it means the accessor was closed or the file was deleted.
        /// </summary>
        /// <value></value>
        protected bool CanEdit
        {
            get
            {
                return m_Accessor.CanWrite;
            }
        }

        /// <summary>
        /// Closes and deletes the shared memory file.
        /// </summary>
        public void Delete()
        {
            Close();
            File.Delete(m_FilePath);
        }
    }
}
