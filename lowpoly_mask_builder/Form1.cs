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
        private Edge activeEdge = null;
        private bool isAddingTriangle = false;

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

            selectedVertex = null;
            activeEdge = null;  // 明示的にnullを設定
            isAddingTriangle = false;  // これも初期化
            activeEdge = null;
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

            // 新しい三角形の追加中に仮の三角形を描画
            if ( activeEdge != null)
            {
                if (isAddingTriangle)
                {
                    DrawTemporaryTriangle(g);
                }
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

            // 三角形の各辺について、activeEdgeと一致するかどうかを個別に判定
            bool isEdge1Active = activeEdge != null &&
                ((triangle.V1 == activeEdge.VertexIndex1 && triangle.V2 == activeEdge.VertexIndex2) ||
                 (triangle.V1 == activeEdge.VertexIndex2 && triangle.V2 == activeEdge.VertexIndex1));

            bool isEdge2Active = activeEdge != null &&
                ((triangle.V2 == activeEdge.VertexIndex1 && triangle.V3 == activeEdge.VertexIndex2) ||
                 (triangle.V2 == activeEdge.VertexIndex2 && triangle.V3 == activeEdge.VertexIndex1));

            bool isEdge3Active = activeEdge != null &&
                ((triangle.V3 == activeEdge.VertexIndex1 && triangle.V1 == activeEdge.VertexIndex2) ||
                 (triangle.V3 == activeEdge.VertexIndex2 && triangle.V1 == activeEdge.VertexIndex1));

            // 各辺を個別に描画し、アクティブな辺のみ太線で描画
            DrawEdge(g, p1, p2, isEdge1Active);
            DrawEdge(g, p2, p3, isEdge2Active);
            DrawEdge(g, p3, p1, isEdge3Active);
        }
        private void DrawEdge(Graphics g, Point start, Point end, bool isActive)
        {
            float penWidth = isActive ? 3.0f : 1.5f;
            Pen pen = new Pen(Color.Black, penWidth);
            g.DrawLine(pen, start, end);
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

        private void pictureBoxRight_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Vertex nearestVertex = FindNearestVertex(e.Location, 10);
                if (nearestVertex != null)
                {
                    // 頂点を選択した場合
                    selectedVertex = nearestVertex;
                    isDragging = true;
                    isAddingTriangle = false;
                    vScrollBarZ.Value = vScrollBarZ.Maximum - selectedVertex.Z;
                }
                else if (activeEdge != null)
                {
                    // アクティブな辺を選択した場合、新しい三角形の追加を開始
                    isAddingTriangle = true;
                }
                pictureBoxRight.Invalidate();
            }
        }

        private void pictureBoxRight_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && selectedVertex != null)
            {
                // 頂点の移動ドラッグ処理
                int newX = e.X / GRID_SIZE * GRID_SIZE;
                int newY = e.Y / GRID_SIZE * GRID_SIZE;

                newX = Math.Max(0, Math.Min(200, newX));
                newY = Math.Max(0, Math.Min(300, newY));

                selectedVertex.X = newX;
                selectedVertex.Y = newY;
                pictureBoxRight.Invalidate();
            }
            else if (!isAddingTriangle)  // 新しい三角形の追加ドラッグ中でない場合のみ
            {
                // 辺のアクティブ判定を行う
                Edge nearestEdge = FindNearestEdgeMiddle(e.Location, EDGE_ACTIVE_DISTANCE);
                if (nearestEdge != activeEdge)
                {
                    activeEdge = nearestEdge;
                }
            }
            // isAddingTriangleがtrueの場合は、辺のアクティブ判定を行わない
            pictureBoxRight.Invalidate();
        }

        private void pictureBoxRight_MouseUp(object sender, MouseEventArgs e)
        {
            if (isAddingTriangle && activeEdge != null)
            {
                AddTriangleFromActiveEdge(e.Location);
            }

            activeEdge = null;
            isAddingTriangle = false;
            isDragging = false;
            pictureBoxRight.Invalidate();
        }

        private Edge FindNearestEdgeMiddle(Point mousePoint, int maxDistance)
        {
            Edge closestEdge = null;
            double minDistance = double.MaxValue;

            foreach (var triangle in triangles)
            {
                var edges = new[] {
                    new Edge(triangle.V1, triangle.V2),
                    new Edge(triangle.V2, triangle.V3),
                    new Edge(triangle.V3, triangle.V1)
                };

                foreach (var edge in edges)
                {
                    if (IsMouseNearEdgeMiddle(edge, mousePoint, maxDistance))
                    {
                        double distance = CalculateDistanceToEdge(edge, mousePoint);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestEdge = edge;
                        }
                    }
                }
            }

            return closestEdge;
        }

        private bool IsMouseNearEdgeMiddle(Edge edge, Point mousePoint, int maxDistance)
        {
            Point p1 = WorldToScreen(vertices[edge.VertexIndex1]);
            Point p2 = WorldToScreen(vertices[edge.VertexIndex2]);

            // 辺の中央点を計算
            int midX = (p1.X + p2.X) / 2;
            int midY = (p1.Y + p2.Y) / 2;

            // マウスカーソルと辺の中央点との距離を計算
            int dx = mousePoint.X - midX;
            int dy = mousePoint.Y - midY;
            double distanceToMiddle = Math.Sqrt(dx * dx + dy * dy);

            // 辺の中央点から指定された距離（maxDistance）以内にあるかチェック
            return distanceToMiddle <= maxDistance;
        }

        private double CalculateDistanceToEdge(Edge edge, Point point)
        {
            Point p1 = WorldToScreen(vertices[edge.VertexIndex1]);
            Point p2 = WorldToScreen(vertices[edge.VertexIndex2]);

            int dx = p2.X - p1.X;
            int dy = p2.Y - p1.Y;
            double lengthSquared = dx * dx + dy * dy;

            if (lengthSquared == 0) return Distance(p1, point);

            double t = Math.Max(0, Math.Min(1, ((point.X - p1.X) * dx + (point.Y - p1.Y) * dy) / lengthSquared));
            Point projection = new Point(p1.X + (int)(t * dx), p1.Y + (int)(t * dy));

            return Distance(projection, point);
        }

        private double Distance(Point a, Point b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private void AddTriangleFromActiveEdge(Point mouseLocation)
        {
            int newX = mouseLocation.X / GRID_SIZE * GRID_SIZE;
            int newY = mouseLocation.Y / GRID_SIZE * GRID_SIZE;

            newX = Math.Max(0, Math.Min(200, newX));
            newY = Math.Max(0, Math.Min(300, newY));

            // 同じ座標の既存の頂点を探す
            Vertex existingVertex = vertices.FirstOrDefault(v => v.X == newX && v.Y == newY);

            if (existingVertex != null)
            {
                // 既存の頂点を使用する
                int existingVertexIndex = vertices.IndexOf(existingVertex);
                triangles.Add(new Triangle(activeEdge.VertexIndex1, activeEdge.VertexIndex2, existingVertexIndex));
            }
            else
            {
                // 新しい頂点を追加
                int newVertexIndex = vertices.Count;
                vertices.Add(new Vertex(newX, newY));
                triangles.Add(new Triangle(activeEdge.VertexIndex1, activeEdge.VertexIndex2, newVertexIndex));
            }

            isAddingTriangle = false;
        }

        private void DrawTemporaryTriangle(Graphics g)
        {
            if (activeEdge == null) return;

            Point p1 = WorldToScreen(vertices[activeEdge.VertexIndex1]);
            Point p2 = WorldToScreen(vertices[activeEdge.VertexIndex2]);

            // マウス位置を取得
            Point mousePos = pictureBoxRight.PointToClient(Control.MousePosition);

            // AddTriangleFromActiveEdgeと同じスナップとクロッピングを適用
            int snappedX = mousePos.X / GRID_SIZE * GRID_SIZE;
            int snappedY = mousePos.Y / GRID_SIZE * GRID_SIZE;

            // 最大エリアの制限を適用
            snappedX = Math.Max(0, Math.Min(200, snappedX));
            snappedY = Math.Max(0, Math.Min(300, snappedY));

            Point snappedMousePos = new Point(snappedX, snappedY);

            Pen tempPen = new Pen(Color.Green, 2.0f);
            g.DrawLine(tempPen, p1, p2);
            g.DrawLine(tempPen, p2, snappedMousePos);
            g.DrawLine(tempPen, snappedMousePos, p1);
        }

        private Triangle GetTriangleContainingEdge(Edge edge)
        {
            return triangles.FirstOrDefault(t =>
                (t.V1 == edge.VertexIndex1 && t.V2 == edge.VertexIndex2) ||
                (t.V2 == edge.VertexIndex1 && t.V1 == edge.VertexIndex2) ||
                (t.V2 == edge.VertexIndex2 && t.V3 == edge.VertexIndex1) ||
                (t.V3 == edge.VertexIndex2 && t.V2 == edge.VertexIndex1) ||
                (t.V3 == edge.VertexIndex1 && t.V1 == edge.VertexIndex2) ||
                (t.V1 == edge.VertexIndex2 && t.V3 == edge.VertexIndex1));
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
                selectedVertex.Z = vScrollBarZ.Maximum - vScrollBarZ.Value;
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InitializeModel();
        }

        private class Edge
        {
            public int VertexIndex1 { get; set; }
            public int VertexIndex2 { get; set; }

            public Edge(int v1, int v2)
            {
                VertexIndex1 = v1;
                VertexIndex2 = v2;
            }
        }
    }

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

    public class Triangle
    {
        public int V1 { get; set; }
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