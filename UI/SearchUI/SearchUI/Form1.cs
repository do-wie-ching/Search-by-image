﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using System.IO;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace SearchUI
{
    public partial class Form1 : Form
    {
        String imgpath;
        const int IMG_W = 32, IMG_H = 32;
        static int img_type;
        delegate void ProcessFile(object i);
        List<String> pathLists;
        int count = 1;

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        public Form1()
        {
            InitializeComponent();
        }

        static float[] LoadTensorFromFile(ref Mat img)
        {
            //Input image to buffer
            int size = (int)img.Total() * img.ElemSize();
            Console.WriteLine("size {0}", size);
            float[] tensorData = new float[size];
            //img.ToBytes().Clone(tensorData);

            return  tensorData;
        }
        static void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                String res;
                Console.WriteLine(e.Data + Environment.NewLine);
                res = e.Data;
                //Remove "[]"
                res = res.Replace("[","");
                res = res.Replace("]", "");
                img_type = Int32.Parse(res);//To int
            }
        }

        private bool RunONNX()
        {
            try
            {
                string args = "-u";
                string strArr = imgpath;
                string sArguments = @"..\..\..\..\..\..\predict_test.py";

                Process p = new Process();
                p.StartInfo.FileName = @"D:\Anaconda3\envs\TENSOR\python.exe";

                sArguments += " " + strArr;
                sArguments += " " + args;

                p.StartInfo.Arguments = sArguments;

                p.StartInfo.UseShellExecute = false;

                p.StartInfo.RedirectStandardOutput = true;

                p.StartInfo.RedirectStandardInput = true;

                p.StartInfo.RedirectStandardError = true;

                p.StartInfo.CreateNoWindow = true;

                p.Start();
                p.BeginOutputReadLine();
                p.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
                Console.ReadLine();
                p.WaitForExit();
            }
            catch(Exception ex)
            {
                Console.WriteLine("inputMeta.Keys {0}", ex.ToString());
                return false;
            }

            return true;
        }
           

        private FileExtension checkimgfile(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            System.IO.BinaryReader br = new System.IO.BinaryReader(fs);
            FileExtension extension;
            string fileType = string.Empty;
            try
            {
                byte data = br.ReadByte();
                fileType += data.ToString();
                data = br.ReadByte();
                fileType += data.ToString();
                
                
                extension = (FileExtension)Enum.Parse(typeof(FileExtension), fileType);

         
                switch (extension)
                {
                    case FileExtension.GIF:
                    case FileExtension.JPG:
                    case FileExtension.PNG:
                        break;
                    default:
                        extension = FileExtension.VALIDFILE;
                        break;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            
                if (fs != null)
                {
                    fs.Close();
                    br.Close();
                }

            return extension;
        }


        public enum FileExtension
        {
            JPG = 255216,
            GIF = 7173,
            PNG = 13780,
            VALIDFILE = 9999999
        }
        private void browserbtn_Click(object sender, EventArgs e)
        {
            RespicBox.Image = null;
            FileExtension extension;
            openFileDialog1.Title = "C# Corner Open File Dialog";
            openFileDialog1.InitialDirectory = @"c:\";
            openFileDialog1.Filter = "All files (*.*)|*.*|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                load_files_txtbox.Text = openFileDialog1.FileName;
               

                extension = checkimgfile(load_files_txtbox.Text);
                if (extension == FileExtension.VALIDFILE)
                {
                    MessageBox.Show("VALIDFILE", "Error");
                    load_files_txtbox.Clear();
                }
                else
                {
                    imgpath = load_files_txtbox.Text;
                }
            }

        }
        private List<String> find_similar_img()
        {
            ThreadPool.SetMaxThreads(4, 4);
            List<String> pathLists = new List<String>();
            FileExtension extension;

            String basepath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); //Get user desktop path
            String targetpath = (@basepath +@"\" +Convert.ToString(img_type));

            #region lambda
            ProcessFile processfile = directory =>//Use lambda
            {
                Object thisLock = new Object();//Use lock to avoid race condition
                Console.WriteLine(Thread.CurrentThread.GetHashCode());
                lock (thisLock)
                {
                    String str = "";
                    DirectoryInfo d = new DirectoryInfo((String)@directory);//Assuming get file info from each directory
                    FileInfo[] Files = d.GetFiles("*.*"); //Getting files info 


                    foreach (FileInfo file in Files)//Get files
                    {
                        str = "";
                        str += (@directory + "\\" + file.Name);
                        extension = checkimgfile(str);
                        if (extension == FileExtension.VALIDFILE)
                        {
                            continue;
                        }
                        pathLists.Add(str);
                    }
                }
               
            };
            #endregion

            ThreadPool.QueueUserWorkItem(new WaitCallback(processfile), targetpath);//Use threads to find image file
            Thread.Sleep(2000);

            return pathLists;
        }

         private void RespicBox_Click(object sender, EventArgs e)//Show images
        {
            OpenCvSharp.Size size = new OpenCvSharp.Size(RespicBox.Width, RespicBox.Height);
            Mat resultimg = Cv2.ImRead(pathLists[count]);
            Cv2.Resize(resultimg, resultimg, size, 0, 0, InterpolationFlags.Cubic);

            Bitmap bmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(resultimg);

            if (count > 3)
                count = 0;

            //RespicBox.Load(pathLists[count]);
            RespicBox.Image = bmp;
            count++;
        }

        private void RespicBox_MouseEnter(object sender, EventArgs e)
        {
            RespicBox.Cursor = Cursors.Hand;
        }

        private void RespicBox_MouseLeave(object sender, EventArgs e)
        {
            RespicBox.Cursor = Cursors.Default;
        }

        private void OKbtn_Click(object sender, EventArgs e)
        {
            
            RunONNX();

            pathLists = find_similar_img();

            load_files_txtbox.Clear();

            RespicBox.Load(pathLists[0]);//Show frist image when we load app fristly.
        }
    }
}
