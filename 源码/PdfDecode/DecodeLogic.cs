using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using O2S.Components.PDFRender4NET;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing;
using Cognex.VisionPro;
using Cognex.VisionPro.ID;
using System.Threading.Tasks;
using System.Threading;

namespace PdfDecode
{
    public class DecodeLogic
    {
        /// <summary>
        /// 一维码解码
        /// </summary>
        bool _deCode1d;
        double _timeOut1d;

        /// <summary>
        /// DM码解码
        /// </summary>
        bool _deCodeDM;
        double _timeOutDM;

        /// <summary>
        /// QR码解码
        /// </summary>
        bool _deCodeQR;
        double _timeOutQR;

        /// <summary>
        /// 单页解码超时
        /// </summary>
        double _pageDecodeTimeOut;

        /// <summary>
        /// pdf解码结果委托
        /// </summary>
        /// <param name="pdfPath">pdf文件目录</param>
        /// <param name="deCodeStatus">解码是否存在异常</param>
        /// <param name="deCodeUsedTime">解码所用时间</param>
        /// <param name="pageResults">解码结果合集</param>
        /// <param name="errMsg">异常信息</param>
        public delegate void OnPdfDeCodeResults(string pdfPath,bool deCodeStatus,double deCodeUsedTime, Dictionary<int,List<string>> pageResults,string errMsg);

        public event OnPdfDeCodeResults PdfDeCodeResults;

        /// <summary>
        /// 解码逻辑类
        /// </summary>
        /// <param name="deCode1d">是否解条形码</param>
        /// <param name="deCodeDM">是否解DM码</param>
        /// <param name="deCodeQR">是否解QR码</param>
        /// <param name="timeOut1d">一维码超时(ms)</param>
        /// <param name="timeOutDM">DM码超时(ms)</param>
        /// <param name="timeOutQR">QR码超时(ms)</param>
        /// <param name="pageDecodeTimeOut">单页解码超时(ms)</param>
        public DecodeLogic(bool deCode1d, bool deCodeDM, bool deCodeQR, double timeOut1d, double timeOutDM, double timeOutQR, double pageDecodeTimeOut)
        {
            _deCode1d = deCode1d;
            _deCodeDM = deCodeDM;
            _deCodeQR = deCodeQR;
            _timeOut1d = timeOut1d;
            _timeOutDM = timeOutDM;
            _timeOutQR = timeOutQR;
            _pageDecodeTimeOut = pageDecodeTimeOut;
        }

        /// <summary>
        /// Pdf解码
        /// </summary>
        /// <param name="pdfPath">pdf文件路径</param>
        /// <param name="allPageDecode">所有页面都解码</param>
        /// <param name="startPage">开始解码页数</param>
        /// <param name="endPage">结束解码页数</param>
        /// <param name="definition">pdf转图片质量（1~10）影响速度</param>
        public void PdfDecode(string pdfPath,bool allPageDecode,int startPage,int endPage,int definition)
        {
            int codeCount = 0;
            DateTime startTime = DateTime.Now;
           Dictionary<int, List<string>> codeResults = new Dictionary<int, List<string>>();
            string errMsg = string.Empty;
            bool status = true;
            List<Bitmap> bitmaps = PdfConvertBitmap(pdfPath, allPageDecode, startPage, endPage, definition);
            if (bitmaps.Count > 0)
            {
                for (int i = 0; i < bitmaps.Count; i++)
                {
                    List<string> pageResult = new List<string>();
                    if (VpDecode(bitmaps[i], out pageResult))
                    {
                        codeResults.Add(i + 1, pageResult);
                        codeCount = codeCount + pageResult.Count;
                    }
                    else
                    {
                        status = false;
                        errMsg = string.Format("{0}PDF页数{1}解码超时，请尝试增加超时时间！\r\n",errMsg,i+1);
                    }
                }
                if (codeCount == 0)
                {
                    status = false;
                    errMsg = string.Format("{0}解码结果集为空，请尝试提高图像质量！\r\n", errMsg);
                }
            }
            else
            {
                status = false;
                errMsg = string.Format("{0}PDF拆分图像为空！\r\n", errMsg);
            }
            double deCodeTime = (DateTime.Now - startTime).TotalMilliseconds;
            if (PdfDeCodeResults != null)
            {
                PdfDeCodeResults(pdfPath, status, deCodeTime, codeResults, errMsg);
            }
        }

        private bool VpDecode(Bitmap img,out List<string> codeList)
        {
            bool status = false;
            List<string> _codeList = new List<string>();
            DateTime startTime = DateTime.Now;
            bool deCodeStatus1d = false;
            bool deCodeStatusDM = false;
            bool deCodeStatusQR = false;
            //一维码工具
            if (_deCode1d)
            {
                CogImage8Grey cogImg1 = new CogImage8Grey(img);
                Task task1d = new Task(() =>
                {
                    CogIDTool cogIDTool1d = new CogIDTool();
                    cogIDTool1d.RunParams.Timeout = _timeOut1d;
                    cogIDTool1d.RunParams.TimeoutEnabled = true;
                    cogIDTool1d.RunParams.DisableAllCodes();
                    cogIDTool1d.RunParams.Code128.Enabled = true;
                    cogIDTool1d.RunParams.Code39.Enabled = true;
                    cogIDTool1d.RunParams.Code93.Enabled = true;
                    cogIDTool1d.RunParams.UpcEan.Enabled = true;
                    cogIDTool1d.RunParams.ProcessingMode = CogIDProcessingModeConstants.IDMax;
                    cogIDTool1d.RunParams.NumToFind = 15;
                    cogIDTool1d.InputImage = cogImg1;
                    cogIDTool1d.Run();
                    if (cogIDTool1d.Results!=null)
                    {
                        foreach (var result in cogIDTool1d.Results)
                        {
                            lock (_codeList)
                            {
                                _codeList.Add(result.DecodedData.DecodedString);
                            }
                        }
                    }
                    deCodeStatus1d = true;
                });
                task1d.Start();
            }
            else
            {
                deCodeStatus1d = true;
            }
            //DM码工具
            if (_deCodeDM)
            {
                CogImage8Grey cogImgDm = new CogImage8Grey(img);
                Task taskDM = new Task(() =>
                {
                    CogIDTool cogIDToolDM = new CogIDTool();
                    cogIDToolDM.RunParams.Timeout = _timeOutDM;
                    cogIDToolDM.RunParams.TimeoutEnabled = true;
                    cogIDToolDM.RunParams.DisableAllCodes();
                    cogIDToolDM.RunParams.DataMatrix.Enabled = true;
                    cogIDToolDM.RunParams.ProcessingMode = CogIDProcessingModeConstants.IDMax;
                    cogIDToolDM.RunParams.NumToFind = 3;
                    cogIDToolDM.InputImage = cogImgDm;
                    cogIDToolDM.Run();
                    if (cogIDToolDM.Results!=null)
                    {
                        foreach (var result in cogIDToolDM.Results)
                        {
                            lock (_codeList)
                            {
                                _codeList.Add(result.DecodedData.DecodedString);
                            }
                        }
                    }                    
                    deCodeStatusDM = true;
                });
                taskDM.Start();
            }
            else
            {
                deCodeStatusDM = true;
            }
            //QR码工具
            if (_deCodeQR)
            {
                CogImage8Grey cogImgQr = new CogImage8Grey(img);
                Task taskQR = new Task(() =>
                {
                    CogIDTool cogIDToolQR = new CogIDTool();
                    cogIDToolQR.RunParams.Timeout = _timeOutQR;
                    cogIDToolQR.RunParams.TimeoutEnabled = true;
                    cogIDToolQR.RunParams.DisableAllCodes();
                    cogIDToolQR.RunParams.QRCode.Enabled = true;
                    cogIDToolQR.RunParams.ProcessingMode = CogIDProcessingModeConstants.IDMax;
                    cogIDToolQR.RunParams.NumToFind = 3;
                    cogIDToolQR.InputImage = cogImgQr;
                    cogIDToolQR.Run();
                    if (cogIDToolQR.Results!=null)
                    {
                        foreach (var result in cogIDToolQR.Results)
                        {
                            lock (_codeList)
                            {
                                _codeList.Add(result.DecodedData.DecodedString);
                            }
                        }
                    }
                    deCodeStatusQR = true;
                });
                taskQR.Start();
            }
            else
            {
                deCodeStatusQR = true;
            }
            while (true)
            {
                if (deCodeStatus1d && deCodeStatusDM && deCodeStatusQR)
                {
                    status = true;
                    break;
                }
                if ((DateTime.Now - startTime).TotalMilliseconds > _pageDecodeTimeOut)
                {
                    status = false;
                    break;
                }
                Thread.Sleep(10);
            }
            codeList = _codeList;

            return status;
        }

        /// <summary>
        /// pdf转bitmap
        /// </summary>
        /// <param name="pdfPath">pdf文件路径</param>
        /// <param name="usedAllPage">全部转换</param>
        /// <param name="startPageNum">开始页数</param>
        /// <param name="endPageNum">结束页数</param>
        /// <param name="definition">转换质量（1~10）</param>
        /// <returns></returns>
        private List<Bitmap> PdfConvertBitmap(string pdfPath,bool usedAllPage, int startPageNum,int endPageNum,int definition)
        {
            List<Bitmap> bitmaps = new List<Bitmap>();
            //打开pdf文件
            PDFFile pdfFile = PDFFile.Open(pdfPath);
            //页数处理
            if (usedAllPage)
            {
                startPageNum = 1;
                endPageNum = pdfFile.PageCount;
            }
            else
            {
                if (startPageNum <= 0)
                {
                    startPageNum = 1;
                }
                if (endPageNum > pdfFile.PageCount)
                {
                    endPageNum = pdfFile.PageCount;
                }
                if (startPageNum > endPageNum)
                {
                    int tempPageNum = startPageNum;
                    startPageNum = endPageNum;
                    endPageNum = startPageNum;
                }
            }
            //转换质量处理
            if (definition < 0)
            {
                definition = 1;
            }
            if(definition > 10)
            {
                definition = 10;
            }
            //转换图像
            for (int i = startPageNum; i <= endPageNum; i++)
            {
                Bitmap pageImage = pdfFile.GetPageImage(i - 1, 56 * definition);
                bitmaps.Add(pageImage);
            }
            pdfFile.Dispose();

            return bitmaps;
        }

        /// <summary>
        /// 将PDF转换为图片的方法
        /// </summary>
        /// <param name="pdfInputPath">PDF文件路径</param>
        /// <param name="imageOutputPath">图片输出路径</param>
        /// <param name="imageName">生成图片的名字</param>
        /// <param name="startPageNum">从PDF文档的第几页开始转换</param>
        /// <param name="endPageNum">从PDF文档的第几页开始停止转换</param>
        /// <param name="imageFormat">设置所需图片格式</param>
        /// <param name="definition">设置图片的清晰度，数字越大越清晰</param>
        public string[] PdfToPng(string pdfInputPath, string imageOutputPath,
            string imageName, int startPageNum, int endPageNum, ImageFormat imageFormat, int definition)
        {
            List<string> outFileList = new List<string>();
            PDFFile pdfFile = PDFFile.Open(pdfInputPath);
            if (!Directory.Exists(imageOutputPath))
            {
                Directory.CreateDirectory(imageOutputPath);
            }
            // validate pageNum
            if (startPageNum <= 0)
            {
                startPageNum = 1;
            }
            if (endPageNum > pdfFile.PageCount)
            {
                endPageNum = pdfFile.PageCount;
            }
            if (startPageNum > endPageNum)
            {
                int tempPageNum = startPageNum;
                startPageNum = endPageNum;
                endPageNum = startPageNum;
            }
            // start to convert each page
            if (endPageNum == 1)
            {
                Bitmap pageImage = pdfFile.GetPageImage(1 - 1, 56 * (int)definition);
                pageImage.Save(imageOutputPath + imageName + "." + imageFormat, imageFormat);
                pageImage.Dispose();
                outFileList.Add(imageOutputPath + imageName + "." + imageFormat);
            }
            else
            {
                for (int i = startPageNum; i <= endPageNum; i++)
                {
                    Bitmap pageImage = pdfFile.GetPageImage(i - 1, 56 * (int)definition);
                    pageImage.Save(imageOutputPath + imageName + i + "." + imageFormat, imageFormat);
                    pageImage.Dispose();
                    outFileList.Add(imageOutputPath + imageName + i + "." + imageFormat);
                }
            }
            pdfFile.Dispose();
            return outFileList.ToArray(); ;
        }
    }
}
