using System;
using System.Text;

namespace LazyFetcher
{

    [Serializable]
    public class ServerVersion
    {
        public int major;
        public int minor;
        public int[] subversions;

        public ServerVersion(string version)
        {
            string[] parts = version.Split('.');
            if (parts.Length >= 2)
            {
                int.TryParse(parts[0], out major);
                int.TryParse(parts[1], out minor);

                if (parts.Length > 2)
                {
                    subversions = new int[parts.Length - 2];
                    for (int i = 2; i < parts.Length; i++)
                    {
                        int.TryParse(parts[i], out subversions[i - 2]);
                    }
                }
            }
            else
            {
                throw new ArgumentException("Invalid version format. Expected format: major.minor[.subversion1.subversion2...]");
            }
        }

        public ServerVersion(int major, int minor, params int[] subversions)
        {
            this.major = major;
            this.minor = minor;
            this.subversions = subversions;
        }


        public bool IsHigherThan(ServerVersion other)
        {
            //If other Version is null- considered as outdated
            if (other == null)
                return true;

            // Compare major versions
            if (major > other.major)
                return true;
            else if (major < other.major)
                return false;

            // Compare minor versions
            if (minor > other.minor)
                return true;
            else if (minor < other.minor)
                return false;

            // Compare subversions
            int minLength = Math.Min(subversions.Length, other.subversions.Length);
            for (int i = 0; i < minLength; i++)
            {
                if (subversions[i] > other.subversions[i])
                    return true;
                else if (subversions[i] < other.subversions[i])
                    return false;
            }

            // If all parts are equal, consider the versions equal
            return false;
        }

        public override string ToString()
        {
            StringBuilder versionBuilder = new StringBuilder();
            versionBuilder.Append(major);
            versionBuilder.Append('.');
            versionBuilder.Append(minor);

            if (subversions != null)
            {
                for (int i = 0; i < subversions.Length; i++)
                {
                    versionBuilder.Append('.');
                    versionBuilder.Append(subversions[i]);
                }
            }

            return versionBuilder.ToString();
        }
    }
}