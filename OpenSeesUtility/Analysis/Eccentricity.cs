using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Display;
using System.IO;

using System.Drawing;
using System.Windows.Forms;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Rhino;
///****************************************
using System.Diagnostics;

using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp;

namespace OpenSeesUtility
{
    public class Eccentricity : GH_Component
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
        public Eccentricity()
          : base("Eccentricity", "Eccentricity",
              "Calc Eccentricity",
              "OpenSees", "Analysis")
        {
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("index", "index", "[...](element No. List to show)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("R", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("IJ", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("D", "D", "[[dxi,dyi,dzi,theta_xi,theta_yi,theta_zi,dxj,dyj,dzj,theta_xj,theta_yj,theta_zj],...](DataTree)", GH_ParamAccess.tree, -9999);///2
            pManager.AddNumberParameter("sec_f", "sec_f", "[[element No.,Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree, -9999);///4
            pManager.AddNumberParameter("Iy", "Iy", "[...](Second moment of area around section local y-axis)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("Iz", "Iz", "[...](Second moment of area around section local z-axis)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("E", "E", "[...](Young's modulus)", GH_ParamAccess.list, -9999);///
            pManager.AddTextParameter("outputname", "outputname", "output file name", GH_ParamAccess.item, "Eccentricity");
            pManager.AddIntegerParameter("nrow", "nrow", "number of rows per page", GH_ParamAccess.item, 50);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Kx", "Kx", "Horizontal rigidity in x-direction", GH_ParamAccess.list);
            pManager.AddNumberParameter("Ky", "Ky", "Horizontal rigidity in y-direction", GH_ParamAccess.list);
            pManager.AddPointParameter("C1", "C1", "centroid", GH_ParamAccess.item);
            pManager.AddPointParameter("C2", "C2", "center of gravity", GH_ParamAccess.item);
            pManager.AddNumberParameter("Rex, Rey", "Rex, Rey", "eccentricity for X and Y direction", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<double> index = new List<double>(); DA.GetDataList("index", index);
            var Iy = new List<double>(); DA.GetDataList("Iy", Iy); var Iz = new List<double>(); DA.GetDataList("Iz", Iz); var E = new List<double>(); DA.GetDataList("E", E);
            DA.GetDataTree("R", out GH_Structure<GH_Number> _R); var R = _R.Branches;
            DA.GetDataTree("IJ", out GH_Structure<GH_Number> _IJ); var IJ = _IJ.Branches;
            DA.GetDataTree("D", out GH_Structure<GH_Number> _D); var D = _D.Branches;
            DA.GetDataTree("sec_f", out GH_Structure<GH_Number> _sec_f); var sec_f = _sec_f.Branches;
            var X = new List<double>(); var Y = new List<double>(); var N = new List<double>(); var Qx = new List<double>(); var Qy = new List<double>(); var NX = new List<double>(); var NY = new List<double>(); var Dx = new List<double>(); var Dy = new List<double>();
            var Kx = new List<double>(); var Ky = new List<double>(); var KxY = new List<double>(); var KyX = new List<double>();
            var KxY2 = new List<double>(); var KyX2 = new List<double>();
            if (R[0][0].Value == -9999 || IJ[0][0].Value == -9999) { return; }
            if ((sec_f[0].Count / 18 < 3 || D[0].Count / 12 < 3) && (Iy[0] == -9999 || Iz[0] == -9999 || E[0] == -9999)) { return; }
            for (int i = 0; i < index.Count; i++)
            {
                int e = (int)index[i];
                int ni = (int)IJ[e][0].Value; int nj = (int)IJ[e][1].Value; double theta = IJ[e][4].Value;
                var x = R[ni][0].Value; var y = R[nj][1].Value; var n = sec_f[e][0].Value;
                X.Add(x); Y.Add(y); N.Add(n); NX.Add(n*x); NY.Add(n*y);
                if (D[0].Count / 12 >= 3)
                {
                    var qy = sec_f[e][18 + 1].Value; var qz = sec_f[e][18 + 2].Value;
                    Qx.Add(Math.Abs(qy * Math.Pow(Math.Cos(theta / 180.0 * Math.PI),2)) + Math.Abs(qz * Math.Pow(Math.Cos((90 - theta) / 180.0 * Math.PI),2)));
                    qy = sec_f[e][18 * 2 + 1].Value; qz = sec_f[e][18 * 2 + 2].Value;
                    Qy.Add(Math.Abs(qy * Math.Pow(Math.Sin(theta / 180.0 * Math.PI), 2)) + Math.Abs(qz * Math.Pow(Math.Sin((90 - theta) / 180.0 * Math.PI),2)));
                    var dx = D[e][12 + 6].Value - D[e][12 + 0].Value; var dy = D[e][12 * 2 + 7].Value - D[e][12 * 2 + 1].Value;
                    Dx.Add(dx); Dy.Add(dy);
                    Kx.Add(Qx[i] / dx); Ky.Add(Qy[i] / dy);
                    KyX.Add(Ky[i] * X[i]); KxY.Add(Kx[i] * Y[i]);
                    KyX2.Add(Ky[i] * Math.Pow(X[i],2)); KxY2.Add(Kx[i] * Math.Pow(Y[i],2));
                }
                else
                {
                    int mat = (int)IJ[e][2].Value; int sec = (int)IJ[e][3].Value;
                    var l = Math.Sqrt(Math.Pow(R[ni][0].Value - R[nj][0].Value, 2) + Math.Pow(R[ni][1].Value - R[nj][1].Value, 2) + Math.Pow(R[ni][2].Value - R[nj][2].Value, 2));
                    Kx.Add((Math.Abs(12 * E[mat] * Iy[sec] * Math.Pow(Math.Cos(theta / 180.0 * Math.PI),2)) + Math.Abs(12 * E[mat] * Iz[sec] * Math.Pow(Math.Cos((90-theta) / 180.0 * Math.PI),2))) / Math.Pow(l, 3));
                    Ky.Add((Math.Abs(12 * E[mat] * Iy[sec] * Math.Pow(Math.Sin(theta / 180.0 * Math.PI), 2)) + Math.Abs(12 * E[mat] * Iz[sec] * Math.Pow(Math.Sin((90 - theta) / 180.0 * Math.PI), 2))) / Math.Pow(l, 3));
                    KyX.Add(Ky[i] * X[i]); KxY.Add(Kx[i] * Y[i]);
                }
            }
            DA.SetDataList("Kx", Kx); DA.SetDataList("Ky", Ky);
            var sumN = N.Sum(); var sumNX = NX.Sum(); var sumNY = NY.Sum();
            var C2 = new Point3d(sumNX / sumN, sumNY / sumN, 0);
            var sumKx = Kx.Sum(); var sumKy = Ky.Sum(); var sumKyX = KyX.Sum(); var sumKxY = KxY.Sum(); var sumKR = KyX2.Sum() + KxY2.Sum();
            var C1 = new Point3d(sumKyX / sumKy, sumKxY / sumKx, 0);
            var ex = Math.Abs(C1[0] - C2[0]); var ey = Math.Abs(C1[1] - C2[1]); var rex = Math.Sqrt(sumKR / sumKx); var rey = Math.Sqrt(sumKR / sumKy);
            DA.SetData("C1", C1); if (on_off_1 == 1) { _p1.Add(C1); }
            DA.SetData("C2", C2); if (on_off_2 == 1) { _p2.Add(C2); }
            DA.SetDataList("Rex, Rey", new List<double> { ey / rex, ex / rey });
            if (on_off == 1)
            {
                int nrow = 0; DA.GetData("nrow", ref nrow);
                var pdfname = "Eccentricity"; DA.GetData("outputname", ref pdfname);
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
                var label1 = new List<string>(); var label2 = new List<string>(); var label3 = new List<string>(); var label4 = new List<string>(); var label5 = new List<string>(); var label6 = new List<string>(); var label7 = new List<string>(); var label8 = new List<string>(); var label9 = new List<string>(); var label10 = new List<string>(); var label11 = new List<string>(); var label12 = new List<string>();
                label1.Add("要素番号"); label2.Add("X[m]"); label3.Add("Y[m]"); label4.Add("N[kN]"); label5.Add("Kx[kN/m]"); label6.Add("Ky[kN/m]"); label7.Add("NX[kNm]"); label8.Add("NY[kNm]"); label9.Add("KyX[kN]"); label10.Add("KxY[kN]"); label11.Add("KyX2[kN]"); label12.Add("KxY2[kN]");
                for (int i = 0; i < index.Count; i++)
                {
                    label1.Add(((int)index[i]).ToString()); label2.Add(Math.Round(X[i], 2).ToString()); label3.Add(Math.Round(Y[i], 2).ToString()); label4.Add(Math.Round(N[i], 2).ToString()); label5.Add(Math.Round(Kx[i], 0).ToString()); label6.Add(Math.Round(Ky[i], 0).ToString()); label7.Add(Math.Round(NX[i], 0).ToString()); label8.Add(Math.Round(NY[i], 0).ToString()); label9.Add(Math.Round(KyX[i], 0).ToString()); label10.Add(Math.Round(KxY[i], 0).ToString()); label11.Add(Math.Round(KyX2[i], 0).ToString()); label12.Add(Math.Round(KxY2[i], 0).ToString());
                }
                label1.Add(""); label2.Add("∑"); label3.Add(""); label4.Add(Math.Round(sumN, 0).ToString()); label5.Add(Math.Round(sumKx, 0).ToString()); label6.Add(Math.Round(sumKy, 0).ToString()); label7.Add(Math.Round(sumNX, 0).ToString()); label8.Add(Math.Round(sumNY, 0).ToString()); label9.Add(Math.Round(sumKyX, 0).ToString()); label10.Add(Math.Round(sumKxY, 0).ToString()); label11.Add(Math.Round(KyX2.Sum(), 0).ToString()); label12.Add(Math.Round(KxY2.Sum(), 0).ToString());

                label1.Add("gx[m]"); label2.Add("gy[m]"); label3.Add("lx[m]"); label4.Add("ly[m]"); label5.Add("ex[m]"); label6.Add("ey[m]"); label7.Add("KR[kNm]"); label8.Add(""); label9.Add("rex[m]"); label10.Add("rey[m]"); label11.Add("Rex"); label12.Add("Rey");

                label1.Add(Math.Round(C2[0],2).ToString()); label2.Add(Math.Round(C2[1], 2).ToString()); label3.Add(Math.Round(C1[0], 2).ToString()); label4.Add(Math.Round(C1[1], 2).ToString()); label5.Add(Math.Round(ex, 2).ToString()); label6.Add(Math.Round(ey, 2).ToString()); label7.Add(Math.Round(sumKR, 0).ToString()); label8.Add(""); label9.Add(Math.Round(rex, 2).ToString()); label10.Add(Math.Round(rey, 2).ToString()); label11.Add(Math.Round(ey / rex, 2).ToString()); label12.Add(Math.Round(ex / rey, 2).ToString());
                var label_width = 40; var offset_x = 25; var offset_y = 25; var pitchy = 13; PdfPage page = new PdfPage(); page.Size = PageSize.A4;
                for (int ii = 0; ii < label1.Count; ii++)
                {
                    var color1 = XBrushes.Black; var color2 = XBrushes.Black; var color3 = XBrushes.Black; var color4 = XBrushes.Black;
                    if (ii / (double)nrow == Math.Floor((double)ii / (double)nrow))
                    {
                        page = document.AddPage();// 空白ページを作成。
                        gfx = XGraphics.FromPdfPage(page);// 描画するためにXGraphicsオブジェクトを取得。
                    }
                    int i = ii - (int)Math.Floor((double)ii / (double)nrow) * nrow;
                    if (i == index.Count + 1)
                    {
                        gfx.DrawLine(pen, offset_x, offset_y + pitchy * i, offset_x + label_width * 12, offset_y + pitchy * i);//横線
                        foreach (int j in new int[]{ 0,3,4,5,6,7,8,9,10,11,12})
                        {
                            gfx.DrawLine(pen, offset_x + label_width * j, offset_y + pitchy * i, offset_x + label_width * j, offset_y + pitchy * (i + 1));//縦線
                        }
                    }
                    else if (i < index.Count + 1)
                    {
                        foreach (int j in new int[] { 0, 1, 3, 4, 6, 8, 10, 12 })
                        {
                            gfx.DrawLine(pen, offset_x + label_width * j, offset_y + pitchy * i, offset_x + label_width * j, offset_y + pitchy * (i + 1));//縦線
                        }
                    }
                    else
                    {
                        gfx.DrawLine(pen, offset_x, offset_y + pitchy * i, offset_x + label_width * 12, offset_y + pitchy * i);//横線
                        foreach (int j in new int[] { 0, 2, 4, 6, 8, 10, 12 })
                        {
                            gfx.DrawLine(pen, offset_x + label_width * j, offset_y + pitchy * i, offset_x + label_width * j, offset_y + pitchy * (i + 1));//縦線
                        }
                    }
                    gfx.DrawString(label1[ii], font, color1, new XRect(offset_x + label_width * 0, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                    gfx.DrawString(label2[ii], font, color1, new XRect(offset_x + label_width * 1, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                    gfx.DrawString(label3[ii], font, color1, new XRect(offset_x + label_width * 2, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                    gfx.DrawString(label4[ii], font, color1, new XRect(offset_x + label_width * 3, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                    gfx.DrawString(label5[ii], font, color1, new XRect(offset_x + label_width * 4, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                    gfx.DrawString(label6[ii], font, color1, new XRect(offset_x + label_width * 5, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                    if (i > index.Count + 1)
                    {
                        gfx.DrawString(label7[ii], font, color1, new XRect(offset_x + label_width * 6.5, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                    }
                    else
                    {
                        gfx.DrawString(label7[ii], font, color1, new XRect(offset_x + label_width * 6, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                        gfx.DrawString(label8[ii], font, color1, new XRect(offset_x + label_width * 7, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                    }
                    gfx.DrawString(label9[ii], font, color1, new XRect(offset_x + label_width * 8, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                    gfx.DrawString(label10[ii], font, color1, new XRect(offset_x + label_width * 9, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                    gfx.DrawString(label11[ii], font, color1, new XRect(offset_x + label_width * 10, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                    gfx.DrawString(label12[ii], font, color1, new XRect(offset_x + label_width * 11, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                    if (ii == label1.Count - 1)
                    {
                        gfx.DrawLine(pen, offset_x, offset_y + pitchy * (i + 1), offset_x + label_width * 12, offset_y + pitchy * (i + 1));//横線
                    }
                    if (ii == 0)
                    {
                        gfx.DrawLine(pen, offset_x, offset_y + pitchy * i, offset_x + label_width * 12, offset_y + pitchy * i);//横線
                        gfx.DrawLine(pen, offset_x, offset_y + pitchy * (i + 1), offset_x + label_width * 12, offset_y + pitchy * (i + 1));//横線
                    }
                }
                var dir = Path.GetDirectoryName(Rhino.RhinoDoc.ActiveDoc.Path);
                // ドキュメントを保存。
                var filename = dir + "/" + pdfname + ".pdf";
                document.Save(filename);
                // ビューアを起動。
                Process.Start(new ProcessStartInfo(@filename) { UseShellExecute = true });
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
                return OpenSeesUtility.Properties.Resources.eccentric;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("f54cc233-352a-4ee3-853b-5bd619955738"); }
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
                    graphics.DrawString("Center of rigidity", GH_FontServer.Standard, Brushes.Black, text_rec_1);

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