using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class SCAI32 : EyeMacro
    {

        public SCAI32(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
                     : base(wp, param, macroClassName, macroName, eyeParam, lineNumber)
        {
            ProfilesEnabled = "I";
        }

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

            //
            //  Lista dei punti che descrivono il contorno da estrudere
            //
            List<ProgramPoint> macroPoint = new List<ProgramPoint>();
            List<Brep> breps = new List<Brep>();

            //
            // Estrusione anima
            //

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            macroPoint.Add(new ProgramPoint(ParD - ParA / 2, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD + ParA / 2, TB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD + ParA / 2, ParB, 0, ParR));
            macroPoint.Add(new ProgramPoint(ParD - ParA / 2, ParB, 0, ParR));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, base.TolWebFlange, ref breps, Surplus);

            //
            // Estrusione ala
            //

            macroPoint.Clear();

            extrusionDepth = TB + Radius;
            extrusionPlane = Side;

            double offsetY = SB / 2;

            //TolWebFlange è stata aggiunta per far si che i lati del rombo non coincidessero con i lati del solido asportato dall'anima.
            //Permettendo così di asportarli entrambi correttamente.

            macroPoint.Add(new ProgramPoint(ParD - ParA / 2 + TolWebFlange, offsetY - TA / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD - ParA / 2 + TolWebFlange, offsetY + TA / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD, offsetY + ParC, 0, ParS));
            macroPoint.Add(new ProgramPoint(ParD + ParA / 2 - TolWebFlange, offsetY + TA / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD + ParA / 2 - TolWebFlange, offsetY - TA / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParD, offsetY - ParC, 0, ParS));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, base.TolWebFlange, ref breps, Surplus);


            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}