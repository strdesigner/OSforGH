using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Display;
using Rhino.Geometry;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
///****************************************
using System.Diagnostics;

using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp;

namespace OpenSeesUtility
{
    public class SolidBase : GH_Component
    {
        public static int on_off_1 = 0; public static int on_off_2 = 0; static int on_off = 0;
        public static PdfCreate.JapaneseFontResolver fontresolver = new PdfCreate.JapaneseFontResolver();
        public static XGraphics gfx;
        public static void SetButton(string s, int i)
        {
            if (s == "c1")
            {
                on_off_1 = i;
            }
            else if (s == "c2")
            {
                on_off_2 = i;
            }
            else if (s == "1")
            {
                on_off = i;
            }
        }
        public SolidBase()
          : base("Calc pressure load N/A+M/Z of solid base", "SolidBase",
              "Calc pressure load N/A+M/Z of solid base",
              "OpenSees", "Analysis")
        {
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("reaction force", "reac_f", "[[node No.,X,Y,Z,MX,MY,MZ],...](DataTree) 0-1 variable", GH_ParamAccess.tree);///
            pManager.AddBrepParameter("S", "S", "surface of solid base shape", GH_ParamAccess.item);
            pManager.AddPointParameter("C", "C", "center of gravity", GH_ParamAccess.item);
            pManager.AddNumberParameter("A", "A", "get area of the closed curve", GH_ParamAccess.item);
            pManager.AddNumberParameter("Zx1", "Zx1", "get cross-sectional coefficient around global x axis(top)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Zx2", "Zx2", "get cross-sectional coefficient around global x axis(bottom)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Zy1", "Zy1", "get cross-sectional coefficient around global y axis(right)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Zy2", "Zy2", "get cross-sectional coefficient around global y axis(left)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Pa", "Pa", "long term allowable pressure load[kN/m2]", GH_ParamAccess.item, 20);
            pManager.AddTextParameter("outputname", "outputname", "output file name", GH_ParamAccess.item, "SolidBaseCheck");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("C1", "C1", "centroid", GH_ParamAccess.item);
            pManager.AddPointParameter("C2", "C2", "center of gravity", GH_ParamAccess.item);
            pManager.AddNumberParameter("N", "N", "sum of axial loads", GH_ParamAccess.item);
            pManager.AddNumberParameter("MX", "MX", "eccentric bending moment around global x axis", GH_ParamAccess.item);
            pManager.AddNumberParameter("MY", "MY", "eccentric bending moment around global y axis", GH_ParamAccess.item);
            pManager.AddNumberParameter("N/A+M/Z", "N/A+M/Z", "pressure load", GH_ParamAccess.item);
            pManager.AddNumberParameter("N/A-M/Z", "N/A-M/Z", "pressure load", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r); var r = _r.Branches;
            DA.GetDataTree("reaction force", out GH_Structure<GH_Number> _reac_f); var reac_f = _reac_f.Branches;
            var C = new Point3d(); DA.GetData("C", ref C); var A = 0.0; DA.GetData("A", ref A);
            var Zx1 = 0.0; DA.GetData("Zx1", ref Zx1); var Zx2 = 0.0; DA.GetData("Zx2", ref Zx2); var Zy1 = 0.0; DA.GetData("Zy1", ref Zy1); var Zy2 = 0.0; DA.GetData("Zy2", ref Zy2);
            var Zx = Zx1; var Zy = Zy1;
            Brep surf = new Brep(); DA.GetData("S", ref surf); var face = surf.Faces[0];
            var solid = Brep.CreateFromOffsetFace(face, 0.1, 5e-3, true, true); var N = 0.0; var NX = 0.0; var NY = 0.0;
            var Nlist = new List<string>(); var Xlist = new List<string>(); var Ylist = new List<string>(); var nlist = new List<string>();
            for (int e = 0; e < reac_f.Count; e++)
            {
                int i = (int)reac_f[e][0].Value;
                var p = new Point3d(r[i][0].Value, r[i][1].Value, r[i][2].Value);
                if (solid.IsPointInside(p, 5e-3, false) == true)
                {
                    N += reac_f[e][3].Value; NX += reac_f[e][3].Value * p[0]; NY += reac_f[e][3].Value * p[1];
                    if (on_off == 1)
                    {
                        nlist.Add(((int)reac_f[e][0].Value).ToString()); Nlist.Add(Math.Round(reac_f[e][3].Value, 2).ToString()); Xlist.Add(Math.Round(p[0], 2).ToString()); Ylist.Add(Math.Round(p[1], 2).ToString());
                    }
                }
            }
            var X = NX / N; var Y = NY / N; var CX = C[0]; var CY = C[1];
            var C2 = new Point3d(X, Y, C[2]);
            if (X < CX) { Zy = Zy2; }
            if (Y < CY) { Zx = Zx2; }
            var ex = Math.Abs(X - CX); var ey = Math.Abs(Y - CY);
            var MY = N * ex; var MX = N * ey;
            DA.SetData("C1", C); DA.SetData("C2", C2);
            var M_Z = Math.Sqrt(Math.Pow(MX / Zx, 2) + Math.Pow(MY / Zy, 2)); var N_A = N / A;
            DA.SetData("N", N); DA.SetData("MX", MX); DA.SetData("MY", MY);
            DA.SetData("N/A+M/Z", N_A + M_Z); DA.SetData("N/A-M/Z", N_A - M_Z);
            if (on_off_1 == 1) { _p1.Add(C); }
            if (on_off_2 == 1) { _p2.Add(C2); }
            if (on_off == 1)
            {
                var Pa = 0.0; DA.GetData("Pa", ref Pa);
                var pdfname = "SolidBaseCheck"; DA.GetData("outputname", ref pdfname);
                // フォントリゾルバーのグローバル登録
                if (PdfCreate.JapaneseFontResolver.fontset == 0) { PdfSharp.Fonts.GlobalFontSettings.FontResolver = fontresolver; PdfCreate.JapaneseFontResolver.fontset = 1; }
                // PDFドキュメントを作成。
                PdfDocument document = new PdfDocument();
                document.Info.Title = pdfname;
                document.Info.Author = "Shinnosuke Fujita, Assoc. Prof., The Univ. of Kitakyushu";
                // フォントを作成。
                XFont font = new XFont("Gen Shin Gothic", 8, XFontStyle.Regular);
                XFont fontbold = new XFont("Gen Shin Gothic", 8, XFontStyle.Bold);
                var pen = XPens.Black;
                var label1 = new List<string>(); var label2 = new List<string>(); var label3 = new List<string>(); var label4 = new List<string>();
                label1.Add("節点番号"); label2.Add("X[m]"); label3.Add("Y[m]"); label4.Add("軸力N[kN]");
                for (int e = 0; e < Nlist.Count; e++)
                {
                    label1.Add(nlist[e]); label2.Add(Xlist[e]); label3.Add(Ylist[e]); label4.Add(Nlist[e]);
                }
                label1.Add("∑N[kN]"); label3.Add(Math.Round(N,0).ToString()); label2.Add(""); label4.Add("");
                label1.Add("A[m2]"); label3.Add(Math.Round(A, 2).ToString()); label2.Add(""); label4.Add("");
                label1.Add("N/A[kN/m2]"); label3.Add(Math.Round(N_A, 2).ToString()); label2.Add(""); label4.Add("");
                label1.Add("重心座標X[m]"); label2.Add("重心座標Y[m]"); label3.Add(Math.Round(X, 2).ToString()); label4.Add(Math.Round(Y, 2).ToString());
                label1.Add("図心座標X[m]"); label2.Add("図心座標Y[m]"); label3.Add(Math.Round(CX, 2).ToString()); label4.Add(Math.Round(CY, 2).ToString());
                label1.Add("偏心距離ex[m]"); label2.Add("偏心距離ey[m]"); label3.Add(Math.Round(ex, 2).ToString()); label4.Add(Math.Round(ey, 2).ToString());
                label1.Add("偏心Mx[kNm]"); label2.Add("偏心My[kNm]"); label3.Add(Math.Round(MX, 2).ToString()); label4.Add(Math.Round(MY, 2).ToString());
                label1.Add("断面係数Zx[m3]"); label2.Add("断面係数Zy[m3]"); label3.Add(Math.Round(Zx, 2).ToString()); label4.Add(Math.Round(Zy, 2).ToString());
                label1.Add("N/A+M/Z[kN/m2]"); label2.Add("N/A-M/Z[kN/m2]"); label3.Add(Math.Round(N_A + M_Z, 2).ToString()); label4.Add(Math.Round(N_A - M_Z, 2).ToString());
                label1.Add("長期地耐力[kN/m2]"); label2.Add("検定比"); label3.Add(Math.Round(Pa, 2).ToString());
                if ((N_A + M_Z) / Pa <= 1) { label4.Add(Math.Round((N_A + M_Z) / Pa, 2).ToString() + ":O.K."); }
                else { label4.Add(Math.Round((N_A + M_Z) / Pa, 2).ToString() + ":N.G."); }
                var label_width = 100; var offset_x = 25; var offset_y = 25; var pitchy = 13; PdfPage page = new PdfPage(); page.Size = PageSize.A4;
                for (int i = 0; i<label1.Count; i++)
                {
                    var color1 = XBrushes.Black; var color2 = XBrushes.Black; var color3 = XBrushes.Black; var color4 = XBrushes.Black;
                    if (i % 50 == 0)
                    {
                        page = document.AddPage();// 空白ページを作成。
                        gfx = XGraphics.FromPdfPage(page);// 描画するためにXGraphicsオブジェクトを取得。
                        if (i > reac_f.Count && i < reac_f.Count + 4)
                        {
                            gfx.DrawLine(pen, offset_x, offset_y + pitchy * i, offset_x + label_width * 4, offset_y + pitchy * i);//横線
                            gfx.DrawLine(pen, offset_x + label_width * 0, offset_y + pitchy * i, offset_x + label_width * 0, offset_y + pitchy * (i + 1));//縦線
                            gfx.DrawLine(pen, offset_x + label_width * 1, offset_y + pitchy * i, offset_x + label_width * 1, offset_y + pitchy * (i + 1));//縦線
                            gfx.DrawLine(pen, offset_x + label_width * 4, offset_y + pitchy * i, offset_x + label_width * 4, offset_y + pitchy * (i + 1));//縦線
                        }
                        else if (i > 1 && i < reac_f.Count + 1)
                        {
                            gfx.DrawLine(pen, offset_x + label_width * 0, offset_y + pitchy * i, offset_x + label_width * 0, offset_y + pitchy * (i + 1));//縦線
                            gfx.DrawLine(pen, offset_x + label_width * 1, offset_y + pitchy * i, offset_x + label_width * 1, offset_y + pitchy * (i + 1));//縦線
                            gfx.DrawLine(pen, offset_x + label_width * 2, offset_y + pitchy * i, offset_x + label_width * 2, offset_y + pitchy * (i + 1));//縦線
                            gfx.DrawLine(pen, offset_x + label_width * 3, offset_y + pitchy * i, offset_x + label_width * 3, offset_y + pitchy * (i + 1));//縦線
                            gfx.DrawLine(pen, offset_x + label_width * 4, offset_y + pitchy * i, offset_x + label_width * 4, offset_y + pitchy * (i + 1));//縦線
                        }
                        else
                        {
                            gfx.DrawLine(pen, offset_x, offset_y + pitchy * i, offset_x + label_width * 4, offset_y + pitchy * i);//横線
                            gfx.DrawLine(pen, offset_x + label_width * 0, offset_y + pitchy * i, offset_x + label_width * 0, offset_y + pitchy * (i + 1));//縦線
                            gfx.DrawLine(pen, offset_x + label_width * 1, offset_y + pitchy * i, offset_x + label_width * 1, offset_y + pitchy * (i + 1));//縦線
                            gfx.DrawLine(pen, offset_x + label_width * 2, offset_y + pitchy * i, offset_x + label_width * 2, offset_y + pitchy * (i + 1));//縦線
                            gfx.DrawLine(pen, offset_x + label_width * 3, offset_y + pitchy * i, offset_x + label_width * 3, offset_y + pitchy * (i + 1));//縦線
                            gfx.DrawLine(pen, offset_x + label_width * 4, offset_y + pitchy * i, offset_x + label_width * 4, offset_y + pitchy * (i + 1));//縦線
                        }
                    }
                    gfx.DrawString(label1[i], font, color1, new XRect(offset_x + label_width * 0, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                    gfx.DrawString(label2[i], font, color1, new XRect(offset_x + label_width * 1, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                    gfx.DrawString(label3[i], font, color1, new XRect(offset_x + label_width * 2, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                    gfx.DrawString(label4[i], font, color1, new XRect(offset_x + label_width * 3, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                    if (i > reac_f.Count && i < reac_f.Count + 4)
                    {
                        gfx.DrawLine(pen, offset_x, offset_y + pitchy * i, offset_x + label_width * 4, offset_y + pitchy * i);//横線
                        gfx.DrawLine(pen, offset_x + label_width * 0, offset_y + pitchy * i, offset_x + label_width * 0, offset_y + pitchy * (i + 1));//縦線
                        gfx.DrawLine(pen, offset_x + label_width * 1, offset_y + pitchy * i, offset_x + label_width * 1, offset_y + pitchy * (i + 1));//縦線
                        gfx.DrawLine(pen, offset_x + label_width * 4, offset_y + pitchy * i, offset_x + label_width * 4, offset_y + pitchy * (i + 1));//縦線
                    }
                    else if (i > 1 && i < reac_f.Count + 1)
                    {
                        gfx.DrawLine(pen, offset_x + label_width * 0, offset_y + pitchy * i, offset_x + label_width * 0, offset_y + pitchy * (i + 1));//縦線
                        gfx.DrawLine(pen, offset_x + label_width * 1, offset_y + pitchy * i, offset_x + label_width * 1, offset_y + pitchy * (i + 1));//縦線
                        gfx.DrawLine(pen, offset_x + label_width * 2, offset_y + pitchy * i, offset_x + label_width * 2, offset_y + pitchy * (i + 1));//縦線
                        gfx.DrawLine(pen, offset_x + label_width * 3, offset_y + pitchy * i, offset_x + label_width * 3, offset_y + pitchy * (i + 1));//縦線
                        gfx.DrawLine(pen, offset_x + label_width * 4, offset_y + pitchy * i, offset_x + label_width * 4, offset_y + pitchy * (i + 1));//縦線
                    }
                    else
                    {
                        gfx.DrawLine(pen, offset_x, offset_y + pitchy * i, offset_x + label_width * 4, offset_y + pitchy * i);//横線
                        gfx.DrawLine(pen, offset_x + label_width * 0, offset_y + pitchy * i, offset_x + label_width * 0, offset_y + pitchy * (i + 1));//縦線
                        gfx.DrawLine(pen, offset_x + label_width * 1, offset_y + pitchy * i, offset_x + label_width * 1, offset_y + pitchy * (i + 1));//縦線
                        gfx.DrawLine(pen, offset_x + label_width * 2, offset_y + pitchy * i, offset_x + label_width * 2, offset_y + pitchy * (i + 1));//縦線
                        gfx.DrawLine(pen, offset_x + label_width * 3, offset_y + pitchy * i, offset_x + label_width * 3, offset_y + pitchy * (i + 1));//縦線
                        gfx.DrawLine(pen, offset_x + label_width * 4, offset_y + pitchy * i, offset_x + label_width * 4, offset_y + pitchy * (i + 1));//縦線
                    }
                    if (i == label1.Count - 1)
                    {
                        gfx.DrawLine(pen, offset_x, offset_y + pitchy * (i + 1), offset_x + label_width * 4, offset_y + pitchy * (i + 1));//横線
                    }
                }
                var dir = Path.GetDirectoryName(Rhino.RhinoDoc.ActiveDoc.Path);
                // ドキュメントを保存。
                var filename = dir + "/" + pdfname + ".pdf";
                document.Save(filename);
                // ビューアを起動。
                Process.Start(filename);
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return OpenSeesUtility.Properties.Resources.solidbase;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("a391da3a-bf0f-4c76-92ee-a62c86e3dca2"); }
        }
        ///ここからカスタム関数群********************************************************************************
        private readonly List<Point3d> _p1 = new List<Point3d>();
        private readonly List<Point3d> _p2 = new List<Point3d>();
        protected override void BeforeSolveInstance()
        {
            _p1.Clear();
            _p2.Clear();
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            args.Viewport.GetFrustumFarPlane(out Plane plane);
            RhinoViewport viewport = args.Viewport;
            ///重心の描画用関数*********************************************************************************
            for (int i = 0; i < _p1.Count; i++)
            {
                Point3d point = _p1[i];
                args.Display.DrawPoint(point, PointStyle.Square, 3, Color.Red);
            }
            ///図心の描画用関数*********************************************************************************
            for (int i = 0; i < _p2.Count; i++)
            {
                Point3d point = _p2[i];
                args.Display.DrawPoint(point, PointStyle.Circle, 3, Color.Blue);
            }
        }
        public class CustomGUI : GH_ComponentAttributes
        {
            public CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle title_rec;
            private Rectangle radio_rec; private Rectangle radio_rec2;
            private Rectangle radio_rec_1; private Rectangle text_rec_1;
            private Rectangle radio_rec_2; private Rectangle text_rec_2;
            private Rectangle radio_rec2_1; private Rectangle text_rec2_1;
            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int radi1 = 7; int radi2 = 4;
                int pitchx = 8; int pitchy = 11; int textheight = 20;
                int width = global_rec.Width;
                title_rec = global_rec;
                title_rec.Y = title_rec.Bottom;
                title_rec.Height = 22;

                radio_rec = title_rec;
                radio_rec.Y += title_rec.Height;

                radio_rec_1 = radio_rec;
                radio_rec_1.X += 5; radio_rec_1.Y += 5;
                radio_rec_1.Height = radi1; radio_rec_1.Width = radi1;

                text_rec_1 = radio_rec_1;
                text_rec_1.X += pitchx; text_rec_1.Y -= radi2;
                text_rec_1.Height = textheight; text_rec_1.Width = width;

                radio_rec_2 = radio_rec_1; radio_rec_2.Y += pitchy;
                text_rec_2 = radio_rec_2;
                text_rec_2.X += pitchx; text_rec_2.Y -= radi2;
                text_rec_2.Height = textheight; text_rec_2.Width = width;
                
                radio_rec.Height = text_rec_2.Bottom - radio_rec.Y;

                radio_rec2 = radio_rec;
                radio_rec2.Y = radio_rec.Y + radio_rec.Height;
                radio_rec2.Height = textheight;

                radio_rec2_1 = radio_rec2;
                radio_rec2_1.X += 5; radio_rec2_1.Y += 5;
                radio_rec2_1.Height = radi1; radio_rec2_1.Width = radi1;

                text_rec2_1 = radio_rec2_1;
                text_rec2_1.X += pitchx; text_rec2_1.Y -= radi2;
                text_rec2_1.Height = textheight; text_rec2_1.Width = width;

                global_rec.Height += (radio_rec2_1.Bottom - global_rec.Bottom);
                Bounds = global_rec;
            }
            Brush c1 = Brushes.White; Brush c2 = Brushes.White; Brush c3 = Brushes.White;
            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
            {
                base.Render(canvas, graphics, channel);
                if (channel == GH_CanvasChannel.Objects)
                {
                    GH_Capsule title = GH_Capsule.CreateCapsule(title_rec, GH_Palette.Pink, 2, 0);
                    title.Render(graphics, Selected, Owner.Locked, false);
                    title.Dispose();

                    StringFormat format = new StringFormat();
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    format.Trimming = StringTrimming.EllipsisCharacter;

                    RectangleF textRectangle = title_rec;
                    textRectangle.Height = 20;
                    graphics.DrawString("Display Option", GH_FontServer.Standard, Brushes.White, textRectangle, format);

                    GH_Capsule radio = GH_Capsule.CreateCapsule(radio_rec, GH_Palette.White, 2, 0);
                    radio.Render(graphics, Selected, Owner.Locked, false); radio.Dispose();

                    GH_Capsule radio_1 = GH_Capsule.CreateCapsule(radio_rec_1, GH_Palette.Black, 5, 5);
                    radio_1.Render(graphics, Selected, Owner.Locked, false); radio_1.Dispose();
                    graphics.FillEllipse(c1, radio_rec_1);
                    graphics.DrawString("Centroid", GH_FontServer.Standard, Brushes.Black, text_rec_1);

                    GH_Capsule radio_2 = GH_Capsule.CreateCapsule(radio_rec_2, GH_Palette.Black, 5, 5);
                    radio_2.Render(graphics, Selected, Owner.Locked, false); radio_2.Dispose();
                    graphics.FillEllipse(c2, radio_rec_2);
                    graphics.DrawString("Center of Gravity", GH_FontServer.Standard, Brushes.Black, text_rec_2);

                    GH_Capsule radio2 = GH_Capsule.CreateCapsule(radio_rec2, GH_Palette.White, 2, 0);
                    radio2.Render(graphics, Selected, Owner.Locked, false); radio2.Dispose();

                    GH_Capsule radio2_1 = GH_Capsule.CreateCapsule(radio_rec2_1, GH_Palette.Black, 5, 5);
                    radio2_1.Render(graphics, Selected, Owner.Locked, false); radio2_1.Dispose();
                    graphics.FillEllipse(c3, radio_rec2_1);
                    graphics.DrawString("PDF OUTPUT", GH_FontServer.Standard, Brushes.Black, text_rec2_1);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec1 = radio_rec_1; RectangleF rec2 = radio_rec_2; RectangleF rec3 = radio_rec2_1;
                    if (rec1.Contains(e.CanvasLocation))
                    {
                        if (c1 == Brushes.White) { c1 = Brushes.Black; SetButton("c1", 1); }
                        else { c1 = Brushes.White; SetButton("c1", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec2.Contains(e.CanvasLocation))
                    {
                        if (c2 == Brushes.White) { c2 = Brushes.Black; SetButton("c2", 1); }
                        else { c2 = Brushes.White; SetButton("c2", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec3.Contains(e.CanvasLocation))
                    {
                        if (c3 == Brushes.Black) { c3 = Brushes.White; SetButton("1", 0); }
                        else
                        { c3 = Brushes.Black; SetButton("1", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}