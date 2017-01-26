using System;

namespace WorldSaver
{
    public struct WorldFormat
    {
        string name;
        int version;

        public string Name
        {
            get { return name; }
        }

        public int Version
        {
            get { return version; }
        }

        public WorldFormat(string name, int version)
        {
            this.name = name;
            this.version = version;
        }

        public WorldFormat(string formatID)
        {
            int versionIndex = formatID.LastIndexOf('_');

            if (versionIndex == -1) throw new ArgumentException("Format must contain a version number.");

            if (!int.TryParse(formatID.Substring(versionIndex + 1), out version)) throw new ArgumentException("Invalid version number");

            name = formatID.Substring(0, versionIndex);
        }

        public static bool operator ==(WorldFormat f1, WorldFormat f2)
        {
            return f1.name == f2.name && f1.version == f2.version;
        }

        public static bool operator !=(WorldFormat f1, WorldFormat f2)
        {
            return !(f1 == f2);
        }

        public static bool operator >(WorldFormat f1, WorldFormat f2)
        {
            return f1.name == f2.name && f1.version > f2.version;
        }

        public static bool operator <(WorldFormat f1, WorldFormat f2)
        {
            return f1.name == f2.name && f1.version < f2.version;
        }

        public static bool operator >=(WorldFormat f1, WorldFormat f2)
        {
            return !(f1 < f2);
        }

        public static bool operator <=(WorldFormat f1, WorldFormat f2)
        {
            return !(f1 > f2);
        }

        public override string ToString()
        {
            return name + "_" + version;
        }

        public static WorldFormat STANDARD
        {
            get { return new WorldFormat("standard", 2); }
        }

        public static WorldFormat SIMPLE
        {
            get { return new WorldFormat("simple", 1); }
        }
    }
}
