using devDept.Eyeshot.Entities;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using System.Collections.Generic;
using System.Linq;

namespace Ficep.MacroGra
{
    public class INTC29 : EyeMacro
    {
        public INTC29(IWorkPiece wp, ICopeParams param, string macroClassName, string macroName, EyeParam eyeParam, uint lineNumber = 0)
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

            double extrusionDepth = SB;
            string extrusionPlane = "C";

            double rotationAngle = ParALFA.ToRad();
            //
            // Estrusione anima
            //
            macroPoint.Add(new ProgramPoint(ParA - ParE / 2, ParB - ParC / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParE / 2, ParB - ParC / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParD / 2, ParB, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA + ParE / 2, ParB + ParC / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParE / 2, ParB + ParC / 2, 0, 0));
            macroPoint.Add(new ProgramPoint(ParA - ParD / 2, ParB, 0, 0));

            EyeGeometryUtils.AddContourExtrusion(macroPoint, extrusionPlane, extrusionDepth, MirrorInizialeFinale, MirrorSideASideB, MirrorAltoBasso, Wp, TolBrep, TolLinear, TolAngle, TolWebFlange, ref breps, Surplus, 0, rotationAngle);

           // è bruttissimo ma funziona per ora
           if (!rotationAngle.IsEqualTo(0, TolAngle))
            {
                Brep feature = breps[0];
                EyeGeometryUtils.RotateSolid(rotationAngle, ParA, ParB, "C", Wp, ref feature);
                breps[0] = feature;
            }
            ///////////////////////////////
            //      CODA: fissa
            ///////////////////////////////

            Features.AddRange(breps.Select(brep => new EyeFeature(brep)));

            return true;
        }

    }
}