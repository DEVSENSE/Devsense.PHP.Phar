using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Devsense.PHP.Phar
{
    /// <summary>
    /// Represents PHAR manifest section.
    /// </summary>
    public sealed class Manifest
    {
        private uint _entriesCount;
        private Version _manifestVersion;
        private uint _flags;
        private string _alias;
        private byte[] _metadata;
        private Dictionary<string, Entry>/*!!*/_entries;

        /// <summary>
        /// Gets the PHAR version.
        /// </summary>
        public Version ManifestVersion { get { return _manifestVersion; } }

        /// <summary>
        /// Gets contained PHAR entries.
        /// </summary>
        public Dictionary<string, Entry> Entries { get { return _entries; } }

        /// <summary>
        /// Gets entry corresponding to given name (file name or directory name).
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Entry this[string name]
        {
            get
            {
                Entry entry;
                _entries.TryGetValue(name, out entry);
                return entry;
            }
        }

        /// <summary>
        /// Gets PHAR entry corresponding to given file.
        /// </summary>
        public Entry GetFileEntry(string name)
        {
            Entry entry;
            if (_entries.TryGetValue(name, out entry) && entry != null && entry.IsFile)
                return entry;

            return null;
        }

        /// <summary>
        /// Minimal PHAR version supporting directories.
        /// </summary>
        private static Version DirSupportVersion = new Version(1, 1, 1);

        private Manifest()
        {

        }

        private static Version ParseManifestVersion(uint ver)
        {
            return new Version((int)(ver >> 12), (int)((ver >> 8) & 0xF), (int)((ver >> 4) & 0x0F));
        }

        private void Initialize(BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            _entriesCount = reader.ReadUInt32();
            _manifestVersion = ParseManifestVersion(((uint)reader.ReadByte() << 8) | ((uint)reader.ReadByte()) & 0xfff0);
            _flags = reader.ReadUInt32();
            uint aliasLength = reader.ReadUInt32();
            _alias = (aliasLength > 0) ? (Encoding.UTF8.GetString(reader.ReadBytes((int)aliasLength))) : null;
            uint metadataLength = reader.ReadUInt32();
            _metadata = reader.ReadBytes((int)metadataLength);
            
            // entries
            InitializeEntries(reader);
        }

        private void/*!!*/InitializeEntries(BinaryReader reader)
        {
            _entries = new Dictionary<string, Entry>((int)_entriesCount, StringComparer.OrdinalIgnoreCase);
            List<Entry> list = new List<Entry>((int)_entriesCount);

            bool supportsDir = _manifestVersion >= DirSupportVersion;

            // read entries
            for (int entryindex = 0; entryindex < _entriesCount; entryindex++)
            {
                list.Add(Entry.CreateEntry(reader, supportsDir));
            }

            // read entries content
            foreach (var entry in list)
            {
                entry.InitializeContent(reader);
                _entries.Add(entry.Name, entry);
            }
        }

        /// <summary>
        /// Creates <see cref="Manifest"/> and shifts reader to the end of entries.
        /// </summary>
        public static Manifest/*!*/Create(BinaryReader reader)
        {
            var manifest = new Manifest();
            manifest.Initialize(reader);
            return manifest;
        }
    }
}
