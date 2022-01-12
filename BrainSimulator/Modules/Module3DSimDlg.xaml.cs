//
// Copyright (c) Charles Simon. All rights reserved.  
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
//  

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace BrainSimulator.Modules
{
    public partial class Module3DSimDlg : ModuleBaseDlg
    {
        struct physObject
        {
            public Point3D thePos;
            public System.Windows.Media.Color theColor;
            public float scaleX;
            public float scaleY;
        }
        List<physObject> objects = new List<physObject>();


        public Module3DSimDlg()
        {
            InitializeComponent();
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
        }
        public override bool Draw(bool checkDrawTimer)
        {

            MyCanvas();
            return true;
        }

        private void TheCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Draw(true);
        }
        public void MyCanvas()
        {
            Module3DSim parent = (Module3DSim)base.ParentModule;

            DrawObject(parent.cameraPosition, parent.cameraDirection, objects);
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
            
            //ImageBrush theImageBrush = new ImageBrush (new System.Windows.Media.Imaging.BitmapImage(new Uri(@"C:\Users\c_sim\Pictures\Img_7837.jpg"))) ;
            // Define material and apply to the mesh geometries.
            DiffuseMaterial myMaterial = new DiffuseMaterial(theBrush);
            //SpecularMaterial myMaterial = new SpecularMaterial(theBrush,50);
            myGeometryModel.Material = myMaterial;
            myGeometryModel.BackMaterial = myMaterial;
            return myGeometryModel;
        }
    }
}
