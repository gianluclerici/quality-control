using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Ficep.AnyCut.Common
{
    //
    //  ENUMERATORI
    //

    //  Tipologia macchina Ficep
    public enum FicepMachineType : uint
    {
        Unknown = 0,
        GeminiLaser = 1,
        GeminiPlasma = 2,
        Robot = 3
    }

    //  Lato di compensazione raggio utensile
    public enum ToolSideComp : uint
    {
        None = 40,
        Left = 41,
        Right = 42,
    }

    //  Tipologia Path
    public enum PathType : uint
    {
        Default = 0,
        Positioning = 1,
        Cutting = 3,
        Marking = 4,
        TrueHole = 5,
        Scribing = 6,
        Milling = 7
    }

    // Tipologia di lavorazione marcatura per laser
    public enum MarkProcessingType
    {
        NotDefined,
        Hard,
        Film,
        Soft = 4
    }

    // Tipologia di lavorazione taglio per laser
    public enum CutProcessingType
    {
        NotDefined = 0,
        Fast = 1,
        Medium = 2,
        // Value 3 intentionally left out
        Slow = 4
    }

    //  Posizione Path
    public enum PathPosition: uint 
    {
        Taglio = 0,
        Iniziale = 1,
        Finale = 2,
        Interno = 3,
        Lung = 4
    }


    //  Tipologia Point
    public enum PointType : uint
    {
        Pierce = 0,
        Line = 1,
        Arc = 2,
        Probing = 3,
        Home = 4,
        Axes = 5
    }

    //  Tipologia Material
    public enum MaterialType : uint
    {
        Default,
        MildSteel,
        StainlessSteel,
        HardSteel
    }
    //  Tipologia Lead-In/Out
    public enum LeadInOutType : uint
    {
        None,
        Line,
        Arc
    }
    //  Tipologia tecnologia di taglio
    public enum CuttingTool : uint
    {
        Default = 0,
        Plasma = 1,
        Oxycutting = 2,
        Laser = 3
    }

    //
    //  ENUMERATORI
    //
    //  Criterio di ordinamento
    public enum SortCriterion : uint
    {
        SortByOriginalSeq = 0,  //	Ordinamento che mantiene esattamente la sequenza in ingresso
        SortByX = 1,            //	Ordinamento per quota X
        SortBySetup = 2,        //	Ordinamento per valori di Setup
        SortByNp = 100          //	Ordinamento per NP (utilizzata solo internamente)
    }

    [DataContract]
    [XmlType("PROFILE")]
    public class Profile
    {
        [DataMember]
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [DataMember]
        [XmlAttribute("Code")]
        public string Code { get; set; }

        [DataMember]
        [XmlAttribute("HA")]
        public double HA { get; set; }

        [DataMember]
        [XmlAttribute("TA")]
        public double TA { get; set; }

        [DataMember]
        [XmlAttribute("HB")]
        public double HB { get; set; }

        [DataMember]
        [XmlAttribute("TB")]
        public double TB { get; set; }

        [DataMember]
        [XmlAttribute("HC")]
        public double HC { get; set; }

        [DataMember]
        [XmlAttribute("TC")]
        public double TC { get; set; }

        [DataMember]
        [XmlAttribute("Radius")]
        public double Radius { get; set; }

        public Profile()
        {
            Name = "-";
            Code = "-";
            HA = TA = HB = TB = HC = TC = Radius = 0;
        }

        public Profile(string name, string code, double hA, double tA, double hB, double tB, double hC, double tC, double radius)
        {
            Name = name;
            Code = code;
            HA = hA;
            TA = tA;
            HB = hB;
            TB = tB;
            HC = hC;
            TC = tC;
            Radius = radius;
        }

        //
        //  Converte le dimensioni del profilo nel formato FNC
        //
        public bool GetFncProfileDimensions(ref double sa, ref double ta, ref double sb, ref double tb, ref double radius)
        {
            radius = Radius;

            if (Code == "R")
            {
                sa = HC;
                ta = TC;
            }
            else if (Code =="L")
            {
                sa = HA;
                ta = TA;
                sb = HB;
                tb = TB;
            }
            else
            {
                sa = HC;
                ta = TC;
                sb = HA;
                tb = TA;
            }

            return true;
        }
    }

    [DataContract]
    [XmlType("MATERIAL")]
    public class Material
    {
        [DataMember]
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [DataMember]
        [XmlAttribute("Type")]
        public MaterialType Type { get; set; }

        public Material()
        {
            Name = "-";
            Type = MaterialType.Default;
        }

        public Material(string name, MaterialType type)
        {
            Name = name;
            Type = type;
        }
    }

    [DataContract]
    [XmlType("DIMENSION")]
    public class Dimension
    {
        [DataMember]
        [XmlAttribute("Length")]
        public double Length { get; set; }

        public Dimension()
        {
            Length = 0;
        }
        public Dimension(double length)
        {
            Length = length;
        }
    }

    //
    //  STOCK_ITEM
    //
    [DataContract]
    [XmlType("STOCK_ITEM")]
    public class StockItem
    {
        [DataMember]
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [DataMember]
        [XmlAttribute("Quantity")]
        // Messa sempre a 1
        public uint Quantity { get; set; }

        [DataMember]
        [XmlElement("MATERIAL")]
        public Material Material { get; set; }

        [DataMember]
        [XmlElement("PROFILE")]
        public Profile Profile { get; set; }

        [DataMember]
        [XmlElement("DIMENSION")]
        public Dimension Dimension { get; set; }

        public StockItem()
        {
            Name = "";
            Quantity = 0;
            Material = new Material();
            Profile = new Profile();
            Dimension = new Dimension();
        }

        public StockItem(uint quantity, Material material, Profile profile, double length)
        {
            Name = "S_" + profile.Name;
            Quantity = quantity;

            Material = material;
            Profile = profile;

            Dimension = new Dimension();
            Dimension.Length = length;
        }
    }

    //
    //  SEPCUT
    //
    [DataContract]
    [XmlType("SEPCUT")]
    public class SepCut
    {
        [DataMember]
        [XmlAttribute("Name")]
        public string Name { get; set; }

        [DataMember]
        [XmlAttribute("NP")]
        public uint NP { get; set; }

        [DataMember]
        [XmlAttribute("NestingIdentifier")]
        public uint NestingIdentifier { get; set; }

        [DataMember]
        [XmlAttribute("X")]
        public double X { get; set; }

        [DataMember]
        [XmlAttribute("ANGA")]
        public double ANGA { get; set; }

        [DataMember]
        [XmlAttribute("ANGB")]
        public double ANGB { get; set; }
        
        [DataMember]
        [XmlAttribute("PieceCutting")]
        public bool InitialPieceCutting { get; set; }

        [DataMember]
        [XmlAttribute("Shared")]
        public bool Shared { get; set; }

        [DataMember]
        [XmlAttribute("Stitched")]
        public bool Stitched { get; set; }

        [DataMember]
        [XmlAttribute("SecondTAGLIO")]
        public bool SecondTAGLIO { get; set; }
        public bool ReqStitchesTAGLIOSecondaryRules { get; set; }
        public bool LastReqFinalPathRepos { get; set; }
        public uint TotalNumberPath { get; set; }
        public uint CounterPath { get; set; }

        public SepCut()
        {
            Name = "";
            NP = 0;
            NestingIdentifier = 0;
            X = 0;
            ANGA = ANGB = 0;
            InitialPieceCutting = Shared = Stitched = SecondTAGLIO = 
                ReqStitchesTAGLIOSecondaryRules = LastReqFinalPathRepos = false;
            TotalNumberPath = CounterPath = 0;
        }

        public SepCut(string name, uint prevSepCutNP, uint np, uint nestingIdentifier, double x, double angA, double angB, bool initialPieceCutting, bool shared): this()
        {
            Name = name;
            NP = np;
            NestingIdentifier = nestingIdentifier;

            if (prevSepCutNP == np)
                SecondTAGLIO = true;

            X = x;
            ANGA = angA;
            ANGB = angB;
            InitialPieceCutting = initialPieceCutting;
            Shared = shared;
            TotalNumberPath = CounterPath = 0;
        }

        public SepCut Clone()
        {
            // Serialize the object to JSON and deserialize it to create a deep clone.
            string jsonString = SerializeObject.SerializeToJSONString(this);

            return (SepCut)SerializeObject.DeserializeJSONStringToObject<SepCut>(jsonString);
        }

        public void MarkPathExecution ()
        {
            CounterPath = Math.Max(CounterPath - 1, 0);
        }

        public bool AllPathExecuted ()
        {
            return CounterPath <= 0;
        }

        //
        //	Creo l'informazione sulla posizione dello sfrido:
        //
        //	SfridoSx	= true se (visto dal lato piano A) lo sfrido si trova a sinistra del TAGLIO
        //
        //	SfridoSx	= true se il taglio è iniziale oppure il taglio non è condiviso (default)
        //
        public bool IsSfridoSx(bool macchinaSx)
        {
            bool condiviso = Shared, taglioIniziale = InitialPieceCutting;
            bool sfridoSx = (condiviso || !condiviso && taglioIniziale);
            if (macchinaSx)
                sfridoSx = !sfridoSx;

            return sfridoSx;
        }

    }

    //
    //  Classe per la memorizzazione dei tempi stimati da AnyCut
    //
    public class EstimatedTimes
    {
        //  Tempo totale (è la somma di tutti i tempi parziali)
        public double TotalTime { get; set; }
        //  Tempo di riposizionamento robot
        public double RobotPositioningTime { get; set; }
        //  Tempo di taglio plasma
        public double CuttingTime { get; set; }
        //  Tempo di marcatura plasma
        public double PlasmaMarkingTime { get; set; }
        //  Tempo di piercing
        public double PierceTime { get; set; }
        //  Tempo di palpatura
        public double ProbingTime { get; set; }
        //  Tempo di bloccaggio pezzo (morese / staffaggi)
        public double ClampingTime { get; set; }
        //  Tempo di scarico pezzi
        public double UnLoadingTime { get; set; }
        //  Tempo di carico della barra
        public double LoadingTime { get; set; }
        //  Tempi ausiliari (non riconducibili ad altri tempi, come ad esempio
        //  i tempi per l'invio del programma via FTP)
        public double AuxiliaryTime { get; set; }
        //  Tempo di movimentazione asse X come posizionatore
        public double ExtXTime { get; set; }
        //  Tempo di cambio utensile
        public double ToolChangeTime { get; set; }
        //  Tempo di taglio ossitaglio
        public double OxyCuttingTime { get; set; }

        public EstimatedTimes()
        {
            Reset();
        }
        public void Reset()
        {
            TotalTime = 0;
            RobotPositioningTime = 0;
            CuttingTime = 0;
            PlasmaMarkingTime = 0;
            PierceTime = 0;
            ProbingTime = 0;
            ClampingTime = 0;
            UnLoadingTime = 0;
            LoadingTime = 0;
            AuxiliaryTime = 0;
            ExtXTime = 0;
            ToolChangeTime = 0;
            OxyCuttingTime = 0;
        }
    }

}
