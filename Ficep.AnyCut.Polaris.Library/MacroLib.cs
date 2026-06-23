using System.Text.Json;

namespace Ficep.MacroLibrary
{
    public class MacroLib
    {
        public List<PolarisLibraryGroup> MacroGroups { get; set; }

        public MacroLib() 
        {
            MacroGroups = new List<PolarisLibraryGroup>();
        }

        public bool LoadLibrary (string jsonFileName)
        {
            if (!System.IO.File.Exists(jsonFileName))
                return false;

            string jsonFile = File.ReadAllText(jsonFileName);
            MacroGroups = JsonSerializer.Deserialize<List<PolarisLibraryGroup>>(jsonFile);

            return true;
        }

        //
        //  Rende il nome della classe e del file di bitmap della macro passata
        //
        public bool FindMacro(string macroName, string CodePrf, out PolarisLibraryMacro libmacro, 
            out PolarisLibraryGroup libgroup)
        {
            libmacro = null;
            libgroup = null;

            if (macroName == null)
                return false;

            PolarisLibraryMacro macro = null;

            foreach (var group  in MacroGroups) 
            {
                if (group.ProfileType == CodePrf)
                {
                    macro = group.Macros.Where(m => m.Name == macroName).FirstOrDefault();

                    if (macro !=  null)
                    {
                        libmacro = macro;
                        libgroup = group;

                        break;
                    }
                }
            }

            //macro = MacroGroups.Where(g => g.ProfileType == CodePrf).SelectMany(g => g.Macros).Where(m => m.Name == macroName).FirstOrDefault();

            return libmacro != null;
        }

    }


    public class PolarisLibraryGroup
    {
        public string ProfileType { get; set; }
        public string Group { get; set; }
        public string LocalizationKey { get; set; }
        public List<PolarisLibraryMacro> Macros { get; set; }
    }

    public class PolarisLibraryMacro
    {
        private string _vertex_X;

        public string Name { get; set; }
        public string MacroClassName { get; set; }
        public bool Enable { get; set; }
        public string ImageCode { get; set; }
        public string Side { get; set; }
        public string Vertex_X { get { return _vertex_X == "Initial" ? "I" : _vertex_X == "Final" ? "F" : _vertex_X; } set { _vertex_X = value; } }
        public string Vertex_Y { get; set; }
        public List<PolarisLibraryAttribute> Attributes { get; set; }
        public List<PolarisLibraryToolGroup> Groups { get; set; }
    }

    public class PolarisLibraryAttribute
    {
        public string Parameter { get; set; }
        public string LocalizationKey { get; set; }
        public string Format { get; set; }
    }

    public class PolarisLibraryToolGroup
    {
        public string GroupName { get; set; }
        public PolarisLibraryToolGroupValues Values { get; set; }
    }

    public class PolarisLibraryToolGroupValues
    {
        public List<string> ToolType { get; set; }
        public List<string> HiddenAttributes { get; set; }
    }

}
