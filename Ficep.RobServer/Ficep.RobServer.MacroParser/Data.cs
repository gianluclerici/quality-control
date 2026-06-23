using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ficep.RobServer.MacroParser
{
    public class MAT : ISection
    {
        public string Name { get; set; }
        public List<IDataLine> DataLines { get; set; }
        private string[] _sectionLines;
        private string _DataPattern = @"^\[.+\]$"; // Pattern that used in a Regex check if the string start and end with just 1 [ and 1 ] 

        public MAT(string name, string[] sectionLines)
        {
            Name = name;
            DataLines = new List<IDataLine>();
            _sectionLines = sectionLines.ToArray();
        }

        public MAT(string name, List<string> sectionLines)
        {
            Name = name;
            DataLines = new List<IDataLine>();
            _sectionLines = sectionLines.ToArray();
        }

        public MAT(string name, string sectionLines)
        {
            Name = name;
            DataLines = new List<IDataLine>();
            _sectionLines = sectionLines.Split('\n').Where(s => s != "" && s != "\r").Select(x => x.Trim('\r')).ToArray();
            CreateDataLines();
        }

        private void CreateDataLines()
        {
            string currentLine = _sectionLines[0];
            if (_sectionLines.Length > 1)
            {
                for (int i = 0; i < _sectionLines.Length - 1; i++)
                {
                    currentLine = _sectionLines[i];
                    string nextLine = _sectionLines[i + 1];

                    // Take the first token of the string and check if matches the data pattern
                    Match m = Regex.Match(currentLine.Split(' ').First(), _DataPattern);

                    // Take the first token of the next string and check if match the data pattern
                    // and also if the current string matches the data pattern
                    if (m.Success && Regex.IsMatch(nextLine.Split(' ').First(), _DataPattern))
                    {
                        if (m.Value == "[MAT]")
                            DataLines.Add(new MatLine(currentLine));
                    }
                    else
                    {
                        do
                        {
                            currentLine += " " + nextLine;
                            i++;
                            nextLine = _sectionLines[i + 1];
                        } while (!(m.Success && Regex.IsMatch(nextLine.Split(' ').First(), _DataPattern)));

                        if (m.Value == "[MAT]")
                            DataLines.Add(new MatLine(currentLine));
                    }

                }
            }

            Match m1 = Regex.Match(currentLine.Split(' ').First(), _DataPattern);

            if (m1.Success)
            {
                if (m1.Value == "[MAT]")
                    DataLines.Add(new MatLine(currentLine));
            }
        }
    }

    public class PCS : ISection
    {
        public string Name { get; set; }
        public List<IDataLine> DataLines { get; set; }
        private string[] _sectionLines;
        private string _DataPattern = @"^\[.+\]$"; // Pattern that used in a Regex check if the string start and end with just 1 [ and 1 ]

        public PCS(string name, string[] sectionLines)
        {
            Name = name;
            _sectionLines = sectionLines.ToArray();
            DataLines = new List<IDataLine>();
            CreateDataLines();
        }

        public PCS(string name, List<string> sectionLines)
        {
            Name = name;
            _sectionLines = sectionLines.ToArray();
            DataLines = new List<IDataLine>();
            CreateDataLines();
        }

        public PCS(string name, string sectionLines)
        {
            Name = name;
            _sectionLines = sectionLines.Split('\n').Where(s => s != "" && s != "\r").Select(x => x.Trim('\r')).ToArray();
            DataLines = new List<IDataLine>();
            CreateDataLines();
        }

        private void CreateDataLines()
        {
            string currentLine = _sectionLines[0];
            if (_sectionLines.Length > 1)
            {
                for (int i = 0; i < _sectionLines.Length - 1; i++)
                {
                    currentLine = _sectionLines[i];
                    string nextLine = _sectionLines[i + 1];

                    // Take the first token of the string and check if matches the data pattern
                    Match m = Regex.Match(currentLine.Split(' ').First(), _DataPattern);

                    if (!m.Success)
                        continue;

                    // Take the first token of the next string and check if match the data pattern
                    // and also if the current string matches the data pattern
                    if (Regex.IsMatch(nextLine.Split(' ').First(), _DataPattern))
                    {
                        if (m.Value == "[HEAD]")
                            DataLines.Add(new HeadLine(currentLine));
                        else if (m.Value == "[LEAD]")
                            DataLines.Add(new LeadLine(currentLine));
                        else if (m.Value == "[CUT]")
                            DataLines.Add(new CutLine(currentLine));
                        else if (m.Value == "[MARK]")
                            DataLines.Add(new MarkLine(currentLine));
                        else if (m.Value == "[COPE]")
                            DataLines.Add(new CopeLine(currentLine));
                        else if (m.Value == "[HOL]")
                            DataLines.Add(new HoleLine(currentLine));

                        currentLine = nextLine;
                    }
                    else
                    {
                        do
                        {
                            currentLine += " " + nextLine;
                            i++;

                            if(i == _sectionLines.Length - 1)
                                break;

                            nextLine = _sectionLines[i + 1];
                        } while (!Regex.IsMatch(nextLine.Split(' ').First(), _DataPattern));

                        if (m.Value == "[HEAD]")
                            DataLines.Add(new HeadLine(currentLine));
                        else if (m.Value == "[LEAD]")
                            DataLines.Add(new LeadLine(currentLine));
                        else if (m.Value == "[CUT]")
                            DataLines.Add(new CutLine(currentLine));
                        else if (m.Value == "[MARK]")
                            DataLines.Add(new MarkLine(currentLine));
                        else if (m.Value == "[COPE]")
                            DataLines.Add(new CopeLine(currentLine));
                        else if (m.Value == "[HOL]")
                            DataLines.Add(new HoleLine(currentLine));

                        currentLine = nextLine;
                    }
                }
            }

            Match m1 = Regex.Match(currentLine.Split(' ').First(), _DataPattern);

            if (m1.Success)
            {
                if (m1.Value == "[HEAD]")
                    DataLines.Add(new HeadLine(currentLine));
                else if (m1.Value == "[LEAD]")
                    DataLines.Add(new LeadLine(currentLine));
                else if (m1.Value == "[CUT]")
                    DataLines.Add(new CutLine(currentLine));
                else if (m1.Value == "[MARK]")
                    DataLines.Add(new MarkLine(currentLine));
                else if (m1.Value == "[COPE]")
                    DataLines.Add(new CopeLine(currentLine));
                else if (m1.Value == "[HOL]")
                    DataLines.Add(new HoleLine(currentLine));
            }
        }
    }
    public class PRF : ISection
    {
        public string Name { get; set; }

        private string[] _sectionLines;
        private string _DataPattern = @"^\[.+\]$"; // Pattern that used in a Regex check if the string start and end with just 1 [ and 1 ]
        public List<IDataLine> DataLines { get; set; }

        public PRF(string name, string[] sectionLines)
        {
            Name = name;
            _sectionLines = sectionLines.ToArray();
            DataLines = new List<IDataLine>();
            CreateDataLines();
        }

        public PRF(string name, List<string> sectionLines)
        {
            Name = name;
            _sectionLines = sectionLines.ToArray();
            DataLines = new List<IDataLine>();
            CreateDataLines();
        }

        public PRF(string name, string sectionLines)
        {
            Name = name;
            _sectionLines = sectionLines.Split('\n').Where(s => s != "" && s != "\r").Select(x => x.Trim('\r')).ToArray();
            DataLines = new List<IDataLine>();
            CreateDataLines();
        }

        private void CreateDataLines()
        {
            string currentLine = _sectionLines[0];
            if (_sectionLines.Length > 1)
            {
                for (int i = 0; i < _sectionLines.Length - 1; i++)
                {
                    currentLine = _sectionLines[i];
                    string nextLine = _sectionLines[i + 1];

                    // Take the first token of the string and check if matches the data pattern
                    Match m = Regex.Match(currentLine.Split(' ').First(), _DataPattern);

                    // Take the first token of the next string and check if match the data pattern
                    // and also if the current string matches the data pattern
                    if (m.Success && Regex.IsMatch(nextLine.Split(' ').First(), _DataPattern))
                    {
                        if (m.Value == "[PRF]")
                            DataLines.Add(new PrfLine(currentLine));
                    }
                    else
                    {
                        do
                        {
                            currentLine += " " + nextLine;
                            i++;
                            nextLine = _sectionLines[i + 1];
                        } while (!(m.Success && Regex.IsMatch(nextLine.Split(' ').First(), _DataPattern)));

                        if (m.Value == "[PRF]")
                            DataLines.Add(new PrfLine(currentLine));
                    }

                }
            }

            Match m1 = Regex.Match(currentLine.Split(' ').First(), _DataPattern);

            if (m1.Success)
            {
                if (m1.Value == "[PRF]")
                    DataLines.Add(new PrfLine(currentLine));

            }
        }
    }

    public class CopeLine : IDataLine
    {
        public List<string> Tokens { get; set; }
        
        public CopeLine(string txtLine)
        {
            txtLine = txtLine.Trim();
            Tokens = txtLine.Split(' ').Where(t => t != string.Empty).Skip(1).ToList();
        }
    }

    public class HeadLine : IDataLine
    {
        public List<string> Tokens { get; set; }

        public HeadLine(string txtLine)
        {
            txtLine = txtLine.Trim();
            Tokens = txtLine.Split(' ').Where(t => t != string.Empty).Skip(1).ToList();
        }
    }

    public class HoleLine : IDataLine
    {
        public List<string> Tokens { get; set; }

        public HoleLine(string txtLine)
        {
            txtLine = txtLine.Trim();
            Tokens = txtLine.Split(' ').Where(t => t != string.Empty).Skip(1).ToList();
        }
    }

    public class MarkLine : IDataLine
    {
        public List<string> Tokens { get; set; }

        public MarkLine(string txtLine)
        {
            txtLine = txtLine.Trim();
            Tokens = txtLine.Split(' ').Where(t => t != string.Empty).Skip(1).ToList();
        }
    }

    public class LeadLine : IDataLine
    {
        public List<string> Tokens { get; set; }

        public LeadLine(string txtLine)
        {
            txtLine = txtLine.Trim();
            Tokens = txtLine.Split(' ').Where(t => t != string.Empty).Skip(1).ToList();
        }
    }

    public class CutLine : IDataLine
    {
        public List<string> Tokens { get; set; }

        public CutLine(string txtLine)
        {
            txtLine = txtLine.Trim();
            Tokens = txtLine.Split(' ').Where(t => t != string.Empty).Skip(1).ToList();
        }
    }

    public class PrfLine : IDataLine
    {
        public List<string> Tokens { get; set; }

        public PrfLine(string txtLine)
        {
            txtLine = txtLine.Trim();
            Tokens = txtLine.Split(' ').Skip(1).Where(t => t != string.Empty).ToList();
        }
    }

    public class MatLine : IDataLine
    {
        public List<string> Tokens { get; set; }

        /// <summary>
        /// Create a MatLine object with the tokens of the string passed in
        /// </summary>
        /// <param name="txtLine">
        /// Line containig the header token [MAT]
        /// </param>
        public MatLine(string txtLine)
        {
            txtLine = txtLine.Trim();
            Tokens = txtLine.Split(' ').Skip(1).Where(t => t != string.Empty).ToList();
        }
    }
    public interface ISection
    {
        string Name { get; set; }

        List<IDataLine> DataLines { get; set; }
    }

    public interface IDataLine
    {
        List<string> Tokens { get; set; }
    }
}
