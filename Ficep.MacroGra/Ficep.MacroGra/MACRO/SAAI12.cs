using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SAAI12 : EyeMacro
    {

        public SAAI12(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "I";
        }

        //
        //  Questa è una rara SCxx che ha 8 casi
        //  Il mirroring alto-basso va gestito come ECCEZIONE all'interno della macro
        //
        public override bool CreateMacro()
        {
            ///////////////////////////////
            //      TESTA: fissa
            ///////////////////////////////
            
            //  Verifico che il profilo sia tra quelli abilitati
            if (!ProfilesEnabled.Contains(CodePrf))
                return false;

            //  Validazione parametri geometrici
            if (Validate() != ErrMacro.No_err)
                return false;

            ///////////////////////////////
            //      CORPO: variabile
            ///////////////////////////////
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            List<Brep> breps = new List<Brep>();

            //
            //  ESTRUSIONE ANIMA
            //
            string extrusionPlane = "C";
            double extrusionDepth = SB;

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            macroPoint.Add(new ProgramPoint(0, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, 0, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParE, ParD - (ParD - ParB) * ParE / ParA, 0, 0));
            macroPoint.Add(new ProgramPoint(0, ParD + ParC, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, false/*MirrorAltoBasso*/, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            //
            //  Ripulisco lista punti precedentemente creati per l'estrusione dell'anima
            //
            macroPoint.Clear();

            //
            //  ESTRUSIONE ALA
            //
            extrusionDepth = TB + Radius;
            extrusionPlane = Side;

            macroPoint.Add(new ProgramPoint(ParA, SB / 2 + ParG, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParF, SB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA, SB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus);

            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}
