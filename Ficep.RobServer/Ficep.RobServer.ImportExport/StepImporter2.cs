using devDept.Eyeshot;
using devDept.Eyeshot.Entities;
using devDept.Geometry;
using Ficep.AnyCut.Mathematics;
using Ficep.RobServer.Utility3D;
using Ficep.Utils;
using System;
using System.Linq;
using Vector3D = devDept.Geometry.Vector3D;

namespace Ficep.RobServer.ImportExport
{
    public partial class StepImporter
    {
        static bool newMethod = false;

        //
        //	Verifica se il punto si trova all'interno della sezione YZ del Solid
        //
        public static bool IsPointInsideTheSolid(Point3D point, Brep solid)
        {
            if (solid == null || point == null)
                return false;

            //  Verifico se il punto è interno al solid o sulla sua superficie
            pointStatusType statusType = solid.IsPointInside(point);
            if (statusType == pointStatusType.Inside || statusType == pointStatusType.Onto)
                return true;

            return false;
        }

        //
        //	Verifica se il centro del BBOX si trova all'interno della sezione YZ del Solid
        //
        public static bool IsBBOXCenterInsideTheSolid(Brep solid)
        {
            if (solid == null)
                return false;

            //  Centro del BBOX
            Point3D center = (solid.BoxMin + solid.BoxMax) / 2;

            //  Verifico se il center è interno al solid o sulla sua superficie
            pointStatusType statusType = solid.IsPointInside(center);
            if (statusType == pointStatusType.Inside || statusType == pointStatusType.Onto)
                return true;

            center.X = (solid.BoxMin.X + solid.BoxMax.X) / 4;
            //  Verifico se il center shiftato in X è interno al solid o sulla sua superficie
            statusType = solid.IsPointInside(center);
            if (statusType == pointStatusType.Inside || statusType == pointStatusType.Onto)
                return true;

            center.X = (solid.BoxMin.X + solid.BoxMax.X) / 2;
            center.Y = (solid.BoxMin.Y + solid.BoxMax.Y) / 4;
            //  Verifico se il center shiftato in X è interno al solid o sulla sua superficie
            statusType = solid.IsPointInside(center);
            if (statusType == pointStatusType.Inside || statusType == pointStatusType.Onto)
                return true;

            return false;
        }

        //
        //	Verifica se Z = z è un piano orizzontale del profilo
        //
        public static bool HasHorizontalPlaneSolid(Brep solid, double z)
        {
            if (solid == null)
                return false;

            double boxYDim = solid.BoxSize.Y;

            double toll = 0.1;

            bool exists = solid.Edges.Any(e =>
                Math.Abs(e.Curve.StartPoint.Z - z) < toll &&
                Math.Abs(e.Curve.EndPoint.Z - z) < toll &&
                Math.Abs(e.Curve.StartPoint.Y - e.Curve.EndPoint.Y) >= boxYDim / 4);

            return exists;
        }

        //
        //	Verifica se Y = y è un piano verticale del profilo
        //
        public static bool HasVerticalPlaneSolid(Brep solid, double y)
        {
            if (solid == null)
                return false;

            double boxZDim = solid.BoxSize.Z;

            double toll = 0.1;

            bool exists = solid.Edges.Any(e =>
                Math.Abs(e.Curve.StartPoint.Y - y) < toll &&
                Math.Abs(e.Curve.EndPoint.Y - y) < toll &&
                Math.Abs(e.Curve.StartPoint.Z - e.Curve.EndPoint.Z) >= boxZDim / 4);

            return exists;
        }

        public static bool GetProfileInformations2(ref Brep solid, out EyeWorkPiece wp)
        {
            wp = null;

            if (solid == null)
                return false;

            //
            //  Valori Min e Max dei punti degli edges
            //
            double edgesMinX = solid.BoxMin.X, edgesMaxX = solid.BoxMax.X,
                   edgesMinY = solid.BoxMin.Y, edgesMaxY = solid.BoxMax.Y,
                   edgesMinZ = solid.BoxMin.Z, edgesMaxZ = solid.BoxMax.Z;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //
            //	MAPPATURA AUTOMATICA ASSI XYZ
            //	
            //	Voglio calcolare in automatico come ruotare il profilo in modo da
            //	mapparlo sul frame di lavoro corretto. Faccio delle ipotesi semplificative:
            //	
            //	Ipotesi semplificata: la direzione X deve coincidere con quella della massima dimensione profilo (longitudinale)
            //	Ipotesi semplificata: la direzione Y deve coincidere con quella della massima dimensione della sezione YZ
            //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            double deltaX = edgesMaxX - edgesMinX,
                deltaY = edgesMaxY - edgesMinY,
                deltaZ = edgesMaxZ - edgesMinZ;
            double offsetX = edgesMinX, offsetY = edgesMinY, offsetZ = edgesMinZ;

            //
            //  INCH / MM
            //
            //	Se le dimensioni sono minori della minima dimensione in mm, ipotizzo che i punti
            //	siano in inch
            //
            double minSectionDimension = 20, scale = 1;
            if (deltaX < minSectionDimension || deltaY < minSectionDimension || deltaZ < minSectionDimension)
                scale = 25.4;

            //
            //	Determino la coordinata con massima escursione BBOX
            //
            bool maxOnX = false, maxOnY = false, maxOnZ = false;
            if (deltaX >= deltaY)
            {
                if (deltaX >= deltaZ)
                    maxOnX = true;
                else
                    maxOnZ = true;
            }
            else if (deltaY >= deltaZ)
                maxOnY = true;
            else
                maxOnZ = true;

            //
            //  Ruoto il profilo in modo da:
            //
            //  -   riportare la dimensione maggiore sull'asse X
            //  -   riportare la seconda dimensione sull'asse Y
            //
            if (maxOnX)
            {
                if (edgesMaxY - edgesMinY < edgesMaxZ - edgesMinZ)
                    solid.Rotate(-Math.PI / 2, Vector3D.AxisX);
            }
            else if (maxOnY)
            {
                solid.Rotate(-Math.PI / 2, Vector3D.AxisZ);

                edgesMinX = solid.BoxMin.X;
                edgesMaxX = solid.BoxMax.X;
                edgesMinY = solid.BoxMin.Y;
                edgesMaxY = solid.BoxMax.Y;
                edgesMinZ = solid.BoxMin.Z;
                edgesMaxZ = solid.BoxMax.Z;

                if (edgesMaxY - edgesMinY < edgesMaxZ - edgesMinZ)
                    solid.Rotate(-Math.PI / 2, Vector3D.AxisX);
            }
            else if (maxOnZ)
            {
                solid.Rotate(-Math.PI / 2, Vector3D.AxisY);

                edgesMinX = solid.BoxMin.X;
                edgesMaxX = solid.BoxMax.X;
                edgesMinY = solid.BoxMin.Y;
                edgesMaxY = solid.BoxMax.Y;
                edgesMinZ = solid.BoxMin.Z;
                edgesMaxZ = solid.BoxMax.Z;

                if (edgesMaxY - edgesMinY < edgesMaxZ - edgesMinZ)
                    solid.Rotate(-Math.PI / 2, Vector3D.AxisX);
            }

            //
            //  Traslo il profilo sull'origine (0, 0, 0)
            //
            edgesMinX = solid.BoxMin.X;
            edgesMaxX = solid.BoxMax.X;
            edgesMinY = solid.BoxMin.Y;
            edgesMaxY = solid.BoxMax.Y;
            edgesMinZ = solid.BoxMin.Z;
            edgesMaxZ = solid.BoxMax.Z;

            solid.Translate(-edgesMinX, -edgesMinY, -edgesMinZ);

            //
            //  Scalo le dimensioni nel caso di inches in modo da riportarle a mm
            //
            if (scale > 0 && !FMath.Equal(scale, 1))
            {
                Point3D origin = new Point3D(0, 0, 0);

                solid.Scale(origin, scale);

                TraversalParams tp = new TraversalParams();
                solid.UpdateBoundingBox(tp);

                edgesMinX = solid.BoxMin.X;
                edgesMaxX = solid.BoxMax.X;
                edgesMinY = solid.BoxMin.Y;
                edgesMaxY = solid.BoxMax.Y;
                edgesMinZ = solid.BoxMin.Z;
                edgesMaxZ = solid.BoxMax.Z;
            }

            //
            //	I valori di HA, HB, HC vengono dedotti direttamente dal BBOX
            //
            double ha = edgesMaxZ - edgesMinZ, hb = ha;
            double hc = edgesMaxY - edgesMinY;
            double ta = 0, tb = 0, tc = 0;

            //
            //	Calcolo dimensioni profilo.
            //
            double toll = 0.5;
            double edgeLen = 0;
            double yMinMaxZOrizontalEdge = 0, yMaxMaxZOrizontalEdge = 0, zMinPseudoOrizontalEdge = edgesMaxZ;
            double minAllowedTA = 2, minAllowedTB = 2, minVerticalEdgeLen = Math.Min (20, (edgesMaxZ - edgesMinZ) / 4), minChangeZPseudoVerticalEdge = 5,
                maxAngPseudoVerticalEdge = 10, maxChangeZPseudoOrizontalEdge = 5, minChangeYMediumYCenteredEdge = 5;
            bool verticalEdge = false, horizontalEdge = false, longYEdge = false, mediumYCenteredEdge = false,
                pseudoHorizontalEdge = false, pseudoVerticalEdge = false;
            bool okTA = false, okTB = false, okTC = false;
            bool okZTopWeb = false, okZBottomWeb = false;

            double zTopWeb = 0, zBottomWeb = 0, minZOrizontalEdge = edgesMaxZ, maxZOrizontalEdge = edgesMinZ;
            double pseudoTA = 0, pseudoTB = 0;
            //	Campi per identificare un profilo tubo tondo
            double bboxYCenter = (edgesMaxY - edgesMinY) / 2, bboxZCenter = (edgesMaxZ - edgesMinZ) / 2;
            bool squareBBOX = (edgesMaxY - edgesMinY).IsEqualTo(edgesMaxZ - edgesMinZ, 0.1);
            double bboxRadius = squareBBOX ? (edgesMaxY - edgesMinY) / 2 : 0;
            bool isPrfofileR = false, isPrfofileF = false;

            //
            //  Nella identificazione del tipo di profilo è importante:
            //
            //  -   sapere se il centro del BBOX è interno al profilo (vale solo per I e F)
            //  -   sapere se il profilo ha 2 piani verticali a 0 e HC (vale solo per I e Q)
            //  -   sapere se il profilo ha un piano orizzontale a 0 (vale solo per LQF e U orientati verso l'alto)
            //  -   sapere se il profilo ha un piano orizzontale a HA (vale solo per QF e U orientati verso il basso)
            //
            bool isBBOXCenterInside = IsBBOXCenterInsideTheSolid(solid),
                hasBottomHorizontalPlane = HasHorizontalPlaneSolid(solid, 0),
                hasTopHorizontalPlane = HasHorizontalPlaneSolid(solid, ha),
                hasBottomVerticalPlane = HasVerticalPlaneSolid(solid, 0),
                hasTopVerticalPlane = HasVerticalPlaneSolid(solid, hc);

            //
            //	Il profilo F è l'unico che ha un piano orizzontale a Z = 0, un piano orizzontale a Z = HA e
            //	il baricentro del BBOX all'interno della sezione profilo
            //
            isPrfofileF = isBBOXCenterInside && hasBottomHorizontalPlane && hasTopHorizontalPlane;

            //
            //	Profili IULQFR
            //	
            //	TA:	viene calcolata cercando un Edge verticale (o pseudoverticale nel caso di profilo U con ala
            //		interna non verticale) con la y massima all'interno del semivolume y < HC / 2
            //	TB:	viene calcolata cercando un Edge verticale (o pseudoverticale nel caso di profilo U con ala
            //		interna non verticale) con la y minima all'interno del semivolume y > HC / 2
            //	TC:	viene calcolata andando a identificarei 2 Edge orizzontali suff. lunghi, con Z minima e massima
            //		rispettivamente, ma distinti dai piani ala inferiore/superiore
            //
            //	Profilo R
            // 
            //	TA/TB/TC:	vengono calcolati identificando l'Edge pseudoorizzontale a cavallo di y = HC / 2
            //		con la y minima ma > 0
            //
            foreach (var edge in solid.Edges)
            {
                //
                //	p1, p2 sono gli estremi dell'EDGE corrente
                //
                Point3D p1 = edge.Curve.StartPoint,
                    p2 = edge.Curve.EndPoint;

                edgeLen = p1.DistanceTo(p2);
                verticalEdge = p1.Y.IsEqualTo(p2.Y, 0.01);

                if (Math.Abs(p1.Z - p2.Z) > minChangeZPseudoVerticalEdge)
                    pseudoVerticalEdge = Math.Abs(Math.Atan(p1.Y - p2.Y) / (p1.Z - p2.Z)) / FMath.FAT_RAD < maxAngPseudoVerticalEdge;
                else
                    pseudoVerticalEdge = false;
                horizontalEdge = FMath.Equal(p1.Z, p2.Z, 0.01);
                longYEdge = Math.Abs(p1.Y - p2.Y) > (edgesMaxY - edgesMinY) / 4;
                mediumYCenteredEdge = Math.Abs(p1.Y - p2.Y) > minChangeYMediumYCenteredEdge && p1.Y > 0.25 * (edgesMaxY - edgesMinY) / 4 && p1.Y < 0.75 * (edgesMaxY - edgesMinY);
                pseudoHorizontalEdge = FMath.Equal(p1.Z, p2.Z, maxChangeZPseudoOrizontalEdge);

                if (horizontalEdge)
                {
                    //	Calcolo quote Z piani orizzontali considerando tratti orizzontali lunghi
                    //	oppure di media lunghezza ma lontano dalle ali, in modo da evitare
                    //	gli EDGE orizzontali su TOP/BOTTOM ali
                    if (longYEdge || mediumYCenteredEdge)
                    {
                        if (!isBBOXCenterInside && p1.Z >= 0 || isBBOXCenterInside && p1.Z > 0.25 * ha)
                        {
                            if (okZBottomWeb)
                                zBottomWeb = Math.Min(zBottomWeb, p1.Z);
                            else
                                zBottomWeb = p1.Z;

                            okZBottomWeb = true;

                            minZOrizontalEdge = Math.Min(minZOrizontalEdge, p1.Z);
                        }

                        if (p1.Z > 0 && (!isBBOXCenterInside || isBBOXCenterInside && p1.Z < 0.75 * ha) && p1.Z >= zTopWeb - 1)
                        {
                            zTopWeb = p1.Z;
                            okZTopWeb = true;

                            if (p1.Z > maxZOrizontalEdge)
                            {
                                maxZOrizontalEdge = Math.Max(maxZOrizontalEdge, p1.Z);
                                yMinMaxZOrizontalEdge = Math.Min(p1.Y, p2.Y);
                                yMaxMaxZOrizontalEdge = Math.Max(p1.Y, p2.Y);
                            }
                            else if (FMath.Equal(p1.Z, maxZOrizontalEdge, 0.1))
                            {
                                //	Qui entra se p1.Z = MaxZOrizontalEdge
                                yMinMaxZOrizontalEdge = Math.Min(Math.Min(p1.Y, p2.Y), yMinMaxZOrizontalEdge);
                                yMaxMaxZOrizontalEdge = Math.Max(Math.Max(p1.Y, p2.Y), yMaxMaxZOrizontalEdge);
                            }
                        }

                        if (okZTopWeb && okZBottomWeb)
                        {
                            tc = zTopWeb - zBottomWeb;
                            okTC = true;
                        }
                    }
                }
                else if (verticalEdge)
                {
                    if (edgeLen > minVerticalEdgeLen)
                    {
                        if (p1.Y > minAllowedTA && p1.Y < (edgesMaxY - edgesMinY) / 2 - minAllowedTA)
                        {
                            //	Ricerco un EDGE verticale con y < (EdgesMaxY - EdgesMinY) / 2)
                            ta = Math.Max(ta, p1.Y);
                            okTA = true;
                        }
                        else if (p1.Y < edgesMaxY - minAllowedTB && p1.Y > (edgesMaxY - edgesMinY) / 2 + minAllowedTB)
                        {
                            //	Ricerco un EDGE verticale con y > (EdgesMaxY - EdgesMinY) / 2)
                            tb = Math.Max(tb, edgesMaxY - p1.Y);
                            okTB = true;
                        }
                    }
                }
                else if (pseudoVerticalEdge)
                {
                    if (p1.Y > minAllowedTA && p1.Y < (edgesMaxY - edgesMinY) / 2 - minAllowedTA)
                    {
                        //	Ricerco un EDGE verticale con y < (EdgesMaxY - EdgesMinY) / 2)
                        pseudoTA = Math.Max(pseudoTA, p1.Y);
                    }
                    else if (p1.Y < edgesMaxY - minAllowedTB && p1.Y > (edgesMaxY - edgesMinY) / 2 + minAllowedTB)
                    {
                        //	Ricerco un EDGE verticale con y > (EdgesMaxY - EdgesMinY) / 2)
                        pseudoTB = Math.Max(pseudoTB, edgesMaxY - p1.Y);
                    }
                }
                else if (pseudoHorizontalEdge)
                {
                    //	Considero Edges pseudo-orizzontali a cavallo di HC / 2
                    if (p1.Y <= hc / 2 && p2.Y >= hc / 2 || p1.Y >= hc / 2 && p2.Y <= hc / 2)
                    {
                        if (Math.Min(p1.Z, p2.Z) > 1.0f)
                            zMinPseudoOrizontalEdge = Math.Min(zMinPseudoOrizontalEdge, Math.Min(p1.Z, p2.Z));
                    }
                }

                //
                //	Se entrambi i punti di un EDGE, che non è orizzontale e non è verticale (quindi è un raggio),
                //	hanno distanza dal centro di un BBOX quadrato pari a metà della sezione del BBOX,
                //	per forza si deve trattare di un tubo tondo
                //
                if (!horizontalEdge && !verticalEdge && squareBBOX)
                {
                    if (FMath.Equal(FMath.Distance(p1.Y, p1.Z, 0, bboxYCenter, bboxZCenter, 0), bboxRadius) &&
                        FMath.Equal(FMath.Distance(p2.Y, p2.Z, 0, bboxYCenter, bboxZCenter, 0), bboxRadius))
                        isPrfofileR = true;
                }
            }

            //	Calcolo TC solo dopo la lettura di tutti gli Edges
            if (!okTC && okZTopWeb)
            {
                tc = zTopWeb - zBottomWeb;
                okTC = true;
            }

            if (hasBottomVerticalPlane && hasTopVerticalPlane)
            {
                ta = tb = Math.Max(ta, tb);
                pseudoTA = pseudoTB = Math.Max(pseudoTA, pseudoTB);
            }

            //
            //	I test per l'identificazione dei profili vanno eseguiti esattamente in questo ordine (NON SPOSTARE !!!!)
            // 
            //	1)	R	-	BBOX quadrato
            //			-	Esiste almeno un Edge obliquo con distanza dal centro del BBOX pari a metà larghezza BBOX
            //	2)	F	-	Ha un piano orizzontale a Z = 0
            //			-	Ha un piano orizzontale a Z = HA
            //			-	Ha il centro del BBOX all'interno della sezione profilo
            //	3)	I	-	Ho entrambe le ali verticali (TA > 0, TB > 0)
            //			-	Il centro BBOX è contenuto nella sezione del profilo
            //	4)	Q	-	Ho entrambe le ali verticali (TA > 0, TB > 0)
            //			-	Il centro BBOX non è contenuto nella sezione del profilo (differenza rispetto al profilo I)
            //			-	Ho 2 piani orizzontali a 0 e HA
            //	5)	U	-	Ho entrambe le ali verticali (TA > 0, TB > 0)
            //			-	Il centro BBOX non è contenuto nella sezione del profilo
            //			-	Ho 1 solo piano orizzontale a 0 o HA (differenza rispetto al profilo Q)
            //	6)	L	-	Ho l'ala A verticale e l'anima C (TA > 0, TC > 0)
            //

            string profileCode;

            if (isPrfofileR)
            {
                profileCode = "R";
                ta = tb = tc = zMinPseudoOrizontalEdge;
            }
            else if (isPrfofileF)
            {
                profileCode = "F";
                ta = tb = 0;
                tc = ha;
            }
            else if (ta > 0 && tb > 0 || pseudoTA > 0 && pseudoTB > 0)
            {
                if (isBBOXCenterInside)
                    profileCode = "I";
                else if (hasTopHorizontalPlane && hasBottomHorizontalPlane)
                {
                    profileCode = "Q";
                    tc = ta;
                }
                else if ((hasTopHorizontalPlane || hasBottomHorizontalPlane) && hasBottomVerticalPlane && hasTopVerticalPlane)
                {
                    profileCode = "U";

                    if (ta < tc || tb < tc)
                    {
                        ta = pseudoTA;
                        tb = pseudoTB;
                    }
                }
                else
                    profileCode = "L";
            }
            else if (ta > 0 && tc > 0)
                profileCode = "L";
            else
                profileCode = "";

            //	Raggio profilo (interno per IULQ, esterno per R)
            double radiusPrf = 0;
            if (profileCode == "I" || profileCode == "U")
                radiusPrf = Math.Min(yMinMaxZOrizontalEdge - ta, hc - ta - yMaxMaxZOrizontalEdge);
            else if (profileCode == "Q")
                radiusPrf = Math.Min(yMinMaxZOrizontalEdge, hc - yMaxMaxZOrizontalEdge);
            else if (profileCode == "R")
                radiusPrf = hc / 2;

            //  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //
            //  Da verificare se il raggio profilo del profilo Q sia corretto
            //  (dovrebbe essere quello esterno e non interno)
            //
            //  !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            //
            //	Nel caso in cui serva un raggio profilo non nullo e questo non sia stato identificato,
            //  lo forzo io in funzione delle dimensioni degli spessori
            //
            bool reqOverwriteRadiusZero = profileCode != "F";
            if (radiusPrf <= 0 && reqOverwriteRadiusZero)
            {
                if (profileCode == "L")
                    radiusPrf = Math.Min(ta, tc);
                else
                    radiusPrf = Math.Max(ta, tc);
            }

            //
            //  Rotazioni profili U e L per riportarli nell'orientamento previsto dai formati XML/FNC
            //
            if (profileCode == "U")
            {
                //
                //  Nel caso in cui il profilo abbia le ali verso il basso,
                //  applico una rotazione X per riportarlo con le ali verso l'alto
                //
                if (hasTopHorizontalPlane)
                {
                    solid.Rotate(Math.PI, Vector3D.AxisX);

                    edgesMinX = solid.BoxMin.X;
                    edgesMaxX = solid.BoxMax.X;
                    edgesMinY = solid.BoxMin.Y;
                    edgesMaxY = solid.BoxMax.Y;
                    edgesMinZ = solid.BoxMin.Z;
                    edgesMaxZ = solid.BoxMax.Z;

                    solid.Translate(-edgesMinX, -edgesMinY, -edgesMinZ);
                }
            }
            else if (profileCode == "L")
            {
                if (!IsPointInsideTheSolid(new Point3D (1, 1, 1), solid))
                    solid.Rotate(Math.PI, Vector3D.AxisX);
                else if (!IsPointInsideTheSolid(new Point3D(1, 1, solid.BoxMax.Z - 1), solid))
                    solid.Rotate(-Math.PI / 2, Vector3D.AxisX);
                else if (!IsPointInsideTheSolid(new Point3D(1, solid.BoxMax.Y - 1, 1), solid))
                    solid.Rotate(Math.PI / 2, Vector3D.AxisX);

                edgesMinX = solid.BoxMin.X;
                edgesMaxX = solid.BoxMax.X;
                edgesMinY = solid.BoxMin.Y;
                edgesMaxY = solid.BoxMax.Y;
                edgesMinZ = solid.BoxMin.Z;
                edgesMaxZ = solid.BoxMax.Z;

                solid.Translate(-edgesMinX, -edgesMinY, -edgesMinZ);

                //if (hasTopHorizontalPlane)
                //{
                //    //
                //    //  Nel caso in cui il profilo L non abbia il piano orizzontale a quota Z = 0,
                //    //  applico 1 o 2 rotazioni in X per riportarlo in questa posizione
                //    //
                //    solid.Rotate(Math.PI / 2, Vector3D.AxisX);

                //    if (!HasHorizontalPlaneSolid(solid, 0))
                //        solid.Rotate(Math.PI / 2, Vector3D.AxisX);

                //    edgesMinX = solid.BoxMin.X;
                //    edgesMaxX = solid.BoxMax.X;
                //    edgesMinY = solid.BoxMin.Y;
                //    edgesMaxY = solid.BoxMax.Y;
                //    edgesMinZ = solid.BoxMin.Z;
                //    edgesMaxZ = solid.BoxMax.Z;

                //    solid.Translate(-edgesMinX, -edgesMinY, -edgesMinZ);
                //}
            }

            //
            //  Adesso che ho identificato profilo e dimensioni, posso creare l'oggetto WorkPiece
            //
            double _sa = hc,
                _ta = tc,
                _sb = ha,
                _tb = ta,
                r = profileCode == "R" ? 0 : radiusPrf,
                lp = solid.BoxSize.X;

            wp = new EyeWorkPiece(profileCode, _sa, _ta, _sb, _tb, r, lp);

            return true;
        }
    }
}