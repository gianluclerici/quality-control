using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ficep.RobServer.Data;
using Ficep.Utils;

namespace FicepDstvParser
{
    public class DSTVParser
    {
        private List<string> headerLines;

        public IWorkPiece Wp { get; private set; }
        public List<IDstvBlock> Blocks { get; private set; }
        private string errorMessage;
        private string _path;

        public DSTVParser(string path) 
        {
            Blocks = new List<IDstvBlock>();
            _path = path;
        }

        public bool ReadDstv()
        {
            List<string> dstvLines;

            List<string> headerLines = null;
            List<IDstvBlock> akBlocks = null,
            ikBlocks = null,
            boBlocks = null;
            

            try
            {
                dstvLines = File.ReadAllLines(_path).ToList();
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                return false;
            }

            // Check the correctness of the file 
            if (dstvLines.Count == 0 || dstvLines.First() != "ST" || dstvLines.Last() != "EN")
                return false;

            dstvLines = dstvLines.Where(s => s != "ST" && s != "EN" && s != string.Empty && !s.StartsWith("**")).Skip(7).ToList();

            headerLines = dstvLines.Take(13).ToList();
            dstvLines = dstvLines.Skip(13).ToList();

            // WorkPiece parameters 
            if (!double.TryParse(headerLines[1], out double lenght))
                return false;

            // Profile parameters
            string codePrf = headerLines[0].Trim();

            if (!double.TryParse(headerLines[2], out double hc))
                return false;
            if (!double.TryParse(headerLines[3], out double sa))
                return false;
            if (!double.TryParse(headerLines[4], out double ta))
                return false;
            if (!double.TryParse(headerLines[5], out double tc))
                return false;
            if (!double.TryParse(headerLines[6], out double radius))
                return false;

            // Check if they coincide with the definition in the fnc *******************************

            if (!double.TryParse(headerLines[7], out double webStart))
                return false;
            if (!double.TryParse(headerLines[8], out double webEnd))
                return false;
            if (!double.TryParse(headerLines[9], out double flangeStart))
                return false;
            if (!double.TryParse(headerLines[10], out double flangeEnd))
                return false;

            Wp = new WorkPiece(codePrf, hc, tc, sa, ta, radius, lenght, webStart, webEnd, flangeStart, flangeEnd);

            // Retrieve the dstv blocks
            GetDstvBlocks("ak", ref dstvLines, out akBlocks);
            GetDstvBlocks("ik", ref dstvLines, out ikBlocks);
            GetDstvBlocks("bo", ref dstvLines, out boBlocks);

            Blocks.AddRange(akBlocks);
            Blocks.AddRange(ikBlocks);
            Blocks.AddRange(boBlocks);

            if (!ComputeProgramPointsList())
                return false;

            return true;
        }

        /// <summary>
        /// Retrieve the dstv blocks specified
        /// </summary>
        /// <param name="dstvBlock">
        /// The dstv block desired
        /// </param>
        /// <param name="dstvLines">
        /// List of strings of the dstv file
        /// </param>
        /// <param name="dstvBlocks">
        /// the retrieved blocks
        /// </param>
        /// <returns>
        /// The retrieved blocks and the boolean to indicate if the operation is succeded
        /// </returns>
        private bool GetDstvBlocks(in string dstvBlock, ref List<string> dstvLines, out List<IDstvBlock> dstvBlocks)
        {
            dstvBlocks = new List<IDstvBlock>();

            List<string> dstvBlockLines;
            bool dstvBlockFound = true;
            while (dstvBlockFound)
            {
                dstvBlockFound = GetDstvBlockLines(dstvBlock, ref dstvLines, out dstvBlockLines);

                // if no ak block found exit
                if (!dstvBlockFound)
                    break;

                List<DstvLine> dstvBlockLineList = new List<DstvLine>();
                string plane = null;
                foreach (string line in dstvBlockLines)
                {
                    if (!ParseDataLine(line, out List<(string, double?, string)> lineTokens, out string tempPlane))
                        return false;

                    dstvBlockLineList.Add(new DstvLine(lineTokens));

                    if (tempPlane != null)
                        plane = tempPlane;
                }

                if (dstvBlock.Equals("ak", StringComparison.CurrentCultureIgnoreCase))
                {
                    Ak akBlock;
                    if (plane != null)
                    {
                        akBlock = new Ak(plane, dstvBlockLineList);
                        dstvBlocks.Add(akBlock);
                    }
                    else
                        return false;
                }
                else if (dstvBlock.Equals("ik", StringComparison.CurrentCultureIgnoreCase))
                {
                    Ik ikBlock;
                    if (plane != null)
                    {
                        ikBlock = new Ik(plane, dstvBlockLineList);
                        dstvBlocks.Add((ikBlock));
                    }
                    else
                        return false;
                }
                else if (dstvBlock.Equals("bo", StringComparison.CurrentCultureIgnoreCase))
                {
                    Bo boBlock;
                    if (plane != null)
                    {
                        boBlock = new Bo(plane, dstvBlockLineList);
                        dstvBlocks.Add(boBlock);
                    }
                    else
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Retrieve the specified dstv block lines without the name of the block 
        /// </summary>
        /// <param name="dstvBlock">String representing the dstv block to be retrieved</param>
        /// <param name="dstvLines">List of lines in the dstv file</param>
        /// <param name="blockLines">List of lines in the first dstv block found</param>
        /// <returns>The specified block of lines and the dstv block of lines without the block of lines found</returns>
        private bool GetDstvBlockLines(in string dstvBlock, ref List<string> dstvLines, out List<string> blockLines)
        {
            blockLines = new List<string>();

            // Check the correctness of the dstvlines passed in
            if (dstvLines is null || dstvLines.Count == 0)
                return false;

            // Initialize the needed variables
            bool blockIdentifierFound = false;
            string[] lines = new string[dstvLines.Count]; // Copy of the dstv lines that will not be modified 
            dstvLines.CopyTo(lines);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // Check if the line is equal to the dstvBlock line identifier searched, in case delete the line from the input list
                if (line.Equals(dstvBlock, StringComparison.OrdinalIgnoreCase) && !blockIdentifierFound)
                {
                    blockIdentifierFound = true;
                    dstvLines.Remove(line);
                    continue;
                }

                // If previously found the dstvBlock line identifier and the line don't start with spaces
                // exit the function because the block of line searched is finished, i.e. starts another block
                if (blockIdentifierFound && !line.StartsWith(" "))
                {
                    break;
                }

                // If previously found the dstvBlock line identifier add the line to the list and remove it from the 
                // input list passed in 
                if (blockIdentifierFound)
                { 
                    blockLines.Add(line);
                    dstvLines.Remove(line);
                }
            }

            return blockLines.Count > 0;
        }

        /// <summary>
        /// Parse a data line 
        /// </summary>
        /// <param name="dataLine">
        /// The string containing the data line
        /// </param>
        /// <param name="lineTokens">
        /// List of tokens found in the data line with the data attributes (leading letters, value, trailing letters)
        /// </param>
        /// <param name="plane">
        /// Plane of the data line if present
        /// </param>
        /// <returns>
        /// The list of tokens, the plane and if the parsing operation is succeded or not
        /// </returns>
        private bool ParseDataLine(in string dataLine, out List<(string, double?, string)> lineTokens, out string plane)
        {
            lineTokens = new List<(string, double?, string)>();
            plane = null;

            // Check the correctness of the input data
            if (dataLine is null)
                return false;

            // Split the data line in tokens
            List<string> tokens = dataLine.TrimStart(' ').Split(' ').Where(s => !s.Equals("")).ToList();

            if (tokens.Count == 0) 
                return false;

            foreach (string t in tokens)
            {
                (string leadingLetters, double? value, string trailingLetters) token = ParseString(t);

                if (token.leadingLetters is null && token.trailingLetters is null && token.value is null)
                    return false;

                // If the token is the plane save it and skip it
                if (token.trailingLetters.Length == 1 && token.leadingLetters.Length == 1 && token.trailingLetters == token.leadingLetters && token.value == null)
                { 
                    plane = token.leadingLetters;
                    continue;
                }

                lineTokens.Add(token);
            }

            return true;
        }

        /// <summary>
        /// Parse a string searching the dstv data attributes
        /// </summary>
        /// <param name="token"
        /// Input string token of the data line 
        /// </param>
        /// <returns>
        /// A tuple containing the leading, trailing letters and the value  
        /// </returns>
        private  (string leadingLetters, double? number, string trailingLetters) ParseString(string token)
        {
            // Define regular expressions patterns for leading letters, float numbers, and trailing letters
            string leadingLettersPattern = @"^[A-Za-z]+";
            string floatPattern = @"[-+]?\d*\.\d+|\d+";
            string trailingLettersPattern = @"[A-Za-z]+$";

            // Extract leading letters
            Match leadingLettersMatch = Regex.Match(token, leadingLettersPattern);
            string leadingLetters = leadingLettersMatch.Success ? leadingLettersMatch.Value : "";

            // Extract float number
            Match floatMatch = Regex.Match(token, floatPattern);
            double? number = floatMatch.Success ? double.Parse(floatMatch.Value) : (double?)null;

            // Extract trailing letters
            Match trailingLettersMatch = Regex.Match(token, trailingLettersPattern);
            string trailingLetters = trailingLettersMatch.Success ? trailingLettersMatch.Value : "";

            return (leadingLetters, number, trailingLetters);
        }

        private bool ComputeProgramPointsList()
        {
            var contourBlocks = Blocks.Where(b => b is Ak || b is Ik).ToList();
            foreach (var block in contourBlocks)
            {
                if (block is IContour contour)
                {
                    //if (!GenerateProgramPoints(block, contour))
                    if (!GenerateShiftedProgramPoints(block, contour))
                        return false;
                }
            }

            return true;
        }

        private bool GenerateProgramPoints(IDstvBlock dstvBlock, IContour contour)
        {
            var dstvLines = dstvBlock.DstvLines;
            int chamferIndex = 0;

            // Fill the list of point
            for (int i = 0; i < dstvLines.Count; i++)
            {
                DstvLine dstvLine = dstvLines[i];
                if (!ComputePoint(dstvBlock.Plane, dstvLine, out double x, out double y, out double z, out double r, out bool wNotch))
                    continue;

                contour.ProgramPoints.Add(new ProgramPoint(x, y, z, r));

                if (dstvLine.LineTokens.Count > 3)
                {
                    double phi1 = dstvLine.LineTokens[3].value.Value,
                           y1 = dstvLine.LineTokens.Count > 4? dstvLine.LineTokens[4].value.Value : 0,
                           phi2 = dstvLine.LineTokens.Count > 5 ? dstvLine.LineTokens[5].value.Value : 0,
                           y2 = dstvLine.LineTokens.Count > 6 ? dstvLine.LineTokens[6].value.Value : 0;

                    contour.ChamferDescriptionList.Add((chamferIndex, phi1, y1, phi2, y2));
                }
                chamferIndex++;
            }

            return true;
        }

        private bool GenerateShiftedProgramPoints(IDstvBlock dstvBlock, IContour contour)
        {
            var dstvLines = dstvBlock.DstvLines;
            int chamferIndex = 0;

            // Fill the list of point
            for (int i = 0; i < dstvLines.Count; i++)
            {
                DstvLine dstvLine = dstvLines[i];
                if (!ComputePoint(dstvBlock.Plane, dstvLine, out double x, out double y, out double z, out double r, out bool wNotch))
                    continue;

                ProgramPoint curr = new ProgramPoint(x, y, z, r);

                bool isChamfer = dstvLine.LineTokens.Count > 3;
                if (isChamfer)
                {
                    double phi1 = dstvLine.LineTokens[3].value.Value,
                           y1 = dstvLine.LineTokens.Count > 4 ? dstvLine.LineTokens[4].value.Value : 0,
                           phi2 = dstvLine.LineTokens.Count > 5 ? dstvLine.LineTokens[5].value.Value : 0,
                           y2 = dstvLine.LineTokens.Count > 6 ? dstvLine.LineTokens[6].value.Value : 0;

                    contour.ChamferDescriptionList.Add((chamferIndex, phi1, y1, phi2, y2));
                }

                // If the current point is a w notch, the previous and next point are not added to the contour
                if (wNotch)
                {
                    contour.ProgramPoints.RemoveAt(contour.ProgramPoints.Count - 1);
                    i++;// do not add the next point
                }
                contour.ProgramPoints.Add(new ProgramPoint(x, y, z, r));

                chamferIndex++;
            }


            // TO DO: per ora implementato lo shifting senza considerare il piano 
            for (int i = 0; i < contour.ChamferDescriptionList.Count; i++)
            {
                (int chamferIndex, double phi1, double y1, double phi2, double y2) chamferDescription = contour.ChamferDescriptionList[i];

                // Succede solo se la descrizione del dstv è errata 
                if (chamferIndex + 1 > contour.ProgramPoints.Count)
                    continue;

                ProgramPoint curr = contour.ProgramPoints[chamferIndex],
                             next = contour.ProgramPoints[chamferIndex + 1];

                double bevelAngle = chamferDescription.phi1,
                       bevelDepth = Wp.Prf.TA - chamferDescription.y1;

                bool topBevel = bevelAngle > 0;
                ComputeBevelCoordinates(ref next, ref curr, bevelAngle, bevelDepth, topBevel);
            }

            return true;
        }

        private bool ComputeBevelCoordinates(ref ProgramPoint pCurr, ref ProgramPoint pPrev, double bevelAngle, double bevelDepth, bool topBevel)
        {
            // Vettore rappresentante il percorso tra vCurr e vPrev
            Vector3 pathVect = Vector3.Normalize(new Vector3((float)(pCurr.X - pPrev.X), (float)(pCurr.Y - pPrev.Y), 0)),
                    zVect = new Vector3(0, 0, 1);
            // Vettore normale alla direzione del percorso
            Vector3 normalDir = Vector3.Cross(pathVect, zVect);

            // calcolo le nuove coordinate del vertex
            if (topBevel)
            {
                pCurr.X -= normalDir.X * bevelDepth * Math.Tan(bevelAngle.ToRad());
                pCurr.Y -= normalDir.Y * bevelDepth * Math.Tan(bevelAngle.ToRad());
                pPrev.X -= normalDir.X * bevelDepth * Math.Tan(bevelAngle.ToRad());
                pPrev.Y -= normalDir.Y * bevelDepth * Math.Tan(bevelAngle.ToRad());
            }
            else
            {
                //pCurr.X -= normalDir.X * bevelDepth * Math.Tan(bevelAngle.ToRad());
                //pCurr.Y -= normalDir.Y * bevelDepth * Math.Tan(bevelAngle.ToRad());
            }

            return true;
        }

        private bool ComputePoint(string plane, in DstvLine dstvLine, out double x, out double y, out double z, out double r, out bool wNotch)
        {
            x = 0; y = 0; z = 0; r = 0;
            wNotch = false;

            if (
                (
                 dstvLine.LineTokens[1].trailinglLetters.Equals("w", StringComparison.OrdinalIgnoreCase) &&
                 dstvLine.LineTokens[2].value == 0
                ) 
                ||
                 dstvLine.LineTokens[1].trailinglLetters.Equals("t", StringComparison.OrdinalIgnoreCase)
                )
                return false;
            else if (dstvLine.LineTokens[1].trailinglLetters.Equals("w", StringComparison.OrdinalIgnoreCase))
            {
                // Add a hole needed to make the w notch
                wNotch = true;
                var bo = Blocks.Where(b => b is Bo && b.Plane == plane).FirstOrDefault();

                // If the block is not present create it, otherwise add the hole to the block 
                if (bo != null)
                    ((Bo)bo).Holes.Add(new DstvHole(Math.Abs(dstvLine.LineTokens[2].value.Value * 2), dstvLine.LineTokens[0].value.Value, dstvLine.LineTokens[1].value.Value, 0, plane));
                else
                {
                    Bo w = new Bo(plane, new List<DstvLine>());
                    w.Holes.Add(new DstvHole(Math.Abs(dstvLine.LineTokens[2].value.Value * 2), dstvLine.LineTokens[0].value.Value, dstvLine.LineTokens[1].value.Value, 0, plane));

                    Blocks.Add(w);

                }
            }                                    

            //    throw new Exception("w notch not handled");


            if (plane == "v")
            { 
                x = dstvLine.LineTokens[0].value ?? 0;
                y = dstvLine.LineTokens[1].value ?? 0;
                z = Wp.Prf.CodePrf == "I" ? Wp.Prf.SB / 2 - Wp.Prf.TA / 2 : 0;
                r = wNotch ? 0 : dstvLine.LineTokens[2].value ?? 0;
            }
            else if (plane == "u")
            {
                x = dstvLine.LineTokens[0].value ?? 0;
                y = Wp.Prf.TB;
                z = dstvLine.LineTokens[1].value ?? 0;
                r = wNotch ? 0 : dstvLine.LineTokens[2].value ?? 0;
            }
            else if (plane == "o")
            {
                x = dstvLine.LineTokens[0].value ?? 0;
                y = Wp.Prf.SA - Wp.Prf.TB;
                z = dstvLine.LineTokens[1].value ?? 0;
                r = wNotch ? 0 : dstvLine.LineTokens[2].value ?? 0;
            }
            else
            {
                x = 0; y = 0; z = 0; r = 0;
            }

            return true;
        }
    }
}


