using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Display;
using System.Drawing;
using System.Windows.Forms;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
///****************************************
using System.Diagnostics;

using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp;
using Rhino;

namespace OpenSeesUtility
{
    public class AnalysisResultPDFroof : GH_Component
    {
        public static int on_off = 0; public static double fontsize = 9;
        public static PdfCreate.JapaneseFontResolver fontresolver = new PdfCreate.JapaneseFontResolver();
        public static XGraphics gfx;
        public static void SetButton(string s, int i)
        {
            if (s == "1")
            {
                on_off = i;
            }
        }
        public AnalysisResultPDFroof()
          : base("AnalysisResultPDFroof", "AnalysisResultPDFroof",
              "Output Analysis result to pdf",
              "OpenSees", "Utility")
        {
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("R", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("IJ", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("sec_f", "sec_f", "[[element No.,Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree, -9999);
            pManager.AddNumberParameter("reac_f", "reac_f", "[[Node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddVectorParameter("l_vec", "l_vec", "element axis vector for each elements", GH_ParamAccess.list, new Vector3d(-9999, -9999, -9999));
            pManager.AddNumberParameter("index(model)", "index(model)", "[...](element No. List to show any symbols)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("index(bar)", "index(bar)", "[...](element No. List to show any symbols)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("index", "index", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("index(spring)", "index(spring)", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("KABE_W", "KABE_W", "[[No.i,No.j,No.k,No.l,kabebairitsu],...](DataTree)", GH_ParamAccess.tree, -9999);///9
            pManager.AddNumberParameter("shear_w", "shear_w", "[Q1,Q2,...](DataList)", GH_ParamAccess.list, -9999);///10
            pManager.AddNumberParameter("B", "B", "[[node No.,X,Y,Z,MX,MY,MZ],...](DataTree) 0-1 variable", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("joint", "joint", "[[Ele. No., 0 or 1 or 2(means i or j or both), kx, ky, kz, rx, ry, rz(spring value)],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("spring", "spring", "[[No.i,No.j,kx+,kx-,ky+,ky-,kz+,kz-,mx,my,mz,angle],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("spring_f", "spring_f", "[[element No.,Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree,-9999);
            pManager.AddNumberParameter("p_load", "p_load", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("e_load", "e_load", "[[Element No.,line_load],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("f_load", "f_load", "[[No.i,No.j,No.k,No.l,floor_load],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("bar", "bar", "[...](name from SteelBar component)", GH_ParamAccess.list, -9999);///
            pManager.AddTextParameter("name(bar)", "name(bar)", "[...](name from SteelBar component)", GH_ParamAccess.list, "-9999");///
            pManager.AddTextParameter("name(sec)", "name(sec)", "[...](section name list)", GH_ParamAccess.list, "-9999");///
            pManager.AddTextParameter("name floor", "name floor", "layer name for floor", GH_ParamAccess.item, "floor");
            pManager.AddNumberParameter("kentei", "kentei", "[[element No.,long-term,short-term],...](Datatree)", GH_ParamAccess.tree, -9999);
            pManager.AddNumberParameter("kentei(kabe)", "kentei(kabe)", "[[element No.,long-term,short-term],...](Datatree)", GH_ParamAccess.tree, -9999);
            pManager.AddNumberParameter("kentei(spring)", "kentei(spring)", "[[element No.,long-term,short-term],...](Datatree)", GH_ParamAccess.tree, -9999);
            pManager.AddTextParameter("outputname", "outputname", "output file name", GH_ParamAccess.item, "");///
            pManager.AddNumberParameter("scaling", "scaling", "scale factor of figures", GH_ParamAccess.item, 0.85);///
            pManager.AddNumberParameter("offset", "offset", "offset value of figures", GH_ParamAccess.item, 25.0);///
            pManager.AddNumberParameter("scale_factor_for_N", "NS", "scale factor for N and N(spring)", GH_ParamAccess.list, new List<double> { 0.1, 0.01 });///
            pManager.AddNumberParameter("scale_factor_for_Q", "QS", "scale factor for Q and Q(spring)", GH_ParamAccess.list, new List<double> { 0.1, 0.01 });///
            pManager.AddNumberParameter("scale_factor_for_M", "MS", "scale factor for M and M(spring)", GH_ParamAccess.list, new List<double> { 0.15, 0.015 });///
            pManager.AddNumberParameter("scale_factor_for_Qw", "QwS", "scale factor for Qw", GH_ParamAccess.item, 0.1);///
            pManager.AddNumberParameter("scale_factor_for_R", "RS", "scale factor for reaction force", GH_ParamAccess.item, 0.1);///
            pManager.AddTextParameter("casename", "casename", "files are named _casename.pdf", GH_ParamAccess.list, new List<string> { "L", "X", "Y", "P" });///
            pManager.AddTextParameter("casememo", "casememo", "load case name in sheets", GH_ParamAccess.list, new List<string> { "(長期荷重時)", "+X荷重時", "+Y荷重時", "接地圧作用時" });///
            pManager.AddNumberParameter("fontsize", "fontsize", "fontsize", GH_ParamAccess.item, 9);///
            pManager.AddNumberParameter("linewidth", "linewidth", "linewidth", GH_ParamAccess.item, 1);///
            pManager.AddNumberParameter("pointsize", "pointsize", "pointsize", GH_ParamAccess.item, 2);///
            pManager.AddNumberParameter("jointsize", "jointsize", "jointsize", GH_ParamAccess.item, 3);///
            pManager.AddTextParameter("plan names", "plan names", "title name of each plan", GH_ParamAccess.list, "-9999");///
            pManager.AddNumberParameter("Nmin", "Nmin", "lower bound to show N value", GH_ParamAccess.item, 0.1);///
            pManager.AddNumberParameter("Qmin", "Qmin", "lower bound to show Q value", GH_ParamAccess.item, 0.1);///
            pManager.AddNumberParameter("Mmin", "Mmin", "lower bound to show M value", GH_ParamAccess.item, 0.1);///
            pManager.AddNumberParameter("kenteimin", "kenteimin", "lower bound to show kenteihi", GH_ParamAccess.item, 0.01);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("nod", "nod", "nod", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("nodall", "nodall", "nodall", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (on_off == 1)
            {
                XColor RGB(double h, double s, double l)//convert HSL to RGB
                {
                    var max = 0.0; var min = 0.0; var r = 0.0; var g = 0.0; var b = 0.0;
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
                        r = max;
                        g = (h / hp) * (max - min) + min;
                        b = min;
                    }
                    else if (q <= 2)
                    {
                        r = ((hp * 2 - h) / hp) * (max - min) + min;
                        g = max;
                        b = min;
                    }
                    else if (q <= 3)
                    {
                        r = min;
                        g = max;
                        b = ((h - hp * 2) / hp) * (max - min) + min;
                    }
                    else if (q <= 4)
                    {
                        r = min;
                        g = ((hp * 4 - h) / hp) * (max - min) + min;
                        b = max;
                    }
                    else if (q <= 5)
                    {
                        r = ((h - hp * 4) / hp) * (max - min) + min;
                        g = min;
                        b = max;
                    }
                    else
                    {
                        r = max;
                        g = min;
                        b = ((HUE_MAX - h) / hp) * (max - min) + min;
                    }
                    r *= RGB_MAX; g *= RGB_MAX; b *= RGB_MAX;
                    return XColor.FromArgb((int)r, (int)g, (int)b);
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
                DA.GetDataTree("R", out GH_Structure<GH_Number> _R); var R = _R.Branches; DA.GetDataTree("IJ", out GH_Structure<GH_Number> _ij); var ij = _ij.Branches; DA.GetDataTree("sec_f", out GH_Structure<GH_Number> _sec_f); var sec_f = _sec_f.Branches;
                DA.GetDataTree("reac_f", out GH_Structure<GH_Number> _reac_f); var reac_f = _reac_f.Branches;
                DA.GetDataTree("joint", out GH_Structure<GH_Number> _joint); var joint = _joint.Branches;
                DA.GetDataTree("B", out GH_Structure<GH_Number> _B); var B = _B.Branches;
                var joint_No = new List<int>(); for (int i = 0; i < joint.Count; i++) { joint_No.Add((int)joint[i][0].Value); }
                var B_No = new List<int>(); for (int i = 0; i < B.Count; i++) { B_No.Add((int)B[i][0].Value); }
                var l_vec = new List<Vector3d>(); DA.GetDataList("l_vec", l_vec);
                DA.GetDataTree("KABE_W", out GH_Structure<GH_Number> _kabe_w); var kabe_w = _kabe_w.Branches;
                var shear_w = new List<double>(); DA.GetDataList("shear_w", shear_w);
                var floorname = "floor"; DA.GetData("name floor", ref floorname);
                var pdfname = "TimberCheck"; DA.GetData("outputname", ref pdfname); var scaling = 0.95; DA.GetData("scaling", ref scaling); var offset = 25.0; DA.GetData("offset", ref offset); var offsety = offset * 2; var lw = 1.0; DA.GetData("linewidth", ref lw); var js = 1.0; DA.GetData("jointsize", ref js); var ps = 1.0; DA.GetData("pointsize", ref ps);
                var _nscale = new List<double>(); var nscale = 0.1; var nscale2 = 0.1;
                DA.GetDataList("scale_factor_for_N", _nscale); nscale = _nscale[0]; nscale2 = _nscale[1];
                var _qscale = new List<double>(); var qscale = 0.1; var qscale2 = 0.1;
                DA.GetDataList("scale_factor_for_Q", _qscale); qscale = _qscale[0]; qscale2 = _qscale[1];
                var _mscale = new List<double>(); var mscale = 0.1; var mscale2 = 0.1;
                DA.GetDataList("scale_factor_for_M", _mscale); mscale = _mscale[0]; mscale2 = _mscale[1];
                var qwscale = 0.025; DA.GetData("scale_factor_for_Qw", ref qwscale); var rscale = 0.1; DA.GetData("scale_factor_for_R", ref rscale);
                var index = new List<double>(); DA.GetDataList("index", index);
                var index_spring = new List<double>(); DA.GetDataList("index(spring)", index_spring);
                DA.GetData("fontsize", ref fontsize);
                var layer = new List<string>(); var wick = new List<string>(); var wicks = new List<List<string>>(); var wicks2 = new List<List<string>>(); var wicks3 = new List<List<string>>();
                DA.GetDataTree("spring", out GH_Structure<GH_Number> _spring); var spring = _spring.Branches;
                DA.GetDataTree("spring_f", out GH_Structure<GH_Number> _spring_f); var spring_f = _spring_f.Branches;
                var index_model = new List<double>(); DA.GetDataList("index(model)", index_model);
                var index_bar = new List<double>(); DA.GetDataList("index(bar)", index_bar);
                var bar = new List<double>(); DA.GetDataList("bar", bar);
                var name_bar = new List<string>(); DA.GetDataList("name(bar)", name_bar);
                var name_sec = new List<string>(); DA.GetDataList("name(sec)", name_sec);
                var Nbound = 0.1; var Qbound = 0.1; var Mbound = 0.1; var kbound = 0.1;
                DA.GetData("Nmin", ref Nbound); DA.GetData("Qmin", ref Qbound); DA.GetData("Mmin", ref Mbound); DA.GetData("kenteimin", ref kbound);
                DA.GetDataTree("p_load", out GH_Structure<GH_Number> _p_load); var p_load = _p_load.Branches;
                DA.GetDataTree("e_load", out GH_Structure<GH_Number> _e_load); var e_load = _e_load.Branches;
                DA.GetDataTree("f_load", out GH_Structure<GH_Number> _f_load); var f_load = _f_load.Branches;
                DA.GetDataTree("kentei", out GH_Structure<GH_Number> _kentei); var kentei = _kentei.Branches;
                DA.GetDataTree("kentei(kabe)", out GH_Structure<GH_Number> _kentei1); var kentei1 = _kentei1.Branches;
                DA.GetDataTree("kentei(spring)", out GH_Structure<GH_Number> _kentei2); var kentei2 = _kentei2.Branches;
                var nod = new GH_Structure<GH_Number>(); var nodall = new GH_Structure<GH_Number>();
                var plan = new List<string>(); DA.GetDataList("plan names", plan);
                if (plan[0] == "-9999")
                {
                    plan = new List<string>();
                    for (int i = 0; i < 100; i++){ plan.Add(i.ToString()); }
                }
                else
                {
                    var ii = plan.Count;
                    for (int i = ii; i < 100; i++) { plan.Add(ii.ToString()); }
                }
                if (index[0] == -9999)
                {
                    index = new List<double>();
                    for (int e = 0; e < ij.Count; e++) { index.Add(e); }
                }
                if (index_spring[0] == -9999)
                {
                    index_spring = new List<double>();
                    for (int e = 0; e < spring.Count; e++) { index_spring.Add(e); }
                }
                if (index_model[0] == -9999)
                {
                    index_model = new List<double>();
                    for (int e = 0; e < ij.Count; e++) { index_model.Add(e); }
                }
                if (index_bar[0] == -9999)
                {
                    index_bar = new List<double>();
                    for (int e = 0; e < ij.Count; e++) { index_bar.Add(e); }
                }
                var doc = RhinoDoc.ActiveDoc;
                if (R[0][0].Value!=-9999 && ij[0][0].Value != -9999)
                {
                    // フォントリゾルバーのグローバル登録
                    if (PdfCreate.JapaneseFontResolver.fontset == 0) { PdfSharp.Fonts.GlobalFontSettings.FontResolver = fontresolver; PdfCreate.JapaneseFontResolver.fontset = 1; }
                    // フォントを作成。
                    XFont font = new XFont("Gen Shin Gothic", fontsize, XFontStyle.Regular);
                    XFont titlefont = new XFont("Gen Shin Gothic", fontsize * 2, XFontStyle.Regular);
                    XFont fontbold = new XFont("Gen Shin Gothic", fontsize, XFontStyle.Bold);
                    var pen = new XPen(XColors.Black, lw); var penspring = new XPen(XColors.BlueViolet, lw);
                    var pengray = new XPen(XColors.Gray, lw); var pengray2 = new XPen(XColor.FromArgb(60,255,0,0), lw * 0.5);
                    var penreaction = new XPen(XColors.Red, lw); var penwick = new XPen(XColors.LightGray, lw * 0.5); penwick.DashStyle = XDashStyle.Dot;
                    var dir = Path.GetDirectoryName(Rhino.RhinoDoc.ActiveDoc.Path);//カレントディレクトリ
                    PdfDocument document = new PdfDocument();// PDFドキュメントを作成。
                    document.Info.Title = pdfname;
                    document.Info.Author = "Shinnosuke Fujita, Assoc. Prof., The Univ. of Kitakyushu shinnosuke@dn-archi.com";
                    var figname = new List<string> { "節点番号伏図", "部材番号伏図", "材料番号伏図", "断面番号伏図", "コードアングル伏図" };
                    if (spring[0][0].Value != -9999) { figname.Add("部材番号伏図(ばね)"); }
                    if (p_load[0][0].Value != -9999) { figname.Add("集中荷重伏図[kN]"); }
                    if (e_load[0][0].Value != -9999) { figname.Add("分布荷重伏図[kN/m]"); }
                    if (f_load[0][0].Value != -9999) { figname.Add("面荷重伏図[kN/m2]"); }
                    if (bar[0] != -9999 && name_bar[0] != "-9999") { figname.Add("配筋符号伏図"); }
                    if (name_sec[0] != "-9999") { figname.Add("断面符号伏図"); }
                    if (kabe_w[0][0].Value != -9999) { figname.Add("壁床線材置換伏図"); }
                    var Xmin = 9999.0; var Xmax = -9999.0; var Ymin = 9999.0; var Ymax = -9999.0;
                    for (int ii = 0; ii < R.Count; ii++)
                    {
                        Xmin = Math.Min(Xmin, R[ii][0].Value); Xmax = Math.Max(Xmax, R[ii][0].Value);
                        Ymin = Math.Min(Ymin, R[ii][1].Value); Ymax = Math.Max(Ymax, R[ii][1].Value);
                    }
                    var rangex = Xmax - Xmin; var rangey = Ymax - Ymin;//架構の範囲
                    var scale = Math.Min(594.0 / rangex * scaling, 842.0 / rangey * scaling); var pinwidth = 0.04;
                    var tri = lw * 3;
                    for (int i = 0; i < 100; i++)
                    {
                        List<Curve> lines = new List<Curve>();
                        var ij_new = new List<List<double>>();//その面の要素節点関係
                        var sec_f_new = new List<List<double>>();//その面の断面力
                        var spring_new = new List<List<double>>();//その面の要素節点関係(ばね)
                        var spring_f_new = new List<List<double>>();//その面の断面力(ばね)
                        var line = doc.Objects.FindByUserString(floorname, i.ToString(), true);
                        if (line.Length == 0) { break; }
                        var nod_No = new List<int>(); var nod_No_all = new List<int>(); var nod_P = new List<int>();
                        for (int j = 0; j < line.Length; j++)
                        {
                            var obj = line[j];
                            if (obj.GetType().ToString() != "Rhino.DocObjects.PointObject")
                            {

                                Curve[] l = new Curve[] { (new ObjRef(obj)).Curve() };
                                int nl = (new ObjRef(obj)).Curve().SpanCount;//ポリラインのセグメント数
                                if (nl > 1) { l = (new ObjRef(obj)).Curve().DuplicateSegments(); }
                                for (int jj = 0; jj < nl; jj++)
                                {
                                    lines.Add(l[jj]);
                                    var p0 = l[jj].PointAtStart; var p1 = l[jj].PointAtEnd; var lgh = l[jj].GetLength();
                                    for (int e = 0; e < ij.Count; e++)
                                    {
                                        var list = new List<double>(); var flist = new List<double>();
                                        int ni = (int)ij[e][0].Value; int nj = (int)ij[e][1].Value;
                                        var ri = new Point3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value);
                                        var rj = new Point3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value);
                                        if (Math.Abs((p0 - ri).Length + (ri - p1).Length - lgh) < 1e-5 && Math.Abs((p0 - rj).Length + (rj - p1).Length - lgh) < 1e-5)
                                        {
                                            list.Add(e);
                                            for (int a = 0; a < ij[e].Count; a++) { list.Add(ij[e][a].Value); }
                                            if (index.Contains(e) == true && sec_f[0][0].Value != -9999)
                                            {
                                                flist.Add(e);
                                                for (int ii = 0; ii < sec_f[0].Count; ii++)
                                                {
                                                    flist.Add(sec_f[e][ii].Value);
                                                }
                                                sec_f_new.Add(flist);
                                            }
                                            ij_new.Add(list);
                                        }
                                    }
                                    if (spring[0][0].Value != -9999)
                                    {
                                        for (int ind = 0; ind < index_spring.Count; ind++)
                                        {
                                            var slist = new List<double>(); var sflist = new List<double>();
                                            int e = (int)index_spring[ind];
                                            int ni = (int)spring[e][0].Value; int nj = (int)spring[e][1].Value;
                                            var ri = new Point3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value);
                                            var rj = new Point3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value);
                                            if (Math.Abs((p0 - ri).Length + (ri - p1).Length - lgh) < 1e-5 && Math.Abs((p0 - rj).Length + (rj - p1).Length - lgh) < 1e-5)
                                            {
                                                sflist.Add(e); slist.Add(e);
                                                for (int ii = 0; ii < spring_f[0].Count; ii++)
                                                {
                                                    sflist.Add(spring_f[e][ii].Value);
                                                }
                                                for (int ii = 0; ii < spring[0].Count; ii++)
                                                {
                                                    slist.Add(spring[e][ii].Value);
                                                }
                                                spring_f_new.Add(sflist); spring_new.Add(slist);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                var p = (new ObjRef(obj)).Point().Location;
                                for (int k = 0; k < R.Count; k++)
                                {
                                    if (Math.Abs(p[0] - R[k][0].Value) < 5e-3 && Math.Abs(p[1] - R[k][1].Value) < 5e-3 && Math.Abs(p[2] - R[k][2].Value) < 5e-3)
                                    {
                                        nod_No_all.Add(k); nod_No.Add(k); nod_P.Add(k);
                                    }
                                }
                            }
                        }
                        for (int k = 0; k < figname.Count; k++)
                        {
                            PdfPage page = new PdfPage(); page.Size = PageSize.A4;// 空白ページを作成。width x height = 594 x 842
                            page = document.AddPage();// 描画するためにXGraphicsオブジェクトを取得。
                            gfx = XGraphics.FromPdfPage(page);
                            var xmin = 9999.0; var xmax = -9999.0; var ymin = 9999.0; var ymax = -9999.0; var zmin = 9999.0; var zmax = -9999.0;
                            for (int e = 0; e < ij_new.Count; e++)
                            {
                                int ni = (int)ij_new[e][1]; int nj = (int)ij_new[e][2];
                                xmin = Math.Min(xmin, R[ni][0].Value); xmax = Math.Max(xmax, R[ni][0].Value); xmin = Math.Min(xmin, R[nj][0].Value); xmax = Math.Max(xmax, R[nj][0].Value);
                                ymin = Math.Min(ymin, R[ni][1].Value); ymax = Math.Max(ymax, R[ni][1].Value); ymin = Math.Min(ymin, R[nj][1].Value); ymax = Math.Max(ymax, R[nj][1].Value);
                            }
                            for (int j = 0; j < nod_P.Count; j++)
                            {
                                int ni = nod_P[j];
                                xmin = Math.Min(xmin, R[ni][0].Value); xmax = Math.Max(xmax, R[ni][0].Value); ymin = Math.Min(ymin, R[ni][1].Value); ymax = Math.Max(ymax, R[ni][1].Value);
                            }
                            for (int e = 0; e < spring_new.Count; e++)
                            {
                                var position = XStringFormats.BaseLineLeft;
                                if (e % 4 == 1) { position = XStringFormats.TopRight; }
                                if (e % 4 == 2) { position = XStringFormats.BaseLineRight; }
                                if (e % 4 == 3) { position = XStringFormats.TopLeft; }
                                int ni = (int)spring_new[e][1]; int nj = (int)spring_new[e][2]; int nel = (int)spring_new[e][0];
                                var r1 = new List<double>(); r1.Add(offset + (R[ni][0].Value - xmin) * scale); r1.Add(842 - offsety - (R[ni][1].Value - ymin) * scale);
                                var r2 = new List<double>(); r2.Add(offset + (R[nj][0].Value - xmin) * scale); r2.Add(842 - offsety - (R[nj][1].Value - ymin) * scale);
                                gfx.DrawLine(penspring, r1[0], r1[1], r2[0], r2[1]);//ばねの描画
                                gfx.DrawEllipse(XBrushes.Black, r1[0] - ps / 2.0, r1[1] - ps / 2.0, ps, ps);//i節点の描画
                                if (figname[k] == "節点番号伏図" && nod_No.Contains(ni) != true) { gfx.DrawString(ni.ToString(), font, XBrushes.Red, r1[0], r1[1], position); nod_No.Add(ni); }//i節点の節点番号描画
                                gfx.DrawEllipse(XBrushes.Black, r2[0] - ps / 2.0, r2[1] - ps / 2.0, ps, ps);//j節点の描画
                                if (figname[k] == "節点番号伏図" && nod_No.Contains(nj) != true) { gfx.DrawString(nj.ToString(), font, XBrushes.Red, r2[0], r2[1], position); nod_No.Add(nj); }//j節点の節点番号描画
                                if (figname[k] == "部材番号伏図(ばね)") { gfx.DrawString(nel.ToString(), font, XBrushes.DarkOrange, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, position); }//要素番号描画
                            }
                            for (int e = 0; e < ij_new.Count; e++)
                            {
                                int ni = (int)ij_new[e][1]; int nj = (int)ij_new[e][2]; int nel = (int)ij_new[e][0]; int mat = (int)ij_new[e][3]; int sec = (int)ij_new[e][4];
                                var r1 = new List<double>(); r1.Add(offset + (R[ni][0].Value-xmin) * scale); r1.Add(842 - offsety - (R[ni][1].Value-ymin) * scale);
                                var r2 = new List<double>(); r2.Add(offset + (R[nj][0].Value-xmin) * scale); r2.Add(842 - offsety - (R[nj][1].Value-ymin) * scale);
                                var pencil = pen;
                                if (index_model.Contains(nel) != true)
                                {
                                    gfx.DrawLine(pengray2, r1[0], r1[1], r2[0], r2[1]);
                                }
                                else
                                {
                                    gfx.DrawLine(pen, r1[0], r1[1], r2[0], r2[1]);//骨組の描画
                                    gfx.DrawEllipse(XBrushes.Black, r1[0] - ps / 2.0, r1[1] - ps / 2.0, ps, ps);//i節点の描画
                                    gfx.DrawEllipse(XBrushes.Black, r2[0] - ps / 2.0, r2[1] - ps / 2.0, ps, ps);//j節点の描画
                                }
                                if (index_model.Contains(nel) != true) { pencil = pengray2; }
                                if (joint_No.Contains(nel) == true)//材端ピン
                                {
                                    int ii = joint_No.IndexOf(nel);
                                    if (joint[ii][1].Value == 0 || joint[ii][1].Value == 2)
                                    {
                                        var rp1 = new List<double>(); rp1.Add(r1[0] + (r2[0] - r1[0]) * pinwidth); rp1.Add(r1[1] + (r2[1] - r1[1]) * pinwidth);
                                        gfx.DrawEllipse(pencil, XBrushes.White, rp1[0] - js / 2.0, rp1[1] - js / 2.0, js, js);//ピン記号
                                    }
                                    if (joint[ii][1].Value == 1 || joint[ii][1].Value == 2)
                                    {
                                        var rp2 = new List<double>(); rp2.Add(r2[0] + (r1[0] - r2[0]) * pinwidth); rp2.Add(r2[1] + (r1[1] - r2[1]) * pinwidth);
                                        gfx.DrawEllipse(pencil, XBrushes.White, rp2[0] - js / 2.0, rp2[1] - js / 2.0, js, js);//ピン記号
                                    }
                                }
                                var position = XStringFormats.BaseLineLeft;
                                if (e % 4 == 1) { position = XStringFormats.CenterRight; }
                                if (e % 4 == 2) { position = XStringFormats.BaseLineRight; }
                                if (e % 4 == 3) { position = XStringFormats.CenterLeft; }
                                if (index_model.Contains(nel) == true)
                                {
                                    if (figname[k] == "節点番号伏図")
                                    {
                                        if (nod_No_all.Contains(ni) != true) { nod_No_all.Add(ni); }
                                        if (nod_No_all.Contains(nj) != true) { nod_No_all.Add(nj); }
                                        if (nod_No.Contains(ni) != true)
                                        {
                                            nod_No.Add(ni);  gfx.DrawString(ni.ToString(), font, XBrushes.Red, r1[0], r1[1], position);//i節点の節点番号描画
                                        }
                                        if (nod_No.Contains(nj) != true)
                                        {
                                            nod_No.Add(nj); gfx.DrawString(nj.ToString(), font, XBrushes.Red, r2[0], r2[1], position);//j節点の節点番号描画
                                        }
                                    }
                                    if (figname[k] == "部材番号伏図") { gfx.DrawString(nel.ToString(), font, XBrushes.Blue, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, position); }//要素番号描画
                                    if (figname[k] == "材料番号伏図") { gfx.DrawString(mat.ToString(), font, XBrushes.Orange, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, position); }//材料番号描画
                                    if (figname[k] == "断面番号伏図") { gfx.DrawString(sec.ToString(), font, XBrushes.Crimson, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, position); }//断面番号描画
                                    if (figname[k] == "コードアングル伏図") { gfx.DrawString(((int)ij_new[e][5]).ToString() + "°", font, XBrushes.DarkGreen, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, position); }//コードアングル描画
                                    if (figname[k] == "配筋符号伏図")
                                    {
                                        if (index_bar.Contains(nel) == true)
                                        {
                                            var barname = name_bar[(int)bar[nel]];
                                            gfx.DrawString(barname, font, XBrushes.BlueViolet, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, position);
                                        }
                                    }//配筋符号描画
                                    if (figname[k] == "断面符号伏図")
                                    {
                                        if (index_model.Contains(nel) == true)
                                        {
                                            var secname = name_sec[sec];
                                            gfx.DrawString(secname, font, XBrushes.BlueViolet, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, position);
                                        }
                                    }//断面符号描画
                                }
                                else if (figname[k] == "節点番号伏図")
                                {
                                    if (nod_No_all.Contains(ni) != true)
                                    {
                                        nod_No_all.Add(ni);//i節点の節点番号描画
                                    }
                                    if (nod_No_all.Contains(nj) != true)
                                    {
                                        nod_No_all.Add(nj);//j節点の節点番号描画
                                    }
                                }
                            }
                            if (figname[k] == "節点番号伏図")
                            {
                                for (int j = 0; j < nod_P.Count; j++)
                                {
                                    var position = XStringFormats.BaseLineLeft;
                                    var ni = nod_P[j];
                                    var r1 = new List<double>(); r1.Add(offset + (R[ni][0].Value - xmin) * scale); r1.Add(842 - offsety - (R[ni][1].Value - ymin) * scale);
                                    gfx.DrawEllipse(XBrushes.Black, r1[0] - ps / 2.0, r1[1] - ps / 2.0, ps, ps);//節点の描画
                                    gfx.DrawString(ni.ToString(), font, XBrushes.Red, r1[0], r1[1], position);//節点の節点番号描画
                                }
                            }
                            if (figname[k] == "集中荷重伏図[kN]")
                            {
                                for (int j = 0; j < p_load.Count; j++)
                                {
                                    var ni = (int)p_load[j][0].Value; var fx = Math.Round(p_load[j][1].Value, 3); var fy = Math.Round(p_load[j][2].Value, 3); var fz = Math.Round(p_load[j][3].Value,3);
                                    if (nod_No_all.Contains(ni)==true)
                                    {
                                        var r1= new List<double>(); r1.Add(offset + (R[ni][0].Value - xmin) * scale); r1.Add(842 - offsety - (R[ni][1].Value - ymin) * scale);
                                        var position = XStringFormats.BottomCenter;
                                        var pts = new XPoint[3]; pts[0].X = r1[0]; pts[0].Y = r1[1]; pts[1].X = r1[0] - tri / 2.0; pts[1].Y = r1[1] - tri / 2.0 * Math.Sqrt(3); pts[2].X = r1[0] + tri / 2.0; pts[2].Y = r1[1] - tri / 2.0 * Math.Sqrt(3);
                                        var pts2 = new XPoint[3]; pts2[0].X = r1[0]; pts2[0].Y = r1[1]; pts2[1].X = r1[0] - tri / 2.0 * Math.Sqrt(3); pts2[1].Y = r1[1] + tri / 2.0; pts2[2].X = r1[0] - tri / 2.0 * Math.Sqrt(3); pts2[2].Y = r1[1] - tri / 2.0;
                                        var pts3 = new XPoint[3]; pts3[0].X = r1[0]; pts3[0].Y = r1[1]; pts3[1].X = r1[0] - tri / 2.0; pts3[1].Y = r1[1] + tri / 2.0 * Math.Sqrt(3); pts3[2].X = r1[0] + tri / 2.0; pts3[2].Y = r1[1] + tri / 2.0 * Math.Sqrt(3);
                                        if (Math.Abs(fz) != 0)
                                        {
                                            if (fz > 0)
                                            {
                                                pts = new XPoint[3]; pts[0].X = r1[0]; pts[0].Y = r1[1]; pts[1].X = r1[0] - tri / 2.0; pts[1].Y = r1[1] + tri / 2.0 * Math.Sqrt(3); pts[2].X = r1[0] + tri / 2.0; pts[2].Y = r1[1] + tri / 2.0 * Math.Sqrt(3);
                                                position = XStringFormats.TopCenter;
                                            }
                                            gfx.DrawPolygon(new XPen(XColors.Black, 0), XBrushes.Red, pts, XFillMode.Winding);
                                            gfx.DrawString(Math.Abs(fz).ToString().Substring(0, Math.Min(Math.Abs(fz).ToString().Length, 4)), font, XBrushes.Blue, (pts[1].X + pts[2].X) / 2.0, (pts[1].Y + pts[2].Y) / 2.0, position);//鉛直集中荷重値
                                        }
                                        if (Math.Abs(fy) != 0)
                                        {
                                            position = XStringFormats.TopCenter;
                                            if (fy < 0)
                                            {
                                                pts3[1].Y = -pts3[1].Y; pts3[2].Y = -pts3[2].Y; position = XStringFormats.BottomCenter;
                                            }
                                            gfx.DrawPolygon(new XPen(XColors.Black, 0), XBrushes.Red, pts3, XFillMode.Winding);
                                            gfx.DrawString(Math.Abs(fy).ToString().Substring(0, Math.Min(Math.Abs(fy).ToString().Length, 4)), font, XBrushes.Blue, (pts3[1].X + pts3[2].X) / 2.0, (pts3[1].Y + pts3[2].Y) / 2.0, position);//X集中荷重値
                                        }
                                        if (Math.Abs(fx) != 0)
                                        {
                                            position = XStringFormats.CenterRight;
                                            if (fx < 0)
                                            {
                                                pts2[1].X = -pts2[1].X; pts2[2].X = -pts2[2].X; position = XStringFormats.CenterLeft;
                                            }
                                            gfx.DrawPolygon(new XPen(XColors.Black, 0), XBrushes.Red, pts2, XFillMode.Winding);
                                            gfx.DrawString(Math.Abs(fx).ToString().Substring(0, Math.Min(Math.Abs(fx).ToString().Length, 4)), font, XBrushes.Blue, (pts2[1].X + pts2[2].X) / 2.0, (pts2[1].Y + pts2[2].Y) / 2.0, position);//X集中荷重値
                                        }
                                    }
                                }
                            }
                            if (figname[k] == "分布荷重伏図[kN/m]")
                            {
                                for (int j = 0; j < e_load.Count; j++)
                                {
                                    var e = (int)e_load[j][0].Value; var fz = Math.Round(e_load[j][3].Value,3); int ni = (int)ij[e][0].Value; int nj = (int)ij[e][1].Value;
                                    if (nod_No_all.Contains(ni)==true && nod_No_all.Contains(nj)==true)
                                    {
                                        var r1 = new List<double>(); r1.Add(offset + ((R[ni][0].Value + R[nj][0].Value) / 2.0 - xmin) * scale); r1.Add(842 - offsety - ((R[ni][1].Value + R[nj][1].Value) / 2.0 - ymin) * scale);
                                        var position = XStringFormats.BottomCenter;
                                        var pts = new XPoint[3]; pts[0].X = r1[0]; pts[0].Y = r1[1]; pts[1].X = r1[0] - tri / 2.0; pts[1].Y = r1[1] - tri / 2.0 * Math.Sqrt(3); pts[2].X = r1[0] + tri / 2.0; pts[2].Y = r1[1] - tri / 2.0 * Math.Sqrt(3);
                                        if (fz > 0)
                                        {
                                            pts = new XPoint[3]; pts[0].X = r1[0]; pts[0].Y = r1[1]; pts[1].X = r1[0] - tri / 2.0; pts[1].Y = r1[1] + tri / 2.0 * Math.Sqrt(3); pts[2].X = r1[0] + tri / 2.0; pts[2].Y = r1[1] + tri / 2.0 * Math.Sqrt(3);
                                            position = XStringFormats.TopCenter;
                                        }
                                        gfx.DrawPolygon(new XPen(XColors.Black, 0), XBrushes.Green, pts, XFillMode.Winding);
                                        gfx.DrawString(Math.Abs(fz).ToString("F").Substring(0, Math.Min(Math.Abs(fz).ToString().Length, 4)), font, XBrushes.Blue, (pts[1].X + pts[2].X) / 2.0, (pts[1].Y + pts[2].Y) / 2.0, position);//鉛直分布荷重値
                                    }
                                }
                            }
                            if (figname[k] == "面荷重伏図[kN/m2]")
                            {
                                var semiTransBrush = new XSolidBrush(XColor.FromArgb(50, 192,192,192));
                                for (int j = 0; j < f_load.Count; j++)
                                {
                                    var fz = Math.Round(f_load[j][4].Value,3); int ni = (int)f_load[j][0].Value; int nj = (int)f_load[j][1].Value; int nk = (int)f_load[j][2].Value; int nl = (int)f_load[j][3].Value;
                                    if (nod_No_all.Contains(ni) == true && nod_No_all.Contains(nj) == true && nod_No_all.Contains(nk) == true && (nod_No_all.Contains(nl) == true || nl == -1))
                                    {
                                        var rc = new List<double> { (R[ni][0].Value + R[nj][0].Value + R[nk][0].Value) / 3.0, (R[ni][1].Value + R[nj][1].Value + R[nk][1].Value) / 3.0 };
                                        
                                        if (nl != -1) { rc = new List<double> { (R[ni][0].Value + R[nj][0].Value + R[nk][0].Value + R[nl][0].Value) / 4.0, (R[ni][1].Value + R[nj][1].Value + R[nk][1].Value + R[nl][1].Value) / 4.0 }; }
                                        var r1 = new List<double>(); r1.Add(offset + (rc[0] - xmin) * scale); r1.Add(842 - offsety - (rc[1] - ymin) * scale);
                                        var position = XStringFormats.Center; var arrow = "⇊"; if (fz > 0) { arrow = "⇈"; }
                                        var pts = new XPoint[3]; if (nl != -1) { pts = new XPoint[4]; }
                                        pts[0].X = offset + (R[ni][0].Value - xmin) * scale; pts[0].Y = 842 - offsety - (R[ni][1].Value - ymin) * scale;
                                        pts[1].X = offset + (R[nj][0].Value - xmin) * scale; pts[1].Y = 842 - offsety - (R[nj][1].Value - ymin) * scale;
                                        pts[2].X = offset + (R[nk][0].Value - xmin) * scale; pts[2].Y = 842 - offsety - (R[nk][1].Value - ymin) * scale;
                                        if (nl != -1)
                                        {
                                            pts[3].X = offset + (R[nl][0].Value - xmin) * scale; pts[3].Y = 842 - offsety - (R[nl][1].Value - ymin) * scale;
                                        }
                                        gfx.DrawPolygon(penreaction, semiTransBrush, pts, XFillMode.Winding);
                                        gfx.DrawString(arrow + Math.Abs(fz).ToString().Substring(0, Math.Min(Math.Abs(fz).ToString().Length, 4)), font, XBrushes.Black, r1[0], r1[1], position);//鉛直面荷重値
                                    }
                                }
                            }
                            if (figname[k] == "壁床線材置換伏図")
                            {
                                for (int j = 0; j < kabe_w.Count; j++)
                                {
                                    int ni = (int)kabe_w[j][0].Value; int nj = (int)kabe_w[j][1].Value; int nk = (int)kabe_w[j][2].Value; int nl = (int)kabe_w[j][3].Value;
                                    if (nod_No_all.Contains(ni) == true && nod_No_all.Contains(nj) == true && nod_No_all.Contains(nk) == true && kabe_w[j][4].Value!=0)
                                    {
                                        var position = XStringFormats.Center;
                                        var r1 = new List<double>(); r1.Add(offset + (R[ni][0].Value - xmin) * scale); r1.Add(842 - offsety - (R[ni][1].Value - ymin) * scale);
                                        var r2 = new List<double>(); r2.Add(offset + (R[nj][0].Value - xmin) * scale); r2.Add(842 - offsety - (R[nj][1].Value - ymin) * scale);
                                        var r3 = new List<double>(); r3.Add(offset + (R[nk][0].Value - xmin) * scale); r3.Add(842 - offsety - (R[nk][1].Value - ymin) * scale);
                                        var r4 = new List<double>(); r4.Add(offset + (R[nl][0].Value - xmin) * scale); r4.Add(842 - offsety - (R[nl][1].Value - ymin) * scale);
                                        var rp1 = new List<double>(); rp1.Add(r1[0] + (r3[0] - r1[0]) * pinwidth); rp1.Add(r1[1] + (r3[1] - r1[1]) * pinwidth);
                                        var rp2 = new List<double>(); rp2.Add(r2[0] + (r4[0] - r2[0]) * pinwidth); rp2.Add(r2[1] + (r4[1] - r2[1]) * pinwidth);
                                        var rp3 = new List<double>(); rp3.Add(r3[0] + (r1[0] - r3[0]) * pinwidth); rp3.Add(r3[1] + (r1[1] - r3[1]) * pinwidth);
                                        var rp4 = new List<double>(); rp4.Add(r4[0] + (r2[0] - r4[0]) * pinwidth); rp4.Add(r4[1] + (r2[1] - r4[1]) * pinwidth);
                                        var rc = new List<double> { (r1[0] + r2[0] + r3[0] + r4[0]) / 4.0, (r1[1] + r2[1] + r3[1] + r4[1]) / 4.0 };
                                        gfx.DrawLine(pengray, r1[0], r1[1], r3[0], r3[1]); gfx.DrawLine(pengray, r2[0], r2[1], r4[0], r4[1]);//線材置換トラスの描画
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp1[0] - js / 2.0, rp1[1] - js / 2.0, js, js);//ピン記号
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp2[0] - js / 2.0, rp2[1] - js / 2.0, js, js);//ピン記号
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp3[0] - js / 2.0, rp3[1] - js / 2.0, js, js);//ピン記号
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp4[0] - js / 2.0, rp4[1] - js / 2.0, js, js);//ピン記号
                                        var alpha = kabe_w[j][4].Value;
                                        var color = new XSolidBrush(RGB(Math.Max(0, (1 - Math.Min(alpha / 5.0, 1.0)) * 1.9 / 3.0), 1, 0.5));
                                        gfx.DrawString(Math.Round(alpha, 2).ToString().Substring(0, Math.Min(4, Math.Round(alpha, 2).ToString().Length)) + "倍", font, color, rc[0], rc[1], XStringFormats.TopCenter);//壁倍率
                                        gfx.DrawString(j.ToString(), font, XBrushes.Black, rc[0], rc[1], XStringFormats.BottomCenter);//壁番号
                                    }
                                }
                            }
                            gfx.DrawString(figname[k] + "(" + plan[i] + ")", titlefont, XBrushes.Black, offset, 842 - offset, XStringFormats.BaseLineLeft);
                        }
                        var nlist = new List<GH_Number>(); var nlist2 = new List<GH_Number>();
                        for (int ii = 0; ii < nod_No.Count; ii++)
                        {
                            nlist.Add(new GH_Number(nod_No[ii]));
                        }
                        for (int ii = 0; ii < nod_No_all.Count; ii++)
                        {
                            nlist2.Add(new GH_Number(nod_No_all[ii]));
                        }
                        nod.AppendRange(nlist, new GH_Path(i)); nodall.AppendRange(nlist2, new GH_Path(i));
                    }
                    DA.SetDataTree(0, nod); DA.SetDataTree(1, nodall);
                    var filename = dir + "/" + pdfname + "_planmodel.pdf";
                    document.Save(filename);// ドキュメントを保存。
                    Process.Start(new ProcessStartInfo(@filename) { UseShellExecute = true });
                    //反力図
                    if (reac_f[0][0].Value != -9999)
                    {
                        int nf = reac_f[0].Count; var label = new List<string> { "L", "X", "Y", "P" }; var casememo = new List<string> { "(長期荷重時)", "+X荷重時", "+Y荷重時", "接地圧作用時" };
                        for (int kkk = 0; kkk < nf / 7; kkk++)
                        {
                            //鉛直反力図///////////////////////////////////////////////////////////////////////////////////////////
                            document = new PdfDocument();
                            document.Info.Title = pdfname;
                            document.Info.Author = "Shinnosuke Fujita, Assoc. Prof., The Univ. of Kitakyushu";
                            var reac_f_index = new List<int>();
                            for (int i = 0; i < reac_f.Count; i++) { reac_f_index.Add((int)reac_f[i][0].Value); }
                            for (int i = 0; i < 100; i++)
                            {
                                List<Curve> lines = new List<Curve>();
                                var ij_new = new List<List<double>>();//その面の要素節点関係
                                var sec_f_new = new List<List<double>>();//その面の断面力
                                var spring_new = new List<List<double>>();//その面の要素節点関係(ばね)
                                var spring_f_new = new List<List<double>>();//その面の断面力(ばね)
                                var line = doc.Objects.FindByUserString(floorname, i.ToString(), true); if (line.Length == 0) { break; }
                                var nod_No = new List<int>(); var nod_P = new List<int>();
                                for (int j = 0; j < line.Length; j++)
                                {
                                    var obj = line[j];
                                    if (obj.GetType().ToString() != "Rhino.DocObjects.PointObject")
                                    {

                                        Curve[] l = new Curve[] { (new ObjRef(obj)).Curve() };
                                        int nl = (new ObjRef(obj)).Curve().SpanCount;//ポリラインのセグメント数
                                        if (nl > 1) { l = (new ObjRef(obj)).Curve().DuplicateSegments(); }
                                        for (int jj = 0; jj < nl; jj++)
                                        {
                                            lines.Add(l[jj]);
                                            var p0 = l[jj].PointAtStart; var p1 = l[jj].PointAtEnd; var lgh = l[jj].GetLength();
                                            for (int e = 0; e < ij.Count; e++)
                                            {
                                                var list = new List<double>(); var flist = new List<double>();
                                                int ni = (int)ij[e][0].Value; int nj = (int)ij[e][1].Value;
                                                var ri = new Point3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value);
                                                var rj = new Point3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value);
                                                if (Math.Abs((p0 - ri).Length + (ri - p1).Length - lgh) < 1e-5 && Math.Abs((p0 - rj).Length + (rj - p1).Length - lgh) < 1e-5)
                                                {
                                                    list.Add(e);
                                                    for (int a = 0; a < ij[e].Count; a++) { list.Add(ij[e][a].Value); }
                                                    if (index.Contains(e) == true && sec_f[0][0].Value != -9999)
                                                    {
                                                        flist.Add(e);
                                                        for (int ii = 0; ii < sec_f[0].Count; ii++)
                                                        {
                                                            flist.Add(sec_f[e][ii].Value);
                                                        }
                                                        ij_new.Add(list); sec_f_new.Add(flist);
                                                    }
                                                }
                                            }
                                            if (spring[0][0].Value != -9999)
                                            {
                                                for (int ind = 0; ind < index_spring.Count; ind++)
                                                {
                                                    var slist = new List<double>(); var sflist = new List<double>();
                                                    int e = (int)index_spring[ind];
                                                    int ni = (int)spring[e][0].Value; int nj = (int)spring[e][1].Value;
                                                    var ri = new Point3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value);
                                                    var rj = new Point3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value);
                                                    if (Math.Abs((p0 - ri).Length + (ri - p1).Length - lgh) < 1e-5 && Math.Abs((p0 - rj).Length + (rj - p1).Length - lgh) < 1e-5)
                                                    {
                                                        sflist.Add(e); slist.Add(e);
                                                        for (int ii = 0; ii < spring_f[0].Count; ii++)
                                                        {
                                                            sflist.Add(spring_f[e][ii].Value);
                                                        }
                                                        for (int ii = 0; ii < spring[0].Count; ii++)
                                                        {
                                                            slist.Add(spring[e][ii].Value);
                                                        }
                                                        spring_f_new.Add(sflist); spring_new.Add(slist);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var p = (new ObjRef(obj)).Point().Location;
                                        for (int k = 0; k < R.Count; k++)
                                        {
                                            if (Math.Abs(p[0] - R[k][0].Value) < 5e-3 && Math.Abs(p[1] - R[k][1].Value) < 5e-3 && Math.Abs(p[2] - R[k][2].Value) < 5e-3)
                                            {
                                                nod_No.Add(k); nod_P.Add(k);
                                            }
                                        }
                                    }
                                }
                                PdfPage page = new PdfPage(); page.Size = PageSize.A4;// 空白ページを作成。width x height = 594 x 842
                                page = document.AddPage();// 描画するためにXGraphicsオブジェクトを取得。
                                gfx = XGraphics.FromPdfPage(page);
                                var xmin = 9999.0; var xmax = -9999.0; var ymin = 9999.0; var ymax = -9999.0; var zmin = 9999.0; var zmax = -9999.0;
                                for (int e = 0; e < ij_new.Count; e++)
                                {
                                    int ni = (int)ij_new[e][1]; int nj = (int)ij_new[e][2];
                                    xmin = Math.Min(xmin, R[ni][0].Value); xmax = Math.Max(xmax, R[ni][0].Value); xmin = Math.Min(xmin, R[nj][0].Value); xmax = Math.Max(xmax, R[nj][0].Value);
                                    ymin = Math.Min(ymin, R[ni][1].Value); ymax = Math.Max(ymax, R[ni][1].Value); ymin = Math.Min(ymin, R[nj][1].Value); ymax = Math.Max(ymax, R[nj][1].Value);
                                }
                                for (int j = 0; j < nod_P.Count; j++)
                                {
                                    int ni = nod_P[j];
                                    xmin = Math.Min(xmin, R[ni][0].Value); xmax = Math.Max(xmax, R[ni][0].Value); ymin = Math.Min(ymin, R[ni][1].Value); ymax = Math.Max(ymax, R[ni][1].Value);
                                }
                                for (int e = 0; e < spring_new.Count; e++)
                                {
                                    var position = XStringFormats.BaseLineLeft;
                                    if (e % 4 == 1) { position = XStringFormats.TopRight; }
                                    if (e % 4 == 2) { position = XStringFormats.BaseLineRight; }
                                    if (e % 4 == 3) { position = XStringFormats.TopLeft; }
                                    int ni = (int)spring_new[e][1]; int nj = (int)spring_new[e][2]; int nel = (int)spring_new[e][0];
                                    var r1 = new List<double>(); r1.Add(offset + (R[ni][0].Value - xmin) * scale); r1.Add(842 - offsety - (R[ni][1].Value - ymin) * scale);
                                    var r2 = new List<double>(); r2.Add(offset + (R[nj][0].Value - xmin) * scale); r2.Add(842 - offsety - (R[nj][1].Value - ymin) * scale);
                                    gfx.DrawLine(penspring, r1[0], r1[1], r2[0], r2[1]);//ばねの描画
                                    gfx.DrawEllipse(XBrushes.Black, r1[0] - ps / 2.0, r1[1] - ps / 2.0, ps, ps);//i節点の描画
                                    gfx.DrawEllipse(XBrushes.Black, r2[0] - ps / 2.0, r2[1] - ps / 2.0, ps, ps);//j節点の描画
                                    if (nod_No.Contains(ni) != true) { nod_No.Add(ni); }
                                    if (nod_No.Contains(nj) != true) { nod_No.Add(nj); }
                                }
                                for (int e = 0; e < ij_new.Count; e++)
                                {
                                    int ni = (int)ij_new[e][1]; int nj = (int)ij_new[e][2]; int nel = (int)ij_new[e][0]; int mat = (int)ij_new[e][3]; int sec = (int)ij_new[e][4];
                                    var r1 = new List<double>(); r1.Add(offset + (R[ni][0].Value - xmin) * scale); r1.Add(842 - offsety - (R[ni][1].Value - ymin) * scale);
                                    var r2 = new List<double>(); r2.Add(offset + (R[nj][0].Value - xmin) * scale); r2.Add(842 - offsety - (R[nj][1].Value - ymin) * scale);
                                    var pencil = pen;
                                    if (index_model.Contains(nel) != true) { gfx.DrawLine(pengray2, r1[0], r1[1], r2[0], r2[1]); }
                                    else
                                    {
                                        gfx.DrawLine(pen, r1[0], r1[1], r2[0], r2[1]);//骨組の描画
                                        gfx.DrawEllipse(XBrushes.Black, r1[0] - ps / 2.0, r1[1] - ps / 2.0, ps, ps);//i節点の描画
                                        gfx.DrawEllipse(XBrushes.Black, r2[0] - ps / 2.0, r2[1] - ps / 2.0, ps, ps);//j節点の描画
                                    }
                                    if (index_model.Contains(nel) != true) { pencil = pengray2; }
                                    if (joint_No.Contains(nel) == true)//材端ピン
                                    {
                                        int ii = joint_No.IndexOf(nel);
                                        if (joint[ii][1].Value == 0 || joint[ii][1].Value == 2)
                                        {
                                            var rp1 = new List<double>(); rp1.Add(r1[0] + (r2[0] - r1[0]) * pinwidth); rp1.Add(r1[1] + (r2[1] - r1[1]) * pinwidth);
                                            gfx.DrawEllipse(pencil, XBrushes.White, rp1[0] - js / 2.0, rp1[1] - js / 2.0, js, js);//ピン記号
                                        }
                                        if (joint[ii][1].Value == 1 || joint[ii][1].Value == 2)
                                        {
                                            var rp2 = new List<double>(); rp2.Add(r2[0] + (r1[0] - r2[0]) * pinwidth); rp2.Add(r2[1] + (r1[1] - r2[1]) * pinwidth);
                                            gfx.DrawEllipse(pencil, XBrushes.White, rp2[0] - js / 2.0, rp2[1] - js / 2.0, js, js);//ピン記号
                                        }
                                    }
                                    if (nod_No.Contains(ni) != true) { nod_No.Add(ni); }
                                    if (nod_No.Contains(nj) != true) { nod_No.Add(nj); }
                                }
                                for (int e = 0; e < reac_f_index.Count; e++)
                                {
                                    if (nod_No.Contains(reac_f_index[e]) == true)
                                    {
                                        int ni = (int)reac_f[e][0].Value;
                                        var r1 = new List<double>(); r1.Add(offset + (R[ni][0].Value - xmin) * scale); r1.Add(842 - offsety - (R[ni][1].Value - ymin) * scale);
                                        gfx.DrawEllipse(XBrushes.Black, r1[0] - ps / 2.0, r1[1] - ps / 2.0, ps, ps);//節点の描画
                                        gfx.DrawString(ni.ToString(), font, XBrushes.Red, r1[0], r1[1], XStringFormats.BottomCenter);//節点の節点番号描画
                                        gfx.DrawString(reac_f[e][kkk*7+3].Value.ToString().Substring(0, Math.Min(reac_f[e][kkk * 7 + 3].Value.ToString().Length, 4)), font, XBrushes.Black, r1[0], r1[1], XStringFormats.TopCenter);
                                    }
                                }
                                gfx.DrawString("反力図" + casememo[kkk] + "(" + plan[i] + ")", titlefont, XBrushes.Black, offset, 842 - offset, XStringFormats.BaseLineLeft);
                            }
                            filename = dir + "/" + pdfname + "_planR" + label[kkk] + ".pdf";
                            document.Save(filename);// ドキュメントを保存。
                            Process.Start(new ProcessStartInfo(@filename) { UseShellExecute = true });
                        }
                    }
                    //検定比図
                    document = new PdfDocument();// PDFドキュメントを作成。
                    document.Info.Title = pdfname;
                    document.Info.Author = "Shinnosuke Fujita, Assoc. Prof., The Univ. of Kitakyushu shinnosuke@dn-archi.com";
                    var kentei_index = new List<int>();
                    if (kentei[0][0].Value != -9999) { for (int i = 0; i < kentei.Count; i++) { kentei_index.Add((int)kentei[i][0].Value); } }
                    var kentei1_index = new List<int>();
                    if (kentei1[0][0].Value != -9999) { for (int i = 0; i < kentei1.Count; i++) { kentei1_index.Add((int)kentei1[i][0].Value); } }
                    var kentei2_index = new List<int>();
                    if (kentei2[0][0].Value != -9999) { for (int i = 0; i < kentei2.Count; i++) { kentei2_index.Add((int)kentei2[i][0].Value); } }
                    figname = new List<string>();
                    if (kentei[0][0].Value != -9999) { figname.Add("長期最大検定比伏図"); if (kentei[0].Count == 3) { figname.Add("短期最大検定比伏図"); } }
                    if (kentei1[0][0].Value != -9999) { figname.Add("長期最大検定比伏図(壁床線材置換)"); if (kentei1[0].Count == 3) { figname.Add("短期最大検定比伏図(壁床線材置換)"); } }
                    if (kentei2[0][0].Value != -9999) { figname.Add("長期最大検定比伏図(ばね)"); if (kentei2[0].Count == 3) { figname.Add("短期最大検定比伏図(ばね)"); } }
                    for (int i = 0; i < 100; i++)
                    {
                        List<Curve> lines = new List<Curve>();
                        var ij_new = new List<List<double>>();//その面の要素節点関係
                        var sec_f_new = new List<List<double>>();//その面の断面力
                        var spring_new = new List<List<double>>();//その面の要素節点関係(ばね)
                        var spring_f_new = new List<List<double>>();//その面の断面力(ばね)
                        var kabe_w_new = new List<List<double>>();//その面の耐力壁
                        var line = doc.Objects.FindByUserString(floorname, i.ToString(), true);
                        if (line.Length == 0) { break; }
                        for (int j = 0; j < line.Length; j++)
                        {
                            var obj = line[j];
                            if (obj.GetType().ToString() != "Rhino.DocObjects.PointObject")
                            {
                                Curve[] l = new Curve[] { (new ObjRef(obj)).Curve() };
                                int nl = (new ObjRef(obj)).Curve().SpanCount;//ポリラインのセグメント数
                                if (nl > 1) { l = (new ObjRef(obj)).Curve().DuplicateSegments(); }
                                for (int jj = 0; jj < nl; jj++)
                                {
                                    lines.Add(l[jj]);
                                    var p0 = l[jj].PointAtStart; var p1 = l[jj].PointAtEnd; var lgh = l[jj].GetLength();
                                    for (int e = 0; e < ij.Count; e++)
                                    {
                                        var list = new List<double>(); var flist = new List<double>();
                                        int ni = (int)ij[e][0].Value; int nj = (int)ij[e][1].Value;
                                        var ri = new Point3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value);
                                        var rj = new Point3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value);
                                        if (Math.Abs((p0 - ri).Length + (ri - p1).Length - lgh) < 1e-5 && Math.Abs((p0 - rj).Length + (rj - p1).Length - lgh) < 1e-5)
                                        {
                                            list.Add(e);
                                            for (int a = 0; a < ij[e].Count; a++) { list.Add(ij[e][a].Value); }
                                            if (index.Contains(e) == true && sec_f[0][0].Value != -9999)
                                            {
                                                flist.Add(e);
                                                for (int ii = 0; ii < sec_f[0].Count; ii++)
                                                {
                                                    flist.Add(sec_f[e][ii].Value);
                                                }
                                                ij_new.Add(list); sec_f_new.Add(flist);
                                            }
                                        }
                                    }
                                    if (spring[0][0].Value != -9999)
                                    {
                                        for (int ind = 0; ind < index_spring.Count; ind++)
                                        {
                                            var slist = new List<double>(); var sflist = new List<double>();
                                            int e = (int)index_spring[ind];
                                            int ni = (int)spring[e][0].Value; int nj = (int)spring[e][1].Value;
                                            var ri = new Point3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value);
                                            var rj = new Point3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value);
                                            if (Math.Abs((p0 - ri).Length + (ri - p1).Length - lgh) < 1e-5 && Math.Abs((p0 - rj).Length + (rj - p1).Length - lgh) < 1e-5)
                                            {
                                                sflist.Add(e); slist.Add(e);
                                                for (int ii = 0; ii < spring_f[0].Count; ii++)
                                                {
                                                    sflist.Add(spring_f[e][ii].Value);
                                                }
                                                for (int ii = 0; ii < spring[0].Count; ii++)
                                                {
                                                    slist.Add(spring[e][ii].Value);
                                                }
                                                spring_f_new.Add(sflist); spring_new.Add(slist);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        for (int k = 0; k < figname.Count; k++)
                        {
                            PdfPage page = new PdfPage(); page.Size = PageSize.A4;// 空白ページを作成。width x height = 594 x 842
                            page = document.AddPage();// 描画するためにXGraphicsオブジェクトを取得。
                            gfx = XGraphics.FromPdfPage(page);
                            var xmin = 9999.0; var xmax = -9999.0; var ymin = 9999.0; var ymax = -9999.0; var zmin = 9999.0; var zmax = -9999.0;
                            for (int e = 0; e < ij_new.Count; e++)
                            {
                                int ni = (int)ij_new[e][1]; int nj = (int)ij_new[e][2];
                                xmin = Math.Min(xmin, R[ni][0].Value); xmax = Math.Max(xmax, R[ni][0].Value); xmin = Math.Min(xmin, R[nj][0].Value); xmax = Math.Max(xmax, R[nj][0].Value);
                                ymin = Math.Min(ymin, R[ni][1].Value); ymax = Math.Max(ymax, R[ni][1].Value); ymin = Math.Min(ymin, R[nj][1].Value); ymax = Math.Max(ymax, R[nj][1].Value);
                            }
                            for (int e = 0; e < spring_new.Count; e++)
                            {
                                var position = XStringFormats.BaseLineLeft;
                                if (e % 4 == 1) { position = XStringFormats.TopRight; }
                                if (e % 4 == 2) { position = XStringFormats.BaseLineRight; }
                                if (e % 4 == 3) { position = XStringFormats.TopLeft; }
                                int ni = (int)spring_new[e][1]; int nj = (int)spring_new[e][2]; int nel = (int)spring_new[e][0];
                                var r1 = new List<double>(); r1.Add(offset + (R[ni][0].Value - xmin) * scale); r1.Add(842 - offsety - (R[ni][1].Value - ymin) * scale);
                                var r2 = new List<double>(); r2.Add(offset + (R[nj][0].Value - xmin) * scale); r2.Add(842 - offsety - (R[nj][1].Value - ymin) * scale);
                                gfx.DrawLine(penspring, r1[0], r1[1], r2[0], r2[1]);//ばねの描画
                                gfx.DrawEllipse(XBrushes.Black, r1[0] - ps / 2.0, r1[1] - ps / 2.0, ps, ps);//i節点の描画
                                gfx.DrawEllipse(XBrushes.Black, r2[0] - ps / 2.0, r2[1] - ps / 2.0, ps, ps);//j節点の描画
                                if (figname[k] == "長期最大検定比伏図(ばね)")
                                {
                                    int aa = kentei2_index.IndexOf(nel);
                                    if (aa != -1)
                                    {
                                        var kk = kentei2[aa][1].Value;
                                        if (kk > kbound)
                                        {
                                            var color = new XSolidBrush(RGB((1 - Math.Min(kk, 1.0)) * 1.9 / 3.0, 1, 0.5));
                                            gfx.DrawString(kk.ToString().Substring(0, Math.Min(kk.ToString().Length, 4)), font, color, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, position);
                                        }
                                    }
                                }
                                if (figname[k] == "短期最大検定比伏図(ばね)")
                                {
                                    int aa = kentei2_index.IndexOf(nel);
                                    if (aa != -1)
                                    {
                                        var kk = kentei2[aa][2].Value;
                                        if (kk > kbound)
                                        {
                                            var color = new XSolidBrush(RGB((1 - Math.Min(kk, 1.0)) * 1.9 / 3.0, 1, 0.5));
                                            gfx.DrawString(kk.ToString().Substring(0, Math.Min(kk.ToString().Length, 4)), font, color, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, position);
                                        }
                                    }
                                }
                            }
                            var nod_No_all = new List<int>();
                            for (int e = 0; e < ij_new.Count; e++)
                            {
                                int ni = (int)ij_new[e][1]; int nj = (int)ij_new[e][2]; int nel = (int)ij_new[e][0]; int mat = (int)ij_new[e][3]; int sec = (int)ij_new[e][4];
                                var r1 = new List<double>(); r1.Add(offset + (R[ni][0].Value - xmin) * scale); r1.Add(842 - offsety - (R[ni][1].Value - ymin) * scale);
                                var r2 = new List<double>(); r2.Add(offset + (R[nj][0].Value - xmin) * scale); r2.Add(842 - offsety - (R[nj][1].Value - ymin) * scale);
                                var pencil = pen;
                                if (index_model.Contains(nel) != true) { gfx.DrawLine(pengray2, r1[0], r1[1], r2[0], r2[1]); }
                                else
                                {
                                    gfx.DrawLine(pen, r1[0], r1[1], r2[0], r2[1]);//骨組の描画
                                    gfx.DrawEllipse(XBrushes.Black, r1[0] - ps / 2.0, r1[1] - ps / 2.0, ps, ps);//i節点の描画
                                    gfx.DrawEllipse(XBrushes.Black, r2[0] - ps / 2.0, r2[1] - ps / 2.0, ps, ps);//j節点の描画
                                }
                                if (index_model.Contains(nel) != true) { pencil = pengray2; }
                                if (joint_No.Contains(nel) == true)//材端ピン
                                {
                                    int ii = joint_No.IndexOf(nel);
                                    if (joint[ii][1].Value == 0 || joint[ii][1].Value == 2)
                                    {
                                        var rp1 = new List<double>(); rp1.Add(r1[0] + (r2[0] - r1[0]) * pinwidth); rp1.Add(r1[1] + (r2[1] - r1[1]) * pinwidth);
                                        gfx.DrawEllipse(pencil, XBrushes.White, rp1[0] - js / 2.0, rp1[1] - js / 2.0, js, js);//ピン記号
                                    }
                                    if (joint[ii][1].Value == 1 || joint[ii][1].Value == 2)
                                    {
                                        var rp2 = new List<double>(); rp2.Add(r2[0] + (r1[0] - r2[0]) * pinwidth); rp2.Add(r2[1] + (r1[1] - r2[1]) * pinwidth);
                                        gfx.DrawEllipse(pencil, XBrushes.White, rp2[0] - js / 2.0, rp2[1] - js / 2.0, js, js);//ピン記号
                                    }
                                }
                                var position = XStringFormats.BaseLineLeft;
                                if (e % 4 == 1) { position = XStringFormats.CenterRight; }
                                if (e % 4 == 2) { position = XStringFormats.BaseLineRight; }
                                if (e % 4 == 3) { position = XStringFormats.CenterLeft; }
                                if (index_model.Contains(nel) == true)
                                {
                                    if (figname[k] == "長期最大検定比伏図")
                                    {
                                        int aa = kentei_index.IndexOf(nel);
                                        if (aa != -1)
                                        {
                                            var kk = kentei[aa][1].Value;
                                            if (kk > kbound)
                                            {
                                                var color = new XSolidBrush(RGB((1 - Math.Min(kk, 1.0)) * 1.9 / 3.0, 1, 0.5));
                                                gfx.DrawString(kk.ToString().Substring(0, Math.Min(kk.ToString().Length, 4)), font, color, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, position);
                                            }
                                        }
                                    }
                                    if (figname[k] == "短期最大検定比伏図")
                                    {
                                        int aa = kentei_index.IndexOf(nel);
                                        if (aa != -1)
                                        {
                                            var kk = kentei[aa][2].Value;
                                            if (kk > kbound)
                                            {
                                                var color = new XSolidBrush(RGB((1 - Math.Min(kk, 1.0)) * 1.9 / 3.0, 1, 0.5));
                                                gfx.DrawString(kk.ToString().Substring(0, Math.Min(kk.ToString().Length, 4)), font, color, (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0, position);
                                            }
                                        }
                                    }
                                }
                                if (figname[k] == "長期最大検定比伏図(壁床線材置換)" | figname[k] == "短期最大検定比伏図(壁床線材置換)")
                                {
                                    if (nod_No_all.Contains(ni) != true) { nod_No_all.Add(ni); }
                                    if (nod_No_all.Contains(nj) != true) { nod_No_all.Add(nj); }
                                }
                            }
                            if (figname[k] == "長期最大検定比伏図(壁床線材置換)" | figname[k] == "短期最大検定比伏図(壁床線材置換)")
                            {
                                for (int j = 0; j < kabe_w.Count; j++)
                                {
                                    int ni = (int)kabe_w[j][0].Value; int nj = (int)kabe_w[j][1].Value; int nk = (int)kabe_w[j][2].Value; int nl = (int)kabe_w[j][3].Value;
                                    if (nod_No_all.Contains(ni) == true && nod_No_all.Contains(nj) == true && nod_No_all.Contains(nk) == true && kabe_w[j][4].Value != 0)
                                    {
                                        var position = XStringFormats.Center;
                                        var r1 = new List<double>(); r1.Add(offset + (R[ni][0].Value - xmin) * scale); r1.Add(842 - offsety - (R[ni][1].Value - ymin) * scale);
                                        var r2 = new List<double>(); r2.Add(offset + (R[nj][0].Value - xmin) * scale); r2.Add(842 - offsety - (R[nj][1].Value - ymin) * scale);
                                        var r3 = new List<double>(); r3.Add(offset + (R[nk][0].Value - xmin) * scale); r3.Add(842 - offsety - (R[nk][1].Value - ymin) * scale);
                                        var r4 = new List<double>(); r4.Add(offset + (R[nl][0].Value - xmin) * scale); r4.Add(842 - offsety - (R[nl][1].Value - ymin) * scale);
                                        var rp1 = new List<double>(); rp1.Add(r1[0] + (r3[0] - r1[0]) * pinwidth); rp1.Add(r1[1] + (r3[1] - r1[1]) * pinwidth);
                                        var rp2 = new List<double>(); rp2.Add(r2[0] + (r4[0] - r2[0]) * pinwidth); rp2.Add(r2[1] + (r4[1] - r2[1]) * pinwidth);
                                        var rp3 = new List<double>(); rp3.Add(r3[0] + (r1[0] - r3[0]) * pinwidth); rp3.Add(r3[1] + (r1[1] - r3[1]) * pinwidth);
                                        var rp4 = new List<double>(); rp4.Add(r4[0] + (r2[0] - r4[0]) * pinwidth); rp4.Add(r4[1] + (r2[1] - r4[1]) * pinwidth);
                                        var rc = new List<double> { (r1[0] + r2[0] + r3[0] + r4[0]) / 4.0, (r1[1] + r2[1] + r3[1] + r4[1]) / 4.0 };
                                        gfx.DrawLine(pengray, r1[0], r1[1], r3[0], r3[1]); gfx.DrawLine(pengray, r2[0], r2[1], r4[0], r4[1]);//線材置換トラスの描画
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp1[0] - js / 2.0, rp1[1] - js / 2.0, js, js);//ピン記号
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp2[0] - js / 2.0, rp2[1] - js / 2.0, js, js);//ピン記号
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp3[0] - js / 2.0, rp3[1] - js / 2.0, js, js);//ピン記号
                                        gfx.DrawEllipse(pengray, XBrushes.White, rp4[0] - js / 2.0, rp4[1] - js / 2.0, js, js);//ピン記号
                                        int aa = kentei1_index.IndexOf(j);
                                        if (figname[k] == "長期最大検定比伏図(壁床線材置換)" && kentei1[0][0].Value != -9999)
                                        {
                                            if (aa != -1)
                                            {
                                                var kk = Math.Round(kentei1[aa][1].Value, 2);
                                                if (kk > kbound)
                                                {
                                                    var color = new XSolidBrush(RGB((1 - Math.Min(kk, 1.0)) * 1.9 / 3.0, 1, 0.5));
                                                    gfx.DrawString(kk.ToString().Substring(0, Math.Min(kk.ToString().Length, 4)), font, color, rc[0], rc[1], XStringFormats.TopCenter);
                                                }
                                            }
                                        }
                                        if (figname[k] == "短期最大検定比伏図(壁床線材置換)" && kentei1[0][0].Value != -9999)
                                        {
                                            if (aa != -1)
                                            {
                                                var kk = Math.Round(kentei1[aa][2].Value, 2);
                                                if (kk > kbound)
                                                {
                                                    var color = new XSolidBrush(RGB((1 - Math.Min(kk, 1.0)) * 1.9 / 3.0, 1, 0.5));
                                                    gfx.DrawString(kk.ToString().Substring(0, Math.Min(kk.ToString().Length, 4)), font, color, rc[0], rc[1], XStringFormats.TopCenter);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            gfx.DrawString(figname[k] + "(" + plan[i] + ")".ToString(), titlefont, XBrushes.Black, offset, 842 - offset, XStringFormats.BaseLineLeft);
                        }
                    }
                    filename = dir + "/" + pdfname + "_plankentei.pdf";
                    document.Save(filename);// ドキュメントを保存。
                    Process.Start(new ProcessStartInfo(@filename) { UseShellExecute = true });
                    //引張力図
                    if (spring_f[0][0].Value != -9999)
                    {
                        int nf = spring_f[0].Count; var label = new List<string> { "L", "X", "Y", "P" }; var casememo = new List<string> { "(長期荷重時)", "+X荷重時", "+Y荷重時", "接地圧作用時" };
                        DA.GetDataList("casename", label); DA.GetDataList("casememo", casememo);
                        var Tmax = 0.0; var Cmax = 0.0; var Mxmax = 0.0; var Mymax = 0.0; var Mzmax = 0.0; var Qymax = 0.0; var Qzmax = 0.0;
                        for (int ii = 0; ii < nf / 6; ii++)
                        {
                            for (int i = 0; i < spring_f.Count; i++)
                            {
                                Tmax = Math.Max(Tmax, spring_f[i][ii * 6 + 0].Value * nscale2);
                            }
                        }
                        for (int ii = 0; ii < nf / 6; ii++)
                        {
                            document = new PdfDocument();// PDFドキュメントを作成。
                            document.Info.Title = pdfname;
                            document.Info.Author = "Shinnosuke Fujita, Assoc. Prof., The Univ. of Kitakyushu shinnosuke@dn-archi.com";
                            for (int i = 0; i < 100; i++)
                            {
                                List<Curve> lines = new List<Curve>();
                                var ij_new = new List<List<double>>();//その面の要素節点関係
                                var spring_new = new List<List<double>>();//その面の要素節点関係(ばね)
                                var spring_f_new = new List<List<double>>();//その面の断面力(ばね)
                                var line = doc.Objects.FindByUserString(floorname, i.ToString(), true); var nod_P = new List<int>();
                                if (line.Length == 0) { break; }
                                for (int j = 0; j < line.Length; j++)
                                {
                                    var obj = line[j];
                                    if (obj.GetType().ToString() != "Rhino.DocObjects.PointObject")
                                    {

                                        Curve[] l = new Curve[] { (new ObjRef(obj)).Curve() };
                                        int nl = (new ObjRef(obj)).Curve().SpanCount;//ポリラインのセグメント数
                                        if (nl > 1) { l = (new ObjRef(obj)).Curve().DuplicateSegments(); }
                                        for (int jj = 0; jj < nl; jj++)
                                        {
                                            lines.Add(l[jj]);
                                            var p0 = l[jj].PointAtStart; var p1 = l[jj].PointAtEnd; var lgh = l[jj].GetLength();
                                            for (int e = 0; e < ij.Count; e++)
                                            {
                                                var list = new List<double>(); var flist = new List<double>();
                                                int ni = (int)ij[e][0].Value; int nj = (int)ij[e][1].Value;
                                                var ri = new Point3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value);
                                                var rj = new Point3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value);
                                                if (Math.Abs((p0 - ri).Length + (ri - p1).Length - lgh) < 1e-5 && Math.Abs((p0 - rj).Length + (rj - p1).Length - lgh) < 1e-5)
                                                {
                                                    list.Add(e);
                                                    for (int a = 0; a < ij[e].Count; a++) { list.Add(ij[e][a].Value); }
                                                    if (index.Contains(e) == true)
                                                    {
                                                        ij_new.Add(list);
                                                    }
                                                }
                                            }
                                            if (spring[0][0].Value != -9999)
                                            {
                                                for (int ind = 0; ind < index_spring.Count; ind++)
                                                {
                                                    var slist = new List<double>(); var sflist = new List<double>();
                                                    int e = (int)index_spring[ind];
                                                    int ni = (int)spring[e][0].Value; int nj = (int)spring[e][1].Value;
                                                    var ri = new Point3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value);
                                                    var rj = new Point3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value);
                                                    if (Math.Abs((p0 - ri).Length + (ri - p1).Length - lgh) < 1e-5 && Math.Abs((p0 - rj).Length + (rj - p1).Length - lgh) < 1e-5)
                                                    {
                                                        sflist.Add(e); slist.Add(e);
                                                        for (int iii = 0; iii < spring_f[0].Count; iii++)
                                                        {
                                                            sflist.Add(spring_f[e][iii].Value);
                                                        }
                                                        for (int iii = 0; iii < spring[0].Count; iii++)
                                                        {
                                                            slist.Add(spring[e][iii].Value);
                                                        }
                                                        spring_f_new.Add(sflist); spring_new.Add(slist);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        var p = (new ObjRef(obj)).Point().Location;
                                        for (int k = 0; k < R.Count; k++)
                                        {
                                            if (Math.Abs(p[0] - R[k][0].Value) < 5e-3 && Math.Abs(p[1] - R[k][1].Value) < 5e-3 && Math.Abs(p[2] - R[k][2].Value) < 5e-3)
                                            {
                                                nod_P.Add(k);
                                            }
                                        }
                                    }
                                }
                                PdfPage page = new PdfPage(); page.Size = PageSize.A4;// 空白ページを作成。width x height = 594 x 842
                                page = document.AddPage();// 描画するためにXGraphicsオブジェクトを取得。
                                gfx = XGraphics.FromPdfPage(page);
                                var xmin = 9999.0; var xmax = -9999.0; var ymin = 9999.0; var ymax = -9999.0; var zmin = 9999.0; var zmax = -9999.0;
                                for (int e = 0; e < ij_new.Count; e++)
                                {
                                    int ni = (int)ij_new[e][1]; int nj = (int)ij_new[e][2];
                                    xmin = Math.Min(xmin, R[ni][0].Value); xmax = Math.Max(xmax, R[ni][0].Value); xmin = Math.Min(xmin, R[nj][0].Value); xmax = Math.Max(xmax, R[nj][0].Value);
                                    ymin = Math.Min(ymin, R[ni][1].Value); ymax = Math.Max(ymax, R[ni][1].Value); ymin = Math.Min(ymin, R[nj][1].Value); ymax = Math.Max(ymax, R[nj][1].Value);
                                }
                                for (int e = 0; e < spring_new.Count; e++)
                                {
                                    var position = XStringFormats.BaseLineLeft;
                                    if (e % 4 == 1) { position = XStringFormats.TopRight; }
                                    if (e % 4 == 2) { position = XStringFormats.BaseLineRight; }
                                    if (e % 4 == 3) { position = XStringFormats.TopLeft; }
                                    int ni = (int)spring_new[e][1]; int nj = (int)spring_new[e][2]; int nel = (int)spring_new[e][0];
                                    var r1 = new List<double>(); r1.Add(offset + (R[ni][0].Value - xmin) * scale); r1.Add(842 - offsety - (R[ni][1].Value - ymin) * scale);
                                    var r2 = new List<double>(); r2.Add(offset + (R[nj][0].Value - xmin) * scale); r2.Add(842 - offsety - (R[nj][1].Value - ymin) * scale);
                                    gfx.DrawLine(penspring, r1[0], r1[1], r2[0], r2[1]);//ばねの描画
                                    gfx.DrawEllipse(XBrushes.Black, r1[0] - ps / 2.0, r1[1] - ps / 2.0, ps, ps);//i節点の描画
                                    gfx.DrawEllipse(XBrushes.Black, r2[0] - ps / 2.0, r2[1] - ps / 2.0, ps, ps);//j節点の描画
                                    var T = Math.Max(0.0, spring_f[nel][ii * 6].Value); var angle = 0.0;//spring[nel][11].Value;
                                    if (T > Nbound)
                                    {
                                        var ri = new Vector3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value);
                                        var rj = new Vector3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value);
                                        var p1 = new Vector2d(r1[0], r1[1]); var p2 = new Vector2d(r2[0], r2[1]);
                                        var c1 = RGB((1 - Math.Min(T * nscale2 / Tmax, 1.0)) * 1.9 / 3.0, 1, 0.5);
                                        var pen1 = new XPen(c1, T * nscale2 / Tmax * 20); gfx.DrawLine(pen1, p1.X, p1.Y, p2.X, p2.Y);
                                        gfx.DrawString(T.ToString().Substring(0, Math.Min(T.ToString().Length, 4)), font, XBrushes.Black, (p1.X + p2.X) / 2.0, (p1.Y + p2.Y) / 2.0, position);
                                    }
                                }
                                for (int e = 0; e < ij_new.Count; e++)
                                {
                                    int ni = (int)ij_new[e][1]; int nj = (int)ij_new[e][2]; int nel = (int)ij_new[e][0]; int mat = (int)ij_new[e][3]; int sec = (int)ij_new[e][4];
                                    var r1 = new List<double>(); r1.Add(offset + (R[ni][0].Value - xmin) * scale); r1.Add(842 - offsety - (R[ni][1].Value - ymin) * scale);
                                    var r2 = new List<double>(); r2.Add(offset + (R[nj][0].Value - xmin) * scale); r2.Add(842 - offsety - (R[nj][1].Value - ymin) * scale);
                                    var pencil = pen;
                                    if (index_model.Contains(nel) != true) { gfx.DrawLine(pengray2, r1[0], r1[1], r2[0], r2[1]); }
                                    else
                                    {
                                        gfx.DrawLine(pen, r1[0], r1[1], r2[0], r2[1]);//骨組の描画
                                        gfx.DrawEllipse(XBrushes.Black, r1[0] - ps / 2.0, r1[1] - ps / 2.0, ps, ps);//i節点の描画
                                        gfx.DrawEllipse(XBrushes.Black, r2[0] - ps / 2.0, r2[1] - ps / 2.0, ps, ps);//j節点の描画
                                    }
                                    if (index_model.Contains(nel) != true) { pencil = pengray2; }
                                    if (joint_No.Contains(nel) == true)//材端ピン
                                    {
                                        int iii = joint_No.IndexOf(nel);
                                        if (joint[iii][1].Value == 0 || joint[iii][1].Value == 2)
                                        {
                                            var rp1 = new List<double>(); rp1.Add(r1[0] + (r2[0] - r1[0]) * pinwidth); rp1.Add(r1[1] + (r2[1] - r1[1]) * pinwidth);
                                            gfx.DrawEllipse(pencil, XBrushes.White, rp1[0] - js / 2.0, rp1[1] - js / 2.0, js, js);//ピン記号
                                        }
                                        if (joint[iii][1].Value == 1 || joint[iii][1].Value == 2)
                                        {
                                            var rp2 = new List<double>(); rp2.Add(r2[0] + (r1[0] - r2[0]) * pinwidth); rp2.Add(r2[1] + (r1[1] - r2[1]) * pinwidth);
                                            gfx.DrawEllipse(pencil, XBrushes.White, rp2[0] - js / 2.0, rp2[1] - js / 2.0, js, js);//ピン記号
                                        }
                                    }
                                    var position = XStringFormats.BaseLineLeft;
                                    if (e % 4 == 1) { position = XStringFormats.CenterRight; }
                                    if (e % 4 == 2) { position = XStringFormats.BaseLineRight; }
                                    if (e % 4 == 3) { position = XStringFormats.CenterLeft; }
                                }
                                gfx.DrawString(plan[i] + "ばね引張力図" + casememo[ii], titlefont, XBrushes.Black, offset, 842 - offset, XStringFormats.BaseLineLeft);
                            }
                            filename = dir + "/" + pdfname + "_planTspring" + label[ii] + ".pdf";
                            document.Save(filename);// ドキュメントを保存。
                            Process.Start(new ProcessStartInfo(@filename) { UseShellExecute = true });
                        }
                    }
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
                return OpenSeesUtility.Properties.Resources.VisRoofPDF;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("ef95d812-48b1-4a27-a792-065258872957"); }
        }///ここからGUIの作成*****************************************************************************************
        public class CustomGUI : GH_ComponentAttributes
        {
            public CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle radio_rec1;
            private Rectangle radio_rec1_1; private Rectangle text_rec1_1;
            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int height = 17; int radi1 = 7; int radi2 = 4;
                int pitchx = 8; int textheight = 20;
                int width = global_rec.Width;

                radio_rec1 = global_rec; radio_rec1.Y = radio_rec1.Bottom;
                radio_rec1.Height = height;
                global_rec.Height += height;

                radio_rec1_1 = radio_rec1;
                radio_rec1_1.X += 5; radio_rec1_1.Y += 5;
                radio_rec1_1.Height = radi1; radio_rec1_1.Width = radi1;

                text_rec1_1 = radio_rec1_1;
                text_rec1_1.X += pitchx; text_rec1_1.Y -= radi2;
                text_rec1_1.Height = textheight; text_rec1_1.Width = width;

                Bounds = global_rec;
            }
            Brush c1 = Brushes.White;
            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
            {
                base.Render(canvas, graphics, channel);
                if (channel == GH_CanvasChannel.Objects)
                {
                    GH_Capsule radio1 = GH_Capsule.CreateCapsule(radio_rec1, GH_Palette.White, 2, 0);
                    radio1.Render(graphics, Selected, Owner.Locked, false); radio1.Dispose();

                    GH_Capsule radio1_1 = GH_Capsule.CreateCapsule(radio_rec1_1, GH_Palette.Black, 5, 5);
                    radio1_1.Render(graphics, Selected, Owner.Locked, false); radio1_1.Dispose();
                    graphics.FillEllipse(c1, radio_rec1_1);
                    graphics.DrawString("PDF OUTPUT", GH_FontServer.Standard, Brushes.Black, text_rec1_1);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec1 = radio_rec1_1;
                    if (rec1.Contains(e.CanvasLocation))
                    {
                        if (c1 == Brushes.White) { c1 = Brushes.Black; SetButton("1", 1); }
                        else { c1 = Brushes.White; SetButton("1", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}