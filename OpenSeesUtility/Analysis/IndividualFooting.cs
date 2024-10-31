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

namespace OpenSeesUtility
{
    public class IndividualFooting : GH_Component
    {
        static int BaseShape = 0; static int BaseXxYxt = 0; static int BaseBar = 0; static int BaseP = 0;
        static int LongTerm = 1; static int ShortTerm = 0; static int Pkentei = 0; static int Qkentei = 0; static int Mkentei = 0;
        double fontsize = 10.0; int digit = 4; static int on_off = 0;
        public static PdfCreate.JapaneseFontResolver fontresolver = new PdfCreate.JapaneseFontResolver();
        public static XGraphics gfx;
        public static void SetButton_for_IndividualFooting(string s, int i)
        {
            if (s == "c11")
            {
                BaseShape = i;
            }
            else if (s == "c13")
            {
                BaseXxYxt = i;
            }
            else if (s == "c21")
            {
                BaseBar = i;
            }
            else if (s == "c22")
            {
                BaseP = i;
            }
            else if (s == "1")
            {
                on_off = i;
            }
            else if (s == "c0")
            {
                LongTerm = i;
            }
            else if (s == "c1")
            {
                ShortTerm = i;
            }
            else if (s == "c3")
            {
                Pkentei = i;
            }
            else if (s == "c4")
            {
                Qkentei = i;
            }
            else if (s == "c5")
            {
                Mkentei = i;
            }
        }
        public IndividualFooting()
          : base("IndividualFooting", "IndividualFooting",
              "read indivisual footing infomation and calc pressure load",
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
            pManager.AddNumberParameter("reac_f", "reac_f", "[[Node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddTextParameter("layer", "layer", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list, "bnds");
            pManager.AddTextParameter("name X x Y x t", "name X x Y x t", "name of base size[mm]", GH_ParamAccess.list, new List<string> { "X", "Y", "t" });
            pManager.AddTextParameter("name b x d", "name b x d", "name of column size[mm]", GH_ParamAccess.list, new List<string> { "b", "d" });
            pManager.AddTextParameter("name ex&ey", "name ex&ey", "name of eccentric distance[mm]", GH_ParamAccess.list, new List<string> { "ex", "ey" });
            pManager.AddTextParameter("name bar", "name bar", "name of steel bars", GH_ParamAccess.list, new List<string> { "barx", "bary" });
            pManager.AddTextParameter("name rho", "name rho", "name of unit volume weight[kN/m3]", GH_ParamAccess.item, "ρ");
            pManager.AddTextParameter("name Fc", "name Fc", "name of concrete compressive strength [N/mm2]", GH_ParamAccess.item, "Fc");
            pManager.AddTextParameter("name as", "name as", "name of long-term arrowable pressure [kN/m2]", GH_ParamAccess.item, "as");
            pManager.AddNumberParameter("FS", "FS", "font size for display texts", GH_ParamAccess.item, 10.0);///
            pManager.AddTextParameter("outputname", "outputname", "output file name", GH_ParamAccess.item, "IndividualBase");///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("X", "X", "[X1,X2...](DataList)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("Y", "Y", "[Y1,Y2...](DataList)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("A", "A", "[A1,A2...](DataList)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("PL", "PL", "[PL1,PL2...](DataList)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("PS", "PS", "[PS1,PS2...](DataList)", GH_ParamAccess.list);///
            pManager.AddBrepParameter("Shape", "Shape", "Base shape", GH_ParamAccess.list);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int Digit(int num)//数字の桁数を求める関数
            {
                // Mathf.Log10(0)はNegativeInfinityを返すため、別途処理する。
                return (num == 0) ? 1 : ((int)Math.Log10(num) + 1);
            }
            XColor RGB(double h, double s, double l)//convert HSL to RGB
            {
                var max = 0.0; var min = 0.0; var R = 0.0; var g = 0.0; var bb = 0.0;
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
                    bb = min;
                }
                else if (q <= 2)
                {
                    R = ((hp * 2 - h) / hp) * (max - min) + min;
                    g = max;
                    bb = min;
                }
                else if (q <= 3)
                {
                    R = min;
                    g = max;
                    bb = ((h - hp * 2) / hp) * (max - min) + min;
                }
                else if (q <= 4)
                {
                    R = min;
                    g = ((hp * 4 - h) / hp) * (max - min) + min;
                    bb = max;
                }
                else if (q <= 5)
                {
                    R = ((h - hp * 4) / hp) * (max - min) + min;
                    g = min;
                    bb = max;
                }
                else
                {
                    R = max;
                    g = min;
                    bb = ((HUE_MAX - h) / hp) * (max - min) + min;
                }
                R *= RGB_MAX; g *= RGB_MAX; bb *= RGB_MAX;
                return XColor.FromArgb((int)R, (int)g, (int)bb);
            }
            Vector3d rotation(Vector3d a, Vector3d bb, double theta)
            {
                double rad = theta * Math.PI / 180;
                double s = Math.Sin(rad); double c = Math.Cos(rad);
                bb /= Math.Sqrt(Vector3d.Multiply(bb, bb));
                double b1 = bb[0]; double b2 = bb[1]; double b3 = bb[2];
                Vector3d m1 = new Vector3d(c + Math.Pow(b1, 2) * (1 - c), b1 * b2 * (1 - c) - b3 * s, b1 * b3 * (1 - c) + b2 * s);
                Vector3d m2 = new Vector3d(b2 * b1 * (1 - c) + b3 * s, c + Math.Pow(b2, 2) * (1 - c), b2 * b3 * (1 - c) - b1 * s);
                Vector3d m3 = new Vector3d(b3 * b1 * (1 - c) - b2 * s, b3 * b2 * (1 - c) + b1 * s, c + Math.Pow(b3, 2) * (1 - c));
                return new Vector3d(Vector3d.Multiply(m1, a), Vector3d.Multiply(m2, a), Vector3d.Multiply(m3, a));
            }
            var doc = RhinoDoc.ActiveDoc;
            DA.GetDataTree("R", out GH_Structure<GH_Number> _r); var r = _r.Branches;
            DA.GetDataTree("reac_f", out GH_Structure<GH_Number> _reac_f); var reac_f = _reac_f.Branches; var m = reac_f.Count;
            List<string> layer = new List<string>(); var nameXxYxt = new List<string> { "X", "Y", "t" }; var namebxd = new List<string> { "b", "d" }; var namerho = "ρ"; var namee = new List<string> { "ex", "ey" }; var namebar = new List<string> { "barx", "bary" }; var nameFc = "Fc"; var nameac = "as";
            DA.GetDataList("layer", layer); DA.GetDataList("name X x Y x t", nameXxYxt); DA.GetDataList("name b x d", namebxd); DA.GetData("name rho", ref namerho); DA.GetDataList("name ex&ey", namee); DA.GetDataList("name bar", namebar); DA.GetData("name as", ref nameac); DA.GetData("name Fc", ref nameFc);
            DA.GetData("FS", ref fontsize);
            var pdfname = "IndividualBase"; DA.GetData("outputname", ref pdfname);

            var PL = new List<double>(); var PS = new List<double>(); 
            var B = new List<double>(); var D = new List<double>(); var t = new List<double>(); var b = new List<double>(); var d = new List<double>(); var ex = new List<double>(); var ey = new List<double>(); var Rz = new List<List<double>>(); var Sz = new List<double>(); var A = new List<double>(); var rho = new List<double>(); var Fc = new List<double>(); var PaL = new List<double>(); var PaS = new List<double>(); var barx = new List<string>(); var bary = new List<string>(); var number = new List<int>(); var RMx = new List<List<double>>(); var RMy = new List<List<double>>(); var Zx = new List<double>(); var Zy = new List<double>(); var Lx = new List<double>(); var Ly = new List<double>(); var nx = new List<double>(); var ny = new List<double>(); var Dx = new List<double>(); var Dy = new List<double>(); var ftLx = new List<double>(); var ftLy = new List<double>(); var ftSx = new List<double>(); var ftSy = new List<double>(); var QxL = new List<double>(); var QxS = new List<double>(); var QyL = new List<double>(); var QyS = new List<double>(); var J = new List<double>(); var QaxL = new List<double>(); var QaxS = new List<double>(); var QayL = new List<double>(); var QayS = new List<double>(); var fsL = new List<double>(); var fsS = new List<double>(); var MxL = new List<double>(); var MxS = new List<double>(); var MyL = new List<double>(); var MyS = new List<double>(); var MaxL = new List<double>(); var MaxS = new List<double>(); var MayL = new List<double>(); var MayS = new List<double>();
            var angle = new List<double>();
            for (int i = 0; i < layer.Count; i++)
            {
                var point = doc.Objects.FindByLayer(layer[i]);
                for (int j = 0; j < point.Length; j++)
                {
                    var obj = point[j]; var p = (new ObjRef(obj)).Point().Location; int n = 0;
                    for (int k = 0; k < reac_f.Count; k++)
                    {
                        n = (int)reac_f[k][0].Value;
                        if (Math.Abs(p[0] - r[n][0].Value) < 5e-3 && Math.Abs(p[1] - r[n][1].Value) < 5e-3 && Math.Abs(p[2] - r[n][2].Value) < 5e-3)
                        {
                            number.Add(n);
                            if (reac_f[k].Count == 7)
                            {
                                Rz.Add(new List<double> { reac_f[k][3].Value, 0, 0, 0, 0 });
                                RMx.Add(new List<double> { reac_f[k][4].Value, 0, 0, 0, 0 });
                                RMy.Add(new List<double> { reac_f[k][5].Value, 0, 0, 0, 0 });
                            }
                            else if (reac_f[k].Count == 21)
                            {
                                Rz.Add(new List<double> { reac_f[k][3].Value, reac_f[k][10].Value, reac_f[k][17].Value, -reac_f[k][10].Value, -reac_f[k][17].Value });
                                RMx.Add(new List<double> { reac_f[k][4].Value, reac_f[k][11].Value, reac_f[k][18].Value, -reac_f[k][11].Value, -reac_f[k][18].Value });
                                RMy.Add(new List<double> { reac_f[k][5].Value, reac_f[k][12].Value, reac_f[k][19].Value, -reac_f[k][12].Value, -reac_f[k][19].Value });
                            }
                            else if (reac_f[k].Count == 34)
                            {
                                Rz.Add(new List<double> { reac_f[k][3].Value, reac_f[k][10].Value, reac_f[k][17].Value, reac_f[k][24].Value, reac_f[k][31].Value });
                                RMx.Add(new List<double> { reac_f[k][4].Value, reac_f[k][11].Value, reac_f[k][18].Value, reac_f[k][25].Value, reac_f[k][32].Value });
                                RMy.Add(new List<double> { reac_f[k][5].Value, reac_f[k][12].Value, reac_f[k][19].Value, reac_f[k][26].Value, reac_f[k][33].Value });
                            }
                            break;
                        }
                    }
                    if (obj.Attributes.GetUserString(nameXxYxt[0]) != null && obj.Attributes.GetUserString(nameXxYxt[1]) != null && obj.Attributes.GetUserString(nameXxYxt[2]) != null && obj.Attributes.GetUserString(namebxd[0]) != null && obj.Attributes.GetUserString(namebxd[1]) != null)
                    {
                        B.Add(float.Parse(obj.Attributes.GetUserString(nameXxYxt[0]))); D.Add(float.Parse(obj.Attributes.GetUserString(nameXxYxt[1]))); t.Add(float.Parse(obj.Attributes.GetUserString(nameXxYxt[2])));
                        b.Add(float.Parse(obj.Attributes.GetUserString(namebxd[0]))); d.Add(float.Parse(obj.Attributes.GetUserString(namebxd[1])));
                        if (obj.Attributes.GetUserString(namee[0]) == null) { ex.Add(0.0); }
                        else { ex.Add(float.Parse(obj.Attributes.GetUserString(namee[0]))); }

                        if (obj.Attributes.GetUserString(namee[1]) == null) { ey.Add(0.0); }
                        else { ey.Add(float.Parse(obj.Attributes.GetUserString(namee[1]))); }

                        if (obj.Attributes.GetUserString(namerho) == null) { rho.Add(24.0); }
                        else { rho.Add(float.Parse(obj.Attributes.GetUserString(namerho))); }

                        if (obj.Attributes.GetUserString(nameac) == null) { PaL.Add(30.0); }
                        else { PaL.Add(float.Parse(obj.Attributes.GetUserString(nameac))); }

                        if (obj.Attributes.GetUserString(nameFc) == null) { Fc.Add(24.0); }
                        else { Fc.Add(float.Parse(obj.Attributes.GetUserString(nameFc))); }

                        if (obj.Attributes.GetUserString(namebar[0]) == null) { barx.Add(""); }
                        else { barx.Add(obj.Attributes.GetUserString(namebar[0])); }

                        if (obj.Attributes.GetUserString(namebar[1]) == null) { bary.Add(""); }
                        else { bary.Add(obj.Attributes.GetUserString(namebar[1])); }

                        if (obj.Attributes.GetUserString("angle") == null) { angle.Add(0); }
                        else { angle.Add(float.Parse(obj.Attributes.GetUserString("angle"))); }
                    }
                }
            }
            for (int i = 0; i < B.Count; i++)
            {
                A.Add(B[i] * D[i] / 1e+6);//[m2]
                Zx.Add(B[i] * Math.Pow(D[i], 2) / 6.0 / 1e+9); Zy.Add(D[i] * Math.Pow(B[i], 2) / 6.0 / 1e+9);//[m3]
                Lx.Add((B[i] - b[i]) / 2.0 + Math.Abs(ex[i])); Ly.Add((D[i] - d[i]) / 2.0 + Math.Abs(ey[i]));//[mm]
                nx.Add(float.Parse(barx[i].Substring(0,barx[i].IndexOf("-")))); ny.Add(float.Parse(bary[i].Substring(0, bary[i].IndexOf("-"))));
                Dx.Add(float.Parse(barx[i].Substring(barx[i].IndexOf("-") + 2))); Dy.Add(float.Parse(bary[i].Substring(bary[i].IndexOf("-") + 2)));//[mm]
                J.Add((t[i] - 60 - Dx[i] - Dy[i] / 2.0) * 7.0 / 8.0);//[mm]
                ftLx.Add(195.0); ftLy.Add(195.0);
                if (Dx[i] > 18.9 && Dx[i] < 28.9) { ftLx[i] = 215.0; }
                if (Dy[i] > 18.9 && Dy[i] < 28.9) { ftLy[i] = 215.0; }
                ftSx.Add(295.0); ftSy.Add(295.0);
                if (Dx[i] > 18.9 && Dx[i] < 28.9) { ftSx[i] = 345.0; }
                else if (Dx[i] > 28.9) { ftSx[i] = 390.0; }
                if (Dy[i] > 18.9 && Dy[i] < 28.9) { ftSy[i] = 345.0; }
                else if (Dy[i] > 28.9) { ftSy[i] = 390.0; }
                Sz.Add(A[i] * t[i] / 1e+3 * rho[i]);//[kN]
                PL.Add((Rz[i][0] + Sz[i]) / A[i] + Math.Abs(RMx[i][0]) / Zx[i] + Math.Abs(RMy[i][0]) / Zy[i]);//[kN/m2]
                QxL.Add(Lx[i] * D[i] / 1e+6 * PL[i]); QyL.Add(Ly[i] * B[i] / 1e+6 * PL[i]);//[kN]
                fsL.Add(Math.Min(Fc[i] / 10.0, 0.49 + Fc[i] / 100.0)); fsS.Add(fsL[i] * 1.5);//[N/mm2]
                QaxL.Add(fsL[i] * D[i] * J[i] / 1e+3); QayL.Add(fsL[i] * B[i] * J[i] / 1e+3);//[kN]
                MxL.Add(PL[i] / 2.0 * D[i] / 1e+3 * Math.Pow(Lx[i] / 1e+3, 2)); MyL.Add(PL[i] / 2.0 * B[i] / 1e+3 * Math.Pow(Ly[i] / 1e+3, 2));//[kNm]
                var ax = nx[i] * Math.Pow(Dx[i], 2) * Math.PI / 4.0; var ay = ny[i] * Math.Pow(Dy[i], 2) * Math.PI / 4.0;
                MaxL.Add(ax * ftLx[i] * J[i] / 1e+6); MayL.Add(ay * ftLy[i] * J[i] / 1e+6);//[kNm]
                if (reac_f[0].Count >= 21)
                {
                    var px1 = (Rz[i][0] + Sz[i] + Rz[i][1]) / A[i] + (RMx[i][0] + RMx[i][1]) / Zx[i] + (RMy[i][0] + RMy[i][1]) / Zy[i];
                    var py1 = (Rz[i][0] + Sz[i] + Rz[i][2]) / A[i] + (RMx[i][0] + RMx[i][2]) / Zx[i] + (RMy[i][0] + RMy[i][2]) / Zy[i];
                    var px2 = (Rz[i][0] + Sz[i] + Rz[i][3]) / A[i] + (RMx[i][0] + RMx[i][3]) / Zx[i] + (RMy[i][0] + RMy[i][3]) / Zy[i];
                    var py2 = (Rz[i][0] + Sz[i] + Rz[i][4]) / A[i] + (RMx[i][0] + RMx[i][4]) / Zx[i] + (RMy[i][0] + RMy[i][4]) / Zy[i];
                    PS.Add(Math.Max(Math.Max(px1, py1), Math.Max(px2, py2))); PaS.Add(PaL[i] * 2);
                    QxS.Add(Lx[i] * D[i] / 1e+6 * PS[i]); QyS.Add(Ly[i] * B[i] / 1e+6 * PS[i]);
                    QaxS.Add(fsS[i] * D[i] * J[i] / 1e+3); QayS.Add(fsS[i] * B[i] * J[i] / 1e+3);//[kN]
                    MxS.Add(PS[i] / 2.0 * D[i] / 1e+3 * Math.Pow(Lx[i] / 1e+3, 2)); MyS.Add(PS[i] / 2.0 * B[i] / 1e+3 * Math.Pow(Ly[i] / 1e+3, 2));//[kNm]
                    MaxS.Add(ax * ftSx[i] * J[i] / 1e+6); MayS.Add(ay * ftSy[i] * J[i] / 1e+6);//[kNm]
                }
                var rc = new Point3d(r[number[i]][0].Value - ex[i] / 1e+3, r[number[i]][1].Value - ey[i] / 1e+3, r[number[i]][2].Value);
                if (BaseShape == 1)
                {
                    Random rand1 = new Random((int)(B[i] * 1000)); Random rand2 = new Random((int)(D[i] * 2000)); Random rand3 = new Random((int)(t[i] * 3000));
                    _c.Add(Color.FromArgb(rand1.Next(0, 256), rand2.Next(0, 256), rand3.Next(0, 256)));
                    var p1 = rc + new Vector3d(-B[i]/2e+3, D[i]/2e+3, 0);
                    var p2 = rc + new Vector3d(B[i] / 2e+3, D[i] / 2e+3, 0);
                    var p3 = rc + new Vector3d(B[i] / 2e+3, -D[i] / 2e+3, 0);
                    var p4 = rc + new Vector3d(-B[i] / 2e+3, -D[i] / 2e+3, 0);
                    if (angle[i] != 0)
                    {
                        p1 = rc + rotation(p1 - rc, new Vector3d(0, 0, 1), angle[i]);
                        p2 = rc + rotation(p2 - rc, new Vector3d(0, 0, 1), angle[i]);
                        p3 = rc + rotation(p3 - rc, new Vector3d(0, 0, 1), angle[i]);
                        p4 = rc + rotation(p4 - rc, new Vector3d(0, 0, 1), angle[i]);
                    }
                    var brep = Brep.CreatePlanarBreps(new Polyline(new List<Point3d> { p1, p2, p3, p4, p1 }).ToNurbsCurve(), 0.001)[0];
                    _s.Add(brep);
                }
                if (BaseXxYxt == 1)
                {
                    _pt.Add(new Point3d(rc));
                    _text.Add(Math.Round(B[i],0).ToString()+"x"+ Math.Round(D[i], 0).ToString()+"x"+ Math.Round(t[i], 0).ToString());
                    _c2.Add(Color.Black);
                }
                if (BaseP == 1 && LongTerm == 1)
                {
                    _pt.Add(new Point3d(rc));
                    _text.Add(PL[i].ToString("F6").Substring(0, digit) + "kN/m2");
                    _c2.Add(Color.Red);
                }
                if (BaseP == 1 && ShortTerm == 1)
                {
                    _pt.Add(new Point3d(rc));
                    _text.Add(PS[i].ToString("F6").Substring(0, digit) + "kN/m2");
                    _c2.Add(Color.Purple);
                }
                if (BaseBar == 1)
                {
                    _pt.Add(new Point3d(rc));
                    _text.Add(" X:"+barx[i] + " \r \n Y:" + bary[i]);
                    _c2.Add(Color.Blue);
                }
                if (LongTerm == 1 && Pkentei == 1)
                {
                    _pt.Add(new Point3d(rc));
                    var k = PL[i] / PaL[i];
                    _text.Add(k.ToString("F6").Substring(0, digit));
                    _c2.Add(new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5));
                }
                if (ShortTerm == 1 && Pkentei == 1)
                {
                    _pt.Add(new Point3d(rc));
                    var k = PS[i] / PaS[i];
                    _text.Add(k.ToString("F6").Substring(0, digit));
                    _c2.Add(new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5));
                }
                if (LongTerm == 1 && Qkentei == 1)
                {
                    _pt.Add(new Point3d(rc));
                    var k = Math.Max(QxL[i] / QaxL[i], QyL[i] / QayL[i]);
                    _text.Add(k.ToString("F6").Substring(0, digit));
                    _c2.Add(new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5));
                }
                if (ShortTerm == 1 && Qkentei == 1)
                {
                    _pt.Add(new Point3d(rc));
                    var k = Math.Max(QxS[i] / QaxS[i], QyS[i] / QayS[i]);
                    _text.Add(k.ToString("F6").Substring(0, digit));
                    _c2.Add(new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5));
                }
                if (LongTerm == 1 && Mkentei == 1)
                {
                    _pt.Add(new Point3d(rc));
                    var k = Math.Max(MxL[i] / MaxL[i], MyL[i] / MayL[i]);
                    _text.Add(k.ToString("F6").Substring(0, digit));
                    _c2.Add(new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5));
                }
                if (ShortTerm == 1 && Mkentei == 1)
                {
                    _pt.Add(new Point3d(rc));
                    var k = Math.Max(MxS[i] / MaxS[i], MyS[i] / MayS[i]);
                    _text.Add(k.ToString("F6").Substring(0, digit));
                    _c2.Add(new ColorHSL((1 - Math.Min(k, 1.0)) * 1.9 / 3.0, 1, 0.5));
                }
            }
            DA.SetDataList("X", B); DA.SetDataList("Y", D); DA.SetDataList("A", A); DA.SetDataList("PL", PL); DA.SetDataList("PS", PS); DA.SetDataList("Shape", _s);
            //pdf作成
            if (on_off == 1)
            {
                // フォントリゾルバーのグローバル登録
                if (PdfCreate.JapaneseFontResolver.fontset == 0) { PdfSharp.Fonts.GlobalFontSettings.FontResolver = fontresolver; PdfCreate.JapaneseFontResolver.fontset = 1; }
                // PDFドキュメントを作成。
                PdfDocument document = new PdfDocument();
                document.Info.Title = pdfname;
                document.Info.Author = "Shinnosuke Fujita, Assoc. Prof., The Univ. of Kitakyushu";
                // フォントを作成。
                XFont font = new XFont("Gen Shin Gothic", 9, XFontStyle.Regular);
                XFont fontbold = new XFont("Gen Shin Gothic", 9, XFontStyle.Bold);
                var pen = XPens.Black;
                var labels = new List<string>
                        {
                           "Node No.", "検討方向", "B x D [mm]", "t[mm]", "b x d [mm]", "ex, ey [mm]", "Lx, Ly [mm]", "配筋", "ρ[kN/m3]", "W[kN]", "Zx, Zy [m3]", "長期N[kN]", "+X荷重時N[kN]", "+Y荷重時N[kN]", "-X荷重時N[kN]", "-Y荷重時N[kN]", "長期M[kNm]", "+X荷重時M[kNm]", "+Y荷重時M[kNm]", "-X荷重時M[kNm]", "-Y荷重時M[kNm]","","PL[kN/m2]","PaL[kN/m2]","検定比","判定", "fs[N/mm2]", "QL[kN]", "QaL[kN]","検定比","判定", "ft[N/mm2]", "ML[kN]", "MaL[kN]","検定比","判定"
                        };
                if (PS.Count != 0)
                {
                    labels = new List<string>
                        {
                           "Node No.", "検討方向", "B x D [mm]", "t[mm]", "b x d [mm]", "ex, ey [mm]", "Lx, Ly [mm]", "配筋", "ρ[kN/m3]", "W[kN]", "Zx, Zy [m3]", "長期N[kN]", "+X荷重時N[kN]", "+Y荷重時N[kN]", "-X荷重時N[kN]", "-Y荷重時N[kN]", "長期M[kNm]", "+X荷重時M[kNm]", "+Y荷重時M[kNm]", "-X荷重時M[kNm]", "-Y荷重時M[kNm]","","PL[kN/m2]","PaL[kN/m2]","検定比","判定", "fs[N/mm2]", "QL[kN]", "QaL[kN]","検定比","判定", "ft[N/mm2]", "ML[kN]", "MaL[kN]","検定比","判定","","PS[kN/m2]","PaS[kN/m2]","検定比","判定", "fs[N/mm2]", "QS[kN]", "QaS[kN]","検定比","判定", "ft[N/mm2]", "MS[kN]", "MaS[kN]","検定比","判定"
                        };
                }
                var label_width = 85; var offset_x = 25; var offset_y = 25; var pitchy = 13; var text_width = 45; PdfPage page = new PdfPage(); page.Size = PageSize.A4;
                for (int e = 0; e < PL.Count; e++)
                {
                    var flag1 = "O.K."; var flag2 = "O.K."; var flag3 = "O.K."; var flag4 = "O.K."; var flag5 = "O.K.";
                    if (PL[e] / PaL[e] > 1) { flag1 = "N.G."; }
                    if (QxL[e] / QaxL[e] > 1) { flag2 = "N.G."; }
                    if (QyL[e] / QayL[e] > 1) { flag3 = "N.G."; }
                    if (MxL[e] / MaxL[e] > 1) { flag4 = "N.G."; }
                    if (MyL[e] / MayL[e] > 1) { flag5 = "N.G."; }
                    var values = new List<List<string>>(); var colors = new List<List<XSolidBrush>>();
                    values.Add(new List<string> { number[e].ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                    values.Add(new List<string>{ "X","Y" }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(B[e],0).ToString(), Math.Round(D[e], 0).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(t[e],0).ToString()}); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(b[e],0).ToString(), Math.Round(d[e], 0).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(ex[e],0).ToString(), Math.Round(ey[e], 0).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(Lx[e],0).ToString(), Math.Round(Ly[e], 0).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ barx[e],bary[e] }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(rho[e],1).ToString()}); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(Sz[e],1).ToString()}); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(Zx[e],5).ToString(), Math.Round(Zy[e], 5).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(Rz[e][0],2).ToString()}); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(Rz[e][1],2).ToString()}); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(Rz[e][2],2).ToString()}); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(Rz[e][3],2).ToString()}); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(Rz[e][4],2).ToString()}); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(RMx[e][0], 2).ToString(), Math.Round(RMy[e][0], 2).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(RMx[e][1], 2).ToString(), Math.Round(RMy[e][1], 2).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(RMx[e][2], 2).ToString(), Math.Round(RMy[e][2], 2).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(RMx[e][3], 2).ToString(), Math.Round(RMy[e][3], 2).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(RMx[e][4], 2).ToString(), Math.Round(RMy[e][4], 2).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ "長期検討" }); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(PL[e], 1).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(PaL[e], 1).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(PL[e]/PaL[e], 2).ToString() }); colors.Add(new List<XSolidBrush> { new XSolidBrush(RGB((1 - Math.Min(PL[e] / PaL[e], 1.0)) * 1.9 / 3.0, 1, 0.5)) });
                    values.Add(new List<string> { flag1 }); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(fsL[e], 2).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(QxL[e], 2).ToString(), Math.Round(QyL[e], 2).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(QaxL[e], 2).ToString(), Math.Round(QayL[e], 2).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(QxL[e]/QaxL[e], 2).ToString(), Math.Round(QyL[e] / QayL[e], 2).ToString() }); colors.Add(new List<XSolidBrush> { new XSolidBrush(RGB((1 - Math.Min(QxL[e] / QaxL[e], 1.0)) * 1.9 / 3.0, 1, 0.5)), new XSolidBrush(RGB((1 - Math.Min(QyL[e] / QayL[e], 1.0)) * 1.9 / 3.0, 1, 0.5)) });
                    values.Add(new List<string> { flag2, flag3 }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(ftLx[e], 0).ToString(), Math.Round(ftLy[e], 0).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(MxL[e], 2).ToString(), Math.Round(MyL[e], 2).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(MaxL[e], 2).ToString(), Math.Round(MayL[e], 2).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    values.Add(new List<string>{ Math.Round(MxL[e]/MaxL[e], 2).ToString(), Math.Round(MyL[e] / MayL[e], 2).ToString() }); colors.Add(new List<XSolidBrush> { new XSolidBrush(RGB((1 - Math.Min(MxL[e] / MaxL[e], 1.0)) * 1.9 / 3.0, 1, 0.5)), new XSolidBrush(RGB((1 - Math.Min(MyL[e] / MayL[e], 1.0)) * 1.9 / 3.0, 1, 0.5)) });
                    values.Add(new List<string> { flag4, flag5 }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    if (PS.Count != 0)
                    {
                        flag1 = "O.K."; flag2 = "O.K."; flag3 = "O.K."; flag4 = "O.K."; flag5 = "O.K.";
                        if (PS[e] / PaS[e] > 1) { flag1 = "N.G."; }
                        if (QxS[e] / QaxS[e] > 1) { flag2 = "N.G."; }
                        if (QyS[e] / QayS[e] > 1) { flag3 = "N.G."; }
                        if (MxS[e] / MaxS[e] > 1) { flag4 = "N.G."; }
                        if (MyS[e] / MayS[e] > 1) { flag5 = "N.G."; }
                        values.Add(new List<string> { "短期検討" }); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                        values.Add(new List<string> { Math.Round(PS[e], 1).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                        values.Add(new List<string> { Math.Round(PaS[e], 1).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                        values.Add(new List<string> { Math.Round(PS[e] / PaS[e], 2).ToString() }); colors.Add(new List<XSolidBrush> { new XSolidBrush(RGB((1 - Math.Min(PS[e] / PaS[e], 1.0)) * 1.9 / 3.0, 1, 0.5)) });
                        values.Add(new List<string> { flag1 }); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                        values.Add(new List<string> { Math.Round(fsS[e], 2).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black });
                        values.Add(new List<string> { Math.Round(QxS[e], 2).ToString(), Math.Round(QyS[e], 2).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                        values.Add(new List<string> { Math.Round(QaxS[e], 2).ToString(), Math.Round(QayS[e], 2).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                        values.Add(new List<string> { Math.Round(QxS[e] / QaxS[e], 2).ToString(), Math.Round(QyS[e] / QayS[e], 2).ToString() }); colors.Add(new List<XSolidBrush> { new XSolidBrush(RGB((1 - Math.Min(QxS[e] / QaxS[e], 1.0)) * 1.9 / 3.0, 1, 0.5)), new XSolidBrush(RGB((1 - Math.Min(QyS[e] / QayS[e], 1.0)) * 1.9 / 3.0, 1, 0.5)) });
                        values.Add(new List<string> { flag2, flag3 }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                        values.Add(new List<string> { Math.Round(ftSx[e], 0).ToString(), Math.Round(ftSy[e], 0).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                        values.Add(new List<string> { Math.Round(MxS[e], 2).ToString(), Math.Round(MyS[e], 2).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                        values.Add(new List<string> { Math.Round(MaxS[e], 2).ToString(), Math.Round(MayS[e], 2).ToString() }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                        values.Add(new List<string> { Math.Round(MxS[e] / MaxS[e], 2).ToString(), Math.Round(MyS[e] / MayS[e], 2).ToString() }); colors.Add(new List<XSolidBrush> { new XSolidBrush(RGB((1 - Math.Min(MxS[e] / MaxS[e], 1.0)) * 1.9 / 3.0, 1, 0.5)), new XSolidBrush(RGB((1 - Math.Min(MyS[e] / MayS[e], 1.0)) * 1.9 / 3.0, 1, 0.5)) });
                        values.Add(new List<string> { flag4, flag5 }); colors.Add(new List<XSolidBrush> { XBrushes.Black, XBrushes.Black });
                    }
                    if (e % 5 == 0)
                    {
                        page = document.AddPage(); gfx = XGraphics.FromPdfPage(page);
                        for (int i = 0; i < labels.Count; i++)
                        {
                            gfx.DrawLine(pen, offset_x, offset_y + pitchy * i, offset_x + label_width, offset_y + pitchy * i);//横線
                            gfx.DrawLine(pen, offset_x + label_width, offset_y + pitchy * i, offset_x + label_width, offset_y + pitchy * (i + 1));//縦線
                            gfx.DrawString(labels[i], font, XBrushes.Black, new XRect(offset_x, offset_y + pitchy * i, label_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                            if (i == labels.Count - 1)
                            {
                                i += 1;
                                gfx.DrawLine(pen, offset_x, offset_y + pitchy * i, offset_x + label_width, offset_y + pitchy * i);//横線
                            }
                        }
                    }
                    var j = e % 5;
                    for (int i = 0; i < values.Count; i++)
                    {
                        gfx.DrawLine(pen, offset_x + label_width + text_width * 2 * j, offset_y + pitchy * i, offset_x + label_width + text_width * 2 * (j + 1), offset_y + pitchy * i);//横線
                        gfx.DrawLine(pen, offset_x + label_width + text_width * 2 * j, offset_y + pitchy * i, offset_x + label_width + text_width * 2 * j, offset_y + pitchy * (i + 1));//縦線
                        if (values[i].Count == 1)
                        {
                            gfx.DrawString(values[i][0], font, colors[i][0], new XRect(offset_x + label_width + text_width * (2 * j + 0.5), offset_y + pitchy * i, text_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                        }
                        else
                        {
                            gfx.DrawString(values[i][0], font, colors[i][0], new XRect(offset_x + label_width + text_width * (2 * j), offset_y + pitchy * i, text_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                            gfx.DrawString(values[i][1], font, colors[i][1], new XRect(offset_x + label_width + text_width * (2 * j + 1), offset_y + pitchy * i, text_width, offset_y + pitchy * (i + 1)), XStringFormats.TopCenter);
                        }
                        if (i == values.Count - 1)
                        {
                            gfx.DrawLine(pen, offset_x + label_width + text_width * 2 * j, offset_y + pitchy * (i + 1), offset_x + label_width + text_width * 2 * (j + 1), offset_y + pitchy * (i + 1));//横線
                        }
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
                return OpenSeesUtility.Properties.Resources.Indivisualfooting;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("f7e916c3-8630-4a19-a5e4-c43b6ca81c06"); }
        }
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
                var H = TextHorizontalAlignment.Left; var V = TextVerticalAlignment.Bottom;
                if (_c2[i] == Color.Black) { H = TextHorizontalAlignment.Right; }
                if (_c2[i] == Color.Blue) { H = TextHorizontalAlignment.Right; V = TextVerticalAlignment.Top; }
                if (_c2[i] == Color.Red || _c2[i] == Color.Purple) { V = TextVerticalAlignment.Top; }
                argments.Display.Draw3dText(tt, _c2[i], pln, size, "", false, false, H, V);
            }

        }
        ///ここからGUIの作成*****************************************************************************************
        public class CustomGUI : GH_ComponentAttributes
        {
            public CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle title_rec; private Rectangle title_rec0; private Rectangle radio_rec0; private Rectangle title_rec1; private Rectangle radio_rec1;
            private Rectangle radio_rec; private Rectangle radio_rec2;
            private Rectangle radio_rec_1; private Rectangle radio_rec_2; private Rectangle text_rec_1; private Rectangle text_rec_2;
            private Rectangle radio_rec_3; private Rectangle text_rec_3; private Rectangle radio_rec_4; private Rectangle text_rec_4; private Rectangle radio_rec_5; private Rectangle text_rec_5;
            private Rectangle radio_rec_11; private Rectangle text_rec_11; private Rectangle radio_rec_13; private Rectangle text_rec_13;
            private Rectangle radio_rec_21; private Rectangle text_rec_21; private Rectangle radio_rec_22; private Rectangle text_rec_22;
            private Rectangle radio_rec2_1; private Rectangle text_rec2_1;

            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int subwidth = 44; int radi1 = 7; int radi2 = 4;
                int pitchx = 6; int textheight = 20; int titleheight = 22;

                //////////////////////////////////////////////////////////////////////////////////////
                title_rec0 = global_rec;//StressDesign
                title_rec0.Y = title_rec0.Bottom;
                title_rec0.Height = titleheight;

                radio_rec0 = title_rec0;//StressDesign
                radio_rec0.Y += title_rec0.Height;

                radio_rec_1 = radio_rec0;
                radio_rec_1.X += radi2 - 1; radio_rec_1.Y = title_rec0.Bottom + radi2;
                radio_rec_1.Height = radi1; radio_rec_1.Width = radi1;

                text_rec_1 = radio_rec_1;
                text_rec_1.X += pitchx; text_rec_1.Y -= radi2;
                text_rec_1.Height = textheight; text_rec_1.Width = subwidth * 2;

                radio_rec_2 = radio_rec_1;
                radio_rec_2.Y += text_rec_1.Height - radi1;
                radio_rec_2.Height = radi1; radio_rec_2.Width = radi1;

                text_rec_2 = radio_rec_2;
                text_rec_2.X += pitchx; text_rec_2.Y -= radi2;
                text_rec_2.Height = textheight; text_rec_2.Width = subwidth * 2;

                radio_rec0.Height = text_rec_2.Bottom - title_rec0.Bottom -radi2;
                //////////////////////////////////////////////////////////////////////////////////////

                //////////////////////////////////////////////////////////////////////////////////////
                title_rec1 = global_rec;//DisplayOption
                title_rec1.Y = radio_rec0.Bottom;
                title_rec1.Height = titleheight;

                radio_rec1 = title_rec1;//DisplayOption
                radio_rec1.Y += title_rec1.Height;

                radio_rec_3 = radio_rec1;
                radio_rec_3.X += radi2 - 1; radio_rec_3.Y = title_rec1.Bottom + radi2;
                radio_rec_3.Height = radi1; radio_rec_3.Width = radi1;

                text_rec_3 = radio_rec_3;
                text_rec_3.X += pitchx; text_rec_3.Y -= radi2;
                text_rec_3.Height = textheight; text_rec_3.Width = subwidth * 2;

                radio_rec_4 = radio_rec_3;
                radio_rec_4.Y += text_rec_3.Height - radi1;
                radio_rec_4.Height = radi1; radio_rec_4.Width = radi1;

                text_rec_4 = radio_rec_4;
                text_rec_4.X += pitchx; text_rec_4.Y -= radi2;
                text_rec_4.Height = textheight; text_rec_4.Width = subwidth * 2;

                radio_rec_5 = radio_rec_4;
                radio_rec_5.Y += text_rec_4.Height - radi1;
                radio_rec_5.Height = radi1; radio_rec_5.Width = radi1;

                text_rec_5 = radio_rec_5;
                text_rec_5.X += pitchx; text_rec_5.Y -= radi2;
                text_rec_5.Height = textheight; text_rec_5.Width = subwidth * 2;

                radio_rec1.Height = text_rec_5.Bottom - title_rec1.Bottom - radi2;
                //////////////////////////////////////////////////////////////////////////////////////

                title_rec = global_rec;//DisplayOption
                title_rec.Y = radio_rec1.Bottom;
                title_rec.Height = titleheight;

                radio_rec = title_rec;//DisplayOption
                radio_rec.Y += title_rec.Height;

                radio_rec_11 = radio_rec;
                radio_rec_11.X += radi2 - 1; radio_rec_11.Y = title_rec.Bottom + radi2;
                radio_rec_11.Height = radi1; radio_rec_11.Width = radi1;

                text_rec_11 = radio_rec_11;
                text_rec_11.X += pitchx; text_rec_11.Y -= radi2;
                text_rec_11.Height = textheight; text_rec_11.Width = subwidth * 2;

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
                text_rec_22.Height = textheight; text_rec_22.Width = subwidth * 2;

                radio_rec.Height = text_rec_22.Y + textheight - radio_rec.Y - radi2;

                radio_rec2 = radio_rec;
                radio_rec2.Y = radio_rec.Y + radio_rec.Height;
                radio_rec2.Height = textheight;

                radio_rec2_1 = radio_rec2;
                radio_rec2_1.X += 5; radio_rec2_1.Y += 5;
                radio_rec2_1.Height = radi1; radio_rec2_1.Width = radi1;

                text_rec2_1 = radio_rec2_1;
                text_rec2_1.X += pitchx; text_rec2_1.Y -= radi2;
                text_rec2_1.Height = textheight; text_rec2_1.Width = subwidth * 2;

                global_rec.Height = text_rec2_1.Bottom - global_rec.Y;
                ///******************************************************************************************

                Bounds = global_rec;
            }
            Brush c11 = Brushes.White; Brush c13 = Brushes.White; Brush c21 = Brushes.White; Brush c22 = Brushes.White; Brush c2 = Brushes.White; Brush c0 = Brushes.Black; Brush c1 = Brushes.White; Brush c3 = Brushes.White; Brush c4 = Brushes.White; Brush c5 = Brushes.White;
            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
            {
                base.Render(canvas, graphics, channel);
                if (channel == GH_CanvasChannel.Objects)
                {
                    StringFormat format = new StringFormat();
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    format.Trimming = StringTrimming.EllipsisCharacter;


                    //////////////////////////////////////////////////////////////////////////////////////
                    GH_Capsule title0 = GH_Capsule.CreateCapsule(title_rec0, GH_Palette.Pink, 2, 0);
                    title0.Render(graphics, Selected, Owner.Locked, false);
                    title0.Dispose();

                    RectangleF textRectangle0 = title_rec0;
                    textRectangle0.Height = 20;
                    graphics.DrawString("Stress design", GH_FontServer.Standard, Brushes.White, textRectangle0, format);

                    GH_Capsule radio0 = GH_Capsule.CreateCapsule(radio_rec0, GH_Palette.White, 2, 0);
                    radio0.Render(graphics, Selected, Owner.Locked, false); radio0.Dispose();

                    GH_Capsule radio_1 = GH_Capsule.CreateCapsule(radio_rec_1, GH_Palette.Black, 5, 5);
                    radio_1.Render(graphics, Selected, Owner.Locked, false); radio_1.Dispose();
                    graphics.FillEllipse(c0, radio_rec_1);
                    graphics.DrawString("Long-term", GH_FontServer.Standard, Brushes.Black, text_rec_1);

                    GH_Capsule radio_2 = GH_Capsule.CreateCapsule(radio_rec_2, GH_Palette.Black, 5, 5);
                    radio_2.Render(graphics, Selected, Owner.Locked, false); radio_2.Dispose();
                    graphics.FillEllipse(c1, radio_rec_2);
                    graphics.DrawString("Short-term", GH_FontServer.Standard, Brushes.Black, text_rec_2);
                    //////////////////////////////////////////////////////////////////////////////////////

                    //////////////////////////////////////////////////////////////////////////////////////
                    GH_Capsule title1 = GH_Capsule.CreateCapsule(title_rec1, GH_Palette.Pink, 2, 0);
                    title1.Render(graphics, Selected, Owner.Locked, false);
                    title1.Dispose();

                    RectangleF textRectangle1 = title_rec1;
                    textRectangle1.Height = 20;
                    graphics.DrawString("Display option", GH_FontServer.Standard, Brushes.White, textRectangle1, format);

                    GH_Capsule radio1 = GH_Capsule.CreateCapsule(radio_rec1, GH_Palette.White, 2, 0);
                    radio1.Render(graphics, Selected, Owner.Locked, false); radio1.Dispose();

                    GH_Capsule radio_3 = GH_Capsule.CreateCapsule(radio_rec_3, GH_Palette.Black, 5, 5);
                    radio_3.Render(graphics, Selected, Owner.Locked, false); radio_3.Dispose();
                    graphics.FillEllipse(c3, radio_rec_3);
                    graphics.DrawString("P kentei", GH_FontServer.Standard, Brushes.Black, text_rec_3);

                    GH_Capsule radio_4 = GH_Capsule.CreateCapsule(radio_rec_4, GH_Palette.Black, 5, 5);
                    radio_4.Render(graphics, Selected, Owner.Locked, false); radio_4.Dispose();
                    graphics.FillEllipse(c4, radio_rec_4);
                    graphics.DrawString("Q kentei", GH_FontServer.Standard, Brushes.Black, text_rec_4);

                    GH_Capsule radio_5 = GH_Capsule.CreateCapsule(radio_rec_5, GH_Palette.Black, 5, 5);
                    radio_5.Render(graphics, Selected, Owner.Locked, false); radio_5.Dispose();
                    graphics.FillEllipse(c5, radio_rec_5);
                    graphics.DrawString("M kentei", GH_FontServer.Standard, Brushes.Black, text_rec_5);
                    //////////////////////////////////////////////////////////////////////////////////////

                    GH_Capsule title = GH_Capsule.CreateCapsule(title_rec, GH_Palette.Pink, 2, 0);
                    title.Render(graphics, Selected, Owner.Locked, false);
                    title.Dispose();

                    RectangleF textRectangle = title_rec;
                    textRectangle.Height = 20;
                    graphics.DrawString("Base Data", GH_FontServer.Standard, Brushes.White, textRectangle, format);

                    GH_Capsule radio = GH_Capsule.CreateCapsule(radio_rec, GH_Palette.White, 2, 0);
                    radio.Render(graphics, Selected, Owner.Locked, false); radio.Dispose();

                    GH_Capsule radio_11 = GH_Capsule.CreateCapsule(radio_rec_11, GH_Palette.Black, 5, 5);
                    radio_11.Render(graphics, Selected, Owner.Locked, false); radio_11.Dispose();
                    graphics.FillEllipse(c11, radio_rec_11);
                    graphics.DrawString("BaseShape", GH_FontServer.Standard, Brushes.Black, text_rec_11);

                    GH_Capsule radio_13 = GH_Capsule.CreateCapsule(radio_rec_13, GH_Palette.Black, 5, 5);
                    radio_13.Render(graphics, Selected, Owner.Locked, false); radio_13.Dispose();
                    graphics.FillEllipse(c13, radio_rec_13);
                    graphics.DrawString("XxYxt", GH_FontServer.Standard, Brushes.Black, text_rec_13);

                    GH_Capsule radio_21 = GH_Capsule.CreateCapsule(radio_rec_21, GH_Palette.Black, 5, 5);
                    radio_21.Render(graphics, Selected, Owner.Locked, false); radio_21.Dispose();
                    graphics.FillEllipse(c21, radio_rec_21);
                    graphics.DrawString("Bar", GH_FontServer.Standard, Brushes.Black, text_rec_21);

                    GH_Capsule radio_22 = GH_Capsule.CreateCapsule(radio_rec_22, GH_Palette.Black, 5, 5);
                    radio_22.Render(graphics, Selected, Owner.Locked, false); radio_22.Dispose();
                    graphics.FillEllipse(c22, radio_rec_22);
                    graphics.DrawString("Pressure", GH_FontServer.Standard, Brushes.Black, text_rec_22);

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
                    RectangleF rec21 = radio_rec_21; RectangleF rec22 = radio_rec_22; RectangleF rec2 = radio_rec2_1;
                    RectangleF rec0 = radio_rec_1; RectangleF rec1 = radio_rec_2; RectangleF rec3 = radio_rec_3; RectangleF rec4 = radio_rec_4; RectangleF rec5 = radio_rec_5;
                    if (rec0.Contains(e.CanvasLocation))
                    {
                        if (c0 == Brushes.Black) { c0 = Brushes.White; SetButton_for_IndividualFooting("c0", 0); c1 = Brushes.Black; SetButton_for_IndividualFooting("c1", 1); }
                        else
                        { c0 = Brushes.Black; SetButton_for_IndividualFooting("c0", 1); c1 = Brushes.White; SetButton_for_IndividualFooting("c1", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec1.Contains(e.CanvasLocation))
                    {
                        if (c1 == Brushes.Black) { c1 = Brushes.White; SetButton_for_IndividualFooting("c1", 0); c0 = Brushes.Black; SetButton_for_IndividualFooting("c0", 1); }
                        else
                        { c1 = Brushes.Black; SetButton_for_IndividualFooting("c1", 1); c0 = Brushes.White; SetButton_for_IndividualFooting("c0", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec3.Contains(e.CanvasLocation))
                    {
                        if (c3 == Brushes.Black) { c3 = Brushes.White; SetButton_for_IndividualFooting("c3", 0);}
                        else
                        { c3 = Brushes.Black; SetButton_for_IndividualFooting("c3", 1); c4 = Brushes.White; SetButton_for_IndividualFooting("c4", 0); c5 = Brushes.White; SetButton_for_IndividualFooting("c5", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec4.Contains(e.CanvasLocation))
                    {
                        if (c4 == Brushes.Black) { c4 = Brushes.White; SetButton_for_IndividualFooting("c4", 0); }
                        else
                        { c4 = Brushes.Black; SetButton_for_IndividualFooting("c4", 1); c3 = Brushes.White; SetButton_for_IndividualFooting("c3", 0); c5 = Brushes.White; SetButton_for_IndividualFooting("c5", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec5.Contains(e.CanvasLocation))
                    {
                        if (c5 == Brushes.Black) { c5 = Brushes.White; SetButton_for_IndividualFooting("c5", 0); }
                        else
                        { c5 = Brushes.Black; SetButton_for_IndividualFooting("c5", 1); c4 = Brushes.White; SetButton_for_IndividualFooting("c4", 0); c3 = Brushes.White; SetButton_for_IndividualFooting("c3", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec11.Contains(e.CanvasLocation))
                    {
                        if (c11 == Brushes.Black) { c11 = Brushes.White; SetButton_for_IndividualFooting("c11", 0); }
                        else
                        { c11 = Brushes.Black; SetButton_for_IndividualFooting("c11", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec13.Contains(e.CanvasLocation))
                    {
                        if (c13 == Brushes.Black) { c13 = Brushes.White; SetButton_for_IndividualFooting("c13", 0); }
                        else
                        { c13 = Brushes.Black; SetButton_for_IndividualFooting("c13", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec21.Contains(e.CanvasLocation))
                    {
                        if (c21 == Brushes.Black) { c21 = Brushes.White; SetButton_for_IndividualFooting("c21", 0); }
                        else
                        { c21 = Brushes.Black; SetButton_for_IndividualFooting("c21", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec22.Contains(e.CanvasLocation))
                    {
                        if (c22 == Brushes.Black) { c22 = Brushes.White; SetButton_for_IndividualFooting("c22", 0); }
                        else
                        { c22 = Brushes.Black; SetButton_for_IndividualFooting("c22", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec2.Contains(e.CanvasLocation))
                    {
                        if (c2 == Brushes.Black) { c2 = Brushes.White; SetButton_for_IndividualFooting("1", 0); }
                        else
                        { c2 = Brushes.Black; SetButton_for_IndividualFooting("1", 1); }
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