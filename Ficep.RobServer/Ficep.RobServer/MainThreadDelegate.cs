using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using devDept.Eyeshot;
using Ficep.MacroGra;
using Ficep.RobServer.Data;
using Ficep.RobServer.ImportExport;
using Ficep.RobServer.Utility3D;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Ficep.AnyCut.ConNet.RobServerTcpConnection;
using System.Windows.Forms;
using Ficep.RobServer.MacroParser;
using FicepDstvParser;

namespace Ficep.RobServer
{
    partial class Form1
    {
        //
        //  Funzione delegate chiamata dal thread del server TCP per gestire
        //  le azioni richieste specificate tramite la stringa notification
        //
        private void MainThreadDelegate(string notification, out string returnMessage)
        {
            returnMessage = string.Empty;
            var tokens = notification.Trim().Split(' ').ToList();

            int cmd = tokens != null ? int.Parse(tokens[0]) : -1;
            bool reqGetStatus = cmd == (int)MessageCode.GetStatus ? true : false;
            bool reqGetVersion = cmd == (int)MessageCode.GetVersion ? true : false;
            bool reqValidateTool = tokens != null && tokens.Count > 1 && cmd == (int)MessageCode.ValidateTool ? true : false;
            bool reqValidateGeometry = tokens != null && tokens.Count > 1 && cmd == (int)MessageCode.ValidateGeometry ? true : false;
            bool reqPathExtraction = tokens != null && tokens.Count > 3 && cmd == (int)MessageCode.PathExtraction ? true : false;

            if (reqGetStatus)
            {
                //  GetStatus
                returnMessage = notification;
            }
            else if (reqGetVersion)
            {
                //  GetVersion
                returnMessage = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            else if (reqValidateTool || reqValidateGeometry)
            {
                //
                //  Validate
                //
                // Ogni lista di stringhe corrisponde ai token di una macro senza il token [COPE]
                List<List<string>> macroTokenList = new List<List<string>>();
                List<string> remainingTokens = new List<string>();
                List<string> macroTokens = null;
                bool isInsideCopeSection = false, isInsideOtherSection = false;

                foreach (var token in tokens.Skip(1))
                {
                    if (token == "[COPE]")
                    {
                        if (isInsideCopeSection && macroTokens != null)
                            macroTokenList.Add(macroTokens);

                        isInsideCopeSection = true;
                        macroTokens = new List<string>();
                        continue;
                    }
                    else if (!(token.Contains("[") && token.Contains("]")))
                    {
                        if (macroTokens != null && isInsideCopeSection)
                            macroTokens.Add(token);
                        else if (isInsideOtherSection)
                            remainingTokens.Add(token);
                    }

                    else if ((token.Contains("[") && token.Contains("]")) || (token.Contains("[[") && token.Contains("]]")))
                    {
                        remainingTokens.Add(token);

                        if (isInsideCopeSection && macroTokens != null)
                            macroTokenList.Add(macroTokens);
                        macroTokens = null;

                        isInsideCopeSection = false;
                        isInsideOtherSection = true;
                        continue;
                    }
                }

                IWorkPiece wp = null;
                Interpreter.SetWorkpieceParameters(remainingTokens, ref wp);

                List<IMacro> macros = new List<IMacro>();

                if (robotIniPath.Contains(".ini", StringComparison.CurrentCultureIgnoreCase))
                {
                    importExport.GetMacros(robotIniPath, null, ref wp, ref macros, eyeParam, macroTokenList);
                }
                else if (robotIniPath.Contains(".json", StringComparison.CurrentCultureIgnoreCase))
                {
                    importExport.GetMacros(null, macroLibGroup, ref wp, ref macros, eyeParam, macroTokenList);
                }

                if (reqValidateTool)
                    ValidateTool(macros, wp, ref returnMessage);
                if (reqValidateGeometry)
                    ValidateGeometry(macros, ref returnMessage);

                if (returnMessage == string.Empty)
                    returnMessage = "Ok";
                else
                    returnMessage = "Not_Ok " + returnMessage;
            }
            else if (reqPathExtraction)
            {
                //
                //  PathExtraction
                //
                string fncFile = tokens[1], xmlFile = tokens[2], extraToken = tokens[3];

                if (extraToken == "/N")
                    processAsPieceCutToMeasure = false;
                else if (extraToken == "/P")
                    processAsPieceCutToMeasure = true;

                //
                //  La stringa FNC segue i primi 4 token
                //
                int position = notification.IndexOf(tokens[4]);
                string fncString = notification.Substring(position, notification.Length - position);

                fncPath = fncFile;
                exportFormat = "xml";
                exportPath = xmlFile;

                design1.Entities.Clear();
                design1.Entities.Clear();

                wpDynamicObj.wp = new EyeWorkPiece();

                // If the fnc is specified in the arguments open it
                if (fncPath != null)
                {
                    try
                    {
                        ImportFNC();
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message);
                    }
                }

                // Clear the previus solids
                wpDynamicObj.exportEntities.Clear();

                xmlInterface.FeatureExtractionParam.EnableWorkpieceVectors = enableWorkpieceVectors &&
                    (wpDynamicObj.wp.Prf.CodePrf == "I" && enableWorkpieceVectorsI ||
                    wpDynamicObj.wp.Prf.CodePrf == "U" && enableWorkpieceVectorsU ||
                    wpDynamicObj.wp.Prf.CodePrf == "L" && enableWorkpieceVectorsL ||
                    wpDynamicObj.wp.Prf.CodePrf == "Q" && enableWorkpieceVectorsQ ||
                    wpDynamicObj.wp.Prf.CodePrf == "R" && enableWorkpieceVectorsR ||
                    wpDynamicObj.wp.Prf.CodePrf == "F" && enableWorkpieceVectorsF);

                if (wpDynamicObj.macros.Count != 0)
                {
                    if (wpDynamicObj.macros[0].Wp is EyeWorkPiece wp)
                    {
                        wpDynamicObj.finalPart = (Brep)wp.Solid.Clone();
                        wpDynamicObj.exportEntities.Add(wpDynamicObj.finalPart);

                        // Create the macro in the final part
                        if (xmlInterface.FeatureExtractionParam.EnableWorkpieceVectors)
                        {
                            if (!CreateFinalPartComputingVectorsAtTheEnd())
                                log.Debug("Failed CreateFinalPartComputingVectorsAtTheEnd()");
                        }
                        else
                            CreateFinalPart(wp);
                    }
                }

                importExport.ExportFicepXML(exportPath, eyeParam.Tol.Brep, wpDynamicObj.wp, wpDynamicObj.macros.Select(m => m as EyeMacro).ToList(), wpDynamicObj.finalPart, processAsPieceCutToMeasure, ref importExport, ref xmlInterface);

                // Read the content of the XML file into a string
                string xmlString = "";
                using (StreamReader reader = new StreamReader(exportPath))
                {
                    xmlString = reader.ReadToEnd();
                }

                returnMessage = "Ok " + xmlString;

                // Add the WorkedPart to the design
                if (fncPath != null)
                {
                    design1.Entities.Add(wpDynamicObj.finalPart.Clone() as Entity, EyeUtils.workedPiece);
                }

                design1.CompileUserInterfaceElements();

                // fits the model in the viewport
                design1.ZoomFit(50);

                design1.Invalidate();

            }
            else
            {
                design1.Entities.Clear();
                design1.Entities.Clear();

                wpDynamicObj.wp = new EyeWorkPiece();

                //
                //  Parso la stringa in ingresso per decodificare il comando richiesto
                //
                int idxtoken = 0;

                foreach (var token in tokens)
                {
                    if (idxtoken == 0)
                        robotIniPath = token;
                    else if (idxtoken == 1)
                        fncPath = token;
                    else if (idxtoken == 2)
                    {
                        if (token.StartsWith("/"))
                            exportFormat = token.TrimStart('/');
                    }
                    else if (idxtoken == 3)
                        exportPath = token;
                    else if (idxtoken == 4)
                    {
                        processAsPieceCutToMeasure = token == "/N" ? false : true;
                    }

                    idxtoken++;
                    token.Trim();
                }

                // If the fnc is specified in the arguments open it
                if (fncPath != null)
                {
                    try
                    {
                        ImportFNC();
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex.Message);
                    }
                }

                // Clear the previus solids
                wpDynamicObj.exportEntities.Clear();

                xmlInterface.FeatureExtractionParam.EnableWorkpieceVectors = enableWorkpieceVectors &&
                    (wpDynamicObj.wp.Prf.CodePrf == "I" && enableWorkpieceVectorsI ||
                    wpDynamicObj.wp.Prf.CodePrf == "U" && enableWorkpieceVectorsU ||
                    wpDynamicObj.wp.Prf.CodePrf == "L" && enableWorkpieceVectorsL ||
                    wpDynamicObj.wp.Prf.CodePrf == "Q" && enableWorkpieceVectorsQ ||
                    wpDynamicObj.wp.Prf.CodePrf == "R" && enableWorkpieceVectorsR ||
                    wpDynamicObj.wp.Prf.CodePrf == "F" && enableWorkpieceVectorsF);

                if (wpDynamicObj.macros.Count != 0)
                {
                    if (wpDynamicObj.macros[0].Wp is EyeWorkPiece wp)
                    {
                        wpDynamicObj.finalPart = (Brep)wp.Solid.Clone();
                        wpDynamicObj.exportEntities.Add(wpDynamicObj.finalPart);

                        // Create the macro in the final part
                        if (xmlInterface.FeatureExtractionParam.EnableWorkpieceVectors)
                        {
                            if (!CreateFinalPartComputingVectorsAtTheEnd())
                                log.Debug("Failed CreateFinalPartComputingVectorsAtTheEnd()");
                        }
                        else
                            CreateFinalPart(wp);
                    }
                }

                if (exportFormat == "eye")
                {
                    importExport.ExportEntities(wpDynamicObj.exportEntities, exportPath, singleFile);
                }
                else if (exportFormat == "stl")
                {
                    wpDynamicObj.exportEntities.Clear();
                    wpDynamicObj.finalPart.Regen(eyeParam.Tol.Brep);
                    wpDynamicObj.exportEntities.Add(wpDynamicObj.finalPart);
                    WriteFileParams writeFileParams = new WriteFileParams(wpDynamicObj.exportEntities, new LayerKeyedCollection(new Layer("Default")));
                    WriteSTL writeSTL = new WriteSTL(writeFileParams, exportPath);
                    writeSTL.DoWork();
                }
                else if (exportFormat == "xml")
                {
                    importExport.ExportFicepXML(exportPath, eyeParam.Tol.Brep, wpDynamicObj.wp, wpDynamicObj.macros.Select(m => m as EyeMacro).ToList(), wpDynamicObj.finalPart, processAsPieceCutToMeasure, ref importExport, ref xmlInterface);
                }

                // Add the WorkedPart to the design
                if (fncPath != null)
                {
                    design1.Entities.Add(wpDynamicObj.finalPart.Clone() as Entity, EyeUtils.workedPiece);
                }

                design1.CompileUserInterfaceElements();

                // fits the model in the viewport
                design1.ZoomFit(50);

                design1.Invalidate();
            }

            //  Segnalo al server il completamento del lavoro
            tcpServer.WorkCompleted();
        }

        private void ValidateTool(List<IMacro> macroList, IWorkPiece wp, ref string retMessage)
        {
            foreach (IMacro macro in macroList)
            {
                if (macro is EyeMacro)
                {
                    EyeMacro eyeMacro = macro as EyeMacro;
                    List<string> tools;

                    if (isJsonIniFilePath)
                        Importer.GetMacroToolsFromJson(eyeMacro.MacroName, wp, macroLibGroup, out tools);
                    else
                        Importer.GetMacroToolsFromIni(eyeMacro.MacroName, wp, robotIni, out tools);

                    Ficep.MacroLibrary.Constants.ErrMacro validTool = eyeMacro.ValidateTool(tools),
                                                     errMacro = eyeMacro.Validate();

                    bool validMacro = validTool == Ficep.MacroLibrary.Constants.ErrMacro.No_err && errMacro == Ficep.MacroLibrary.Constants.ErrMacro.No_err;

                    if (!validMacro)
                    {
                        retMessage += "MAC:" + eyeMacro.MacroName + " LINE:" + eyeMacro.LineNumber;

                        if (errMacro != Ficep.MacroLibrary.Constants.ErrMacro.No_err)
                            retMessage += " " + errMacro.ToString();
                        if (validTool == Ficep.MacroLibrary.Constants.ErrMacro.err_Tool)
                            retMessage += " " + validTool.ToString();

                        retMessage += " ; ";
                    }
                }
                else if (macro is EyeMacroLung)
                {
                    EyeMacroLung eyeMacroLung = macro as EyeMacroLung;
                    List<string> tools;

                    if (isJsonIniFilePath)
                        Importer.GetMacroToolsFromJson(eyeMacroLung.MacroName, wp, macroLibGroup, out tools);
                    else
                        Importer.GetMacroToolsFromIni(eyeMacroLung.MacroName, wp, robotIni, out tools);

                    Ficep.MacroLibrary.Constants.ErrMacro validTool = eyeMacroLung.ValidateTool(tools),
                                                     errMacro = eyeMacroLung.Validate();

                    bool validMacro = validTool == Ficep.MacroLibrary.Constants.ErrMacro.No_err && errMacro == Ficep.MacroLibrary.Constants.ErrMacro.No_err;

                    if (!validMacro)
                    {
                        retMessage += "MAC:" + eyeMacroLung.MacroName + " LINE:" + eyeMacroLung.LineNumber;

                        if (errMacro != Ficep.MacroLibrary.Constants.ErrMacro.No_err)
                            retMessage += " " + errMacro.ToString();
                        if (validTool == Ficep.MacroLibrary.Constants.ErrMacro.err_Tool)
                            retMessage += " " + validTool.ToString();

                        retMessage += " ; ";
                    }
                }
            }
        }

        private void ValidateGeometry(List<IMacro> macroList, ref string retMessage)
        {
            foreach (IMacro macro in macroList)
            {
                if (!macro.ValidateGeometry())
                    retMessage += "MAC:" + macro.MacroName + " LINE:" + macro.LineNumber + " ";
            }

        }
    }
}
