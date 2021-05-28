using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Devsense.PHP.Phar
{
    /// <summary>
    /// Represents a PHAR file.
    /// </summary>
    public sealed class PharFile
    {
        /// <summary>
        /// Reads and parses given PHAR file.
        /// </summary>
        public static PharFile OpenPharFile(string filename)
        {
            if (filename == null)
                throw new ArgumentNullException(nameof(filename));

            byte[] zip_magic = new byte[]{(byte)'P', (byte)'K', 0x03, 0x04};
            byte[] gz_magic = new byte[] { 0x1f, 0x8b, 0x08 };
            byte[] bz_magic = new byte[] { (byte)'B', (byte)'Z', (byte)'h' };

            using (var stream = File.OpenRead(filename))
            {
                byte[] buffer = new byte[4];
                stream.Read(buffer, 0, buffer.Length);

                if (buffer.IsPrefixed(zip_magic))
                {
                    throw new NotSupportedException("zip");
                }
                else if (buffer.IsPrefixed(gz_magic))
                {
                    throw new NotSupportedException("gz");
                }
                else if (buffer.IsPrefixed(bz_magic))
                {
                    throw new NotSupportedException("bz");
                }
                else 
                {
                    // TODO: try to open as tar // https://code.google.com/p/tar-cs/

                    stream.Seek(0, SeekOrigin.Begin);
                    return ReadPharFile(filename, stream);
                }
            }
        }

        private static string ReadStub(Stream stream)
        {
            // look for token
            byte[] token = Encoding.ASCII.GetBytes("__HALT_COMPILER();");   // + optionally ['whitespace', '?', '>'] [\r] [\n]

            List<byte> stub = new List<byte>();
            int b;
            while ((b = stream.ReadByte()) >= 0)
            {
                stub.Add((byte)b);
                if (stub.IsSuffixed(token))
                {
                    byte[] buffer = new byte[5];
                    if (stream.Read(buffer, 0, buffer.Length) != buffer.Length)
                        throw new FormatException();

                    int offset = 0;
                    if (buffer[0] == (byte)' ' || buffer[0] == (byte)'\n')
                        if (buffer[1] == (byte)'?')
                            if (buffer[2] == (byte)'>')
                            {
                                offset += 3;

                                if (buffer[3] == (byte)'\r')
                                {
                                    if (buffer[4] != (byte)'\n')
                                        throw new FormatException();

                                    offset += 2;
                                }
                                else if (buffer[3] == (byte)'\n')
                                {
                                    offset += 1;
                                }
                            }

                    stream.Seek(-buffer.Length + offset, SeekOrigin.Current);
                    stub.AddRange(buffer.Take(offset));
                    
                    //
                    return Encoding.UTF8.GetString(stub.ToArray());
                }
            }

            return null;
        }

        private static Manifest ReadManifest(Stream stream)
        {
            var reader = new BinaryReader(stream);

            uint manifestLength = reader.ReadUInt32();
            if (manifestLength < 10)
                throw new FormatException();

            return Manifest.Create(reader);
        }

        private static PharFile ReadPharFile(string filename, Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var stub = ReadStub(stream);
            var manifest = ReadManifest(stream);

            return new PharFile(filename, stub, manifest);
        }

        private readonly string _filename;
        private readonly string _stub;
        private readonly Manifest _manifest;

        /// <summary>
        /// Gets file name of the PHAR file if any.
        /// </summary>
        public string FileName { get { return _filename; } }

        /// <summary>
        /// Gets the stub.
        /// </summary>
        public string StubCode { get { return _stub; } }

        /// <summary>
        /// Gets manifest with contained files.
        /// </summary>
        public Manifest/*!*/Manifest { get { return _manifest; } }

        private PharFile(string filename, string stub, Manifest manifest)
        {
            _filename = filename;
            _stub = stub;
            _manifest = manifest;
        }
    }
}
