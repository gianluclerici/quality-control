using Ficep.RobServer.Data;
using Ficep.MacroGra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using ListView = System.Windows.Forms.ListView;
using devDept.Eyeshot.Entities;
using Ficep.RobServer.Utility3D;
using System.Reflection;
using Ficep.RobServer.MacroParser;
using TextBox = System.Windows.Forms.TextBox;
using Button = System.Windows.Forms.Button;
using Ficep.RobServer.ImportExport;
using System.IO;
using FicepXml;

namespace Ficep.RobServer
{
    //
    //  Item della lista di macro
    //
    public class ItemMacroList
    {
        public IMacroCope Macro;
        public int Id { get; set; }
        public bool Enabled { get; set; }

        public ItemMacroList(IMacroCope macro, int id, bool enabled)
        {
            Macro = macro;
            Id = id;
            Enabled = enabled;
        }
    }

    //
    //  Form che contiene un controllo di editing multilinea
    //
    public class EditForm : Form
    {
        public System.Windows.Forms.TextBox editText;
        private System.Windows.Forms.Button okButton;
        public bool singleLine = true;

        public EditForm(bool singleLine = true)
        {
            this.singleLine = singleLine;
            // Initialize the form and controls
            InitializeComponents();
        }
        private void InitializeComponents()
        {
            // Set the title of the form
            Text = "Edit macro";

            // Set the dimensions of the form
            Width = 800;
            Height = 150;

            editText = new TextBox
            {
                Multiline = true,
                Dock = DockStyle.Fill
            };

            okButton = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Bottom
            };

            Controls.Add(editText);
            Controls.Add(okButton);

            // Center the form on the screen
            StartPosition = FormStartPosition.CenterScreen;

            editText.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnKeyDown);
        }

        private void OnKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                // Handle the Escape key press
                this.Close();

                e.Handled = true; // Set this to true to prevent the TextBox from processing the Enter key
            }
            else if (e.KeyCode == Keys.Return)
            {
                if (singleLine)
                {
                    // Set DialogResult to OK and close the form
                    DialogResult = DialogResult.OK;
                    this.Close();

                    e.Handled = true; // Set this to true to prevent the TextBox from processing the Enter key
                }
            }
        }

        // Property to access the edited text from the main form
        public string EditedText => editText.Text;
    }

    public class MacroList
    {
        public ListView listView = null;
        private devDept.Eyeshot.Control.Design design1 = null;
        public WpDynamicObjects wpDynamicObj;
        private EyeParam eyeParam = null;
        private string fileIniPath = "";
        private string pictureFolder = "";
        private Importer importExport = null;
        private readonly log4net.ILog log = null;
        public double tolBrep = 0;
        public double tolLinear = 0;
        public double arcSegmentLength = 0;
        PictureBox pictureBox = null;
        public IniReader robotIni;
        public List<MacroLibGroup> macroLibGroupList;
        private bool isJsonIniFilePath; // Indica se il percorso del file di configurazione è un json

        //
        //  Flag che diventa true durante l'esecuzione della ListPopulate
        //
        public bool ListIsPopulating = false;
        private ContextMenuStrip ListcontextMenuStrip = null;
        private int ColumnIndex = -1;

        public MacroList(
            ListView listView,
            WpDynamicObjects wpDynamicObj,
            devDept.Eyeshot.Control.Design design1,
            Importer importExport,
            EyeParam eyeParam,
            PictureBox pictureBox,
            string fileIniPath,
            string pictureFolder,
            double arcSegmentLength,
            log4net.ILog log,
            List<MacroLibGroup> macroLibGroupList)
        {
            this.listView = listView;
            this.design1 = design1;
            this.wpDynamicObj = wpDynamicObj;
            this.eyeParam = eyeParam;
            this.importExport = importExport;
            this.pictureBox = pictureBox;
            this.fileIniPath = fileIniPath;
            this.pictureFolder = pictureFolder;
            tolBrep = eyeParam.Tol.Brep;
            tolLinear = eyeParam.Tol.Linear;
            this.arcSegmentLength = arcSegmentLength;
            this.log = log;
            isJsonIniFilePath = fileIniPath.Contains(".json", StringComparison.CurrentCultureIgnoreCase); 

            if (isJsonIniFilePath)
            { 
                this.macroLibGroupList = macroLibGroupList;
                robotIni = null;
            }
            else
            {
                robotIni = new IniReader(fileIniPath);
                this.macroLibGroupList = null;
            }

            ListCreate();
        }

        //
        //  Sottrae tutte le Features di una macro dal Brep
        //
        public bool SubtractMacroFromBrep(IEyeMacro macro, log4net.ILog log, ref Brep brep)
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

        public bool ContextMenuCreate()
        {
            ////////////////////
            //
            //  CONTEXT MENU
            // 
            ////////////////////
            if (ListcontextMenuStrip == null)
            {
                ListcontextMenuStrip = new ContextMenuStrip();
                ListcontextMenuStrip.Items.Add("Edit", null, ListContextMenuItem_Click);
                ListcontextMenuStrip.Items.Add("Add", null, ListContextMenuItem_Click);
                ListcontextMenuStrip.Items.Add("Delete", null, ListContextMenuItem_Click);
            }

            return true;
        }

        //
        //  Creazione della ListView
        //
        public bool ListCreate()
        {
            if (listView == null)
                return false;

            ////////////////////
            //
            //  LISTVIEW
            //
            ////////////////////
            //
            //  Rimuove tutte gli ITEMS e le COLUMNS
            //
            listView.Clear();

            //
            //  Aggiunge le COLUMNS
            //
            listView.Columns.Add("ID");
            listView.Columns.Add("Name");
            listView.Columns.Add("Side");
            listView.Columns.Add("VX");
            listView.Columns.Add("VY");

            listView.Columns[0].Width = 50;
            listView.Columns[1].Width = 100;
            listView.Columns[2].Width = 50;
            listView.Columns[3].Width = 50;
            listView.Columns[4].Width = 50;

            // Enable drag and drop
            listView.AllowDrop = true;

            ////////////////////
            //
            //  CONTEXT MENU
            // 
            ////////////////////
            ContextMenuCreate();

            LoadBitmap("");

            return true;
        }

        //
        //  Popola la ListView delle macro a partire dalla lista passata
        //
        public bool ListPopulate()
        {
            if (listView == null || wpDynamicObj.macros == null)
                return false;

            if (!ListCreate())
                return false;

            List<IMacroCope> macroList = wpDynamicObj.macros.Where(m => m is IMacroCope).Select(x => x as IMacroCope).ToList();

            if (macroList == null || macroList.Count == 0)
                return false;

            LoadBitmap("");

            ListIsPopulating = true;

            int macroCounter = 0;

            // Bind the list of objects to the ListView
            foreach (var macro in macroList)
            {
                macroCounter++;
            
                var itemMacroList = new ItemMacroList(macro, macroCounter, true);
                var listItem = new ListViewItem(itemMacroList.Id.ToString());
                listItem.SubItems.Add(macro.MacroName);
                listItem.SubItems.Add(macro.Params.SIDE);
                listItem.SubItems.Add(macro.Params.VX);
                listItem.SubItems.Add(macro.Params.VY);
                listItem.Tag = itemMacroList;// Associate macro with ListViewItem using Tag
                listItem.Checked = true;
            
                // Add the new item to the ListView
                listView.Items.Add(listItem);
            }

            ListIsPopulating = false;

            return true;
        }

        //
        //  -   Popola la ListView delle macro a partire da una lista di IMacro
        //  -   Crea la FinalPart a partire dalle macro della ListView
        //  -   Seleziona il primo elemento della ListView
        //  -   Evidenzia graficamente l'elemnto selezionato disegnandone il volume sottratto
        //
        public bool ListRePopulate()
        {
            if (listView == null)
                return false;

            ListPopulate();
            ListCreateFinalPart();
            ListSelectFirstItem();
            ListDrawVolumeSelected();

            if (design1 != null)
                design1.Invalidate();

            return true;
        }

        public bool ListRecreateFinalPartWithSelection()
        {
            if (listView == null)
                return false;

            ListCreateFinalPart();
            //ListSelectFirstItem();
            ListDrawVolumeSelected();

            if (design1 != null)
                design1.Invalidate();

            return true;
        }

        //
        //  Seleziona il primo elemento della ListView
        //
        public bool ListSelectFirstItem()
        {
            if (listView == null)
                return false;

            // Check if there is at least one item in the ListView
            if (listView.Items.Count > 0)
            {
                listView.SelectedItems.Clear();

                // Select the first item
                listView.Items[0].Selected = true;

                // Optionally, ensure the selected item is visible
                listView.Items[0].EnsureVisible();

                listView.Focus();
            }

            return true;
        }

        //
        //  Seleziona l'ultimo elemento della ListView
        //
        private bool ListSelectLastItem()
        {
            if (listView == null)
                return false;

            // Check if there is at least one item in the ListView
            if (listView.Items.Count > 0)
            {
                listView.SelectedItems.Clear();

                // Select the first item
                listView.Items[listView.Items.Count - 1].Selected = true;

                // Optionally, ensure the selected item is visible
                listView.Items[listView.Items.Count - 1].EnsureVisible();
            }

            return true;
        }

        //
        //  Rende la macro associata all'elemento correntemente selezionato nella ListView
        //
        public ItemMacroList ListGetSelectedItem()
        {
            if (listView == null)
                return null;

            ItemMacroList selectedItem = null;

            // Assuming that the Tag property of ListViewItem is set to ItemMacroList
            if (listView.SelectedItems.Count > 0)
                selectedItem = (ItemMacroList)listView.SelectedItems[0].Tag;

            return selectedItem;
        }

        //
        //  Cancella la macro associata all'item selezionato e aggiorna la ListView
        //
        private bool ListDeleteMacroSelectedItem(bool repopulate)
        {
            if (listView == null || wpDynamicObj.macros == null)
                return false;

            ItemMacroList item = ListGetSelectedItem();
            if (item == null)
                return false;

            wpDynamicObj.macros.Remove(item.Macro);

            if (repopulate)
                ListRePopulate();

            return true;
        }

        //
        //  Evidenzia graficamente l'elemento selezionato della ListView disegnandone il volume sottratto
        //
        public bool ListDrawVolumeSelected()
        {
            if (listView == null || design1 == null || wpDynamicObj.exportEntities == null)
                return false;

            if (listView.SelectedItems.Count > 0)
            {
                int selectedIndex = listView.SelectedIndices[0];

                if (selectedIndex >= 0)
                {
                    // Remove all the difference solids from the design
                    List<Entity> differenceSolids = design1.Entities.Where(e => e.LayerName == EyeUtils.differenceSolids).ToList();
                    design1.Entities.Remove(differenceSolids);
                    // Remove all the lung curves from the design
                    List<Entity> lungCurves = design1.Entities.Where(e => e.LayerName == EyeUtils.macroLungCurves).ToList();
                    design1.Entities.Remove(lungCurves);

                    IMacroCope selectedMacro = ((ItemMacroList)listView.Items[selectedIndex].Tag).Macro;
                    if (selectedMacro is EyeMacro eyeMacro)
                    {
                        // Line of code to extraxt all the solids of a macro
                        eyeMacro.GetMacroSolids(out List<Brep> solids);

                        if (solids != null && solids.Count != 0)
                            design1.Entities.AddRange(solids, EyeUtils.differenceSolids, Color.FromArgb(50, Color.Green));
                    }
                    else if (selectedMacro is EyeMacroLung eyeMacroLung)
                    {
                        List<Brep> solids = eyeMacroLung.Features.Select(f => f.Solid).ToList();
                        List<Entity> drawingCurves;

                        if (eyeMacroLung.DrawingCurves.Count != 0)
                        {
                            drawingCurves = eyeMacroLung.DrawingCurves.Select(c => c as Entity).ToList();
                        }
                        else
                        {
                            eyeMacroLung.GetDrawingCurves(wpDynamicObj.finalPart, out List<ICurve> curves);
                            drawingCurves = curves.Select(c => c as Entity).ToList(); 
                        }

                        // Add all the difference solids to the design
                        design1.Entities.AddRange(solids, EyeUtils.differenceSolids, Color.FromArgb(50, Color.Green));
                        // Add all the lung curves to the design
                        design1.Entities.AddRange(drawingCurves, EyeUtils.macroLungCurves, Color.Orange);
                    }
                       
                    design1.Invalidate();
                }
            }

            return true;
        }

        //
        //  Aggiorna lo stato di macro abilitata di tutti
        //  gli item della lista sulla base della checkbox
        //
        public bool ListUpdateEnabledMacroStatus()
        {
            if (listView == null)
                return false;

            // Iterate through each ListViewItem in the listView
            foreach (ListViewItem item in listView.Items)
            {
                // Check the Checked property to get the checked status
                bool isChecked = item.Checked;

                ItemMacroList itemMacroList = (ItemMacroList)item.Tag;

                itemMacroList.Enabled = isChecked;
            }

            return true;
        }

        //
        //  Crea le entità Brep a partire dalle sole macro abilitate della lista
        //
        public bool ListCreateBrepEntities()
        {
            if (listView == null || wpDynamicObj.exportEntities == null || wpDynamicObj.finalPart == null)
                return false;

            //
            //  Sottraggo ad una ad una le macro abilitate
            //  tramite gli item della ListView
            //
            foreach (ListViewItem item in listView.Items)
            {
                ItemMacroList itemMacroList = (ItemMacroList)item.Tag;
                if (itemMacroList.Macro is IEyeMacro eyeMacro)
                {
                    //
                    //  Inserisco i Brep delle Features nella lista di tutti i Brep
                    //
                    wpDynamicObj.exportEntities.AddRange(eyeMacro.Features.Select(x => x.Solid));

                    if (!itemMacroList.Enabled)
                        continue;

                    //
                    //  Aggiorno la finalPart sottraenedo le Features della macro
                    //
                    SubtractMacroFromBrep(eyeMacro, log, ref wpDynamicObj.finalPart);

                    if (itemMacroList.Macro is EyeMacroLung eyeMacroLung)
                    {
                        //
                        // Aggiorno le curve da disegnare
                        //
                        eyeMacroLung.GetDrawingCurves(wpDynamicObj.finalPart, out List<ICurve> drawingCurves);
                        design1.Entities.AddRange(drawingCurves.Select(x => x as Entity), Color.Orange);
                    }
                }
            }

            //
            //  Calcolo le features e i vettori dell'EyeWorkPiece a partire dalla finalPart
            //
            EyeUtils.ComputeEyeWorkPieceFeaturesAndVectorsFromBrep(wpDynamicObj.wp as EyeWorkPiece, tolBrep, tolLinear, arcSegmentLength, log, ref wpDynamicObj.finalPart, out List<Line> lines, out List<Line> oppositeLines);

            if (lines != null)
            {
                design1.Entities.AddRange(lines, EyeUtils.vectors);
                design1.Entities.AddRange(oppositeLines, EyeUtils.vectors);
            }

            return true;
        }

        //
        //  Crea la FinalPart a partire dalle sole macro abilitate della lista
        //
        public bool ListCreateFinalPart()
        {
            if (listView == null || design1 == null || wpDynamicObj.exportEntities == null || wpDynamicObj.macros == null)
                return false;

            if (listView.Items.Count > 0)
            {
                // Clear the previus solids
                wpDynamicObj.exportEntities.Clear();

                EyeWorkPiece wp = wpDynamicObj.macros[0].Wp as EyeWorkPiece;

                if (wp != null)
                {
                    if (wp.Solid == null)
                        return false;

                    wpDynamicObj.finalPart = (Brep)(wp).Solid.Clone();

                    wpDynamicObj.exportEntities.Add(wpDynamicObj.finalPart);
                    design1.Entities.Clear();

                    if (!ListCreateBrepEntities())
                        log.Debug("Failed CreateFinalPartComputingVectorsAtTheEnd()");
                }
            }
            else
            {
                EyeWorkPiece wp = this.wpDynamicObj.wp as EyeWorkPiece;

                if (wp != null)
                    wpDynamicObj.finalPart = (Brep)(wp).Solid.Clone();

                design1.Entities.Clear();
            }

            if (wpDynamicObj.finalPart != null)
                design1.Entities.Add(wpDynamicObj.finalPart, EyeUtils.workedPiece, Color.Gray);

            return true;
        }

        //
        //  Carica nella pictureBox la bitmap passata
        //
        private void LoadBitmap(string BitmapName)
        {
            if (pictureBox != null)
            {
                string bmpPath = pictureFolder + "\\" + BitmapName;
                if (File.Exists(bmpPath))
                    pictureBox.Image = new Bitmap(bmpPath);
                else
                    pictureBox.Image = null;
            }
        }

        private bool UpdateMacroItem(ItemMacroList item, IMacroCope macro)
        {
            if (item == null)
                return false;

            item.Macro = macro;

            return true;
        }


        //
        //  Crea una stringa a partire dai soli campi double dell'oggetto passato
        //  con nome che inizia con "Par"
        //
        private string GetMacroParamsString(object obj)
        {
            Type type = obj.GetType();
            PropertyInfo[] properties = type.GetProperties();

            string formattedString = "";

            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(double))
                {
                    double doubleValue = (double)property.GetValue(obj);
                    if (doubleValue != 0 && property.Name.StartsWith("Par"))
                    {
                        string name = property.Name.Substring(3);
                        formattedString += $"{name}{doubleValue:F2} ";
                    }
                }
            }

            // Remove the trailing comma and space
            formattedString = formattedString.TrimEnd(',', ' ');

            return formattedString;
        }

        //
        //  Crea la stringa in formato FNC della macro associata all'Item passato
        //
        private string GetFNCMacroStringFromItem(ItemMacroList item)
        {
            if (item == null)
                return "";

            string strMacro = "";

            //EyeMacro macro = item.Macro;

            if (item.Macro is IMacroCope eyeMacro)
            {
                string paramsString = GetMacroParamsString(eyeMacro);

                strMacro = String.Format("[COPE] MAC:{0} {1}", eyeMacro.MacroName, paramsString);
            }
            

            return strMacro;
        }

        //
        //  Crea una nuova macro a partire da una stringa FNC e un EyeWorkPiece e la aggiunge
        //  ad una lista di IMacro
        //
        private IMacroCope CreateNewMacroFromString(string multilineStrMacro)
        {
            if (wpDynamicObj.wp == null || wpDynamicObj.macros == null || importExport == null)
                return null;

            IMacroCope macro = null;

            // Split the multiline string into an array of lines
            string[] linesArray = multilineStrMacro.Split(new[] { "\r\n" }, StringSplitOptions.None);

            foreach (var line in linesArray)
            {
                if (line == "")
                    continue;

                // Tokenize the string using the specified delimiter
                List<string> tokens = line.Split(' ').ToList();

                if (!Interpreter.SetMacroParam(tokens, out (ICopeParams copeParams, string macroName, string bitmapName, uint lineNumber) cope))
                    return null;

                string className = "", bitmapName = "";

                if (robotIni != null)
                    Importer.GetMacroClassNameFromIni(cope.macroName, wpDynamicObj.wp, robotIni, ref cope.copeParams, ref className, ref bitmapName);
                else if (macroLibGroupList != null && macroLibGroupList.Count > 0)
                    Importer.GetMacroClassNameFromJson(cope.macroName, wpDynamicObj.wp, macroLibGroupList, ref cope.copeParams, ref className, ref bitmapName);


                if (className == "")
                    MessageBox.Show($"Unknown MACRO: {cope.macroName}");
                else
                {
                    Type classType = importExport.GetClassType(className);
                    IMacroCope newmacro = Activator.CreateInstance(classType, wpDynamicObj.wp, cope.copeParams, className, cope.macroName, eyeParam, cope.lineNumber) as IMacroCope;
                    newmacro.MacroBitmapName = bitmapName;

                    //
                    //  Creo le Features della macro
                    //
                    newmacro.CreateMacro();
                    // !!!!!!!! Manca la SetParams !!!!!!!!!!

                    wpDynamicObj.macros.Add(newmacro);

                    if (macro == null)
                        macro = newmacro;
                }
            }

            return macro;
        }

        public static DialogResult ShowConfirmAbortMessageBox(string message)
        {
            string caption = "Confirmation";
            MessageBoxButtons buttons = MessageBoxButtons.OKCancel;
            MessageBoxIcon icon = MessageBoxIcon.Question;

            // Display the message box with confirm and abort options
            DialogResult result = MessageBox.Show(message, caption, buttons, icon);

            return result;
        }

        public void Edit ()
        {
            ItemMacroList item = ListGetSelectedItem();
            int itemIndex = item != null ? listView.SelectedIndices[0] : -1;
            string strMacro = GetFNCMacroStringFromItem(item);

            if (strMacro != "")
            {
                // Open a new form with an edit control (TextBox)
                using (EditForm editForm = new EditForm())
                {
                    editForm.Text = "Edit macro";
                    editForm.editText.Text = strMacro;
                    editForm.editText.SelectionStart = editForm.editText.Text.Length;

                    // Show the form as a dialog
                    if (editForm.ShowDialog() == DialogResult.OK)
                    {
                        // Access the edited text from the edit form
                        string editedText = editForm.EditedText;

                        IMacroCope macro = CreateNewMacroFromString(editedText);
                        if (macro != null)
                        {
                            ListDeleteMacroSelectedItem(false);

                            UpdateMacroItem(item, macro);

                            listView.SelectedItems.Clear();
                            if (itemIndex >= 0)
                                listView.Items[itemIndex].Selected = true;

                            ListRecreateFinalPartWithSelection();
                        }
                    }
                }
            }
        }

        public void Delete ()
        {
            // Display a message box with confirm and abort options
            DialogResult result = ShowConfirmAbortMessageBox("Delete macro: do you want to proceed?");

            // Test the return value
            if (result == DialogResult.OK)
            {
                ListDeleteMacroSelectedItem(true);
            }
        }

        public void Add ()
        {
            ItemMacroList item = ListGetSelectedItem();
            int itemIndex = item != null ? listView.SelectedIndices[0] : -1;
            string strMacro = GetFNCMacroStringFromItem(item);

            // Open a new form with an edit control (TextBox)
            using (EditForm editForm = new EditForm(false))
            {
                editForm.Text = "Add macro";

                if (strMacro != "")
                    editForm.editText.Text = strMacro;

                // Show the form as a dialog
                if (editForm.ShowDialog() == DialogResult.OK)
                {
                    // Access the edited text from the edit form
                    string editedText = editForm.EditedText;

                    CreateNewMacroFromString(editedText);

                    ListRePopulate();

                    ListSelectLastItem();
                }
            }
        }

        //
        //  Apro menu sul right click
        //
        private void ListContextMenuItem_Click(object sender, EventArgs e)
        {
            if (listView == null)
                return;

            // Handle the context menu item click
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            string selectedOption = menuItem.Text;

            if (selectedOption == "Add")
            {
                Add();
            }
            else if (selectedOption == "Edit")
            {
                Edit();
            }
            else if (selectedOption == "Delete")
            {
                Delete();
            }
        }

        //
        //  Cambio selezione nella lista macro
        //
        public void OnSelChangeMacroList(object sender, EventArgs e)
        {
            ListDrawVolumeSelected();

            ItemMacroList item = ListGetSelectedItem();
            if (item != null)
                LoadBitmap(item.Macro.MacroBitmapName);
        }

        //
        //  Check / Uncheck di un item della lista
        //
        public void OnItemCheckedMacroList(object sender, ItemCheckedEventArgs e)
        {
            ListUpdateEnabledMacroStatus();

            if (ListIsPopulating)
                return;

            ListCreateFinalPart();

            ListDrawVolumeSelected();

            if (design1 != null)
                design1.Invalidate();
        }

        public void OnClickMacroList(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (listView == null)
                return;

            // Handle the MouseClick event to identify the column
            ListViewHitTestInfo hitTestInfo = listView.HitTest(e.Location);

            if (hitTestInfo.SubItem != null)
                ColumnIndex = hitTestInfo.Item.SubItems.IndexOf(hitTestInfo.SubItem);
            else
                ColumnIndex = -1;

            if (e.Button == MouseButtons.Right && ColumnIndex > 0)
            {
                // Right-click detected
                ListViewItem selectedItem = listView.GetItemAt(e.X, e.Y);

                if (selectedItem != null)
                {
                    // Show the context menu at the right-click location
                    ContextMenuCreate();
                    ListcontextMenuStrip.Show(listView, e.Location);
                }
            }
        }

    }

    partial class Form1
    {
        //
        //  La MacroList è abilitata solo se ho caricato un file FNC
        //
        bool IsMacroListEnabled ()
        {
            return curFileExtension == ".FNC";
        }

        //
        //  Cambio selezione nella lista macro
        //
        public void OnSelChangeMacroList(object sender, EventArgs e)
        {
            if (macroList != null)
                macroList.OnSelChangeMacroList(sender, e);
        }

        //
        //  Check / Uncheck di un item della lista
        //
        public void OnItemCheckedMacroList(object sender, ItemCheckedEventArgs e)
        {
            if (macroList != null)
            {
                macroList.OnItemCheckedMacroList(sender, e);
            }
        }

        public void OnClickMacroList(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (macroList != null)
                macroList.OnClickMacroList(sender, e);
        }
        private void OnKeyPressMacroList(object sender, KeyPressEventArgs e)
        {
            if (macroList != null)
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    // Handle the Enter key press
                    macroList.Edit();

                    e.Handled = true; // Set this to true to prevent the TextBox from processing the Enter key
                }
            }
        }

        private void OnKeyDownMacroLIst(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (macroList != null)
            {
                if (e.KeyCode == Keys.Delete)
                {
                    // Handle the Delete key press
                    macroList.Delete();

                    e.Handled = true; // Set this to true to prevent the TextBox from processing the Enter key
                }
                else if (e.KeyCode == Keys.Insert)
                {
                    // Handle the Insert key press
                    macroList.Add();

                    e.Handled = true; // Set this to true to prevent the TextBox from processing the Enter key
                }
            }

        }

        private void OnDragDropMacroList(object sender, DragEventArgs e)
        {
            DragDrop(sender, e);
        }
        private void OnDragEnterMacroList(object sender, DragEventArgs e)
        {
            DragEnter(sender, e);
        }
    }

    public class WpDynamicObjects
    {
        public List<Entity> exportEntities;
        public Brep finalPart;
        public List<IMacro> macros;
        public IWorkPiece wp;

        public WpDynamicObjects()
        {
            exportEntities = new List<Entity>();
            macros = new List<IMacro>();
            finalPart = null;
            wp = null;
        }

        public WpDynamicObjects(List<Entity> exportEntities, Brep finalPart, List<IMacro> macros, IWorkPiece wp)
        {
            this.exportEntities = exportEntities;
            this.finalPart = finalPart;
            this.macros = macros;
            this.wp = wp;
        }
    }
}
