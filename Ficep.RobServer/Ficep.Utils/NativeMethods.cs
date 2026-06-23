using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Ficep.AnyCut.Common
{
    public class NativeMethods
    {

        [DllImport("User32.dll", CharSet = CharSet.Unicode)]
        public static extern int MessageBox(IntPtr h, string m, string c, int type);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        public static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        public static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        public static extern int GetPrivateProfileInt(string Section, string Key, int Default, string FilePath);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        //  Windows messages
        public const uint WM_CLOSE = 0x0010;
        //  SmartRob message
        public const uint WM_SMARTROB_RELOAD_PRG = 0x0400 + 5;

        //  Import IRobot.dll, per utilizzare funzioni AIROBOT
        //
        //  Regole per l'utilizzo di PInvoke con DLL scritta in C++:
        //
        //  1)  Nella DLL C++, va necessariamente specificato il blocco "C"
        //      extern "C" __FMATHLIB__ double Distance(double, double, double, double, double, double);
        //
        //  2)  Nel codice C#, va specificato CallingConvention = CallingConvention.Cdecl
        //
        [DllImport("libs\\IRobot.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern long FILE_ConvertSTLToXML(string STLFile, string XMLFile, string FrameMapping, string ConfigIni);

        [DllImport("libs\\IRobot.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern long ProcessProgram(string PathName);

        //
        //  Lettura chiave da sezione di un file
        //
        public static string _GetPrivateProfileString(string section, string key, string defaultValue, string filePath)
        {
            var retVal = new StringBuilder(255);
            NativeMethods.GetPrivateProfileString(section, key, defaultValue, retVal, 255, filePath);
            return retVal.ToString();
        }

        //
        //  Scrittura chiave all'interno della sezione di un file
        //
        public static long _WritePrivateProfileString(string Section, string Key, string Value, string FilePath)
        {
            return NativeMethods.WritePrivateProfileString(Section, Key, Value, FilePath);
        }

    }
}
