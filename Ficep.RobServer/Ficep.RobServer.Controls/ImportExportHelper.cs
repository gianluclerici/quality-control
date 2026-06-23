using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using devDept.Eyeshot;
using devDept.Eyeshot.Control;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using devDept.Geometry;
using devDept.Serialization;

namespace devDept.CustomControls
{
    /// <summary>
    /// The bitmask of enabled file formats for the Import dialog.
    /// </summary>
    [Flags]
    public enum importFormats
    {
        PointCloud = 1,
        SurfaceAndBrep = 2,
        Autodesk = 4,
        Mesh = 8,
        Cnc = 16,
        Ifc = 32,
        Fem = 64,
        Pdf = 128,
        Svg = 256,
        All = PointCloud | SurfaceAndBrep | Autodesk | Mesh | Cnc | Ifc | Fem | Pdf | Svg
    }

    /// <summary>
    /// The bitmask of enabled file formats for the Export dialog.
    /// </summary>
    [Flags]
    public enum exportFormats
    {
        PointCloud = 1,
        SurfaceAndBrep = 2,
        Autodesk = 4,
        Mesh = 8,
        All = PointCloud | SurfaceAndBrep | Autodesk | Mesh
    }

    /// <summary>
    /// Helper class for import and export operations.
    /// </summary>
    public class ImportExportHelper
    {
        static readonly string _eyeExtension = "eye";
        static readonly string _asmExtension = _eyeExtension + MultiFileHelper.AsmSuffix;

        #region Save Dialog

        private static SaveFileDialog _saveFileDialog;
        private static SaveFileAddOn _saveDialogCtrl;

        /// <summary>
        /// Shows the dialog to save the scene in the Eyeshot file format.
        /// </summary>
        /// <param name="design">The Design control containing the scene to store.</param>
        /// <param name="customSerializer">The file serializer containing the definition for custom objects (Can be null/Nothing).</param>
        /// <param name="tag">The tag to add to the file header. (Can be null/Nothing).</param>
        /// <param name="multiFileParallel">Indicates whether the multi-file must be stored with parallel threads or not.</param>
        /// <param name="multiFileUpdateThumbnails">Indicates whether the thumbnail must be updated for all the written files or not.</param>
        /// <returns>The WriteFile workunit. It's ready to be run for storing the data on disk. Can be Null/Nothing</returns>
        public static WriteFile ShowSaveDialog(Design design, FileSerializer customSerializer = null, string tag = null, bool multiFileParallel = true,
            bool multiFileUpdateThumbnails = false)
        {
            return ShowSaveDialog(design, null, customSerializer, tag, multiFileParallel, multiFileUpdateThumbnails);
        }

        /// <summary>
        /// Shows the dialog to save the scene in the Eyeshot file format.
        /// </summary>
        /// <param name="design">The Design control containing the scene to store.</param>
        /// <param name="drawing">The Drawing control with 2D sheets to store.</param>
        /// <param name="customSerializer">The file serializer containing the definition for custom objects (Can be null/Nothing).</param>
        /// <param name="tag">The tag to add to the file header. (Can be null/Nothing).</param>
        /// <param name="multiFileParallel">Indicates whether the multi-file must be stored with parallel threads or not.</param>
        /// <param name="multiFileUpdateThumbnails">Indicates whether the thumbnail must be updated for all the written files or not.</param>
        /// <returns>The WriteFile workunit. It's ready to be run for storing the data on disk. Can be Null/Nothing</returns>
        public static WriteFile ShowSaveDialog(Design design, Drawing drawing, FileSerializer customSerializer = null, string tag = null, bool multiFileParallel = true,
            bool multiFileUpdateThumbnails = false)
        {
            WriteFile writeFile = null;

            using (_saveFileDialog = new SaveFileDialog())
            using (_saveDialogCtrl = new SaveFileAddOn())
            {
                _saveFileDialog.Filter = $"All Eyeshot file types (*.*)|*.{_eyeExtension}; *.{_asmExtension}|Eyeshot (*.{_eyeExtension})|*.{_eyeExtension}|Eyeshot Assembly (*.{_asmExtension})|*.{_asmExtension}";
                _saveFileDialog.AddExtension = true;
                _saveFileDialog.CheckPathExists = true;
                _saveFileDialog.ShowHelp = true;
                _saveFileDialog.OverwritePrompt = false; // We handle it in FileOk event.
                _saveFileDialog.RestoreDirectory = true;

                _saveFileDialog.FileOk += SaveFileDialog_FileOk;

                if (!String.IsNullOrEmpty(design.RootBlock.FilePath))
                {
                    // If the root block has a full path stored for MultiFile, we can purpose it by default.
                    _saveFileDialog.RestoreDirectory = true;
                    _saveFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(design.RootBlock.FilePath);
                    if (!System.IO.Directory.Exists(_saveFileDialog.InitialDirectory))
                        System.IO.Directory.CreateDirectory(_saveFileDialog.InitialDirectory);
                    _saveFileDialog.FileName = design.RootBlock.FilePath;
                    _saveDialogCtrl.FileModeOption = System.IO.Path.GetExtension(_saveFileDialog.FileName).EndsWith(_asmExtension) ? 1 : 0;
                }

                if (_saveFileDialog.ShowDialog(_saveDialogCtrl, null) == DialogResult.OK)
                {
                    fileType type = (fileType) _saveDialogCtrl.FileModeOption;

                    var serializer = customSerializer ?? new FileSerializer();

                    if (type == fileType.Standard)
                    {
                        var dataParams = new WriteFileParams(design.Document, drawing?.Document, (contentType) _saveDialogCtrl.ContentOption,
                            (serializationType) _saveDialogCtrl.SerialOption, _saveDialogCtrl.SelectedOnly, true,
                            !design.IsOpenRootLevel)
                        {
                            Tag = tag,
                            Purge = !design.IsOpenRootLevel || _saveDialogCtrl.Purge
                        };

                        writeFile = new WriteFile(dataParams, _saveFileDialog.FileName, serializer);
                    }
                    else // Assembly
                    {
                        var dataParams = new WriteMultiFileParams(design.Document)
                        {
                            Tag = tag,
                            Content = (contentType) _saveDialogCtrl.ContentOption,
                            SerializationMode = (serializationType) _saveDialogCtrl.SerialOption,
                            SelectedOnly = _saveDialogCtrl.SelectedOnly,
                            Purge = _saveDialogCtrl.Purge,
                            UpdateThumbnails = multiFileUpdateThumbnails,
                            Parallel = multiFileParallel,
                            BlockName = design.IsOpenRootLevel ? null : design.OpenBlock.Name
                        };

                        writeFile = new WriteMultiFile(dataParams, _saveFileDialog.FileName, serializer);
                    }
                }

                _saveFileDialog.FileOk -= SaveFileDialog_FileOk;
            }

            return writeFile;
        }

        private static void SaveFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            if ((fileType)_saveDialogCtrl.FileModeOption == fileType.Assembly)
                _saveFileDialog.FileName = System.IO.Path.ChangeExtension(_saveFileDialog.FileName, _asmExtension);
            else
                _saveFileDialog.FileName = System.IO.Path.ChangeExtension(_saveFileDialog.FileName, _eyeExtension);

            if (File.Exists(_saveFileDialog.FileName))
            {
                if (MessageBox.Show($"File {_saveFileDialog.FileName} already exists.{System.Environment.NewLine}Do you want to replace it?", "Save", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    e.Cancel = true;
            }
        }

        #endregion

        #region Open Dialog

        private static OpenFileAddOn _openFileAddOn;

        /// <summary>
        /// Shows the dialog to load the scene from an Eyeshot file format.
        /// </summary>
        /// <param name="multiFileParallel">Indicates whether the multi-file must be read with parallel threads or not.</param>
        /// <returns>The ReadFile workunit. It's ready to be run for loading the data from disk. Can be Null/Nothing.</returns>
        public static ReadFile ShowOpenDialog(bool multiFileParallel = true)
        {
            string filePath;
            bool insertAsBlock;
            return ShowOpenDialog<FileSerializer>(out filePath, out insertAsBlock, multiFileParallel);
        }

        /// <summary>
        /// Shows the dialog to load the scene from an Eyeshot file format.
        /// </summary>
        /// <typeparam name="T">The type of the FileSerializer containing the definition for loading custom objects.</typeparam>
        /// <param name="multiFileParallel">Indicates whether the multi-file must be read with parallel threads or not.</param>
        /// <returns>The ReadFile workunit. It's ready to be run for loading the data from disk. Can be Null/Nothing.</returns>
        public static ReadFile ShowOpenDialog<T>(bool multiFileParallel = true) where T : FileSerializer
        {
            string filePath;
            bool insertAsBlock;
            return ShowOpenDialog<T>(out filePath, out insertAsBlock, multiFileParallel);
        }

        /// <summary>
        /// Shows the dialog to load the scene from an Eyeshot file format.
        /// </summary>
        /// <param name="insertAsBlock">Indicates whether the "Insert As Block" option has been checked or not.</param>
        /// <param name="multiFileParallel">Indicates whether the multi-file must be read with parallel threads or not.</param>
        /// <returns>The ReadFile workunit. It's ready to be run for loading the data from disk. Can be Null/Nothing.</returns>
        public static ReadFile ShowOpenDialog(out bool insertAsBlock, bool multiFileParallel = true)
        {
            string filePath;
            return ShowOpenDialog<FileSerializer>(out filePath, out insertAsBlock, multiFileParallel);
        }

        /// <summary>
        /// Shows the dialog to load the scene from an Eyeshot file format.
        /// </summary>
        /// <typeparam name="T">The type of the FileSerializer containing the definition for loading custom objects.</typeparam>
        /// <param name="insertAsBlock">Indicates whether the "Insert As Block" option has been checked or not.</param>
        /// <param name="multiFileParallel">Indicates whether the multi-file must be read with parallel threads or not.</param>
        /// <returns>The ReadFile workunit. It's ready to be run for loading the data from disk. Can be Null/Nothing.</returns>
        public static ReadFile ShowOpenDialog<T>(out bool insertAsBlock, bool multiFileParallel = true) where T : FileSerializer
        {
            string filePath;
            return ShowOpenDialog<T>(out filePath, out insertAsBlock, multiFileParallel);
        }

        /// <summary>
        /// Shows the dialog to load the scene from an Eyeshot file format.
        /// </summary>
        /// <param name="filePath">The path and file name choose by the user. Null/Nothing if the dialog is canceled.</param>
        /// <param name="insertAsBlock">Indicates whether the "Insert As Block" option has been checked or not.</param>
        /// <param name="multiFileParallel">Indicates whether the multi-file must be read with parallel threads or not.</param>
        /// <returns>The ReadFile workunit. It's ready to be run for loading the data from disk. Can be Null/Nothing.</returns>
        public static ReadFile ShowOpenDialog(out string filePath, out bool insertAsBlock, bool multiFileParallel = true)
        {
            return ShowOpenDialog<FileSerializer>(out filePath, out insertAsBlock, multiFileParallel);
        }

        /// <summary>
        /// Shows the dialog to load the scene from an Eyeshot file format.
        /// </summary>
        /// <typeparam name="T">The type of the FileSerializer containing the definition for loading custom objects.</typeparam>
        /// <param name="filePath">The path and file name choose by the user. Null/Nothing if the dialog is canceled.</param>
        /// <param name="insertAsBlock">Indicates whether the "Insert As Block" option has been checked or not.</param>
        /// <param name="multiFileParallel">Indicates whether the multi-file must be read with parallel threads or not.</param>
        /// <param name="enableStructureOnly">Indicates whether the "Structure Only" option is enabled or not.</param>
        /// <returns>The ReadFile workunit. It's ready to be run for loading the data from disk. Can be Null/Nothing.</returns>
        public static ReadFile ShowOpenDialog<T>(out string filePath, out bool insertAsBlock, bool multiFileParallel = true, bool enableStructureOnly = false) where T : FileSerializer
        {
            insertAsBlock = false; // value is ignored by Insert Component function in AssemblyTreeViewEx
            filePath = null;
            ReadFile readFile = null;

            using (var openFileDialog1 = new OpenFileDialog())
            {
                openFileDialog1.Filter = $"All Eyeshot file types (*.*)|*.{_eyeExtension}; *.{_asmExtension}|Eyeshot (*.{_eyeExtension})|*.{_eyeExtension}|Eyeshot Assembly (*.{_asmExtension})|*.{_asmExtension}";
                openFileDialog1.Multiselect = false;
                openFileDialog1.AddExtension = true;
                openFileDialog1.CheckFileExists = true;
                openFileDialog1.CheckPathExists = true;
                openFileDialog1.DereferenceLinks = true;
                openFileDialog1.RestoreDirectory = true;

                using (_openFileAddOn = new OpenFileAddOn(enableStructureOnly))
                {
                    _openFileAddOn.EventFileNameChanged += OpenFileAddOn_EventFileNameChanged;

                    if (openFileDialog1.ShowDialog(_openFileAddOn, null) == DialogResult.OK)
                    {
                        var serializer = (T) Activator.CreateInstance(typeof(T), (contentType) _openFileAddOn.ContentOption);

                        filePath = openFileDialog1.FileName;
                        insertAsBlock = _openFileAddOn.InsertAsBlock;
                        bool structureOnly = _openFileAddOn.StructureOnly;

                        readFile = ReadFile.GetHeader(filePath).FileMode == fileType.Assembly
                            ? new ReadMultiFile(filePath, serializer)
                                { StructureOnly = structureOnly, Parallel = multiFileParallel }
                            : new ReadFile(filePath, serializer);
                    }

                    _openFileAddOn.EventFileNameChanged -= OpenFileAddOn_EventFileNameChanged;
                }
            }

            return readFile;
        }

        /// <summary>
        /// Shows the dialog to load the scene from an Eyeshot file format.
        /// </summary>
        /// <param name="filePath">The path and file name choose by the user. Null/Nothing if the dialog is canceled.</param>
        /// <param name="insertAsBlock">Indicates whether the "Insert As Block" option has been checked or not.</param>
        /// <param name="multiFileParallel">Indicates whether the multi-file must be read with parallel threads or not.</param>
        /// <param name="enableStructureOnly">Indicates whether the "Structure Only" option is enabled or not.</param>
        /// <returns>The ReadFile workunit. It's ready to be run for loading the data from disk. Can be Null/Nothing.</returns>
        public static ReadFile ShowOpenDialog(out string filePath, out bool insertAsBlock, bool multiFileParallel = true, bool enableStructureOnly = false)
        {
            return ShowOpenDialog<FileSerializer>(out filePath, out insertAsBlock, multiFileParallel, enableStructureOnly);
        }

        private static void OpenFileAddOn_EventFileNameChanged(IWin32Window sender, string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                int version = ReadFile.GetVersion(filePath);

                if (version > devDept.Serialization.Serializer.LastVersion)
                {
                    _openFileAddOn.SetFileInfo(null, $"File version {version} is not supported!" +
                                                     $"{System.Environment.NewLine}" +
                                                     $"Latest file version supported is {devDept.Serialization.Serializer.LastVersion}.");
                }
                else
                {
                    using (ReadFile rf = new ReadFile(filePath, true))
                    {
                        _openFileAddOn.SetFileInfo(UtilityEx.ConvertBytesToImage(rf.GetThumbnail()), rf.GetFileInfo(),
                            _openFileAddOn.StructureOnlyEnabled && rf.FileSerializer.FileHeader?.FileMode == fileType.Assembly);
                    }
                }
            }
            else
            {
                _openFileAddOn.ResetFileInfo();
            }
        }

        #endregion

        #region Export Dialog

        /// <summary>
        /// Shows the dialog to export the scene in a supported file format.
        /// </summary>
        /// <param name="design">The Design control containing the scene to store.</param>
        /// <param name="formats">The supported file format to show in the dialog.</param>
        /// <returns>The WriteFileAsync workunit. It's ready to be run for storing the data on disk. Can be Null/Nothing</returns>
        public static WriteFileAsync ShowExportDialog(Design design, exportFormats formats)
        {
            string filePath;
            return ShowExportDialog(design, formats, out filePath);
        }

        /// <summary>
        /// Shows the dialog to export the scene in a supported file format.
        /// </summary>
        /// <param name="design">The Design control containing the scene to store.</param>
        /// <param name="formats">The supported file format to show in the dialog.</param>
        /// <param name="filePath">The path and the name of the file chosen by the user. Null/Nothing if the dialog is canceled.</param>
        /// <returns>The WriteFileAsync workunit. It's ready to be run for storing the data on disk. Can be Null/Nothing</returns>
        public static WriteFileAsync ShowExportDialog(Design design, exportFormats formats, out string filePath)
        {
            filePath = null;
            using (SaveFileDialog saveFileDialog1 = new SaveFileDialog())
            {
                saveFileDialog1.Filter = GetExportFilter(formats);

                saveFileDialog1.AddExtension = true;
                saveFileDialog1.CheckPathExists = true;
                saveFileDialog1.RestoreDirectory = true;

                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    filePath = saveFileDialog1.FileName;
                    return GetExportWriter(filePath, design);
                }
            }

            return null;
        }

        #endregion

        #region Import Dialog

        /// <summary>
        /// Shows the dialog to import the scene from a supported file format.
        /// </summary>
        /// <param name="formats">The supported file format to show in the dialog.</param>
        /// <param name="yAxisUp">Indicates whether the option "Geometry in Y Axis Up" has been checked or not.</param>
        /// <param name="removeJittering">Indicates whether the option "Remove Jittering" has been checked or not.</param>
        /// <param name="insertAsBlock">Indicates whether the option "Insert As Block" has been checked or not.</param>
        /// <returns>The ReadFileAsync workunit. It's ready to be run for loading the data from disk. Can be Null/Nothing.</returns>
        public static ReadFileAsync ShowImportDialog(importFormats formats, out bool yAxisUp, out bool removeJittering, out bool insertAsBlock)
        {
            string filePath = null;
            return ShowImportDialog(formats, out filePath, out yAxisUp, out removeJittering, out insertAsBlock);
        }

        /// <summary>
        /// Shows the dialog to import the scene from a supported file format.
        /// </summary>
        /// <param name="formats">The supported file format to show in the dialog.</param>
        /// <param name="filePath">The path and the name of the file chosen by the user. Null/Nothing if the dialog is canceled.</param>
        /// <param name="yAxisUp">Indicates whether the option "Geometry in Y Axis Up" has been checked or not.</param>
        /// <param name="removeJittering">Indicates whether the option "Remove Jittering" has been checked or not.</param>
        /// <param name="insertAsBlock">Indicates whether the option "Insert As Block" has been checked or not.</param>
        /// <returns>The ReadFileAsync workunit. It's ready to be run for loading the data from disk. Can be Null/Nothing.</returns>
        public static ReadFileAsync ShowImportDialog(importFormats formats, out string filePath, out bool yAxisUp, out bool removeJittering, out bool insertAsBlock)
        {
            filePath = null;
            yAxisUp = removeJittering = insertAsBlock = false;

            using (var importFileDialog1 = new OpenFileDialog())
            using (var importFileAddOn = new ImportFileAddOn())
            {
                importFileDialog1.Filter = GetImportFilter(formats);

                importFileDialog1.Multiselect = false;
                importFileDialog1.AddExtension = true;
                importFileDialog1.CheckFileExists = true;
                importFileDialog1.CheckPathExists = true;
                importFileDialog1.RestoreDirectory = true;

                if (importFileDialog1.ShowDialog(importFileAddOn, null) == DialogResult.OK)
                {
                    filePath = importFileDialog1.FileName;
                    yAxisUp = importFileAddOn.YAxisUp;
                    removeJittering = importFileAddOn.Jittering;
                    insertAsBlock = importFileAddOn.InsertAsBlock;

                    return GetImportReader(filePath);
                }
            }

            return null;
        }

        #endregion

        /// <summary>
        /// Returns a ReadFileAsync object based on the file extension.
        /// </summary>
        public static ReadFileAsync GetImportReader(string filePath)
        {
            string ext = System.IO.Path.GetExtension(filePath);

            if (ext != null)
            {
                ext = ext.TrimStart('.').ToLower();

                switch (ext)
                {
                    case "asc":
                        return new ReadASC(filePath);
                    case "jt":
                        return new ReadJT(filePath);
                    case "stl":
                        return new ReadSTL(filePath);
                    case "obj":
                        return new ReadOBJ(filePath);
                    case "gltf":
                    case "glb":
                        return new ReadGLTF(filePath);
                    case "las":
                        return new ReadLAS(filePath, ReadFastPointCloudBase.formatType.Colors);
                    case "nc":
                    case "ncc":
                    case "cnc":
                    case "tap":
                        return new ReadGCode(filePath);
                    case "pdf":
                        return new ReadPDF(filePath);
                    case "igs":
                    case "iges":
                        if (Utility.ProductEdition != licenseType.Pro)
                            return new ReadIGES(filePath);
                        break;
                    case "stp":
                    case "step":
                        if (Utility.ProductEdition != licenseType.Pro)
                            return new ReadSTEP(filePath);
                        break;
                    case "ifc":
                    case "ifczip":
                        return new ReadIFC(filePath);
                    case "mesh":
                        if (Utility.ProductEdition == licenseType.Ultimate ||
                            Utility.ProductEdition == licenseType.Fem)
                            return new ReadMedit(filePath);
                        break;
                    case "bdf":
                    case "nas":
                    case "dat":
                        if (Utility.ProductEdition == licenseType.Ultimate ||
                            Utility.ProductEdition == licenseType.Fem)
                            return new ReadNastran(filePath);
                        break;
                    case "svg":
                        return new ReadSVG(filePath);
                    case "nc1":
                        return new ReadDSTV(filePath);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns WriteFileAsync object based on the file extension.
        /// </summary>
        public static WriteFileAsync GetExportWriter (string filePath, devDept.Eyeshot.Control.Design design)
        {
            WriteParams dataParams;
            string ext = System.IO.Path.GetExtension(filePath);
            
            if (ext != null)
            {
                ext = ext.TrimStart('.').ToLower();

                switch (ext)
                {
                    case "obj":
                        dataParams = new WriteParamsWithMaterials(design.Document, false, !design.IsOpenRootLevel);
                        return new WriteOBJ((WriteParamsWithMaterials)dataParams, filePath);
                    case "stl":
                        dataParams = new WriteParams(design.Document, false, !design.IsOpenRootLevel);
                        return new WriteSTL(dataParams, filePath);
                    case "las":
                        return new WriteLAS(design.Entities.Where(x => x is Eyeshot.Entities.FastPointCloud).FirstOrDefault() as Eyeshot.Entities.FastPointCloud, filePath);
                    case "html":
                        dataParams = new WriteParamsWithMaterials(design.Document, false, !design.IsOpenRootLevel);
                        return new WriteWebGL((WriteParamsWithMaterials)dataParams, filePath);
                    case "step":
                        if (Utility.ProductEdition != licenseType.Pro)
                        {
                            dataParams = new WriteParamsWithUnits(design.Document, false, !design.IsOpenRootLevel);
                            return new WriteSTEP((WriteParamsWithUnits) dataParams, filePath);
                        }
                        break;
                    case "iges":
                        if (Utility.ProductEdition != licenseType.Pro)
                        {
                            dataParams = new WriteParamsWithUnits(design.Document, false, !design.IsOpenRootLevel);
                            return new WriteIGES((WriteParamsWithUnits) dataParams, filePath);
                        }
                        break;
                    case "glb":
                        dataParams = new WriteParamsWithMaterials(design, false, !design.IsOpenRootLevel);
                        return new WriteGLTF((WriteParamsWithMaterials) dataParams, filePath, true);
                    case "gltf":
                        dataParams = new WriteParamsWithMaterials(design, false, !design.IsOpenRootLevel);
                        return new WriteGLTF((WriteParamsWithMaterials) dataParams, filePath, false);
                }
            }
            return null;
        }

        /// <summary>
        /// Returns a chained filter string with extensions and descriptions for the import dialog.
        /// </summary>
        /// <param name="formats">The <see cref="importFormats"/> that must be enabled.</param>
        /// <returns>A chained filter string with extensions and descriptions for the import dialog.</returns>
        public static string GetImportFilter(importFormats formats)
        {
            string extensions = "All compatible file types (*.*)|";
            string descriptions = "";

            if (formats.HasFlag(importFormats.PointCloud))
            {
                extensions += "*.asc;*.las;";
                descriptions+= "|Points (*.asc)|*.asc|Laser LAS (*.las)|*.las";
            }
            if (Utility.ProductEdition != licenseType.Pro && formats.HasFlag(importFormats.SurfaceAndBrep))
            {
                extensions += "*.stp; *.step; *.igs; *.iges;";
                descriptions += "|Standard for the Exchange of Product Data (*.stp; *.step)|*.stp; *.step|Initial Graphics Exchange Specification (*.igs; *.iges)|*.igs; *.iges";
            }
            if (formats.HasFlag(importFormats.Autodesk))
            {
                extensions += "*.dxf; *.dwg; *.dwf; *.dwfx;";
                descriptions += "|Drawing Exchange Format (*.dxf)|*.dxf|CAD drawings (*.dwg)|*.dwg|Design Web Format (*.dwf; *.dwfx)|*.dwf; *.dwfx";
            }
            
            if (formats.HasFlag(importFormats.Mesh))
            {
                extensions += "*.obj; *.stl; *.jt; *.glb; *.gltf";
                descriptions += "|WaveFront OBJ (*.obj)|*.obj|Stereolithography (*.stl)|*.stl|JT (*.jt)|*.jt|glTF 2.0 (*.glb, *.gltf)|*.glb;*.gltf";
            }
            
            if (formats.HasFlag(importFormats.Cnc))
            {
                extensions += "*.nc; *.ncc; *.cnc; *.tap;";
                descriptions += "|CNC (*.nc; *.ncc; *.cnc; *.tap)|*.nc; *.ncc; *.cnc; *.tap";
            }
            if (formats.HasFlag(importFormats.Ifc))
            {
                extensions += "*.ifc;*.ifczip;";
                descriptions += "|IFC (*.ifc; *.ifczip)|*.ifc; *.ifczip";
            }
            if ((Utility.ProductEdition == licenseType.Fem ||
                 Utility.ProductEdition == licenseType.Trial) &&
                formats.HasFlag(importFormats.Fem))
            {
                extensions += "*.bdf;*.nas;*.dat;*.mesh";
                descriptions += "|Nastran (*.bdf; *.nas; *.dat)|*.bdf; *.nas; *.dat|Medit (*.mesh)|*.mesh";
            }
            if (formats.HasFlag(importFormats.Pdf))
            {
                extensions += "*.pdf;";
                descriptions += "|PDF (*.pdf)|*.pdf;";
            }
            if (formats.HasFlag(importFormats.Svg))
            {
                extensions += "*.svg;";
                descriptions += "|Scalable Vector Graphics (*.svg)|*.svg;";
            }


            return extensions + descriptions;
        }

        /// <summary>
        /// Returns chained filter string with extensions and descriptions for the export dialog.
        /// </summary>
        /// <param name="formats">The <see cref="exportFormats"/> that must be enabled.</param>
        /// <returns>A chained filter string with extensions and descriptions for the export dialog.</returns>
        public static string GetExportFilter(exportFormats formats)
        {
            string descriptions = "";

            if (formats.HasFlag(exportFormats.PointCloud))
            {
                descriptions += "|Laser LAS (*.las)|*.las";
            }
            if (Utility.ProductEdition != licenseType.Pro && formats.HasFlag(exportFormats.SurfaceAndBrep))
            {
                descriptions += "|Standard for the Exchange of Product Data (*.step)|*.step|Initial Graphics Exchange Specification (*.iges)|*.iges";
            }
            if (formats.HasFlag(exportFormats.Autodesk))
            {
                descriptions += "|Drawing Exchange Format (*.dxf)|*.dxf|CAD drawings (*.dwg)|*.dwg|3D PDF (*.pdf)|*.pdf";
            }
            if (formats.HasFlag(exportFormats.Mesh))
            {
                descriptions += "|WaveFront OBJ (*.obj)|*.obj|Stereolithography (*.stl)|*.stl|WebGL (*.html)|*.html|glTF 2.0 (*.glb, *.gltf)|*.glb;*.gltf";
            }

            return descriptions.TrimStart('|');
        }

        public static BlockReference InsertAsBlock( Design design, ReadFileAsync rf, RegenOptions ro = null )
        {
            Hashtable renamedBlocks = new Hashtable();
            string fileName = System.IO.Path.GetFileNameWithoutExtension(rf.FilePath);
            BlockReference rootBlockRef = null;

            ReadFileAsyncWithBlocks rfb = rf as ReadFileAsyncWithBlocks;
                     

            if (rfb != null)
            {
                // Looking for duplicated block names

                //here we merge the two BlockKeyedCollection 
                var mergedBlocks = MergeBlocks(design, rfb);

                foreach (Block block in rfb.Blocks)
                {
                    string blockName = block.Name;
                    if (design.Blocks.Contains(blockName))
                    {
                        string newBlockName = UtilityEx.GetUnusedBlockName(block.Name, design.Blocks, true);

                        if (!string.IsNullOrEmpty(rfb.Blocks.RootBlockName) && rfb.Blocks.RootBlockName.Equals(blockName) )
                        {
                            // File contains a root block with a name already presents in the scene.
                            newBlockName = UtilityEx.GetUnusedBlockName(fileName, mergedBlocks, true);
                            rootBlockRef = new BlockReference(newBlockName);
                        }

                        renamedBlocks.Add(blockName, newBlockName);
                        block.Name = newBlockName;
                    }
                }

                if (renamedBlocks.Count > 0)
                {
                    // Fixes BlockReference's block name

                    foreach (Block block in rfb.Blocks)
                    {
                        foreach (Entity entity in block.Entities)
                        {
                            if (entity is BlockReference)
                            {
                                BlockReference br = (BlockReference) entity;

                                if (renamedBlocks.Contains(br.BlockName))
                                {
                                    br.BlockName = (string) renamedBlocks[br.BlockName];
                                }
                            }
                        }
                    }
                }
            }

            // Merges master collection read with the ones of the design workspace.
            rf.FillAllCollectionsData(design);

            if (rootBlockRef == null)
            {
                if (rfb != null && !string.IsNullOrEmpty(rfb.Blocks.RootBlockName))
                {
                    // File contains a root block with a name not presents in the scene, so I just add a BlockReference related to it.
                    rootBlockRef = new BlockReference(0, 0, 0, rfb.Blocks.RootBlockName, design.RootBlock.Units, design.Blocks, 0); // this constructor automatically scales the inserted content based on design units.
                   
                }
                else
                {
                    // File does not contain a root block, so I create a new one with the entities stored in the ReadFileAsync.Entities property.
                    var blockName = UtilityEx.GetUnusedBlockName(fileName, design.Blocks, true);
                    
                    Block block = new Block(blockName) {Units = rfb?.Units ?? design.RootBlock.Units};
                    block.Entities.AddRange(rf.Entities);
                    design.Blocks.Add(block);
                    rootBlockRef = new BlockReference(0, 0, 0, block.Name, design.RootBlock.Units, design.Blocks, 0); // this constructor automatically scales the inserted content based on design units.
                }
            }

            // Adds the BlockReference related to the RootBlock of the file to insert.
            if (ro == null)
                design.Entities.Add(rootBlockRef);
            else
                design.Entities.Add(rootBlockRef, ro);
            return rootBlockRef;
            
        }

        private static BlockKeyedCollection MergeBlocks(Design design, ReadFileAsyncWithBlocks rfb)
        {
            List<Block> blocks = new List<Block>();
            blocks.AddRange(design.Blocks);
            blocks.AddRange(rfb.Blocks);
            BlockKeyedCollection blockKeyedCollection = new BlockKeyedCollection();
            foreach (Block block in blocks)
                blockKeyedCollection.TryAdd(block);
            return blockKeyedCollection;
        }
    }
}
