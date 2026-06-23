using Ficep.RobServer.Data;
using Ficep.RobServer.Utility3D;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Ficep.RobServer.MacroParser
{
    public static class Interpreter
    {
        /// <summary>
        /// Given a cope line, set the macro parameters
        /// </summary>
        /// <param name="txtLine">
        /// Line in the fnc file describing the macro
        /// </param>
        /// <returns>
        /// true if the operation has been successful, false otherwise
        /// </returns>
        /// <exception cref="Exception"></exception>
        public static bool SetMacroParam(in List<string> tokens, out (ICopeParams copeParams, string macroName, string bitmapName, uint lineNumber) cope)
        {
            bool successful = true;
            cope = (null, null, null, 0);

            if (tokens != null && tokens.Count == 0)
                return false;

            var macToken = tokens.Find(x => x.StartsWith("MAC"));
            
            if (macToken is null)
                return false;

            string macName = macToken.Substring(macToken.IndexOf(":") + 1);

            if (macName is null)
                return false;

            var lineToken = tokens.Find(x => x.StartsWith("LINE"));

            if (!(lineToken is null))
            {
                string line = lineToken.Substring(lineToken.IndexOf(":") + 1);

                if (line is null || !uint.TryParse(line, out uint lineNumber))
                    return false;

                cope.lineNumber = lineNumber;
            }
            else
                cope.lineNumber = 0;

            lineToken = tokens.Find(x => x.StartsWith("TS"));
            ICopeParams copeParams = new CopeParam();

            if (!(lineToken is null))
            {
                int toolCode;
                if(!int.TryParse(new string(lineToken.SkipWhile(char.IsAsciiLetter).ToArray()), out toolCode))
                    return false;

                copeParams.CuttingTool = (CuttingTool)toolCode;
            }
            else
                copeParams.CuttingTool = CuttingTool.Default;

            cope.copeParams = copeParams;
            cope.macroName =  macName;

            foreach (var token in tokens.Where(x => !x.StartsWith("[COPE]") && !x.StartsWith("SKIP1") && !x.Contains(':')))
            {
                string name;
                double value;

                // Extract consecutive letters from token
                name = new string(token.TakeWhile(char.IsLetter).ToArray()).ToUpper();

                // Extract consecutive numbers from token
                string numbers = new string(token.SkipWhile(char.IsLetter).ToArray());

                // TODO vedere se si riesce a mettere nel log
                if(!double.TryParse(numbers, out value))
                    return false;

                PropertyInfo propertyInfo = cope.copeParams.GetType().GetProperty(name);
                
                if (propertyInfo is null)
                {
                    successful = false;
                    continue;
                }
                
                propertyInfo.SetValue(cope.copeParams, value);
            }

            return successful;
        }

        /// <summary>
        /// Given a cope line, set the macro parameters
        /// </summary>
        /// <param name="txtLine">
        /// Line in the fnc file describing the macro
        /// </param>
        /// <returns>
        /// true if the operation has been successful, false otherwise
        /// </returns>
        /// <exception cref="Exception"></exception>
        private static bool SetMacroParam(in List<string> tokens, out IAngTaglio macro)
        {
            bool successful = true;
            macro = null;

            if (tokens != null && tokens.Count == 0)
                return false;
            
                macro = new ParamTaglio();

                foreach (var token in tokens)
                {
                    string name;
                    double value;

                    // Extract consecutive letters from token
                    name = new string(token.TakeWhile(char.IsLetter).ToArray()).ToUpper();

                    // Extract consecutive numbers from token
                    string numbers = new string(token.SkipWhile(char.IsLetter).ToArray());
                    // TODO vedere se si riesce a mettere nel log
                    if (!double.TryParse(numbers, out value))
                        return false;

                    PropertyInfo propertyInfo = macro.GetType().GetProperty(name);

                    if (propertyInfo is null)
                    {
                        successful = false;
                        continue;
                    }

                    propertyInfo.SetValue(macro, value);
                }

            return successful;
        }

        // Receive a string and if contains profile information set the profile parameters 
        private static bool SetWorkpieceParameters(in string txtLine, ref IWorkPiece wp)
        {
            var tokens = txtLine.Trim().Split(' ').ToList();
            tokens.Remove("[PRF]");

            if (wp == null)
                wp = new EyeWorkPiece();

            foreach (var token in tokens)
            {
                token.Trim();
                string name = "";
                double value = 0;

                if (token.Contains("[HEAD]"))
                    continue;
                else if (token.Contains("CP:"))
                    wp.Prf.CodePrf = token.Split(':')[1];
                else if (!token.Contains(":"))
                {
                    name = new string(token.TakeWhile(char.IsLetter).ToArray());
                    name = name.ToUpper();
                    string numbers = new string(token.SkipWhile(char.IsLetter).ToArray());
                    value = double.Parse(numbers);
                }
                else
                    name = token;

                switch (name.ToUpper())
                {
                    case "LP":
                        {
                            wp.Lp = value;
                            break;
                        }
                    case "SA":
                        {
                            wp.Prf.SA = value;
                            break;
                        }
                    case "TA":
                        {
                            wp.Prf.TA = value;
                            break;
                        }
                    case "SB":
                        {
                            wp.Prf.SB = value;
                            break;
                        }
                    case "TB":
                        {
                            wp.Prf.TB = value;
                            break;
                        }
                    case "R":
                        {
                            if (value > 0)
                                wp.Prf.Radius = value;
                            break;
                        }
                }
            }

            if (wp.Prf.Radius <= 0)
            {
                if (wp.Prf.TA > 0)
                    wp.Prf.Radius = wp.Prf.TA;
                else if (wp.Prf.TB > 0)
                    wp.Prf.Radius = wp.Prf.TB;
                else
                    wp.Prf.Radius = 1;
            }

            return true;
        }

        // Receive a list of tokens and if contains profile information set the profile parameters 
        public static bool SetWorkpieceParameters(in List<string> tokens, ref IWorkPiece wp)
        {
            if (wp == null)
                wp = new EyeWorkPiece();

            foreach (var token in tokens)
            {
                string name = "";
                double value = 0;

                if (token.Contains("[") && token.Contains("]") || token.Contains("[[") && token.Contains("]]"))
                    continue;
                if (token.Contains("CP:"))
                    wp.Prf.CodePrf = token.Split(':')[1];
                else if (!token.Contains(":"))
                {
                    name = new string(token.TakeWhile(char.IsLetter).ToArray());
                    name = name.ToUpper();
                    string numbers = new string(token.SkipWhile(char.IsLetter).ToArray());
                    value = double.Parse(numbers);
                }
                else
                {
                    var nameAndValue = token.Split(':');
                    name = nameAndValue[0];
                    double.TryParse(nameAndValue[1], out value);
                }

                switch (name.ToUpper())
                {
                    case "LP":
                        {
                            wp.Lp = value;
                            break;
                        }
                    case "SA":
                        {
                            wp.Prf.SA = value;
                            break;
                        }
                    case "TA":
                        {
                            wp.Prf.TA = value;
                            break;
                        }
                    case "SB":
                        {
                            wp.Prf.SB = value;
                            break;
                        }
                    case "TB":
                        {
                            wp.Prf.TB = value;
                            break;
                        }
                    case "R":
                        {
                            if (value > 0)
                                wp.Prf.Radius = value;
                            break;
                        }
                }
            }

            if (wp.Prf.Radius <= 0)
            {
                if (wp.Prf.TA > 0)
                    wp.Prf.Radius = wp.Prf.TA;
                else if (wp.Prf.TB > 0)
                    wp.Prf.Radius = wp.Prf.TB;
                else
                    wp.Prf.Radius = 1;
            }

            return true;
        }

        /// <summary>
        /// Set the VX, VY, SIDE, macroClassName, macroBitmapName parameters of the macro
        /// </summary>
        /// <param name="txtLine"></param>
        /// <param name="macro"></param>
        /// <returns></returns>
        public static bool SetMacroParameters(in string txtLine, ref (ICopeParams copeParams, string macroName, string macroClassName, string macroBitmapName) macro)
        {
            List<string> tokens = txtLine.ToUpper().Split(' ').Where(x => x.StartsWith("VX") || x.StartsWith("SIDE") || x.StartsWith("VY") || x.StartsWith("GRA:") || x.StartsWith("BMP:")).ToList();
            string value = tokens.Where(x => x.StartsWith("VX")).Select(x => x.Split(':')[1]).FirstOrDefault();
            if (value != null)
                macro.copeParams.VX = value;
            value = tokens.Where(x => x.StartsWith("VY")).Select(x => x.Split(':')[1]).FirstOrDefault();
            if (value != null)
                macro.copeParams.VY = value;
            value = tokens.Where(x => x.StartsWith("SIDE")).Select(x => x.Split(':')[1]).FirstOrDefault();
            if (value != null)
                macro.copeParams.SIDE = value;
            value = tokens.Where(x => x.StartsWith("GRA")).Select(x => x.Split(':')[1]).FirstOrDefault();
            if (value != null)
                macro.macroClassName = value;
            value = tokens.Where(x => x.StartsWith("BMP")).Select(x => x.Split(':')[1]).FirstOrDefault();
            if (value != null)
                macro.macroBitmapName = value;

            return true;
        }

        public static bool InterpretFNC(in string file, out List<(ICopeParams copeParams, string Macroname, string macroClassName, string macroBitmapName)> copeParamList, out IAngTaglio angTaglio, ref IWorkPiece wp, double brepTol = 0.01)
        {
            angTaglio = null;
            copeParamList = new List<(ICopeParams, string, string, string)>();

            if (file is null)
                return false;

            FNCParser fnc = new FNCParser(file);

            ISection prfSection = fnc.Sections.Where(x => x is PRF).FirstOrDefault(),
                     pcsSection = fnc.Sections.Where(x => x is PCS).FirstOrDefault();

            if(!(prfSection is null))
            { 
                if (prfSection.DataLines.Count != 1 && !(prfSection.DataLines.First() is PrfLine)) 
                    return false;
                else 
                    SetWorkpieceParameters(prfSection.DataLines.First().Tokens, ref wp);
            }

            if (!(pcsSection is null))
            {
                IDataLine HeadLine = pcsSection.DataLines.Where(l => l is HeadLine).FirstOrDefault();

                if (HeadLine is null)
                    return false;

                SetWorkpieceParameters(HeadLine.Tokens, ref wp);

                var taglioTokens = HeadLine.Tokens.Where(
                                                          t => new string(t.TakeWhile(c => char.IsLetter(c)).ToArray()) == "RBI" ||
                                                          new string(t.TakeWhile(c => char.IsLetter(c)).ToArray()) == "RBF" ||
                                                          new string(t.TakeWhile(c => char.IsLetter(c)).ToArray()) == "RAI" ||
                                                          new string(t.TakeWhile(c => char.IsLetter(c)).ToArray()) == "RAF"
                                                         ).ToList();

                if (taglioTokens.Count > 0)
                {
                    // TODO se è falsa vedere di mettere nel log
                    if(!SetMacroParam(taglioTokens, out angTaglio))
                        return false;
                }
                else
                    angTaglio = new ParamTaglio();

                var copeLines = pcsSection.DataLines.Where(l => l is CopeLine).ToList();
                
                if (copeLines.Count > 0)
                {
                    foreach (var copeLine in copeLines)
                    {
                        // TODO guardare se si riesce a mettere nel log
                        if (!SetMacroParam(copeLine.Tokens, out (ICopeParams copeParams, string macroName, string macroBitmapName, uint lineNumber) macro))
                            continue;

                        copeParamList.Add((macro.copeParams, macro.macroName, macro.macroBitmapName, null));
                    }
                }
            }

            //  Eseguo la chiamata solo se wp è di classe EyeWorkPiece
            if (wp is EyeWorkPiece eyeWp)
                eyeWp.CreateSolidRawPart(brepTol);

            return true;
            
        }
    }
}
