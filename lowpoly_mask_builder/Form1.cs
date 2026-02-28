using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Numerics;

namespace lowpoly_mask_builder
{
    public partial class Form1 : Form
    {
        // ============================================================
        // UNDO 機能関連
        // ============================================================
        private class ModelState
        {
            public List<Vertex> Vertices { get; set; }
            public List<Triangle> Triangles { get; set; }

            public ModelState(List<Vertex> v, List<Triangle> t)
            {
                Vertices = v.Select(x => new Vertex(x.X, x.Y, x.Z)).ToList();
                Triangles = t.Select(x => new Triangle(x.V1, x.V2, x.V3)).ToList();
            }
        }

        private readonly List<ModelState> undoStack = new List<ModelState>();
        private const int MaxUndoCount = 256;

        private void SaveUndoState()
        {
            // 現在の状態を保存（＝変更後の状態）
            undoStack.Add(new ModelState(vertices, triangles));

            if (undoStack.Count > MaxUndoCount + 1)  // +1 が超重要！
                undoStack.RemoveAt(0);

            // 編集が発生したら未保存フラグを立てる
            if (undoStack.Count > 1)
            {
                //undoStack.Count=1がアプリ起動後の初期モデル配置の状態
                isModified = true;
                UpdateTitle();
            }
        }

        private void PerformUndo()
        {
            // スタックに2個以上入っていないと戻れない（＝空振りしない）
            if (undoStack.Count < 2) return;

            // 最新（今見えている状態）は無視 → 1つ前の状態に戻る
            undoStack.RemoveAt(undoStack.Count - 1);  // 今の状態を捨てる
            ModelState previousState = undoStack[undoStack.Count - 1];  // 1つ前の状態

            // 復元
            vertices.Clear();
            triangles.Clear();
            vertices.AddRange(previousState.Vertices.Select(v => new Vertex(v.X, v.Y, v.Z)));
            triangles.AddRange(previousState.Triangles.Select(t => new Triangle(t.V1, t.V2, t.V3)));

            selectedVertex = null;
            activeEdge = null;
            isAddingTriangle = false;
            isDragging = false;

            pictureBoxRight.Invalidate();
            DrawMirrorImage();
            RefreshPreview();
            UpdateStatusLabel();
        }
        // ============================================================

        private CheckBox heightMapCheckBox;
        private FormPreview previewForm;

        private List<Vertex> vertices = new List<Vertex>();
        private List<Triangle> triangles = new List<Triangle>();
        private Vertex selectedVertex = null;
        private bool isDragging = false;
        private Edge activeEdge = null;
        private bool isAddingTriangle = false;
        private string currentFileName = null;
        private bool isModified = false;  // 編集フラグ（未保存状態かどうか）

        // 定数
        private const int GRID_SIZE = 1;
        private const int WORLD_WIDTH = 200;
        private const int WORLD_HEIGHT = 300;
        private const int WORLD_DEPTH = 150;
        private const int POINT_RADIUS = 4;
        private const int EDGE_ACTIVE_DISTANCE = 8;
        private const int THICKNESS_MM = 2;
        private const string APPLCATION_VERSION = " Ver.0.3";

        // ズーム関連
        private int zoomRate = 1;
        private const int zoomRateMax = 4;
        private Point zoomDrugStartMouse = new Point(-1, -1);
        private List<Label> xAxisLabels = new List<Label>();
        private List<Label> yAxisLabels = new List<Label>();

        public Form1()
        {
            InitializeComponent();
            InitializeHeightMapCheckBox();
            InitializeModel();
            pictureBoxRight.MouseWheel += PictureBoxRight_MouseWheel;

            // Ctrl+Z を有効化
            this.KeyPreview = true;
            this.KeyDown += Form1_KeyDown;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Z)
            {
                PerformUndo();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
            else if (e.Shift && e.KeyCode == Keys.Delete)
            {
                DeleteTrianglesUnderMouse();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void DeleteTrianglesUnderMouse()
        {
            Point mousePos = pictureBoxRight.PointToClient(Cursor.Position);
            if (!pictureBoxRight.ClientRectangle.Contains(mousePos))
                return;

            Vertex worldPoint = ScreenToWorld(mousePos);

            // マウス位置にある三角形をすべて取得
            var trianglesToDelete = triangles
                .Where(t => IsPointInTriangle(worldPoint, t))
                .ToList();

            if (trianglesToDelete.Count == 0)
                return;

            string message = trianglesToDelete.Count == 1
                ? "Delete the selected triangle?"
                : $"Delete the selected {trianglesToDelete.Count} triangles?";

            if (MessageBox.Show(message, "Delete Triangle(s)", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // Undo用に現在の状態を保存（削除前）
                SaveUndoState();

                // 削除実行
                foreach (var tri in trianglesToDelete)
                {
                    triangles.Remove(tri);
                }

                // 退化三角形や不要頂点の後始末（必要に応じて）
                RemoveDegenerateTriangles();
                // 未使用頂点のクリーンアップ
                CleanupUnusedVertices();

                // 再描画
                pictureBoxRight.Invalidate();
                DrawMirrorImage();
                RefreshPreview();
            }
        }

        private void CleanupUnusedVertices()
        {
            // 現在使用されている頂点インデックスのセットを作成
            HashSet<int> usedIndices = new HashSet<int>();

            foreach (var triangle in triangles)
            {
                usedIndices.Add(triangle.V1);
                usedIndices.Add(triangle.V2);
                usedIndices.Add(triangle.V3);
            }

            // すべての頂点を走査して、使われていないものを無効化
            for (int i = 0; i < vertices.Count; i++)
            {
                if (!usedIndices.Contains(i))
                {
                    vertices[i].X = -1;
                    vertices[i].Y = -1;
                    vertices[i].Z = -1;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            InitializeGridLabel();
            pictureBoxRight.MouseEnter += (s, args) => pictureBoxRight.Focus();
            trackBarZ.Maximum = WORLD_DEPTH;
            numericUpDownZ.Maximum = WORLD_DEPTH;

            // labelX0, labelY0をリストに追加
            xAxisLabels.Add(labelX0);
            yAxisLabels.Add(labelY0);
        }

        private void InitializeHeightMapCheckBox()
        {
            heightMapCheckBox = new CheckBox();
            heightMapCheckBox.Text = "Hightmap mode  ";
            heightMapCheckBox.CheckedChanged += HeightMapCheckBox_CheckedChanged;

            //heightMapCheckBoxHost = new ToolStripControlHost(heightMapCheckBox);
            //heightMapCheckBoxHost.Alignment = ToolStripItemAlignment.Right;

            //statusStrip1.Items.Add(heightMapCheckBoxHost);
        }

        private void HeightMapCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            DrawMirrorImage();
        }

        private void InitializeModel()
        {
            vertices.Clear();
            triangles.Clear();

            // 1. exeと同じフォルダにある new_data.lmb を探して読み込みを試みる
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string defaultFile = Path.Combine(exeDir, "new_data.lmb");

            bool loaded = false;

            if (File.Exists(defaultFile))
            {
                try
                {
                    string json = File.ReadAllText(defaultFile);
                    var data = JsonConvert.DeserializeObject<MaskBuilderFileData>(json);

                    if (data != null && data.Application == "lowpoly_mask_builder_v1")
                    {
                        foreach (var v in data.Vertices)
                            vertices.Add(new Vertex(v.X, v.Y, v.Z));

                        foreach (var t in data.Triangles)
                            triangles.Add(new Triangle(t.V1, t.V2, t.V3));

                        loaded = true;
                    }
                }
                catch
                {
                    // 何もせずフォールバック（デバッグ中はコンソールに出してもOK）
                    System.Diagnostics.Debug.WriteLine("new_data.lmb の読み込みに失敗しました。デフォルトモデルを使用します。");
                }
            }

            // 2. 読み込めなかったら従来の三角形1枚を作成
            if (!loaded)
            {
                vertices.Add(new Vertex(0, 0));
                vertices.Add(new Vertex(100, 0));
                vertices.Add(new Vertex(0, 100));
                triangles.Add(new Triangle(0, 1, 2));
            }

            // 3. 共通の初期化
            selectedVertex = null;
            activeEdge = null;
            isAddingTriangle = false;
            isDragging = false;
            currentFileName = null;                    // 新規扱い
            this.Text = "Untitled - Lowpoly Mask Builder" + APPLCATION_VERSION;

            pictureBoxRight.Invalidate();   // 右側を再描画
            DrawMirrorImage();              // ← 左側（ミラー）も確実に描画
            RefreshPreview();

            undoStack.Clear();
            SaveUndoState(); // 初期状態もUNDO可能に
        }

        private void InitializeGridLabel()
        {
            for (int n = 10; n <= 190; n += 10)
            {
                Label lbl = labelX0.Clone();
                lbl.Name = $"labelX{n}";
                lbl.Text = $"{n}";
                lbl.Location = new Point(labelX0.Location.X + 3 * n, labelX0.Location.Y);
                this.Controls.Add(lbl);
                lbl.Parent = panelZoom;
                xAxisLabels.Add(lbl);
            }
            for (int n = 10; n <= 290; n += 10)
            {
                Label lbl = labelY0.Clone();
                lbl.Name = $"labelY{n}";
                lbl.Text = $"{n}";
                lbl.Location = new Point(labelY0.Location.X, labelY0.Location.Y - 3 * n);
                this.Controls.Add(lbl);
                lbl.Parent = panelZoom;
                yAxisLabels.Add(lbl);
            }
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
        }

        private Point MirrorWorldToScreen(Vertex vertex)
        {
            Point rightScreenPos = WorldToScreen(vertex);
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
            Pen gridPen = new Pen(Color.FromArgb(128, 128, 128));
            Pen gridPen2 = new Pen(Color.FromArgb(64, 64, 64));

            for (int x = 0; x <= WORLD_WIDTH; x += 2)
            {
                int screenX = (int)(x * (float)pictureBoxRight.Width / WORLD_WIDTH);
                if (x % 10 == 0)
                {
                    g.DrawLine(gridPen2, screenX, 0, screenX, pictureBoxRight.Height);
                }
                else
                {
                    g.DrawLine(gridPen, screenX, 0, screenX, pictureBoxRight.Height);
                }
            }

            for (int y = 0; y <= WORLD_HEIGHT; y += 2)
            {
                int screenY = (int)((WORLD_HEIGHT - y) * (float)pictureBoxRight.Height / WORLD_HEIGHT) - 1;
                if (y % 10 == 0)
                {
                    g.DrawLine(gridPen2, 0, screenY, pictureBoxRight.Width, screenY);
                }
                else
                {
                    g.DrawLine(gridPen, 0, screenY, pictureBoxRight.Width, screenY);
                }
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
            float penWidth = isActive ? 3.0f : 2f;
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
            int screenY = (int)((WORLD_HEIGHT - vertex.Y) * scaleY);

            return new Point(screenX, screenY);
        }

        private Vertex ScreenToWorld(Point screenPoint)
        {
            float scaleX = (float)pictureBoxRight.Width / WORLD_WIDTH;
            float scaleY = (float)pictureBoxRight.Height / WORLD_HEIGHT;

            int worldX = (int)(screenPoint.X / scaleX);
            int worldY = WORLD_HEIGHT - (int)(screenPoint.Y / scaleY);

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
                    trackBarZ.Value = selectedVertex.Z;
                    numericUpDownZ.Value = selectedVertex.Z;
                    isAddingTriangle = false;
                    UpdateStatusLabel();
                }
                else if (activeEdge != null)
                {
                    isAddingTriangle = true;
                }
                else
                {
                    selectedVertex = null;
                    isDragging = false;
                    trackBarZ.Value = trackBarZ.Minimum;
                    numericUpDownZ.Value = 0;
                    UpdateStatusLabel();
                }
                pictureBoxRight.Invalidate();
            }
            else if (e.Button == MouseButtons.Right)
            {
                zoomDrugStartMouse = e.Location;
            }
        }

        private void pictureBoxRight_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && zoomRate > 1 && zoomDrugStartMouse.X >= 0 && zoomDrugStartMouse.Y >= 0)
            {
                int deltaX = zoomDrugStartMouse.X - e.X;
                int deltaY = zoomDrugStartMouse.Y - e.Y;
                hScrollBarZoom.Value = Math.Max(0, Math.Min(hScrollBarZoom.Value + deltaX, hScrollBarZoom.Maximum));
                vScrollBarZoom.Value = Math.Max(0, Math.Min(vScrollBarZoom.Value + deltaY, vScrollBarZoom.Maximum));
                panelZoom.Top = -vScrollBarZoom.Value;
                panelZoom.Left = -hScrollBarZoom.Value;
                DrawMirrorImage();
            }
            else
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

                    UpdateStatusLabel();
                    pictureBoxRight.Invalidate();
                }
                else if (!isAddingTriangle)
                {
                    Edge nearestEdge = FindNearestEdgeMiddle(e.Location, EDGE_ACTIVE_DISTANCE * Math.Max(pictureBoxRight.Size.Width / WORLD_WIDTH, pictureBoxRight.Size.Height / WORLD_HEIGHT));
                    if (nearestEdge != activeEdge)
                    {
                        activeEdge = nearestEdge;
                    }
                }
                pictureBoxRight.Invalidate();
            }
        }

        private void UpdateStatusLabel()
        {
            if (selectedVertex != null)
            {
                statusLabel1.Text = $"vertex : (x:{selectedVertex.X}, y:{selectedVertex.Y}, z:{selectedVertex.Z})";
            }
            else
            {
                statusLabel1.Text = "vertex : ";
            }
        }

        private void pictureBoxRight_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                bool modelChanged = false;

                if (isAddingTriangle && activeEdge != null)
                {
                    AddTriangleFromActiveEdge(e.Location);
                    modelChanged = true;
                }

                if (isDragging && selectedVertex != null)
                {
                    MergeVerticesAtSamePosition(selectedVertex);
                    modelChanged = true;
                }

                DrawMirrorImage();

                activeEdge = null;
                isAddingTriangle = false;
                isDragging = false;

                UpdateStatusLabel();
                RefreshPreview();
                pictureBoxRight.Invalidate();

                if (modelChanged)
                {
                    SaveUndoState();
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                zoomDrugStartMouse = new Point(-1, -1);
            }
        }

        private void DrawMirrorImage(Graphics g = null)
        {
            if (pictureBoxLeft == null) return;

            if (pictureBoxLeft.Image != null)
            {
                pictureBoxLeft.Image.Dispose();
                pictureBoxLeft.Image = null;
            }

            if (heightMapCheckBox.Checked)
            {
                GenerateAndDisplayHeightMap();
            }
            else
            {
                Graphics gLeft = null;
                if (g == null)
                {
                    gLeft = pictureBoxLeft.CreateGraphics();
                }
                else
                {
                    gLeft = g;
                }
                gLeft.Clear(Color.FromArgb(224, 224, 224));

                foreach (var triangle in triangles)
                {
                    DrawMirrorTriangle(gLeft, triangle);
                }

                foreach (var vertex in vertices)
                {
                    DrawMirrorVertex(gLeft, vertex);
                }

                if (g == null)
                {
                    gLeft.Dispose();
                }
            }
        }

        private void PictureBoxRight_MouseWheel(object sender, MouseEventArgs e)
        {
            // Zoom mode
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                int nextZoomRate = (0 < e.Delta) ? 2 : -2;
                SetZoomMode(nextZoomRate);
                return;
            }

            if (selectedVertex == null) return;

            int delta = e.Delta;
            int step = 1;
            int change = (delta > 0) ? step : -step;
            int newZ = Math.Max(0, Math.Min(WORLD_DEPTH, selectedVertex.Z + change));

            if (selectedVertex.Z != newZ)
            {
                selectedVertex.Z = newZ;

                trackBarZ.Value = Math.Max(trackBarZ.Minimum, Math.Min(trackBarZ.Maximum, newZ));
                if (numericUpDownZ.Value != newZ)
                {
                    numericUpDownZ.Value = newZ;
                }

                DrawMirrorImage();
                RefreshPreview();
                UpdateStatusLabel();

                SaveUndoState();
            }
        }

        private void MergeVerticesAtSamePosition(Vertex primaryVertex)
        {
            List<int> indicesToOverwrite = new List<int>();
            int primaryIndex = vertices.IndexOf(primaryVertex);

            for (int i = 0; i < vertices.Count; i++)
            {
                if (i != primaryIndex && vertices[i].X == primaryVertex.X && vertices[i].Y == primaryVertex.Y)
                {
                    indicesToOverwrite.Add(i);
                    // 上書き予定の頂点のZをコピー
                    primaryVertex.Z = vertices[i].Z;
                }
            }

            if (indicesToOverwrite.Count > 0)
            {
                foreach (var triangle in triangles.ToList())
                {
                    if (indicesToOverwrite.Contains(triangle.V1)) triangle.V1 = primaryIndex;
                    if (indicesToOverwrite.Contains(triangle.V2)) triangle.V2 = primaryIndex;
                    if (indicesToOverwrite.Contains(triangle.V3)) triangle.V3 = primaryIndex;
                }

                RemoveDegenerateTriangles();

                foreach (int index in indicesToOverwrite)
                {
                    vertices[index].X = -1;
                    vertices[index].Y = -1;
                    vertices[index].Z = -1;
                }
            }
        }

        private void RemoveDegenerateTriangles()
        {
            var trianglesToRemove = new List<Triangle>();

            foreach (var triangle in triangles)
            {
                Vertex v1 = vertices[triangle.V1];
                Vertex v2 = vertices[triangle.V2];
                Vertex v3 = vertices[triangle.V3];

                if (v1.X == -1 || v2.X == -1 || v3.X == -1)
                {
                    trianglesToRemove.Add(triangle);
                    continue;
                }

                bool isDegenerate = false;
                if (v1.X == v2.X && v1.Y == v2.Y) isDegenerate = true;
                else if (v2.X == v3.X && v2.Y == v3.Y) isDegenerate = true;
                else if (v3.X == v1.X && v3.Y == v1.Y) isDegenerate = true;

                if (isDegenerate)
                {
                    trianglesToRemove.Add(triangle);
                }
            }

            foreach (var triangleToRemove in trianglesToRemove)
            {
                triangles.Remove(triangleToRemove);
            }
        }

        private void AddTriangleFromActiveEdge(Point mouseLocation)
        {
            int v1 = activeEdge.VertexIndex1;
            int v2 = activeEdge.VertexIndex2;

            if (vertices[v1].X == 0 && vertices[v2].X == 0)
            {
                SplitMirrorBoundaryEdge();
                return;
            }

            var trianglesContainingEdge = triangles.Where(t =>
                ((t.V1 == v1 && t.V2 == v2) || (t.V1 == v2 && t.V2 == v1) ||
                 (t.V2 == v1 && t.V3 == v2) || (t.V2 == v2 && t.V3 == v1) ||
                 (t.V3 == v1 && t.V1 == v2) || (t.V3 == v2 && t.V1 == v1))
            ).ToList();

            if (trianglesContainingEdge.Count > 1)
            {
                SplitTrianglesByAddingMidpoint(trianglesContainingEdge);
                return;
            }

            Vertex worldPos = ScreenToWorld(mouseLocation);
            int newX = worldPos.X / GRID_SIZE * GRID_SIZE;
            int newY = worldPos.Y / GRID_SIZE * GRID_SIZE;
            int newZ = (vertices[v1].Z + vertices[v2].Z) / 2;
            newX = Math.Max(0, Math.Min(WORLD_WIDTH, newX));
            newY = Math.Max(0, Math.Min(WORLD_HEIGHT, newY));

            Vertex existingVertex = vertices.FirstOrDefault(v => v.X == newX && v.Y == newY);
            int newVertexIndex;

            if (existingVertex != null)
            {
                newVertexIndex = vertices.IndexOf(existingVertex);
            }
            else
            {
                newVertexIndex = vertices.Count;
                vertices.Add(new Vertex(newX, newY, newZ));
            }

            Triangle newTriangle = CreateTriangleWithCorrectOrientation(v1, v2, newVertexIndex, trianglesContainingEdge);
            triangles.Add(newTriangle);
        }

        private Triangle CreateTriangleWithCorrectOrientation(int v1, int v2, int newVertexIndex, List<Triangle> trianglesContainingEdge)
        {
            if (trianglesContainingEdge.Count == 0)
            {
                // 隣接する三角形がない場合は標準の順序で作成
                return new Triangle(v1, v2, newVertexIndex);
            }

            // activeEdgeが所属する三角形の接続順を確認
            var adjacentTriangle = trianglesContainingEdge.First();

            // activeEdgeがこの三角形内でどの順序で接続されているかを確認
            if ((adjacentTriangle.V1 == v1 && adjacentTriangle.V2 == v2) ||
                (adjacentTriangle.V2 == v2 && adjacentTriangle.V3 == v1) ||
                (adjacentTriangle.V3 == v1 && adjacentTriangle.V2 == v2))
            {
                // activeEdgeがこの三角形内で「v1→v2」の順序で接続されている場合、
                // 新しい三角形でも同じ順序「v1→v2」を使用する
                // というのはバグかもしれない？？？　暫定で2->1にしておく
                return new Triangle(v2, v1, newVertexIndex);
            }
            else
            {
                // activeEdgeがこの三角形内で「v2→v1」の順序で接続されている場合、
                // 新しい三角形でも同じ順序「v2→v1」を使用する
                return new Triangle(v2, v1, newVertexIndex);
            }
        }

        private void SplitMirrorBoundaryEdge()
        {
            int v1 = activeEdge.VertexIndex1;
            int v2 = activeEdge.VertexIndex2;

            // 中点の座標を計算（X座標は0のまま）
            int midX = (vertices[v1].X + vertices[v2].X) / 2;
            int midY = (vertices[v1].Y + vertices[v2].Y) / 2;
            int midZ = (vertices[v1].Z + vertices[v2].Z) / 2;
            midY = midY / GRID_SIZE * GRID_SIZE;  // グリッドにスナップ
            midZ = midZ / GRID_SIZE * GRID_SIZE;  // グリッドにスナップ


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
                vertices.Add(new Vertex(midX, midY, midZ));
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

            // 中点を作成
            int midX = (vertices[v1].X + vertices[v2].X) / 2;
            int midY = (vertices[v1].Y + vertices[v2].Y) / 2;
            int midZ = (vertices[v1].Z + vertices[v2].Z) / 2;
            midX = midX / GRID_SIZE * GRID_SIZE;
            midY = midY / GRID_SIZE * GRID_SIZE;
            midZ = midZ / GRID_SIZE * GRID_SIZE;

            Vertex existingMidpoint = vertices.FirstOrDefault(v => v.X == midX && v.Y == midY);
            int midpointIndex;
            if (existingMidpoint != null)
            {
                midpointIndex = vertices.IndexOf(existingMidpoint);
            }
            else
            {
                midpointIndex = vertices.Count;
                vertices.Add(new Vertex(midX, midY, midZ));
            }

            // 元の三角形を削除
            var trianglesToRemove = new List<Triangle>(trianglesContainingEdge);
            foreach (var triangleToRemove in trianglesToRemove)
            {
                triangles.Remove(triangleToRemove);
            }

            foreach (var originalTriangle in trianglesContainingEdge)
            {
                int oppositeVertex = GetOppositeVertex(originalTriangle, v1, v2);
                if (oppositeVertex != -1)
                {
                    // この三角形内で、共有する辺がどの位置にあるかを特定
                    int firstEdgeVertex, secondEdgeVertex;

                    if (originalTriangle.V1 == v1 && originalTriangle.V2 == v2)
                    {
                        // 辺が V1→V2 の順序
                        firstEdgeVertex = v1;
                        secondEdgeVertex = v2;
                    }
                    else if (originalTriangle.V2 == v1 && originalTriangle.V3 == v2)
                    {
                        // 辺が V2→V3 の順序
                        firstEdgeVertex = v1;
                        secondEdgeVertex = v2;
                    }
                    else if (originalTriangle.V3 == v1 && originalTriangle.V1 == v2)
                    {
                        // 辺が V3→V1 の順序
                        firstEdgeVertex = v1;
                        secondEdgeVertex = v2;
                    }
                    else
                    {
                        // 逆順の場合
                        firstEdgeVertex = v2;
                        secondEdgeVertex = v1;
                    }

                    // 元の三角形の接続順序を維持して2つの新しい三角形を作成
                    triangles.Add(new Triangle(firstEdgeVertex, midpointIndex, oppositeVertex));
                    triangles.Add(new Triangle(midpointIndex, secondEdgeVertex, oppositeVertex));
                }
            }
        }

        private int GetOppositeVertex(Triangle triangle, int edgeVertex1, int edgeVertex2)
        {
            if ((triangle.V1 == edgeVertex1 || triangle.V1 == edgeVertex2) &&
                (triangle.V2 == edgeVertex1 || triangle.V2 == edgeVertex2))
            {
                return triangle.V3;
            }
            else if ((triangle.V2 == edgeVertex1 || triangle.V2 == edgeVertex2) &&
                     (triangle.V3 == edgeVertex1 || triangle.V3 == edgeVertex2))
            {
                return triangle.V1;
            }
            else if ((triangle.V3 == edgeVertex1 || triangle.V3 == edgeVertex2) &&
                     (triangle.V1 == edgeVertex1 || triangle.V1 == edgeVertex2))
            {
                return triangle.V2;
            }
            return -1;
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

        private void trackBarZ_Scroll(object sender, EventArgs e)
        {
            if (selectedVertex != null)
            {
                int newZ = trackBarZ.Value;
                if (selectedVertex.Z != newZ)
                {
                    selectedVertex.Z = newZ;
                    DrawMirrorImage();
                    RefreshPreview();
                    UpdateStatusLabel();

                    if ((int)numericUpDownZ.Value != newZ)
                    {
                        numericUpDownZ.Value = newZ;
                    }

                    SaveUndoState();
                }
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            InitializeModel();
            DrawMirrorImage();
            RefreshPreview();
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

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "Lowpoly Mask Builder files (*.lmb)|*.lmb|All files (*.*)|*.*";
                openDialog.Title = "Open File";

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string jsonText = File.ReadAllText(openDialog.FileName);
                        var data = JsonConvert.DeserializeObject<MaskBuilderFileData>(jsonText);

                        if (data == null || data.Application != "lowpoly_mask_builder_v1")
                        {
                            MessageBox.Show("Invalid file format.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        vertices.Clear();
                        triangles.Clear();

                        foreach (var vertexData in data.Vertices)
                        {
                            vertices.Add(new Vertex(vertexData.X, vertexData.Y, vertexData.Z));
                        }

                        foreach (var triangleData in data.Triangles)
                        {
                            triangles.Add(new Triangle(triangleData.V1, triangleData.V2, triangleData.V3));
                        }

                        currentFileName = openDialog.FileName;
                        this.Text = Path.GetFileName(currentFileName) + " - Lowpoly Mask Builder" + APPLCATION_VERSION;

                        SaveUndoState(); // 開いた状態もUNDO可能に

                        pictureBoxRight.Invalidate();
                        DrawMirrorImage();
                        RefreshPreview();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading file:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "Lowpoly Mask Builder files (*.lmb)|*.lmb";
                saveDialog.Title = "Save As";

                string defaultFileName = GetDefaultFileName();
                saveDialog.FileName = defaultFileName;

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        SaveFile(saveDialog.FileName);
                        currentFileName = saveDialog.FileName;
                        this.Text = Path.GetFileName(currentFileName) + " - Lowpoly Mask Builder" + APPLCATION_VERSION;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error saving file:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void SaveFile(string fileName)
        {
            var data = new MaskBuilderFileData
            {
                Application = "lowpoly_mask_builder_v1",
                Vertices = vertices.Select(v => new VertexData { X = v.X, Y = v.Y, Z = v.Z }).ToList(),
                Triangles = triangles.Select(t => new TriangleData { V1 = t.V1, V2 = t.V2, V3 = t.V3 }).ToList()
            };

            string jsonText = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(fileName, jsonText);

            // 保存成功したら未保存フラグをクリア
            isModified = false;
            UpdateTitle();
        }

        private void UpdateTitle()
        {
            string fileNamePart = string.IsNullOrEmpty(currentFileName)
                ? "Untitled"
                : Path.GetFileName(currentFileName);

            string modifiedMark = isModified ? "*" : "";

            this.Text = $"{fileNamePart}{modifiedMark} - Lowpoly Mask Builder{APPLCATION_VERSION}";
        }

        private string GetDefaultFileName()
        {
            if (!string.IsNullOrEmpty(currentFileName))
            {
                return currentFileName;
            }

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string baseName = "lowpoly_mask_builder";
            int counter = 1;

            while (true)
            {
                string candidateFileName = Path.Combine(documentsPath, $"{baseName}{counter:D3}.lmb");
                if (!File.Exists(candidateFileName))
                {
                    return candidateFileName;
                }
                counter++;
            }
        }

        private void numericUpDownZ_ValueChanged(object sender, EventArgs e)
        {
            if (selectedVertex != null)
            {
                int newZValue = (int)numericUpDownZ.Value;
                if (selectedVertex.Z != newZValue)
                {
                    selectedVertex.Z = newZValue;

                    if (trackBarZ.Value != newZValue)
                    {
                        trackBarZ.Value = newZValue;
                    }
                    DrawMirrorImage();
                    UpdateStatusLabel();
                    RefreshPreview();

                    SaveUndoState();
                }
            }
        }

        private void GenerateAndDisplayHeightMap()
        {
            if (pictureBoxLeft == null) return;

            int width = pictureBoxLeft.Width;
            int height = pictureBoxLeft.Height;

            Bitmap heightMap = new Bitmap(width, height);

            BitmapData bitmapData = heightMap.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);

            try
            {
                unsafe
                {
                    Parallel.For(0, height, y =>
                    {
                        byte* basePtr = (byte*)bitmapData.Scan0 + y * bitmapData.Stride;

                        for (int x = 0; x < width; x++)
                        {
                            int mirroredScreenX = width - x;
                            Vertex worldPoint = ScreenToWorld(new Point(mirroredScreenX, y));

                            float interpolatedZ = 0.0f;
                            bool insideAnyTriangle = false;

                            foreach (var triangle in triangles)
                            {
                                if (IsPointInTriangle(worldPoint, triangle))
                                {
                                    interpolatedZ = InterpolateZInTriangle(worldPoint, triangle);
                                    insideAnyTriangle = true;
                                    break;
                                }
                            }

                            int grayValue;
                            if (insideAnyTriangle)
                            {
                                grayValue = (int)((interpolatedZ / 100.0f) * 255.0f);
                                grayValue = Math.Max(0, Math.Min(255, grayValue));
                            }
                            else
                            {
                                grayValue = 0;
                            }

                            int pixelIndex = x * 3;
                            basePtr[pixelIndex] = (byte)grayValue;     // Blue
                            basePtr[pixelIndex + 1] = (byte)grayValue; // Green
                            basePtr[pixelIndex + 2] = (byte)grayValue; // Red
                        }
                    });
                }
            }
            finally
            {
                heightMap.UnlockBits(bitmapData);
            }

            pictureBoxLeft.Image = heightMap;
        }

        // 指定された点が三角形内にあるかどうかを判定
        private bool IsPointInTriangle(Vertex point, Triangle triangle)
        {
            Vertex v1 = vertices[triangle.V1];
            Vertex v2 = vertices[triangle.V2];
            Vertex v3 = vertices[triangle.V3];

            // 重心座標を計算
            float denominator = ((v2.Y - v3.Y) * (v1.X - v3.X) + (v3.X - v2.X) * (v1.Y - v3.Y));
            if (Math.Abs(denominator) < 0.0001f) return false;

            float alpha = ((v2.Y - v3.Y) * (point.X - v3.X) + (v3.X - v2.X) * (point.Y - v3.Y)) / denominator;
            float beta = ((v3.Y - v1.Y) * (point.X - v3.X) + (v1.X - v3.X) * (point.Y - v3.Y)) / denominator;
            float gamma = 1.0f - alpha - beta;

            // 辺上も含めるために、判定条件を緩和（0以上または非常に小さな負の値も許容）
            const float epsilon = 1e-6f;  // 微小な誤差を許容するための閾値
            bool alphaOk = alpha >= -epsilon;
            bool betaOk = beta >= -epsilon;
            bool gammaOk = gamma >= -epsilon;

            // 重心座標の和が1に近いことを確認
            bool barycentricSumOk = Math.Abs(alpha + beta + gamma - 1.0f) < epsilon;

            return alphaOk && betaOk && gammaOk && barycentricSumOk;
        }

        // Z値の補間関数も同様に修正
        private float InterpolateZInTriangle(Vertex point, Triangle triangle)
        {
            Vertex v1 = vertices[triangle.V1];
            Vertex v2 = vertices[triangle.V2];
            Vertex v3 = vertices[triangle.V3];

            // 三点から平面方程式 ax + by + cz = d を求める
            float a1 = v2.X - v1.X;
            float b1 = v2.Y - v1.Y;
            float c1 = v2.Z - v1.Z;

            float a2 = v3.X - v1.X;
            float b2 = v3.Y - v1.Y;
            float c2 = v3.Z - v1.Z;

            // 平面の法線ベクトル (a, b, c) を求める
            float a = b1 * c2 - b2 * c1;
            float b = c1 * a2 - c2 * a1;
            float c = a1 * b2 - a2 * b1;

            // 平面方程式の定数項 d を求める
            float d = -(a * v1.X + b * v1.Y + c * v1.Z);

            // 指定された点 (point.X, point.Y) で平面上のZ値を求める
            // 平面方程式：a*x + b*y + c*z + d = 0 より、z = -(a*x + b*y + d) / c
            if (Math.Abs(c) > 1e-6f)
            {
                float z = -(a * point.X + b * point.Y + d) / c;
                return z;
            }

            // cが非常に小さい場合は平面が垂直に近いため、平均値を使用
            return (v1.Z + v2.Z + v3.Z) / 3.0f;
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "STL files (*.stl)|*.stl|All files (*.*)|*.*";
                saveDialog.Title = "Export as STL";

                string defaultFileName = GetDefaultFileName();
                // .lmbから.stlに拡張子を変更
                string stlFileName = Path.ChangeExtension(defaultFileName, "surface.stl");
                saveDialog.FileName = stlFileName;

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ExportToBinaryStl(saveDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error exporting STL file:\n{ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExportToBinaryStl(string fileName)
        {
            // 有効な頂点（マージされた無効な頂点）を抽出
            List<Vertex> validVertices = vertices.Where(v => v.X != -1 && v.Y != -1).ToList();

            // 頂点インデックスの再マッピングを作成
            Dictionary<Vertex, int> vertexRemapping = new Dictionary<Vertex, int>();
            for (int i = 0; i < validVertices.Count; i++)
            {
                vertexRemapping[validVertices[i]] = i;
            }

            // 右側の三角形をコピー（頂点インデックスを再マッピング）
            List<Triangle> rightTriangles = new List<Triangle>();
            foreach (var triangle in triangles)
            {
                if (vertexRemapping.ContainsKey(vertices[triangle.V1]) &&
                    vertexRemapping.ContainsKey(vertices[triangle.V2]) &&
                    vertexRemapping.ContainsKey(vertices[triangle.V3]))
                {
                    rightTriangles.Add(new Triangle(
                        vertexRemapping[vertices[triangle.V1]],
                        vertexRemapping[vertices[triangle.V2]],
                        vertexRemapping[vertices[triangle.V3]]));
                }
            }

            // 左側の頂点を生成（X座標のみを負に変換）
            List<Vertex> leftVertices = new List<Vertex>();
            foreach (var vertex in validVertices)
            {
                // X座標を負に変換して左側の頂点を追加
                leftVertices.Add(new Vertex(-vertex.X, vertex.Y, vertex.Z));
            }

            // 左側の三角形を生成（頂点インデックスに右側の頂点数分のオフセットを追加）
            List<Triangle> leftTriangles = new List<Triangle>();
            int rightVertexCount = validVertices.Count; // 右側の頂点数
            foreach (var rightTriangle in rightTriangles)
            {
                // 左側の三角形を作成。頂点インデックスに右側の頂点数分のオフセットを追加
                leftTriangles.Add(new Triangle(
                    rightVertexCount + rightTriangle.V3,
                    rightVertexCount + rightTriangle.V2,
                    rightVertexCount + rightTriangle.V1
                ));
            }

            // 全ての頂点と三角形を結合
            List<Vertex> allVertices = new List<Vertex>();
            allVertices.AddRange(validVertices);   // 右側の頂点（インデックス 0 ～ rightVertexCount-1）
            allVertices.AddRange(leftVertices);   // 左側の頂点（インデックス rightVertexCount ～）

            List<Triangle> allTriangles = new List<Triangle>();
            allTriangles.AddRange(rightTriangles);  // 右側の三角形
            allTriangles.AddRange(leftTriangles);  // 左側の三角形

            // バイナリSTLファイルとして保存
            using (FileStream stream = new FileStream(fileName, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    // STLヘッダー（80バイト）
                    string header = "Lowpoly Mask Builder";
                    byte[] headerBytes = new byte[80];
                    byte[] headerStringBytes = System.Text.Encoding.ASCII.GetBytes(header);
                    Array.Copy(headerStringBytes, headerBytes, Math.Min(headerStringBytes.Length, 80));
                    writer.Write(headerBytes);

                    // 三角形の数
                    writer.Write(allTriangles.Count);

                    // 各三角形のデータを書き込み
                    foreach (var triangle in allTriangles)
                    {
                        Vertex v1 = allVertices[triangle.V1];
                        Vertex v2 = allVertices[triangle.V2];
                        Vertex v3 = allVertices[triangle.V3];

                        // 法線ベクトルを計算
                        float nx, ny, nz;
                        CalculateNormal(v1, v2, v3, out nx, out ny, out nz);

                        // 法線ベクトル
                        writer.Write((float)nx);
                        writer.Write((float)ny);
                        writer.Write((float)nz);

                        // 各頂点の座標
                        writer.Write((float)v1.X);
                        writer.Write((float)v1.Y);
                        writer.Write((float)v1.Z);

                        writer.Write((float)v2.X);
                        writer.Write((float)v2.Y);
                        writer.Write((float)v2.Z);

                        writer.Write((float)v3.X);
                        writer.Write((float)v3.Y);
                        writer.Write((float)v3.Z);

                        // 属性バイト数（通常は0）
                        writer.Write((ushort)0);
                    }
                }
            }
        }

        private void CalculateNormal(Vertex v1, Vertex v2, Vertex v3, out float nx, out float ny, out float nz)
        {
            // 2つの辺ベクトルを計算
            float edge1x = v2.X - v1.X;
            float edge1y = v2.Y - v1.Y;
            float edge1z = v2.Z - v1.Z;

            float edge2x = v3.X - v1.X;
            float edge2y = v3.Y - v1.Y;
            float edge2z = v3.Z - v1.Z;

            // 外積を計算して法線ベクトルを得る
            nx = edge1y * edge2z - edge1z * edge2y;
            ny = edge1z * edge2x - edge1x * edge2z;
            nz = edge1x * edge2y - edge1y * edge2x;

            // 正規化
            float length = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (length > 0.0001f)
            {
                nx /= length;
                ny /= length;
                nz /= length;
            }
        }

        private void transparentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            transparentToolStripMenuItem.Checked = !transparentToolStripMenuItem.Checked;
            if (transparentToolStripMenuItem.Checked)
            {
                this.TransparencyKey = Color.LightCyan;
            }
            else
            {
                this.TransparencyKey = Color.Empty;
            }
        }

        private void export2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "STL files (*.stl)|*.stl|All files (*.*)|*.*";
                saveDialog.Title = "Export as Volume STL";

                string defaultFileName = GetDefaultFileName();
                string stlFileName = Path.ChangeExtension(defaultFileName, "volume.stl");
                saveDialog.FileName = stlFileName;

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ExportVolumeWithMirror(saveDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error during export:\n{ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExportVolumeWithMirror(string fileName)
        {
            // 1) 有効頂点抽出（元と同様）
            List<Vertex> validVerticesRaw = vertices.Where(v => v.X != -1 && v.Y != -1).ToList();

            // 2) 完全一致頂点をマージ（Index マップを得る）
            //    Standalone と同等の挙動にするために完全一致マージを行う
            var (mergedVerts, indexMap) = MergeDuplicateVertices(validVerticesRaw);

            // 3) base (右側) 三角形を構築（インデックスを remap）
            List<Triangle> rightTriangles = new List<Triangle>();
            foreach (var tri in triangles)
            {
                // vertices[tri.V1] などは元の vertices リストの参照なので、validVerticesRaw 内の位置を使う必要がある
                // ただし元のコードでは vertexRemap[vertices[triangle.V1]] を使っていたので、ここも同様に処理するため
                // まず original vertices 配列中の tri.V* が有効な頂点であるかチェックします
                Vertex va = vertices[tri.V1];
                Vertex vb = vertices[tri.V2];
                Vertex vc = vertices[tri.V3];

                // 各頂点が validVerticesRaw の何番目かを探す -> but we have indexMap keyed by original validVerticesRaw index.
                // To be robust, build reverse map: Vertex instance -> index in validVerticesRaw
            }

            // To simplify (and safe): build map from Vertex instance (reference) to index in validVerticesRaw
            Dictionary<Vertex, int> refIndex = new Dictionary<Vertex, int>();
            for (int i = 0; i < validVerticesRaw.Count; i++) refIndex[validVerticesRaw[i]] = i;

            // Now build rightTriangles using remapped indices into mergedVerts
            for (int i = 0; i < triangles.Count; i++)
            {
                var tri = triangles[i];
                if (!refIndex.ContainsKey(vertices[tri.V1]) ||
                    !refIndex.ContainsKey(vertices[tri.V2]) ||
                    !refIndex.ContainsKey(vertices[tri.V3]))
                    continue;

                int idxA = refIndex[vertices[tri.V1]];
                int idxB = refIndex[vertices[tri.V2]];
                int idxC = refIndex[vertices[tri.V3]];

                // map through indexMap (merged index)
                int mA = indexMap[idxA];
                int mB = indexMap[idxB];
                int mC = indexMap[idxC];

                // Add triangle using merged vertex indices
                rightTriangles.Add(new Triangle(mA, mB, mC));
            }

            int n = mergedVerts.Count;
            float thickness = THICKNESS_MM;

            // 4) 裏面頂点を作る（まずコピー）
            List<Vertex> backVertices = new List<Vertex>(n);
            for (int i = 0; i < n; i++)
            {
                var v = mergedVerts[i];
                backVertices.Add(new Vertex(v.X, v.Y, v.Z));
            }

            // 5) 面法線→頂点平均を計算
            Vector3[] accum = new Vector3[n];
            int[] counts = new int[n];

            foreach (var t in rightTriangles)
            {
                var a = mergedVerts[t.V1];
                var b = mergedVerts[t.V2];
                var c = mergedVerts[t.V3];

                Vector3 p1 = new Vector3(a.X, a.Y, a.Z);
                Vector3 p2 = new Vector3(b.X, b.Y, b.Z);
                Vector3 p3 = new Vector3(c.X, c.Y, c.Z);

                Vector3 faceNormal = Vector3.Cross(p2 - p1, p3 - p1);
                float len = faceNormal.Length();
                if (len > 1e-8f) faceNormal = Vector3.Normalize(faceNormal);
                else faceNormal = new Vector3(0, 0, 1);

                // accumulate face normals
                accum[t.V1] += faceNormal;
                accum[t.V2] += faceNormal;
                accum[t.V3] += faceNormal;

                counts[t.V1]++; counts[t.V2]++; counts[t.V3]++;
            }

            // 6) 押し出し（Standalone と同じく **逆方向に押し出す**：-normal * thickness）
            for (int i = 0; i < n; i++)
            {
                if (counts[i] == 0) continue;
                Vector3 avg = accum[i] / counts[i];
                if (avg.Length() <= 1e-8f) avg = new Vector3(0, 0, 1);

                Vector3 dir = Vector3.Normalize(avg);
                Vector3 offset = dir * (-thickness); // <<--- **符号が負：法線の逆方向**（Standalone と整合）

                // float -> int で丸め（既存 Vertex が int のままなので丸める）
                backVertices[i].X = (int)Math.Round(mergedVerts[i].X + offset.X);
                backVertices[i].Y = (int)Math.Round(mergedVerts[i].Y + offset.Y);
                backVertices[i].Z = (int)Math.Round(mergedVerts[i].Z + offset.Z);
            }

            // 7) 裏面三角形（表を逆順にして追加）
            List<Triangle> backTriangles = new List<Triangle>();
            for (int i = 0; i < rightTriangles.Count; i++)
            {
                var t = rightTriangles[i];
                backTriangles.Add(new Triangle(t.V3 + n, t.V2 + n, t.V1 + n)); // reverse winding
            }

            // 8) 境界エッジのみを側面で閉じる（エッジカウントで判定）
            var edgeCount = new Dictionary<(int, int), int>();
            foreach (var t in rightTriangles)
            {
                AddEdgeCount(edgeCount, t.V1, t.V2);
                AddEdgeCount(edgeCount, t.V2, t.V3);
                AddEdgeCount(edgeCount, t.V3, t.V1);
            }

            List<Triangle> sideTriangles = new List<Triangle>();
            foreach (var kv in edgeCount)
            {
                if (kv.Value != 1) continue; // 境界でないエッジは無視

                int a = kv.Key.Item1;
                int b = kv.Key.Item2;

                int a2 = a + n;
                int b2 = b + n;

                // side: two triangles (winding such that normal points outward)
                sideTriangles.Add(new Triangle(a, b, b2));
                sideTriangles.Add(new Triangle(a, b2, a2));
            }

            // 9) 元頂点群（mergedVerts + backVertices）をまとめる
            List<Vertex> origAllVerts = new List<Vertex>();
            origAllVerts.AddRange(mergedVerts);
            origAllVerts.AddRange(backVertices);
            int origCount = origAllVerts.Count;

            // 10) ミラー（X反転）を作る
            List<Vertex> mirrorVerts = new List<Vertex>(origCount);
            for (int i = 0; i < origCount; i++)
            {
                var v = origAllVerts[i];
                mirrorVerts.Add(new Vertex(-v.X, v.Y, v.Z));
            }

            // 11) ミラー側三角形を作る（インデックスをオフセット）
            List<Triangle> allOrigTriangles = new List<Triangle>();
            allOrigTriangles.AddRange(rightTriangles);
            allOrigTriangles.AddRange(backTriangles);
            allOrigTriangles.AddRange(sideTriangles);

            List<Triangle> mirrorTriangles = new List<Triangle>();
            int mirrorOffset = origCount;
            foreach (var t in allOrigTriangles)
            {
                // reverse winding for mirror copy (keep consistent outward normals)
                mirrorTriangles.Add(new Triangle(mirrorOffset + t.V3, mirrorOffset + t.V2, mirrorOffset + t.V1));
            }

            // 12) 最終頂点/三角形集合を作成して書き出す
            List<Vertex> finalVerts = new List<Vertex>();
            finalVerts.AddRange(origAllVerts);
            finalVerts.AddRange(mirrorVerts);

            List<Triangle> finalTris = new List<Triangle>();
            finalTris.AddRange(allOrigTriangles);
            finalTris.AddRange(mirrorTriangles);

            // 13) 出力（既存 SaveBinaryStl 互換）
            SaveBinaryStl(fileName, finalVerts, finalTris);
        }

        // エッジ重複排除用
        private void AddEdgeCount(Dictionary<(int, int), int> dict, int a, int b)
        {
            var key = a < b ? (a, b) : (b, a);
            if (!dict.ContainsKey(key)) dict[key] = 0;
            dict[key]++;
        }

        // 完全一致頂点のマージ（戻り値：merged vertex list, indexMap: 元 validVerticesRaw index -> merged index）
        private (List<Vertex> merged, int[] indexMap) MergeDuplicateVertices(List<Vertex> validRaw)
        {
            var map = new Dictionary<string, int>();
            var merged = new List<Vertex>();
            int[] indexMap = new int[validRaw.Count];
            for (int i = 0; i < validRaw.Count; i++)
            {
                var v = validRaw[i];
                string key = $"{v.X}_{v.Y}_{v.Z}";
                if (!map.TryGetValue(key, out int idx))
                {
                    idx = merged.Count;
                    map[key] = idx;
                    merged.Add(new Vertex(v.X, v.Y, v.Z));
                }
                indexMap[i] = idx;
            }
            return (merged, indexMap);
        }

        private void SaveBinaryStl(string fileName, List<Vertex> verts, List<Triangle> tris)
        {
            using (FileStream stream = new FileStream(fileName, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                byte[] header = new byte[80];
                writer.Write(header);

                writer.Write(tris.Count);

                foreach (var t in tris)
                {
                    var v1 = verts[t.V1];
                    var v2 = verts[t.V2];
                    var v3 = verts[t.V3];

                    float nx, ny, nz;
                    CalculateNormal(v1, v2, v3, out nx, out ny, out nz);

                    writer.Write(nx); writer.Write(ny); writer.Write(nz);

                    writer.Write((float)v1.X); writer.Write((float)v1.Y); writer.Write((float)v1.Z);
                    writer.Write((float)v2.X); writer.Write((float)v2.Y); writer.Write((float)v2.Z);
                    writer.Write((float)v3.X); writer.Write((float)v3.Y); writer.Write((float)v3.Z);

                    writer.Write((ushort)0);
                }
            }
        }
        private void unifyTriangleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (triangles.Count == 0)
            {
                MessageBox.Show("There are no triangles.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SaveUndoState(); // 変更前に保存

            HashSet<Triangle> processedTriangles = new HashSet<Triangle>();
            Queue<Triangle> triangleQueue = new Queue<Triangle>();
            triangleQueue.Enqueue(triangles[0]);
            processedTriangles.Add(triangles[0]);
            int flippedCount = 0;

            while (triangleQueue.Count > 0)
            {
                Triangle currentTriangle = triangleQueue.Dequeue();

                foreach (var adjacentTriangle in triangles)
                {
                    if (processedTriangles.Contains(adjacentTriangle)) continue;

                    if (HasSharedEdge(currentTriangle, adjacentTriangle))
                    {
                        if (!AreTriangleNormalsConsistent(currentTriangle, adjacentTriangle))
                        {
                            FlipTriangle(adjacentTriangle);
                            flippedCount++;
                        }

                        processedTriangles.Add(adjacentTriangle);
                        triangleQueue.Enqueue(adjacentTriangle);
                    }
                }
            }

            pictureBoxRight.Invalidate();
            DrawMirrorImage();
            RefreshPreview();

            MessageBox.Show($"Triangle orientations have been unified.\nNumber of flipped triangles: {flippedCount}",
                            "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool HasSharedEdge(Triangle triangle1, Triangle triangle2)
        {
            var edges1 = new[] { (triangle1.V1, triangle1.V2), (triangle1.V2, triangle1.V3), (triangle1.V3, triangle1.V1) };

            foreach (var edge1 in edges1)
            {
                // 同じ頂点ペアを持つかチェック（順序は問わない）
                if ((triangle2.V1 == edge1.Item1 && triangle2.V2 == edge1.Item2) ||
                    (triangle2.V1 == edge1.Item2 && triangle2.V2 == edge1.Item1) ||
                    (triangle2.V2 == edge1.Item1 && triangle2.V3 == edge1.Item2) ||
                    (triangle2.V2 == edge1.Item2 && triangle2.V3 == edge1.Item1) ||
                    (triangle2.V3 == edge1.Item1 && triangle2.V1 == edge1.Item2) ||
                    (triangle2.V3 == edge1.Item2 && triangle2.V1 == edge1.Item1))
                {
                    return true;
                }
            }
            return false;
        }

        // 二つの三角形の法線が同じ方向を向いているかを判定
        private bool AreTriangleNormalsConsistent(Triangle triangle1, Triangle triangle2)
        {
            float nx1, ny1, nz1;
            float nx2, ny2, nz2;

            CalculateNormal(vertices[triangle1.V1], vertices[triangle1.V2], vertices[triangle1.V3], out nx1, out ny1, out nz1);
            CalculateNormal(vertices[triangle2.V1], vertices[triangle2.V2], vertices[triangle2.V3], out nx2, out ny2, out nz2);

            // 法線の方向が同じ方向を向いているか（内積が正であるか）を確認
            float dotProduct = nx1 * nx2 + ny1 * ny2 + nz1 * nz2;

            // 内積が正であれば、同じ方向を向いている
            return dotProduct > 0.0f;
        }
        private void FlipTriangle(Triangle triangle)
        {
            int temp = triangle.V1;
            triangle.V1 = triangle.V3;
            triangle.V3 = temp;
        }

        private void flipAllTriangleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (triangles.Count == 0)
            {
                MessageBox.Show("There are no triangles.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SaveUndoState();

            int flippedCount = 0;
            foreach (var triangle in triangles)
            {
                int temp = triangle.V1;
                triangle.V1 = triangle.V3;
                triangle.V3 = temp;
                flippedCount++;
            }

            pictureBoxRight.Invalidate();
            DrawMirrorImage();
            RefreshPreview();

            MessageBox.Show($"All triangles have been flipped.\nNumber of flipped triangles: {flippedCount}",
                            "Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void previewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (previewForm == null || previewForm.IsDisposed)
            {
                previewForm = new FormPreview();
                previewForm.Owner = this;
                previewForm.FormClosed += (s, args) => previewForm = null;
                previewForm.UpdateModel(vertices, triangles);
                previewForm.Show();
            }
            else
            {
                previewForm.BringToFront();
            }
        }

        private void RefreshPreview()
        {
            previewForm?.UpdateModel(vertices, triangles);
        }

        private void scaleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var scaleForm = new FormScale())
            {
                if (scaleForm.ShowDialog(this) == DialogResult.OK)
                {
                    SaveUndoState();

                    float sx = scaleForm.ScaleX / 100.0f;
                    float sy = scaleForm.ScaleY / 100.0f;
                    float sz = scaleForm.ScaleZ / 100.0f;

                    foreach (var v in vertices)
                    {
                        if (v.X == -1 && v.Y == -1) continue;

                        v.X = Math.Max(0, Math.Min(WORLD_WIDTH, (int)(v.X * sx + 0.5f)));
                        v.Y = Math.Max(0, Math.Min(WORLD_HEIGHT, (int)(v.Y * sy + 0.5f)));
                        v.Z = Math.Max(0, Math.Min(WORLD_DEPTH, (int)(v.Z * sz + 0.5f)));
                    }

                    pictureBoxRight.Invalidate();
                    DrawMirrorImage();
                    RefreshPreview();
                }
            }
        }

        private void moveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var moveForm = new FormMove())
            {
                if (moveForm.ShowDialog(this) == DialogResult.OK)
                {
                    SaveUndoState();

                    int sx = moveForm.MoveX;
                    int sy = moveForm.MoveY;
                    int sz = moveForm.MoveZ;

                    foreach (var v in vertices)
                    {
                        if (v.X == -1 && v.Y == -1) continue;

                        v.X = Math.Max(0, Math.Min(WORLD_WIDTH, (v.X + sx)));
                        v.Y = Math.Max(0, Math.Min(WORLD_HEIGHT, (v.Y + sy)));
                        v.Z = Math.Max(0, Math.Min(WORLD_DEPTH, (v.Z + sz)));
                    }

                    pictureBoxRight.Invalidate();
                    DrawMirrorImage();
                    RefreshPreview();
                }
            }
        }

        private void pictureBoxLeft_Paint(object sender, PaintEventArgs e)
        {

            DrawMirrorImage(e.Graphics);
            pictureBoxLeft.Paint -= pictureBoxLeft_Paint;
        }

        private void undoCtrlZToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PerformUndo();
        }

        private void deleteTriangleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "To delete a triangle, hover the mouse over it and press Shift + Delete.",
                "Delete Triangle",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isModified)
            {
                DialogResult result = MessageBox.Show(
                    "You have unsaved changes. Do you want to close without saving?",
                    "Unsaved Changes",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.No)
                {
                    e.Cancel = true;  // 閉じるのをキャンセル
                }
            }
        }

        private void zoomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            zoomToolStripMenuItem.Checked = !zoomToolStripMenuItem.Checked;
            int nextZoomRate = zoomToolStripMenuItem.Checked ? 2 : -zoomRate;
            SetZoomMode(nextZoomRate);
        }

        private void SetZoomMode(int nextZoomRate)
        {
            if ((0 < nextZoomRate && zoomRateMax <= zoomRate) || (nextZoomRate < 0 && zoomRate <= 1))
            {
                return;
            }

            int nextZoomRateAbs = -nextZoomRate;
            zoomRate = (0 < nextZoomRate) ? Math.Min(zoomRate * nextZoomRate, zoomRateMax) : Math.Max(1, zoomRate / nextZoomRateAbs);
            bool isZoomed = (1 < zoomRate);

            // 対象コントロール一覧
            var controls = new List<Control> { pictureBoxLeft, pictureBoxRight };
            controls.AddRange(xAxisLabels);
            controls.AddRange(yAxisLabels);

            if (nextZoomRate > 0)
            {
                // Zoom Up
                foreach (var ctrl in controls)
                {
                    ctrl.Location = new Point(ctrl.Location.X * nextZoomRate, ctrl.Location.Y * nextZoomRate);

                    if (ctrl is PictureBox)
                    {
                        ctrl.Size = new Size(ctrl.Size.Width * nextZoomRate, ctrl.Size.Height * nextZoomRate);
                    }
                }
                panelZoom.Size = new Size(panelZoom.Size.Width * nextZoomRate, panelZoom.Size.Height * nextZoomRate);
            }
            else
            {
                // Zoom Down
                foreach (var ctrl in controls)
                {
                    ctrl.Location = new Point(ctrl.Location.X / nextZoomRateAbs, ctrl.Location.Y / nextZoomRateAbs);
                    if (ctrl is PictureBox)
                    {
                        ctrl.Size = new Size(ctrl.Size.Width / nextZoomRateAbs, ctrl.Size.Height / nextZoomRateAbs);
                    }
                }
                panelZoom.Size = new Size(panelZoom.Size.Width / nextZoomRateAbs, panelZoom.Size.Height / nextZoomRateAbs);
            }

            if (isZoomed)
            {
                hScrollBarZoom.Maximum = panelZoom.Width - panelParent.Width;
                vScrollBarZoom.Maximum = panelZoom.Height - panelParent.Height;
                hScrollBarZoom.Value = pictureBoxLeft.Width;
                vScrollBarZoom.Value = vScrollBarZoom.Maximum / 2;
                panelZoom.Top = -vScrollBarZoom.Value;
                panelZoom.Left = -hScrollBarZoom.Value;
            }
            else
            {
                panelZoom.Location = new Point(0, 0);
            }
            hScrollBarZoom.Visible = isZoomed;
            vScrollBarZoom.Visible = isZoomed;
            pictureBoxRight.Invalidate();
            DrawMirrorImage();
        }

        private void vScrollBarZoom_Scroll(object sender, ScrollEventArgs e)
        {
            panelZoom.Top = -vScrollBarZoom.Value;
            DrawMirrorImage();
        }

        private void hScrollBarZoom_Scroll(object sender, ScrollEventArgs e)
        {
            panelZoom.Left = -hScrollBarZoom.Value;
            DrawMirrorImage();
        }
    }

    // 以下、クラス定義（変更なし）
    public class Vertex
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public Vertex(int x, int y, int z = 0)
        {
            X = x; Y = y; Z = z;
        }
    }

    public class Triangle
    {
        public int V1 { get; set; }
        public int V2 { get; set; }
        public int V3 { get; set; }

        public Triangle(int v1, int v2, int v3)
        {
            V1 = v1; V2 = v2; V3 = v3;
        }
    }

    public class MaskBuilderFileData
    {
        public string Application { get; set; }
        public List<VertexData> Vertices { get; set; }
        public List<TriangleData> Triangles { get; set; }
    }

    public class VertexData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
    }

    public class TriangleData
    {
        public int V1 { get; set; }
        public int V2 { get; set; }
        public int V3 { get; set; }
    }

    public static class ControlExtensions
    {
        public static Label Clone(this Label source)
        {
            Label lbl = new Label();
            lbl.AutoSize = source.AutoSize;
            lbl.Size = source.Size;
            lbl.Font = source.Font;
            lbl.ForeColor = source.ForeColor;
            lbl.BackColor = source.BackColor;
            lbl.TextAlign = source.TextAlign;
            lbl.BorderStyle = source.BorderStyle;
            lbl.Padding = source.Padding;
            lbl.Margin = source.Margin;
            lbl.Visible = source.Visible;
            lbl.Enabled = source.Enabled;
            lbl.Tag = source.Tag;
            return lbl;
        }
    }
}