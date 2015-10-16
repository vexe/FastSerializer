using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Vexe.Fast.Serializer.Internal;

namespace Vexe.Fast.Serializer.Serializers
{
    public class Basic : IBaseSerializer
    {
        public static readonly Type[] SupportedTypes = new Type[]
        {
            typeof(byte),
            typeof(sbyte),
            typeof(byte[]),
            typeof(bool),
            typeof(int),
            typeof(uint),
            typeof(short),
            typeof(ushort),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(char),
            typeof(string),
            typeof(DateTime),
        };

        public override bool CanHandle(Type type)
        {
            for (int i = 0; i < SupportedTypes.Length; i++)
                if (SupportedTypes[i] == type)
                    return true;
            return false;
        }

        public override Type[] GetTypeDependency(Type type)
        {
            return Type.EmptyTypes;
        }

        public override void EmitWrite(Type type)
        {
            var writer = GetWriter(type);
            EmitHelper.EmitCall(writer, type == typeof(string) ? 3 : 2);
        }

        public override void EmitRead(Type type)
        {
            var reader = GetReader(type);
            EmitHelper.EmitCall(reader, type == typeof(string) ? 2 : 1);
        }

        public static MethodInfo GetWriter(Type forType)
        {
            var getter = _getWriter ?? (_getWriter = new Func<Type, MethodInfo>(x =>
            {
                if (x == typeof(string))
                    return typeof(Basic).GetMethod("Write_String");

                string methodName = "Write" + x.Name;
                if (x.IsArray)
                    methodName = methodName.Replace("[]", "Array");
                var method = typeof(Basic).GetMethod(methodName, new Type[] { typeof(Stream), x });
                if (method == null)
                    throw new InvalidOperationException("Method not found: " + methodName);
                return method;
            }).Memoize());

            return getter(forType);
        }

        public static MethodInfo GetWriter<T>()
        {
            return GetWriter(typeof(T));
        }

        public static MethodInfo GetReader(Type forType)
        {
            var getter = _getReader ?? (_getReader = new Func<Type, MethodInfo>(x =>
            {
                if (x == typeof(string))
                    return typeof(Basic).GetMethod("Read_String");
                
                var methodName = "Read" + x.Name;
                if (x.IsArray)
                    methodName = methodName.Replace("[]", "Array");
                var method = typeof(Basic).GetMethod(methodName, new Type[] { typeof(Stream) });
                if (method == null)
                    throw new InvalidOperationException("Method not found: " + methodName);
                return method;
            }).Memoize());

            return getter(forType);
        }

        public static MethodInfo GetReader<T>()
        {
            return GetReader(typeof(T));
        }

        public static void WriteInt16(Stream stream, short value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            stream.Write(_buffer, 0, 2);
        }

        public static short ReadInt16(Stream stream)
        {
            stream.Read(_buffer, 0, 2);
            return (short)((int)_buffer[0] | (int)_buffer[1] << 8);
        }

        public static void WriteUInt16(Stream stream, ushort value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            stream.Write(_buffer, 0, 2);
        }

        public static ushort ReadUInt16(Stream stream)
        {
            stream.Read(_buffer, 0, 2);
            return (ushort)((int)_buffer[0] | (int)_buffer[1] << 8);
        }

        public static void WriteUInt32(Stream stream, uint value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            stream.Write(_buffer, 0, 4);
        }

        public static uint ReadUInt32(Stream stream)
        {
            stream.Read(_buffer, 0, 4);
            return (uint)((int)_buffer[0] | (int)_buffer[1] << 8 | (int)_buffer[2] << 16 | (int)_buffer[3] << 24);
        }

        public static void WriteInt32(Stream stream, int value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            stream.Write(_buffer, 0, 4);
        }

        public static int ReadInt32(Stream stream)
        {
            stream.Read(_buffer, 0, 4);
            var result = (int)_buffer[0] | (int)_buffer[1] << 8 | (int)_buffer[2] << 16 | (int)_buffer[3] << 24;
            return result;
        }

        public static void WriteInt64(Stream stream, long value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            _buffer[4] = (byte)(value >> 32);
            _buffer[5] = (byte)(value >> 40);
            _buffer[6] = (byte)(value >> 48);
            _buffer[7] = (byte)(value >> 56);
            stream.Write(_buffer, 0, 8);
        }

        public static long ReadInt64(Stream stream)
        {
            stream.Read(_buffer, 0, 8);
            uint x1 = (uint)((int)_buffer[0] | (int)_buffer[1] << 8 | (int)_buffer[2] << 16 | (int)_buffer[3] << 24);
            uint x2 = (uint)((int)_buffer[4] | (int)_buffer[5] << 8 | (int)_buffer[6] << 16 | (int)_buffer[7] << 24);
            return (long)((ulong)x2 << 32 | (ulong)x1);
        }

        public static void WriteUInt64(Stream stream, ulong value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            _buffer[4] = (byte)(value >> 32);
            _buffer[5] = (byte)(value >> 40);
            _buffer[6] = (byte)(value >> 48);
            _buffer[7] = (byte)(value >> 56);
            stream.Write(_buffer, 0, 8);
        }

        public static ulong ReadUInt64(Stream stream)
        {
            stream.Read(_buffer, 0, 8);
            uint x1 = (uint)((int)_buffer[0] | (int)_buffer[1] << 8 | (int)_buffer[2] << 16 | (int)_buffer[3] << 24);
            uint x2 = (uint)((int)_buffer[4] | (int)_buffer[5] << 8 | (int)_buffer[6] << 16 | (int)_buffer[7] << 24);
            return (ulong)x2 << 32 | (ulong)x1;
        }

        public static unsafe void WriteSingle(Stream stream, float value)
        {
            uint num = *(uint*)(&value);
            _buffer[0] = (byte)num;
            _buffer[1] = (byte)(num >> 8);
            _buffer[2] = (byte)(num >> 16);
            _buffer[3] = (byte)(num >> 24);
            stream.Write(_buffer, 0, 4);
        }

        public unsafe static float ReadSingle(Stream stream)
        {
            stream.Read(_buffer, 0, 4);
            uint num = (uint)((int)_buffer[0] | (int)_buffer[1] << 8 | (int)_buffer[2] << 16 | (int)_buffer[3] << 24);
            return *(float*)(&num);
        }

        public static unsafe void WriteDouble(Stream stream, double value)
        {
            ulong num = (ulong)(*(long*)(&value));
            _buffer[0] = (byte)num;
            _buffer[1] = (byte)(num >> 8);
            _buffer[2] = (byte)(num >> 16);
            _buffer[3] = (byte)(num >> 24);
            _buffer[4] = (byte)(num >> 32);
            _buffer[5] = (byte)(num >> 40);
            _buffer[6] = (byte)(num >> 48);
            _buffer[7] = (byte)(num >> 56);
            stream.Write(_buffer, 0, 8);
        }

        public unsafe static double ReadDouble(Stream stream)
        {
            stream.Read(_buffer, 0, 8);
            uint x1 = (uint)((int)_buffer[0] | (int)_buffer[1] << 8 | (int)_buffer[2] << 16 | (int)_buffer[3] << 24);
            uint x2 = (uint)((int)_buffer[4] | (int)_buffer[5] << 8 | (int)_buffer[6] << 16 | (int)_buffer[7] << 24);
            ulong x3 = (ulong)x2 << 32 | (ulong)x1;
            return *(double*)(&x3);
        }

        public static void WriteBoolean(Stream stream, bool value)
        {
            _buffer[0] = (byte)(value ? 1 : 0);
            stream.Write(_buffer, 0, 1);
        }

        public static bool ReadBoolean(Stream stream)
        {
            return stream.ReadByte() != 0;
        }

        public static void WriteByte(Stream stream, byte value)
        {
            _buffer[0] = value;
            stream.Write(_buffer, 0, 1);
        }

        public static byte ReadByte(Stream stream)
        {
            stream.Read(_buffer, 0, 1);
            return _buffer[0];
        }

        public static void WriteSByte(Stream stream, sbyte value)
        {
            WriteByte(stream, (byte)value);
        }

        public static sbyte ReadSByte(Stream stream)
        {
            return (sbyte)ReadByte(stream);
        }

        public static void WriteByteArray(Stream stream, byte[] value)
        {
            WriteByteArray(stream, value, value.Length);
        }

        public static void WriteByteArray(Stream stream, byte[] value, int count)
        {
            if (value == null)
            {
                WriteInt32(stream, -1);
                return;
            }

            if (value.Length == 0)
            {
                WriteInt32(stream, 0);
                return;
            }

            WriteInt32(stream, count);
            stream.Write(value, 0, count);
        }

        public static byte[] ReadByteArray(Stream stream)
        {
            int length = ReadInt32(stream);

            if (length == -1)
                return null;

            if (length == 0)
                return _emptyBuffer;

            var array = new byte[length];
            stream.Read(array, 0, length);
            return array;
        }

        public static void WriteChar(Stream stream, char value)
        {
            WriteInt16(stream, (short)value);
        }

        public static char ReadChar(Stream stream)
        {
            return (char)ReadInt16(stream);
        }

        public static void WriteDateTime(Stream stream, DateTime value)
        {
            WriteInt64(stream, value.ToBinary());
        }

        public static DateTime ReadDateTime(Stream stream)
        {
            return DateTime.FromBinary(ReadInt64(stream));
        }

        // called by serializer. shouldn't be touched by user
        public static void Write_String(Stream stream, string value, SerializationContext ctx)
        {
            //WriteString(stream, value);
            //return;

            if (value == null)
            {
                WriteInt32(stream, -1);
                return;
            }

            int strId = ctx.Cache.GetStrId(value);

            WriteInt32(stream, strId);

            if (ctx.Cache.IsStrMarked(strId))
            {
                WriteBoolean(stream, true);
                return;
            }

            WriteBoolean(stream, false);

            ctx.Cache.MarkStr(value, strId);

            WriteStringDirect(stream, value);
        }

        public static void WriteString(Stream stream, string value)
        {
            if (value == null)
            {
                WriteInt32(stream, -1);
                return;
            }

            WriteStringDirect(stream, value);
        }

        private static void WriteStringDirect(Stream stream, string value)
        {
            if (value.Length == 0)
            {
                WriteInt32(stream, 0);
                return;
            }

            //var byteCount = Encoding.Unicode.GetByteCount(value);
            //WriteInt32(stream, byteCount);
            //Encoding.Unicode.GetBytes(value, 0, value.Length, _strBuffer, 0);
            //stream.Write(_strBuffer, 0, byteCount);

            int charCount = value.Length;
            int byteCount = charCount * 2;

            WriteInt32(stream, byteCount);

            var buffer = byteCount < _strBuffer.Length ?
                _strBuffer : new byte[byteCount];

            StringToBytes(value, buffer, byteCount);

            stream.Write(buffer, 0, byteCount);
        }

        // called by serializer. shouldn't be touched by user
        public static string Read_String(Stream stream, SerializationContext ctx)
        {
            //return ReadString(stream);

            int refId = ReadInt32(stream);
            if (refId == -1)
                return null;

            bool isMarked = ReadBoolean(stream);
            if (isMarked)
                return ctx.Cache.GetStr(refId);

            int byteCount = ReadInt32(stream);
            string result = ReadStringDirect(stream, byteCount);
            ctx.Cache.MarkStr(result, refId);
            return result;
        }

        public static string ReadString(Stream stream)
        {
            int byteCount = ReadInt32(stream);
            if (byteCount == -1)
                return null;

            return ReadStringDirect(stream, byteCount);
        }

        private static string ReadStringDirect(Stream stream, int byteCount)
        {
            if (byteCount == 0)
                return string.Empty;

            //stream.Read(_strBuffer, 0, byteCount);
            //var result = Encoding.Unicode.GetString(_strBuffer, 0, byteCount);
            //return result;

            var buffer = byteCount > _strBuffer.Length ?
                new byte[byteCount] : _strBuffer;

            stream.Read(buffer, 0, byteCount);

            string result = BytesToString(buffer, byteCount / 2);
            return result;
        }

        static unsafe void StringToBytes(string input, byte[] output, int byteCount)
        {
            fixed (void* ptr = input)
                Marshal.Copy((IntPtr)ptr, output, 0, byteCount);
        }

        static unsafe string BytesToString(byte[] bytes, int charCount)
        {
            fixed (byte* ptr = bytes)
                return new string((char*)ptr, 0, charCount);
        }

        static readonly byte[] _buffer = new byte[8];
        static readonly byte[] _emptyBuffer = new byte[0];
        static readonly byte[] _strBuffer = new byte[512];

        static Func<Type, MethodInfo> _getWriter;
        static Func<Type, MethodInfo> _getReader;
    }
}
