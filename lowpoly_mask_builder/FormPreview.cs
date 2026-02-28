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
        private List<Triangle> triangles = new List<Triangle>(); private float zoom = 0.8f;
        private Quaternion rotation = Quaternion.Identity;
        private Point lastPos;
        private float panX = 0.0f;   // 左右移動
        private float panY = 0.0f;   // 上下移動
        private Vector3 arcballStartVec;
        private Quaternion startRotation;

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
            glControl1.MouseUp += glControl1_MouseUp;
        }

        private void glControl1_Load(object sender, EventArgs e)
        {
            GL.ClearColor(Color.Gray);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            GL.FrontFace(FrontFaceDirection.Ccw);

            GL.Enable(EnableCap.Lighting);
            GL.Enable(EnableCap.Light0);
            GL.Enable(EnableCap.Light1);
            GL.Enable(EnableCap.ColorMaterial);           // 頂点色をそのまま材質に使う
            GL.ColorMaterial(MaterialFace.FrontAndBack, ColorMaterialParameter.AmbientAndDiffuse);

            // ライトの位置（斜め上から照らす＝顔が一番きれいに見える）
            float[] lightPos = { 150f, 200f, 400f, 0f };  // (x, y, z, w=0=方向光)
            float[] lightDiffuse = { 1.0f, 1.0f, 1.0f, 1.0f };   // 白い光
            float[] lightAmbient = { 0.35f, 0.35f, 0.4f, 1.0f }; // ちょっと環境光

            GL.Light(LightName.Light0, LightParameter.Position, lightPos);
            GL.Light(LightName.Light0, LightParameter.Diffuse, lightDiffuse);
            GL.Light(LightName.Light0, LightParameter.Ambient, lightAmbient);

            GL.Enable(EnableCap.Light1);
            float[] lightPos2 = { -200f, 150f, 300f, 0f };
            GL.Light(LightName.Light1, LightParameter.Position, lightPos2);
            GL.Light(LightName.Light1, LightParameter.Diffuse, new float[] { 0.6f, 0.6f, 0.8f, 1f });

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

            // Light0 の位置設定を削除（Loadで設定済みのため）
            // GL.Light(LightName.Light0, LightParameter.Position, new float[] { 100f, 200f, 400f, 0f }); 

            // カメラ操作
            GL.Translate(panX, panY, -500.0f * zoom);
            Matrix4 rotMatrix = Matrix4.CreateFromQuaternion(rotation);
            GL.MultMatrix(ref rotMatrix);
            GL.Translate(0.0f, -150.0f, 0.0f);     // モデル中心を原点に（Zも少し下げる）

            double bright = 0.6;

            foreach (var t in triangles)
            {
                var a = vertices[t.V1];
                var b = vertices[t.V2];
                var c = vertices[t.V3];

                // --- 1. 面の法線ベクトルを計算 ---
                Vector3 v_ab = new Vector3(b.X - a.X, b.Y - a.Y, b.Z - a.Z);
                Vector3 v_ac = new Vector3(c.X - a.X, c.Y - a.Y, c.Z - a.Z);
                Vector3 normal = Vector3.Cross(v_ab, v_ac);
                normal.Normalize();

                GL.Begin(PrimitiveType.Triangles);

                // --- 表面 (法線設定を追加) ---
                GL.Normal3(normal); // 法線ベクトルを設定
                GL.Color3(0.78 * bright, 0.78 * bright, 0.7 * bright);
                GL.Vertex3(a.X, a.Y, a.Z);
                GL.Vertex3(b.X, b.Y, b.Z);
                GL.Vertex3(c.X, c.Y, c.Z);

                // --- 裏面 (法線設定を追加) ---
                GL.Normal3(-normal); // 裏面なので法線を反転
                GL.Color3(1.0, 0.3, 0.3);
                GL.Vertex3(c.X, c.Y, c.Z);
                GL.Vertex3(b.X, b.Y, b.Z);
                GL.Vertex3(a.X, a.Y, a.Z);

                // --- 表面（-Xミラー）---
                Vector3 normal_mirrored_correct = new Vector3(
                    -normal.X, // X座標を反転させているので、法線のX成分も反転
                    normal.Y,
                    normal.Z);

                GL.Normal3(normal_mirrored_correct); // 法線ベクトルを設定
                GL.Color3(0.78 * bright, 0.78 * bright, 0.7 * bright);
                GL.Vertex3(-c.X, c.Y, c.Z);
                GL.Vertex3(-b.X, b.Y, b.Z);
                GL.Vertex3(-a.X, a.Y, a.Z);

                // --- 裏面（-Xミラー）---
                GL.Normal3(-normal_mirrored_correct); // 裏面なので法線を反転
                GL.Color3(1.0, 0.3, 0.3);
                GL.Vertex3(-a.X, a.Y, a.Z);
                GL.Vertex3(-b.X, b.Y, b.Z);
                GL.Vertex3(-c.X, c.Y, c.Z);
                GL.End();
            }

            // --- ワイヤーフレーム描画（法線は不要）---
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Disable(EnableCap.Lighting); // ワイヤーフレーム描画中はライティングを無効化
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

            GL.Enable(EnableCap.Lighting); // 塗りつぶし描画に戻す前にライティングを有効に戻す
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

            glControl1.SwapBuffers();
        }

        private Vector3 GetArcballVector(int x, int y)
        {
            float px = 2.0f * (float)x / glControl1.Width - 1.0f;
            float py = 1.0f - 2.0f * (float)y / glControl1.Height;
            float len2 = px * px + py * py;
            Vector3 p;
            if (len2 <= 1.0f)
            {
                p = new Vector3(px, py, (float)Math.Sqrt(1.0f - len2));
            }
            else
            {
                float len = (float)Math.Sqrt(len2);
                p = new Vector3(px / len, py / len, 0.0f);
            }
            return p;
        }

        private void glControl1_MouseWheel(object sender, MouseEventArgs e)
        {
            zoom *= e.Delta > 0 ? 0.9f : 1.11f;
            zoom = Math.Max(0.1f, Math.Min(20.0f, zoom));
            glControl1.Invalidate();
        }

        private void glControl1_MouseDown(object sender, MouseEventArgs e)
        {
            lastPos = e.Location;
            if (e.Button == MouseButtons.Left)
            {
                arcballStartVec = GetArcballVector(e.X, e.Y);
                startRotation = rotation;
            }
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                glControl1.Capture = true;  // マウスキャプチャ（ウィンドウ外でも追従）
            }
        }

        private void glControl1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // ファイルダイアログダブルクリックでマウスイベントをご検知する場合がある。
                if (arcballStartVec == Vector3.Zero) {return; }

                Vector3 currVec = GetArcballVector(e.X, e.Y);
                float dot = Vector3.Dot(arcballStartVec, currVec);
                dot = Math.Max(-1.0f, Math.Min(1.0f, dot));
                float angle = (float)Math.Acos(dot);
                if (angle < 1e-6f) return;

                Vector3 axis = Vector3.Cross(arcballStartVec, currVec);
                axis.Normalize();

                // 感度調整
                angle *= 2.0f;

                // 回転方向の調整（直感的でない場合、angle = -angle）
                // angle = -angle; // 必要に応じて反転

                Quaternion delta = Quaternion.FromAxisAngle(axis, angle);
                rotation = delta * startRotation;
                rotation.Normalize(); // 累積誤差防止

                glControl1.Invalidate();
            }
            else if (e.Button == MouseButtons.Right)
            {
                // 右ドラッグ＝パン（水平移動）
                int dx = e.X - lastPos.X;
                int dy = e.Y - lastPos.Y;

                // 感度調整（カメラ距離に応じて自然に動くように）
                float sensitivity = 0.5f * zoom;

                panX += dx * sensitivity;
                panY -= dy * sensitivity;  // Yは画面下が正なので反転

                lastPos = e.Location;
                glControl1.Invalidate();
            }
        }

        private void glControl1_MouseUp(object sender, MouseEventArgs e)
        {
            glControl1.Capture = false;
        }

        // Form1 から呼ばれる更新メソッド
        public void UpdateModel(List<Vertex> vList, List<Triangle> tList)
        {
            vertices.Clear();
            triangles.Clear();

            foreach (var v in vList) vertices.Add(new Vertex(v.X, v.Y, v.Z));
            foreach (var t in tList) triangles.Add(new Triangle(t.V1, t.V2, t.V3));

            if (glControl1.InvokeRequired)
            {
                glControl1.BeginInvoke((MethodInvoker)glControl1.Invalidate);
            }
            else
            {
                glControl1.Invalidate();
            }
        }
    }
}

