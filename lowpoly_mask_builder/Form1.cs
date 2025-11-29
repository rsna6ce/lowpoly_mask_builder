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
        private const int EDGE_ACTIVE_DISTANCE = 8;

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
            Graphics gRight = e.Graphics;
            gRight.Clear(Color.LightCyan);

            DrawGrid(gRight);

            foreach (var triangle in triangles)
            {
                DrawTriangle(gRight, triangle);
            }

            if (activeEdge != null && isAddingTriangle)
            {
                DrawTemporaryTriangle(gRight);
            }

            foreach (var vertex in vertices)
            {
                DrawVertex(gRight, vertex, vertex == selectedVertex);
            }
            // pictureBoxLeftの描画は削除
        }

        private Point MirrorWorldToScreen(Vertex vertex)
        {
            // pictureBoxRightでの座標を計算
            Point rightScreenPos = WorldToScreen(vertex);
            // pictureBoxLeftで左右反転した座標を計算
            int mirroredX = pictureBoxLeft.Width - rightScreenPos.X;
            return new Point(mirroredX, rightScreenPos.Y);
        }

        private void DrawMirrorGrid(Graphics g)
        {
            Pen gridPen = new Pen(Color.FromArgb(200, 200, 200));

            for (int x = 0; x <= WORLD_WIDTH; x += 2)
            {
                int screenX = (int)(x * (float)pictureBoxLeft.Width / WORLD_WIDTH);
                g.DrawLine(gridPen, screenX, 0, screenX, pictureBoxLeft.Height);
            }
            for (int y = 0; y <= WORLD_HEIGHT; y += 2)
            {
                int screenY = (int)(y * (float)pictureBoxLeft.Height / WORLD_HEIGHT);
                g.DrawLine(gridPen, 0, screenY, pictureBoxLeft.Width, screenY);
            }
        }

        private void DrawMirrorTriangle(Graphics g, Triangle triangle)
        {
            Point p1 = MirrorWorldToScreen(vertices[triangle.V1]);
            Point p2 = MirrorWorldToScreen(vertices[triangle.V2]);
            Point p3 = MirrorWorldToScreen(vertices[triangle.V3]);

            // 辺のみを描画
            g.DrawLine(Pens.Black, p1, p2);
            g.DrawLine(Pens.Black, p2, p3);
            g.DrawLine(Pens.Black, p3, p1);
        }

        private void DrawMirrorVertex(Graphics g, Vertex vertex)
        {
            // 無効な座標（マージされた頂点）の場合は描画しない
            if (vertex.X == -1 && vertex.Y == -1)
            {
                return;
            }

            Point screenPos = MirrorWorldToScreen(vertex);
            int radius = POINT_RADIUS;

            int grayValue = (int)((float)vertex.Z / 100.0f * 255.0f);
            grayValue = Math.Max(0, Math.Min(255, grayValue));
            Brush brush = new SolidBrush(Color.FromArgb(grayValue, grayValue, grayValue));

            g.FillEllipse(brush, screenPos.X - radius, screenPos.Y - radius, radius * 2, radius * 2);
            g.DrawEllipse(Pens.Black, screenPos.X - radius, screenPos.Y - radius, radius * 2, radius * 2);
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
            // 無効な座標（マージされた頂点）の場合は描画しない
            if (vertex.X == -1 && vertex.Y == -1)
            {
                return;
            }

            Point screenPos = WorldToScreen(vertex);
            int radius = isSelected ? POINT_RADIUS + 2 : POINT_RADIUS;

            Brush brush;
            if (isSelected)
            {
                brush = Brushes.Red;
            }
            else
            {
                int grayValue = (int)((float)vertex.Z / 100.0f * 255.0f);
                grayValue = Math.Max(0, Math.Min(255, grayValue));
                brush = new SolidBrush(Color.FromArgb(grayValue, grayValue, grayValue));
            }

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
                    if (nearestVertex == selectedVertex)
                    {
                        // すでに選択されている頂点が再度クリックされた場合、選択を解除
                        selectedVertex = null;
                        isDragging = false;
                        vScrollBarZ.Value = vScrollBarZ.Maximum; // スライダーを最上部に設定
                    }
                    else
                    {
                        // 異なる頂点が選択された場合、その頂点を新たに選択
                        selectedVertex = nearestVertex;
                        isDragging = true;
                        vScrollBarZ.Value = vScrollBarZ.Maximum - selectedVertex.Z; // Z座標に対応した位置に設定
                    }
                    isAddingTriangle = false;
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

            if (isDragging && selectedVertex != null)
            {
                MergeVerticesAtSamePosition(selectedVertex);
            }

            // pictureBoxLeftを再描画
            DrawMirrorImage();

            activeEdge = null;
            isAddingTriangle = false;
            isDragging = false;
            pictureBoxRight.Invalidate();
        }

        private void DrawMirrorImage()
        {
            if (pictureBoxLeft == null) return;

            Graphics gLeft = pictureBoxLeft.CreateGraphics();
            gLeft.Clear(Color.FromArgb(224, 224, 224));

            // 鏡像の三角形（辺のみ）を描画
            foreach (var triangle in triangles)
            {
                DrawMirrorTriangle(gLeft, triangle);
            }

            // 鏡像の頂点を描画
            foreach (var vertex in vertices)
            {
                DrawMirrorVertex(gLeft, vertex);
            }

            gLeft.Dispose();
        }

        private void MergeVerticesAtSamePosition(Vertex primaryVertex)
        {
            List<int> indicesToOverwrite = new List<int>();

            // primaryVertexと同じ座標を持つ他の頂点のインデックスを取得
            int primaryIndex = vertices.IndexOf(primaryVertex);
            for (int i = 0; i < vertices.Count; i++)
            {
                if (i != primaryIndex && vertices[i].X == primaryVertex.X && vertices[i].Y == primaryVertex.Y)
                {
                    indicesToOverwrite.Add(i);
                }
            }

            if (indicesToOverwrite.Count > 0)
            {
                // 三角形の頂点インデックスをprimaryIndexに上書き
                foreach (var triangle in triangles)
                {
                    if (indicesToOverwrite.Contains(triangle.V1))
                    {
                        triangle.V1 = primaryIndex;
                    }
                    if (indicesToOverwrite.Contains(triangle.V2))
                    {
                        triangle.V2 = primaryIndex;
                    }
                    if (indicesToOverwrite.Contains(triangle.V3))
                    {
                        triangle.V3 = primaryIndex;
                    }
                }

                // indicesToOverwriteの頂点を無効な座標に設定してレンダリングされないようにする
                foreach (int index in indicesToOverwrite)
                {
                    vertices[index].X = -1;
                    vertices[index].Y = -1;
                    vertices[index].Z = -1;
                }
            }
        }



        private void AddTriangleFromActiveEdge(Point mouseLocation)
        {
            int v1 = activeEdge.VertexIndex1;
            int v2 = activeEdge.VertexIndex2;

            // ActiveEdgeの両端が鏡像境界（X=0）にあるかチェック
            if (vertices[v1].X == 0 && vertices[v2].X == 0)
            {
                // 鏡像境界上のエッジを中点で分割
                SplitMirrorBoundaryEdge();
            }
            else
            {
                // activeEdgeが所属している三角形をすべて取得
                var trianglesContainingEdge = triangles.Where(t =>
                    ((t.V1 == v1 && t.V2 == v2) || (t.V1 == v2 && t.V2 == v1) ||
                     (t.V2 == v1 && t.V3 == v2) || (t.V2 == v2 && t.V3 == v1) ||
                     (t.V3 == v1 && t.V1 == v2) || (t.V3 == v2 && t.V1 == v1))
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
        }

        private void SplitMirrorBoundaryEdge()
        {
            int v1 = activeEdge.VertexIndex1;
            int v2 = activeEdge.VertexIndex2;

            // 中点の座標を計算（X座標は0のまま）
            int midX = 0;  // 鏡像境界上なのでX=0
            int midY = (vertices[v1].Y + vertices[v2].Y) / 2;
            midY = midY / GRID_SIZE * GRID_SIZE;  // グリッドにスナップ

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

            // このエッジを含む三角形を取得
            var trianglesContainingEdge = triangles.Where(t =>
                ((t.V1 == v1 && t.V2 == v2) || (t.V1 == v2 && t.V2 == v1) ||
                 (t.V2 == v1 && t.V3 == v2) || (t.V2 == v2 && t.V3 == v1) ||
                 (t.V3 == v1 && t.V1 == v2) || (t.V3 == v2 && t.V1 == v1))
            ).ToList();

            // 各三角形を2つに分割
            foreach (var triangle in trianglesContainingEdge.ToList())
            {
                // ActiveEdgeの反対側の頂点を取得
                int oppositeVertex = -1;
                if ((triangle.V1 == v1 || triangle.V1 == v2) && (triangle.V2 == v1 || triangle.V2 == v2))
                {
                    oppositeVertex = triangle.V3;
                }
                else if ((triangle.V2 == v1 || triangle.V2 == v2) && (triangle.V3 == v1 || triangle.V3 == v2))
                {
                    oppositeVertex = triangle.V1;
                }
                else if ((triangle.V3 == v1 || triangle.V3 == v2) && (triangle.V1 == v1 || triangle.V1 == v2))
                {
                    oppositeVertex = triangle.V2;
                }

                if (oppositeVertex != -1)
                {
                    // 元の三角形を削除
                    triangles.Remove(triangle);

                    // 2つの新しい三角形を作成
                    triangles.Add(new Triangle(v1, midpointIndex, oppositeVertex));
                    triangles.Add(new Triangle(midpointIndex, v2, oppositeVertex));
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
                DrawMirrorImage();
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