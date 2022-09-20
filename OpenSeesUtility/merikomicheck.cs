using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Display;

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
    public class merikomicheck : GH_Component
    {
        public static int on_off_11 = 0; public static int on_off_12 = 0;
        public double fontsize = 20; static int on_off = 0;
        public static PdfCreate.JapaneseFontResolver fontresolver = new PdfCreate.JapaneseFontResolver();
        public static XGraphics gfx;
        public static void SetButton(string s, int i)
        {
            if (s == "11")
            {
                on_off_11 = i;
            }
            else if (s == "12")
            {
                on_off_12 = i;
            }
            if (s == "1")
            {
                on_off = i;
            }
        }
        public merikomicheck()
          : base("MERIKOMI stress design for timber", "MerikomiCheck",
              "MERIKOMI stress design for timber",
              "OpenSees", "Analysis")
        {
        }
        public override bool IsPreviewCapable { get { return true; } }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("sectional_force", "sec_f", "[[Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("index", "index", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("Fcv", "Fcv", "hinoki&Hiba:7.8, Cedar:6.0, Pine:9.0(Standard allowable merikomi stress)[N/mm2]", GH_ParamAccess.item, 7.8);///
            pManager.AddNumberParameter("A", "A", "section area (A) as list", GH_ParamAccess.list, new List<double> { });///
            pManager.AddTextParameter("secname", "secname", "secname as list", GH_ParamAccess.list, new List<string> { });///
            pManager.AddNumberParameter("hozosize", "hozosize", "[hozosizeX, hozosizeY] [mm]", GH_ParamAccess.list, new List<double> {90,30});///
            pManager.AddNumberParameter("fontsize", "FS", "font size for display texts", GH_ParamAccess.item, 12.0);///
            pManager.AddTextParameter("outputname", "outputname", "output file name", GH_ParamAccess.item, "MerikomiCheck");///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("NL", "NL", "NL[kN]", GH_ParamAccess.list);///
            pManager.AddNumberParameter("NX1", "NX1", "NX1[kN]", GH_ParamAccess.list);///
            pManager.AddNumberParameter("NY1", "NY1", "NY1[kN]", GH_ParamAccess.list);///
            pManager.AddNumberParameter("NX2", "NX2", "NX2[kN]", GH_ParamAccess.list);///
            pManager.AddNumberParameter("NY2", "NY2", "NY2[kN]", GH_ParamAccess.list);///
            pManager.AddNumberParameter("NaL", "NaL", "NaL[kN]", GH_ParamAccess.list);///
            pManager.AddNumberParameter("NaS", "NaS", "NaS[kN]", GH_ParamAccess.list);///
            pManager.AddNumberParameter("kentei(max)", "kentei(max)", "[[ele. No.,for Long-term, for Short-term],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("kmax", "kmax", "[[ele. No.,Long-term max],[ele. No.,Short-term max]](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r); var r = _r.Branches;
            DA.GetDataTree("element_node_relationship", out GH_Structure<GH_Number> _ij); var ij = _ij.Branches;
            DA.GetDataTree("sectional_force", out GH_Structure<GH_Number> _sec_f); var sec_f = _sec_f.Branches;
            var hozosize = new List<double> { 90, 30 }; var Fcv = 7.8; var A = new List<double>(); DA.GetDataList("A", A); var secname = new List<string>(); DA.GetDataList("secname", secname);
            DA.GetDataList("hozosize", hozosize); DA.GetData("fontsize", ref fontsize); DA.GetData("Fcv", ref Fcv); List<double> index = new List<double>(); DA.GetDataList("index", index);
            int Digit(int num)//数字の桁数を求める関数
            {
                // Mathf.Log10(0)はNegativeInfinityを返すため、別途処理する。
                return (num == 0) ? 1 : ((int)Math.Log10(num) + 1);
            }
            XColor RGB(double h, double s, double l)//convert HSL to RGB
            {
                var max = 0.0; var min = 0.0; var rr = 0.0; var g = 0.0; var b = 0.0;
                if (l < 0.5)
                {
                    max = l + l * s;
                    min = l - l * s;
                }
                else
                {
                    max = l + (1 - l) * s;
                    min = l - (1 - l) * s;
                }
                var HUE_MAX = 360.0; var RGB_MAX = 255;
                var hp = HUE_MAX / 6.0; h *= HUE_MAX; var q = h / hp;
                if (q <= 1)
                {
                    rr = max;
                    g = (h / hp) * (max - min) + min;
                    b = min;
                }
                else if (q <= 2)
                {
                    rr = ((hp * 2 - h) / hp) * (max - min) + min;
                    g = max;
                    b = min;
                }
                else if (q <= 3)
                {
                    rr = min;
                    g = max;
                    b = ((h - hp * 2) / hp) * (max - min) + min;
                }
                else if (q <= 4)
                {
                    rr = min;
                    g = ((hp * 4 - h) / hp) * (max - min) + min;
                    b = max;
                }
                else if (q <= 5)
                {
                    rr = ((h - hp * 4) / hp) * (max - min) + min;
                    g = min;
                    b = max;
                }
                else
                {
                    rr = max;
                    g = min;
                    b = ((HUE_MAX - h) / hp) * (max - min) + min;
                }
                rr *= RGB_MAX; g *= RGB_MAX; b *= RGB_MAX;
                return XColor.FromArgb((int)rr, (int)g, (int)b);
            }
            if (index[0] == -9999)
            {
                index = new List<double>();
                for (int e = 0; e < ij.Count; e++) { index.Add(e); }
            }
            var NL = new List<double> { }; var NX1 = new List<double> { }; var NX2 = new List<double> { }; var NY1 = new List<double> { }; var NY2 = new List<double> { };
            var NaL = new List<double> { }; var NaS = new List<double> { }; var kenteimax = new GH_Structure<GH_Number>(); var sec = new List<string>(); var A1 = new List<double>(); var A2 = hozosize[0] * hozosize[1];
            var f1 = Fcv * 1.5 / 3.0; var f2 = Fcv * 2.0 / 3.0; var k1 = new List<double>(); var k2 = new List<double>(); var k3 = new List<double>(); var k4 = new List<double>(); var k5 = new List<double>(); var ks = new List<double>(); var rc = new List<Point3d>(); int digit = 4;
            if (r[0][0].Value != -9999 && ij[0][0].Value != -9999 && sec_f[0][0].Value != -9999)
            {
                if (sec_f[0][0].Value != -9999)
                {
                    int m2 = sec_f[0].Count;
                    for (int ind = 0; ind < index.Count; ind++)
                    {
                        int e = (int)index[ind]; int s = (int)ij[e][3].Value;
                        NL.Add(sec_f[e][0].Value); sec.Add(secname[s]); A1.Add(A[s] * 1e+6); NaL.Add(f1 * (A1[ind] - A2) / 1000);
                        k1.Add(NL[ind] / NaL[ind]);
                        var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                        var r1 = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value); var r2 = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value);
                        rc.Add((r1 + r2) / 2.0);
                    }
                    if (m2 / 18 >= 3)
                    {
                        for (int ind = 0; ind < index.Count; ind++)
                        {
                            int e = (int)index[ind]; int s = (int)ij[e][3].Value;
                            NX1.Add(sec_f[e][18 + 0].Value); NY1.Add(sec_f[e][18 * 2 + 0].Value);
                            if (m2 / 18 == 5) { NX2.Add(sec_f[e][18 * 3 + 0].Value); NY2.Add(sec_f[e][18 * 4 + 0].Value); }
                            else
                            {
                                NX2.Add(-NX1[ind]); NY2.Add(-NY1[ind]);
                            }
                            NaS.Add(f2 * (A1[ind] - A2) / 1000);
                            k2.Add(Math.Max(0.0, NL[ind]+NX1[ind]) / NaS[ind]); k3.Add(Math.Max(0.0, NL[ind] + NY1[ind]) / NaS[ind]); k4.Add(Math.Max(0.0, NL[ind] + NX2[ind]) / NaS[ind]); k5.Add(Math.Max(0.0, NL[ind] + NY2[ind]) / NaS[ind]);
                            ks.Add(Math.Max(Math.Max(k2[ind], k3[ind]), Math.Max(k4[ind], k5[ind])));
                        }
                    }
                    else { ks.Add(0.0); }
                    var n1 = 0; var n2 = 0; var kmax1 = 0.0; var kmax2 = 0.0;
                    for (int i = 0; i < index.Count; i++)
                    {
                        int e = (int)index[i];
                        kenteimax.AppendRange(new List<GH_Number> { new GH_Number(e), new GH_Number(k1[i]), new GH_Number(ks[i]) }, new GH_Path(i));
                        if (kmax1 < k1[i]) { n1 = e; kmax1 = k1[i]; }
                        if (kmax2 < ks[i]) { n2 = e; kmax2 = ks[i]; }
                    }
                    var kmax = new GH_Structure<GH_Number>();
                    kmax.AppendRange(new List<GH_Number> { new GH_Number(n1), new GH_Number(kmax1) }, new GH_Path(0));
                    DA.SetDataList("NL", NL); DA.SetDataList("NX1", NX1); DA.SetDataList("NY1", NY1); DA.SetDataList("NX2", NX2); DA.SetDataList("NY2", NY2); DA.SetDataList("NaL", NaL); DA.SetDataList("NaS", NaS);
                    if (m2 / 18 >= 3) { kmax.AppendRange(new List<GH_Number> { new GH_Number(n2), new GH_Number(kmax2) }, new GH_Path(1)); }
                    DA.SetDataTree(7, kenteimax); DA.SetDataTree(8, kmax);
                    if (on_off_11 == 1)
                    {
                        for (int i = 0; i < k1.Count; i++)
                        {
                            var k = k1[i];
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _text.Add(k.ToString("F").Substring(0, digit)); _p.Add(rc[i]); _c.Add(color);
                        }
                    }
                    else if (on_off_12 == 1)
                    {
                        for (int i = 0; i < ks.Count; i++)
                        {
                            var k = ks[i];
                            var color = new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5);
                            _text.Add(k.ToString("F").Substring(0, digit)); _p.Add(rc[i]); _c.Add(color);
                        }
                    }
                }
                if (on_off == 1)
                {
                    var pdfname = "MerikomiCheck"; DA.GetData("outputname", ref pdfname);
                    // フォントリゾルバーのグローバル登録
                    if (PdfCreate.JapaneseFontResolver.fontset == 0) { PdfSharp.Fonts.GlobalFontSettings.FontResolver = fontresolver; PdfCreate.JapaneseFontResolver.fontset = 1; }
                    // PDFドキュメントを作成。
                    PdfDocument document = new PdfDocument();
                    document.Info.Title = pdfname;
                    document.Info.Author = "Shinnosuke Fujita, Assoc. Prof., The Univ. of Kitakyushu";
                    // フォントを作成。
                    XFont font = new XFont("Gen Shin Gothic", 9, XFontStyle.Regular);
                    var pen = XPens.Black;
                    if (sec_f[0].Count >= 18)
                    {
                        var labels = new List<string>
                        {
                            "部材番号","柱断面[mm]","ほぞ[mm]","支圧面積[mm2]","","許容支圧応力度fcv[N/mm2]","許容支圧耐力Na[kN]","N[kN]","検定比N/Na"
                        };
                        if (sec_f[0].Count >= 18 * 3)
                        {
                            labels = new List<string>
                            {
                            "部材番号","柱断面[mm]","ほぞ[mm]","支圧面積[mm2]","","許容支圧応力度fcv[N/mm2]","許容支圧耐力Na[kN]","N[kN]","検定比N/Na",
                            "","許容支圧応力度fcv[N/mm2]","許容支圧耐力Na[kN]","","N(L+X)[kN]","検定比N(L+X)/Na","","N(L+Y)[kN]","検定比N(L+Y)/Na","","N(L-X)[kN]","検定比N(L-X)/Na","","N(L-Y)[kN]","検定比N(L-Y)/Na"
                            };
                        }
                        var n = 9; var k = 0; var label_width = 115; var offset_x = 25; var offset_y = 25; var pitchy = 11; var text_width = 47; PdfPage page = new PdfPage(); page.Size = PageSize.A4;
                        for (int j = 0; j < k1.Count; j++)
                        {
                            var values = new List<string>();
                            int e = (int)index[j];
                            values.Add(e.ToString()); values.Add(sec[j]); values.Add("■-" + hozosize[0].ToString() + "x" + hozosize[1].ToString()); values.Add(((A1[j] - A2)).ToString());
                            values.Add("長期検討"); values.Add(f1.ToString());
                            values.Add(NaL[j].ToString("F6").Substring(0, Math.Max(4, Digit((int)NaL[j]) + 2)));
                            values.Add(NL[j].ToString("F6").Substring(0, Math.Max(4, Digit((int)NL[j]) + 2)));
                            var text = ":O.K.";
                            if (k1[j] >= 1.0) { text = ":N.G."; }
                            values.Add(k1[j].ToString("F6").Substring(0, 4) + text);
                            if (sec_f[0].Count >= 18 * 3)
                            {
                                values.Add("短期検討"); values.Add(f2.ToString());
                                values.Add(NaS[j].ToString("F6").Substring(0, Math.Max(4, Digit((int)NaS[j]) + 2)));
                                values.Add("L+X検討");
                                values.Add((NL[j]+NX1[j]).ToString("F6").Substring(0, Math.Max(4, Digit((int)(NL[j] + NX1[j])) + 2)));
                                text = ":O.K.";
                                if (k2[j] >= 1.0) { text = ":N.G."; }
                                values.Add(k2[j].ToString("F6").Substring(0, 4) + text);
                                values.Add("L+Y検討");
                                values.Add((NL[j] + NY1[j]).ToString("F6").Substring(0, Math.Max(4, Digit((int)(NL[j] + NY1[j])) + 2)));
                                text = ":O.K.";
                                if (k3[j] >= 1.0) { text = ":N.G."; }
                                values.Add(k3[j].ToString("F6").Substring(0, 4) + text);
                                values.Add("L-X検討");
                                values.Add((NL[j] + NX2[j]).ToString("F6").Substring(0, Math.Max(4, Digit((int)(NL[j] + NX2[j])) + 2)));
                                text = ":O.K.";
                                if (k4[j] >= 1.0) { text = ":N.G."; }
                                values.Add(k4[j].ToString("F6").Substring(0, 4) + text);
                                values.Add("L-Y検討");
                                values.Add((NL[j] + NY2[j]).ToString("F6").Substring(0, Math.Max(4, Digit((int)(NL[j] + NY2[j])) + 2)));
                                text = ":O.K.";
                                if (k5[j] >= 1.0) { text = ":N.G."; }
                                values.Add(k5[j].ToString("F6").Substring(0, 4) + text);
                            }
                            var slide = 0.0;
                            if (n <= k % (n * 3) && k % (n * 3) < n * 2) { slide = pitchy * 24.5; }
                            if (n * 2 <= k % (n * 3) && k % (n * 3) < n * 3) { slide = pitchy * 24.5 * 2; }
                            if (k % n == 0)
                            {
                                if (k % (n * 3) == 0)
                                {
                                    page = document.AddPage();// 空白ページを作成。
                                    gfx = XGraphics.FromPdfPage(page);// 描画するためにXGraphicsオブジェクトを取得。
                                }
                                for (int ii = 0; ii < labels.Count; ii++)//ラベル列**************************************************************************
                                {
                                    gfx.DrawLine(pen, offset_x, offset_y + pitchy * ii + slide, offset_x + label_width, offset_y + pitchy * ii + slide);//横線
                                    gfx.DrawLine(pen, offset_x + label_width, offset_y + pitchy * ii + slide, offset_x + label_width, offset_y + pitchy * (ii + 1) + slide);//縦線
                                    gfx.DrawLine(pen, offset_x, offset_y + pitchy * ii + slide, offset_x, offset_y + pitchy * (ii + 1) + slide);//縦線
                                    gfx.DrawString(labels[ii], font, XBrushes.Black, new XRect(offset_x, offset_y + pitchy * ii + slide, label_width, offset_y + pitchy * (ii + 1) + slide), XStringFormats.TopCenter);
                                    if (ii == labels.Count - 1)
                                    {
                                        ii += 1;
                                        gfx.DrawLine(pen, offset_x, offset_y + pitchy * ii + slide, offset_x + label_width, offset_y + pitchy * ii + slide);//横線
                                    }
                                }
                            }
                            for (int ii = 0; ii < values.Count; ii++)
                            {
                                var jj = k % n;
                                gfx.DrawLine(pen, offset_x + label_width + text_width * jj, offset_y + pitchy * ii + slide, offset_x + label_width + text_width * (jj + 1), offset_y + pitchy * ii + slide);//横線
                                gfx.DrawLine(pen, offset_x + label_width + text_width * (jj + 1), offset_y + pitchy * ii + slide, offset_x + label_width + text_width * (jj + 1), offset_y + pitchy * (ii + 1) + slide);//縦線
                                if (ii == values.Count - 1)
                                {
                                    gfx.DrawLine(pen, offset_x + label_width + text_width * jj, offset_y + pitchy * (ii + 1) + slide, offset_x + label_width + text_width * (jj + 1), offset_y + pitchy * (ii + 1) + slide);//横線
                                }
                                var color = XBrushes.Black;
                                if (ii == 8 || ii == 14 || ii == 17 || ii == 20 || ii == 23)
                                {
                                    if (ii == 8) { color = new XSolidBrush(RGB((1 - Math.Min(k1[j], 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                    if (ii == 14) { color = new XSolidBrush(RGB((1 - Math.Min(k2[j], 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                    if (ii == 17) { color = new XSolidBrush(RGB((1 - Math.Min(k3[j], 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                    if (ii == 20) { color = new XSolidBrush(RGB((1 - Math.Min(k4[j], 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                    if (ii == 23) { color = new XSolidBrush(RGB((1 - Math.Min(k5[j], 1.0)) * 1.9 / 3.0, 1, 0.5)); }
                                }
                                gfx.DrawString(values[ii], font, color, new XRect(offset_x + label_width + text_width * jj, offset_y + pitchy * ii + slide, text_width, offset_y + pitchy * (ii + 1) + slide), XStringFormats.TopCenter);
                            }
                            k += 1;
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
                return OpenSeesUtility.Properties.Resources.merikomicheck;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6f8f25a8-e8d9-4ec4-9a16-07b938f05426"); }
        }
        ///ここからカスタム関数群********************************************************************************
        private readonly List<string> _text = new List<string>();
        private readonly List<Point3d> _p = new List<Point3d>();
        private readonly List<Color> _c = new List<Color>();
        private readonly List<double> _size = new List<double>();
        protected override void BeforeSolveInstance()
        {
            _text.Clear();
            _c.Clear();
            _p.Clear();
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            args.Viewport.GetFrustumFarPlane(out Plane plane);
            RhinoViewport viewport = args.Viewport;
            for (int i = 0; i < _text.Count; i++)
            {
                double size = fontsize; Point3d point = _p[i];
                plane.Origin = point;
                viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                Text3d drawText = new Text3d(_text[i], plane, size);
                args.Display.Draw3dText(drawText, _c[i]);
                drawText.Dispose();
            }
        }///ここからGUIの作成*****************************************************************************************
        public class CustomGUI : GH_ComponentAttributes
        {
            public CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle title_rec;
            private Rectangle radio_rec; private Rectangle radio_rec2; private Rectangle radio_rec3;
            private Rectangle radio_rec_1; private Rectangle text_rec_1;
            private Rectangle radio_rec_2; private Rectangle text_rec_2;
            private Rectangle radio_rec3_1; private Rectangle text_rec3_1;
            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int height = 120; int radi1 = 7; int radi2 = 4; int titleheight = 20;
                int pitchx = 8; int pitchy = 11; int textheight = 20;
                global_rec.Height += height;
                int width = global_rec.Width;
                title_rec = global_rec;
                title_rec.Y = title_rec.Bottom - height;
                title_rec.Height = titleheight;

                radio_rec = title_rec;
                radio_rec.Y += title_rec.Height;
                radio_rec.Height = 5;

                radio_rec_1 = radio_rec;
                radio_rec_1.X += 5; radio_rec_1.Y += 5;
                radio_rec_1.Height = radi1; radio_rec_1.Width = radi1;

                text_rec_1 = radio_rec_1;
                text_rec_1.X += pitchx; text_rec_1.Y -= radi2;
                text_rec_1.Height = textheight; text_rec_1.Width = width;
                radio_rec.Height += pitchy;

                radio_rec_2 = radio_rec_1; radio_rec_2.Y += pitchy;
                text_rec_2 = radio_rec_2;
                text_rec_2.X += pitchx; text_rec_2.Y -= radi2;
                text_rec_2.Height = textheight; text_rec_2.Width = width;
                radio_rec.Height += pitchy;

                radio_rec3 = radio_rec;
                radio_rec3.Y = radio_rec.Bottom + radio_rec2.Height;
                radio_rec3.Height = textheight;

                radio_rec3_1 = radio_rec3;
                radio_rec3_1.X += 5; radio_rec3_1.Y += 5;
                radio_rec3_1.Height = radi1; radio_rec3_1.Width = radi1;

                text_rec3_1 = radio_rec3_1;
                text_rec3_1.X += pitchx; text_rec3_1.Y -= radi2;
                text_rec3_1.Height = textheight; text_rec3_1.Width = width * 3;
                global_rec.Height = radio_rec3.Bottom - global_rec.Top;

                Bounds = global_rec;
            }
            Brush c1 = Brushes.White; Brush c2 = Brushes.White; Brush c3 = Brushes.White;
            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
            {
                base.Render(canvas, graphics, channel);
                if (channel == GH_CanvasChannel.Objects)
                {
                    StringFormat format = new StringFormat();
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    format.Trimming = StringTrimming.EllipsisCharacter;

                    GH_Capsule title = GH_Capsule.CreateCapsule(title_rec, GH_Palette.Pink, 2, 0);
                    title.Render(graphics, Selected, Owner.Locked, false);
                    title.Dispose();

                    RectangleF textRectangle = title_rec;
                    textRectangle.Height = 20;
                    graphics.DrawString("Stress design", GH_FontServer.Standard, Brushes.White, textRectangle, format);

                    GH_Capsule radio = GH_Capsule.CreateCapsule(radio_rec, GH_Palette.White, 2, 0);
                    radio.Render(graphics, Selected, Owner.Locked, false); radio.Dispose();

                    GH_Capsule radio_1 = GH_Capsule.CreateCapsule(radio_rec_1, GH_Palette.Black, 5, 5);
                    radio_1.Render(graphics, Selected, Owner.Locked, false); radio_1.Dispose();
                    graphics.FillEllipse(c1, radio_rec_1);
                    graphics.DrawString("Long-term", GH_FontServer.Standard, Brushes.Black, text_rec_1);

                    GH_Capsule radio_2 = GH_Capsule.CreateCapsule(radio_rec_2, GH_Palette.Black, 5, 5);
                    radio_2.Render(graphics, Selected, Owner.Locked, false); radio_2.Dispose();
                    graphics.FillEllipse(c2, radio_rec_2);
                    graphics.DrawString("Short-term", GH_FontServer.Standard, Brushes.Black, text_rec_2);

                    GH_Capsule radio3 = GH_Capsule.CreateCapsule(radio_rec3, GH_Palette.White, 2, 0);
                    radio3.Render(graphics, Selected, Owner.Locked, false); radio3.Dispose();

                    GH_Capsule radio3_1 = GH_Capsule.CreateCapsule(radio_rec3_1, GH_Palette.Black, 5, 5);
                    radio3_1.Render(graphics, Selected, Owner.Locked, false); radio3_1.Dispose();
                    graphics.FillEllipse(c3, radio_rec3_1);
                    graphics.DrawString("PDF OUTPUT", GH_FontServer.Standard, Brushes.Black, text_rec3_1);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec1 = radio_rec_1; RectangleF rec2 = radio_rec_2;
                    RectangleF rec3 = radio_rec3_1;
                    if (rec1.Contains(e.CanvasLocation))
                    {
                        if (c1 == Brushes.White) { c1 = Brushes.Black; SetButton("11", 1); c2 = Brushes.White; SetButton("12", 0); }
                        else { c1 = Brushes.White; SetButton("11", 0);}
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec2.Contains(e.CanvasLocation))
                    {
                        if (c2 == Brushes.White) { c2 = Brushes.Black; SetButton("12", 1); c1 = Brushes.White; SetButton("11", 0); }
                        else { c2 = Brushes.White; SetButton("12", 0);}
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