using devDept.Eyeshot.Milling;
using devDept.Geometry;
using Ficep.RobServer.Data;
using Ficep.RobServer.Utility3D;
using Ficep.Utils;
using FicepXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ficep.RobServer.XmlNet
{
    // Interface for any algorithm that modifies/optimizes a Task
    public interface ITaskPostProcessor
    {
        void Execute();
    }

    public class SafeCutStrategy : ITaskPostProcessor
    {
        // Dati generali
        private CTask _task;
        private IWorkPiece _wp;
        private EyeFeature.FeatureType _featureType;
        private double TorchRadius = 50;

        //
        // Dati profilo 
        //
        private double _sb;
        private double _tb;
        private double _ta;
        private double _sa;
        private double _zWebUp;
        private double _zWebDown;
        private double _lp;

        //
        // Dati task
        //
        private bool _isExternalCut;
        private bool _isIniCut;

        //
        // Funzioni di utilità
        //

        private Func<string, string> _GetPlane = (pathName) => pathName.Split("-", StringSplitOptions.RemoveEmptyEntries)[1].Trim();

        public SafeCutStrategy(CTask task, IWorkPiece wp, EyeFeature.FeatureType featureType)
        {
            _task = task;
            _wp = wp;
            _featureType = featureType;
        }

        public void Execute()
        {
            if (_task == null || _wp == null)
                return;

            //
            // Gestiti solo profili I al momento
            //
            if (_wp.Prf.CodePrf != "I")
                return;

            // Inizializzo i dati del profilo e del task
            InitializeProfileAndTaskData();

            if (_featureType == EyeFeature.FeatureType.EST)
            {
                // Analizzo i tagli esterni per vedere se ci sono flange da splittare
                var flangesToBeSplitted = AnalyzeExternalCuts();

                // Eseguo lo split dei path
                if (flangesToBeSplitted.Count > 0)
                {
                    SplitPathsExternal(flangesToBeSplitted);
                    AddWebLeadOutExternal(flangesToBeSplitted);
                }
            }
            else if (_featureType == EyeFeature.FeatureType.INT)
            {
                // Analizzo i tagli esterni per vedere se ci sono flange da splittare
                var internalLinks = AnalyzeInternalCuts();

                // Eseguo lo split dei path
                if (internalLinks.Count > 0)
                {
                    SplitPathsInternal(internalLinks);
                    //AddWebLeadOut();
                }
                    
            }
        }

        // 
        // Funzione di inizializzazione dei dati del profilo e del task
        //
        private void InitializeProfileAndTaskData()
        {
            _sb = _wp.Prf.SB;
            _tb = _wp.Prf.TB;
            _ta = _wp.Prf.TA;
            _sa = _wp.Prf.SA;
            _zWebUp = _sb / 2 + _ta / 2;
            _zWebDown = _sb / 2 - _ta / 2;
            _lp = _wp.Lp;
            _isExternalCut = _featureType == EyeFeature.FeatureType.EST;

            if (_task.PathList.Count > 0)
                _isIniCut = _task.PathList.Max(path => path.PointList.Max(p => p.Position.X)) < _wp.Lp / 2;
        }

        //
        // Funzione di calcolo dei path estesi per una data plane
        //
        private List<IPath> GetPathsForPlane(string plane, List<CPath> paths, double sb, double tb, double ta, double sa, bool isIni, double tol)
        {
            var extendedPaths = new List<IPath>();
            foreach (var path in paths)
            {

                bool isWebCrossing = false;

                if (plane == "A" || plane == "B")
                {
                    PathFlange flangePath = new PathFlange(plane, path);
                    extendedPaths.Add(flangePath);

                    var firstZ = path.PointList.First().Position.Z;
                    var lastZ = path.PointList.Last().Position.Z;

                    if (firstZ.IsEqualTo(0, tol))
                    {
                        double targetZ = _zWebUp;
                        isWebCrossing = path.PointList.Any(p => p.Position.Z > targetZ);
                    }
                    else if (Math.Abs(firstZ - sb) < 1e-3)
                    {
                        double targetZ = _zWebDown;
                        isWebCrossing = path.PointList.Any(p => p.Position.Z < targetZ);
                    }

                    if (!isWebCrossing)
                    {
                        flangePath.IsWebCrossing = false;
                        continue;
                    }
                    else
                        flangePath.IsWebCrossing = true;

                    double zTarget = _zWebUp;
                    double? xAtZ = GetXInterceptAtZ(path, zTarget);

                    if (!xAtZ.HasValue)
                        continue;

                    flangePath.XIntercept = xAtZ.Value;

                    // Calcolo lo shift che andrà applicato in caso di cianfrino
                    if (!Math.Abs(path.PointList[0].Vector.Vy).IsEqualTo(1, tol * 0.1))
                    {
                        var p0 = path.PointList.First().Position;
                        var v0 = path.PointList.First().Vector;
                        double depth = tb;
                        double angleRad = Math.Atan2(v0.Vy, v0.Vx);
                        angleRad = plane == "A" ? angleRad : -angleRad;
                        //double deltaX = plane == "A" ? -depth * Math.Tan(angleRad) : depth * Math.Tan(angleRad);
                        double deltaX = depth * Math.Tan(Math.PI / 2 - angleRad);

                        flangePath.ChamferXDisplacement = deltaX;
                    }
                }
                else if (plane == "C")
                {
                    PathWeb webPath = new PathWeb(plane, path);
                    extendedPaths.Add(webPath);

                    List<CPoint> xSegmentA = GetSegmentWithClosestY(path.PointList, 0);
                    List<CPoint> xSegmentB = GetSegmentWithClosestY(path.PointList, sa);

                    if (xSegmentA != null && xSegmentA.Count > 0)
                    {
                        var sortedA = path.PointList.OrderBy(p => p.Position.X).ToList();
                        webPath.MinCoordinateA = (sortedA.First().Position.X, sortedA.First().Position.Y);
                        webPath.MaxCoordinateA = (sortedA.Last().Position.X, sortedA.Last().Position.Y);
                    }

                    if (xSegmentB != null && xSegmentB.Count > 0)
                    {
                        var sortedB = path.PointList.OrderBy(p => p.Position.X).ToList();
                        webPath.MinCoordinateB = (path.PointList.First().Position.X, sortedB.First().Position.Y);
                        webPath.MaxCoordinateB = (path.PointList.Last().Position.X, sortedB.Last().Position.Y);
                    }
                }
            }

            return extendedPaths;
        }

        #region Internal Cut Functions
        // Updated Helper Class
        private class InternalLinkGroup
        {
            public PathWeb WebPath { get; set; }
            public List<PathFlange> FlangePaths { get; set; }
            public string Plane { get; set; }
            public double MinX { get; set; }
            public double MaxX { get; set; }
        }
        private List<InternalLinkGroup> AnalyzeInternalCuts()
        {
            List<InternalLinkGroup> linkGroups = new List<InternalLinkGroup>();
            double tol = 0.1;
            double TorchRadius = 50;

            // Calcolo paths con l'indice associato ad essi nella task.PathList
            var pathsA = _task.PathList?.Where(x => _GetPlane(x.Name) == "A").ToList();
            var pathsB = _task.PathList?.Where(x => _GetPlane(x.Name) == "B").ToList();
            var pathsC = _task.PathList?.Where(x => _GetPlane(x.Name) == "C").ToList();

            if (pathsC == null || pathsC.Count == 0) return linkGroups;

            // Calcolo le classi necessarie per l'analisi
            List<PathFlange> PathsA = GetPathsForPlane("A", pathsA, _sb, _tb, _ta, _sa, _isIniCut, tol).Cast<PathFlange>().ToList();
            List<PathFlange> PathsB = GetPathsForPlane("B", pathsB, _sb, _tb, _ta, _sa, _isIniCut, tol).Cast<PathFlange>().ToList();
            List<PathWeb> PathsC = GetPathsForPlane("C", pathsC, _sb, _tb, _ta, _sa, _isIniCut, tol).Cast<PathWeb>().ToList();

            foreach (var pathC in PathsC)
            {
                // Per le attività INT, cerchiamo casi in cui un Web cut (pathC)
                // ha DUE collegamenti allo stesso piano di flangia
                CheckAndAddInternalLinks(pathC, PathsA, "A", TorchRadius, tol, linkGroups);
                CheckAndAddInternalLinks(pathC, PathsB, "B", TorchRadius, tol, linkGroups);
            }

            return linkGroups;
        }

        private void CheckAndAddInternalLinks(PathWeb web, List<PathFlange> flanges, string plane, double torchRadius, double tol, List<InternalLinkGroup> groups)
        {
            // Determinare quali coordinate del web utilizzare in base al piano
            var webMin = (plane == "A") ? web.MinCoordinateA : web.MinCoordinateB;
            var webMax = (plane == "A") ? web.MaxCoordinateA : web.MaxCoordinateB;
            double distToFlange = (plane == "A") ? (webMin.y - _wp.Prf.TB) : (_wp.Prf.SA - _wp.Prf.TB - webMin.y);

            // Se la torcia è troppo vicina alla giunzione della flangia
            if (distToFlange < torchRadius)
            {
                // Trovare un taglio di flangia che corrisponda alla X minima e un altro che corrisponda alla X massima
                var flangeAtMin = flanges.FirstOrDefault(f => f.IsWebCrossing &&
                    (f.XIntercept.Value + f.ChamferXDisplacement).IsEqualTo(webMin.x, tol));

                var flangeAtMax = flanges.FirstOrDefault(f => f.IsWebCrossing &&
                    (f.XIntercept.Value + f.ChamferXDisplacement).IsEqualTo(webMax.x, tol));

                // Se abbiamo trovato entrambi i tagli (l’ingresso e l’uscita per la caratteristica interna)
                if (flangeAtMin != null && flangeAtMax != null)
                {
                    groups.Add(new InternalLinkGroup
                    {
                        WebPath = web,
                        FlangePaths = new List<PathFlange> { flangeAtMin, flangeAtMax },
                        Plane = plane,
                        MinX = webMin.x,
                        MaxX = webMax.x
                    });
                }
            }
        }

        private void SplitPathsInternal(List<InternalLinkGroup> groups)
        {
            var pathsToAdd = new List<CPath>();
            var pathsToRemove = new List<CPath>();

            foreach (var group in groups)
            {
                if (group.FlangePaths.Count < 2) continue;

                var p1 = group.FlangePaths[0].CPath;
                var p2 = group.FlangePaths[1].CPath;

                // Creare il percorso superiore (Z > WebUp)
                CPath upperPath = CloneCPath(p1);
                upperPath.Name += "_UPPER";
                var points1Up = p1.PointList.Where(p => p.Position.Z >= _zWebUp).ToList();
                var points2Up = p2.PointList.Where(p => p.Position.Z >= _zWebUp).ToList();

                if (points1Up.Any() && points2Up.Any())
                {
                    // Collegare points1Up -> Intercept -> points2Up
                    upperPath.PointList = new List<CPoint>(points1Up);
                    // Punto intermedio a ZWebUp per collegare i due tagli
                    upperPath.PointList.Add(CreateBridgePoint(group.FlangePaths[0].XIntercept.Value, points1Up.OrderBy(p => p.Position.Z).First(), _zWebUp));
                    upperPath.PointList.Add(CreateBridgePoint(group.FlangePaths[1].XIntercept.Value, points2Up.OrderBy(p => p.Position.Z).First(), _zWebUp));
                    upperPath.PointList.AddRange(points2Up);
                    pathsToAdd.Add(upperPath);
                }

                // Creare il percorso inferiore (Z < WebDown)
                CPath lowerPath = CloneCPath(p1);
                lowerPath.Name += "_LOWER";
                var points1Down = p1.PointList.Where(p => p.Position.Z <= _zWebDown).ToList();
                var points2Down = p2.PointList.Where(p => p.Position.Z <= _zWebDown).ToList(); // Pseudo-code: replace with appropriate list access

                var pts1D = p1.PointList.Where(p => p.Position.Z <= _zWebDown).ToList();
                var pts2D = p2.PointList.Where(p => p.Position.Z <= _zWebDown).ToList();

                if (pts1D.Any() && pts2D.Any())
                {
                    lowerPath.PointList = new List<CPoint>(pts1D);
                    lowerPath.PointList.Add(CreateBridgePoint(group.FlangePaths[0].XIntercept.Value, pts1D.OrderByDescending(p => p.Position.Z).First(), _zWebDown));
                    lowerPath.PointList.Add(CreateBridgePoint(group.FlangePaths[1].XIntercept.Value, pts2D.OrderByDescending(p => p.Position.Z).First(), _zWebDown));
                    lowerPath.PointList.AddRange(pts2D);
                    pathsToAdd.Add(lowerPath);
                }

                pathsToRemove.Add(group.FlangePaths[0].CPath);
                pathsToRemove.Add(group.FlangePaths[1].CPath);
            }

            // Aggiornare Task PathList
            foreach (var path in pathsToRemove)
                _task.PathList.Remove(path);

            foreach (var newPath in pathsToAdd)
                _task.PathList.Insert(0, newPath);
        }

        private CPoint CreateBridgePoint(double x, CPoint template, double zTarget)
        {
            return new CPoint
            {
                Position = new CPosition(x, template.Position.Y, zTarget),
                Vector = new CVector(template.Vector.Vx, template.Vector.Vy, template.Vector.Vz), // Keeping the plane normal
                End = false,
                Overlapped = false
            };
        }
        #endregion

        #region External Cut Functions
        private List<PathFlange> AnalyzeExternalCuts()
        {
            List<PathFlange> FlangesToBeSplitted = new List<PathFlange>();

            //
            // Dati hard per test da rimuovere
            //
            double tol = 0.1;

            // Calcolo paths con l'indice associato ad essi nella task.PathList
            var pathsA = _task.PathList?
                .Where(x => _GetPlane(x.Name) == "A")?
                .ToList();

            var pathsB = _task.PathList?
                .Where(x => _GetPlane(x.Name) == "B")?
                .ToList();

            var pathsC = _task.PathList?
                .Where(x => _GetPlane(x.Name) == "C")?
                .ToList();

            // Se non ci sono pathsC o non ci sono path sulle ali non ci può essere link
            if (pathsC == null || pathsC.Count == 0 || (pathsA == null || pathsA.Count == 0) && (pathsB == null || pathsB.Count == 0))
                return FlangesToBeSplitted;

            List<PathFlange> PathsA = GetPathsForPlane("A", pathsA, _sb, _tb, _ta, _sa, _isIniCut, tol).Select(p => p as PathFlange).ToList();
            List<PathFlange> PathsB = GetPathsForPlane("B", pathsB, _sb, _tb, _ta, _sa, _isIniCut, tol).Select(p => p as PathFlange).ToList();
            
            List<PathWeb> PathsC = GetPathsForPlane("C", pathsC, _sb, _tb, _ta, _sa, _isIniCut, tol).Select(p => p as PathWeb).ToList();

            //
            // Ora posso cercare i link tra ali e anima
            //

            foreach (var pathC in PathsC)
            {
                foreach (var pathA in PathsA)
                {
                    // Se non passa il web non può esserci link
                    if (!pathA.IsWebCrossing || !pathA.XIntercept.HasValue)
                        continue;

                    // Calcolo la posizione X del path A al livello di taglio sull'animaconsiderando il cianfrino
                    double xa = pathA.XIntercept.Value + pathA.ChamferXDisplacement;

                    if ( xa.IsEqualTo(pathC.MaxCoordinateA.x, tol))
                    {
                        // Link trovato tra path C e path A
                        // Marco i path come linkati
                        pathA.LinkC.Add(pathC);
                        pathC.LinkA.Add(pathA);
                    }
                }

                foreach (var pathB in PathsB)
                {
                    // Se non passa il web non può esserci link
                    if (!pathB.IsWebCrossing || !pathB.XIntercept.HasValue)
                        continue;

                    // Calcolo la posizione X del path B al livello di taglio sull'animaconsiderando il cianfrino
                    double xb = pathB.XIntercept.Value + pathB.ChamferXDisplacement;

                    if (pathB.IsWebCrossing && xb.IsEqualTo(pathC.MaxCoordinateB.x, tol))
                    {
                        // Link trovato tra path C e path A
                        // Marco i path come linkati
                        pathB.LinkC.Add(pathC);
                        pathC.LinkB.Add(pathB);
                    }
                }

                //
                // Se ci sono link tra path C e A o B verifico se è necessario splittare
                //

                // Se la x massima del segmento lungo la pseudo X è 0 o lp vuoldire che il taglio è all'inizio fine del'anima e non deve essere splittato
                bool isNotWebAtEdge = !(pathC.MaxCoordinateA.x.IsEqualTo(0, tol) || pathC.MaxCoordinateA.x.IsEqualTo(_lp, tol));

                if (pathC.LinkA.Count != 0 && isNotWebAtEdge)
                {
                    double spaceToFlangeA = pathC.MaxCoordinateA.y - _wp.Prf.TB;
                    if (spaceToFlangeA < TorchRadius)
                    {
                        // Se c'è più di un link, aggiungi solo quello con vettore normale al piano (Vy = ±1)
                        var normalA = pathC.LinkA
                            .FirstOrDefault(pa =>
                                pa.CPath.PointList.Count > 0 &&
                                Math.Abs(pa.CPath.PointList[0].Vector.Vy).IsEqualTo(1, 0.01)
                            );
                        if (normalA != null)
                            FlangesToBeSplitted.Add(normalA);
                    }
                }

                isNotWebAtEdge = !(pathC.MaxCoordinateB.x.IsEqualTo(0, tol) || pathC.MaxCoordinateB.x.IsEqualTo(_lp, tol));

                if (pathC.LinkB.Count != 0 && isNotWebAtEdge)
                {
                    double spaceToFlangeB = _wp.Prf.SA - _wp.Prf.TB - pathC.MaxCoordinateB.y;
                    if (spaceToFlangeB < TorchRadius)
                    {
                        // Se c'è più di un link, aggiungi solo quello con vettore normale al piano (Vy = ±1)
                        var normalB = pathC.LinkB
                            .FirstOrDefault(pb =>
                                pb.CPath.PointList.Count > 0 &&
                                Math.Abs(pb.CPath.PointList[0].Vector.Vy).IsEqualTo(1, 0.01)
                            );
                        if (normalB != null)
                            FlangesToBeSplitted.Add(normalB);
                    }
                }
            }

            return FlangesToBeSplitted;
        }

        private void SplitPathsExternal(List<PathFlange> pathToBeSplittedList)
        {
            // Variabile che fa uscire il taglio dell'ala di quella quantità in x
            double surplusX = 5;

            var pathsToBeAdded = new List<CPath>();

            foreach (var path in pathToBeSplittedList)
            {
                var pathToBeSplitted = path.CPath;

                if (pathToBeSplitted.PointList.Count < 2)
                    continue;

                // Se è un'esterna so che devo splittare il taglio sull'ala in due e andare in x = 0 o x = Lp in base a isIni
                if (_isExternalCut)
                {

                    var upperFlangePath = CloneCPath(pathToBeSplitted);
                    var lowerFlangePath = CloneCPath(pathToBeSplitted);

                    // Upper flange
                    upperFlangePath.PointList = upperFlangePath.PointList
                        .Where(p => p.Position.Z > _zWebUp).OrderByDescending(p => p.Position.Z)
                        .ToList();

                    if (upperFlangePath.PointList.Count >= 1)
                    {
                        // Punto 1
                        double p1X = path.XIntercept.Value,
                               p1Y = upperFlangePath.PointList.Last().Position.Y,
                               p1Z = _zWebUp,
                               v1X = upperFlangePath.PointList.Last().Vector.Vx,
                               v1Y = upperFlangePath.PointList.Last().Vector.Vy,
                               v1Z = upperFlangePath.PointList.Last().Vector.Vz;
                        // Punto 2
                        double p2X = _isIniCut ? -surplusX : _wp.Lp + surplusX,
                               p2Y = upperFlangePath.PointList.Last().Position.Y,
                               p2Z = _zWebUp,
                               v2X = 0,
                               v2Y = path.Plane == "A" ? -1 : 1,
                               v2Z = 0;

                        // Aggiungo i 2 punti di split
                        upperFlangePath.PointList.Insert(0, new CPoint
                        {
                            Position = new CPosition(p1X, p1Y, p1Z),
                            Vector = new CVector(v1X, v1Y, v1Z),
                            End = false,
                            Overlapped = false
                        });

                        upperFlangePath.PointList.Insert(0, new CPoint
                        {
                            Position = new CPosition(p2X, p2Y, p2Z),
                            Vector = new CVector(v2X, v2Y, v2Z),
                            End = true,
                            Overlapped = false
                        });
                    }

                    
                    // Lower flange
                    lowerFlangePath.PointList = lowerFlangePath.PointList
                        .Where(p => p.Position.Z < _zWebDown).OrderBy(p => p.Position.Z)
                        .ToList();

                    if (lowerFlangePath.PointList.Count >= 1)
                    {
                        // Punto 1
                        double p1X = path.XIntercept.Value,
                               p1Y = upperFlangePath.PointList.Last().Position.Y,
                               p1Z = _zWebDown,
                               v1X = lowerFlangePath.PointList.Last().Vector.Vx,
                               v1Y = lowerFlangePath.PointList.Last().Vector.Vy,
                               v1Z = lowerFlangePath.PointList.Last().Vector.Vz;
                        // Punto 2
                        double p2X = _isIniCut ? -surplusX : _wp.Lp + surplusX,
                               p2Y = lowerFlangePath.PointList.Last().Position.Y,
                               p2Z = _zWebDown,
                               v2X = 0,
                               v2Y = path.Plane == "A" ? -1 : 1,
                               v2Z = 0;

                        // Aggiungo i 2 punti di split
                        lowerFlangePath.PointList.Insert(0, new CPoint
                        {
                            Position = new CPosition(p1X, p1Y, p1Z),
                            Vector = new CVector(v1X, v1Y, v1Z),
                            End = false,
                            Overlapped = false
                        });

                        lowerFlangePath.PointList.Insert(0, new CPoint
                        {
                            Position = new CPosition(p2X, p2Y, p2Z),
                            Vector = new CVector(v2X, v2Y, v2Z),
                            End = true,
                            Overlapped = false
                        });
                    }

                    pathsToBeAdded.Add(upperFlangePath);
                    pathsToBeAdded.Add(lowerFlangePath);

                    //// Sostituisco il path originale con i 2 nuovi path splittati
                    //_task.PathList.Insert(0, lowerFlangePath);
                    //_task.PathList.Insert(0, upperFlangePath);
                    //
                    //// Rimuovo il path originale
                    //_task.PathList.RemoveAt(path.IndexInPathList + 2);
                }
            }

            // Rimuovo i path originali da splittare, partendo da quello con indice più alto per evitare di sfasare gli indici
            foreach (var path in pathToBeSplittedList)
            {
                if (path.CPath != null)
                {
                    _task.PathList.Remove(path.CPath);
                }
            }

            // Aggiungo i nuovi path splittati alla fine della lista
            foreach (var newPath in pathsToBeAdded)
            {
                _task.PathList.Insert(0, newPath);
            }
        }

        private void AddWebLeadOutExternal(List<PathFlange> pathsToBeSplitted)
        {
            // Distanza di sicurezza per non far collidere la torcia con l'ala
            double safeDistance = 1,
                   exitDistance = 5;
            List<PathWeb> pathc = pathsToBeSplitted.SelectMany(p => p.LinkC).Distinct().ToList();

            foreach (var path in pathc)
            {
                if (_isIniCut)
                {
                    var pi = path.CPath.PointList.First();
                    var     pf = path.CPath.PointList.Last();

                    CPoint p;

                    if (pi.Position.X > pf.Position.X)
                        p = pi;
                    else
                        p = pf;

                    CPoint p1 = new CPoint
                    {
                        Position = new CPosition(0, 0, p.Position.Z),
                        Vector = new CVector(p.Vector.Vx, p.Vector.Vy, p.Vector.Vz),
                        End = true,
                        Overlapped = false
                    },
                    p2 = new CPoint
                    {
                        Position = new CPosition(0, 0, p.Position.Z),
                        Vector = new CVector(p.Vector.Vx, p.Vector.Vy, p.Vector.Vz),
                        End = true,
                        Overlapped = false
                    };

                    double x = p.Position.X - TorchRadius - safeDistance;

                    // Se x < 0 significa che non c'è spazio per uscire in y perchè la torcia è più grande dell'apertura in x
                    // in questo caso esco in x 
                    // Altrimenti esco in y
                    if (x < 0)
                    {
                        p1.Position.X = -exitDistance;
                        p1.Position.Y = p.Position.Y;

                        path.CPath.PointList.Insert(0, p1);
                    }
                    else
                    {
                        p1.Position.X = x;
                        p2.Position.X = x;
                        p2.Position.Y = -exitDistance;

                        path.CPath.PointList.Insert(0, p1);
                        path.CPath.PointList.Insert(0, p2);
                    }
                }
            }
        }
        #endregion

        private CPath CloneCPath(CPath source)
        {
            if (source == null)
                return null;

            var clone = new CPath
            {
                Name = source.Name,
                Comp = source.Comp,
                Type = source.Type,
                End = source.End,
                LeadIn = source.LeadIn != null ? new CLeadInOut
                {
                    Type = source.LeadIn.Type,
                    Length = source.LeadIn.Length,
                    Radius = source.LeadIn.Radius,
                    Angle = source.LeadIn.Angle
                } : null,
                LeadOut = source.LeadOut != null ? new CLeadInOut
                {
                    Type = source.LeadOut.Type,
                    Length = source.LeadOut.Length,
                    Radius = source.LeadOut.Radius,
                    Angle = source.LeadOut.Angle
                } : null
            };

            if (source.PointList != null)
            {
                clone.PointList = new List<CPoint>(source.PointList.Count);
                foreach (var p in source.PointList)
                {
                    var pointClone = new CPoint
                    {
                        Position = p.Position != null ? new CPosition(p.Position.X, p.Position.Y, p.Position.Z) : null,
                        Vector = p.Vector != null ? new CVector(p.Vector.Vx, p.Vector.Vy, p.Vector.Vz) : null,
                        End = p.End,
                        Overlapped = p.Overlapped
                    };
                    clone.PointList.Add(pointClone);
                }
            }
            else
            {
                clone.PointList = new List<CPoint>();
            }

            return clone;
        }
        

        #region Geometric Helpers

        /// <summary>
        /// Calcola l'intercetta X del CPath ad una quota Z specificata.
        /// Restituisce null se non esiste alcuna intercetta.
        /// </summary>
        private double? GetXInterceptAtZ(CPath path, double targetZ, double tol = 1e-3)
        {
            if (path == null || path.PointList == null || path.PointList.Count < 2)
                return null;

            for (int i = 1; i < path.PointList.Count; i++)
            {
                var p0 = path.PointList[i - 1].Position;
                var p1 = path.PointList[i].Position;

                // Verifica se il segmento attraversa la quota Z
                if ((p0.Z - targetZ) * (p1.Z - targetZ) <= 0 && Math.Abs(p0.Z - p1.Z) > tol)
                {
                    // Interpolazione lineare per trovare X all'intercetta Z
                    double t = (targetZ - p0.Z) / (p1.Z - p0.Z);
                    double xIntercept = p0.X + t * (p1.X - p0.X);
                    return xIntercept;
                }
            }
            return null;
        }

        /// <summary>
        /// Restituisce i punti appartenenti al segmento lungo X con Y più vicina a yTarget.
        /// Un segmento è definito come una sequenza di punti consecutivi con Y costante (entro una tolleranza).
        /// </summary>
        private List<CPoint> GetSegmentWithClosestY(List<CPoint> pointList, double yTarget, double tol = 1e-3)
        {
            if (pointList == null || pointList.Count == 0)
                return new List<CPoint>();

            //
            // Raggruppa i punti in segmenti consecutivi con Y costante (entro tolleranza)
            //
            var segments = new List<List<CPoint>>();
            var currentSegment = new List<CPoint> { pointList[0] };

            for (int i = 1; i < pointList.Count; i++)
            {
                if (Math.Abs(pointList[i].Position.Y - pointList[i - 1].Position.Y) < tol)
                {
                    currentSegment.Add(pointList[i]);
                }
                else
                {
                    segments.Add(new List<CPoint>(currentSegment));
                    currentSegment.Clear();
                    currentSegment.Add(pointList[i]);
                }
            }
            if (currentSegment.Count > 1)
                segments.Add(currentSegment);

            //
            // Trova il segmento con la Y media più vicina a yTarget
            //
            double minDist = double.MaxValue;
            List<CPoint> bestSegment = null;
            foreach (var seg in segments)
            {
                double yAvg = seg.Average(p => p.Position.Y);
                double dist = Math.Abs(yAvg - yTarget);
                if (dist < minDist)
                {
                    minDist = dist;
                    bestSegment = seg;
                }
            }

            return bestSegment ?? new List<CPoint>();
        }
        #endregion
    }

    public interface IPath
    {
        public string Plane { get; set; }
        public CPath CPath { get; set; }
    }

    public class PathFlange : IPath
    {
        public string Plane { get; set; }
        public CPath CPath { get; set; }
        public double? XIntercept { get; set; }
        public double ChamferXDisplacement { get; set; }
        public bool IsWebCrossing { get; set; }
        public List<PathWeb> LinkC { get; set; }

        public PathFlange(string plane, CPath cPath)
        {
            Plane = plane;
            XIntercept = null;
            ChamferXDisplacement = 0;
            IsWebCrossing = false;
            LinkC = new List<PathWeb>();
            CPath = cPath;
        }
    }

    public class PathWeb : IPath
    {
        public string Plane { get; set; }
        public CPath CPath { get; set; }
        public List<PathFlange> LinkA { get; set; }
        public List<PathFlange> LinkB { get; set; }

        // Updated: Store both ends of the connection segment for each flange side
        public (double x, double y) MinCoordinateA { get; set; }
        public (double x, double y) MaxCoordinateA { get; set; }
        public (double x, double y) MinCoordinateB { get; set; }
        public (double x, double y) MaxCoordinateB { get; set; }

        public PathWeb(string plane, CPath cPath)
        {
            Plane = plane;
            LinkA = new List<PathFlange>();
            LinkB = new List<PathFlange>();
            CPath = cPath;
        }
    }
}
