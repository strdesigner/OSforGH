using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
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

namespace StripFooting
{
    public class StripFooting : GH_Component
    {
        static int BaseShape = 0; static int BaseNo = 0; static int BaseWidth = 0; static int BaseThick = 0; static int Pressure = 0;
        public static double fontsize =10.0;
        string unit_of_force = "kN"; string unit_of_length = "m"; int digit = 4; public static int on_off = 0;
        public static PdfCreate.JapaneseFontResolver fontresolver = new PdfCreate.JapaneseFontResolver();
        public static XGraphics gfx;
        public static void SetButton_for_StripFooting(string s, int i)
        {
            if (s == "c11")
            {
                BaseShape = i;
            }
            else if (s == "c13")
            {
                BaseNo = i;
            }
            else if (s == "c21")
            {
                BaseWidth = i;
            }
            else if (s == "c22")
            {
                BaseThick = i;
            }
            else if (s == "c23")
            {
                Pressure = i;
            }
            else if (s == "1")
            {
                on_off = i;
            }
        }
        public StripFooting()
          : base("StripFooting", "StripFooting",
              "read strip footing infomation and calc pressure load",
              "OpenSees", "Analysis")
        {
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("R", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("reac_f", "reac_f", "[[Node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddTextParameter("layer", "layer", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list,"布基礎");
            pManager.AddTextParameter("name B", "name B", "name of base width[m]", GH_ParamAccess.item,"幅");
            pManager.AddTextParameter("name t", "name t", "name of base thickness[m]", GH_ParamAccess.item,"t");
            pManager.AddTextParameter("name rho", "name rho", "name of unit volume weight[kN/m3]", GH_ParamAccess.item,"ρ");
            pManager.AddTextParameter("name bar", "name bar", "name of bar size [mm]", GH_ParamAccess.item, "D");
            pManager.AddTextParameter("name pitch", "name pitch", "name of bar pitch [mm]", GH_ParamAccess.item, "@");
            pManager.AddTextParameter("name w & e", "name w & e", "name of base beam width & eccentric distance [m]", GH_ParamAccess.list, new List<string> { "w","e" });
            pManager.AddTextParameter("name ft", "name ft", "name of long-term arrowable stress [N/mm2]", GH_ParamAccess.item, "ft");
            pManager.AddTextParameter("name as", "name as", "name of long-term arrowable pressure [kN/m2]", GH_ParamAccess.item, "as");
            pManager.AddNumberParameter("FS", "FS", "font size for display texts", GH_ParamAccess.item, 10.0);///
            pManager.AddTextParameter("outputname", "outputname", "output file name", GH_ParamAccess.item, "StripBase");///
            pManager.AddNumberParameter("offset", "offset", "boundary level from base line", GH_ParamAccess.item, 0.0);
            pManager.AddIntegerParameter("option", "option", "1:floating check, 2:no moment check", GH_ParamAccess.item, 0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("base", "base", "base lines", GH_ParamAccess.list);
            pManager.AddNumberParameter("N/A", "N/A", "N/A", GH_ParamAccess.list);
            pManager.AddNumberParameter("e_load", "e_load", "[[element No.,Wx,Wy,Wz],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("B number", "B number", "Boundary numbers of each footing", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int Digit(int num)//数字の桁数を求める関数
            {
                var keta = (int)Math.Log10(num) + 1;
                if (num < 0) { keta = (int)Math.Log10(-num) + 1; }
                // Mathf.Log10(0)はNegativeInfinityを返すため、別途処理する。
                return (num == 0) ? 1 : (keta);
            }
            XColor RGB(double h, double s, double l)//convert HSL to RGB
            {
                var max = 0.0; var min = 0.0; var R = 0.0; var g = 0.0; var b = 0.0;
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
                    R = max;
                    g = (h / hp) * (max - min) + min;
                    b = min;
                }
                else if (q <= 2)
                {
                    R = ((hp * 2 - h) / hp) * (max - min) + min;
                    g = max;
                    b = min;
                }
                else if (q <= 3)
                {
                    R = min;
                    g = max;
                    b = ((h - hp * 2) / hp) * (max - min) + min;
                }
                else if (q <= 4)
                {
                    R = min;
                    g = ((hp * 4 - h) / hp) * (max - min) + min;
                    b = max;
                }
                else if (q <= 5)
                {
                    R = ((h - hp * 4) / hp) * (max - min) + min;
                    g = min;
                    b = max;
                }
                else
                {
                    R = max;
                    g = min;
                    b = ((HUE_MAX - h) / hp) * (max - min) + min;
                }
                R *= RGB_MAX; g *= RGB_MAX; b *= RGB_MAX;
                return XColor.FromArgb((int)R, (int)g, (int)b);
            }
            Vector3d rotation(Vector3d a, Vector3d b, double theta)
            {
                double rad = theta * Math.PI / 180;
                double s = Math.Sin(rad); double c = Math.Cos(rad);
                b /= Math.Sqrt(Vector3d.Multiply(b, b));
                double b1 = b[0]; double b2 = b[1]; double b3 = b[2];
                Vector3d m1 = new Vector3d(c + Math.Pow(b1, 2) * (1 - c), b1 * b2 * (1 - c) - b3 * s, b1 * b3 * (1 - c) + b2 * s);
                Vector3d m2 = new Vector3d(b2 * b1 * (1 - c) + b3 * s, c + Math.Pow(b2, 2) * (1 - c), b2 * b3 * (1 - c) - b1 * s);
                Vector3d m3 = new Vector3d(b3 * b1 * (1 - c) - b2 * s, b3 * b2 * (1 - c) + b1 * s, c + Math.Pow(b3, 2) * (1 - c));
                return new Vector3d(Vector3d.Multiply(m1, a), Vector3d.Multiply(m2, a), Vector3d.Multiply(m3, a));
            }
            var doc = RhinoDoc.ActiveDoc;
            DA.GetDataTree("R", out GH_Structure<GH_Number> _r); var r = _r.Branches; var option = 0; DA.GetData("option", ref option);
            DA.GetDataTree("reac_f", out GH_Structure<GH_Number> _reac_f); var reac_f = _reac_f.Branches; var m = reac_f.Count;
            List<string> layer = new List<string>(); var nameB = "布基礎"; var namet = "t"; var namerho = "ρ"; var nameD = "D"; var namepitch = "@"; var nameft = "ft"; var namew = new List<string> {}; var nameac = "as";
            DA.GetDataList("layer", layer); DA.GetData("name B", ref nameB); DA.GetData("name t", ref namet); DA.GetData("name rho", ref namerho); DA.GetData("name bar", ref nameD); DA.GetData("name pitch", ref namepitch); DA.GetData("name ft", ref nameft); DA.GetDataList("name w & e", namew); DA.GetData("name as", ref nameac);
            DA.GetData("FS", ref fontsize);
            var pdfname = "StripBase"; DA.GetData("outputname", ref pdfname);

            var pressure = new List<double>(); var baseshape = new List<Curve>();
            var B = new List<double>(); var T = new List<double>(); var L = new List<double>(); var Rz = new List<double>(); var Sz = new List<double>(); var A = new List<double>();
            var bar = new List<string>(); var M = new List<double>(); var Ma = new List<double>(); var LL = new List<double>(); var Ft = new List<double>(); var J = new List<double>(); var At = new List<double>(); var Ac = new List<double>(); var Ecc = new List<double>(); var Z = new List<double>();
            var Rzx = new List<double>(); var Rzy = new List<double>(); var Rzx2 = new List<double>(); var Rzy2 = new List<double>();
            var lines = new List<List<Curve>>(); GH_Structure<GH_Number> B_number = new GH_Structure<GH_Number>();
            var Rz_all = new GH_Structure<GH_Number>(); var Rzx_all = new GH_Structure<GH_Number>(); var Rzy_all = new GH_Structure<GH_Number>(); var Rzx2_all = new GH_Structure<GH_Number>(); var Rzy2_all = new GH_Structure<GH_Number>();
            for (int i = 0; i < layer.Count; i++)
            {
                var line = doc.Objects.FindByLayer(layer[i]);
                for (int j = 0; j < line.Length; j++)
                {
                    List<GH_Number> flist = new List<GH_Number>(); var nlist = new List<GH_Number>(); var nxlist = new List<GH_Number>(); var nylist = new List<GH_Number>(); var nx2list = new List<GH_Number>(); var ny2list = new List<GH_Number>();
                    var obj = line[j]; Curve[] l = new Curve[] { (new ObjRef(obj)).Curve() }; baseshape.Add(l[0]);
                    int nl = (new ObjRef(obj)).Curve().SpanCount;//ポリラインのセグメント数
                    if (nl > 1) { l = (new ObjRef(obj)).Curve().DuplicateSegments(); }
                    var llist = new List<Curve>();
                    for (int aa = 0; aa < nl; aa++) { llist.Add(l[aa]); }
                    lines.Add(llist);
                    var lgh = 0.0;
                    for (int aa = 0; aa < nl; aa++) { lgh += l[aa].GetLength(); }
                    var b = float.Parse(obj.Attributes.GetUserString(nameB));
                    var t = float.Parse(obj.Attributes.GetUserString(namet));
                    var text_rho = obj.Attributes.GetUserString(namerho);
                    var rho = 24.0;
                    if (text_rho == null) { rho = 24.0; }
                    else { rho = float.Parse(text_rho); }
                    var text = "0.0";
                    if (namew.Count > 1) { text = obj.Attributes.GetUserString(namew[1]); }
                    var ecc = 0.0;//[m]
                    if (text != null) { ecc = float.Parse(text); }
                    var N = 0.0; var Nx = 0.0; var Ny = 0.0; var Nx2 = 0.0; var Ny2 = 0.0;
                    for (int k = 0; k < m; k++)
                    {
                        int e = (int)reac_f[k][0].Value;
                        var pt = new Point3d(r[e][0].Value, r[e][1].Value, r[e][2].Value);
                        double distance = 9999;
                        for (int aa = 0; aa < nl; aa++)
                        {
                            var para = 0.0;
                            var r1 = l[aa].PointAtStart; var r2 = l[aa].PointAtEnd; var l2 = r2 - r1;
                            var l1 = pt - r1;
                            if (l2.Length - l1.Length >= -1e-8 && (l1 / l1.Length - l2 / l2.Length).Length < 1e-8)
                            {
                                N += reac_f[k][3].Value; flist.Add(new GH_Number(e));
                                if (reac_f[0].Count == 7 * 3)
                                {
                                    Nx += reac_f[k][3 + 7 * 1].Value; Ny += reac_f[k][3 + 7 * 2].Value;
                                    Nx2 -= reac_f[k][3 + 7 * 1].Value; Ny2 -= reac_f[k][3 + 7 * 2].Value;
                                }
                                else if (reac_f[0].Count == 7 * 5)
                                {
                                    Nx += reac_f[k][3 + 7 * 1].Value; Ny += reac_f[k][3 + 7 * 2].Value;
                                    Nx2 += reac_f[k][3 + 7 * 3].Value; Ny2 += reac_f[k][3 + 7 * 4].Value;
                                }
                            }
                        }
                        nlist.Add(new GH_Number(N)); nxlist.Add(new GH_Number(Nx)); nylist.Add(new GH_Number(Ny)); nx2list.Add(new GH_Number(Nx)); ny2list.Add(new GH_Number(Ny));
                    }
                    L.Add(lgh); A.Add(b * L[j]);
                    B.Add(b); T.Add(t); Rz.Add(N); Sz.Add(t * A[j] * rho); Rzx.Add(Nx); Rzy.Add(Ny); Rzx2.Add(Nx2); Rzy2.Add(Ny2); Ecc.Add(ecc);
                    Z.Add(L[j] * Math.Pow(b, 2) / 6.0);
                    var prs = (N + Sz[j]) / A[j] + N * Math.Abs(ecc / Z[j]);
                    pressure.Add(prs);
                    var xc = 0.0; var yc = 0.0; var zc = 0.0;
                    for (int aa = 0; aa < nl; aa++) { var pts = (l[aa].PointAtStart + l[aa].PointAtEnd) / 2.0; xc += pts[0]; yc += pts[1]; zc += pts[2]; }
                    xc = xc / (double)nl; yc = yc / (double)nl; zc = zc / (double)nl;
                    if (BaseWidth == 1)
                    {
                        _pt.Add(new Point3d(xc, yc, zc));
                        _text.Add(b.ToString("F6").Substring(0, digit) + unit_of_length);
                        _c2.Add(Color.Blue);
                    }
                    if (BaseNo == 1)
                    {
                        _pt.Add(new Point3d(xc, yc, zc));
                        _text.Add(j.ToString());
                        _c2.Add(Color.Black);
                    }
                    if (BaseThick == 1)
                    {
                        _pt.Add(new Point3d(xc, yc, zc));
                        _text.Add(t.ToString("F6").Substring(0, digit) + unit_of_length);
                        _c2.Add(Color.Purple);
                    }
                    if (Pressure == 1)
                    {
                        _pt.Add(new Point3d(xc, yc, zc));
                        _text.Add(prs.ToString("F6").Substring(0, digit) + unit_of_force + "/" + unit_of_length + "2");
                        _c2.Add(Color.Red);
                    }
                    if (BaseShape == 1)
                    {
                        Random rand1 = new Random((i + 1) * (j + 1) * 1000); Random rand2 = new Random((i + 1) * (j + 1) * 2000); Random rand3 = new Random((i + 1) * (j + 1) * 3000);
                        var c = Color.FromArgb(rand1.Next(0, 256), rand2.Next(0, 256), rand3.Next(0, 256));
                        for (int aa = 0; aa < nl; aa++)
                        {
                            _c.Add(c);
                            var r1 = l[aa].PointAtStart; var r2 = l[aa].PointAtEnd;
                            var l1 = rotation(r2-r1, new Vector3d(0, 0, 1), 90); l1 = l1 / l1.Length;
                            var p1 = r1 + l1 * (b / 2.0 + ecc); var p2 = r2 + l1 * (b / 2.0 + ecc); var p3 = r2 - l1 * (b / 2.0 - ecc); var p4 = r1 - l1 * (b / 2.0 - ecc);
                            var brep = Brep.CreatePlanarBreps(new Polyline(new List<Point3d> { p1, p2, p3, p4, p1 }).ToNurbsCurve(), 0.001)[0];
                            _s.Add(brep);
                        }
                    }
                    text = obj.Attributes.GetUserString(nameD);
                    var D = 10.0;//[mm]
                    if (text != null) { D = float.Parse(text); }
                    text = obj.Attributes.GetUserString(namepitch);
                    var pitch = 200.0;//[mm]
                    if (text != null) { pitch = float.Parse(text); }
                    var ft = 195.0;//[mm2]
                    text = obj.Attributes.GetUserString(nameft);
                    if (text != null) { ft = float.Parse(text); }
                    text = obj.Attributes.GetUserString(namew[0]);
                    var w = 0.2;//[m]
                    if (text != null) { w = float.Parse(text); }
                    text = obj.Attributes.GetUserString(nameac);
                    var ac = 30.0;//[kN/m2]
                    if (text != null) { ac = float.Parse(text); }
                    Ac.Add(ac);
                    var at = Math.PI * Math.Pow(D, 2) / 4.0 * 1000.0 / pitch;//[mm2]
                    var dj = (t * 1000 - 60) * 7 / 8;//[mm]
                    J.Add(dj); At.Add(at);
                    var span = (b - w) / 2.0 + ecc; LL.Add(span);
                    M.Add(prs * Math.Pow(span, 2) / 2.0);//[kNm]
                    Ma.Add(at * ft * dj / 1e+6);//[kNm]
                    bar.Add("D" + ((int)D).ToString() + "@" + ((int)pitch).ToString()); Ft.Add(ft);
                    B_number.AppendRange(flist, new GH_Path(j)); Rz_all.AppendRange(nlist, new GH_Path(j)); Rzx_all.AppendRange(nxlist, new GH_Path(j)); Rzy_all.AppendRange(nylist, new GH_Path(j)); Rzx2_all.AppendRange(nx2list, new GH_Path(j)); Rzy2_all.AppendRange(ny2list, new GH_Path(j));
                }
            }
            DA.SetDataList("base", baseshape);
            DA.SetDataList("N/A", pressure); DA.SetDataTree(3, B_number);
            DA.GetDataTree("element_node_relationship", out GH_Structure<GH_Number> _ij);
            var ij = _ij.Branches; GH_Structure<GH_Number> e_load = new GH_Structure<GH_Number>(); int kk = 0;
            var offset = 0.0; DA.GetData("offset", ref offset);
            if (_ij.Branches[0][0].Value != -9999)
            {
                for (int k = 0; k < pressure.Count; k++)
                {
                    for (int aa = 0; aa < lines[k].Count; aa++)
                    {
                        var ri = lines[k][aa].PointAtStart; var rj = lines[k][aa].PointAtEnd;//布基礎の両端の座標
                        ri[2] += offset; rj[2] += offset;
                        var xi = ri[0]; var yi = ri[1]; var zi = ri[2]; var xj = rj[0]; var yj = rj[1]; var zj = rj[2];
                        var xmin = Math.Min(xi, xj) - 0.1; var xmax = Math.Max(xi, xj) + 0.1; var ymin = Math.Min(yi, yj) - 0.1; var ymax = Math.Max(yi, yj) + 0.1; var zmin = Math.Min(zi, zj) - 0.1; var zmax = Math.Max(zi, zj) + 0.1;
                        var v1 = rj - ri;
                        for (int e = 0; e < ij.Count; e++)
                        {
                            int ni = (int)ij[e][0].Value; int nj = (int)ij[e][1].Value;
                            var x1 = r[ni][0].Value; var y1 = r[ni][1].Value; var z1 = r[ni][2].Value; var x2 = r[nj][0].Value; var y2 = r[nj][1].Value; var z2 = r[nj][2].Value;
                            var r1 = new Point3d(x1, y1, z1); var r2 = new Point3d(x2, y2, z2);
                            var v2 = r2 - r1;
                            if (Math.Abs(Math.Abs(Vector3d.VectorAngle(v1, v2))) < 1e-2 || Math.Abs(Math.Abs(Vector3d.VectorAngle(v1, v2))) + 1e-2 > Math.PI)
                            {
                                if (xmin < x1 && x1 < xmax && xmin < x2 && x2 < xmax && ymin < y1 && y1 < ymax && ymin < y2 && y2 < ymax && zmin < z1 && z1 < zmax && zmin < z2 && z2 < zmax)
                                {
                                    List<GH_Number> flist = new List<GH_Number>();
                                    flist.Add(new GH_Number(e)); flist.Add(new GH_Number(0)); flist.Add(new GH_Number(0)); flist.Add(new GH_Number(pressure[k] * B[k]));
                                    e_load.AppendRange(flist, new GH_Path(kk));
                                    kk += 1;
                                }
                            }
                        }
                    }
                }
                DA.SetDataTree(2, e_load);
            }
            //pdf作成
            if (on_off == 1)
            {
                // フォントリゾルバーのグローバル登録
                if (PdfCreate.JapaneseFontResolver.fontset == 0) { PdfSharp.Fonts.GlobalFontSettings.FontResolver = fontresolver; PdfCreate.JapaneseFontResolver.fontset = 1; }
                // フォントを作成。
                XFont font = new XFont("Gen Shin Gothic", 9, XFontStyle.Regular);
                XFont fontbold = new XFont("Gen Shin Gothic", 9, XFontStyle.Bold);
                var pen = XPens.Black;
                var dir = Path.GetDirectoryName(Rhino.RhinoDoc.ActiveDoc.Path);//カレントディレクトリ
                // PDFドキュメントを作成。
                PdfDocument document = new PdfDocument();
                document.Info.Title = pdfname;
                document.Info.Author = "Shinnosuke Fujita, Assoc. Prof., The Univ. of Kitakyushu";
                var labels = new List<string>
                        {
                            "基礎番号","幅B[m]","厚みt[m]","長さL[m]","偏心距離e[m]","底面積A[m2]","断面係数Z[m3]","反力合計[kN]","基礎自重[kN]","接地圧N/A+M/Z[kN/m2]","許容地耐力[kN/m2]","地耐力検定比","基礎出幅[m]","M[kNm]","配筋","断面積at[mm2]","ft[N/mm2]","応力中心間距離j[mm]","Ma[kNm]","曲げ検定比M/Ma","反力合計(L+X)[kN]","反力合計(L+Y)[kN]","反力合計(L-X)[kN]","反力合計(L-Y)[kN]","浮き上がり"
                        };
                if (option == 0)
                {
                    labels = new List<string>
                        {
                            "基礎番号","幅B[m]","厚みt[m]","長さL[m]","偏心距離e[m]","底面積A[m2]","断面係数Z[m3]","反力合計[kN]","基礎自重[kN]","接地圧N/A+M/Z[kN/m2]","許容地耐力[kN/m2]","地耐力検定比","基礎出幅[m]","M[kNm]","配筋","断面積at[mm2]","ft[N/mm2]","応力中心間距離j[mm]","Ma[kNm]","曲げ検定比M/Ma"
                        };
                }
                else if (option == 2)
                {
                    labels = new List<string>
                        {
                            "基礎番号","幅B[m]","厚みt[m]","長さL[m]","偏心距離e[m]","底面積A[m2]","断面係数Z[m3]","反力合計[kN]","基礎自重[kN]","接地圧N/A+M/Z[kN/m2]","許容地耐力[kN/m2]","地耐力検定比"
                        };

                }
                var label_width = 105; var offset_x = 25; var offset_y = 25; var pitchy = 11; var text_width = 20; PdfPage page = new PdfPage(); page.Size = PageSize.A4;
                for (int e = 0; e < pressure.Count; e++)
                {
                    var e_text = e.ToString();
                    var B_text = B[e].ToString("F").Substring(0, Digit((int)(B[e])) + 3);
                    var t_text = T[e].ToString("F").Substring(0, Digit((int)(T[e])) + 3);
                    var L_text = L[e].ToString("F").Substring(0, Digit((int)(L[e])) + 3);
                    var ecc_text = Ecc[e].ToString("F").Substring(0, Digit((int)(Ecc[e])) + 3);
                    var A_text = A[e].ToString("F").Substring(0, Digit((int)(A[e])) + 3);
                    var Z_text = Z[e].ToString("F").Substring(0, Digit((int)(Z[e])) + 3);
                    var Rz_text = Rz[e].ToString("F").Substring(0, Digit((int)(Rz[e])) + 3);
                    var Sz_text = Sz[e].ToString("F").Substring(0, Digit((int)(Sz[e])) + 3);
                    var P_text = pressure[e].ToString("F").Substring(0, Digit((int)(pressure[e])) + 3);
                    var ac_text = Ac[e].ToString("F").Substring(0, Digit((int)(Ac[e])) + 3);
                    var l_text = LL[e].ToString("F").Substring(0, Digit((int)(LL[e])) + 3);
                    var M_text = M[e].ToString("F").Substring(0, Digit((int)(M[e])) + 3);
                    var Ma_text = Ma[e].ToString("F").Substring(0, Digit((int)(Ma[e])) + 3);
                    var at_text = At[e].ToString("F").Substring(0, Digit((int)(At[e])) + 3);
                    var ft_text = Ft[e].ToString("F").Substring(0, Digit((int)(Ft[e])) + 3);
                    var j_text = J[e].ToString("F").Substring(0, Digit((int)(J[e])) + 3);
                    var bar_text = bar[e];
                    var kentei2 = pressure[e] / Ac[e];
                    var k2_text = kentei2.ToString("F").Substring(0, 4) + "     "; var k2_color = new XSolidBrush(RGB((1 - Math.Min(kentei2, 1.0)) * 1.9 / 3.0, 1, 0.5));
                    var kentei = M[e] / Ma[e];
                    var k_text = kentei.ToString("F").Substring(0, 4) + "     "; var k_color = new XSolidBrush(RGB((1 - Math.Min(kentei, 1.0)) * 1.9 / 3.0, 1, 0.5));
                    var Rzx_text = (Rz[e] + Sz[e] + Rzx[e]).ToString("F10").Substring(0, Math.Min(((Rz[e] + Sz[e] + Rzx[e]).ToString().Length), Digit((int)(Rz[e] + Sz[e] + Rzx[e])) + 3));
                    var Rzy_text = (Rz[e] + Sz[e] + Rzy[e]).ToString("F10").Substring(0, Math.Min(((Rz[e] + Sz[e] + Rzy[e]).ToString().Length), Digit((int)(Rz[e] + Sz[e] + Rzy[e])) + 3));
                    var Rzx2_text = (Rz[e] + Sz[e] + Rzx2[e]).ToString("F10").Substring(0, Math.Min(((Rz[e] + Sz[e] + Rzx2[e]).ToString().Length), Digit((int)(Rz[e] + Sz[e] + Rzx2[e])) + 3));
                    var Rzy2_text = (Rz[e] + Sz[e] + Rzy2[e]).ToString("F10").Substring(0, Math.Min(((Rz[e] + Sz[e] + Rzy2[e]).ToString().Length), Digit((int)(Rz[e] + Sz[e] + Rzy2[e])) + 3));
                    var ukiagari = "なし";
                    if (Rz[e] + Sz[e] + Rzx[e] < 0 || Rz[e] + Sz[e] + Rzy[e] < 0|| Rz[e] + Sz[e] + Rzx2[e] < 0|| Rz[e] + Sz[e] + Rzy2[e] < 0) { ukiagari = "あり"; }
                    var values = new List<string>();
                    values.Add(e_text); values.Add(B_text); values.Add(t_text); values.Add(L_text); values.Add(ecc_text); values.Add(A_text); values.Add(Z_text); values.Add(Rz_text); values.Add(Sz_text); values.Add(P_text); values.Add(ac_text); values.Add(k2_text); values.Add(l_text); values.Add(M_text); values.Add(bar_text); values.Add(at_text); values.Add(ft_text); values.Add(j_text); values.Add(Ma_text);
                    values.Add(k_text);
                    if (option == 1) { values.Add(Rzx_text); values.Add(Rzy_text); values.Add(Rzx2_text); values.Add(Rzy2_text); values.Add(ukiagari); }
                    var slide = 0.0;
                    if (6 <= e % 18 && e % 18 < 12) { slide = pitchy * 25.5; }
                    if (12 <= e % 18 && e % 18 < 18) { slide = pitchy * 25.5 * 2; }
                    if ((e % 18) % 6 == 0)
                    {
                        // 空白ページを作成。
                        if ((e % 18) % (6 * 3) == 0) { page = document.AddPage(); gfx = XGraphics.FromPdfPage(page); }
                        // 描画するためにXGraphicsオブジェクトを取得。
                        for (int i = 0; i < labels.Count; i++)//ラベル列**************************************************************************
                        {
                            gfx.DrawLine(pen, offset_x, offset_y + pitchy * i + slide, offset_x + label_width, offset_y + pitchy * i + slide);//横線
                            gfx.DrawLine(pen, offset_x + label_width, offset_y + pitchy * i + slide, offset_x + label_width, offset_y + pitchy * (i + 1) + slide);//縦線
                            gfx.DrawString(labels[i], font, XBrushes.Black, new XRect(offset_x, offset_y + pitchy * i + slide, label_width, offset_y + pitchy * (i + 1) + slide), XStringFormats.TopCenter);
                            if (i == labels.Count - 1)
                            {
                                i += 1;
                                gfx.DrawLine(pen, offset_x, offset_y + pitchy * i + slide, offset_x + label_width, offset_y + pitchy * i + slide);//横線
                            }
                        }//***********************************************************************************************************************
                    }
                    for (int i = 0; i < labels.Count; i++)
                    {
                        var j = e % 6;
                        gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i + slide, offset_x + label_width + text_width * 3 * (j + 1), offset_y + pitchy * i + slide);//横線
                        gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i + slide, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * (i + 1) + slide);//縦線
                        if (i == labels.Count - 1)
                        {
                            gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * (i + 1) + slide, offset_x + label_width + text_width * 3 * (j + 1), offset_y + pitchy * (i + 1) + slide);//横線
                        }
                        var color = XBrushes.Black;
                        if (i == 11) {
                            color = k2_color;
                            if (kentei2 <= 1.0)
                            {
                                gfx.DrawString("    :O.K.", font, XBrushes.Black, new XRect(offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i + slide, text_width * 3, offset_y + pitchy * (i + 1) + slide), XStringFormats.TopCenter);
                            }
                            else
                            {
                                gfx.DrawString("    :N.G.", font, XBrushes.Black, new XRect(offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i + slide, text_width * 3, offset_y + pitchy * (i + 1) + slide), XStringFormats.TopCenter);
                            }
                        }
                        if (i == 19) {
                            color = k_color;
                            if (kentei <= 1.0)
                            {
                                gfx.DrawString("    :O.K.", font, XBrushes.Black, new XRect(offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i + slide, text_width * 3, offset_y + pitchy * (i + 1) + slide), XStringFormats.TopCenter);
                            }
                            else
                            {
                                gfx.DrawString("    :N.G.", font, XBrushes.Black, new XRect(offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i + slide, text_width * 3, offset_y + pitchy * (i + 1) + slide), XStringFormats.TopCenter);
                            }
                        }
                        if (values[i] == "なし") { color = XBrushes.Blue; }
                        if (values[i] == "あり") { color = XBrushes.Red; }
                        gfx.DrawString(values[i], font, color, new XRect(offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i + slide, text_width * 3, offset_y + pitchy * (i + 1) + slide), XStringFormats.TopCenter);
                    }
                }
                var filename = dir + "/" + pdfname + ".pdf";
                document.Save(filename);// ドキュメントを保存。
                Process.Start(new ProcessStartInfo(@filename) { UseShellExecute = true });
                ////支点番号と反力
                //document = new PdfDocument();
                //document.Info.Title = pdfname;
                //document.Info.Author = "Shinnosuke Fujita, Assoc. Prof., The Univ. of Kitakyushu";
                //labels = new List<string> { "基礎番号", "支点番号" };
                //RhinoApp.WriteLine("test0");
                //label_width = 50; text_width = 11;
                //page = new PdfPage(); page.Size = PageSize.A4;
                //RhinoApp.WriteLine("test01");
                //gfx = XGraphics.FromPdfPage(page);
                //RhinoApp.WriteLine("test02");
                //var numbers = B_number.Branches; var valuelist = new List<List<string>>(); var valuelist2 = new List<List<string>>();
                //var maxrange = -9999; var rz = Rz_all.Branches; var slide2 = 0.0;
                //RhinoApp.WriteLine("test1");
                //for (int e = 0; e < numbers.Count; e++)
                //{
                //    var vlist = new List<string>(); var rzlist = new List<string>();
                //    maxrange = Math.Max(maxrange, numbers[e].Count + 1); vlist.Add(e.ToString());
                //    for (int j = 0; j < numbers[e].Count; j++)
                //    {
                //        vlist.Add(((int)numbers[e][j].Value).ToString());
                //        rzlist.Add((Rz_all[e][j].Value).ToString("F").Substring(0, Math.Min(((Rz_all[e][j].Value).ToString("F")).Length, Digit((int)Rz_all[e][j].Value) + 2)));
                //    }
                //    valuelist.Add(vlist); valuelist2.Add(rzlist);
                //    RhinoApp.WriteLine(e.ToString());
                //    if (e % 12 == 0)
                //    {
                //        if (e != 0) { gfx.DrawLine(pen, offset_x, offset_y + pitchy * maxrange + slide2, offset_x + label_width + text_width * 3 * 12, offset_y + pitchy * maxrange + slide2); }
                //        slide2 += (maxrange + 1.5) * pitchy;
                //        RhinoApp.WriteLine("test2");
                //        if (slide2 + (maxrange) * pitchy >= pitchy * 23.5 * 3) { slide2 = 0; }
                //        RhinoApp.WriteLine("test3");
                //        if (slide2 == 0)
                //        { page = document.AddPage(); gfx = XGraphics.FromPdfPage(page); maxrange = 0; }
                //        RhinoApp.WriteLine("test4");
                //    }
                //    for (int i = 0; i < labels.Count; i++)//ラベル列**************************************************************************
                //    {
                //        gfx.DrawLine(pen, offset_x, offset_y + pitchy * i + slide2, offset_x + label_width, offset_y + pitchy * i + slide2);//横線
                //        gfx.DrawLine(pen, offset_x + label_width, offset_y + pitchy * i + slide2, offset_x + label_width, offset_y + pitchy * (i + 1) + slide2);//縦線
                //        gfx.DrawString(labels[i], font, XBrushes.Black, new XRect(offset_x, offset_y + pitchy * i + slide2, label_width, offset_y + pitchy * (i + 1) + slide2), XStringFormats.TopCenter);
                //    }
                //    for (int i = 0; i < valuelist[e].Count; i++)
                //    {
                //        var j = e % 12;
                //        gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i + slide2, offset_x + label_width + text_width * 3 * (j + 1), offset_y + pitchy * i + slide2);//横線
                //        gfx.DrawLine(pen, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * i + slide2, offset_x + label_width + text_width * 3 * j, offset_y + pitchy * (i + 1) + slide2);//縦線
                //    }
                //}
                //filename = dir + "/" + pdfname + "_number.pdf";
                //document.Save(filename);// ドキュメントを保存。
                //Process.Start(new ProcessStartInfo(@filename) { UseShellExecute = true });
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        { get { return OpenSeesUtility.Properties.Resources.stripbase; } }
        public override Guid ComponentGuid
        { get { return new Guid("4a2e6e4e-590f-42d0-88ee-7830013c67c2"); } }
        ///ここからカスタム関数群********************************************************************************
        private readonly List<Point3d> _pt = new List<Point3d>();
        private readonly List<string> _text = new List<string>();
        private readonly List<Brep> _s = new List<Brep>();
        private readonly List<Color> _c = new List<Color>();
        private readonly List<Color> _c2 = new List<Color>();
        protected override void BeforeSolveInstance()
        {
            _s.Clear();
            _pt.Clear();
            _text.Clear();
            _c.Clear();
            _c2.Clear();
        }
        public override void DrawViewportWires(IGH_PreviewArgs argments)
        {
            argments.Viewport.GetFrustumFarPlane(out Plane pln);
            RhinoViewport viewp = argments.Viewport;
            for (int i = 0; i < _s.Count; i++)
            {
                var material = new DisplayMaterial(_c[i], 0.5);
                argments.Display.DrawBrepShaded(_s[i], material);
            }
            for (int i = 0; i < _pt.Count; i++)
            {
                double size = fontsize;
                Point3d point = _pt[i];
                pln.Origin = point;
                viewp.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                var tt = _text[i];
                var H = TextHorizontalAlignment.Right; var V = TextVerticalAlignment.Top;
                if (_c2[i] == Color.Black) { V = TextVerticalAlignment.Bottom; }
                if (_c2[i] == Color.Blue) { H = TextHorizontalAlignment.Left; V = TextVerticalAlignment.Bottom; }
                if (_c2[i] == Color.Purple) { H = TextHorizontalAlignment.Left; }
                argments.Display.Draw3dText(tt, _c2[i], pln, size, "", false, false, H, V);
            }
                
        }
        ///ここからGUIの作成*****************************************************************************************
        public class CustomGUI : GH_ComponentAttributes
        {
            public CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle title_rec;
            private Rectangle radio_rec; private Rectangle radio_rec2;
            private Rectangle radio_rec_11; private Rectangle text_rec_11; private Rectangle radio_rec_13; private Rectangle text_rec_13;
            private Rectangle radio_rec_21; private Rectangle text_rec_21; private Rectangle radio_rec_22; private Rectangle text_rec_22; private Rectangle radio_rec_23; private Rectangle text_rec_23;
            private Rectangle radio_rec2_1; private Rectangle text_rec2_1;

            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int height = 71; int subwidth = 44; int radi1 = 7; int radi2 = 4;
                int pitchx = 6; int textheight = 20;
                global_rec.Height += height;
                title_rec = global_rec;
                title_rec.Y = title_rec.Bottom - height;
                title_rec.Height = 22;

                radio_rec = title_rec;
                radio_rec.Y += title_rec.Height;

                radio_rec_11 = radio_rec;
                radio_rec_11.X += radi2 - 1; radio_rec_11.Y = title_rec.Bottom + radi2;
                radio_rec_11.Height = radi1; radio_rec_11.Width = radi1;

                text_rec_11 = radio_rec_11;
                text_rec_11.X += pitchx; text_rec_11.Y -= radi2;
                text_rec_11.Height = textheight; text_rec_11.Width = subwidth*2;

                radio_rec_13 = text_rec_11;
                radio_rec_13.X += text_rec_11.Width - radi2; radio_rec_13.Y = radio_rec_11.Y;
                radio_rec_13.Height = radi1; radio_rec_13.Width = radi1;

                text_rec_13 = radio_rec_13;
                text_rec_13.X += pitchx; text_rec_13.Y -= radi2;
                text_rec_13.Height = textheight; text_rec_13.Width = subwidth;

                radio_rec_21 = radio_rec_11;
                radio_rec_21.Y += text_rec_11.Height - radi1;
                radio_rec_21.Height = radi1; radio_rec_11.Width = radi1;

                text_rec_21 = radio_rec_21;
                text_rec_21.X += pitchx; text_rec_21.Y -= radi2;
                text_rec_21.Height = textheight; text_rec_21.Width = subwidth;

                radio_rec_22 = text_rec_21;
                radio_rec_22.X += text_rec_21.Width - radi2; radio_rec_22.Y = radio_rec_21.Y;
                radio_rec_22.Height = radi1; radio_rec_22.Width = radi1;

                text_rec_22 = radio_rec_22;
                text_rec_22.X += pitchx; text_rec_22.Y -= radi2;
                text_rec_22.Height = textheight; text_rec_22.Width = subwidth;

                radio_rec_23 = text_rec_22;
                radio_rec_23.X += text_rec_22.Width - radi2; radio_rec_23.Y = radio_rec_22.Y;
                radio_rec_23.Height = radi1; radio_rec_23.Width = radi1;

                text_rec_23 = radio_rec_23;
                text_rec_23.X += pitchx; text_rec_23.Y -= radi2;
                text_rec_23.Height = textheight; text_rec_23.Width = subwidth + 30;

                radio_rec.Height = text_rec_23.Y + textheight - radio_rec.Y - radi2;
                
                radio_rec2 = radio_rec;
                radio_rec2.Y = radio_rec.Y + radio_rec.Height;
                radio_rec2.Height = textheight;

                radio_rec2_1 = radio_rec2;
                radio_rec2_1.X += 5; radio_rec2_1.Y += 5;
                radio_rec2_1.Height = radi1; radio_rec2_1.Width = radi1;

                text_rec2_1 = radio_rec2_1;
                text_rec2_1.X += pitchx; text_rec2_1.Y -= radi2;
                text_rec2_1.Height = textheight; text_rec2_1.Width = subwidth * 2;
                ///******************************************************************************************

                Bounds = global_rec;
            }
            Brush c11 = Brushes.White; Brush c13 = Brushes.White; Brush c21 = Brushes.White; Brush c22 = Brushes.White; Brush c23 = Brushes.White; Brush c2 = Brushes.White;
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
                    graphics.DrawString("Display Option", GH_FontServer.Standard, Brushes.White, textRectangle, format);

                    GH_Capsule radio = GH_Capsule.CreateCapsule(radio_rec, GH_Palette.White, 2, 0);
                    radio.Render(graphics, Selected, Owner.Locked, false); radio.Dispose();

                    GH_Capsule radio_11 = GH_Capsule.CreateCapsule(radio_rec_11, GH_Palette.Black, 5, 5);
                    radio_11.Render(graphics, Selected, Owner.Locked, false); radio_11.Dispose();
                    graphics.FillEllipse(c11, radio_rec_11);
                    graphics.DrawString("BaseShape", GH_FontServer.Standard, Brushes.Black, text_rec_11);

                    GH_Capsule radio_13 = GH_Capsule.CreateCapsule(radio_rec_13, GH_Palette.Black, 5, 5);
                    radio_13.Render(graphics, Selected, Owner.Locked, false); radio_13.Dispose();
                    graphics.FillEllipse(c13, radio_rec_13);
                    graphics.DrawString("No", GH_FontServer.Standard, Brushes.Black, text_rec_13);

                    GH_Capsule radio_21 = GH_Capsule.CreateCapsule(radio_rec_21, GH_Palette.Black, 5, 5);
                    radio_21.Render(graphics, Selected, Owner.Locked, false); radio_21.Dispose();
                    graphics.FillEllipse(c21, radio_rec_21);
                    graphics.DrawString("Width", GH_FontServer.Standard, Brushes.Black, text_rec_21);

                    GH_Capsule radio_22 = GH_Capsule.CreateCapsule(radio_rec_22, GH_Palette.Black, 5, 5);
                    radio_22.Render(graphics, Selected, Owner.Locked, false); radio_22.Dispose();
                    graphics.FillEllipse(c22, radio_rec_22);
                    graphics.DrawString("Thick", GH_FontServer.Standard, Brushes.Black, text_rec_22);

                    GH_Capsule radio_23 = GH_Capsule.CreateCapsule(radio_rec_23, GH_Palette.Black, 5, 5);
                    radio_23.Render(graphics, Selected, Owner.Locked, false); radio_23.Dispose();
                    graphics.FillEllipse(c23, radio_rec_23);
                    graphics.DrawString("N/A", GH_FontServer.Standard, Brushes.Black, text_rec_23);

                    GH_Capsule radio2 = GH_Capsule.CreateCapsule(radio_rec2, GH_Palette.White, 2, 0);
                    radio2.Render(graphics, Selected, Owner.Locked, false); radio2.Dispose();

                    GH_Capsule radio2_1 = GH_Capsule.CreateCapsule(radio_rec2_1, GH_Palette.Black, 5, 5);
                    radio2_1.Render(graphics, Selected, Owner.Locked, false); radio2_1.Dispose();
                    graphics.FillEllipse(c2, radio_rec2_1);
                    graphics.DrawString("PDF OUTPUT", GH_FontServer.Standard, Brushes.Black, text_rec2_1);
                    ///******************************************************************************************
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec11 = radio_rec_11; RectangleF rec13 = radio_rec_13;
                    RectangleF rec21 = radio_rec_21; RectangleF rec22 = radio_rec_22; RectangleF rec23 = radio_rec_23; RectangleF rec2 = radio_rec2_1;
                    if (rec11.Contains(e.CanvasLocation))
                    {
                        if (c11 == Brushes.Black) { c11 = Brushes.White; SetButton_for_StripFooting("c11", 0); }
                        else
                        { c11 = Brushes.Black; SetButton_for_StripFooting("c11", 1);}
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec13.Contains(e.CanvasLocation))
                    {
                        if (c13 == Brushes.Black) { c13 = Brushes.White; SetButton_for_StripFooting("c13", 0); }
                        else
                        { c13 = Brushes.Black; SetButton_for_StripFooting("c13", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec21.Contains(e.CanvasLocation))
                    {
                        if (c21 == Brushes.Black) { c21 = Brushes.White; SetButton_for_StripFooting("c21", 0); }
                        else
                        { c21 = Brushes.Black; SetButton_for_StripFooting("c21", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec22.Contains(e.CanvasLocation))
                    {
                        if (c22 == Brushes.Black) { c22 = Brushes.White; SetButton_for_StripFooting("c22", 0); }
                        else
                        { c22 = Brushes.Black; SetButton_for_StripFooting("c22", 1);}
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec23.Contains(e.CanvasLocation))
                    {
                        if (c23 == Brushes.Black) { c23 = Brushes.White; SetButton_for_StripFooting("c23", 0); }
                        else
                        { c23 = Brushes.Black; SetButton_for_StripFooting("c23", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec2.Contains(e.CanvasLocation))
                    {
                        if (c2 == Brushes.Black) { c2 = Brushes.White; SetButton_for_StripFooting("1", 0); }
                        else
                        { c2 = Brushes.Black; SetButton_for_StripFooting("1", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    ///*************************************************************************************************************************************************
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}