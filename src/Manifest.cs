using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Devsense.PHP.Phar
{
    #region EntryKey

    [DebuggerDisplay("{Name}")]
    public readonly struct EntryKey : IEquatable<EntryKey>
    {
        public string Name { get; }

        public bool IsDir { get; }

        public EntryKey(string name, bool isdir)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.IsDir = isdir;
        }

        public static EntryKey File(string name) => new EntryKey(name, false);

        public static EntryKey Directory(string name) => new EntryKey(name, true);

        public override bool Equals(object obj) => obj is EntryKey key && Equals(key);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Name) ^ (IsDir ? 0x3 : 0xf);

        public bool Equals(EntryKey other) => string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) && IsDir == other.IsDir;

        public override string ToString() => Name;

        /// <summary>
        /// Implicit conversion to a file entry key.
        /// </summary>
        public static implicit operator EntryKey(string filename) => File(filename);
    }

    #endregion

    /// <summary>
    /// Represents PHAR manifest section.
    /// </summary>
    public sealed class Manifest
    {
        #region Nested class: EntryKeyComparer

        sealed class EntryKeyComparer : IEqualityComparer<EntryKey>
        {
            public static EntryKeyComparer Instance { get; } = new EntryKeyComparer();

            private EntryKeyComparer() { }

            public bool Equals(EntryKey x, EntryKey y) => x.Equals(y);

            public int GetHashCode(EntryKey obj) => obj.GetHashCode();
        }

        #endregion

        private uint _entriesCount;
        private Version _manifestVersion;
        private uint _flags;
        private string _alias;
        private byte[] _metadata;
        private Dictionary<EntryKey, Entry>/*!!*/_entries;

        /// <summary>
        /// Gets the PHAR version.
        /// </summary>
        public Version ManifestVersion { get { return _manifestVersion; } }

        /// <summary>
        /// Gets contained PHAR entries.
        /// </summary>
        public Dictionary<EntryKey, Entry> Entries { get { return _entries; } }

        /// <summary>
        /// Gets entry corresponding to given name (file name or directory name).
        /// If there is both file and directory with the same name, file entry is returned.
        /// </summary>
        public Entry this[string name]
        {
            get
            {
                return
                    _entries.TryGetValue(EntryKey.File(name), out var entry) ||
                    _entries.TryGetValue(EntryKey.Directory(name), out entry)
                    ? entry
                    : null;
            }
        }

        /// <summary>
        /// Gets PHAR entry corresponding to given file.
        /// </summary>
        public Entry GetFileEntry(string name)
        {
            return _entries.TryGetValue(EntryKey.File(name), out var entry) ? entry : null;
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
                throw new ArgumentNullException(nameof(reader));

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
            _entries = new Dictionary<EntryKey, Entry>((int)_entriesCount, EntryKeyComparer.Instance);

            var list = new List<Entry>((int)_entriesCount);
            var supportsDir = _manifestVersion >= DirSupportVersion;

            // read entries
            for (int entryindex = 0; entryindex < _entriesCount; entryindex++)
            {
                list.Add(Entry.CreateEntry(reader, supportsDir));
            }

            // read entries content
            for (int entryindex = 0; entryindex < list.Count; entryindex++)
            {
                var entry = list[entryindex];

                entry.InitializeContent(reader);

                _entries.Add(new EntryKey(entry.Name, isdir: !entry.IsFile), entry);
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

        public static Manifest/*!*/CreateEmpty()
        {
            return new Manifest()
            {
                _entriesCount = 0,
                _entries = new Dictionary<EntryKey, Entry>(),
            };
        }
    }
}
