//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.Windows.Media.Media3D;
using System.Windows.Threading;
using System.IO;
using System.Drawing;


namespace BrainSimulator
{
    /// <summary>
    /// Interaction logic for ModuleSimViewDlg.xaml
    /// </summary>
    public partial class ModuleSimViewDlg : Window
    {
        struct physObject
        {
            public Point3D thePos;
            public System.Windows.Media.Color theColor;
            public float scaleX;
            public float scaleY;
        }
        List<physObject> objects = new List<physObject>();

        public ModuleSimViewDlg()
        {
            InitializeComponent();
            MyCanvas();
            Random rand = new Random();
            for (int i = 0; i < 20; i++)
            {
                System.Windows.Media.Color tempColor = Colors.Red;
                double x = rand.NextDouble();
                if (x > .33) tempColor = Colors.Blue;
                if (x > .67) tempColor = Colors.Green;
                physObject newObject = new physObject
                {
                    thePos = new Point3D(rand.NextDouble() * 10 - 5, 0, rand.NextDouble() * 10 - 5),
                    theColor = tempColor,
                    scaleX = .5f,
                    scaleY = .75f
                };
                objects.Add(newObject);
            }
            dt.Interval= new TimeSpan(0,0,10);
            dt.Tick += Dt_Tick1;
            dt.Start();
        }

        private void Dt_Tick1(object sender, EventArgs e)
        {
            MyCanvas();
        }

        bool viewChanged = true;
        Point3D cameraPosition = new Point3D(0, 0, 0);
        Vector3D cameraDirection = new Vector3D(0, 0, -1);
        DispatcherTimer dt = new DispatcherTimer();
        double theta = 0;

        public void Move(int x) //you can move forward/back in the direciton you are headed
        {
            cameraPosition.Z += -x / 1000f * Math.Cos(theta);
            cameraPosition.X += x / 1000f * Math.Sin(theta);
            viewChanged = true;
        }
        public void Turn(int x)
        {
            theta -= x / 1000f;
            cameraDirection.X = Math.Sin(theta);
            cameraDirection.Z = -Math.Cos(theta);
            viewChanged = true;
        }

        public void MyCanvas()
        {
            DrawObject(cameraPosition, cameraDirection, objects);
        }

        private void DrawObject(Point3D thePosition, Vector3D theDirection, List<physObject> theObjects)
        {
            // Declare scene objects.
            Viewport3D myViewport3D = new Viewport3D();
            Model3DGroup myModel3DGroup = new Model3DGroup();
            ModelVisual3D myModelVisual3D = new ModelVisual3D();

            // Defines the camera used to view the 3D object. In order to view the 3D object,
            // the camera must be positioned and pointed such that the object is within view 
            // of the camera.
            PerspectiveCamera myPCamera = new PerspectiveCamera();

            // Specify where in the 3D scene the camera is.
            myPCamera.Position = thePosition;// new Point3D(0, 0, 5);

            // Specify the direction that the camera is pointing.
            myPCamera.LookDirection = theDirection;// new Vector3D(0, 0, -1);

            // Define camera's horizontal field of view in degrees.
            myPCamera.FieldOfView = 60;

            // Asign the camera to the viewport
            myViewport3D.Camera = myPCamera;

            // Define the lights cast in the scene. Without light, the 3D object cannot 
            // be seen. Note: to illuminate an object from additional directions, create 
            // additional lights.
            DirectionalLight myDirectionalLight = new DirectionalLight();
            myDirectionalLight.Color = Colors.White;
            myDirectionalLight.Direction = new Vector3D(-0.61, -0.5, -0.61);

            myModel3DGroup.Children.Add(myDirectionalLight);

            AmbientLight ambient = new AmbientLight(Colors.White);
            myModel3DGroup.Children.Add(ambient);

            // Add the geometry model to the model group.
            for (int i = 0; i < theObjects.Count; i++)
            {
                myModel3DGroup.Children.Add(CreateSquare(theObjects[i].thePos, theObjects[i].theColor, theObjects[i].scaleX, theObjects[i].scaleY));
            }
            //myModel3DGroup.Children.Add(CreateSquare(new Point3D(0, 0, -5), Colors.Red, 0.2, .4));
            //myModel3DGroup.Children.Add(CreateSquare(new Point3D(3, 0.5, -7), Colors.Orange));
            //myModel3DGroup.Children.Add(CreateSquare(new Point3D(-2, -0.5, -5), Colors.Gray));
            //myModel3DGroup.Children.Add(CreateSquare(new Point3D(-4, -0.5, -2), Colors.LightGray));
            //myModel3DGroup.Children.Add(CreateSquare(new Point3D(-6, 0.5, -2), Colors.DimGray));
            //myModel3DGroup.Children.Add(CreateSquare(new Point3D(5, 0, 0), Colors.Lavender, .1, .4));
            //myModel3DGroup.Children.Add(CreateSquare(new Point3D(5, 0, 1), Colors.LawnGreen, .2, .3));
            //myModel3DGroup.Children.Add(CreateSquare(new Point3D(5, 0, 2), Colors.LemonChiffon, .3, .2));
            //myModel3DGroup.Children.Add(CreateSquare(new Point3D(5, 0, 3), Colors.LightBlue, .4, .1));
            //myModel3DGroup.Children.Add(CreateSquare(new Point3D(5, 0, 4), Colors.LightCoral));
            //myModel3DGroup.Children.Add(CreateSquare(new Point3D(5, 0, 5), Colors.Green));
            //myModel3DGroup.Children.Add(CreateSquare(new Point3D(0, 0, 5), Colors.Pink));
            //myModel3DGroup.Children.Add(CreateSquare(new Point3D(-5, 0, 5), Colors.DarkBlue));


            //// Apply multiple transformations to the object. In this sample, a rotation and scale 
            //// transform is applied.

            //// Create and apply a transformation that rotates the object.
            //RotateTransform3D myRotateTransform3D = new RotateTransform3D();
            //AxisAngleRotation3D myAxisAngleRotation3d = new AxisAngleRotation3D();
            //myAxisAngleRotation3d.Axis = new Vector3D(0, 3, 0);
            //myAxisAngleRotation3d.Angle = 0; //rotate the rectangle within the space
            //myRotateTransform3D.Rotation = myAxisAngleRotation3d;

            //// Add the rotation transform to a Transform3DGroup
            //Transform3DGroup myTransform3DGroup = new Transform3DGroup();
            //myTransform3DGroup.Children.Add(myRotateTransform3D);

            //// Create and apply a scale transformation that stretches the object along the local x-axis  
            //// by 200 percent and shrinks it along the local y-axis by 50 percent.
            //ScaleTransform3D myScaleTransform3D = new ScaleTransform3D();
            //myScaleTransform3D.ScaleX = 1;
            //myScaleTransform3D.ScaleY = 1;
            //myScaleTransform3D.ScaleZ = 1;

            //// Add the scale transform to the Transform3DGroup.
            //myTransform3DGroup.Children.Add(myScaleTransform3D);

            //Vector3D offset = new Vector3D(0, 0, 0);
            //TranslateTransform3D myTranslateTransform3D = new TranslateTransform3D(offset);
            //myTransform3DGroup.Children.Add(myTranslateTransform3D);

            // Add the group of models to the ModelVisual3d.
            myModelVisual3D.Content = myModel3DGroup;

            myViewport3D.Children.Add(myModelVisual3D);

            // add the viewport to the grid so it will be rendered.
            theGrid.Children.Clear();
            theGrid.Children.Add(myViewport3D);

        }

        private GeometryModel3D CreateSquare(Point3D offset, System.Windows.Media.Color theColor,
            double sizeX = 0.5, double sizeY = 0.5)
        {
            GeometryModel3D myGeometryModel = new GeometryModel3D();
            // The geometry specifes the shape of the 3D plane. In this sample, a flat sheet is created.
            MeshGeometry3D myMeshGeometry3D = new MeshGeometry3D();

            float zVal = 1;
            // Create a collection of normal vectors for the MeshGeometry3D.
            Vector3DCollection myNormalCollection = new Vector3DCollection();
            myNormalCollection.Add(new Vector3D(0, 0, zVal));
            myNormalCollection.Add(new Vector3D(0, 0, zVal));
            myNormalCollection.Add(new Vector3D(0, 0, zVal));
            myNormalCollection.Add(new Vector3D(0, 0, zVal));
            myNormalCollection.Add(new Vector3D(0, 0, zVal));
            myNormalCollection.Add(new Vector3D(0, 0, zVal));
            myMeshGeometry3D.Normals = myNormalCollection;

            // Create a collection of vertex positions for the MeshGeometry3D. 
            Point3DCollection myPositionCollection = new Point3DCollection();
            myPositionCollection.Add(new Point3D(-sizeX + offset.X, -sizeY + offset.Y, offset.Z));
            myPositionCollection.Add(new Point3D(sizeX + offset.X, -sizeY + offset.Y, offset.Z));
            myPositionCollection.Add(new Point3D(sizeX + offset.X, sizeY + offset.Y, offset.Z));
            myPositionCollection.Add(new Point3D(sizeX + offset.X, sizeY + offset.Y, offset.Z));
            myPositionCollection.Add(new Point3D(-sizeX + offset.X, sizeY + offset.Y, offset.Z));
            myPositionCollection.Add(new Point3D(-sizeX + offset.X, -sizeY + offset.Y, offset.Z));
            myMeshGeometry3D.Positions = myPositionCollection;

            // Create a collection of texture coordinates for the MeshGeometry3D.
            PointCollection myTextureCoordinatesCollection = new PointCollection();
            myTextureCoordinatesCollection.Add(new System.Windows.Point(0, 0));
            myTextureCoordinatesCollection.Add(new System.Windows.Point(1, 0));
            myTextureCoordinatesCollection.Add(new System.Windows.Point(1, 1));
            myTextureCoordinatesCollection.Add(new System.Windows.Point(1, 1));
            myTextureCoordinatesCollection.Add(new System.Windows.Point(0, 1));
            myTextureCoordinatesCollection.Add(new System.Windows.Point(0, 0));
            myMeshGeometry3D.TextureCoordinates = myTextureCoordinatesCollection;

            // Create a collection of triangle indices for the MeshGeometry3D.
            Int32Collection myTriangleIndicesCollection = new Int32Collection();
            myTriangleIndicesCollection.Add(0);
            myTriangleIndicesCollection.Add(1);
            myTriangleIndicesCollection.Add(2);
            myTriangleIndicesCollection.Add(3);
            myTriangleIndicesCollection.Add(4);
            myTriangleIndicesCollection.Add(5);
            myMeshGeometry3D.TriangleIndices = myTriangleIndicesCollection;

            // Apply the mesh to the geometry model.
            myGeometryModel.Geometry = myMeshGeometry3D;

            SolidColorBrush theBrush = new SolidColorBrush(theColor);
            // Define material and apply to the mesh geometries.
            //DiffuseMaterial myMaterial = new DiffuseMaterial(myHorizontalGradient);
            DiffuseMaterial myMaterial = new DiffuseMaterial(theBrush);
            myGeometryModel.Material = myMaterial;
            myGeometryModel.BackMaterial = myMaterial;
            return myGeometryModel;
        }
        public Bitmap theBitMap1 = null;
        public Bitmap theBitMap2 = null;
        public void GetBitMap()
        {
            if (!viewChanged) return;
            System.Windows.Size size = new System.Windows.Size(ActualWidth, ActualHeight);
            Measure(size);
            Arrange(new Rect(size));
            // Create a render bitmap and push the surface to it
            RenderTargetBitmap renderBitmap =
              new RenderTargetBitmap(
                (int)size.Width,
                (int)size.Height,
                96d,
                96d,
                PixelFormats.Default);
            renderBitmap.Render(this);

            //System.Drawing.Rectangle bounds = new System.Drawing.Rectangle(0, 0, (int)size.Width, (int)size.Height);// this.Bounds;
            //using ( theBitMap = new Bitmap(bounds.Width, bounds.Height))
            //{
            //    using (Graphics g = Graphics.FromImage(theBitMap))
            //    {
            //        g.CopyFromScreen(new System.Drawing.Point(bounds.Left, bounds.Top), System.Drawing.Point.Empty, bounds.Size);
            //    }
            //    //bitmap.Save("C://test.jpg", ImageFormat.Jpeg);
            //}

            //test by saving the bitmap to a file
            //using (FileStream outStream = new FileStream("E:\\Charlie\\Documents\\Brainsim\\test.bmp", FileMode.Create))
            //{
            //    // Use png encoder for our data
            //    PngBitmapEncoder encoder1 = new PngBitmapEncoder();
            //    // push the rendered bitmap to it
            //    encoder1.Frames.Add(BitmapFrame.Create(renderBitmap));
            //    // save the data to the stream
            //    encoder1.Save(outStream);
            //}

            //Convert the RenderBitmap to a real bitmap
            MemoryStream stream = new MemoryStream();
            BitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            encoder.Save(stream);

            if (theBitMap1 == null)
                theBitMap1 = new Bitmap(stream);
            else if (theBitMap2 == null)
                theBitMap2 = new Bitmap(stream);
            MyCanvas();
            viewChanged = false;
        }
    }
}
