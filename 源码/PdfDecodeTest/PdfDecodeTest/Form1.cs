using PdfDecode;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PdfDecodeTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if(openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    DecodeLogic process = new DecodeLogic(true,true,true,300,300,300,5000);
                    process.PdfDeCodeResults += Process_PdfDeCodeResults;
                    process.PdfDecode(openFileDialog.FileName, true, 0, 0, 3);
                }
                catch (Exception )
                {

                    throw;
                }
                
            }

        }

        private void Process_PdfDeCodeResults(string pdfPath, bool deCodeStatus, double deCodeUsedTime, Dictionary<int, List<string>> codeResults, string errMsg)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new DecodeLogic.OnPdfDeCodeResults(Process_PdfDeCodeResults), pdfPath,deCodeStatus,deCodeUsedTime,codeResults,errMsg);
                return;
            }
            if (deCodeStatus)
            {
                dataGridView1.Rows.Clear();
                label1.Text = string.Format("解码时间：{0}", deCodeUsedTime);
                foreach (var pageResult in codeResults)
                {
                    foreach (var codeResult in pageResult.Value)
                    {
                        dataGridView1.Rows.Insert(0,pageResult.Key,codeResult);
                    }
                }
            }
            else
            {
                MessageBox.Show(errMsg);
            }
        }
    }
}
