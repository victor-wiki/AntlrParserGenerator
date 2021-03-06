﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Configuration;
using System.Reflection;
using System.Diagnostics;

namespace AntlrParserGenerator
{
    public partial class frmMain : Form
    {
        private readonly string currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private string jarFileName = "antlr-4.8-complete.jar";
        private string jarFilePath;
        private bool hasError = false;

        public frmMain()
        {
            InitializeComponent();
            RichTextBox.CheckForIllegalCrossThreadCalls = false;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            string configJarFileName = ConfigurationManager.AppSettings["JarFileName"];

            if (!string.IsNullOrEmpty(configJarFileName))
            {
                this.jarFileName = configJarFileName;
            }

            this.jarFilePath = Path.Combine(this.currentFolder, this.jarFileName);
            this.InitControls();
        }

        private void InitControls()
        {
            string grammarFileExtension = ConfigurationManager.AppSettings["GrammarFileExtension"] ?? ".g4";
            this.openFileDialog1.Filter = $"grammar file|*{grammarFileExtension}|all files|*.*";

            string[] languages = ConfigurationManager.AppSettings["Languages"]?.Split(',');
            if (languages == null || languages.Length == 0)
            {
                languages = Enum.GetNames(typeof(Language));
            }

            this.cboTragetLanguage.Items.AddRange(languages);

            this.cboTragetLanguage.SelectedIndex = 0;
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            DialogResult result = this.openFileDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                this.txtFile.Text = string.Join("|", this.openFileDialog1.FileNames);
            }
        }       

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            string strFiles = this.txtFile.Text;

            string[] fileItems = strFiles.Split('|');
            if (fileItems.Length == 0)
            {
                MessageBox.Show("Please select grammar file first.");
                return;
            }

            IEnumerable<FileInfo> files = fileItems.Select(item => new FileInfo(item));

            IEnumerable<FileInfo> lexerFiles = files.Where(item => item.Name.ToLower().Contains("lexer"));
            IEnumerable<FileInfo> parserFiles = files.Where(item => item.Name.ToLower().Contains("parser"));
            IEnumerable<FileInfo> otherFiles = files.Where(item => !item.Name.ToLower().Contains("lexer") && !item.Name.Contains("parser"));

            List<FileInfo> sortedFiles = new List<FileInfo>();

            sortedFiles.AddRange(lexerFiles);
            sortedFiles.AddRange(parserFiles);
            sortedFiles.AddRange(otherFiles);

            this.hasError = false;
            this.btnGenerate.Enabled = false;
            this.txtMessage.Text = "";
            this.txtMessage.ForeColor = Color.Black;

            foreach (FileInfo file in sortedFiles)
            {
                if (this.hasError)
                {
                    break;
                }

                ProcessHelper.ExecuteCommand(this.jarFilePath, this.BuildCommand(file), this.Process_OutputDataReceived, this.Process_ErrorDataReceived);
            }

            if (!this.hasError)
            {
                MessageBox.Show("Parser files have been generated");

                this.OpenInExplorer(sortedFiles.First().FullName);
            }
            else
            {
                MessageBox.Show("Error occurs while generating.");
            }

            this.btnGenerate.Enabled = true;
        }

        private void OpenInExplorer(string filePath)
        {
            string cmd = "explorer.exe";
            string arg = "/select," + filePath;
            Process.Start(cmd, arg);
        }

        private string BuildCommand(FileInfo file)
        {
            return $"java -jar {this.jarFileName} -Dlanguage={this.cboTragetLanguage.Text} \"{file.FullName}\"";
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                this.AppendMessage(false, e.Data);
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                this.hasError = true;
                this.AppendMessage(true, e.Data);
            }
        }

        private void AppendMessage(bool isError, string message)
        {
            int start = this.txtMessage.Text.Length;

            if (start > 0)
            {
                this.txtMessage.AppendText(Environment.NewLine);
            }

            this.txtMessage.AppendText(message);

            this.txtMessage.Select(start, this.txtMessage.Text.Length - start);
            this.txtMessage.SelectionColor = isError ? Color.Red : Color.Black;

            this.txtMessage.SelectionStart = this.txtMessage.TextLength;
            this.txtMessage.ScrollToCaret();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }

    public enum Language
    {
        Java,
        CSharp,
        Python2,
        Python3,
        Cpp,
        JavaScript,
        Go,
        Swift
    }
}
