using System;
using System.IO;
using System.Text;

namespace Devsense.PHP.Phar
{
    /// <summary>
    /// Represents single entry inside the PHAR file.
    /// </summary>
    public sealed class Entry
    {
        /// <summary>
        /// Flags byte for each file adheres to these bitmasks.
        /// </summary>
        [Flags]
        public enum EntryFlags : uint
        {
            /// <summary>
            /// Compression mask.
            /// </summary>
            PHAR_ENT_COMPRESSION_MASK = 0x0000F000,

            /// <summary>
            /// No compression flag. (zero)
            /// </summary>
            PHAR_ENT_COMPRESSED_NONE  = 0x00000000,

            /// <summary>
            /// GZ compression flag.
            /// </summary>
            PHAR_ENT_COMPRESSED_GZ    = 0x00001000,

            /// <summary>
            /// BZ2 compression flag.
            /// </summary>
            PHAR_ENT_COMPRESSED_BZ2   = 0x00002000,

            /// <summary>
            /// Permission mask.
            /// </summary>
            PHAR_ENT_PERM_MASK        = 0x000001FF,

            /// <summary>
            /// User permission mask.
            /// </summary>
            PHAR_ENT_PERM_MASK_USR    = 0x000001C0,

            /// <summary>
            /// Permission shift user.
            /// </summary>
            PHAR_ENT_PERM_SHIFT_USR   = 6,

            /// <summary>
            /// Group permission mask.
            /// </summary>
            PHAR_ENT_PERM_MASK_GRP    = 0x00000038,

            /// <summary>
            /// Permission group shift.
            /// </summary>
            PHAR_ENT_PERM_SHIFT_GRP   = 3,

            /// <summary>
            /// Other permission mask.
            /// </summary>
            PHAR_ENT_PERM_MASK_OTH    = 0x00000007,

            /// <summary>
            /// Defines file.
            /// </summary>
            PHAR_ENT_PERM_DEF_FILE    = 0x000001B6,

            /// <summary>
            /// Defines directory.
            /// </summary>
            PHAR_ENT_PERM_DEF_DIR     = 0x000001FF,
        }

        /// <summary>
        /// Gets value indicating whether the the content of the entry is compressed.
        /// </summary>
        public bool IsCompressed { get { return (_flags & EntryFlags.PHAR_ENT_COMPRESSION_MASK) != 0; } }

        /// <summary>
        /// Gets value indicating whether the entry represents a directory.
        /// </summary>
        public bool IsDirectory { get { return (_flags & EntryFlags.PHAR_ENT_PERM_DEF_DIR) == EntryFlags.PHAR_ENT_PERM_DEF_DIR; } }

        /// <summary>
        /// Gets value indicating whether the entry represents a file.
        /// </summary>
        public bool IsFile { get { return (_flags & EntryFlags.PHAR_ENT_PERM_DEF_FILE) == EntryFlags.PHAR_ENT_PERM_DEF_FILE; } }

        /// <summary>
        /// Gets the file name or directory name represented by the entry.
        /// </summary>
        public string Name { get { return _fileName; } }

        /// <summary>
        /// Gets the content of the entry.
        /// </summary>
        public string Code { get { return _content; } } // TODO: uncompress

        private string _fileName;
        private uint _timeStamp;
        private int _fileSize;
        private int _compressedSize;
        private uint _checksum;
        private EntryFlags _flags;
        private byte[] _metadata;
        private string _content;
        
        /// <summary>
        /// Reads the entry header and returns new <see cref="Entry"/> instance.
        /// </summary>
        public static Entry/*!*/CreateEntry(BinaryReader reader, bool supportsDir)
        {
            var entry = new Entry();
            entry.Initialize(reader, supportsDir);
            return entry;
        }

        private Entry()
        {
        }

        /// <summary>
        /// Initialize the entry properties. The methods reads the entry header from <paramref name="reader"/>. This is the first phase of the entry construction.
        /// </summary>
        /// <param name="reader">The reader pointing to the start of the entry header.</param>
        /// <param name="supportsDir">Whether PHAR version supports directory entries.</param>
        private void Initialize(BinaryReader reader, bool supportsDir)
        {
            uint filenameLength = reader.ReadUInt32();
            _fileName = Encoding.UTF8.GetString(reader.ReadBytes((int)filenameLength));
            bool isDir = supportsDir && _fileName[_fileName.Length - 1] == '/';
            _fileSize = (int)reader.ReadUInt32();        // Un-compressed file size in bytes
            _timeStamp = reader.ReadUInt32();            // Unix timestamp of file
            _compressedSize = (int)reader.ReadUInt32();  // Compressed file size in bytes
            uint checksum = reader.ReadUInt32();         // CRC32 checksum of un-compressed file contents
            uint flags = reader.ReadUInt32();            // Bit-mapped File-specific flags
            uint metadataLength = reader.ReadUInt32();   // Serialized File Meta-data length (0 for none)
            _metadata = reader.ReadBytes((int)metadataLength);

            if (isDir)
            {
                flags |= (uint)Entry.EntryFlags.PHAR_ENT_PERM_DEF_DIR;
                _fileName = _fileName.TrimEnd('/');
            }

            if (!IsCompressed && _fileSize != _compressedSize)
                throw new FormatException();

            _flags = (EntryFlags)flags;
            _checksum = checksum;
        }

        internal void InitializeContent(BinaryReader reader)
        {
            // TODO: check for compression flags
            if (this.IsCompressed)
                throw new NotImplementedException();

            _content = Encoding.UTF8.GetString(reader.ReadBytes((int)_compressedSize));
        }
    }
}
