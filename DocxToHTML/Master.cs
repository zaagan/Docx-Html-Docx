
using HTMLtoDOCX.Converter;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DocxToHTML
{
    public partial class Master : Form
    {

        string currentFile = string.Empty;
        public Master()
        {
            InitializeComponent();
            ClearScreen();
        }


        /// <summary>
        /// Source : http://stackoverflow.com/questions/5987432/can-the-ckeditor-be-used-in-a-winforms-application-for-xhtml-editing
        /// Source : http://stackoverflow.com/questions/31119631/getting-and-setting-ckeditor-html-in-a-c-net-winform-app
        /// 
        /// </summary>
        private void InitEditor()
        {
            string executionPath = Path.GetDirectoryName(Application.ExecutablePath);
            executionPath = executionPath + @"\EditorInit.html";
            wbHTMLViewer.Navigate(executionPath);
            Application.DoEvents();
            wbHTMLViewer.Document.InvokeScript("InitEditor");
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Office Files|*.docx";
            dialog.InitialDirectory = @"C:\";
            dialog.Title = "Please select your document (docx)";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                currentFile = dialog.FileName;
                lblFileName.Text = Path.GetFileName(currentFile);
            }
        }

        private void btnConvert_Click(object sender, EventArgs e)
        {

            try
            {
                if (File.Exists(currentFile))
                {
                    Cursor = Cursors.WaitCursor;
                    string htmlOutput = string.Empty;
                    Task.Factory.StartNew(() =>
                    {
                        DocxToHTML.Converter.HTMLConverter converter = new DocxToHTML.Converter.HTMLConverter();
                        htmlOutput = converter.ConvertToHtml(currentFile);
                    }).Wait();


                    Object[] objArray = new Object[1];
                    objArray[0] = htmlOutput;
                    wbHTMLViewer.Document.InvokeScript("setHtml", objArray);
                    //wbHTMLViewer.DocumentText = htmlOutput;
                }
                else
                { MessageBox.Show("File does not exist", "File not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation); }

                Cursor = Cursors.Default;

            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                MessageBox.Show(ex.Message, "Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

        }

        private void btnSaveHTML_Click(object sender, EventArgs e)
        {

            string sHtml = (string)wbHTMLViewer.Document.InvokeScript("getHtml");

            SaveFileDialog saveFileDiag = new SaveFileDialog();
            saveFileDiag.Filter = "Docx File (.docx) |*.docx";


            if ((saveFileDiag.ShowDialog() == DialogResult.OK))
            {


                string fileName = saveFileDiag.FileName;

                if (Path.GetExtension(fileName).ToLower() != ".docx")
                    fileName += ".docx";

                DocxConverter converter = new DocxConverter();
                int output = converter.ConvertToDocx(sHtml, fileName);

                if (output == 1)
                {
                    ClearScreen();
                    MessageBox.Show("File saved successfully !!", "Good going", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                    MessageBox.Show("Error in file conversion !!", "Error Occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

            }

        }


        private void ClearScreen()
        {
            currentFile = string.Empty;
            lblFileName.Text = "No file selected";
            InitEditor();
        }

    }
}
