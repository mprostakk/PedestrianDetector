using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.UI;
using Emgu.CV.Structure;
using Emgu.CV.BgSegm;
using Emgu.CV.Cvb;

using Emgu.CV.Features2D;
using Emgu.CV.Cuda;
using Emgu.CV.Util;

namespace EmguCudaTest
{
    public partial class Form1 : Form
    {
        VideoCapture _capture;
        
        CudaBackgroundSubtractorMOG2 cudaBgMOG2;
        //SimpleBlobDetectorParams param;
        //SimpleBlobDetector blobDetector;
        //CvBlobs blobs = new CvBlobs();

        Mat frame;
        Mat gray;
        Mat outSub;
        GpuMat gpuFrame;
        GpuMat gpuSub;

        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
        VectorOfVectorOfPoint contoursGood = new VectorOfVectorOfPoint();
        Mat hiererachy = new Mat();

        Mat element = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
        Image<Gray, byte> grayImage;
        Image<Gray, byte> mask;

        BlobList blobList = new BlobList();

        private void ProcessFrame(object sender, EventArgs e)
        {
            if (_capture != null && _capture.Ptr != IntPtr.Zero)
            {
                _capture.Retrieve(frame, 0);
                gpuFrame.Upload(frame);
                cudaBgMOG2.Apply(gpuFrame, gpuSub);
                CudaInvoke.Threshold(gpuSub, gpuSub, 12, 255, Emgu.CV.CvEnum.ThresholdType.Binary);
                gpuSub.Download(outSub);

                CvInvoke.FindContours(outSub, contours, hiererachy, Emgu.CV.CvEnum.RetrType.List, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxNone);
                
                for(int i = 0; i < contours.Size; i++)
                {
                    if(CvInvoke.ContourArea(contours[i]) > 50)
                    {
                        contoursGood.Push(contours[i]);
                    }
                }
                
                grayImage = new Image<Gray, byte>(frame.Width, frame.Height, new Gray(0));
                grayImage.SetZero();
                CvInvoke.DrawContours(grayImage, contoursGood, -1, new MCvScalar(255, 255, 255), -1);
                CvInvoke.Dilate(grayImage, grayImage, element, new Point(-1, -1), 6, Emgu.CV.CvEnum.BorderType.Constant, new MCvScalar(255, 255, 255));
                contoursGood.Clear();

                CvInvoke.FindContours(grayImage, contours, hiererachy, Emgu.CV.CvEnum.RetrType.List, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxNone);

                List<Point> points = new List<Point>();

                for(int i = 0; i < contours.Size; i++)
                {
                    MCvMoments moments = CvInvoke.Moments(contours[i], false);
                    Point WeightedCentroid = new Point((int)(moments.M10 / moments.M00), (int)(moments.M01 / moments.M00));
                    points.Add(WeightedCentroid);
                }

                blobList.AssignToBlobs(points);
                blobList.Draw(frame);
                blobList.Draw(mask);
                blobList.Update();

                CvInvoke.DrawContours(frame, contours, -1, new MCvScalar(0, 0, 255));

                imageBox1.Image = frame;
                imageBox2.Image = mask;

                grayImage.Dispose();
            }
        }

        public Form1()
        {
            InitializeComponent();

            string path = "1.mov";
            _capture = new VideoCapture(path);
            cudaBgMOG2 = new CudaBackgroundSubtractorMOG2();

            //blobDetector = new CvBlobDetector();
            //param = new SimpleBlobDetectorParams();
            //param.MinThreshold = 10;
            //param.MaxThreshold = 255;
            //param.FilterByArea = true;
            //param.MinArea = 54;
            //param.MaxArea = 30000;
            //blobDetector = new SimpleBlobDetector(param);

            _capture.ImageGrabbed += ProcessFrame;
            frame = new Mat();
            gray = new Mat();
            outSub = new Mat();
            gpuFrame = new GpuMat();
            gpuSub = new GpuMat();
            mask = new Image<Gray, byte>(_capture.Height, _capture.Width, new Gray(0));
            //mask.SetTo(new MCvScalar(0));

            if (_capture != null)
            {
                try
                {
                    _capture.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
