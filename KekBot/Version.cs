using System;
using System.Text.RegularExpressions;
using KekBot.Utils;

namespace KekBot {
    //Direct port from Java version with minor edits, could probably be optimized better.
    internal class Version {
        private int MajorVersion;
        private int MinorVersion;
        private int PatchVersion;
        private int BetaVersion;

        public string VersionString { get {
                return $"{MajorVersion}.{MinorVersion}.{PatchVersion}" + (BetaVersion > 0 ? $"-BETA{BetaVersion}" : "");
            } }

        public Version(int majorVersion, int minorVersion, int patchVersion, int betaVersion) {
            this.MajorVersion = majorVersion;
            this.MinorVersion = minorVersion;
            this.PatchVersion = patchVersion;
            this.BetaVersion = betaVersion;
        }

        public Version(int majorVersion, int minorVersion, int patchVersion) {
            this.MajorVersion = majorVersion;
            this.MinorVersion = minorVersion;
            this.PatchVersion = patchVersion;
            this.BetaVersion = 0;
        }

        public Version(int majorVersion, int minorVersion) {
            this.MajorVersion = majorVersion;
            this.MinorVersion = minorVersion;
            this.PatchVersion = 0;
            this.BetaVersion = 0;
        }

        Version(int majorVersion) {
            this.MajorVersion = majorVersion;
            this.MinorVersion = 0;
            this.PatchVersion = 0;
            this.BetaVersion = 0;
        }

        public static Version fromString(String version) {
            String[] parts = Regex.Split(version, "\\.|-BETA");
            if (parts.Length == 4) {
                return new Version(Util.ParseInt(parts[0], 1), Util.ParseInt(parts[1], 0), Util.ParseInt(parts[2], 0), Util.ParseInt(parts[3], 1));
            } else if (parts.Length == 3) {
                return new Version(Util.ParseInt(parts[0], 1), Util.ParseInt(parts[1], 0), Util.ParseInt(parts[2], 0));
            } else if (parts.Length == 2) {
                return new Version(Util.ParseInt(parts[0], 1), Util.ParseInt(parts[1], 0));
            } else if (parts.Length == 1) {
                return new Version(Util.ParseInt(parts[0], 1));
            }
            return new Version(1);
        }

        public bool isHigherThan(Version? version) {
            if (version == null || this.MajorVersion > version.MajorVersion) {
                return true;
            } else if (this.MajorVersion == version.MajorVersion) {
                if (this.MinorVersion > version.MinorVersion) {
                    return true;
                } else if (this.MinorVersion == version.MinorVersion) {
                    if (this.PatchVersion > version.PatchVersion) {
                        return true;
                    } else if (this.PatchVersion == version.PatchVersion) {
                        if (this.PatchVersion > version.PatchVersion)
                            return true;
                    }
                }
            }
            return false;
        }
    }
}
