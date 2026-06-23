using System;
using System.Windows.Forms;
using devDept.Geometry;

namespace FicepControls
{
    public partial class MeasurePanel : UserControl
    {
        private const string DecimalFormattingLong = "0.000";
        private const string DecimalFormatting = "0.000";

        private linearUnitsType _units;
        private string _unitAbbreviation;

        /// <summary>
        /// Gets or set blocks unit system
        /// </summary>
        public linearUnitsType Units
        {
            get
            {
                return _units;
            }

            set
            {
                _units = value;
                _unitAbbreviation = GetUnitAbbreviation();
            }
        }

        /// <summary>
        /// Gets unit abbreviation
        /// </summary>
        public string UnitAbbreviation => _unitAbbreviation;

        public MeasurePanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Display the given measure
        /// </summary>
        /// <param name="distance">Distance between point A and B</param>
        /// <param name="ptA">Point A</param>
        /// <param name="ptB">Point B</param>
        public void SetMeasure(double distance, Point3D ptA, Point3D ptB)
        {
            if(!double.IsNaN(distance))
            {
                txtDistance.Text = $"{distance.ToString(DecimalFormattingLong)} {UnitAbbreviation}";

                txtDX.Text = $"{Math.Abs(ptA.X - ptB.X).ToString(DecimalFormatting)} {UnitAbbreviation}";
                txtDY.Text = $"{Math.Abs(ptA.Y - ptB.Y).ToString(DecimalFormatting)} {UnitAbbreviation}";
                txtDZ.Text = $"{Math.Abs(ptA.Z - ptB.Z).ToString(DecimalFormatting)} {UnitAbbreviation}";
            }
            else
            {
                txtDistance.Text = txtDX.Text = txtDY.Text = txtDZ.Text = "-";
            }
        }

        private string GetUnitAbbreviation()
        {
            switch (Units)
            {
                case linearUnitsType.Unitless:
                    return "";
                case linearUnitsType.Inches:
                    return "in";
                case linearUnitsType.Feet:
                    return "ft";
                case linearUnitsType.Miles:
                    return "mi";
                case linearUnitsType.Millimeters:
                    return "mm";
                case linearUnitsType.Centimeters:
                    return "cm";
                case linearUnitsType.Meters:
                    return "m";
                case linearUnitsType.Kilometers:
                    return "Km";
                case linearUnitsType.Microinches:
                    return "µin";
                case linearUnitsType.Mils:
                    return "mils";
                case linearUnitsType.Yards:
                    return "yd";
                case linearUnitsType.Angstroms:
                    return "Å";
                case linearUnitsType.Nanometers:
                    return "nm";
                case linearUnitsType.Microns:
                    return "μm";
                case linearUnitsType.Decimeters:
                    return "dm";
                case linearUnitsType.Decameters:
                    return "dam";
                case linearUnitsType.Hectometers:
                    return "hm";
                case linearUnitsType.Gigameters:
                    return "Gm";
                case linearUnitsType.Astronomical:
                    return "au";
                case linearUnitsType.LightYears:
                    return "ly";
                case linearUnitsType.Parsecs:
                    return "pc";
                case linearUnitsType.NotSupported:
                    return "?";
                default:
                    return "";
            }
        }
    }
}
