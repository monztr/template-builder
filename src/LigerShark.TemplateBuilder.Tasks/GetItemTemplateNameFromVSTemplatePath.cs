﻿namespace LigerShark.TemplateBuilder.Tasks {
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using System.IO;
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;
    using System.Net;

    public class GetItemTemplateNameFromVSTemplatePath : Microsoft.Build.Utilities.Task {

        public GetItemTemplateNameFromVSTemplatePath() {
            this.Success = true;
        }
        public bool Success { get; private set; }

        # region Inputs
        [Required]
        public string VstemplateFilePath { get; set; }
        [Required]
        public string ItemTemplateRoot { get; set; }

        public string ItemTemplateZipRootFolder { get; set; }

        public string CustomTemplatesFolder { get; set; }
        #endregion

        #region Outputs
        [Output]
        public string ItemTemplateName { get; set; }

        [Output]
        public string ItemTemplateFolder { get; set; }
        
        [Output]
        public string OutputPathFolder { get; set; }

        [Output]
        public string OutputPathWithFileName { get; set; }
        #endregion

        private string GetCustomOutputPathFolder(string templateRoot) {
            if (string.IsNullOrEmpty(templateRoot)) { throw new ArgumentNullException("templateRoot"); }

            string result = null;
            // see if there is a _preprocess.xml file in the root of the folder
            FileInfo preprocessFi = new FileInfo(Path.Combine(templateRoot, "_preprocess.xml"));
            if (preprocessFi.Exists) {
                // parse the file
                TemplateInfo templateInfo = TemplateInfo.BuildTemplateInfoFrom(preprocessFi.FullName);
                if (!string.IsNullOrEmpty(templateInfo.OverridePath)) {
                    result = templateInfo.OverridePath;
                }
            }

            return result;
        }
        public override bool Execute() {
                Log.LogMessage("GetItemTemplateNameFromVSTemplatePath Starting");
                System.IO.FileInfo vsTemplateFileInfo = new System.IO.FileInfo(VstemplateFilePath);
                System.IO.DirectoryInfo di = vsTemplateFileInfo.Directory;

                var ItemTemplateFolderInfo = new System.IO.FileInfo(di.Parent.FullName);
                ItemTemplateFolder = ItemTemplateFolderInfo.FullName + @"\";

                // we need to get the name of the first folder under 'ItemTemplates' (ItemTemplateRoot)
                var itemTemplateRootUri = new Uri(ItemTemplateRoot);
                var relFolder = itemTemplateRootUri.MakeRelativeUri(new Uri(VstemplateFilePath)).ToString();
                var templateRelPath = relFolder.Substring(0, relFolder.IndexOf('/'));

                if (ItemTemplateZipRootFolder == null) {
                    ItemTemplateZipRootFolder = string.Empty;
                }

                string itRootFileName = di.Parent.Name;
                string subFolder = this.CustomTemplatesFolder;

                string customOutputPathFolder = GetCustomOutputPathFolder(ItemTemplateFolder);

                // set OutputFolder
                // if the name is 
                //  'CSharp.vstemplate' -> CSharp\
                //  'Web.CSharp.vstemplate' -> CSharp\Web\
                //  'VB.vstemplate' -> VisualBasic\
                //  'Web.VB.vstemplate' -> VisualBasic\Web\
                //  'fsharp.vstemplate' -> FSharp\
                if (string.Compare(@"CSharp.vstemplate", vsTemplateFileInfo.Name, StringComparison.OrdinalIgnoreCase) == 0) {
                    ItemTemplateName = string.Format("{0}.csharp", itRootFileName);                    
                    OutputPathFolder = string.Format(@"{0}CSharp\{1}\{2}", ItemTemplateZipRootFolder, templateRelPath, subFolder);
                }
                else if (string.Compare(@"Web.CSharp.vstemplate", vsTemplateFileInfo.Name, StringComparison.OrdinalIgnoreCase) == 0) {
                    ItemTemplateName = string.Format("{0}.web.csharp", itRootFileName);

                    // web site templates do not support any nesting
                    OutputPathFolder = string.Format(@"{0}CSharp\Web\{1}", ItemTemplateZipRootFolder, subFolder);
                }
                else if (string.Compare(@"VB.vstemplate", vsTemplateFileInfo.Name, StringComparison.OrdinalIgnoreCase) == 0) {
                    ItemTemplateName = string.Format("{0}.VB", itRootFileName);
                    OutputPathFolder = string.Format(@"{0}VisualBasic\{1}\{2}", ItemTemplateZipRootFolder, templateRelPath, subFolder);
                }
                else if (string.Compare(@"Web.VB.vstemplate", vsTemplateFileInfo.Name, StringComparison.OrdinalIgnoreCase) == 0) {
                    ItemTemplateName = string.Format("{0}.web.VB", itRootFileName);

                    // web site templates do not support any nesting
                    OutputPathFolder = string.Format(@"{0}VisualBasic\Web\{1}", ItemTemplateZipRootFolder, subFolder);
                }
                else if (string.Compare(@"fsharp.vstemplate", vsTemplateFileInfo.Name, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    ItemTemplateName = string.Format("{0}.fsharp", itRootFileName);
                    OutputPathFolder = string.Format(@"{0}FSharp\{1}\{2}", ItemTemplateZipRootFolder, templateRelPath, subFolder);
                }
                else if (string.Compare(@"_project.vstemplate.xml", vsTemplateFileInfo.Name, StringComparison.OrdinalIgnoreCase) == 0) {
                    // TODO: What to do here?
                    ItemTemplateName = string.Format("{0}.project", itRootFileName);
                    OutputPathFolder = string.Format(@"{0}CSharp\{1}\{2}", ItemTemplateZipRootFolder, templateRelPath, subFolder);
                }
                else {
                    Log.LogError("Unknown value for ItemTemplateName: [{0}]. Supported values include 'CSharp.vstemplate','Web.CSharp.vstemplate','VB.vstemplate' and 'Web.VB.vstemplate' ", vsTemplateFileInfo.Name);
                    return false;
                }

                if (!string.IsNullOrEmpty(customOutputPathFolder)) {
                    OutputPathFolder = string.Format(@"{0}"+customOutputPathFolder, ItemTemplateZipRootFolder, templateRelPath, subFolder);
                }

                OutputPathWithFileName = string.Format(@"{0}\{1}{2}", OutputPathFolder, itRootFileName,di.Parent.Extension);                 

            return Success;
        }
    }
}