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
        private const int WORLD_WIDTH = 200;
        private const int WORLD_HEIGHT = 300;
        private const int POINT_RADIUS = 4;
        private const int EDGE_ACTIVE_DISTANCE = 4;

        public Form1()
        {
            InitializeComponent();
            InitializeModel();
        }

        private void InitializeModel()
        {
            vertices.Clear();
            triangles.Clear();

            vertices.Add(new Vertex(0, 0));   // 頂点0
            vertices.Add(new Vertex(100, 0)); // 頂点1
            vertices.Add(new Vertex(0, 100)); // 頂点2

            triangles.Add(new Triangle(0, 1, 2));

            selectedVertex = null;
            activeEdge = null;
            isAddingTriangle = false;
            pictureBoxRight.Invalidate();
        }

        private void pictureBoxRight_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.LightCyan);

            DrawGrid(g);

            foreach (var triangle in triangles)
            {
                DrawTriangle(g, triangle);
            }

            if (activeEdge != null && isAddingTriangle)
            {
                DrawTemporaryTriangle(g);
            }

            foreach (var vertex in vertices)
            {
                DrawVertex(g, vertex, vertex == selectedVertex);
            }
        }

        private void DrawGrid(Graphics g)
        {
            Pen gridPen = new Pen(Color.FromArgb(200, 200, 200));
            float scaleX = (float)pictureBoxRight.Width / WORLD_WIDTH;
            float scaleY = (float)pictureBoxRight.Height / WORLD_HEIGHT;

            for (int x = 0; x <= WORLD_WIDTH; x += 2)
            {
                int screenX = (int)(x * scaleX);
                g.DrawLine(gridPen, screenX, 0, screenX, pictureBoxRight.Height);
            }
            for (int y = 0; y <= WORLD_HEIGHT; y += 2)
            {
                int screenY = (int)(y * scaleY);
                g.DrawLine(gridPen, 0, screenY, pictureBoxRight.Width, screenY);
            }
        }

        private void DrawTriangle(Graphics g, Triangle triangle)
        {
            Point p1 = WorldToScreen(vertices[triangle.V1]);
            Point p2 = WorldToScreen(vertices[triangle.V2]);
            Point p3 = WorldToScreen(vertices[triangle.V3]);

            bool isEdge1Active = activeEdge != null &&
                ((triangle.V1 == activeEdge.VertexIndex1 && triangle.V2 == activeEdge.VertexIndex2) ||
                 (triangle.V1 == activeEdge.VertexIndex2 && triangle.V2 == activeEdge.VertexIndex1));

            bool isEdge2Active = activeEdge != null &&
                ((triangle.V2 == activeEdge.VertexIndex1 && triangle.V3 == activeEdge.VertexIndex2) ||
                 (triangle.V2 == activeEdge.VertexIndex2 && triangle.V3 == activeEdge.VertexIndex1));

            bool isEdge3Active = activeEdge != null &&
                ((triangle.V3 == activeEdge.VertexIndex1 && triangle.V1 == activeEdge.VertexIndex2) ||
                 (triangle.V3 == activeEdge.VertexIndex2 && triangle.V1 == activeEdge.VertexIndex1));

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
            float scaleX = (float)pictureBoxRight.Width / WORLD_WIDTH;
            float scaleY = (float)pictureBoxRight.Height / WORLD_HEIGHT;
            int screenX = (int)(vertex.X * scaleX);
            int screenY = (int)(vertex.Y * scaleY);
            return new Point(screenX, screenY);
        }

        private Vertex ScreenToWorld(Point screenPoint)
        {
            float scaleX = (float)pictureBoxRight.Width / WORLD_WIDTH;
            float scaleY = (float)pictureBoxRight.Height / WORLD_HEIGHT;
            int worldX = (int)(screenPoint.X / scaleX);
            int worldY = (int)(screenPoint.Y / scaleY);
            return new Vertex(worldX, worldY);
        }

        private void pictureBoxRight_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Vertex nearestVertex = FindNearestVertex(e.Location, 10);
                if (nearestVertex != null)
                {
                    selectedVertex = nearestVertex;
                    isDragging = true;
                    isAddingTriangle = false;
                    vScrollBarZ.Value = vScrollBarZ.Maximum - selectedVertex.Z;
                }
                else if (activeEdge != null)
                {
                    isAddingTriangle = true;
                }
                pictureBoxRight.Invalidate();
            }
        }

        private void pictureBoxRight_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging && selectedVertex != null)
            {
                Vertex worldPos = ScreenToWorld(e.Location);
                int newX = worldPos.X / GRID_SIZE * GRID_SIZE;
                int newY = worldPos.Y / GRID_SIZE * GRID_SIZE;

                newX = Math.Max(0, Math.Min(WORLD_WIDTH, newX));
                newY = Math.Max(0, Math.Min(WORLD_HEIGHT, newY));

                selectedVertex.X = newX;
                selectedVertex.Y = newY;
                pictureBoxRight.Invalidate();
            }
            else if (!isAddingTriangle)
            {
                Edge nearestEdge = FindNearestEdgeMiddle(e.Location, EDGE_ACTIVE_DISTANCE  * Math.Max(pictureBoxRight.Size.Width / WORLD_WIDTH, pictureBoxRight.Size.Height / WORLD_HEIGHT));
                if (nearestEdge != activeEdge)
                {
                    activeEdge = nearestEdge;
                }
            }
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

        private void AddTriangleFromActiveEdge(Point mouseLocation)
        {
            // activeEdgeが所属している三角形をすべて取得
            var trianglesContainingEdge = triangles.Where(t =>
                ((t.V1 == activeEdge.VertexIndex1 && t.V2 == activeEdge.VertexIndex2) ||
                 (t.V1 == activeEdge.VertexIndex2 && t.V2 == activeEdge.VertexIndex1) ||
                 (t.V2 == activeEdge.VertexIndex1 && t.V3 == activeEdge.VertexIndex2) ||
                 (t.V2 == activeEdge.VertexIndex2 && t.V3 == activeEdge.VertexIndex1) ||
                 (t.V3 == activeEdge.VertexIndex1 && t.V1 == activeEdge.VertexIndex2) ||
                 (t.V3 == activeEdge.VertexIndex2 && t.V1 == activeEdge.VertexIndex1))
            ).ToList();

            if (trianglesContainingEdge.Count > 1)
            {
                // activeEdgeが複数の三角形に所属している場合、中点を追加して分割
                SplitTrianglesByAddingMidpoint(trianglesContainingEdge);
            }
            else
            {
                // 通常の処理：新しい頂点を追加して三角形を作成
                Vertex worldPos = ScreenToWorld(mouseLocation);
                int newX = worldPos.X / GRID_SIZE * GRID_SIZE;
                int newY = worldPos.Y / GRID_SIZE * GRID_SIZE;

                newX = Math.Max(0, Math.Min(WORLD_WIDTH, newX));
                newY = Math.Max(0, Math.Min(WORLD_HEIGHT, newY));

                Vertex existingVertex = vertices.FirstOrDefault(v => v.X == newX && v.Y == newY);

                if (existingVertex != null)
                {
                    int existingVertexIndex = vertices.IndexOf(existingVertex);
                    triangles.Add(new Triangle(activeEdge.VertexIndex1, activeEdge.VertexIndex2, existingVertexIndex));
                }
                else
                {
                    int newVertexIndex = vertices.Count;
                    vertices.Add(new Vertex(newX, newY));
                    triangles.Add(new Triangle(activeEdge.VertexIndex1, activeEdge.VertexIndex2, newVertexIndex));
                }
            }
        }

        private void SplitTrianglesByAddingMidpoint(List<Triangle> trianglesContainingEdge)
        {
            int v1 = activeEdge.VertexIndex1;
            int v2 = activeEdge.VertexIndex2;

            // activeEdgeの中点に新しい頂点を追加
            int midX = (vertices[v1].X + vertices[v2].X) / 2;
            int midY = (vertices[v1].Y + vertices[v2].Y) / 2;

            // 中点がグリッドにスナップされるように調整
            midX = midX / GRID_SIZE * GRID_SIZE;
            midY = midY / GRID_SIZE * GRID_SIZE;

            // 中点が既存の頂点と重複しないかチェック
            Vertex existingMidpoint = vertices.FirstOrDefault(v => v.X == midX && v.Y == midY);
            int midpointIndex;

            if (existingMidpoint != null)
            {
                midpointIndex = vertices.IndexOf(existingMidpoint);
            }
            else
            {
                midpointIndex = vertices.Count;
                vertices.Add(new Vertex(midX, midY));
            }

            // 元の三角形を削除し、新しい4つの三角形に置き換える
            var trianglesToRemove = new List<Triangle>(trianglesContainingEdge);

            foreach (var triangleToRemove in trianglesToRemove)
            {
                triangles.Remove(triangleToRemove);
            }

            // 各元の三角形を2つに分割して4つの新しい三角形を作成
            foreach (var originalTriangle in trianglesContainingEdge)
            {
                // activeEdgeの反対側の頂点を取得
                int oppositeVertex = -1;
                if ((originalTriangle.V1 == v1 || originalTriangle.V1 == v2) &&
                    (originalTriangle.V2 == v1 || originalTriangle.V2 == v2))
                {
                    oppositeVertex = originalTriangle.V3;
                }
                else if ((originalTriangle.V2 == v1 || originalTriangle.V2 == v2) &&
                         (originalTriangle.V3 == v1 || originalTriangle.V3 == v2))
                {
                    oppositeVertex = originalTriangle.V1;
                }
                else if ((originalTriangle.V3 == v1 || originalTriangle.V3 == v2) &&
                         (originalTriangle.V1 == v1 || originalTriangle.V1 == v2))
                {
                    oppositeVertex = originalTriangle.V2;
                }

                if (oppositeVertex != -1)
                {
                    // 2つの新しい三角形を作成：(v1, midpoint, opposite) と (midpoint, v2, opposite)
                    triangles.Add(new Triangle(v1, midpointIndex, oppositeVertex));
                    triangles.Add(new Triangle(midpointIndex, v2, oppositeVertex));
                }
            }
        }

        private void DrawTemporaryTriangle(Graphics g)
        {
            if (activeEdge == null) return;

            Point p1 = WorldToScreen(vertices[activeEdge.VertexIndex1]);
            Point p2 = WorldToScreen(vertices[activeEdge.VertexIndex2]);

            Point mousePos = pictureBoxRight.PointToClient(Control.MousePosition);
            Vertex worldMousePos = ScreenToWorld(mousePos);
            int snappedX = worldMousePos.X / GRID_SIZE * GRID_SIZE;
            int snappedY = worldMousePos.Y / GRID_SIZE * GRID_SIZE;

            snappedX = Math.Max(0, Math.Min(WORLD_WIDTH, snappedX));
            snappedY = Math.Max(0, Math.Min(WORLD_HEIGHT, snappedY));

            Point snappedScreenPos = WorldToScreen(new Vertex(snappedX, snappedY));

            Pen tempPen = new Pen(Color.Green, 2.0f);
            g.DrawLine(tempPen, p1, p2);
            g.DrawLine(tempPen, p2, snappedScreenPos);
            g.DrawLine(tempPen, snappedScreenPos, p1);
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

            int midX = (p1.X + p2.X) / 2;
            int midY = (p1.Y + p2.Y) / 2;

            int dx = mousePoint.X - midX;
            int dy = mousePoint.Y - midY;
            double distanceToMiddle = Math.Sqrt(dx * dx + dy * dy);

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

        private Vertex FindNearestVertex(Point screenPoint, int maxDistance)
        {
            Vertex nearest = null;
            double minDistanceSquared = double.MaxValue;

            foreach (var vertex in vertices)
            {
                Point screenPos = WorldToScreen(vertex);
                int dx = screenPoint.X - screenPos.X;
                int dy = screenPoint.Y - screenPos.Y;
                double distanceSquared = dx * dx + dy * dy;

                if (distanceSquared < minDistanceSquared)
                {
                    minDistanceSquared = distanceSquared;
                    nearest = vertex;
                }
            }

            if (nearest != null)
            {
                Point screenPos = WorldToScreen(nearest);
                int dx = screenPoint.X - screenPos.X;
                int dy = screenPoint.Y - screenPos.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance > maxDistance)
                {
                    return null;
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