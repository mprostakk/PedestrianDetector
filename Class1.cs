using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmguCudaTest
{
    class MyBlob
    {
        List<Point> points;
        List<Point> vectors;
        public String name { get; set; }
        public bool found { get; set; }

        public MyBlob(Point p, String name)
        {
            points = new List<Point>();
            vectors = new List<Point>();
            points.Add(p);
            vectors.Add(new Point(0, 0));
            this.name = name;
            this.found = true;
        }

        public void Smooth(int val = 3)
        {
            int amount = 0;
            int sumX = 0;
            int sumY = 0;
            for(int i = points.Count-1; i >= 0 && i >= points.Count-1-val; i--)
            {
                sumX += points[i].X;
                sumY += points[i].Y;
                amount++;
            }
            Point tmp = new Point(sumX / amount, sumY / amount);
            points.RemoveAt(points.Count - 1);
            points.Add(tmp);
        }

        public Point LastPoint()
        {
            return points[points.Count - 1];
        }

        public Point LastVector()
        {
            return vectors[vectors.Count - 1];
        }

        public Point subtractPoints(Point p1, Point p2)
        {
            return new Point(p1.X - p2.X, p1.Y - p2.Y);
        }

        public void addPoint(Point p)
        {
            Point vectorPoint = subtractPoints(p, LastPoint());
            ScalePoint(vectorPoint, 50);
            vectors.Add(vectorPoint);
            points.Add(p);
            this.found = true;
        }

        public void ScalePoint(Point p, int scalar)
        {
            p.X *= scalar;
            p.Y *= scalar;
        }
    }

    class BlobList
    {
        List<MyBlob> blobs;

        public BlobList()
        {
            blobs = new List<MyBlob>();
        }

        int index = 0;
        public void AssignToBlobs(List<Point> points)
        {
            bool anyBlobFound = false;
            foreach (Point point in points)
            {
                MyBlob blob = NearestBlob(point);
                if(blob != null)
                {
                    if (distance(blob.LastPoint(), point) < 1000 && blob.found == false)
                    {
                        blob.addPoint(point);
                        blob.Smooth();
                    }
                    else if(blob.found == false)
                    {
                        blobs.Add(new MyBlob(point, String.Format("{0}", index)));
                        index++;
                    }
                }
                else if(anyBlobFound == false)
                {
                    blobs.Add(new MyBlob(point, String.Format("{0}", index)));
                    index++;
                    anyBlobFound = true;
                }
                
            }
        }

        public void Draw(Mat image)
        {
            foreach (MyBlob blob in blobs)
            {
                CvInvoke.Circle(image, blob.LastPoint(), 2, new Emgu.CV.Structure.MCvScalar(0, 0, 255), -1);
                CvInvoke.PutText(
                    image, 
                    blob.name, 
                    blob.LastPoint(), 
                    Emgu.CV.CvEnum.FontFace.HersheyComplex, 
                    1, new Emgu.CV.Structure.MCvScalar(255, 255, 255));

                CvInvoke.ArrowedLine(image, blob.LastPoint(),
                    new Point(
                        blob.LastPoint().X + blob.LastVector().X,
                        blob.LastPoint().Y + blob.LastVector().Y),
                    new MCvScalar(255, 0, 255), 5, Emgu.CV.CvEnum.LineType.EightConnected, 0, 0.5);
            }
        }

        public void Draw(Image<Gray, byte> image)
        {
            foreach (MyBlob blob in blobs)
            {
                CvInvoke.Circle(image, blob.LastPoint(), 1, new Emgu.CV.Structure.MCvScalar(255), -1);
                CvInvoke.Line(image, blob.LastPoint(),
                    new Point(
                        blob.LastPoint().X + blob.LastVector().X, 
                        blob.LastPoint().Y + blob.LastVector().Y), 
                    new MCvScalar(120, 120, 0), 1);
            }
        }

        public void Update()
        {
            List<MyBlob> blobsToSave = new List<MyBlob>();
            for (int i = 0; i < blobs.Count; i++)
            {
                if (blobs[i].found == true)
                {
                    blobsToSave.Add(blobs[i]);
                }
            }
            blobs = blobsToSave;
            foreach (MyBlob blob in blobs)
            {
                blob.found = false;
            }
        }

        MyBlob NearestBlob(Point point)
        {
            double min = 10000;
            MyBlob tmp = null;
            foreach(MyBlob blob in blobs)
            {
                double dstTmp = distance(blob.LastPoint(), point);
                if (dstTmp < min)
                {
                    min = dstTmp;
                    tmp = blob;
                }
            }
            return tmp;
        }

        public double distance(Point p1, Point p2)
        {
            return (Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
        }
    }
}
