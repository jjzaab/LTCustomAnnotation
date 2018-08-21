using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Leadtools;
using Leadtools.Annotations.Core;
using Leadtools.Annotations.Designers;
using Leadtools.Annotations.Automation;
using Leadtools.Codecs;
using Leadtools.Drawing;
using Leadtools.Controls;
using Leadtools.Annotations.WinForms;
using Leadtools.ScreenCapture;
using Leadtools.WinForms;


namespace TestAnnotations
{
    public partial class Form1 : Form
    {
        private ImageViewer viewer;
        private ImageViewerAutomationControl automationControl;
        // The Automation Manager is used to manage the automation mode. 
        private AnnAutomationManager annAutomationManager;
        private AnnAutomation automation;
        private IAnnAutomationControl _automationControl;
        ScreenCaptureEngine scEngine = new ScreenCaptureEngine();
        private RasterThumbnailBrowser thumbnailBrowser;

        public Form1()
        {
            InitializeComponent();
            RasterSupport.SetLicense(@"C:\LEADTOOLS 19\Common\License\LEADTOOLS.LIC", System.IO.File.ReadAllText(@"C:\LEADTOOLS 19\Common\License\LEADTOOLS.LIC.KEY"));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            viewer = new ImageViewer();
            viewer.Dock = DockStyle.Fill;

            automationControl = new ImageViewerAutomationControl();
            automationControl.ImageViewer = viewer;

            ScreenCaptureEngine.Startup();
            scEngine.CaptureInformation += new EventHandler<ScreenCaptureInformationEventArgs>(scEngine_CaptureInformation);
            ScreenCaptureOptions captureOptions = scEngine.CaptureOptions;
            captureOptions.Hotkey = Keys.None;
            scEngine.CaptureOptions = captureOptions;

            // initialize a new RasterCodecs object 
            RasterCodecs codecs = new RasterCodecs();
            // load the main image into the viewer 
            viewer.Image = codecs.Load(@"C:\Users\Public\Documents\LEADTOOLS Images\OCR1.TIF");
            viewer.Zoom(ControlSizeMode.FitAlways, 1.0, viewer.DefaultZoomOrigin);

            // initialize the interactive mode for the viewer 
            AutomationInteractiveMode automationInteractiveMode = new AutomationInteractiveMode();
            automationInteractiveMode.AutomationControl = automationControl;

            // add the interactive mode to the viewer 
            viewer.InteractiveModes.BeginUpdate();
            viewer.InteractiveModes.Add(automationInteractiveMode);
            viewer.InteractiveModes.EndUpdate();

            if (viewer.Image != null)
            {
                // create and set up the automation manager 
                annAutomationManager = new AnnAutomationManager();
                annAutomationManager.RestrictDesigners = true;
                annAutomationManager.EditObjectAfterDraw = false;

                // Instruct the manager to create all of the default automation objects. 
                annAutomationManager.CreateDefaultObjects();

                AnnObservableCollection<AnnAutomationObject> annObservable = annAutomationManager.Objects;
                foreach(AnnAutomationObject annObject in annObservable)
                {
                    if (annObject.Id != AnnObject.SelectObjectId)
                    {
                      //  annObservable.Remove(annObject);
                       // annAutomationManager.Objects.Remove(annObject);
                    }
                }


                // initialize the manager helper and create the toolbar and add it then the viewer to the controls 
                AutomationManagerHelper managerHelper = new AutomationManagerHelper(annAutomationManager);
                managerHelper.CreateToolBar();
                managerHelper.ToolBar.Dock = DockStyle.Right;
                Controls.Add(managerHelper.ToolBar);
                Controls.Add(viewer);
                

                // set up the automation (it will create the container as well) 
                automation = new AnnAutomation(annAutomationManager, automationControl);
                automation.EditContent += new EventHandler<AnnEditContentEventArgs>(automation_EditContent);
                // set this automation as the active one 
                automation.Active = true;

                //set the size of the container to the size of the viewer 
                automation.Container.Size = automation.Container.Mapper.SizeToContainerCoordinates(LeadSizeD.Create(viewer.Image.ImageWidth, viewer.Image.ImageHeight));
            }

        }


        public AnnAutomation Automation
        {
            get
            {
                if (_automationControl != null)
                    return _automationControl.AutomationObject as AnnAutomation;
                else
                    return null;
            }
        }

        void automation_EditContent(object sender, AnnEditContentEventArgs e)
        {
            AnnObject annObject = e.TargetObject;

            if (annObject == null || !annObject.SupportsContent || annObject is AnnSelectionObject)
                return;

            if (sender is AnnDrawDesigner && annObject.Id != AnnObject.StickyNoteObjectId)
                return;

            using (var dlg = new AutomationUpdateObjectDialog())
            {
                dlg.Automation = this.Automation;
                dlg.SetPageVisible(AutomationUpdateObjectDialogPage.Properties, false);
                dlg.SetPageVisible(AutomationUpdateObjectDialogPage.Reviews, false);
                dlg.TargetObject = annObject;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                    Automation.InvalidateObject(annObject);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ScreenCaptureAreaOptions captureAreaOptions = ScreenCaptureEngine.DefaultCaptureAreaOptions;
            captureAreaOptions.AreaType = ScreenCaptureAreaType.Rectangle;
            captureAreaOptions.Flags = ScreenCaptureAreaFlags.ShowDrawCursor;
            // hide our window
            this.SendToBack();
            // Start the capturing (snipping) operation
            try
            {
                scEngine.CaptureArea(captureAreaOptions, null);
            }
            catch (Exception)
            {
                // If something goes wrong or the user hits ESC, don't try to OCR
                return;
            }
            finally
            {
                // Show our window whether the user captured or not.
                this.BringToFront();
            }
        }

        void scEngine_CaptureInformation(object sender, ScreenCaptureInformationEventArgs e)
        {
            e.Image.XResolution = e.Image.YResolution = 300;
            AnnAutomationObject customAnn = annAutomationManager.FindObjectById(AnnObject.StampObjectId);
            AnnStampObject customStamp = (AnnStampObject) customAnn.ObjectTemplate;
            customStamp.Text = " ";
            
            customStamp.Fill = null;
            customStamp.Stroke = AnnStroke.Create(AnnSolidColorBrush.Create("Red"), LeadLengthD.Create(0)); ;
            customStamp.Picture = new AnnPicture(RasterImageConverter.ConvertToImage(e.Image, ConvertToImageOptions.None));
        }
    }
}