using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ficep.RobServer.MacroParser
{
    public class FNCParser
    {
        public string Path { get; private set; }
        public List<ISection> Sections { get; private set; }

        public FNCParser(string path)
        {
            if (path == null || path == string.Empty)
                return;

            Path = path;
            Sections = new List<ISection>();

            StreamReader sr = new StreamReader(Path);
            var s = Regex.Split(sr.ReadToEnd(), @"\[\[([^\]]*)\]\]");
            sr.Dispose();

            CreateSections(s);
        }

        private void CreateSections(string[] strings)
        {
            for (int i = 0; i < strings.Length; i++)
            {
                string s = strings[i];

                if (s == "PRF")
                {
                    string name = s;
                    i++;
                    s = strings[i];
                    Sections.Add(new PRF(name, s));
                }
                else if (s == "MAT")
                {
                    string name = s;
                    i++;
                    s = strings[i];
                    Sections.Add(new MAT(name, s));
                }
                else if (s == "PCS")
                {
                    string name = s;
                    i++;
                    s = strings[i];
                    Sections.Add(new PCS(name, s));
                }
            }
        }
    }
}
