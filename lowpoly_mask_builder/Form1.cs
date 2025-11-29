using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace lowpoly_mask_builder
{
    

    public partial class Form1 : Form
    {
        private List<Vertex> vertices = new List<Vertex>();
        private List<Triangle> triangles = new List<Triangle>();
        private Vertex selectedVertex = null;
        private bool isDragging = false;
        private Point dragStartPos;

        // 定数
        private const int GRID_SIZE = 1;
        private const int POINT_RADIUS = 4;
        private const int EDGE_ACTIVE_DISTANCE = 4;

        public Form1()
        {
            InitializeComponent();
            InitializeModel();
        }

        private void InitializeModel()
        {
            // 初期状態：(0,0), (100,0), (0,100)の三角形を作成
            vertices.Clear();
            triangles.Clear();

            vertices.Add(new Vertex(0, 0));   // 頂点0
            vertices.Add(new Vertex(100, 0)); // 頂点1
            vertices.Add(new Vertex(0, 100)); // 頂点2

            triangles.Add(new Triangle(0, 1, 2));

            pictureBoxRight.Invalidate();
        }

        private void pictureBoxRight_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.LightCyan);

            // グリッドを描画
            DrawGrid(g);

            // 三角形を描画
            foreach (var triangle in triangles)
            {
                DrawTriangle(g, triangle);
            }

            // 頂点を描画
            foreach (var vertex in vertices)
            {
                DrawVertex(g, vertex, vertex == selectedVertex);
            }
        }

        private void DrawGrid(Graphics g)
        {
            Pen gridPen = new Pen(Color.FromArgb(200, 200, 200));
            int width = pictureBoxRight.Width;
            int height = pictureBoxRight.Height;

            for (int x = 0; x < width; x += 10)
                g.DrawLine(gridPen, x, 0, x, height);
            for (int y = 0; y < height; y += 10)
                g.DrawLine(gridPen, 0, y, width, y);
        }

        private void DrawTriangle(Graphics g, Triangle triangle)
        {
            Point p1 = WorldToScreen(vertices[triangle.V1]);
            Point p2 = WorldToScreen(vertices[triangle.V2]);
            Point p3 = WorldToScreen(vertices[triangle.V3]);

            Pen pen = new Pen(Color.Black, 1.5f);
            g.DrawLine(pen, p1, p2);
            g.DrawLine(pen, p2, p3);
            g.DrawLine(pen, p3, p1);
        }

        private void DrawVertex(Graphics g, Vertex vertex, bool isSelected)
        {
            Point screenPos = WorldToScreen(vertex);
            int radius = isSelected ? POINT_RADIUS + 2 : POINT_RADIUS;
            Brush brush = isSelected ? Brushes.Red : Brushes.Blue;

            g.FillEllipse(brush, screenPos.X - radius, screenPos.Y - radius, radius * 2, radius * 2);
            g.DrawEllipse(Pens.Black, screenPos.X - radius, screenPos.Y - radius, radius * 2, radius * 2);
        }

        private Point WorldToScreen(Vertex vertex)
        {
            return new Point(vertex.X, vertex.Y);
        }

        private Vertex ScreenToWorld(Point screenPoint)
        {
            // 画面座標をワールド座標に変換（現在は1:1）
            return new Vertex(screenPoint.X, screenPoint.Y);
        }

        private void pictureBoxRight_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // 頂点の選択またはドラッグ開始
                Vertex nearestVertex = FindNearestVertex(e.Location, 10);
                if (nearestVertex != null)
                {
                    selectedVertex = nearestVertex;
                    isDragging = true;
                    dragStartPos = e.Location;
                    vScrollBarZ.Value = selectedVertex.Z;
                }
                pictureBoxRight.Invalidate();
            }
        }

        private void pictureBoxRight_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && selectedVertex != null)
            {
                // グリッドにスナップして頂点を移動
                int newX = e.X / GRID_SIZE * GRID_SIZE;
                int newY = e.Y / GRID_SIZE * GRID_SIZE;

                // 範囲制限：X=0-200, Y=0-300
                newX = Math.Max(0, Math.Min(200, newX));
                newY = Math.Max(0, Math.Min(300, newY));

                selectedVertex.X = newX;
                selectedVertex.Y = newY;

                pictureBoxRight.Invalidate();
            }
        }

        private void pictureBoxRight_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private Vertex FindNearestVertex(Point screenPoint, int maxDistance)
        {
            Vertex nearest = null;
            int minDistanceSquared = maxDistance * maxDistance;

            foreach (var vertex in vertices)
            {
                Point screenPos = WorldToScreen(vertex);
                int dx = screenPoint.X - screenPos.X;
                int dy = screenPoint.Y - screenPos.Y;
                int distanceSquared = dx * dx + dy * dy;

                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;
                    nearest = vertex;
                }
            }

            return nearest;
        }

        private void vScrollBarZ_Scroll(object sender, ScrollEventArgs e)
        {
            if (selectedVertex != null)
            {
                selectedVertex.Z = vScrollBarZ.Value;
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InitializeModel();
            selectedVertex = null;
            vScrollBarZ.Value = 0;
        }
    }
    // 頂点クラス
    public class Vertex
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public Vertex(int x, int y, int z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    // 三角形クラス
    public class Triangle
    {
        public int V1 { get; set; } // 頂点インデックス
        public int V2 { get; set; }
        public int V3 { get; set; }

        public Triangle(int v1, int v2, int v3)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
        }
    }
}