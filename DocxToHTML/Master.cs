using HtmlToDocx.Converter;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DocxToHtml;

public partial class Master : Form
{

    string currentFile = string.Empty;
    public Master()
    {
        InitializeComponent();
        ClearScreen();
    }

    /// <summary>
    /// Source : http://stackoverflow.com/questions/5987432/can-the-ckeditor-be-used-in-a-winforms-application-for-xHtml-editing
    /// Source : http://stackoverflow.com/questions/31119631/getting-and-setting-ckeditor-Html-in-a-c-net-winform-app
    /// 
    /// </summary>
    private void InitEditor()
    {
        var executionPath = Path.GetDirectoryName(Application.ExecutablePath);
        executionPath = executionPath + @"\EditorInit.Html";
        wbHtmlViewer.Navigate(executionPath);
        Application.DoEvents();
        //wbHtmlViewer.Document.InvokeScript("InitEditor");
    }

    private void btnOpenFile_Click(object sender, EventArgs e)
    {
        var dialog = new OpenFileDialog();
        dialog.Filter = @"Office Files|*.docx";
        dialog.InitialDirectory = @"C:\";
        dialog.Title = @"Please select your document (docx)";
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
                var HtmlOutput = string.Empty;
                Task.Factory.StartNew(() =>
                {
                    var converter = new Converter.HtmlConverter();
                    HtmlOutput = converter.ConvertToHtml(currentFile);
                }).Wait();


                var objArray = new Object[1];
                objArray[0] = HtmlOutput;
                wbHtmlViewer.Document!.InvokeScript("setHtml", objArray);
            }
            else
            { MessageBox.Show(@"File does not exist", @"File not found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation); }

            Cursor = Cursors.Default;
        }
        catch (Exception ex)
        {
            Cursor = Cursors.Default;
            MessageBox.Show(ex.Message, @"Exception occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
    }

    private void btnSaveHtml_Click(object sender, EventArgs e)
    {

        var sHtml = (string)wbHtmlViewer.Document!.InvokeScript("getHtml");

        var saveFileDiag = new SaveFileDialog();
        saveFileDiag.Filter = @"Docx File (.docx) |*.docx";


        if ((saveFileDiag.ShowDialog() == DialogResult.OK))
        {
            var fileName = saveFileDiag.FileName;

            if (Path.GetExtension(fileName).ToLower() != ".docx")
                fileName += ".docx";

            var converter = new DocxConverter();
            var output = converter.ConvertToDocx(sHtml, fileName);

            if (output == 1)
            {
                ClearScreen();
                MessageBox.Show(@"File saved successfully !!", @"Good going", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                MessageBox.Show(@"Error in file conversion !!", @"Error Occured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

        }
    }

    private void ClearScreen()
    {
        currentFile = string.Empty;
        lblFileName.Text = @"No file selected";
        InitEditor();
    }

}