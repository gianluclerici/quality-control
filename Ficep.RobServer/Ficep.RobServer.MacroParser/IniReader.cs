using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Ficep.AnyCut.Common;

namespace Ficep.RobServer.MacroParser
{
    public class IniReader
    {
        private readonly string path;
        private string exe = Assembly.GetExecutingAssembly().GetName().Name;

        public IniReader(string IniPath = null)
        {
            path = new FileInfo(IniPath ?? exe + ".ini").FullName.ToString();
        }

        public string Read(string Key, string Section = null)
        {
            var RetVal = new StringBuilder(255);
            NativeMethods.GetPrivateProfileString(Section ?? exe, Key, "", RetVal, 255, path);
            return RetVal.ToString();
        }

        public void Write(string Key, string Value, string Section = null)
        {
            NativeMethods.WritePrivateProfileString(Section ?? exe, Key, Value, path);
        }

        public void DeleteKey(string Key, string Section = null)
        {
            Write(Key, null, Section ?? exe);
        }

        public void DeleteSection(string Section = null)
        {
            Write(null, null, Section ?? exe);
        }

        public bool KeyExists(string Key, string Section = null)
        {
            return Read(Key, Section).Length > 0;
        }
    }
}
