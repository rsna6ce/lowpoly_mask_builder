// FormPreview.cs（最終版：Glu不要・OpenTK 3.3.3 完全対応）
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace lowpoly_mask_builder
{
    public partial class FormPreview : Form
    {
        private List<Vertex> vertices = new List<Vertex>();
        private List<Triangle> triangles = new List<Triangle>();

        private float zoom = 0.8f;
        private float rotX = 0f;
        private float rotY = 0f;
        private Point lastPos;

        public FormPreview()
        {
            InitializeComponent();

            glControl1.VSync = false;
            glControl1.Load += glControl1_Load;
            glControl1.Paint += glControl1_Paint;
            glControl1.Resize += glControl1_Resize;
            glControl1.MouseWheel += glControl1_MouseWheel;
            glControl1.MouseDown += glControl1_MouseDown;
            glControl1.MouseMove += glControl1_MouseMove;
        }

        private void glControl1_Load(object sender, EventArgs e)
        {
            GL.ClearColor(Color.CornflowerBlue);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            GL.FrontFace(FrontFaceDirection.Ccw);

            SetupProjection();
        }

        private void glControl1_Resize(object sender, EventArgs e)
        {
            SetupProjection();
            glControl1.Invalidate();
        }

        // Glu.Perspective の代わり（手動で透視投影行列を作成）
        private void SetupProjection()
        {
            if (glControl1.ClientSize.Height == 0) return;

            GL.Viewport(0, 0, glControl1.ClientSize.Width, glControl1.ClientSize.Height);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();

            float aspect = (float)glControl1.ClientSize.Width / glControl1.ClientSize.Height;
            float fovy = 45.0f;
            float zNear = 0.1f;
            float zFar = 10000.0f;

            float f = 1.0f / (float)Math.Tan(MathHelper.DegreesToRadians(fovy) * 0.5f);

            // 1次元配列で渡す（列優先＝Column-major）
            float[] m = new float[16]
            {
        f / aspect, 0.0f      , 0.0f                          ,  0.0f,
        0.0f      , f         , 0.0f                          ,  0.0f,
        0.0f      , 0.0f      , (zFar + zNear) / (zNear - zFar), -1.0f,
        0.0f      , 0.0f      , (2.0f * zFar * zNear) / (zNear - zFar), 0.0f
            };

            GL.MultMatrix(m);  // これで完璧に通る

            GL.MatrixMode(MatrixMode.Modelview);
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.LoadIdentity();

            // カメラ操作
            GL.Translate(0.0f, 0.0f, -500.0f * zoom);   // ズーム
            GL.Rotate(rotX, 1.0f, 0.0f, 0.0f);           // まず真上から
            GL.Rotate(rotY, 0.0f, 1.0f, 0.0f);           // その後左右に首を振る
            GL.Translate(0.0f, -150.0f, 0.0f);        // モデル中心を原点に（Zも少し下げる）

            foreach (var t in triangles)
            {
                var a = vertices[t.V1];
                var b = vertices[t.V2];
                var c = vertices[t.V3];

                GL.Begin(PrimitiveType.Triangles);

                // 表面（反時計回り）→ 灰色
                GL.Color3(0.7, 0.7, 0.7);
                GL.Vertex3(a.X, a.Y, a.Z);
                GL.Vertex3(b.X, b.Y, b.Z);
                GL.Vertex3(c.X, c.Y, c.Z);

                // 裏面（時計回り）→ 赤
                GL.Color3(1.0, 0.3, 0.3);
                GL.Vertex3(c.X, c.Y, c.Z);
                GL.Vertex3(b.X, b.Y, b.Z);
                GL.Vertex3(a.X, a.Y, a.Z);

                // 表面（反時計回り）→ 灰色
                GL.Color3(0.7, 0.7, 0.7);
                GL.Vertex3(-c.X, c.Y, c.Z);
                GL.Vertex3(-b.X, b.Y, b.Z);
                GL.Vertex3(-a.X, a.Y, a.Z);

                // 裏面（時計回り）→ 赤
                GL.Color3(1.0, 0.3, 0.3);
                GL.Vertex3(-a.X, a.Y, a.Z);
                GL.Vertex3(-b.X, b.Y, b.Z);
                GL.Vertex3(-c.X, c.Y, c.Z);
                GL.End();
            }

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.LineWidth(1.5f);
            GL.Color3(0.15, 0.15, 0.15);

            GL.Begin(PrimitiveType.Triangles);

            foreach (var t in triangles)
            {
                var a = vertices[t.V1];
                var b = vertices[t.V2];
                var c = vertices[t.V3];

                GL.Vertex3(a.X, a.Y, a.Z);
                GL.Vertex3(b.X, b.Y, b.Z);
                GL.Vertex3(c.X, c.Y, c.Z);

                GL.Vertex3(-c.X, c.Y, c.Z);
                GL.Vertex3(-b.X, b.Y, b.Z);
                GL.Vertex3(-a.X, a.Y, a.Z);
            }

            GL.End();

            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            glControl1.SwapBuffers();
        }

        private void glControl1_MouseWheel(object sender, MouseEventArgs e)
        {
            zoom *= e.Delta > 0 ? 0.9f : 1.11f;
            zoom = Math.Max(0.1f, Math.Min(20.0f, zoom));
            glControl1.Invalidate();
        }

        private void glControl1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                lastPos = e.Location;
        }

        private void glControl1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int dx = e.X - lastPos.X;
                int dy = e.Y - lastPos.Y;
                rotY += dx * 0.5f;
                rotX += dy * 0.5f;
                lastPos = e.Location;
                glControl1.Invalidate();
            }
        }

        // Form1 から呼ばれる更新メソッド
        public void UpdateModel(List<Vertex> vList, List<Triangle> tList)
        {
            vertices.Clear();
            triangles.Clear();

            foreach (var v in vList) vertices.Add(new Vertex(v.X, v.Y, v.Z));
            foreach (var t in tList) triangles.Add(new Triangle(t.V1, t.V2, t.V3));

            if (glControl1.InvokeRequired)
                glControl1.BeginInvoke((MethodInvoker)glControl1.Invalidate);
            else
                glControl1.Invalidate();
        }
    }
}