using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using Ficep.RobServer.MacroParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Ficep.MacroGra;
using System.Reflection;
using devDept.Geometry;
using System.Diagnostics.Eventing.Reader;
using Ficep.RobServer.XmlNet;
using PathType = FicepXml.PathType;
using Ficep.RobServer.Data;
using Ficep.RobServer.Utility3D;
using Ficep.RobServer.ImportExport;
using System.Diagnostics;
using FicepXml;
using static Microsoft.Isam.Esent.Interop.EnumeratedColumn;
using Xbim.IO.Parser;
using FicepDstvParser;
using devDept.Eyeshot.Control;

namespace Ficep.RobServer.ImportExport
{
    public class Importer
    {
        private Assembly assembly = Assembly.LoadFrom("Ficep.MacroGra.dll");
        private int lastSelectedFilter;
        public Importer()
        {
        }

        public bool ImportDstv(string path, double brepTol, double comparanceTolerance, out Brep finalPart, out List<Brep> scraps, out IWorkPiece wp)
        {
            finalPart = null;
            scraps = null;
            wp = null;

            lastSelectedFilter = 1;
            DSTVParser dstv = new DSTVParser(path);
            
            if (!dstv.ReadDstv())
                return false;

            wp = dstv.Wp;

            DstvBrepConverter converter = new DstvBrepConverter(dstv, brepTol, comparanceTolerance);
            
            if (!converter.ConvertDstv(out finalPart, out scraps))
                return false;

            return finalPart != null;
        }

        public bool ImportEyeshot(ref Brep brep)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            string folder;
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                folder = fbd.SelectedPath;  //selected folder path
            }

            else
                return false;

            List<string> filesName = Directory.GetFiles(folder, "*.eye").ToList();
            if (filesName.Count == 0)
                brep = null;
            else if (filesName.Count == 1 && filesName[0].Split('\\').Last() != "rawpiece.eye")
            {
                ReadFile rf = new ReadFile(filesName[0]);
                rf.DoWork();
                Brep finalPart = null;

                if (rf.Entities.Length == 0)
                    brep = null;
                else if (rf.Entities.Length == 1)
                    brep = rf.Entities.FirstOrDefault(x => x is Brep) as Brep;
                else
                {
                    finalPart = rf.Entities.FirstOrDefault(x => x is Brep) as Brep;
                    foreach (Brep b in rf.Entities.Skip(1))
                    {
                        finalPart = Brep.Difference(finalPart, b).FirstOrDefault();
                    }
                }
                brep = finalPart;
            }
            else
            {
                ReadFile rf = new ReadFile(folder + @"\rawpiece.eye");
                rf.DoWork();
                Brep finalPart = null;
                if (filesName.Count == 0)
                    brep = null;
                else if (filesName.Count == 1)
                    brep = rf.Entities.FirstOrDefault(x => x is Brep) as Brep;
                else
                {
                    finalPart = rf.Entities.FirstOrDefault(x => x is Brep) as Brep;
                    filesName.RemoveAll(x => x.EndsWith("rawpiece.eye"));
                    foreach (var fileName in filesName)
                    {
                        rf = new ReadFile(fileName);
                        rf.DoWork();
                        Brep feature = rf.Entities.FirstOrDefault() as Brep;
                        finalPart = Brep.Difference(finalPart, feature).FirstOrDefault();
                    }
                }
                brep = finalPart;
            }
            return true;
        }

        public bool ImportStlStep(in double tolBrep, ref Mesh mesh, ref Brep brep, ref EyeWorkPiece wp, ref string stlStepDstvPath, out List<Brep> scraps)
        {   
            scraps = null;
            wp = null;

            if (stlStepDstvPath == null || stlStepDstvPath == "")
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog
                {
                    InitialDirectory = @"C:\",
                    Title = "Browse Import Files",

                    CheckFileExists = true,
                    CheckPathExists = true,

                    DefaultExt = "",
                    Filter = "DSTV files (*.nc)|*.nc; *.NC; *.nc1;*NC1|STL files (*.stl)|*.stl|STEP Files (*.stp;*.step)|*.stp;*.step",
                    FilterIndex = lastSelectedFilter,
                    RestoreDirectory = true,

                    ReadOnlyChecked = true,
                    ShowReadOnly = true
                };
                if (openFileDialog1.ShowDialog() != DialogResult.OK)
                    return false;

                lastSelectedFilter = openFileDialog1.FilterIndex;
                stlStepDstvPath = openFileDialog1.FileName;
            }

            string extension = Path.GetExtension(stlStepDstvPath.ToUpper());

            if (extension == ".STL")
            {
                ReadSTL readSTL = new ReadSTL(stlStepDstvPath);
                readSTL.DoWork();
                mesh = (Mesh)readSTL.Entities.FirstOrDefault();
            }
            else if (extension == ".STP" || extension == ".STEP")
            {
                ReadSTEP readSTEP = new ReadSTEP(stlStepDstvPath);
                readSTEP.DoWork();

                // If the step reader has not failed it will contain the brep. We need to check if the brep is contained in Entities or in Blocks
                if (readSTEP.Result)
                {
                    if (readSTEP.Entities.Length == 1)
                    {
                        brep = (Brep)readSTEP.Entities.FirstOrDefault();
                    }
                    else if (readSTEP.Entities.Length == 0)
                    {
                        var blockRef = readSTEP.Blocks.SelectMany(b => b.Entities.Select(e => e as BlockReference)).FirstOrDefault(x => x != null);
                        brep = readSTEP.Blocks.SelectMany(b => b.Entities.Select(e => e as Brep)).FirstOrDefault(x => x != null);
                        if (blockRef != null)
                            brep.TransformBy(blockRef.Transformation);
                    }
                    else
                        return false;
                }

                // TODO In case of round tube the brep regeneration with this tolerance take too time 
                // The order of operation has to be FixTopology and then Regen
                //TODO devDept 2025: Removed obsolete FixTopology(out Brep theFixedSolid) method overload.
                brep.FixTopology();
                brep.Regen(tolBrep);
                StepImporter.TranslateSolid(ref brep);
                // TODO Project the edges in one plane and check which is the profile, probably this reasonament has to be done just for the u and
                // l profiles to detect them even if have rounded corners, or if we want to detect profiles with the chamfer along all the lenght of
                // the part. Filter the too short edges  and then extrecrt the vertices for just the relevant edges 
                
                StepImporter.GetProfileInformations(ref brep, out wp);
            }
            else if (extension == ".NC" || extension == ".NC1")
            {
                double comparanceTolerance = 0.2; 
                if (!ImportDstv(stlStepDstvPath, tolBrep, comparanceTolerance, out brep, out scraps, out IWorkPiece iwp))
                    return false;

                wp = new EyeWorkPiece(iwp.Prf.CodePrf, iwp.Prf.SA, iwp.Prf.TA, iwp.Prf.SB, iwp.Prf.TB, iwp.Prf.Radius, iwp.Lp);
                wp.CreateSolidRawPart();
            }
            else 
                return false;


            return true;
        }

        public bool ExportEyeshot(List<Entity> entities)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "Eyeshot files (*.eye)|*.eye";
            saveFileDialog.AddExtension = true;
            saveFileDialog.CheckPathExists = true;

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return false;

            ExportEntities(entities, saveFileDialog.FileName);
            return true;
        }

        public bool ExportSTL(Brep finalPart, double tolBrep, bool openSaveFileDiaolog = true, string fileName = "")
        {
            if (openSaveFileDiaolog)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();

                saveFileDialog.Filter = "STL files (*.stl)|*.stl";
                saveFileDialog.AddExtension = true;
                saveFileDialog.CheckPathExists = true;

                if (saveFileDialog.ShowDialog() != DialogResult.OK)
                    return false;

                fileName = saveFileDialog.FileName;
            }

            if (fileName == "" || finalPart == null)
                return false;

            finalPart.Regen(tolBrep);
            List<Entity> exportEntities = new List<Entity>();
            exportEntities.Add(finalPart);
            //exportEntities.Add(finalPart.Clone() as Entity);
            WriteFileParams writeFileParams = new WriteFileParams(exportEntities, new LayerKeyedCollection(new Layer("Default")));
            WriteSTL writeSTL = new WriteSTL(writeFileParams, fileName);
            writeSTL.DoWork();

            return true;
        }

        public bool ExportSTEP(Brep finalPart, IDesign design, string fileName = "")
        {
            if (fileName == "" || finalPart == null)
                return false;

            ///////////////////////////////////////////////////////////////////
            bool TestMainAxisZ = true;
            if (TestMainAxisZ)
            {
                //finalPart.Rotate(-Math.PI / 2, Vector3D.AxisY);
                //finalPart.Rotate(-Math.PI / 2, Vector3D.AxisZ);

                Block block = new Block("aa");
                block.Entities.Add(finalPart.Clone() as Brep);
                DesignDocument designDocument = new DesignDocument();
                designDocument.Blocks.Add(block);
                designDocument.Entities.Add(new BlockReference("aa"));
                WriteSTEP w1 = new WriteSTEP(designDocument, fileName);
                w1.DoWork();
            }
            ///////////////////////////////////////////////////////////////////
            else
            {
                List<Entity> list = new List<Entity>();
                list.Add(finalPart.Clone() as Entity);
                WriteParamsWithUnits par = new WriteParamsWithUnits(list, design.Layers, design.Blocks, linearUnitsType.Millimeters);
                WriteSTEP w = new WriteSTEP(par, fileName);
                w.DoWork();
            }

            return true;
        }
        // Given a list of entites export them in eyeshot format in a single file if the boolean is set to true, 
        // in a file for each entities if is set to false
        public void ExportEntities(List<Entity> entities, string exportPath, bool singleFile = true)
        {
            if (entities == null || entities.Count == 0)
                return;

            if (singleFile)
            {
                WriteFileParams writeFileParams = new WriteFileParams(entities);
                WriteFile writeFile = new WriteFile(writeFileParams, exportPath);
                writeFile.DoWork();
            }
            else
            {
                List<Entity> temp = new List<Entity>();
                temp.Add(entities[0]);
                WriteFileParams writeFileParams = new WriteFileParams(temp);
                WriteFile writeFile = new WriteFile(writeFileParams, exportPath + "/rawpiece.eye");
                writeFile.DoWork();
                for (int i = 1; i < entities.Count; i++)
                {
                    temp.Clear();
                    temp.Add(entities[i]);
                    writeFileParams = new WriteFileParams(temp);
                    writeFile = new WriteFile(writeFileParams, exportPath + "/entity" + i + ".eye");
                    writeFile.DoWork();
                }
            }
        }

        public Type GetClassType (string macroClassName)
        {
            return assembly.GetType("Ficep.MacroGra." + macroClassName);
        }

        public bool ImportFNC(IniReader robotIni, List<MacroLibGroup> macroLibGroupList, ref IWorkPiece wp, ref List<IMacro> macros, EyeParam eyeParam, string fncPath = null)
        {
            // If the fnc path is not specified ask to select a fnc path
            if (fncPath == null || fncPath == "")
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog
                {
                    InitialDirectory = @"C:\",
                    Title = "Browse FNC Files",

                    CheckFileExists = true,
                    CheckPathExists = true,

                    DefaultExt = "fnc",
                    Filter = "FNC files (*.fnc)|*.fnc",
                    FilterIndex = 2,
                    RestoreDirectory = true,

                    ReadOnlyChecked = true,
                    ShowReadOnly = true
                };

                if (openFileDialog1.ShowDialog() != DialogResult.OK)
                    return false;

                fncPath = openFileDialog1.FileName;
            }

            List<(ICopeParams copeParams, string Macroname, string macroClassName, string macroBitmapName)> copeList;
            IAngTaglio angTaglio;

            try
            { 
                if (!Interpreter.InterpretFNC(fncPath, out copeList, out angTaglio, ref wp, eyeParam.Tol.Brep))
                {
                    MessageBox.Show("FNC file at: " + fncPath + " NOT OK", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            if (robotIni != null)
            {
                for (int i = 0; i < copeList.Count; i++)
                {
                    string macroName = copeList[i].Macroname;

                    var cope = copeList[i];
                    GetMacroClassNameFromIni(macroName, wp, robotIni, ref cope.copeParams, ref cope.macroClassName, ref cope.macroBitmapName);

                    copeList[i] = cope;
                }
            }
            else if (macroLibGroupList != null && macroLibGroupList.Count > 0)
            {
                for (int i = 0; i < copeList.Count; i++)
                {
                    string macroName = copeList[i].Macroname;

                    var cope = copeList[i];
                    GetMacroClassNameFromJson(macroName, wp, macroLibGroupList, ref cope.copeParams, ref cope.macroClassName, ref cope.macroBitmapName);

                    copeList[i] = cope;
                }
            }
            else
                return false;

            macros = new List<IMacro>();
            // Put the taglio macro at the beginning of the list, if exist
            Type classType = assembly.GetType("Ficep.MacroGra.TAGLIO");
            // create an instance of that object with the default TolThickness and surplus
            EyeMacroTaglio taglioMacro = Activator.CreateInstance(classType, wp, angTaglio, "TAGLIO", "TAGLIO", eyeParam, (uint)0) as EyeMacroTaglio;
            macros.Add(taglioMacro);

            // Create the macro in order to compute the correct workpiece
            taglioMacro.CreateMacro();

            // Create the list of macros skipping the if the taglio macro exist
            foreach ((ICopeParams copeParams, string macroName, string macroClassName, string macroBitmapName) macroParam in copeList)
            {
                // Get the class type of macro
                if (macroParam.macroClassName != null)
                {
                    classType = assembly.GetType("Ficep.MacroGra." + macroParam.macroClassName);
                    // create an instance of that object with the default TolThickness and surplus
                    IMacro macro = Activator.CreateInstance(classType, wp, macroParam.copeParams, macroParam.macroClassName, macroParam.macroName, eyeParam, (uint)0) as IMacro;
                    macro.MacroBitmapName = macroParam.macroBitmapName;
                    macros.Add(macro);
                }
            }

            return true;
        }

        // Ritorna una lista di macro, se è passata vuota la crea nuova e aggiunge le macro altrimenti le aggiunge alla lista passata
        public bool GetMacros(string roboIniPath, List<MacroLibGroup> rootList, ref IWorkPiece wp, ref List<IMacro> macros, EyeParam eyeParam, List<List<string>> macroTokenList)
        {
            if (wp == null || (wp.Prf.CodePrf != "I" && wp.Prf.CodePrf != "L" && wp.Prf.CodePrf != "Q" &&
                              wp.Prf.CodePrf != "F" && wp.Prf.CodePrf != "U" && wp.Prf.CodePrf != "R"))
                return false;

            List<(ICopeParams copeParams, string Macroname, string macroClassName, string macroBitmapName, uint lineNumber)> copeList = new List<(ICopeParams, string, string, string, uint lineNumber)>();

            foreach (var macroToken in macroTokenList)
            {
                (ICopeParams copeParams, string macroName, string bitmapName, uint lineNumber) tuple;
                Interpreter.SetMacroParam(macroToken, out tuple);
                copeList.Add((tuple.copeParams, tuple.macroName, null, tuple.bitmapName, tuple.lineNumber));
            }

            if (roboIniPath != null && roboIniPath != string.Empty)
            {
                IniReader robotIni = new IniReader(roboIniPath);

                for (int i = 0; i < copeList.Count; i++)
                {
                    string macroName = copeList[i].Macroname;

                    var cope = copeList[i];
                    GetMacroClassNameFromIni(macroName, wp, robotIni, ref cope.copeParams, ref cope.macroClassName, ref cope.macroBitmapName);

                    copeList[i] = cope;
                }
            }
            else if (rootList != null && rootList.Count > 0)
            {
                for (int i = 0; i < copeList.Count; i++)
                {
                    string macroName = copeList[i].Macroname;

                    var cope = copeList[i];
                    GetMacroClassNameFromJson(macroName, wp, rootList, ref cope.copeParams, ref cope.macroClassName, ref cope.macroBitmapName);

                    copeList[i] = cope;
                }
            }
            else 
                return false;

            if (macros == null)
                macros = new List<IMacro>();

            // Create the list of macros skipping the if the taglio macro exist
            foreach ((ICopeParams copeParams, string macroName, string macroClassName, string macroBitmapName, uint lineNumber) macroParam in copeList)
            {
                // Get the class type of macro
                if (macroParam.macroClassName != null)
                {
                    var classType = assembly.GetType("Ficep.MacroGra." + macroParam.macroClassName);
                    // create an instance of that object with the default TolThickness and surplus
                    IMacro macro = Activator.CreateInstance(classType, wp, macroParam.copeParams, macroParam.macroClassName, macroParam.macroName, eyeParam, macroParam.lineNumber) as IMacro;
                    macro.MacroBitmapName = macroParam.macroBitmapName;
                    macros.Add(macro);
                }
            }

            return true;
        }
        public bool ExportFicepXML(string pathFileNameXML, double tolBrep, IWorkPiece Wp, List<EyeMacro> macros, Brep finalPart, bool processAsPieceCutToMeasure,
            ref Importer importExport, ref XmlInterface xmlInterface)
        {
            //
            //  ESPORTAZIONE delle superfici in file STL
            //
            //  pathfileNameSTL è il path del file stl (che avrà lo stesso path e nome del file XML ma estensione differente)
            string pathfileNameSTL = pathFileNameXML.ToUpper().Replace(".XML", ".stl");
            //  filenameSTL è il nome del file STL (senza path) che verrà salvato come riferimento al''interno del file XML
            string fileNameSTL = Path.GetFileName(pathfileNameSTL);

            bool successExport = importExport.ExportSTL(finalPart.Clone() as Brep, tolBrep, false, pathfileNameSTL);
            if (!successExport) ;   //  TODO Da gestire ExportSTL fallita

            return xmlInterface.CreateFicepXML(pathFileNameXML, fileNameSTL, Wp, macros, finalPart, processAsPieceCutToMeasure);
        }

        //
        //  Rende il nome della classe e del file di bitmap della macro passata
        //
        public static bool GetMacroClassNameFromIni(string macroName, IWorkPiece wp, IniReader robotIni, ref ICopeParams copeParams, ref string macroClassName, ref string macroBitmapName)
        {
            if (wp == null)
                return false;

            string className = "", bitmapName = "";

            string iniString = robotIni.Read("MAC:" + macroName, "MACRO_" + wp.Prf.CodePrf);

            if (iniString == null || iniString.Equals(string.Empty))
                return false;

            (ICopeParams copeParams, string Macroname, string macroClassName, string macroBitmapName) cope = (copeParams, macroName, className, bitmapName);
            Interpreter.SetMacroParameters(iniString, ref cope);

            macroClassName = cope.macroClassName;
            macroBitmapName = cope.macroBitmapName;

            return true;
        }

        //
        //  Rende il nome della classe e del file di bitmap della macro passata
        //
        public static bool GetMacroClassNameFromJson(string macroName, IWorkPiece wp, List<MacroLibGroup> rootList, ref ICopeParams copeParams, ref string macroClassName, ref string macroBitmapName)
        {
            if (wp == null)
                return false;

            string className = "", bitmapName = "";

            Macro macro = rootList.Where(g => g.ProfileType == wp.Prf.CodePrf).SelectMany(g => g.Macros).Where(m => m.Name == macroName).FirstOrDefault();

            if (macro == null)
                return false;

            (ICopeParams copeParams, string Macroname, string macroClassName, string macroBitmapName) cope = (copeParams, macroName, className, bitmapName);
            cope.macroClassName = macro.MacroClassName;
            cope.macroBitmapName = macro.ImageCode + ".bmp";

            cope.copeParams.VX = macro.Vertex_X != "Undefined"? macro.Vertex_X : cope.copeParams.VX;
            cope.copeParams.VY = macro.Vertex_Y != "Undefined" ? macro.Vertex_Y : cope.copeParams.VY;
            cope.copeParams.SIDE = macro.Side != "X" ? macro.Side : cope.copeParams.SIDE;


            macroClassName = cope.macroClassName;
            macroBitmapName = cope.macroBitmapName;
            

            return true;
        }

        public static bool GetMacroToolsFromIni(string macroName, IWorkPiece wp, IniReader robotIni, out List<string> tools)
        {
            tools = null;

            if (wp == null)
                return false;

            string iniString = robotIni.Read("MAC:" + macroName, "MACRO_" + wp.Prf.CodePrf);

            if (iniString == null || iniString.Equals(string.Empty))
                return false;

            tools = new List<string>();

            string value = iniString.Split(' ').Where(x => x.StartsWith("T")).Select(x => x.Split(':')[1]).FirstOrDefault();

            if (value != null)
            {
                char[] chars = value.ToUpper().ToCharArray();
                foreach (var c in chars)
                {
                    if (c.Equals('O'))
                        tools.Add("TS52");
                    else if (c.Equals('P'))
                        tools.Add("TS51");
                    else
                        return false;
                }
            }
            else 
                return false;

            return true;
        }
        public static bool GetMacroToolsFromJson(string macroName, IWorkPiece wp, List<MacroLibGroup> rootList, out List<string> tools)
        {
            tools = new List<string>();

            Macro macro = rootList.Where(g => g.ProfileType == wp.Prf.CodePrf).SelectMany(g => g.Macros).Where(m => m.Name == macroName).FirstOrDefault();

            Group technologyGroup = macro?.Groups?.Where(g => g.GroupName == "Technology")?.FirstOrDefault();

            if (technologyGroup != null)
                tools.AddRange(technologyGroup.Values.ToolType);
            else 
                return false;

            return true;
        }
    }
}
