using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.RobServer.Data;
using Ficep.Utils;
using Ficep.RobServer.Utility3D;
using FicepDstvParser;
using System.Collections.Generic;
using System.Linq;
using Region = devDept.Eyeshot.Entities.Region;
using System;

namespace Ficep.RobServer.ImportExport
{
    internal class DstvBrepConverter
    {

        // --- Geometry parameters ---
        private double GEOMETRY_SURPLUS = 1.0;
        private double WEB_FLANGE_CLEARANCE = 0.1; // Horizontal distance from web to flange in order to have the solids on the web not coinciding with the flanges 
        private double FLANGE_SURPLUS; // Surplus used to ensure the flange solids exceed the flange in order to have a clean boolean operation, it is dependenr by the web-flange clearance so it is set in the constructor
        private double FLANGE_WEB_CLEARANCE = 0.1;  // Vertical distance from flange to web cuts
        private double FLANGE_X_TRANSLATION = 0.0; // Small translation to ensure clean boolean operations 
        private double WEB_X_TRANSLATION = 0.1 ; // Larger translation to clear the fillet radius
        private double WEB_X_TRANSLATION_Flange = 0.01; // Larger translation to clear the fillet radius


        // --- Properties ---

        public Brep Solid { get; set; }
        public List<Brep> Scraps { get; set; }
        public double BrepTol { get; private set; }
        public Region FlangeO { get; private set; }
        public Region FlangeU { get; private set; }
        public Region Web {  get; private set; }
        public List<Contour> contours { get; private set; }
        public List<IBrepHole> Holes { get; private set; }

        // --- Private fields ---

        private readonly DSTVParser _parser;
        private readonly IWorkPiece _wp;
        private readonly IProfile _profile;
        private readonly double _tolerance;

        // Calculated geometric properties
        private double _zDown; // Z coordinate of the web lower planar face
        private double _zUp;   // Z coordinate of the web upper planar face
        private double _yU;    // Y coordinate of the boundary between web and flange U
        private double _yO;    // Y coordinate of the boundary between web and flange O

        public DstvBrepConverter(DSTVParser parser, double brepTol, double comparanceTolerance)
        {
            _parser = parser;
            _tolerance = comparanceTolerance;
            BrepTol = brepTol;
            Holes = new List<IBrepHole>();
            contours = new List<Contour>();

            _wp = parser.Wp;
            _profile = parser.Wp.Prf;

            _zDown =_profile.CodePrf == "I"? _profile.SB / 2 - _profile.TA / 2 : 0; // Z coordinate of the web lower planar face
            _zUp = _profile.CodePrf == "I" ? _profile.SB / 2 + _profile.TA / 2 : _profile.TA; // Z coordinate of the web upper planar face
            _yU = _profile.TB; // Y coordinate of the boundary between web and flange U
            _yO = _profile.SA - _profile.TB; // Y coordinate of the boundary between web and flange O

            double exceeding = 0.05;// Exceeding value needed to have the solid subtracted on the flange terminating after the web
            FLANGE_SURPLUS = WEB_FLANGE_CLEARANCE + exceeding;
        }

        // TODO aggiungi la logica che porta i punti e le curve 2d in 3d tutta qui 
        public bool ConvertDstv(out Brep ext, out List<Brep> scraps)
        {
            ext = null;
            scraps = null;
            IWorkPiece _wp = _parser.Wp;

            if (!IsProfileSupported())
                return false;

            ProcessDstvBlocks();
            
            if (!ComputeSolid(out ext, out scraps))
                return false;

            if (ext == null)
                return false;

            //var fillet = ext.Clone() as Brep;

            //if (Fillet(prf, ref fillet))
            //    ext = fillet;

            return true;
        }

        private bool IsProfileSupported()
        {
            if (_profile.CodePrf == "RO" || _profile.CodePrf == "T" || _profile.CodePrf == "C" || _profile.CodePrf == "M"
                || _profile.CodePrf == "Q" || _profile.CodePrf == "P" || _profile.CodePrf == "R")
                return false;

            return true;
        }

        private void ProcessDstvBlocks()
        {
            var dstvBlocks = _parser.Blocks;

            foreach (var dstvBlock in dstvBlocks)
            {
                if (dstvBlock is IContour contour)
                {
                    string name = contour.GetType().Name;

                    Contour c = new Contour(contour.Plane, name, contour.ProgramPoints, contour.ChamferDescriptionList, _wp, _tolerance);
                    contours.Add(c);
                }
                else if (dstvBlock is Bo bo)
                {
                    double planeCoordinate = 0;

                    if (dstvBlock.Plane.Equals("v", System.StringComparison.OrdinalIgnoreCase))
                        planeCoordinate = _zDown;
                    else if (dstvBlock.Plane.Equals("o", System.StringComparison.OrdinalIgnoreCase))
                    {
                        planeCoordinate = _yO;
                    }
                    else if (dstvBlock.Plane.Equals("u", System.StringComparison.OrdinalIgnoreCase))
                    {
                        planeCoordinate = _yU;
                    }

                    foreach (var hole in bo.Holes)
                    {
                        if (hole is DstvHole dstvHole)
                        {
                            if (dstvHole.Depth == 0)
                            {
                                if (dstvBlock.Plane.Equals("v", System.StringComparison.OrdinalIgnoreCase))
                                {
                                    dstvHole.Depth = _wp.Prf.TA;
                                }
                                else if (dstvBlock.Plane.Equals("o", System.StringComparison.OrdinalIgnoreCase) ||
                                         dstvBlock.Plane.Equals("u", System.StringComparison.OrdinalIgnoreCase))
                                {
                                    dstvHole.Depth = _wp.Prf.TB;
                                }
                            }

                            var h = new Hole(dstvHole, planeCoordinate);

                            // If the hole is not created probably it is a marking operation so we just skip it for the moment
                            if (!h.CreateSolid())
                                continue;

                            Holes.Add(h);
                        }
                    }
                }
            }
        }

        private bool CreateContourChamfer(Contour contour, out List<Brep> chamferList)
        {
            chamferList = new List<Brep>();

            if (contour.ChamferDescriptionList.Count == 0)
                return true;

            foreach (var chamferDescription in contour.ChamferDescriptionList)
            {
                int index = chamferDescription.idx;
                double phi1 = chamferDescription.phi1,
                       y1 = chamferDescription.y1,
                       phi2 = chamferDescription.phi2,
                       y2 = chamferDescription.y2;

                if(phi1.IsEqualTo(0, 0.1) && phi2.IsEqualTo(0, 0.1))
                    continue;

                phi1 = phi1.ToRad();
                phi2 = phi2.ToRad();

                string plane = contour.Plane;
                ProgramPoint start = contour.ProgramPoints[index],
                             end = contour.ProgramPoints[index + 1];

                if (plane.Equals("v"))
                {
                    if (phi1 > 0)
                    {
                        if(EyeGeometryUtils.AddInternalChamfer(start, end, _wp, "C", phi1, _wp.Prf.TA - y1, false, false, GEOMETRY_SURPLUS, BrepTol, _tolerance, 0.1, out Brep chamfer, true))
                            chamferList.Add(chamfer);
                    }
                    else
                    {
                        if (EyeGeometryUtils.AddExternalChamfer(start, end, _wp, "C", -phi1, y1, false, false, GEOMETRY_SURPLUS, BrepTol, _tolerance, 0.1, out Brep chamfer, true))
                            chamferList.Add(chamfer);
                    }

                    if (phi2 > 0)
                    {
                        if (EyeGeometryUtils.AddInternalChamfer(start, end, _wp, "C", phi2, _wp.Prf.TA - y2, false, false, GEOMETRY_SURPLUS, BrepTol, _tolerance, 0.1, out Brep chamfer, true))
                            chamferList.Add(chamfer);
                    }
                    else
                    {
                        if (EyeGeometryUtils.AddExternalChamfer(start, end, _wp, "C", -phi2, y2, false, false, GEOMETRY_SURPLUS, BrepTol, _tolerance, 0.1, out Brep chamfer, true))
                            chamferList.Add(chamfer);
                    }
                }
                else if (plane.Equals("o"))
                {
                    start.Y = start.Z;
                    end.Y = end.Z;
                    bool mirrorInizialeFinale = start.X > _wp.Lp / 2;
                    if (phi1 > 0)
                    {
                        if(EyeGeometryUtils.AddExternalChamfer(start, end, _wp, "B", phi1, _wp.Prf.TB - y1, false, false, GEOMETRY_SURPLUS, BrepTol, _tolerance, 0.1, out Brep chamfer, mirrorInizialeFinale))
                            chamferList.Add(chamfer);
                    }
                    else
                    {
                        if(EyeGeometryUtils.AddInternalChamfer(start, end, _wp, "B", -phi1, y1, false, false, GEOMETRY_SURPLUS, BrepTol, _tolerance, 0.1, out Brep chamfer, mirrorInizialeFinale))
                            chamferList.Add(chamfer);
                    }

                    if (phi2 > 0)
                    {
                        if (EyeGeometryUtils.AddExternalChamfer(start, end, _wp, "B", phi2, _wp.Prf.TB - y2, false, false, GEOMETRY_SURPLUS, BrepTol, _tolerance, 0.1, out Brep chamfer, mirrorInizialeFinale))
                            chamferList.Add(chamfer);
                    }
                    else
                    {
                        if (EyeGeometryUtils.AddInternalChamfer(start, end, _wp, "B", -phi2, y2, false, false, GEOMETRY_SURPLUS, BrepTol, _tolerance, 0.1, out Brep chamfer, mirrorInizialeFinale))
                            chamferList.Add(chamfer);
                    }
                }
                else if (plane.Equals("u"))
                {
                    start.Y = start.Z;
                    end.Y = end.Z;
                    bool mirrorInizialeFinale = start.X > _wp.Lp / 2;
                    if (phi1 > 0)
                    {
                        if(EyeGeometryUtils.AddExternalChamfer(start, end, _wp, "A", phi1, _wp.Prf.TB - y1, false, false, GEOMETRY_SURPLUS, BrepTol, _tolerance, 0.1, out Brep chamfer, mirrorInizialeFinale))
                            chamferList.Add(chamfer);
                    }
                    else
                    {
                        if(EyeGeometryUtils.AddInternalChamfer(start, end, _wp, "A", -phi1, y1, false, false, GEOMETRY_SURPLUS, BrepTol, _tolerance, 0.1, out Brep chamfer, mirrorInizialeFinale))
                            chamferList.Add(chamfer);
                    }

                    if (phi2 > 0)
                    {
                        if (EyeGeometryUtils.AddExternalChamfer(start, end, _wp, "A", phi2, _wp.Prf.TB - y2, false, false, GEOMETRY_SURPLUS, BrepTol, _tolerance, 0.1, out Brep chamfer, mirrorInizialeFinale))
                            chamferList.Add(chamfer);
                    }
                    else
                    {
                        if (EyeGeometryUtils.AddInternalChamfer(start, end, _wp, "A", -phi2, y2, false, false, GEOMETRY_SURPLUS, BrepTol, _tolerance, 0.1, out Brep chamfer, mirrorInizialeFinale))
                            chamferList.Add(chamfer);
                    }
                }
                else
                    return false; // Piano non gestito

            }

            return true;
        }

        private bool ComputeSolid(out Brep finalPart, out List<Brep> solidScraps)
        {
            solidScraps = null;
            finalPart = null;

            bool isWithoutErrors = true;

            // Extract the AK and IK contours 
            var usedContours = contours.Where(c => (c.Name == Contour.Type.Ak || c.Name == Contour.Type.Ik));

            // 1. Get contours for each primary surface
            var planeContours = contours.Where(c => c.Name == Contour.Type.Ak && (c.Plane == "v" || c.Plane == "o" || c.Plane == "u"));

            List<(Brep solid, Brep? flangeWeb)> scraps = new List<(Brep solid, Brep? flangeWeb)>();
            // 2. Generate the solids to be subtracted (scraps) from the raw part for each surface
            foreach (var contour in planeContours)
            {
                GetDifferenceSolids(contour, _wp, out List<(Brep solid, Brep? isFlange)> webDifferenceSolids);
                scraps.AddRange(webDifferenceSolids);
            }


            // 3. Create the initial raw part solid
            EyeWorkPiece eyeWorkPiece = new EyeWorkPiece(_wp);
            eyeWorkPiece.CreateSolidRawPart();
            finalPart = eyeWorkPiece.Solid.Clone() as Brep;

            // 4. Subtract all the scraps from the raw part
            Brep flangeWeb = null;
            int i = 0;
            foreach (var item in scraps)
            {
                try
                {
                    finalPart = Brep.Difference(finalPart, item.solid).FirstOrDefault();

                    if (i == 2)
                    {
                        try
                        {
                            finalPart = Brep.Difference(finalPart, flangeWeb).FirstOrDefault();
                        }
                        catch
                        {
                            isWithoutErrors = false;
                            // If the subtraction fails, it means the flange web solid is not needed or already subtracted
                            // so we just skip it.
                        }
                        i = 0; // Reset counter after subtracting the flange web
                        flangeWeb = null; // Reset flange web for the next iteration
                    }
                }
                catch 
                {
                    if (i != 0)
                        isWithoutErrors = false;

                    // If the subtraction fails and the solid is the flange save the flange web solid to be subtracted
                    // after the upperflangesolid and the lowerflangesolid. In order to subtract it after them the counter i 
                    // is used to keep track of the number of flange solids subtracted so far.
                    if (item.flangeWeb != null)
                        flangeWeb = item.flangeWeb;
                }

                if (flangeWeb != null)
                    i++;

            }
            solidScraps = scraps.Select(s => s.solid).ToList();

            Solid = finalPart;

            if (Solid != null)
            {
                // 5. Apply internal cuts (IK contours)
                if (!GetSolidWithInternals(_wp, contours.Where(c => c.Plane == "v" && c.Name == Contour.Type.Ik).ToList(), out finalPart))
                    return false;

                if (finalPart != null)
                    Solid = finalPart;

                // 6. Apply holes
                if (!GetSolidWithHoles(out finalPart))
                    return false;

                if (finalPart != null)
                    Solid = finalPart;
            }

            return isWithoutErrors;
        }

        //
        // Pipeline:
        // - Prendo il contour del piano descritto dal dstv, lo allargo di surplus nei punti lungo i confini del piano (x = 0, y = width, y = 0, x = lp)
        // - Genero il parallelepipedo del bounding box del piano allargato di surplus nelle 3 direzioni 
        // - Genero il solido partendo dal contour allargato
        // - Sottraggo al parallelepiedo il solido generato dalla descrizione del dstv così da ottenere i difference solids che andranno poi sottratti al grezzo per ottenere il pezzo finito
        // allargando il contour descritto dal dstv nei punti che giacciono sui confini del piano mi permette poi di sottrarre il solido ottenuto dal parallelepipedo
        // e ottenere così i difference solids più lunghi di surplus rispetto al pezzo grezzo in modo da non far degenerare eyeshot
        //
        private bool GetDifferenceSolids(Contour contour, IWorkPiece wp, out List<(Brep solid, Brep? flangeWeb)> differenceSolids)
        {
            differenceSolids = new List<(Brep solid, Brep? isFlange)>();

            double tolContour = 0.1; // Tolleranza per allargare il contour
            // Sposta di surplus i program points del dstv sui confini del piano  
            if (!EnlargeContour(contour, wp, tolContour, 3 * GEOMETRY_SURPLUS, out Contour enlargedContour))
                return false;

            Region r = new Region(enlargedContour.Curve);
            
            // Determine extrusion amount and translation vectors based on the plane
            var (planeBoundingBox, extrusionAmount, translation) = CreatePlaneSpecificGeometry(enlargedContour);

            Brep dstvSolid = r.ExtrudeAsBrep(extrusionAmount);
            dstvSolid.Translate(translation);

            // Ottengo la lista degli scraps
            var diffSolids = Brep.Difference(planeBoundingBox, dstvSolid)?.ToList();

            if (diffSolids is null)
                return false;

            // Creo i cianfrini 
            CreateContourChamfer(contour, out List<Brep> chamferSolids);

            // Post-process scraps to correctly handle flange/web intersections
            // Se gli scaps appartengono alle ali vanno tagliati
            // faccio un taglio verticale e uso il solido per rimouvere l'ala, poi il pezzo rimanente del taglio che parte da fine ala e arriva a
            // tb + radius lo taglio orizzontalmente per ottenere i pezzi da sottrarre al di sopra dell'anima e al di sotto dell'anima
            if (contour.Plane != "v")
            {
                differenceSolids.AddRange(EyeUtils.ProcessFlangeScraps(diffSolids, contour.Plane, false, _wp.Lp, _yU, _yO, _zUp, _zDown, FLANGE_SURPLUS, FLANGE_WEB_CLEARANCE, FLANGE_X_TRANSLATION, WEB_X_TRANSLATION_Flange, _tolerance));
            }
            else
            {
                differenceSolids.AddRange(ProcessWebScraps(diffSolids).Select(b => (b, (Brep?)null)));
                // Cut web chamfers to avoid intersecting with flanges
                CutWebChamfers(chamferSolids);
            }

            differenceSolids.AddRange(chamferSolids.Select(b => (b, (Brep?)null)));

            return true;
        }

        private bool EnlargeContour(Contour contour, IWorkPiece wp, double tolerance, double surplus, out Contour enlargedContour)
        {
            enlargedContour = null;
            List<ProgramPoint> pointWithSurplus,
                               temp = contour.ProgramPoints.Select(p => p.Clone() as ProgramPoint).SkipLast(1).ToList();

            if (contour.Plane == "o" || contour.Plane == "u")
            {
                EyeGeometryUtils.ApplyContourSurplus(temp, wp.Prf.SB, wp.Lp, surplus, tolerance, true, out pointWithSurplus);
            }
            else if (contour.Plane == "v")
            {
                EyeGeometryUtils.ApplyContourSurplus(temp, wp.Prf.SB, wp.Lp, surplus, tolerance, false, out pointWithSurplus);
            }
            else // al momento non sono gestiti altri piani 
                return false;

            // Faccio diventare il primo e l'ultimo uguali
            //pointWithSurplus[pointWithSurplus.Count - 1] = pointWithSurplus[0];
            pointWithSurplus.Add(pointWithSurplus[0].Clone() as ProgramPoint);

            enlargedContour = new Contour(contour.Plane, "Ak", pointWithSurplus, contour.ChamferDescriptionList, wp, tolerance);

            return true;
        }

        /// <summary>
        /// Creates the bounding box and determines extrusion parameters based on the contour's plane.
        /// </summary>
        private (Brep BoundingBox, double ExtrusionAmount, Vector3D Translation) CreatePlaneSpecificGeometry(Contour contour)
        {
            var isFlange = contour.Plane == "u" || contour.Plane == "o";
            var regionPlane = new Region(contour.Curve).Plane;

            if (isFlange)
            {
                var amount = _profile.TB + _profile.Radius + GEOMETRY_SURPLUS;
                var boundingBox = Region.CreateRectangle(Plane.XZ, _wp.Lp + 2 * GEOMETRY_SURPLUS, _profile.SB + 2 * GEOMETRY_SURPLUS).ExtrudeAsBrep(-amount);

                // Extrude positively along the Y-axis regardless of the region's normal
                var extrusionAmount = regionPlane.Equation.Y < 0 ? -amount * 2 : amount * 2;

                if (contour.Plane == "u")
                {
                    boundingBox.Translate(-GEOMETRY_SURPLUS, -GEOMETRY_SURPLUS, -GEOMETRY_SURPLUS);
                    var translation = new Vector3D(0, -_profile.TB - Math.Abs(extrusionAmount) / 3, 0);
                    return (boundingBox, extrusionAmount, translation);
                }
                else // Plane "o"
                {
                    boundingBox.Translate(-GEOMETRY_SURPLUS, _yO - _profile.Radius, -GEOMETRY_SURPLUS);
                    var translation = new Vector3D(0, -Math.Abs(extrusionAmount) / 3, 0);
                    return (boundingBox, extrusionAmount, translation);
                }
            }
            else // Plane "v"
            {
                var amount = _profile.TA + 2 * _profile.Radius;
                var boundingBox = Region.CreateRectangle(Plane.XY, _wp.Lp + 2 * GEOMETRY_SURPLUS, _profile.SA - 2 * _profile.TB - 2 * WEB_FLANGE_CLEARANCE).ExtrudeAsBrep(amount);
                boundingBox.Translate(-GEOMETRY_SURPLUS, _profile.TB + WEB_FLANGE_CLEARANCE, _zDown - _profile.Radius);

                // Extrude positively along the Z-axis regardless of the region's normal
                var extrusionAmount = regionPlane.Equation.Z < 0 ? -amount * 2 : amount * 2;
                var translation = new Vector3D(0, 0, -_profile.Radius - Math.Abs(extrusionAmount) / 3);
                return (boundingBox, extrusionAmount, translation);
            }
        }

        /// <summary>
        /// Translates web scrap solids at the beam ends to ensure clean boolean operations.
        /// </summary>
        private IEnumerable<Brep> ProcessWebScraps(List<Brep> scraps)
        {
            var processedScraps = new List<Brep>();
            foreach (var scrap in scraps)
            {
                // Test to check if the web fillet is needed
                if (false)
                {
                    var webFilletUpper = (Brep)scrap.Clone();
                    var webFilletLower = (Brep)scrap.Clone();
                    var web = (Brep)scrap.Clone();

                    var upperHorizontalPlane = new Plane(new Point3D(0, 0, _zUp + 1), Vector3D.AxisZ);
                    var lowerHorizontalPlane = new Plane(new Point3D(0, 0, _zDown - 1), Vector3D.AxisZ);

                    web.CutBy(upperHorizontalPlane, false); // Cut the upper part of the flange solid
                    web.CutBy(lowerHorizontalPlane, true); // Cut the lower part of the flange solid


                    var verticalPlane = Plane.XZ;
                    verticalPlane.Translate(0, _profile.Radius + _profile.TB);
                    // 2. Isolate the part that cuts above the web.
                    webFilletUpper.CutBy(upperHorizontalPlane, true);
                    webFilletUpper.CutBy(verticalPlane, true); // Ensure the fillet does not extend into the flange area
                                                               // 3. Isolate the part that cuts below the web.
                    webFilletLower.CutBy(lowerHorizontalPlane, false);

                    EyeUtils.TranslateEndPiece(web, WEB_X_TRANSLATION, _wp.Lp);
                    EyeUtils.TranslateEndPiece(webFilletUpper, WEB_X_TRANSLATION, _wp.Lp);
                    EyeUtils.TranslateEndPiece(webFilletLower, WEB_X_TRANSLATION, _wp.Lp);

                    processedScraps.Add(web);
                    //processedScraps.Add(webFilletUpper);
                    //processedScraps.Add(webFilletLower);
                }
                else
                {
                    EyeUtils.TranslateEndPiece(scrap, WEB_X_TRANSLATION, _wp.Lp);
                    processedScraps.Add(scrap);
                }
                
            }

            return processedScraps;
        }

        

        /// <summary>
        /// Trims web chamfer solids to prevent them from extending into flange areas.
        /// </summary>
        private void CutWebChamfers(List<Brep> chamferSolids)
        {
            var cutPlane1 = new Plane(new Point3D(0, _yU + WEB_FLANGE_CLEARANCE, 0), Vector3D.AxisMinusY);
            var cutPlane2 = new Plane(new Point3D(0, _yO - WEB_FLANGE_CLEARANCE, 0), Vector3D.AxisY);

            foreach (var chamfer in chamferSolids)
            {
                chamfer.CutBy(cutPlane1);
                chamfer.CutBy(cutPlane2);
            }
        }

        private bool GetSolidWithInternals(IWorkPiece  wp, List<Contour> contours, out Brep solid)
        {
            solid = Solid.Clone() as Brep;

            foreach (var contour in contours)
            {

                if (contour == null)
                    continue;

                var region = new Region(contour.Curve);
                Brep feature = null;

                if (contour.Plane == "v")
                {
                    if (region.Plane.Origin.Z.IsEqualTo(_zDown, _tolerance) && region.Plane.Equation.Z.IsEqualTo(-1, _tolerance))
                    {
                        feature = region.ExtrudeAsBrep(-wp.Prf.TA - 1);
                    }
                    else if (region.Plane.Origin.Z.IsEqualTo(_zDown, _tolerance) && region.Plane.Equation.Z.IsEqualTo(1, _tolerance))
                    {
                        feature = region.ExtrudeAsBrep(wp.Prf.TA + 1);
                    }
                }
                try
                {
                    solid = Brep.Difference(solid, feature)?.FirstOrDefault();
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        private bool GetSolidWithHoles(out Brep solid)
        {
            solid = Solid.Clone() as Brep;

            foreach (var hole in Holes)
            {
                try
                {
                    solid = Brep.Difference(solid, hole.Solid)?.FirstOrDefault();
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        private bool Fillet(IProfile prf, ref Brep solid)
        {
            bool success = true;

            for (int i = 0; i < solid.Edges.Length; i++)
            {
                var edge = solid.Edges[i];


                Point3D[] points = new Point3D[]{
                                                 new Point3D(0, _yU, _zDown), // Right down corner
                                                 new Point3D(0, _yO, _zDown), // Left down corner
                                                 new Point3D(0, _yU, _zUp), // Right up corner
                                                 new Point3D(0, _yO, _zUp) // Left up corner
                                                }; 

                    Point3D s = new Point3D(0, edge.Curve.StartPoint.Y, edge.Curve.StartPoint.Z),
                        e = new Point3D(0, edge.Curve.EndPoint.Y, edge.Curve.EndPoint.Z);

                // Get the point in the middle of the edge curve and check if it is inside the flange ragion
                bool isPointInRegion = false;
                Point3D midPoint = edge.Curve.PointAt(edge.Curve.Domain.Mid);
                
                if (s.Y < prf.SA / 2)
                {
                    isPointInRegion = FlangeU.IsPointInside(midPoint);
                }
                else
                {
                    isPointInRegion = FlangeO.IsPointInside(midPoint);
                }

                // If it is not inside skip the edge 
                if (!isPointInRegion)
                    continue;

                // If true check if the projection of start and end points is equal to one of the corners needed to be filled
                foreach ( var p in points )
                {
                    if (EyeUtils.ArePointsEqual(p, s, _tolerance) && EyeUtils.ArePointsEqual(p, e, _tolerance))
                    {
                        try
                        {
                            if (solid.Fillet(i, prf.Radius, 0.0001) != ssiFailureType.Success)
                                return false;
                        }
                        catch
                        {
                            success = false;
                            continue;
                        }
                    }
                }
            }

            return success;
        }
    }

    class Contour : IContour
    {
        public enum Type { Ak, Ik, Bo };
        public List<ProgramPoint> ProgramPoints { get; private set; }

        public string Plane { get; private set; }

        public CompositeCurve Curve { get; private set; }

        public Type Name { get; private set; }

        public List<(int idx, double phi1, double y1, double phi2, double y2)> ChamferDescriptionList { get; private set; }

        public Contour(string plane, string contourName, List<ProgramPoint> programPoints, List<(int idx, double phi1, double y1, double phi2, double y2)> chamferDescription, IWorkPiece wp, double tolerance)
        {
            Plane = plane;
            ProgramPoints = programPoints;
            ChamferDescriptionList = chamferDescription;
            SetType(contourName);

            if (!CreateContourCurve(wp, tolerance))
                return;
        }

        private bool CreateContourCurve(IWorkPiece wp, double tolerance)
        {
            if (Name == Type.Ak && Plane == "v")
            {
                var points = ProgramPoints;

                // Trim web boundaries at the web/flange boundary 
                //if (!EyeUtils.TrimWebBoundaries(ref points, wp, tolerance))
                //    return false;
            }

            if (!EyeUtils.CreateFaceCurves(ProgramPoints, tolerance, wp, out List<ICurve> contourCurves, Plane))
                    return false;

            try
            {
                Curve = new CompositeCurve(contourCurves);

                if (Name == Type.Ik)
                    Curve.Reverse();
            }
            catch
            {
                return false;
            }

            return true;
        }

        private bool SetType(string name)
        {
            var upName = name.ToUpper();

            if (upName == "AK")
                Name = Type.Ak;
            else if (upName == "IK")
                Name= Type.Ik;
            else if (upName == "BO")
                Name = Type.Bo;
            else 
                return false;

            return true;
        }
    }

    class Hole : IBrepHole
    {
        public DstvHole HoleDescription { get; set; }
        public ICurve Contour { get; set; }
        public Brep Solid { get; set; }

        private double _planeCoordinate;

        public Hole(DstvHole dstvHole, double planeCoordinate)
        {
            HoleDescription = dstvHole;
            _planeCoordinate = planeCoordinate;
        }

        public bool CreateSolid()
        {
            double radius = HoleDescription.D / 2,
                   xc = HoleDescription.Xc,
                   yc = HoleDescription.Yc;

            if (radius.IsEqualTo(0, 0.01))
                return false;

            bool vPlane = HoleDescription.Plane.Equals("v", System.StringComparison.OrdinalIgnoreCase),
                 uPlane = HoleDescription.Plane.Equals("u", System.StringComparison.OrdinalIgnoreCase),
                 oPlane = HoleDescription.Plane.Equals("o", System.StringComparison.OrdinalIgnoreCase);

            if (vPlane)
                Contour = new Circle(xc, yc, _planeCoordinate, radius);
            else
                Contour = new Circle(Plane.XZ, new Point3D(xc, _planeCoordinate, yc), radius);

            Region c = new Region(Contour);

            if (vPlane && c.Plane.Equation.Z == 1)
                Solid = c.ExtrudeAsBrep(HoleDescription.Depth);
            else if (vPlane && c.Plane.Equation.Z == -1)
                Solid = c.ExtrudeAsBrep(-HoleDescription.Depth);
            else if (uPlane && c.Plane.Equation.Y == 1)
                Solid = c.ExtrudeAsBrep(-HoleDescription.Depth);
            else if (uPlane && c.Plane.Equation.Y == -1)
                Solid = c.ExtrudeAsBrep(HoleDescription.Depth);
            else if (oPlane && c.Plane.Equation.Y == 1)
                Solid = c.ExtrudeAsBrep(HoleDescription.Depth);
            else if (oPlane && c.Plane.Equation.Y == -1)
                Solid = c.ExtrudeAsBrep(-HoleDescription.Depth);

            return true;
        }
    }

    public interface IBrepHole
    {
        DstvHole HoleDescription { get; set; }
        ICurve Contour { get; set; }
        Brep Solid { get; set; }
    }
}
