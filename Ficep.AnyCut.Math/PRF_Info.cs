
namespace Ficep.AnyCut.Mathematics
{
    public class PRF_Info
    {
        public double Ha { get; set; }                  //	Altezza piano A
        public double Hb {get; set;}                  //	Altezza piano B
        public double Hc {get; set;}                  //	Altezza piano C
        public double Ta {get; set;}                  //	Spessore piano A
        public double Tb {get; set;}                  //	Spessore piano B
        public double Tc {get; set;}                  //	Spessore piano C
        public double Hc_left;         //	Altezza estensione piano C sinistra (profilo O)
        public double Hc_right;            //	Altezza estensione piano C destra (profilo O)
        public double Radius;              //	Raggio nocciolo
        public double Dsa;             //	Disassamento piano A (profili D)
        public double Dsb;             //	Disassamento piano B (profili D)
        public string Profilo;     //	Nome del profilo
        public char CodPrf;             //  Codice del profilo
    }
}
