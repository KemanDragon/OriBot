using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OldOriBot.Data.Persistence;

namespace OriBot.Framework.Extensions
{
    public static class BinaryWriterExtensions
    {
        /// <summary>
        /// Given the <see cref="string"/> <paramref name="str"/>, this will write its contents as an array of UTF-8 <see cref="char"/>s with no preceeding length.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="str"></param>
        /// <exception cref="ArgumentNullException">If any of the two arguments are null.</exception>
        public static void WriteAsChars(this BinaryWriter writer, string str)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (str == null) throw new ArgumentNullException(nameof(str));

            byte[] text = Encoding.UTF8.GetBytes(str);
            foreach (byte b in text)
            {
                writer.Write(b);
            }
        }

        /// <summary>
        /// Reads the given <paramref name="amount"/> of UTF-8 <see cref="char"/>s from the stream and returns them as a <see cref="string"/>.<para/>
        /// This is hilariously prone to buffer overflow exploits c:
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If any of the two arguments are null.</exception>
        public static string ReadChars(this BinaryReader reader, int amount)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (amount == 0) return string.Empty;

            // IS SO FAEST...,,,
            StringBuilder builder = new StringBuilder(amount);
            for (int count = 0; count < amount; count++)
            {
                builder.Append(reader.ReadByte());
            }
            return builder.ToString();
        }

        /// <summary>
        /// Writes the given string as a Unicode string with a length preceeding it as a <see cref="ushort"/>.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="str"></param>
        /// <param name="encoding">The default encoding to use. If <see langword="null"/>, it will be treated as <see cref="Encoding.Unicode"/></param>
        /// <exception cref="ArgumentOutOfRangeException">If the given string is longer than 65535 characters long.</exception>
        /// <exception cref="ArgumentNullException">If any of the two arguments are null.</exception>
        public static void WriteStringSL(this BinaryWriter writer, string str, Encoding encoding = null)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (str == null) throw new ArgumentNullException(nameof(str));
            encoding ??= Encoding.Unicode;

            byte[] text = encoding.GetBytes(str);
            if (text.Length > ushort.MaxValue) throw new ArgumentOutOfRangeException(nameof(str));

            writer.Write((ushort)text.Length);
            foreach (byte b in text)
            {
                writer.Write(b);
            }
        }

        /// <summary>
        /// Reads the given string as a Unicode string with a length preceeding it as a <see cref="ushort"/>.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="encoding">The default encoding to use. If <see langword="null"/>, it will be treated as <see cref="Encoding.Unicode"/></param>
        /// <exception cref="ArgumentOutOfRangeException">If the given string is longer than 65535 characters long.</exception>
        /// <exception cref="ArgumentNullException">If any of the two arguments are null.</exception>
        public static string ReadStringSL(this BinaryReader reader, Encoding encoding = null)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            encoding ??= Encoding.Unicode;

            byte[] chars = new byte[reader.ReadUInt16()];
            reader.Read(chars, 0, chars.Length);

            return encoding.GetString(chars);
        }

        /// <summary>
        /// Writes a list of up to <see cref="ushort.MaxValue"/> <see cref="IBinaryReadWrite"/> elements to the <see cref="BinaryWriter"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="writables"></param>
        /// <returns></returns>
        public static void WriteEntries(this BinaryWriter writer, IEnumerable<IBinaryReadWrite> writables)
        {
            writer.Write((ushort)writables.Count());
            foreach (IBinaryReadWrite obj in writables)
            {
                obj.Write(writer);
            }
        }

        /// <summary>
        /// Writes a list of up to <see cref="ushort.MaxValue"/> <see cref="IMetadataReceiver"/> elements to the <see cref="BinaryWriter"/>
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="writables"></param>
        /// <returns></returns>
        public static void WriteMetaEntries(this BinaryWriter writer, IEnumerable<IMetadataReceiver> writables, params object[] extraData)
        {
            writer.Write((ushort)writables.Count());
            foreach (IMetadataReceiver obj in writables)
            {
                obj.ReceiveMetadata(extraData);
                obj.Write(writer);
            }
        }

        /// <summary>
        /// Reads a list of up to <see cref="ushort.MaxValue"/> <see cref="IBinaryReadWrite"/> elements from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T[] ReadEntries<T>(this BinaryReader reader) where T : IBinaryReadWrite, new()
        {
            ushort numEntries = reader.ReadUInt16();
            T[] elements = new T[numEntries];
            for (int idx = 0; idx < numEntries; idx++)
            {
                T obj = new T();
                obj.Read(reader);
                elements[idx] = obj;
            }
            return elements;
        }

        /// <summary>
        /// Reads a list of up to <see cref="ushort.MaxValue"/> <see cref="IMetadataReceiver"/> elements from the <see cref="BinaryReader"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <param name="extraData"></param>
        /// <returns></returns>
        public static T[] ReadMetaEntries<T>(this BinaryReader reader, params object[] extraData) where T : IMetadataReceiver, new()
        {
            ushort numEntries = reader.ReadUInt16();
            T[] elements = new T[numEntries];
            for (int idx = 0; idx < numEntries; idx++)
            {
                T obj = new T();
                obj.ReceiveMetadata(extraData);
                obj.Read(reader);
                elements[idx] = obj;
            }
            return elements;
        }
    }
}
