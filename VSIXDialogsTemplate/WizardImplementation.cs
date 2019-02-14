﻿using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TemplateWizard;
using System.Windows.Forms;
using EnvDTE;
using System.Management.Automation;
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;

namespace VSIXDialogsTemplate
{
    public class WizardImplementation : IWizard
    {
        private UserInputForm inputForm;
        private string customMessage;
        private string projectPath;
        private string folder;

        // This method is called before opening any item that   
        // has the OpenInEditor attribute.  
        public void BeforeOpeningFile(ProjectItem projectItem)
        {
        }

        public void ProjectFinishedGenerating(Project project)
        {
        }

        // This method is only called for item templates,  
        // not for project templates.  
        public void ProjectItemFinishedGenerating(ProjectItem
            projectItem)
        {
        }

        // This method is called after the project is created.  
        public void RunFinished()
        {
        }

        public void RunStarted(object automationObject,
            Dictionary<string, string> replacementsDictionary,
            WizardRunKind runKind, object[] customParams)
        {
            try
            {
                IntPtr hierarchyPointer, selectionContainerPointer;
                object selectedObject = null;
                IVsMultiItemSelect multiItemSelect;
                uint projectItemId;

                IVsMonitorSelection monitorSelection = (IVsMonitorSelection)Package.GetGlobalService(typeof(SVsShellMonitorSelection));

                monitorSelection.GetCurrentSelection(out hierarchyPointer, out projectItemId, out multiItemSelect, out selectionContainerPointer);

                IVsHierarchy selectedHierarchy = Marshal.GetTypedObjectForIUnknown(hierarchyPointer, typeof(IVsHierarchy)) as IVsHierarchy;

                if (selectedHierarchy != null)
                {
                    ErrorHandler.ThrowOnFailure(selectedHierarchy.GetProperty(projectItemId, (int)__VSHPROPID.VSHPROPID_ExtObject, out selectedObject));
                }

                Project selectedProject = selectedObject as Project;

                this.projectPath = selectedProject.FullName;
                folder = Path.GetDirectoryName(projectPath);

                // Display a form to the user. The form collects   
                // input for the custom message.  
                inputForm = new UserInputForm();
                inputForm.ShowDialog();

                customMessage = UserInputForm.CustomMessage;

                // Add custom parameters.  
                replacementsDictionary.Add("$custommessage$",
                    customMessage);

                RunScript();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        // This method is only called for item templates,  
        // not for project templates.  
        public bool ShouldAddProjectItem(string filePath)
        {
            return true;
        }

        public void RunScript()
        {
            using (PowerShell PowerShellInstance = PowerShell.Create())
            {
                //PowerShellInstance.AddScript(script);
                PowerShellInstance.AddScript(File.ReadAllText(".\\scripts\\DialogsContext.ps1"));
                PowerShellInstance.Runspace.SessionStateProxy.Path.SetLocation(folder);

                //PowerShell.exe - NoProfile - ExecutionPolicy Unrestricted - Command "& {Start-Process PowerShell -windowstyle hidden -ArgumentList '-NoProfile -ExecutionPolicy Unrestricted -noexit -File "$ScriptPath"' -Verb RunAs}"

                // begin invoke execution on the pipeline
                IAsyncResult result = PowerShellInstance.BeginInvoke();

                // do something else until execution has completed.
                // this could be sleep/wait, or perhaps some other work
                while (result.IsCompleted == false)
                {
                    Console.WriteLine("Waiting for pipeline to finish...");
                    //Thread.Sleep(1000);

                    // might want to place a timeout here...
                }

                Console.WriteLine("Finished!");
            }
        }
    }

    public partial class UserInputForm : Form
    {
        private static string customMessage;
        private TextBox textBox1;
        private Button button1;
        private Label label1;

        public UserInputForm()
        {
            this.Size = new System.Drawing.Size(1000, 700);
            this.Text = "Add New Dialog Wizard";

            label1 = new Label();
            label1.Text = "Enter bot file name";
            label1.Location = new System.Drawing.Point(20, 45);
            label1.Visible = true;
            this.Controls.Add(label1);

            textBox1 = new TextBox();
            textBox1.Location = new System.Drawing.Point(20, 85);
            textBox1.Size = new System.Drawing.Size(500, 50);
            this.Controls.Add(textBox1);

            button1 = new Button();
            button1.Location = new System.Drawing.Point(80, 85);
            button1.Size = new System.Drawing.Size(300, 50);
            button1.Text = "Add bot file";
            button1.Click += Button1_Click;
            this.Controls.Add(button1);
        }
        public static string CustomMessage
        {
            get
            {
                return customMessage;
            }
            set
            {
                customMessage = value;
            }
        }
        private void Button1_Click(object sender, EventArgs e)
        {
            customMessage = textBox1.Text;
            //RunScript();

            this.Close();
        }

        //private void RunScript()
        //{

        //    //PowerShell ps = PowerShell.Create(); //.AddCommand("Install-Package").AddParameter("Id", "BotBuilder.Dialogs").Invoke();
        //    //ps.Runspace.SessionStateProxy.Path.SetLocation("C:\\Repositories\\BotBuilder-Samples\\samples\\csharp_dotnetcore\\01.console-echo");
        //    //ps.AddCommand("Install-Package").AddParameter("-Name", "Microsoft.Bot.Builder.Dialogs");
        //    //ps.BeginInvoke();

        //    string script = "" +
        //        "$FileName = \"*Bot.cs\"" + "\n" +
        //        "$Patern = \"turnContext.Activity.Type == ActivityTypes.Message\"" + "\n" +
        //        "$FileOriginal = Get-Content $FileName" + "\n" +
        //        "[String[]] $FileModified = @()" + "\n" +
        //        "Foreach ($Line in $FileOriginal)" + "\n" +
        //        "{" + "\n" +
        //        "   $FileModified += $Line "+ "\n" +
        //        "   if ($Line -match $patern)" + "\n" +
        //        "   {" + "\n" +
        //        "       $foreach.movenext()" + "\n" +
        //        "       $FileModified += \"			{ \"" + "\n" +
        //        "       $FileModified += \"             var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);\"" + "\n" +
        //        "       $FileModified += \"\"" + "\n" +
        //        "       $FileModified += \"             var results = await dialogContext.ContinueDialogAsync(cancellationToken);\"" + "\n" +
        //        "       $FileModified += \"\"" + "\n" +
        //        "       $FileModified += \"             if (results.Status == DialogTurnStatus.Empty)\"" + "\n" +
        //        "       $FileModified += \"             {\"" + "\n" +
        //        "       $FileModified += \"                 await dialogContext.BeginDialogAsync(null, null, cancellationToken);\"" + "\n" +
        //        "       $FileModified += \"             }\"" + "\n" +
        //        "   }" + "\n" +
        //       "}" + "\n" +
        //       "Set-Content $fileName $FileModified" +
        //       "Install - Package Microsoft.Bot.Builder.Dialogs";

        //    using (PowerShell PowerShellInstance = PowerShell.Create())
        //    {
        //        PowerShellInstance.AddScript(script);
        //        PowerShellInstance.Runspace.SessionStateProxy.Path.SetLocation(projectPath);

        //        // begin invoke execution on the pipeline
        //        IAsyncResult result = PowerShellInstance.BeginInvoke();

        //        // do something else until execution has completed.
        //        // this could be sleep/wait, or perhaps some other work
        //        while (result.IsCompleted == false)
        //        {
        //            Console.WriteLine("Waiting for pipeline to finish...");
        //            //Thread.Sleep(1000);

        //            // might want to place a timeout here...
        //        }

        //        Console.WriteLine("Finished!");
        //    }
        //}
    }
}
