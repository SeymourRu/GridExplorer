//------------------------------------------------------------------
// (c) Copywrite Jianzhong Zhang
// This code is under The Code Project Open License
// Please read the attached license document before using this class
//------------------------------------------------------------------

// window class for testing 3d chart using WPF
// version 0.1

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System;
using System.Collections;

namespace GridExplorer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // transform class object for rotate the 3d model
        public GridExplorer.TransformMatrix m_transformMatrix = new GridExplorer.TransformMatrix();

        // ***************************** 3d chart ***************************
        private GridExplorer.Chart3D m_3dChart;       // data for 3d chart
        public int m_nChartModelIndex = -1;         // model index in the Viewport3d
        public int m_nNumElemts_x = 5;     // total number of dots on x
        public int m_nNumElemts_y = 5;     // total number of dots on y
        public int m_nNumElemts_z = 5;     // total number of dots on z
        // ***************************** selection rect ***************************
        ViewportRect m_selectRect = new ViewportRect();
        public int m_nRectModelIndex = -1;

        Random rnd = new Random(228);
        private int m_nscale_factor = 10; 

        public MainWindow()
        {
            InitializeComponent();

            // selection rect
            m_selectRect.SetRect(new Point(-0.5, -0.5), new Point(-0.5, -0.5));
            GridExplorer.Model3D model3d = new GridExplorer.Model3D();
            ArrayList meshs = m_selectRect.GetMeshes();
            m_nRectModelIndex = model3d.UpdateModel(meshs, null, m_nRectModelIndex, this.mainViewport);

            // display the 3d chart data no.
            dataNo_x.Text = String.Format("{0:d}", m_nNumElemts_x);
            dataNo_y.Text = String.Format("{0:d}", m_nNumElemts_y);
            dataNo_z.Text = String.Format("{0:d}", m_nNumElemts_z);
            basis_x.Text = "1";
            basis_y.Text = "1";
            basis_z.Text = "1";

            // display surface chart
            BuildGrid();
            TransformChart();
        }

        // function for set a scatter plot, every dot is just a simple pyramid.
        public void BuildGrid(SHAPE shape = SHAPE.CYLINDER)
        {
            int nElem_x = Int32.Parse(dataNo_x.Text);
            int nElem_y = Int32.Parse(dataNo_y.Text);
            int nElem_z = Int32.Parse(dataNo_z.Text);

            if ((nElem_x <= 0) || (nElem_y <= 0) || (nElem_z <= 0))
            {
                MessageBox.Show("Add at least 1 point to each axis");
                return;
            }
            if ((nElem_x > 10000) || (nElem_y > 10000) || (nElem_z > 10000))
            {
                MessageBox.Show("Too many elements");
                return;
            }

            long bas_x = Int64.Parse(basis_x.Text);
            long bas_y = Int64.Parse(basis_y.Text);
            long bas_z = Int64.Parse(basis_z.Text);

            // 1. set the scatter plot size
            m_3dChart = new ScatterChart3D();
            m_3dChart.SetDataNo(nElem_x * nElem_y * nElem_z);

            // 2. set the properties of each dot
            Random randomObject = new Random();
            int nDataRange = 200;
            for (int i = 0; i < nElem_x; i++)
            {
                for (int j = 0; j < nElem_y; j++)
                {
                    for (int k = 0; k < nElem_z; k++)
                    {
                        ScatterPlotItem plotItem = new ScatterPlotItem();

                        plotItem.w = 2;
                        plotItem.h = 2;

                        plotItem.x = (float)i * bas_x * m_nscale_factor;
                        plotItem.y = (float)j * bas_y * m_nscale_factor;
                        plotItem.z = (float)k * bas_z * m_nscale_factor;

                        plotItem.shape = (int)shape;

                        Byte nR = (Byte)randomObject.Next(256);
                        Byte nG = (Byte)randomObject.Next(256);
                        Byte nB = (Byte)randomObject.Next(256);

                        plotItem.color = Color.FromRgb(nR, nG, nB);

                        // convert [,,] -> []
                        //x + WIDTH * (y + DEPTH * z)
                        ((ScatterChart3D)m_3dChart).SetVertex(i + nElem_x * (j + nElem_y * k), plotItem);
                    }
                }
            }

            // 3. set axes
            m_3dChart.GetDataRange();
            m_3dChart.SetAxes();

            // 4. Get Mesh3D array from scatter plot
            ArrayList meshs = ((ScatterChart3D)m_3dChart).GetMeshes();

            // 5. display vertex no and triangle no.
            UpdateModelSizeInfo(meshs);

            // 6. show 3D scatter plot in Viewport3d
            GridExplorer.Model3D model3d = new GridExplorer.Model3D();
            m_nChartModelIndex = model3d.UpdateModel(meshs, null, m_nChartModelIndex, this.mainViewport);

            // 7. set projection matrix
            float viewRange = (float)nDataRange;
            m_transformMatrix.CalculateProjectionMatrix(0, viewRange, 0, viewRange, 0, viewRange, 0.5);
            TransformChart();
        }


        public void OnViewportMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs args)
        {
            Point pt = args.GetPosition(mainViewport);
            if (args.ChangedButton == MouseButton.Left)         // rotate or drag 3d model
            {
                m_transformMatrix.OnLBtnDown(pt);
            }
            else if (args.ChangedButton == MouseButton.Right)   // select rect
            {
                m_selectRect.OnMouseDown(pt, mainViewport, m_nRectModelIndex);
            }
        }

        public void OnViewportMouseMove(object sender, System.Windows.Input.MouseEventArgs args)
        {
            Point pt = args.GetPosition(mainViewport);

            if (args.LeftButton == MouseButtonState.Pressed)                // rotate or drag 3d model
            {
                m_transformMatrix.OnMouseMove(pt, mainViewport);

                TransformChart();
            }
            else if (args.RightButton == MouseButtonState.Pressed)          // select rect
            {
                m_selectRect.OnMouseMove(pt, mainViewport, m_nRectModelIndex);
            }
            else
            {
                String s1;
                Point pt2 = m_transformMatrix.VertexToScreenPt(new Point3D(0.5, 0.5, 0.3), mainViewport);
                s1 = string.Format("Screen:({0:d},{1:d}), Predicated: ({2:d}, H:{3:d})", 
                    (int)pt.X, (int)pt.Y, (int)pt2.X, (int)pt2.Y);
                this.statusPane.Text = s1;
            }
        }

        public void OnViewportMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs args)
        {
            Point pt = args.GetPosition(mainViewport);
            if (args.ChangedButton == MouseButton.Left)
            {
                m_transformMatrix.OnLBtnUp();
            }
            else if (args.ChangedButton == MouseButton.Right)
            {
                if (m_nChartModelIndex == -1) return;
                // 1. get the mesh structure related to the selection rect
                MeshGeometry3D meshGeometry = Model3D.GetGeometry(mainViewport, m_nChartModelIndex);
                if (meshGeometry == null) return;
              
                // 2. set selection in 3d chart
                m_3dChart.Select(m_selectRect, m_transformMatrix, mainViewport);

                // 3. update selection display
                m_3dChart.HighlightSelection(meshGeometry, Color.FromRgb(200, 200, 200));
            }
        }

        // zoom in 3d display
        public void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs args)
        {
            m_transformMatrix.OnKeyDown(args);
            TransformChart();
        }

        private void scatterButton_Click(object sender, RoutedEventArgs e)
        {
            if ((bool)checkBoxShape.IsChecked)
            {
                var fig = (GridExplorer.SHAPE)rnd.Next(0, 4);
                BuildGrid(fig);
            }
            else
            {
                BuildGrid();
            }
        }

         private void UpdateModelSizeInfo(ArrayList meshs)
        {
            int nMeshNo = meshs.Count;
            int nChartVertNo = 0;
            int nChartTriangelNo = 0;
            for (int i = 0; i < nMeshNo; i++)
            {
                nChartVertNo += ((Mesh3D)meshs[i]).GetVertexNo();
                nChartTriangelNo += ((Mesh3D)meshs[i]).GetTriangleNo();
            }
            labelVertNo.Content = String.Format("Vertex No: {0:d}", nChartVertNo);
            labelTriNo.Content = String.Format("Triangle No: {0:d}", nChartTriangelNo);
        }

        // this function is used to rotate, drag and zoom the 3d chart
        private void TransformChart()
        {
            if (m_nChartModelIndex == -1) return;
            ModelVisual3D visual3d = (ModelVisual3D)(this.mainViewport.Children[m_nChartModelIndex]);
            if (visual3d.Content == null) return;
            Transform3DGroup group1 = visual3d.Content.Transform as Transform3DGroup;
            group1.Children.Clear();
            group1.Children.Add(new MatrixTransform3D(m_transformMatrix.m_totalMatrix));
        }
    }
}
