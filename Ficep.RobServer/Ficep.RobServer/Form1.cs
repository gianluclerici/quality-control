using devDept.Eyeshot.Control;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot;
using devDept.Geometry;
using Ficep.MacroGra;
using Ficep.MacroLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Globalization;
using devDept.Eyeshot.Translators;
using System.IO;
using Ficep.RobServer.Utility3D;
using System.Net;
using Ficep.RobServer.XmlNet;
using Ficep.RobServer.ImportExport;
using Ficep.RobServer.Data;
using System.Windows.Input;
using Ficep.RobServer.MacroParser;
using FicepXml;
using System.Threading;
using FicepControls;
using devDept.Serialization;
using System.Reflection;
using System.Text.Json;
using Ficep.AnyCut.ConNet;
using MessageCode = Ficep.AnyCut.ConNet.RobServerTcpConnection.MessageCode;
using Ficep.Utils;

namespace Ficep.RobServer
{
    public partial class Form1 : Form
    {
        //*********************************************
        //                  PARAMETRI
        //*********************************************
        //
        //  Parametri con override da file .ini
        //
        private bool minimizeWindow = false;
        private bool showMacroList = false;
        private EyeParam eyeParam = new EyeParam();
        private MacroList macroList = null;
        private string curFileExtension = "";
        //
        //  Parametri con override sia da file .ini che da command line
        //
        private string robotIniPath;
        private string robotFolder;

        //
        //  Parametri specificabili solo da command line
        //
        private string fncPath = null;
        private string exportFormat = null;
        private string exportPath = null;
        private readonly bool singleFile;

        //
        //  Parametri per la gestione della modalità DSTV
        //
        private bool enableDstv = false;
        private string dstvFilePath = null;

        //
        //  Parametri per la gestione della modalità SERVER TCP
        //
        private bool enTCPServer = false;
        private TcpServer tcpServer = new TcpServer();

        //
        //  Parametri con override da chiamata CLIENT TCP
        //
        private bool processAsPieceCutToMeasure = true;

        //
        //  Parametri di configurazione interna delle prestazioni sw.
        //  A regime dovranno essere tutti true
        //
        private static bool enableWorkpieceVectors = true;
        private static bool enableWorkpieceVectorsI = true;
        private static bool enableWorkpieceVectorsU = true;
        private static bool enableWorkpieceVectorsL = true;
        private static bool enableWorkpieceVectorsQ = true;
        private static bool enableWorkpieceVectorsR = true;
        private static bool enableWorkpieceVectorsF = true;

        //*********************************************
        //                  VARIABILI
        //*********************************************
        private Importer importExport;
        private XmlInterface xmlInterface;
        private bool showFeatures = false;
        public WpDynamicObjects wpDynamicObj;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Classe contenete le informazioni del file Robot.json corrispondente al file Robot.ini
        List<MacroLibGroup> macroLibGroup = null;
        // Classe contenete le informazioni del file Robot.ini
        IniReader robotIni = null;
        private bool isJsonIniFilePath; // Indica se il percorso del file di configurazione è un json


        //
        //  Argument 1: robotIniPath       Robot.ini file
        //  Argument 2: fncPath            FNC path
        //  Argument 3: exportFormat       /eye or /stl or /xml
        //  Argument 4: exportPath         export path
        //  Argument 5: singleFile         in case of .eye files indicate whether is single file or multifile the saving /single or /multi
        //
        //  EXAMPLE
        //  
        //      C:\PEGASO.MTTEST\\ROBOT.INI
        //      C:\PEGASO.MTTEST\Graficafnc\GRAFICA1.FNC
        //      /eye
        //      C:\PEGASO.MTTEST\Graficafnc\c.eye
        //      /single
        //
        public Form1(string[] args)
        {
            SetEnglishCulture();
            //
            //  Importer non ha parametri di configurazione
            //
            importExport = new Importer();

            //
            //  XmlInterface ha parametri di configurazione che vengono inizializzati e poi
            //  eventualmente overridati dalla lettura del file .ini
            //
            xmlInterface = new XmlInterface();
            xmlInterface.FeatureExtractionParam.EnableWorkpieceVectors = enableWorkpieceVectors;
            xmlInterface.FeatureExtractionParam.LeadIn.Type = FicepXml.LeadInOutType.Line;
            xmlInterface.FeatureExtractionParam.LeadIn.Length = 5;
            xmlInterface.FeatureExtractionParam.LeadIn.Angle = 45;
            xmlInterface.FeatureExtractionParam.LeadOut.Type = FicepXml.LeadInOutType.Line;
            xmlInterface.FeatureExtractionParam.LeadOut.Length = 5;
            xmlInterface.FeatureExtractionParam.LeadOut.Angle = 45;
            xmlInterface.FeatureExtractionParam.OverlapCloseContour = 2;
            xmlInterface.FeatureExtractionParam.MinDistanceFromInnerFlange = 5;
            xmlInterface.FeatureExtractionParam.MinPointDistance = 5;
            xmlInterface.FeatureExtractionParam.ArcSegmentLength = 2;
            xmlInterface.FeatureExtractionParam.TollContourClosed = 1;

            //
            //  Leggo dal file RobServer.ini eventuali parametri di configurazione
            //
            LoadParamsFromIni(xmlInterface.FeatureExtractionParam);

            //
            //  Esempio di command line per TCPServer:
            //
            //  /srv 1940
            //
            //  Esempio di command line per Dstv:
            //  /dstv C:\PEGASO.MTTEST\Graficafnc\GRAFICA1.nc
            //
            if (args.Length >= 2)
            { 
                enTCPServer = (args[0] == "/srv");
                enableDstv = (args[0] == "/dstv");
            }

            if (enTCPServer)
            {
                //
                //  Nella modalità TCPserver, l'applicativo non legge i parametri della
                //  linea di comando e parte di default minimizzato
                //
                minimizeWindow = !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));

                //
                //  Modalità SERVER TCP/IP
                //
                int ipPort = int.Parse(args[1]);
                if (ipPort <= 0)
                    throw new ArgumentException("Invalid IP port");

                //  Avvio il Server passando Port e Delegate da eseguire
                tcpServer.Start(ipPort, MainThreadDelegate);
            }
            else if (enableDstv)
            {
                minimizeWindow = true;

                dstvFilePath = args.Where(x => x.ToLower().EndsWith(".nc") || x.ToLower().EndsWith("nc1")).ToArray()[0];

                if (dstvFilePath == null)
                {
                    MessageBox.Show("ERROR: Dstv file path not found", "Ficep.RobServer");
                    Environment.Exit(0);
                    //throw new ArgumentException("Dstv file path not found");
                }

                Mesh m= null;
                Brep b = null;
                EyeWorkPiece eyeWp = new EyeWorkPiece();
                if (!importExport.ImportStlStep(0.1, ref m, ref b, ref eyeWp, ref dstvFilePath, out List<Brep> scraps))
                    Log.Write(@"C:\Temp\dstv.log", "Failed" + " " + Path.GetFileName(dstvFilePath));

                Environment.Exit(0);
            }
            else
            {
                //
                //  Nella modalità di viewer grafico, vengono letti i parametri della
                //  linea di comando
                //
                if (args.Length >= 1 || robotIniPath == null)
                {
                    if (args.Length < 1 || args.Length > 5 || args.Length == 3)
                    {
                        MessageBox.Show("ERROR: Invalid number of arguments", "Ficep.RobServer");
                        Environment.Exit(0);

                        //throw new ArgumentException("Invalid number of arguments");
                    }
                    if (args.Where(x => x.ToLower().EndsWith(".fnc") || x.ToLower().EndsWith(".json")).Count() != 1 && args.Length != 1)
                    {
                        MessageBox.Show("ERROR: FNC path file not found", "Ficep.RobServer");
                        Environment.Exit(0);

                        //throw new ArgumentException("FNC path file not found");
                    }
                    else if (args.Length != 1)
                        fncPath = args.Where(x => x.ToLower().EndsWith(".fnc")).ToArray()[0];
                    if (args.Where(x => x.ToLower().EndsWith(".ini") || x.ToLower().EndsWith(".json")).Count() != 1)
                    {
                        MessageBox.Show("ERROR: INI path file not found", "Ficep.RobServer");
                        Environment.Exit(0);

                        //throw new ArgumentException("INI path file not found");
                    }
                    else
                        robotIniPath = args.Where(x => x.ToLower().EndsWith(".ini") || x.ToLower().EndsWith(".json")).ToArray()[0];

                    if (args.Length > 3)
                    {
                        if (args[2].StartsWith("/"))
                        {
                            exportFormat = args[2].TrimStart('/');
                        }
                        else
                        {
                            MessageBox.Show("ERROR: File format argument must be preceded by /", "Ficep.RobServer");
                            Environment.Exit(0);

                            //throw new ArgumentException("File format argument must be preceded by /");
                        }

                        exportPath = args[3];
                        if (args.Length == 5)
                            singleFile = args[4] == "/single" ? true : args[4] == "/multi" ? false : throw new ArgumentException("Argument 5 not recognized");
                    }
                }
            }

            InitializeComponent();

            //
            //  Se non è abilitata la visualizzazione della lista macro,
            //  nascondo la lista e visualizzo design1 a pieno applicativo
            //
            if (!showMacroList)
            {
                //  Nascondo la lista
                listMacroView.Hide();

                //  Espando design1 fino ad occupare l'intero SplitContainer
                splitContainer.Panel1Collapsed = false;
                splitContainer.Panel2Collapsed = true;
            }

            bool applyTest = false;
            if (applyTest) 
                Test();
        }

        //
        //  Caricamento parametri di configurazione da file .ini
        //
        private bool LoadParamsFromIni(CFeatureExtractionParam featureExtractionParam)
        {
            string executableDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string iniPath = executableDirectory + "RobServer.ini";
            //
            //  Se il file .ini non esiste, vengono tenuti i valori di default
            //
            if (!File.Exists(iniPath))
                return false;

            IniReader robServerIni = new IniReader(iniPath);

            // **************************************************
            //
            //  [CONFIG]
            //
            // **************************************************

            //
            //  ShowWindow = 1 abilita la visualizzazione della finestra grafica
            //  
            if (robServerIni.Read("ShowWindow", "CONFIG") == "1")
                minimizeWindow = false;

            //
            //  ShowMacroList = 1 abilita la visualizzazione della lista macro
            //  
            if (robServerIni.Read("ShowMacroList", "CONFIG") == "1")
                showMacroList = true;

            //
            //  RobotIni = path assoluto o relativo del file di configurazione robot.ini
            //
            string strrobotIniPath = robServerIni.Read("RobotIni", "CONFIG");
            if (strrobotIniPath != "")
            {
                if (strrobotIniPath.Contains(":"))
                    robotIniPath = strrobotIniPath;
                else
                    robotIniPath = executableDirectory  + strrobotIniPath;
            }

            //
            //  RobotFolder = path del folder contenente le bitmap delle macro
            //
            string strrobotFolder = robServerIni.Read("RobotFolder", "CONFIG");
            if (strrobotFolder != "")
            {
                if (strrobotFolder.Contains(":"))
                    robotFolder = strrobotFolder;
                else
                    robotFolder = executableDirectory + strrobotFolder;
            }

            // **************************************************
            //
            //  [GEOMETRY]
            //
            // **************************************************
            double brepTol = 0.001;
            double tolWebFlange = 0.01;
            double linearTol = 0.1;
            double angleTol = 0.01;
            double surplus = 1;
            double innerChamferDisFromWeb = 2;

            //
            //  TolBrep = tolleranza utilizzata nei calcoli dei brep
            //
            string strbrepTol = robServerIni.Read("TolBrep", "GEOMETRY");
            if (strbrepTol != "")
                brepTol = Math.Max(double.Parse(strbrepTol), 0.001);

            //
            //  TolWebFlange = tolleranza sugli spessori
            //
            string strtolWebFlange = robServerIni.Read("TolWebFlange", "GEOMETRY");
            if (strtolWebFlange != "")
                tolWebFlange = Math.Max(double.Parse(strtolWebFlange), 0.001);

            //
            //  TolLinear = tolleranza sui confronti lineari
            //
            string strLinearTol = robServerIni.Read("TolLinear", "GEOMETRY");
            if (strLinearTol != "")
                linearTol = Math.Max(double.Parse(strLinearTol), 0.001);

            //
            //  TolAngle = tolleranza sui confronti angolari
            //
            string strAngleTol = robServerIni.Read("TolAngle", "GEOMETRY");
            if (strLinearTol != "")
                angleTol = Math.Max(double.Parse(strAngleTol), 0.001);

            //
            //  Surplus = surplus applicato alla profondità di estrusione
            //
            string strsurplus = robServerIni.Read("Surplus", "GEOMETRY");
            if (strsurplus != "")
                surplus = Math.Max(double.Parse(strsurplus), 0.01);

            //
            //  InnerChamferDisFromWeb = distanza dall'anima cianfrini ali spezzati in 2
            //
            string strinnerChamferDisFromWeb = robServerIni.Read("InnerChamferDisFromWeb", "GEOMETRY");
            if (strinnerChamferDisFromWeb != "")
                innerChamferDisFromWeb = Math.Max(double.Parse(strinnerChamferDisFromWeb), 0.1);

            eyeParam = new EyeParam(linearTol, angleTol, brepTol, tolWebFlange, surplus, innerChamferDisFromWeb);

            // **************************************************
            //
            //  [FEATURE]
            //
            // **************************************************

            //
            //  NoLandingOnPipes = 1 elimina landing dai pipes
            //  
            string strSplitinTwoClosedEndContourPrfRQ = robServerIni.Read("SplitinTwoClosedEndContourPrfRQ", "FEATURE");
            if (strSplitinTwoClosedEndContourPrfRQ != "")
                featureExtractionParam.SplitinTwoClosedEndContourPrfRQ = long.Parse(strSplitinTwoClosedEndContourPrfRQ) == 1;

            //
            //  NoLandingOnPipes = 1 elimina landing dai pipes
            //  
            string strNoLandingOnPipes = robServerIni.Read("NoLandingOnPipes", "FEATURE");
            if (strNoLandingOnPipes != "")
                featureExtractionParam.NoLandingOnPipes = long.Parse(strNoLandingOnPipes) == 1;

            //
            //  CCWContour = 1 genero contorni antiorari
            //  
            string strCounterClockwiseContour = robServerIni.Read("CCWContour", "FEATURE");
            if (strCounterClockwiseContour != "")
                featureExtractionParam.CounterClockwiseContour = long.Parse(strCounterClockwiseContour) == 1;

            //
            //  OverlapCloseContour = sovrapposizione contorni chiusi
            //
            string strOverlapCloseContour = robServerIni.Read("OverlapCloseContour", "FEATURE");
            if (strOverlapCloseContour != "")
                featureExtractionParam.OverlapCloseContour = Math.Max(double.Parse(strOverlapCloseContour), 0);

            //
            //  MinDisFromInnerFlange = distanza minima dall'ala interna profili
            //
            string strMinDistanceFromInnerFlange = robServerIni.Read("MinDisFromInnerFlange", "FEATURE");
            if (strMinDistanceFromInnerFlange != "")
                featureExtractionParam.MinDistanceFromInnerFlange = Math.Max(double.Parse(strMinDistanceFromInnerFlange), 0);

            //
            //  MinPointDistance = distanza minima tra 2 punti
            //
            string strMinPointDistance = robServerIni.Read("MinPointDistance", "FEATURE");
            if (strMinPointDistance != "")
                featureExtractionParam.MinPointDistance = Math.Max(double.Parse(strMinPointDistance), 1);

            //
            //  ArcSegmentLength = distanza minima tra 2 punti
            //
            string strArcSegmentLength = robServerIni.Read("ArcSegmentLength", "FEATURE");
            if (strArcSegmentLength != "")
                featureExtractionParam.ArcSegmentLength = Math.Max(double.Parse(strArcSegmentLength), 1);

            //
            //  TolClosedCont = distanza minima tra 2 punti
            //
            string strTollContourClosed = robServerIni.Read("TolClosedCont", "FEATURE");
            if (strTollContourClosed != "")
                featureExtractionParam.TollContourClosed = Math.Max(double.Parse(strTollContourClosed), 1);

            //
            //  LeadInType = Line / Arc
            //
            string strLeadInType = robServerIni.Read("LeadInType", "FEATURE");
            if (strLeadInType != "")
            {
                LeadInOutType enumValue;

                if (Enum.TryParse(strLeadInType, out enumValue))
                    featureExtractionParam.LeadIn.Type = enumValue;
            }

            //
            //  LeadInLen = lunghezza tratto di LEAD-IN
            //
            string strLeadInLen = robServerIni.Read("LeadInLen", "FEATURE");
            if (strLeadInLen != "")
                featureExtractionParam.LeadIn.Length = Math.Max(double.Parse(strLeadInLen), 0);

            //
            //  LeadInAngle = angolo tratto di LEAD-IN
            //
            string strLeadInAngle = robServerIni.Read("LeadInAngle", "FEATURE");
            if (strLeadInAngle != "")
                featureExtractionParam.LeadIn.Angle = Math.Max(double.Parse(strLeadInAngle), 0);

            //
            //  LeadInRadius = raggio tratto di LEAD-IN (nel caso LeadIn.Type = Arc)
            //
            string strLeadInRadius = robServerIni.Read("LeadInRadius", "FEATURE");
            if (strLeadInRadius != "")
                featureExtractionParam.LeadIn.Radius = Math.Max(double.Parse(strLeadInRadius), 0);

            //
            //  LeadOutType = Line / Arc
            //
            string strLeadOutType = robServerIni.Read("LeadOutType", "FEATURE");
            if (strLeadOutType != "")
            {
                LeadInOutType enumValue;

                if (Enum.TryParse(strLeadOutType, out enumValue))
                    featureExtractionParam.LeadOut.Type = enumValue;
            }

            //
            //  LeadOutLen = lunghezza tratto di LEAD-OUT
            //
            string strLeadOutLen = robServerIni.Read("LeadOutLen", "FEATURE");
            if (strLeadOutLen != "")
                featureExtractionParam.LeadOut.Length = Math.Max(double.Parse(strLeadOutLen), 0);

            //
            //  LeadOutAngle = angolo tratto di LEAD-OUT
            //
            string strLeadOutAngle = robServerIni.Read("LeadOutAngle", "FEATURE");
            if (strLeadOutAngle != "")
                featureExtractionParam.LeadOut.Angle = Math.Max(double.Parse(strLeadOutAngle), 0);

            //
            //  LeadOutRadius = raggio tratto di LEAD-IN (nel caso LeadOut.Type = Arc)
            //
            string strLeadOutRadius = robServerIni.Read("LeadOutRadius", "FEATURE");
            if (strLeadOutRadius != "")
                featureExtractionParam.LeadOut.Radius = Math.Max(double.Parse(strLeadOutRadius), 0);

            return true;
        }

        //
        //  Imposta come separatore decimale il '.' indipendentemente dai settaggi locali
        //
        private void SetEnglishCulture()
        {
            CultureInfo englishCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = englishCulture;
            Thread.CurrentThread.CurrentUICulture = englishCulture;
        }
        protected override void OnLoad(EventArgs e)
        {
            if (robotIniPath.Contains(".json", StringComparison.CurrentCultureIgnoreCase))
            {
                isJsonIniFilePath = true;
                string jsonFile = File.ReadAllText(robotIniPath);
                macroLibGroup = JsonSerializer.Deserialize<List<MacroLibGroup>>(jsonFile);
            }
            else
            {
                robotIni = new IniReader(robotIniPath);                
                isJsonIniFilePath = false;
            }

            // Set the form to start in a minimized window state
            if (minimizeWindow)
                WindowState = FormWindowState.Minimized;
            design1.AccurateTransparency = false;

            wpDynamicObj = new WpDynamicObjects();
            wpDynamicObj.wp = new EyeWorkPiece();

            // Add the layers to the design
            design1.Layers.Add(EyeUtils.differenceSolids, Color.FromArgb(50, Color.Green), showFeatures);
            design1.Layers.Add(EyeUtils.workedPiece, Color.Gray);
            design1.Layers.Add(EyeUtils.vectors);
            design1.Layers.Add(EyeUtils.macroLungCurves);
            // If the fnc is specified in the arguments open it
            if (fncPath != null)
            {
                try
                {
                    if (!isJsonIniFilePath)
                    {
                        if (!importExport.ImportFNC(robotIni, null, ref wpDynamicObj.wp, ref wpDynamicObj.macros, eyeParam, fncPath))
                            return;
                    }
                    else 
                    {
                        if (!importExport.ImportFNC(null, macroLibGroup, ref wpDynamicObj.wp, ref wpDynamicObj.macros, eyeParam, fncPath))
                            return;
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message);
                }
            }

            if (wpDynamicObj.macros.Count != 0)
            {
                if (wpDynamicObj.wp is EyeWorkPiece eyeWp)
                {
                    wpDynamicObj.finalPart = (Brep)(eyeWp).Solid.Clone();

                    wpDynamicObj.exportEntities.Add(wpDynamicObj.finalPart.Clone() as Brep);

                    xmlInterface.FeatureExtractionParam.EnableWorkpieceVectors =
                    enableWorkpieceVectors && (
                    eyeWp.Prf.CodePrf == "I" && enableWorkpieceVectorsI ||
                    eyeWp.Prf.CodePrf == "U" && enableWorkpieceVectorsU ||
                    eyeWp.Prf.CodePrf == "L" && enableWorkpieceVectorsL ||
                    eyeWp.Prf.CodePrf == "Q" && enableWorkpieceVectorsQ ||
                    eyeWp.Prf.CodePrf == "R" && enableWorkpieceVectorsR ||
                    eyeWp.Prf.CodePrf == "F" && enableWorkpieceVectorsF
                    );

                    // Create the macro in the final part
                    if (xmlInterface.FeatureExtractionParam.EnableWorkpieceVectors)
                    {
                        if (!CreateFinalPartComputingVectorsAtTheEnd())
                            log.Debug("Failed CreateFinalPartComputingVectorsAtTheEnd()");
                    }
                    else
                        CreateFinalPart(eyeWp);
                }
            }

            // Set the coordinate system in the origin and add it to the design
            Rotation rot = new Rotation(0, Vector3D.AxisZ);
            Translation tra = new Translation(0, 0, 0);
            design1.ActiveViewport.OriginSymbols = new[] { new OriginSymbol(1, "origin", tra * rot, false) };

            if (exportFormat == "eye")
            {
                importExport.ExportEntities(wpDynamicObj.exportEntities, exportPath, singleFile);
                Environment.Exit(0);
            }
            else if (exportFormat == "stl")
            {
                wpDynamicObj.exportEntities.Clear();
                wpDynamicObj.finalPart.Regen(eyeParam.Tol.Brep);
                wpDynamicObj.exportEntities.Add(wpDynamicObj.finalPart.Clone() as Entity);
                WriteFileParams writeFileParams = new WriteFileParams(wpDynamicObj.exportEntities, new LayerKeyedCollection(new Layer("Default")));
                WriteSTL writeSTL = new WriteSTL(writeFileParams, exportPath);
                writeSTL.DoWork();
                Environment.Exit(0);
            }
            else if (exportFormat == "xml")
            {
                importExport.ExportFicepXML(exportPath, eyeParam.Tol.Brep, wpDynamicObj.wp, wpDynamicObj.macros.Select(m => m as EyeMacro).ToList(), wpDynamicObj.finalPart, processAsPieceCutToMeasure, ref importExport, ref xmlInterface);
                Environment.Exit(0);
            }

            // Add the WorkedPart to the design
            if (fncPath != null)
            {
                design1.Entities.Add(wpDynamicObj.finalPart.Clone() as Entity, EyeUtils.workedPiece);
                design1.Entities.Regen();
            }

            // Set the left view to be consinstent with the reference system used in ficep
            design1.SetView(viewType.Left);

            design1.CompileUserInterfaceElements();

            // fits the model in the viewport
            design1.ZoomFit();

            if (minimizeWindow)
            {
                //Width = 0;
                Height = 0;
                Opacity = 0;
                MaximizeBox = false;
                // Set the form as a tool window
                FormBorderStyle = FormBorderStyle.FixedToolWindow;
            }

            if (showMacroList)
            {
                macroList = new MacroList(
                    listMacroView,
                    wpDynamicObj,
                    design1,
                    importExport,
                    eyeParam,
                    pictureBox,
                    robotIniPath,
                    robotFolder,
                    xmlInterface.FeatureExtractionParam.ArcSegmentLength,
                    log,
                    macroLibGroup);
                macroList.ListPopulate();
                macroList.ListSelectFirstItem();
            }

            base.OnLoad(e);
        }

        private void b1Open_Click(object sender, EventArgs e)
        {
            curFileExtension = "";
            macroList.ListCreate();

            Brep b = null;
            if (!importExport.ImportEyeshot(ref b))
                return;
            if (b == null)
            {
                MessageBox.Show("Folder doesn't contain any .eye file");
                return;
            }

            curFileExtension = ".EYE";

            design1.Entities.Clear();
            design1.Entities.Add(b, EyeUtils.workedPiece);
            design1.Invalidate();
        }

        private void b2Import_Click(object sender, EventArgs e)
        {
            curFileExtension = "";
            macroList.ListCreate();

            EyeWorkPiece eyewp = (wpDynamicObj.wp as EyeWorkPiece);
            LoadStlStepDstv(null, ref eyewp);
        }

        private void b3Save_Click(object sender, EventArgs e)
        {
            if (!importExport.ExportEyeshot(wpDynamicObj.exportEntities))
                return;
        }

        private void b4Export_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();

            saveFileDialog.Filter = "STL files (*.stl)|*.stl|Standard for the Exchange of Product Data (*.step)|*.step|XML files (*.xml)|*.xml";
            saveFileDialog.AddExtension = true;
            saveFileDialog.CheckPathExists = true;

            if (saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            string fileName = saveFileDialog.FileName;
            string extension = Path.GetExtension(fileName).ToUpper();

            if (extension == ".STL")
            {
                if (!importExport.ExportSTL(wpDynamicObj.finalPart, eyeParam.Tol.Brep, false, fileName))
                    return;
            }
            else if (extension == ".STEP")
            {
                if (!importExport.ExportSTEP(wpDynamicObj.finalPart, design1, fileName))
                    return;
            }
            else if (extension == ".XML")
            {
                if (!importExport.ExportFicepXML(fileName, eyeParam.Tol.Brep, wpDynamicObj.wp, wpDynamicObj.macros.Select(m => m as EyeMacro).ToList(), wpDynamicObj.finalPart, processAsPieceCutToMeasure, ref importExport, ref xmlInterface))
                    return;
            }

            // Perchè toggliere le entities e poi rimetterle?
            if (false)
            { 
                design1.Entities.Clear();
                if (wpDynamicObj.finalPart != null)
                {
                    design1.Entities.Add(wpDynamicObj.finalPart.Clone() as Entity, EyeUtils.workedPiece);

                    //
                    //  Calcolo le features e i vettori dell'EyeWorkPiece a partire dalla finalPart
                    //
                    EyeUtils.ComputeEyeWorkPieceFeaturesAndVectorsFromBrep(wpDynamicObj.wp as EyeWorkPiece, eyeParam.Tol.Brep, eyeParam.Tol.Linear, xmlInterface.FeatureExtractionParam.ArcSegmentLength, log, ref wpDynamicObj.finalPart, out List<Line> lines, out List<Line> oppositeLines);
                    design1.Entities.AddRange(lines, EyeUtils.vectors);
                    design1.Entities.AddRange(oppositeLines, EyeUtils.vectors);

                }
                design1.Invalidate();
            }
        }
        private void b5FNC_Click(object sender, EventArgs e)
        {
            curFileExtension = "";
            macroList.ListCreate();

            LoadFnc(null);
        }

        private void b6ShowFeatures_Click(object sender, EventArgs e)
        {
            // Vecchia logica per mostrare le features
            if (false)
            {
                if (!showFeatures)
                {
                    design1.Entities.AddRange(wpDynamicObj.exportEntities.Skip(1), EyeUtils.differenceSolids, Color.FromArgb(50, Color.Green));
                    design1.Invalidate();
                    //design1.Layers.Where(l => l.Name == EyeUtils.differenceSolids).First().Visible = true;
                }
                else
                {
                    foreach (var entity in wpDynamicObj.exportEntities)
                        design1.Entities.Remove(entity);

                    //design1.Layers.Where(l => l.Name == EyeUtils.differenceSolids).First().Visible = false;
                    design1.Invalidate();
                }
            }

            showFeatures = !showFeatures;
            design1.Invalidate();
            design1.Layers.Where(l => l.Name == EyeUtils.differenceSolids).First().Visible = showFeatures;
        }


        private void CreateMacroAndEntities ()
        {
            if (wpDynamicObj.macros.Count > 0)
            {
                // Clear the previus solids
                wpDynamicObj.exportEntities.Clear();

                if (wpDynamicObj.macros[0].Wp is EyeWorkPiece wp)
                {
                    if (wp.Solid == null)
                        return;

                    wpDynamicObj.finalPart = (Brep)(wp).Solid.Clone();

                    wpDynamicObj.exportEntities.Add(wpDynamicObj.finalPart);
                    design1.Entities.Clear();

                    xmlInterface.FeatureExtractionParam.EnableWorkpieceVectors = enableWorkpieceVectors;

                    if (xmlInterface.FeatureExtractionParam.EnableWorkpieceVectors)
                    {
                        if (!CreateFinalPartComputingVectorsAtTheEnd())
                            log.Debug("Failed CreateFinalPartComputingVectorsAtTheEnd()");
                    }
                    else
                        CreateFinalPart(wp);
                }
            }
            else
            {
                if (wpDynamicObj.wp is EyeWorkPiece && ((EyeWorkPiece)wpDynamicObj.wp).Solid != null)
                    wpDynamicObj.finalPart = (Brep)((EyeWorkPiece)wpDynamicObj.wp).Solid.Clone();

                design1.Entities.Clear();
            }

            if (wpDynamicObj.finalPart != null)
                design1.Entities.Add(wpDynamicObj.finalPart.Clone() as Entity, EyeUtils.workedPiece);
        }

        private void LoadStlStepDstv (string stlStepDstvFile, ref EyeWorkPiece eyeWp)
        {
            Mesh m = null;
            Brep b = null;
            List<Brep> scraps;

            if (!importExport.ImportStlStep(eyeParam.Tol.Brep, ref m, ref b, ref eyeWp, ref stlStepDstvFile, out scraps))
                return;

            
            if (stlStepDstvFile != null)
            {
                string fileName = Path.GetFileName(stlStepDstvFile);
                this.Text = fileName + " - Ficep RobServer";
            }
            else
                this.Text = "Ficep RobServer";

            curFileExtension = Path.GetExtension(stlStepDstvFile).ToUpper();
            macroList.ListCreate();

            if (m != null)
            {
                design1.Entities.Clear();
                design1.Entities.Add(m, EyeUtils.workedPiece);
                design1.Invalidate();
            }
            else if (b != null)
            {
                design1.Entities.Clear();
                wpDynamicObj.finalPart = b;
                design1.Entities.Add(wpDynamicObj.finalPart.Clone() as Brep, EyeUtils.workedPiece, Color.Gray);
                if (scraps != null) 
                    design1.Entities.AddRange(scraps, EyeUtils.differenceSolids,Color.FromArgb(40, Color.Green));
                design1.Invalidate();
                Tol tol = eyeParam.Tol;

                if (eyeWp != null)
                {
                    macroList.wpDynamicObj.wp = eyeWp;
                    wpDynamicObj.wp = eyeWp;

                    if(EyeUtils.ComputeEyeWorkPieceFeaturesAndVectorsFromBrep(eyeWp, tol.Brep, tol.Linear, xmlInterface.FeatureExtractionParam.ArcSegmentLength, log,
                                                                           ref b, out List<Line> lines, out List<Line> oppositeLines))
                    { 
                        design1.Entities.AddRange(lines, EyeUtils.vectors);
                        design1.Entities.AddRange(oppositeLines, EyeUtils.vectors);
                    }
                }
            }
            else
            {
                MessageBox.Show("Incompatible file");
                return;
            }
        }

        private void LoadFnc(string fncFile)
        {
            wpDynamicObj.wp = new EyeWorkPiece();

            try
            {
                ImportFNC();
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }

            curFileExtension = ".FNC";

            CreateMacroAndEntities();

            if (fncFile != null) 
            {
                string fileName = Path.GetFileName(fncFile);
                this.Text = fileName + " - Ficep RobServer";
            }
            else
                this.Text = "Ficep RobServer";

            design1.Invalidate();

            design1.ZoomFit();

            if (showMacroList)
            {
                if (macroList != null)
                {
                    macroList.ListPopulate();
                    macroList.ListSelectFirstItem();
                }
            }
        }
        
        // Compute the final part computing the vectors just on the macro one by one 
        private void CreateFinalPart(EyeWorkPiece wp)
        {
            var mac = wpDynamicObj.macros.Where(m => m is IMacro).Select(x => x as EyeMacro);
            foreach (var macro in mac)
            {
                if (macro == null) 
                    continue;

                List<Line> lines = new List<Line>();
                // Create the solid to be subtracted
                macro.CreateMacro();

                for (int i = 0; i < macro.Features.Count; i++)
                {
                    EyeFeature feature = macro.Features[i];

                    feature.FaceList = new List<List<EyeCuttingEdge>>();
                    List<List<EyeCuttingEdge>> faceList = feature.FaceList;

                    Brep intersection = null;
                    try
                    {
                        intersection = Brep.Intersection(wpDynamicObj.finalPart, feature.Solid)?.FirstOrDefault();
                        Brep temp = Brep.Difference(wpDynamicObj.finalPart, feature.Solid)?.FirstOrDefault();

                        if (temp != null)
                        {
                            wpDynamicObj.finalPart = temp;
                            wpDynamicObj.exportEntities.AddRange(macro.Features.Select(f => f.Solid));
                        }
                        // Compute the cutting vectors
                        EyeUtils.ComputeVectorsOld(intersection, wpDynamicObj.finalPart, ref faceList, xmlInterface.FeatureExtractionParam.ArcSegmentLength);

                        foreach (var edgeList in faceList)
                        {
                            foreach (var edge in edgeList)
                            {
                                edge.ConvertToLineEdgeOld(wpDynamicObj.finalPart, eyeParam.Tol.Linear, wp, out List<EyeLineEdge> ficepEdges);
                                foreach (var ficepEdge in ficepEdges)
                                {
                                    Line l1 = new Line(ficepEdge.StartPoint, ficepEdge.StartPoint + ficepEdge.V1Start);
                                    Line l2 = new Line(ficepEdge.StartPoint, ficepEdge.StartPoint + ficepEdge.V2Start);
                                    Line l3 = new Line(ficepEdge.EndPoint, ficepEdge.EndPoint + ficepEdge.V1End);
                                    Line l4 = new Line(ficepEdge.EndPoint, ficepEdge.EndPoint + ficepEdge.V2End);
                                    lines.Add(l1);
                                    lines.Add(l2);
                                    lines.Add(l3);
                                    lines.Add(l4);
                                }
                                feature.EdgeList.AddRange(ficepEdges);
                            }
                        }
                        design1.Entities.AddRange(lines, EyeUtils.vectors, Color.OrangeRed);
                    }
                    catch (Exception e)
                    {
                        log.Error(macro.MacroName, e);
                    }
                }
            }
        }

        //
        //  Sottrae tutte le Features di una macro dal Brep
        //
        private bool SubtractMacroFromBrep (EyeMacro macro, ref Brep brep)
        {
            foreach (var f in macro.Features)
            {
                try
                {
                    Brep temp = Brep.Difference(brep, f.Solid)?.FirstOrDefault();

                    if (temp != null)
                    {
                        brep = temp;
                    }
                    else
                        log.Error("Failed Brep.Difference() during " + macro.MacroName);
                }
                catch (Exception e)
                {
                    log.Error(macro.MacroName, e);
                }
            }

            return true;
        }

        //
        //  Sottrae tutte le Features di una macro dal Brep
        //
        private bool SubtractMacroFromBrep(EyeMacroLung macro, ref Brep brep)
        {
            foreach (var f in macro.Features)
            {
                try
                {
                    Brep temp = Brep.Difference(brep, f.Solid)?.FirstOrDefault();

                    if (temp != null)
                    {
                        brep = temp;
                    }
                    else
                        log.Error("Failed Brep.Difference() during " + macro.MacroName);
                }
                catch (Exception e)
                {
                    log.Error(macro.MacroName, e);
                }
            }

            return true;
        }

        private bool CreateFinalPartComputingVectorsAtTheEnd()
        {
            //
            //  Filtro le sole macro di tipo EyeMacro
            //
            List<EyeMacro> eyeMacroList = wpDynamicObj.macros.Where(m => m is EyeMacro).Select(x => x as EyeMacro).ToList();
            //
            //  Filtro le sole macro di tipo EyeMacroLung
            //
            List<EyeMacroLung> eyeMacroLungList = wpDynamicObj.macros.Where(m => m is EyeMacroLung).Select(x => x as EyeMacroLung).ToList();

            //
            //  Sottrraggo ad una ad una le macro filtrate dalla finalPart
            //
            foreach (var eyeMacro in eyeMacroList)
            {
                //
                //  Creo le Features della macro
                //
                eyeMacro.CreateMacro();

                //
                //  Inserisco i Brep delle Features nella lista di tutti i Brep se la macro è di tipo EyeMacro
                //
                wpDynamicObj.exportEntities.AddRange(eyeMacro.Features.Select(x => x.Solid));

                //
                //  Aggiorno la finalPart sottraenedo le Features della macro
                //
                SubtractMacroFromBrep(eyeMacro, ref wpDynamicObj.finalPart);
                    
            }

            //
            // Disegno le curve rappresentati le lung
            //
            foreach (var eyeMacroLung in eyeMacroLungList)
            {
                eyeMacroLung.CreateMacro();

                //
                //  Calcolo le curve che andranno mostrate nel form dovute al taglio di separazione
                //
                eyeMacroLung.GetDrawingCurves(wpDynamicObj.finalPart, out List<ICurve> drawingCurves);
                design1.Entities.AddRange(drawingCurves.Select(x => x as Entity), EyeUtils.macroLungCurves, Color.Orange);

                //
                //  Inserisco i Brep delle Features nella lista di tutti i Brep se la macro è di tipo EyeMacro
                //
                wpDynamicObj.exportEntities.AddRange(eyeMacroLung.Features.Select(x => x.Solid));

                //
                //  Aggiorno la finalPart sottraenedo le Features della macro
                //
                SubtractMacroFromBrep(eyeMacroLung, ref wpDynamicObj.finalPart);
            }
;
            //
            //  Calcolo le features e i vettori dell'EyeWorkPiece a partire dalla finalPart
            //
            EyeUtils.ComputeEyeWorkPieceFeaturesAndVectorsFromBrep(wpDynamicObj.wp as EyeWorkPiece, eyeParam.Tol.Brep, eyeParam.Tol.Linear, xmlInterface.FeatureExtractionParam.ArcSegmentLength, log, ref wpDynamicObj.finalPart, out List<Line> lines, out List<Line> oppositeLines);
            if (lines != null)
            {
                design1.Entities.AddRange(lines, EyeUtils.vectors);
                design1.Entities.AddRange(oppositeLines, EyeUtils.vectors);
            }

            return true;
        }

        //
        //  L'override della WndProc serve per gestire i messaggi di richiesta
        //  di chiusura applicazione da applicativi esterni
        //
        const int WM_ROBSERVER_CLOSEAPP = 0x8000; // Custom message value
        const int WM_CLOSE = 0x0010;
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_ROBSERVER_CLOSEAPP || m.Msg == WM_CLOSE)
                Environment.Exit(0);

            base.WndProc(ref m);
        }
        
        

        private void DragEnter (object sender, DragEventArgs e)
        {
            // Check if the data being dragged contains file drop information
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void DragDrop (object sender, DragEventArgs e)
        {
            curFileExtension = "";

            // Get the array of file paths dropped onto the ListView
            string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);

            // Process each file
            foreach (string filePath in filePaths)
            {
                // Get file information
                FileInfo fileInfo = new FileInfo(filePath);

                curFileExtension = Path.GetExtension(filePath).ToUpper();

                if (curFileExtension == ".FNC")
                {
                    fncPath = fileInfo.FullName;
                    LoadFnc(fncPath);
                }
                else if (curFileExtension == ".STL" || curFileExtension == ".STP" || curFileExtension == ".STEP")
                {
                    string stpPath = fileInfo.FullName;
                    EyeWorkPiece eyewp = (wpDynamicObj.wp as EyeWorkPiece);

                    LoadStlStepDstv(stpPath, ref eyewp);
                    wpDynamicObj.wp = eyewp;
                }
            }
        }

        //
        //  Gestione drag and drop su controllo Eyeshot
        //
        private void OnDragEnterEyeControl(object sender, DragEventArgs e)
        {
            DragEnter(sender, e);
        }

        private void OnDragDropEyeControl(object sender, DragEventArgs e)
        {
            DragDrop(sender, e);
        }
        
        private void b7Measure_Click(object sender, EventArgs e)
        {
            if (wpDynamicObj.finalPart == null)
                return;

            MeasureForm measureForm = new MeasureForm(wpDynamicObj.finalPart.Clone() as Brep);
            measureForm.ShowDialog();
        }

        private void ImportFNC()
        {
            if (!isJsonIniFilePath)
            {
                if (!importExport.ImportFNC(robotIni, null, ref wpDynamicObj.wp, ref wpDynamicObj.macros, eyeParam, fncPath))
                    return;
            }
            else 
            {
                if (!importExport.ImportFNC(null, macroLibGroup, ref wpDynamicObj.wp, ref wpDynamicObj.macros, eyeParam, fncPath))
                    return;
            }
        }

        private void Test ()
        {
            Milling milling = new Milling();
            milling.Test();
        }
    }
}

