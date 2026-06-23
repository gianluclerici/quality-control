global using static Ficep.MacroLibrary.Constants;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;
using System;


namespace Ficep.MacroGra
{

    //
    //  Classe base per la descrizione di ogni singola macro
    //
    public abstract class EyeMacro : IEyeMacro
    {
        //
        //  INPUT
        //
        // Macro parameters
        public string Side { get; private set; }
        public string VX { get; private set; }
        public double ParA { get; private set; }
        public double ParALFA { get; private set; }
        public double ParB { get; private set; }
        public double ParBETA { get; private set; }
        public double ParC { get; private set; }
        public double ParD { get; private set; }
        public double ParDA { get; private set; }
        public double ParDB { get; private set; }
        public double ParDC { get; private set; }
        public double ParE { get; private set; }
        public double ParF { get; private set; }
        public double ParG { get; private set; }
        public double ParH { get; private set; }
        public double ParI { get; private set; }
        public double ParJ { get; private set; }
        public double ParK { get; private set; }
        public double ParL { get; private set; }
        public double ParM { get; private set; }
        public double ParN { get; private set; }
        public double ParO { get; private set; }
        public double ParP { get; private set; }
        public double ParQ { get; private set; }
        public double ParR { get; private set; }
        public double ParS { get; private set; }
        public string VY { get; private set; }

        // Profile Parameters 
        public double Radius { get; private set; }
        public double SA { get; private set; }
        public double SB { get; private set; }
        public double TA { get; private set; }
        public double TB { get; private set; }
        public string CodePrf { get; private set; }
        // Workpiece parameters 
        public double Lp { get; private set; }

        // Mirroring booleans
        public bool MirrorInizialeFinale { get; private set; }
        public bool MirrorAltoBasso { get; private set; }
        public bool MirrorSideASideB { get; private set; }

        // Geometric parameters 
        public double TolBrep { get; private set; }
        public double TolWebFlange { get; private set; }
        public double TolLinear { get; private set; }
        public double TolAngle { get; private set; }
        public double Surplus { get; private set; }
        public double InnerChamferDisFromWeb { get; private set; }

        //  Classe contenente parametri tolleranza e surplus 
        private EyeParam _EyeParam;

        //  Workpiece, contiene la definizione della sezione del profilo e della  lunghezza del pezzo
        public IWorkPiece Wp { get; private set; }
        //  Nome della classe
        public string MacroClassName { get; set; }
        //  Nome della macro
        public string MacroName { get; set; }
        //  Nome della bitmap
        public string MacroBitmapName { get; set; }
        //  Parametri valorizzati
        public ICopeParams Params { get; private set; }
        //  Stringa contenente tutti i profili abilitati per la singola macro (Es: "IUL")
        public string ProfilesEnabled { get; set; }

        //
        //  OUTPUT
        //
        //  Lista delle Features prodotta come output dalla CreateMacro
        //
        public List<EyeFeature> Features { get; protected set; }
        public uint LineNumber { get; set; }

        public EyeMacro(
                        IWorkPiece wp, ICopeParams param, string macroClassName, string macroName,
                        EyeParam eyeParam, uint lineNumber = 0
                       )
        {
            MacroClassName = macroClassName;
            MacroName = macroName;
            MacroBitmapName = "";
            _EyeParam = eyeParam;
            Wp = wp;
            Params = param;
            ProfilesEnabled = "";
            LineNumber = lineNumber;
            Features = new List<EyeFeature>();

            SetParams();
        }

        //
        //  Funzione in cui inserire la validazione geometrica dei parametri
        //
        public virtual ErrMacro Validate()
        {
            return ErrMacro.No_err;
        }

        //
        //  Funzione in cui inserire la validazione del tool di taglio
        //  Riceve una lista di tools e verifica se il suo tool è compatibile con quella lista
        //
        public virtual ErrMacro ValidateTool(List<string> configurationTools)
        {
            ErrMacro errMacro = ErrMacro.err_Tool;
            foreach (var tool in configurationTools)
            {
                if (!tool.Any(char.IsDigit))
                {
                    if (Params.CuttingTool.ToString() == tool)
                        errMacro = ErrMacro.No_err;
                }
                else if (tool.StartsWith("TS"))
                {
                    string value = tool.Substring(2);
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (!int.TryParse(value, out int result))
                            errMacro = ErrMacro.err_Tool;

                        if (((int)Params.CuttingTool) == result)
                            return ErrMacro.No_err;
                    }
                }
            }

            return errMacro;
        }

        //
        //  Funzione invocata per la costruzione della grafica
        //
        public virtual bool CreateMacro()
        {
            return false;
        }

        private void SetParams()
        {
            Side = Params.SIDE;
            VX = Params.VX;
            VY = Params.VY;
            ParA = Params.A;
            ParALFA = Params.ALFA;
            ParB = Params.B;
            ParBETA = Params.BETA;
            ParC = Params.C;
            ParD = Params.D;
            ParDA = Params.DA;
            ParDB = Params.DB;
            ParDC = Params.DC;
            ParE = Params.E;
            ParF = Params.F;
            ParG = Params.G;
            ParH = Params.H;
            ParI = Params.I;
            ParJ = Params.J;
            ParK = Params.K;
            ParL = Params.L;
            ParM = Params.M;
            ParN = Params.N;
            ParO = Params.O;
            ParP = Params.P;
            ParQ = Params.Q;
            ParR = Params.R;
            ParS = Params.S;
            MirrorInizialeFinale = Params.VX != "I";
            MirrorSideASideB = Params.SIDE == "B";
            MirrorAltoBasso = Params.VY != "A" ? Params.VY != "Top" ? true : false : false;
            TolBrep = _EyeParam.Tol.Brep;
            TolWebFlange = _EyeParam.Tol.WebFlange;
            TolLinear = _EyeParam.Tol.Linear;
            TolAngle = _EyeParam.Tol.Angle;
            Surplus = _EyeParam.Surplus;
            InnerChamferDisFromWeb = _EyeParam.InnerChamferDisFromWeb;
            SA = Wp.Prf.SA;
            SB = Wp.Prf.SB;
            TA = Wp.Prf.TA;
            TB = Wp.Prf.TB;
            Radius = Wp.Prf.Radius;
            CodePrf = Wp.Prf.CodePrf;
            Lp = Wp.Lp;
        }

        public bool GetMacroSolids(out List<Brep> macroSolids)
        {
            macroSolids = null;

            if (Features is null || Features.Count == 0)
                return false;

            macroSolids = Features.Select(f => f.Solid).ToList();

            return true;
        }

        public static bool Solve2(double A, double B, double C, ref double root1)
        {
            double discriminant = B * B - 4 * A * C;
            // IF discriminant > 0 then 2 distinct roots if discriminant == 0 the 2 equal roots but the .MAC File treats the same way  and takes only positive root
            if (discriminant < 0)
                return false;
            else
            {
                root1 = (-B + Math.Sqrt(discriminant)) / (2 * A);
                return true;
            }

        }
        public static bool Solve2(double A, double B, double C, ref double root1, ref double root2) // OVERLOAD IF BOTH ROOTS ARE NEEDED
        {
            double discriminant = B * B - 4 * A * C;
            
            if (discriminant < 0)
                return false;
            else
            {
                root1 = (-B + Math.Sqrt(discriminant)) / (2 * A);
                root2 = (-B - Math.Sqrt(discriminant)) / (2 * A);
                return true;
            }

        }

        public virtual bool ValidateGeometry()
        {
            return true;
        }
    }

    public abstract class EyeMacroTaglio : IMacroTaglio
    {
        public EyeParam EyeParam { get; protected set; }

        public IWorkPiece Wp { get; private set; }
        public IAngTaglio Param { get; private set; }
        public string ProfilesEnabled { get; set; }
        public string MacroClassName { get; set; }
        public string MacroName { get; set; }
        public string MacroBitmapName { get; set; }
        public List<EyeFeature> Features { get; protected set; }
        public uint LineNumber { get; set; }

        public EyeMacroTaglio(
                              IWorkPiece wp, IAngTaglio param, string macroClassName, string macroName,
                              EyeParam eyeParam, uint lineNumber = 0
                             )
        {
            MacroClassName = macroClassName;
            MacroName = macroName;
            MacroBitmapName = "";
            EyeParam = eyeParam;
            Wp = wp;
            Param = param;
            ProfilesEnabled = "";
            LineNumber = lineNumber;
            Features = new List<EyeFeature>();
        }

        //
        //  Funzione in cui inserire la validazione geometrica dei parametri
        //
        public virtual ErrMacro Validate()
        {
            return ErrMacro.No_err;
        }

        public virtual bool CreateMacro()
        {
            return false;
        }

        public virtual bool ValidateGeometry()
        {
            return true;
        }
    }

    public abstract class EyeMacroLung : IEyeMacro
    {
        //
        //  INPUT
        //
        // Macro parameters
        public string Side { get; private set; }
        public string VX { get; private set; }
        public double ParA { get; private set; }
        public double ParALFA { get; private set; }
        public double ParB { get; private set; }
        public double ParBETA { get; private set; }
        public double ParC { get; private set; }
        public double ParD { get; private set; }
        public double ParDA { get; private set; }
        public double ParDB { get; private set; }
        public double ParDC { get; private set; }
        public double ParE { get; private set; }
        public double ParF { get; private set; }
        public double ParG { get; private set; }
        public double ParH { get; private set; }
        public double ParI { get; private set; }
        public double ParJ { get; private set; }
        public double ParK { get; private set; }
        public double ParL { get; private set; }
        public double ParM { get; private set; }
        public double ParN { get; private set; }
        public double ParO { get; private set; }
        public double ParP { get; private set; }
        public double ParQ { get; private set; }
        public double ParR { get; private set; }
        public double ParS { get; private set; }
        public string VY { get; private set; }

        // Profile Parameters 
        public double Radius { get; private set; }
        public double SA { get; private set; }
        public double SB { get; private set; }
        public double TA { get; private set; }
        public double TB { get; private set; }
        public string CodePrf { get; private set; }
        // Workpiece parameters 
        public double Lp { get; private set; }

        // Mirroring booleans
        public bool MirrorInizialeFinale { get; private set; }
        public bool MirrorAltoBasso { get; private set; }
        public bool MirrorSideASideB { get; private set; }

        // Geometric parameters 
        public double TolBrep { get; private set; }
        public double TolWebFlange { get; private set; }
        public double TolLinear { get; private set; }
        public double TolAngle { get; private set; }
        public double Surplus { get; private set; }

        //  Classe contenente parametri tolleranza e surplus 
        private EyeParam _EyeParam;

        //  Workpiece, contiene la definizione della sezione del profilo e della  lunghezza del pezzo
        public IWorkPiece Wp { get; private set; }
        //  Nome della classe
        public string MacroClassName { get; set; }
        //  Nome della macro
        public string MacroName { get; set; }
        //  Nome della bitmap
        public string MacroBitmapName { get; set; }
        //  Parametri valorizzati
        public ICopeParams Params { get; private set; }
        //  Stringa contenente tutti i profili abilitati per la singola macro (Es: "IUL")
        public string ProfilesEnabled { get; set; }

        //
        //  OUTPUT
        //
        public List<IEyeCurve> ProgrammedCurves { get; set; }
        public List<Surface> CurvesExtrusion { get; set; }
        public List<IEyeCurve> IntersectionCurves { get; private set; }
        private bool _computedDrawingCurves = false;
        public List<ICurve> DrawingCurves { get; private set; }
        public List<EyeFeature> Features { get; protected set; }
        public uint LineNumber { get; set; }

        public EyeMacroLung(
                        IWorkPiece wp, ICopeParams param, string macroClassName, string macroName,
                        EyeParam eyeParam, uint lineNumber = 0
                       )
        {
            MacroClassName = macroClassName;
            MacroName = macroName;
            MacroBitmapName = "";
            _EyeParam = eyeParam;
            Wp = wp;
            Params = param;
            ProfilesEnabled = "";
            ProgrammedCurves = new List<IEyeCurve>();
            CurvesExtrusion = new List<Surface>();
            DrawingCurves = new List<ICurve>();
            IntersectionCurves = new List<IEyeCurve>();
            Features = new List<EyeFeature>();
            LineNumber = lineNumber;
            SetParams();
        }

        //
        //  Funzione in cui inserire la validazione geometrica dei parametri
        //
        public virtual ErrMacro Validate()
        {
            return ErrMacro.No_err;
        }

        //
        //  Funzione in cui inserire la validazione del tool di taglio
        //  Riceve una lista di tools e verifica se il suo tool è compatibile con quella lista
        //
        public virtual ErrMacro ValidateTool(List<string> configurationTools)
        {
            ErrMacro errMacro = ErrMacro.err_Tool;
            foreach (var tool in configurationTools)
            {
                if (!tool.Any(char.IsDigit))
                {
                    if (Params.CuttingTool.ToString() == tool)
                        errMacro = ErrMacro.No_err;
                }
                else if (tool.StartsWith("TS"))
                {
                    string value = tool.Substring(2);
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (!int.TryParse(value, out int result))
                            errMacro = ErrMacro.err_Tool;

                        if (((int)Params.CuttingTool) == result)
                            return ErrMacro.No_err;
                    }
                }
            }

            return errMacro;
        }

        //
        //  Funzione invocata per la costruzione della grafica
        //
        public virtual bool CreateMacro()
        {
            return false;
        }

        private void SetParams()
        {
            Side = Params.SIDE;
            VX = Params.VX;
            VY = Params.VY;
            ParA = Params.A;
            ParALFA = Params.ALFA;
            ParB = Params.B;
            ParBETA = Params.BETA;
            ParC = Params.C;
            ParD = Params.D;
            ParDA = Params.DA;
            ParDB = Params.DB;
            ParDC = Params.DC;
            ParE = Params.E;
            ParF = Params.F;
            ParG = Params.G;
            ParH = Params.H;
            ParI = Params.I;
            ParJ = Params.J;
            ParK = Params.K;
            ParL = Params.L;
            ParM = Params.M;
            ParN = Params.N;
            ParO = Params.O;
            ParP = Params.P;
            ParQ = Params.Q;
            ParR = Params.R;
            ParS = Params.S;
            MirrorInizialeFinale = Params.VX != "I";
            MirrorSideASideB = Params.SIDE == "B";
            MirrorAltoBasso = Params.VY != "A";
            TolBrep = _EyeParam.Tol.Brep;
            TolWebFlange = _EyeParam.Tol.WebFlange;
            TolLinear = _EyeParam.Tol.Linear;
            TolAngle = _EyeParam.Tol.Angle;
            Surplus = _EyeParam.Surplus;
            SA = Wp.Prf.SA;
            SB = Wp.Prf.SB;
            TA = Wp.Prf.TA;
            TB = Wp.Prf.TB;
            Radius = Wp.Prf.Radius;
            CodePrf = Wp.Prf.CodePrf;
            Lp = Wp.Lp;
        }

        public bool GetDrawingCurves(in Brep finalPart, out List<ICurve> drawingCurves)
        {
            if (!_computedDrawingCurves)
                drawingCurves = new List<ICurve>();
            else
            {
                drawingCurves = DrawingCurves;
                return true;
            }

            if (!ComputeIntersectionCurves(ProgrammedCurves, finalPart))
                return false;

            if (CurvesExtrusion.Count == 0)
                return false;

            double topWebZ = SB / 2 + TA / 2,
                   bottomWebZ = SB / 2 - TA / 2,
                   InternalFlangeAY = TB,
                   InternalFlangeBY = SA - TB,
                   ExternalFlangeAY = 0,
                   ExternalFlangeBY = SA,
                   topFlangeZ = SB,
                   bottomFlangeZ = 0;
            double offset = 0.5;

            foreach (var intersectionCurve in IntersectionCurves)
            {
                List<ICurve> curves = new List<ICurve>();

                if (intersectionCurve is CompositeCurve cc)
                    curves.AddRange(cc.CurveList);
                else
                    curves.Add(intersectionCurve);

                foreach (var curve in curves)
                {
                    Point3D startPoint = curve.StartPoint,
                            endPoint = curve.EndPoint;

                    if (startPoint.Z.IsEqualTo(topFlangeZ, TolLinear) && endPoint.Z.IsEqualTo(topFlangeZ, TolLinear) ||
                        startPoint.Z.IsEqualTo(bottomFlangeZ, TolLinear) && endPoint.Z.IsEqualTo(bottomFlangeZ, TolLinear))
                        continue;
                    // Lines in the web region
                    else if (startPoint.Y.IsLessThan(InternalFlangeBY, TolLinear) && endPoint.Y.IsLessThan(InternalFlangeBY, TolLinear) &&
                        startPoint.Y.IsGreaterThan(InternalFlangeAY, TolLinear) && endPoint.Y.IsGreaterThan(InternalFlangeAY, TolLinear))
                    {
                        if (startPoint.Z.IsGreaterThan(SB / 2, TolLinear) && endPoint.Z.IsGreaterThan(SB / 2, TolLinear))
                            curve.Translate(0, 0, offset);
                        if (startPoint.Z.IsLessThan(SB / 2, TolLinear) && endPoint.Z.IsLessThan(SB / 2, TolLinear))
                            curve.Translate(0, 0, -offset);
                    }
                    else if (startPoint.Y.IsEqualTo(InternalFlangeAY, TolLinear) && endPoint.Y.IsEqualTo(InternalFlangeAY, TolLinear) ||
                             startPoint.Y.IsEqualTo(ExternalFlangeBY, TolLinear) && endPoint.Y.IsEqualTo(ExternalFlangeBY, TolLinear))
                        curve.Translate(0, offset, 0);
                    else if (startPoint.Y.IsEqualTo(InternalFlangeBY, TolLinear) && endPoint.Y.IsEqualTo(InternalFlangeBY, TolLinear) ||
                            startPoint.Y.IsEqualTo(ExternalFlangeAY, TolLinear) && endPoint.Y.IsEqualTo(ExternalFlangeAY, TolLinear))
                        curve.Translate(0, -offset, 0);
                    else
                    {
                        if (startPoint.Z.IsGreaterThan(SB / 2, TolLinear))
                        {
                            if (startPoint.Y.IsLessThan(SA / 2, TolLinear))
                                curve.Translate(0, offset, offset);
                            else
                                curve.Translate(0, -offset, offset);
                        }
                        else
                        {
                            if (startPoint.Y.IsLessThan(SA / 2, TolLinear))
                                curve.Translate(0, offset, -offset);
                            else
                                curve.Translate(0, -offset, -offset);
                        }
                    }
                    drawingCurves.Add(curve);
                }
            }
            
            DrawingCurves = drawingCurves;
            _computedDrawingCurves = true;

            return true;
        }

        private bool ComputeIntersectionCurves(in List<IEyeCurve> programmedCurves, in Brep finalPart)
        {
            Vector3D amount = null;

            foreach (var programmedCurve in programmedCurves)
            {
                bool horizontalPlane = programmedCurve.side == "C" || programmedCurve.side == "D" || programmedCurve.side == "B" && CodePrf == "L",
                verticalPlane = programmedCurve.side == "A" || programmedCurve.side == "B" && CodePrf != "L";

                if (horizontalPlane)
                {
                    double extrusionDepth = CodePrf != "L" ? CodePrf == "I" ? TA + 2 * Radius : TA + Radius : TB + Radius;
                    amount = new Vector3D(0, 0, extrusionDepth);
                }
                else
                {
                    double extrusionDepth = CodePrf != "L" ? TB : TA;
                    amount = new Vector3D(0, extrusionDepth, 0);
                }

                Surface[] curveExtrusion = programmedCurve.ExtrudeAsSurface(amount);

                if (curveExtrusion is null || curveExtrusion.Length > 1)
                    return false;

                CurvesExtrusion.Add(curveExtrusion.First());

                foreach (var intersectionCurve in finalPart.IntersectWith(curveExtrusion.First()))
                {
                    if (intersectionCurve is Line line)
                        IntersectionCurves.Add(new EyeContourLine(line, programmedCurve.side));
                    else if (intersectionCurve is Arc arc)
                        IntersectionCurves.Add(new EyeContourArc(arc, programmedCurve.side));
                    else if (intersectionCurve is CompositeCurve cc)
                        IntersectionCurves.Add(new EyeContourCompositeCurve(cc, programmedCurve.side));
                }
            }

            return true;
        }

        public bool GetMacroSolids(out List<Brep> macroSolids)
        {
            macroSolids = null;

            if (Features is null || Features.Count == 0)
                return false;

            macroSolids = Features.Select(f => f.Solid).ToList();

            return true;
        }

        public virtual bool ValidateGeometry()
        {
            return true;
        }

        public class LongCut
        {
            public static List<ProgramPoint> TrapezoidalCycleCut(bool direction, bool high, int nLoops, double A, double B, double C, List<ProgramPoint> macroPoint)
            {
                // direction è per una futura eventuale implementazione di macchina SX o macchina DX.
                // Per il momento faccio fede alla sezione GRA quindi sempre direction == false (da Lp a 0 quindi macchina DX standard)
                //
                // il periodo inizia sempre con il tratto orizzontale basso se (high == true) e con il tratto orizzontale alto se (high == false)
                double startLength = macroPoint[macroPoint.Count - 1].X;
                double startHeight = macroPoint[macroPoint.Count - 1].Y;

                for (int counter = 0; counter < nLoops; counter++)
                {
                    startLength += (direction ? +C : -C);
                    startHeight -= (high ? B : -B);
                    macroPoint.Add(new ProgramPoint(startLength, startHeight));

                    startLength += (direction ? +A : -A);
                    macroPoint.Add(new ProgramPoint(startLength, startHeight));

                    startLength += (direction ? +C : -C);
                    startHeight -= (high ? -B : +B);
                    macroPoint.Add(new ProgramPoint(startLength, startHeight));

                    startLength += (direction ? +A : -A);
                    macroPoint.Add(new ProgramPoint(startLength, startHeight));
                }
                return macroPoint;
            }

            public static List<ProgramPoint> TrapezoidalLastCut(bool direction, bool high, double spazioRimanente, double endX, double A, double B, double C, List<ProgramPoint> macroPoint)
            {
                //  Quando direction = false non servono correzioni, quando direction = true bisogna spostare l'ultimo punto (con X = endX) a Lp - endX
                double startLength = macroPoint[macroPoint.Count - 1].X;
                double startHeight = macroPoint[macroPoint.Count - 1].Y;
                double offsetHeight = 0;

                if (spazioRimanente > C && spazioRimanente - C > endX) // c'è anora spazio per un tratto in diagonale che scende?
                {
                    startLength += (direction ? +C : -C);
                    startHeight -= (high ? B : -B);
                    macroPoint.Add(new ProgramPoint(startLength, startHeight));//tratto in diagonale che scende

                    if (spazioRimanente - C - A > endX)// c'è anora spazio per un tratto orizzontale basso?
                    {
                        startLength += (direction ? +A : -A);
                        macroPoint.Add(new ProgramPoint(startLength, startHeight)); // tratto orizzontale basso

                        if (spazioRimanente - 2 * C - A > endX)// c'è anora spazio per un tratto in diagonale che sale?
                        {
                            startLength += (direction ? +C : -C);
                            startHeight -= (high ? -B : +B);
                            macroPoint.Add(new ProgramPoint(startLength, startHeight));//tratto diagonale che sale
                                                                                       //
                            macroPoint.Add(new ProgramPoint(endX, startHeight)); // + tratto orizzontale alto fino all'interruzione
                        }
                        else // il taglio si interrompe nel tratto diagonale che sale
                        {
                            if (C > 0)
                                offsetHeight = (high ? (B / C * (spazioRimanente - C - A - endX)) : (-B / C * (spazioRimanente - C - A - endX)));
                            macroPoint.Add(new ProgramPoint(endX, startHeight + offsetHeight));
                        }
                    }
                    else // interrompo nel tratto orizzontale basso
                        macroPoint.Add(new ProgramPoint(endX, startHeight));
                }
                else// il taglio si interrompe nel tratto diagonale che scende
                {
                    offsetHeight = high ? (-B / C * (spazioRimanente - endX)) : (B / C * (spazioRimanente - endX));
                    macroPoint.Add(new ProgramPoint(endX, startHeight + offsetHeight));
                }
                return macroPoint;
            }

            public static List<ProgramPoint> SemiCircleCycleCut(bool direction, bool high, int nLoops, double horizPlateau, double R, double horizStart, double horizEnd, List<ProgramPoint> macroPoint, double C = 0, double F = 0, double G = 0, double H = 0, double K = 0)
            {
                // direction == true -> macchina DX else Macchina SX
                //
                // La funzione inizia sempre con il piccolo tratto orizzontale(horizStart)
                // Poi tratto curvo verso il basso se high == false e veso l'alto se high == true.
                // Dopo di che si va avanti periodicamente fino all'ultimo pezzetto orizzontale(horizEnd)

                // C, F, G, H sono i parametri dei trapezi per saldatura

                double currentX = macroPoint[macroPoint.Count - 1].X;
                double height = macroPoint[macroPoint.Count - 1].Y;
                int counter = 0;

                if (!direction)
                {
                    C = -C; F = -F; G = -G; horizStart = -horizStart; horizEnd = -horizEnd; horizPlateau = -horizPlateau;
                    if (!high)
                        H = -H;
                }
                double Rx = R;
                if (K != 0)
                    Rx = Math.Sqrt(R * R - K * K);
                if (horizStart != 0)
                {
                    if (C != 0 && F != 0 && G != 0 && H != 0) // Trapezio su pezzo orizzontale iniziale
                    {
                        macroPoint.Add(new ProgramPoint(currentX + horizStart - C - 2 * F - G, height));
                        macroPoint.Add(new ProgramPoint(currentX + horizStart - C - F - G, height + H));
                        macroPoint.Add(new ProgramPoint(currentX + horizStart - C - F, height + H));
                        macroPoint.Add(new ProgramPoint(currentX + horizStart - C, height));
                    }
                    currentX += horizStart;
                    macroPoint.Add(new ProgramPoint(currentX, height));
                }

                currentX += (direction ? +2 * Rx : -2 * Rx);
                macroPoint.Add(new ProgramPoint(currentX, height, 0, 0, high ? -R : R));

                while (counter < nLoops)
                {
                    if (C != 0 && F != 0 && G != 0 && H != 0) // Trapezi su Plateau orizzontale ciclico
                    {
                        macroPoint = Picot(currentX + C + F + G / 2, height, F, G, H, macroPoint);
                        macroPoint = Picot(currentX + horizPlateau - (C + F + G / 2), height, F, G, H, macroPoint);
                    }
                    currentX += horizPlateau;
                    macroPoint.Add(new ProgramPoint(currentX, height));

                    currentX += (direction ? +2 * Rx : -2 * Rx);
                    macroPoint.Add(new ProgramPoint(currentX, height, 0, 0, high ? -R : R));
                    counter++;

                }
                if (horizEnd != 0)
                {
                    if (C != 0 && F != 0 && G != 0 && H != 0) // Trapezio su pezzo orizzontale finale
                    {
                        macroPoint.Add(new ProgramPoint(currentX + horizEnd - C - 2 * F - G, height));
                        macroPoint.Add(new ProgramPoint(currentX + horizEnd - C - F - G, height + H));
                        macroPoint.Add(new ProgramPoint(currentX + horizEnd - C - F, height + H));
                        macroPoint.Add(new ProgramPoint(currentX + horizEnd - C, height));
                    }
                    currentX += horizEnd;
                    macroPoint.Add(new ProgramPoint(currentX, height));
                }
                return macroPoint;
            }

            public static List<ProgramPoint> Picot(double X, double Y, double F, double G, double H, List<ProgramPoint> macroPoint)
            {
                // Implementation of MAC_PICOT method
                macroPoint.Add(new ProgramPoint(X - G / 2 - F, Y, 0, 0));
                macroPoint.Add(new ProgramPoint(X - G / 2, Y + H, 0, 0));
                macroPoint.Add(new ProgramPoint(X + G / 2, Y + H, 0, 0));
                macroPoint.Add(new ProgramPoint(X + G / 2 + F, Y, 0, 0));
                return macroPoint;
            }


            public static void CircleLineIntersection(double xA, double yA, double mA, double xC, double yC, double F, double G, double H, double R, out bool intersezioneTrovata, out double intX, out double intY)
            {
                // Initialize output variables
                intersezioneTrovata = false;
                intX = 0;
                intY = 0;

                // Define and initialize variables
                double alfa, beta, delta, orario, para, parb, parc;

                // Calculate internal variables
                orario = (R > 0) ? 0 : 1;

                // Check if line is vertical
                if (Math.Abs(mA) >= 1000000)
                {
                    // Vertical line
                    delta = R * R - Math.Pow(xA - xC, 2);
                    if (delta > 0)
                    {
                        intersezioneTrovata = true;
                        delta = Math.Sqrt(delta);
                        intX = xA;
                        intY = ((F == G) == (orario == H)) ? yC - delta : yC + delta;
                    }
                }
                else
                {
                    // Non-vertical line
                    alfa = mA;
                    beta = yA - mA * xA;
                    para = Math.Pow(alfa, 2) + 1;
                    parb = 2 * (alfa * (beta - yC) - xC);
                    parc = -R * R + Math.Pow(xC, 2) + Math.Pow(beta - yC, 2);
                    delta = Math.Pow(parb, 2) - 4 * para * parc;
                    if (delta > 0)
                    {
                        intersezioneTrovata = true;
                        delta = Math.Sqrt(delta);
                        intX = ((F == G) == (orario == H)) ? -(parb - delta) / 2 / para : -(parb + delta) / 2 / para;
                        intY = alfa * intX + beta;
                    }
                }
            }

            public static void CalFix(double B, double D, double K, double E, double M, double O, double P, double ALFA, out double OffsetP, out double OffsetAlfa)
            {
                //  START
                OffsetP = 0;
                bool intersezioneTrovata = false;
                //  TRATTI LINEARI
                //  interX è la quota X di intersezione calcolata a Y = B.
                double interX = O + (B - K) * Math.Tan(P.ToRad());
                if (!intersezioneTrovata)
                {
                    if (interX <= E - D / 2 && interX >= D / 2)
                    {
                        intersezioneTrovata = true;
                        OffsetP = interX - O;
                    }
                }
                if (!intersezioneTrovata)
                {
                    if (interX <= 2 * E - D / 2 && interX >= E + D / 2)
                    {
                        intersezioneTrovata = true;
                        OffsetP = interX - O;
                    }
                }
                //  TRATTI TONDI
                for (int i = 0; i < 4 && !intersezioneTrovata; i++)
                {
                    CircleLineIntersection(O, B, Math.Tan((90 + P).ToRad()), E * (-1 + i), 0, i < 2 ? 1 : (P > 0 ? 0 : 1), 1, 0, D / 2, out intersezioneTrovata, out double intX, out double intY);
                    if (intersezioneTrovata && intY >= 0)
                        OffsetP = intX - O;
                    else
                        intersezioneTrovata = false;
                }

                //  END
                OffsetAlfa = 0;
                intersezioneTrovata = false;
                //  TRATTI LINEARI
                interX = -M + (B - K) * Math.Tan(ALFA.ToRad());
                if (!intersezioneTrovata)
                {
                    if (interX <= -D / 2 && interX >= -E + D / 2)
                    {
                        intersezioneTrovata = true;
                        OffsetAlfa = -(interX + M);
                    }
                }
                if (!intersezioneTrovata)
                {
                    if (interX <= -(D / 2 + E) && interX >= -2 * E + D / 2)
                    {
                        intersezioneTrovata = true;
                        OffsetAlfa = -(interX + M);
                    }
                }
                //  TRATTI TONDI
                for (int i = 0; i < 4 && !intersezioneTrovata; i++)
                {
                    CircleLineIntersection(-M, B, Math.Tan((90 + ALFA).ToRad()), E * (1 - i), 0, i == 0 || i == 3 ? 1 : (ALFA > 0 ? 0 : 1), 1, 0, D / 2, out intersezioneTrovata, out double intX, out double intY);
                    if (intersezioneTrovata && intY >= 0)
                        OffsetAlfa = -(intX + M);
                    else
                        intersezioneTrovata = false;
                }
            }

            public static void CalMob(double B, double D, double K, double E, double M, double O, double P, double ALFA, out double OffsetP, out double OffsetAlfa)
            {
                //  START
                OffsetP = 0; // REG_24
                bool intersezioneTrovata = false;
                //  TRATTI LINEARI
                //  interX è la quota X di intersezione calcolata a Y = B.
                double interX = O - (B - K) * Math.Tan(P.ToRad());
                if (!intersezioneTrovata)
                {
                    if (interX <= E - D / 2 && interX >= D / 2)
                    {
                        intersezioneTrovata = true;
                        OffsetP = interX - O;
                    }
                }
                if (!intersezioneTrovata)
                {
                    if (interX <= 2 * E - D / 2 && interX >= E + D / 2)
                    {
                        intersezioneTrovata = true;
                        OffsetP = interX - O;
                    }
                }
                //  TRATTI TONDI
                for (int i = 0; i < 4 && !intersezioneTrovata; i++)
                {
                    CircleLineIntersection(O, -B, Math.Tan((90 + P).ToRad()), E * (-1 + i), 0, i == 0 || i == 3 ? 1 : (P > 0 ? 1 : 0), 1, 0, D / 2, out intersezioneTrovata, out double intX, out double intY);
                    if (intersezioneTrovata && intY <= 0)
                        OffsetP = intX - O;
                    else
                        intersezioneTrovata = false;
                }

                //  END
                OffsetAlfa = 0; // 
                intersezioneTrovata = false;
                //  TRATTI LINEARI
                interX = -M - (B - K) * Math.Tan(ALFA.ToRad());
                if (!intersezioneTrovata)
                {
                    if (interX <= -D / 2 && interX >= -E + D / 2)
                    {
                        intersezioneTrovata = true;
                        OffsetAlfa = -(interX + M);
                    }
                }
                if (!intersezioneTrovata)
                {
                    if (interX <= -(D / 2 + E) && interX >= -2 * E + D / 2)
                    {
                        intersezioneTrovata = true;
                        OffsetAlfa = -(interX + M);
                    }
                }
                //  TRATTI TONDI
                for (int i = 0; i < 4 && !intersezioneTrovata; i++)
                {
                    CircleLineIntersection(-M, -B, Math.Tan((90 + ALFA).ToRad()), E * (1 - i), 0, i == 0 || i == 3 ? 1 : (ALFA < 0 ? 0 : 1), 1, 0, D / 2, out intersezioneTrovata, out double intX, out double intY);
                    if (intersezioneTrovata && intY <= 0)
                        OffsetAlfa = -(intX + M);
                    else
                        intersezioneTrovata = false;
                }
            }

            public static List<ProgramPoint> G1LUN15I(double A, double B, double C, double D, double E, double F, double G, double H, double J, double K, double M, double N, double O, List<ProgramPoint> macroPoint, double Lp, double startOffsetP = 0, double endOffsetAlfa = 0)
            {
                double raggioRidottoK = Math.Sqrt(D * D / 4 - K * K);

                //  Primo tratto
                macroPoint.Add(new ProgramPoint(Lp - (A + M + N * E + O - startOffsetP), 0, 0));

                double REG_15 = O > E ? O - E : E - O;
                double REG_16 = Math.Sqrt(D * D / 4 - REG_15 * REG_15);

                if (O <= E - D / 2)
                {
                    macroPoint.Add(new ProgramPoint(Lp - (A + M + N * E + O), B - K, 0));
                    if (J == 1 || J == 3)
                    {
                        if (O == E - D / 2)
                        {
                            macroPoint = LongCut.Picot(Lp - (A + M + N * E + D / 2 + (E - D) / 2 + C / 2), B - K, F, G, H, macroPoint);
                            if (C > 0)
                            {
                                macroPoint = LongCut.Picot(Lp - (A + M + N * E + D / 2 + (E - D) / 2 - C / 2), B - K, F, G, H, macroPoint);
                            }
                        }
                        else
                        {
                            macroPoint = LongCut.Picot(Lp - (A + M + N * E + D / 2 + (O - D / 2) / 2), B - K, F, G, H, macroPoint);
                        }
                    }
                    macroPoint.Add(new ProgramPoint(Lp - (A + M + N * E + D / 2), B - K, 0));
                }
                else
                {
                    macroPoint.Add(new ProgramPoint(Lp - (A + M + N * E + O), B - REG_16, 0));
                    macroPoint.Add(new ProgramPoint(Lp - (A + M + N * E + (E - raggioRidottoK)), B - K, 0, 0, D / 2));

                    if (J == 1 || J == 3)
                    {
                        macroPoint = LongCut.Picot(Lp - (A + M + N * E + D / 2 + (E - D) / 2 + C / 2), B - K, F, G, H, macroPoint);
                        if (C > 0)
                        {
                            macroPoint = LongCut.Picot(Lp - (A + M + N * E + D / 2 + (E - D) / 2 - C / 2), B - K, F, G, H, macroPoint);
                        }
                    }
                    macroPoint.Add(new ProgramPoint(Lp - (A + M + N * E + D / 2), B - K, 0));
                }

                //  Tratto ciclico
                double actualC = (J == 1 || J == 3) ? (E - D - C - G - 2 * F) / 2 : 0;
                macroPoint = LongCut.SemiCircleCycleCut(true, false, (int)N, E - D, D / 2, 0, 0, macroPoint, actualC, F, G, H, K);

                //  Ultimo tratto
                REG_15 = M > E ? M - E : E - M;
                REG_16 = Math.Sqrt(D * D / 4 - REG_15 * REG_15);

                if (M > E - D / 2)
                {
                    if (J == 1 || J == 3)
                    {
                        macroPoint = LongCut.Picot(Lp - (A + M - (D / 2) - (E - D) / 2 + C / 2), B - K, F, G, H, macroPoint);
                        if (C > 0)
                        {
                            macroPoint = LongCut.Picot(Lp - (A + M - (D / 2) - (E - D) / 2 - C / 2), B - K, F, G, H, macroPoint);
                        }
                    }
                    macroPoint.Add(new ProgramPoint(Lp - (A + M - E + D / 2), B - K));
                    macroPoint.Add(new ProgramPoint(Lp - A, B - REG_16, 0, 0, D / 2));
                }
                else
                {
                    if (M > D / 2 + G + 2 * F && (J == 1 || J == 3))
                    {
                        if (M == E - D / 2)
                        {
                            macroPoint = LongCut.Picot(Lp - (A + M - (D / 2) - (E - D) / 2 + C / 2), B - K, F, G, H, macroPoint);
                            if (C > 0)
                            {
                                macroPoint = LongCut.Picot(Lp - (A + M - (D / 2) - (E - D) / 2 - C / 2), B - K, F, G, H, macroPoint);
                            }
                        }
                        else
                        {
                            macroPoint = LongCut.Picot(Lp - (A + (M - D / 2) / 2), B - K, F, G, H, macroPoint);
                        }
                    }
                    macroPoint.Add(new ProgramPoint(Lp - A, B - K));
                }
                macroPoint.Add(new ProgramPoint(Lp - A - endOffsetAlfa, 0));

                return macroPoint;
            }

            public static List<ProgramPoint> G2LUN15I(double A, double B, double C, double D, double E, double F, double G, double H, double J, double K, double M, double N, double O, List<ProgramPoint> macroPoint, double Lp, double width, double startOffsetP = 0, double endOffsetAlfa = 0)
            {
                double raggioRidottoK = Math.Sqrt(D * D / 4 - K * K);

                //  Primo tratto
                macroPoint.Add(new ProgramPoint(Lp - (A + E / 2 + M + N * E + O - startOffsetP), width, 0, 0));

                if (O > E - D / 2)
                {
                    double REG_15 = O > E ? O - E : E - O;
                    double REG_16 = Math.Sqrt(D * D / 4 - REG_15 * REG_15);

                    macroPoint.Add(new ProgramPoint(Lp - (A + E / 2 + M + N * E + O), width - (B - REG_16), 0, 0));
                    macroPoint.Add(new ProgramPoint(Lp - (A + E / 2 + M + N * E + E - raggioRidottoK), width - (B - K), 0, 0, -D / 2));

                    if (J == 2 || J == 3)
                    {
                        macroPoint = LongCut.Picot(Lp - (A + E / 2 + M + N * E + D / 2 + (E - D) / 2 + C / 2), width - (B - K), F, G, H, macroPoint);
                        if (C > 0)
                        {
                            macroPoint = LongCut.Picot(Lp - (A + E / 2 + M + N * E + D / 2 + (E - D) / 2 - C / 2), width - (B - K), F, G, H, macroPoint);
                        }
                    }
                }
                else
                {
                    macroPoint.Add(new ProgramPoint(Lp - (A + E / 2 + M + N * E + O), width - (B - K), 0, 0));
                    if (O > D / 2 + G + 2 * F && (J == 2 || J == 3))
                    {
                        if (O == E - D / 2)
                        {
                            macroPoint = LongCut.Picot(Lp - (A + E / 2 + M + N * E + D / 2 + (E - D) / 2 + C / 2), width - (B - K), F, G, H, macroPoint);
                            if (C > 0)
                            {
                                macroPoint = LongCut.Picot(Lp - (A + E / 2 + M + N * E + D / 2 + (E - D) / 2 - C / 2), width - (B - K), F, G, H, macroPoint);
                            }
                        }
                        else
                        {
                            macroPoint = LongCut.Picot(Lp - (A + E / 2 + M + N * E + D / 2 + (O - D / 2) / 2), width - (B - K), F, G, H, macroPoint);
                        }
                    }
                }

                macroPoint.Add(new ProgramPoint(Lp - (A + E / 2 + M + N * E + raggioRidottoK), width - (B - K), 0, 0));

                //  Tratto ciclico
                macroPoint = LongCut.SemiCircleCycleCut(true, true, (int)N, E - D, D / 2, 0, 0, macroPoint, (J == 2 || J == 3) ? (E - D - C - G - 2 * F) / 2 : 0, F, G, H, K);

                //  Ultimo tratto
                if (M <= E - D / 2)
                {
                    macroPoint.Add(new ProgramPoint(Lp - (A + M + E / 2 - raggioRidottoK), width - (B - K), 0, 0));
                    if (M > D / 2 + G + 2 * F && (J == 2 || J == 3))
                    {
                        if (M == E - D / 2)
                        {
                            macroPoint = LongCut.Picot(Lp - (A + M + C / 2), width - (B - K), F, G, H, macroPoint);
                            if (C > 0)
                            {
                                macroPoint = LongCut.Picot(Lp - (A + M - C / 2), width - (B - K), F, G, H, macroPoint);
                            }
                        }
                        else
                        {
                            macroPoint = LongCut.Picot(Lp - (A + E / 2 + (M - D / 2) / 2), width - (B - K), F, G, H, macroPoint);
                        }
                    }
                    macroPoint.Add(new ProgramPoint(Lp - (A + E / 2), width - (B - K), 0, 0));
                }
                else
                {
                    double REG_15 = M > E ? (M - E) : (E - M);
                    double REG_16 = Math.Sqrt(D * D / 4 - REG_15 * REG_15);

                    macroPoint.Add(new ProgramPoint(Lp - (A + M + E / 2 - raggioRidottoK), width - (B - K), 0, 0));
                    if (J == 2 || J == 3)
                    {
                        macroPoint = LongCut.Picot(Lp - (A + M + C / 2), width - (B - K), F, G, H, macroPoint);
                        if (C > 0)
                        {
                            macroPoint = LongCut.Picot(Lp - (A + M - C / 2), width - (B - K), F, G, H, macroPoint);
                        }
                    }
                    macroPoint.Add(new ProgramPoint(Lp - (A + M - E / 2 + raggioRidottoK), width - (B - K), 0, 0));
                    macroPoint.Add(new ProgramPoint(Lp - (A + E / 2), width - (B - REG_16), 0, 0, -D / 2));
                }
                macroPoint.Add(new ProgramPoint(Lp - (A + E / 2 + endOffsetAlfa), width, 0, 0));
                return macroPoint;
            }

        }
    }

    public interface IEyeMacro : IMacroCope
    {
        List<EyeFeature> Features { get; }

        bool GetMacroSolids(out List<Brep> macroSolids);
    }
}