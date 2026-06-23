using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Ficep.RobServer.MacroParser
{
    public class MacroLibGroup
    {
        public string ProfileType { get; set; }
        public string Group { get; set; }
        public string LocalizationKey { get; set; }
        public List<Macro> Macros { get; set; }
    }

    public class Macro
    {
        private string _vertex_X;

        public string Name { get; set; }
        public string MacroClassName { get; set; }
        public bool Enable { get; set; }
        public string ImageCode { get; set; }
        public string Side { get; set; }
        public string Vertex_X { get {return _vertex_X == "Initial" ? "I" : _vertex_X == "Final" ? "F" : _vertex_X; } set { _vertex_X = value; } }
        public string Vertex_Y { get; set; }
        public List<Attribute> Attributes { get; set; }
        public List<Group> Groups { get; set; }
    }

    public class Attribute
    {
        public string Parameter { get; set; }
        public string LocalizationKey { get; set; }
        public string Format { get; set; }
    }

    public class Group
    {
        public string GroupName { get; set; }
        public Values Values { get; set; }
    }

    public class Values
    {
        public List<string> ToolType { get; set; }
        public List<string> HiddenAttributes { get; set; }
    }

}
