using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.Display;
using Rhino.DocObjects;

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

namespace kabecheckRC
{
    public class KabecheckRC : GH_Component
    {
        double fontsize = 10.0;
        static int PDF = 0;
        public static PdfCreate.JapaneseFontResolver fontresolver = new PdfCreate.JapaneseFontResolver();
        public static XGraphics gfx;
        public static void SetButton(string s, int i)
        {
            if (s == "1")
            {
                PDF = i;
            }
        }
        int Digit(int num)//数字の桁数を求める関数
        {
            // Mathf.Log10(0)はNegativeInfinityを返すため、別途処理する。
            return (num == 0) ? 1 : ((int)Math.Log10(num) + 1);
        }
        XColor RGB(double h, double s, double l)//convert HSL to RGB
        {
            var max = 0.0; var min = 0.0; var R = 0.0; var G = 0.0; var B = 0.0;
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
            var hp = HUE_MAX / 6.0; h *= HUE_MAX; var Q = h / hp;
            if (Q <= 1)
            {
                R = max;
                G = (h / hp) * (max - min) + min;
                B = min;
            }
            else if (Q <= 2)
            {
                R = ((hp * 2 - h) / hp) * (max - min) + min;
                G = max;
                B = min;
            }
            else if (Q <= 3)
            {
                R = min;
                G = max;
                B = ((h - hp * 2) / hp) * (max - min) + min;
            }
            else if (Q <= 4)
            {
                R = min;
                G = ((hp * 4 - h) / hp) * (max - min) + min;
                B = max;
            }
            else if (Q <= 5)
            {
                R = ((h - hp * 4) / hp) * (max - min) + min;
                G = min;
                B = max;
            }
            else
            {
                R = max;
                G = min;
                B = ((HUE_MAX - h) / hp) * (max - min) + min;
            }
            R *= RGB_MAX; G *= RGB_MAX; B *= RGB_MAX;
            return XColor.FromArgb((int)R, (int)G, (int)B);
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        public KabecheckRC()
          : base("Checking RCKABERYOU", "KabeCheckRC",
              "RCKABERYOU calculation based on Japanese Design Code (AIJ book)",
              "OpenSees", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("floor 1", "floor 1", "[floor1layername1,floor1layername2,...](Datalist)", GH_ParamAccess.list);//0
            pManager.AddTextParameter("floor 2", "floor 2", "[floor2layername1,floor2layername2,...](Datalist)", GH_ParamAccess.list);//1
            pManager.AddTextParameter("floor 3", "floor 3", "[floor3layername1,floor3layername2,...](Datalist)", GH_ParamAccess.list);//2
            pManager.AddTextParameter("floor 4", "floor 4", "[floor3layername1,floor3layername2,...](Datalist)", GH_ParamAccess.list);//3
            pManager.AddTextParameter("floor 5", "floor 5", "[floor3layername1,floor3layername2,...](Datalist)", GH_ParamAccess.list);//4
            pManager.AddTextParameter("wall 1", "wall 1", "[wall1layername1,wall1layername2,...](Datalist)", GH_ParamAccess.list);//5
            pManager.AddTextParameter("wall 2", "wall 2", "[wall2layername1,wall2layername2,...](Datalist)", GH_ParamAccess.list);//6
            pManager.AddTextParameter("wall 3", "wall 3", "[wall3layername1,wall3layername2,...](Datalist)", GH_ParamAccess.list);//7
            pManager.AddTextParameter("wall 4", "wall 4", "[wall2layername1,wall2layername2,...](Datalist)", GH_ParamAccess.list);//8
            pManager.AddTextParameter("wall 5", "wall 5", "[wall3layername1,wall3layername2,...](Datalist)", GH_ParamAccess.list);//9
            pManager.AddNumberParameter("P1", "P1", "[■□HL[:B,〇●:R](DataList)", GH_ParamAccess.list);///10
            pManager.AddNumberParameter("P2", "P2", "[■□HL[:D,〇:t,●:0](DataList)", GH_ParamAccess.list);///11
            pManager.AddNumberParameter("Fc", "Fc", "[Fc1,Fc2,...](DataList)", GH_ParamAccess.list);///12
            pManager.AddNumberParameter("H", "H", "[H1,H2,...](DataList)", GH_ParamAccess.list,new List<double> { 3,3,3,3,3});///構造上主要な鉛直支点間距離
            pManager.AddNumberParameter("Z", "Z", "area coefficient", GH_ParamAccess.item,1.0);///14
            pManager.AddNumberParameter("W", "W", "[W1,W2,...](DataList)", GH_ParamAccess.list);///15
            pManager.AddNumberParameter("Ai", "Ai", "[A1,A2,...](DataList)", GH_ParamAccess.list);///16
            pManager.AddIntegerParameter("nBF", "nBF", "Number of basement floors", GH_ParamAccess.item, 0);///17
            pManager.AddTextParameter("name sec", "name sec", "usertextname for section", GH_ParamAccess.item, "sec");
            pManager.AddTextParameter("name angle", "name angle", "usertextname for code-angle", GH_ParamAccess.item, "angle");
            pManager.AddTextParameter("name mat", "name mat", "usertextname for material", GH_ParamAccess.item, "mat");
            pManager.AddNumberParameter("fontsize", "fontsize", "fontsize", GH_ParamAccess.item, 12.0);//21
            pManager.AddTextParameter("outputname", "outputname", "output file name", GH_ParamAccess.item, "wkabeRC");//22
            pManager[0].Optional = true; pManager[1].Optional = true; pManager[2].Optional = true; pManager[3].Optional = true; pManager[4].Optional = true; pManager[5].Optional = true; pManager[6].Optional = true; pManager[7].Optional = true; pManager[8].Optional = true; pManager[9].Optional = true; pManager[15].Optional = true; pManager[16].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("t", "t", "[[thicknesslist 1F],[thicknesslist 2F]...](DataTree)", GH_ParamAccess.tree);///0
            pManager.AddNumberParameter("L0", "L0", "[[L0list 1F],[L0list 2F]...](DataTree)", GH_ParamAccess.tree);///1
            pManager.AddNumberParameter("theta1", "theta1", "[[theta1list 1F],[theta1list 2F]...](DataTree)", GH_ParamAccess.tree);///2
            pManager.AddNumberParameter("theta2", "theta2", "[[theta2list 1F],[theta2list 2F]...](DataTree)", GH_ParamAccess.tree);///3
            pManager.AddNumberParameter("Lx", "Lx", "[[Lxlist 1F],[Lxlist 2F]...](DataTree)", GH_ParamAccess.tree);///4
            pManager.AddNumberParameter("Ly", "Ly", "[[Lylist 1F],[Lylist 2F]...](DataTree)", GH_ParamAccess.tree);///5
            pManager.AddNumberParameter("Ax", "Ax", "[[Axlist 1F],[Axlist 2F]...](DataTree)", GH_ParamAccess.tree);///6
            pManager.AddNumberParameter("Ay", "Ay", "[[Aylist 1F],[Aylist 2F]...](DataTree)", GH_ParamAccess.tree);///7
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> flayer1 = new List<string>(); if (!DA.GetDataList("floor 1", flayer1)) { flayer1 = new List<string>(); };
            List<string> flayer2 = new List<string>(); if (!DA.GetDataList("floor 2", flayer2)) { flayer2 = new List<string>(); };
            List<string> flayer3 = new List<string>(); if (!DA.GetDataList("floor 3", flayer3)) { flayer3 = new List<string>(); };
            List<string> flayer4 = new List<string>(); if (!DA.GetDataList("floor 4", flayer4)) { flayer4 = new List<string>(); };
            List<string> flayer5 = new List<string>(); if (!DA.GetDataList("floor 5", flayer5)) { flayer5 = new List<string>(); };
            List<string> wlayer1 = new List<string>(); if (!DA.GetDataList("wall 1", wlayer1)) { wlayer1 = new List<string>(); };
            List<string> wlayer2 = new List<string>(); if (!DA.GetDataList("wall 2", wlayer2)) { wlayer2 = new List<string>(); };
            List<string> wlayer3 = new List<string>(); if (!DA.GetDataList("wall 3", wlayer3)) { wlayer3 = new List<string>(); };
            List<string> wlayer4 = new List<string>(); if (!DA.GetDataList("wall 4", wlayer4)) { wlayer4 = new List<string>(); };
            List<string> wlayer5 = new List<string>(); if (!DA.GetDataList("wall 5", wlayer5)) { wlayer5 = new List<string>(); };
            List<double> W = new List<double>(); if (!DA.GetDataList("W", W)) { W = new List<double>(); };
            List<double> Ai = new List<double>(); if (!DA.GetDataList("Ai", Ai)) { Ai = new List<double>(); }; var nBF = 0; DA.GetData("nBF", ref nBF);
            var tmin = new List<double>();
            string name_sec = "sec"; DA.GetData("name sec", ref name_sec); string name_angle = "angle"; DA.GetData("name angle", ref name_angle); string name_mat = "mat"; DA.GetData("name mat", ref name_mat);
            var P1 = new List<double>(); DA.GetDataList("P1", P1); var P2 = new List<double>(); DA.GetDataList("P2", P2); var Fc = new List<double>(); DA.GetDataList("Fc", Fc); var H0 = new List<double>(); DA.GetDataList("H", H0); var Z = 1.0; DA.GetData("Z", ref Z); DA.GetData("fontsize", ref fontsize); var pdfname = "wkabeRC"; DA.GetData("outputname", ref pdfname);
            var t = new List<List<double>>(); var L0 = new List<List<double>>(); var theta1 = new List<List<double>>(); var theta2 = new List<List<double>>(); var Ax = new List<List<double>>(); var Ay = new List<List<double>>(); var Lx = new List<List<double>>(); var Ly = new List<List<double>>(); var H = new List<List<double>>(); var t0 = new List<double>(); var Lw0 = new List<double>(); var Lwm = new List<double>(); var beta = new List<double>(); var sumLx = new List<double>(); var sumLy = new List<double>(); var sumAx = new List<double>(); var sumAy = new List<double>(); var sumL0 = new List<double>();
            var r = new List<List<List<Point3d>>>();
            var tall = new GH_Structure<GH_Number>(); var L0all = new GH_Structure<GH_Number>(); var theta1all = new GH_Structure<GH_Number>(); var theta2all = new GH_Structure<GH_Number>(); var Lxall = new GH_Structure<GH_Number>(); var Lyall = new GH_Structure<GH_Number>(); var Axall = new GH_Structure<GH_Number>(); var Ayall = new GH_Structure<GH_Number>();
            if (flayer1.Count != 0 && wlayer1.Count != 0)
            {
                var A = new List<double>();//各階面積
                var doc = RhinoDoc.ActiveDoc;
                var a = 0.0;
                for (int i = 0; i < flayer1.Count; i++)//1F床
                {
                    var shell = doc.Objects.FindByLayer(flayer1[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        a += s.GetArea();//床面積
                    }
                    if (i == flayer1.Count - 1) { A.Add(a); }
                }
                a = 0.0;
                for (int i = 0; i < flayer2.Count; i++)//1F床
                {
                    var shell = doc.Objects.FindByLayer(flayer2[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        a += s.GetArea();//床面積
                    }
                    if (i == flayer2.Count - 1) { A.Add(a); }
                }
                a = 0.0;
                for (int i = 0; i < flayer3.Count; i++)//1F床
                {
                    var shell = doc.Objects.FindByLayer(flayer3[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        a += s.GetArea();//床面積
                    }
                    if (i == flayer3.Count - 1) { A.Add(a); }
                }
                a = 0.0;
                for (int i = 0; i < flayer4.Count; i++)//1F床
                {
                    var shell = doc.Objects.FindByLayer(flayer4[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        a += s.GetArea();//床面積
                    }
                    if (i == flayer4.Count - 1) { A.Add(a); }
                }
                a = 0.0;
                for (int i = 0; i < flayer5.Count; i++)//1F床
                {
                    var shell = doc.Objects.FindByLayer(flayer5[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        a += s.GetArea();//床面積
                    }
                    if (i == flayer5.Count - 1) { A.Add(a); }
                }
                var t_1 = new List<double>(); var L0_1 = new List<double>(); var theta1_1 = new List<double>(); var theta2_1 = new List<double>(); var H_1 = new List<double>(); var Ax_1 = new List<double>(); var Ay_1 = new List<double>(); var Lx_1 = new List<double>(); var Ly_1 = new List<double>();
                var Fci = new List<double>();
                var Xmin = 9999.0; var Xmax = -9999.0; var Ymin = 9999.0; var Ymax = -9999.0;
                var sumFc = 0.0; int nl = 0; var ri = new List<List<Point3d>>();
                for (int i = 0; i < wlayer1.Count; i++)//1F壁
                {
                    var line = doc.Objects.FindByLayer(wlayer1[i]); nl += line.Length;
                    for (int j = 0; j < line.Length; j++)
                    {
                        var obj = line[j]; var l = (new ObjRef(obj)).Curve(); var h = l.GetLength(); H_1.Add(h);
                        var text = obj.Attributes.GetUserString(name_sec);//断面情報
                        if (text == null) { text = "0"; }
                        int sec = int.Parse(text); var lj = P1[sec]; var tj = P2[sec];
                        L0_1.Add(lj); t_1.Add(tj);
                        text = obj.Attributes.GetUserString(name_angle);//角度情報
                        if (text == null) { text = "0"; }
                        var t1 = 90.0 - double.Parse(text); theta1_1.Add(t1);
                        var r1 = l.PointAt(0.0); var r2 = l.PointAt(1.0);
                        var bvec = r2 - r1; var avec = new Vector3d(0, 0, 1);
                        var t2 = Vector3d.VectorAngle(avec, bvec) * 180 / Math.PI;
                        theta2_1.Add(t2);//立面的な角度
                        text = obj.Attributes.GetUserString(name_mat);//材料情報
                        if (text == null) { text = "0"; }
                        sumFc += Fc[int.Parse(text)];
                        var lx = lj * Math.Pow(Math.Cos(t1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(t2 / 180.0 * Math.PI));//X方向壁量
                        var ly = lj * Math.Pow(Math.Sin(t1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(t2 / 180.0 * Math.PI));//Y方向壁量
                        var ra = new Point3d(r1[0] - lj / 2.0 * Math.Cos(t1 / 180.0 * Math.PI), r1[1] + lj / 2.0 * Math.Sin(t1 / 180.0 * Math.PI), r1[2]);
                        var rb = new Point3d(r1[0] + lj / 2.0 * Math.Cos(t1 / 180.0 * Math.PI), r1[1] - lj / 2.0 * Math.Sin(t1 / 180.0 * Math.PI), r1[2]);
                        ri.Add(new List<Point3d> { ra, rb }); Xmin = Math.Min(Math.Min(Xmin, ra[0]), rb[0]); Xmax = Math.Max(Math.Max(Xmax, ra[0]), rb[0]); Ymin = Math.Min(Math.Min(Ymin, ra[1]), rb[1]); Ymax = Math.Max(Math.Max(Ymax, ra[1]), rb[1]);
                        if (P1[sec] <= h * 0.3) { lx = 0.0; ly = 0.0; }
                        Lx_1.Add(lx); Ly_1.Add(ly); Ax_1.Add(lx * tj); Ay_1.Add(ly * tj);
                    }
                }
                Fci.Add(sumFc / nl); beta.Add(Math.Max(Math.Sqrt(18.0 / (sumFc / nl)), 1.0 / Math.Sqrt(2)));
                L0.Add(L0_1); Lx.Add(Lx_1); Ly.Add(Ly_1); theta1.Add(theta1_1); theta2.Add(theta2_1); t.Add(t_1); H.Add(H_1); Ax.Add(Ax_1); Ay.Add(Ay_1); r.Add(ri);
                var t_2 = new List<double>(); var L0_2 = new List<double>(); var theta1_2 = new List<double>(); var theta2_2 = new List<double>(); var H_2 = new List<double>(); var Ax_2 = new List<double>(); var Ay_2 = new List<double>(); var Lx_2 = new List<double>(); var Ly_2 = new List<double>();
                sumFc = 0.0; nl = 0; ri = new List<List<Point3d>>();
                for (int i = 0; i < wlayer2.Count; i++)//2F壁
                {
                    var line = doc.Objects.FindByLayer(wlayer2[i]); nl += line.Length;
                    for (int j = 0; j < line.Length; j++)
                    {
                        var obj = line[j]; var l = (new ObjRef(obj)).Curve(); var h = l.GetLength(); H_2.Add(h);
                        var text = obj.Attributes.GetUserString(name_sec);//断面情報
                        if (text == null) { text = "0"; }
                        int sec = int.Parse(text); var lj = P1[sec]; var tj = P2[sec];
                        L0_2.Add(lj); t_2.Add(tj);
                        text = obj.Attributes.GetUserString(name_angle);//角度情報
                        if (text == null) { text = "0"; }
                        var t1 = 90.0 - double.Parse(text); theta1_2.Add(t1);
                        var r1 = l.PointAt(0.0); var r2 = l.PointAt(1.0);
                        var bvec = r2 - r1; var avec = new Vector3d(0, 0, 1);
                        var t2 = Vector3d.VectorAngle(avec, bvec) * 180 / Math.PI;
                        theta2_2.Add(t2);//立面的な角度
                        text = obj.Attributes.GetUserString(name_mat);//材料情報
                        if (text == null) { text = "0"; }
                        sumFc += Fc[int.Parse(text)];
                        var lx = lj * Math.Pow(Math.Cos(t1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(t2 / 180.0 * Math.PI));//X方向壁量
                        var ly = lj * Math.Pow(Math.Sin(t1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(t2 / 180.0 * Math.PI));//Y方向壁量
                        var ra = new Point3d(r1[0] - lj / 2.0 * Math.Cos(t1 / 180.0 * Math.PI), r1[1] + lj / 2.0 * Math.Sin(t1 / 180.0 * Math.PI), r1[2]);
                        var rb = new Point3d(r1[0] + lj / 2.0 * Math.Cos(t1 / 180.0 * Math.PI), r1[1] - lj / 2.0 * Math.Sin(t1 / 180.0 * Math.PI), r1[2]);
                        ri.Add(new List<Point3d> { ra, rb }); Xmin = Math.Min(Math.Min(Xmin, ra[0]), rb[0]); Xmax = Math.Max(Math.Max(Xmax, ra[0]), rb[0]); Ymin = Math.Min(Math.Min(Ymin, ra[1]), rb[1]); Ymax = Math.Max(Math.Max(Ymax, ra[1]), rb[1]);
                        if (P1[sec] <= h * 0.3) { lx = 0.0; ly = 0.0; }
                        Lx_2.Add(lx); Ly_2.Add(ly); Ax_2.Add(lx * tj); Ay_2.Add(ly * tj);
                    }
                }
                if (wlayer2.Count != 0)
                {
                    Fci.Add(sumFc / nl); beta.Add(Math.Max(Math.Sqrt(18.0 / (sumFc / nl)), 1.0 / Math.Sqrt(2)));
                    L0.Add(L0_2); Lx.Add(Lx_2); Ly.Add(Ly_2); theta1.Add(theta1_2); theta2.Add(theta2_2); t.Add(t_2); H.Add(H_2); Ax.Add(Ax_2); Ay.Add(Ay_2); r.Add(ri);
                }

                var t_3 = new List<double>(); var L0_3 = new List<double>(); var theta1_3 = new List<double>(); var theta2_3 = new List<double>(); var H_3 = new List<double>(); var Ax_3 = new List<double>(); var Ay_3 = new List<double>(); var Lx_3 = new List<double>(); var Ly_3 = new List<double>();
                sumFc = 0.0; nl = 0; ri = new List<List<Point3d>>();
                for (int i = 0; i < wlayer3.Count; i++)//3F壁
                {
                    var line = doc.Objects.FindByLayer(wlayer3[i]); nl += line.Length;
                    for (int j = 0; j < line.Length; j++)
                    {
                        var obj = line[j]; var l = (new ObjRef(obj)).Curve(); var h = l.GetLength(); H_3.Add(h);
                        var text = obj.Attributes.GetUserString(name_sec);//断面情報
                        if (text == null) { text = "0"; }
                        int sec = int.Parse(text); var lj = P1[sec]; var tj = P2[sec];
                        L0_3.Add(lj); t_3.Add(tj);
                        text = obj.Attributes.GetUserString(name_angle);//角度情報
                        if (text == null) { text = "0"; }
                        var t1 = 90.0 - double.Parse(text); theta1_3.Add(t1);
                        var r1 = l.PointAt(0.0); var r2 = l.PointAt(1.0);
                        var bvec = r2 - r1; var avec = new Vector3d(0, 0, 1);
                        var t2 = Vector3d.VectorAngle(avec, bvec) * 180 / Math.PI;
                        theta2_3.Add(t2);//立面的な角度
                        text = obj.Attributes.GetUserString(name_mat);//材料情報
                        if (text == null) { text = "0"; }
                        sumFc += Fc[int.Parse(text)];
                        var lx = lj * Math.Pow(Math.Cos(t1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(t2 / 180.0 * Math.PI));//X方向壁量
                        var ly = lj * Math.Pow(Math.Sin(t1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(t2 / 180.0 * Math.PI));//Y方向壁量
                        var ra = new Point3d(r1[0] - lj / 2.0 * Math.Cos(t1 / 180.0 * Math.PI), r1[1] + lj / 2.0 * Math.Sin(t1 / 180.0 * Math.PI), r1[2]);
                        var rb = new Point3d(r1[0] + lj / 2.0 * Math.Cos(t1 / 180.0 * Math.PI), r1[1] - lj / 2.0 * Math.Sin(t1 / 180.0 * Math.PI), r1[2]);
                        ri.Add(new List<Point3d> { ra, rb }); Xmin = Math.Min(Math.Min(Xmin, ra[0]), rb[0]); Xmax = Math.Max(Math.Max(Xmax, ra[0]), rb[0]); Ymin = Math.Min(Math.Min(Ymin, ra[1]), rb[1]); Ymax = Math.Max(Math.Max(Ymax, ra[1]), rb[1]);
                        if (P1[sec] <= h * 0.3) { lx = 0.0; ly = 0.0; }
                        Lx_3.Add(lx); Ly_3.Add(ly); Ax_3.Add(lx * tj); Ay_3.Add(ly * tj);
                    }
                }
                if (wlayer3.Count != 0)
                {
                    Fci.Add(sumFc / nl); beta.Add(Math.Max(Math.Sqrt(18.0 / (sumFc / nl)), 1.0 / Math.Sqrt(2)));
                    L0.Add(L0_3); Lx.Add(Lx_3); Ly.Add(Ly_3); theta1.Add(theta1_3); theta2.Add(theta2_3); t.Add(t_3); H.Add(H_3); Ax.Add(Ax_3); Ay.Add(Ay_3); r.Add(ri);
                }

                var t_4 = new List<double>(); var L0_4 = new List<double>(); var theta1_4 = new List<double>(); var theta2_4 = new List<double>(); var H_4 = new List<double>(); var Ax_4 = new List<double>(); var Ay_4 = new List<double>(); var Lx_4 = new List<double>(); var Ly_4 = new List<double>();
                sumFc = 0.0; nl = 0; ri = new List<List<Point3d>>();
                for (int i = 0; i < wlayer4.Count; i++)//4F壁
                {
                    var line = doc.Objects.FindByLayer(wlayer4[i]); nl += line.Length;
                    for (int j = 0; j < line.Length; j++)
                    {
                        var obj = line[j]; var l = (new ObjRef(obj)).Curve(); var h = l.GetLength(); H_4.Add(h);
                        var text = obj.Attributes.GetUserString(name_sec);//断面情報
                        if (text == null) { text = "0"; }
                        int sec = int.Parse(text); var lj = P1[sec]; var tj = P2[sec];
                        L0_4.Add(lj); t_4.Add(tj);
                        text = obj.Attributes.GetUserString(name_angle);//角度情報
                        if (text == null) { text = "0"; }
                        var t1 = 90.0 - double.Parse(text); theta1_4.Add(t1);
                        var r1 = l.PointAt(0.0); var r2 = l.PointAt(1.0);
                        var bvec = r2 - r1; var avec = new Vector3d(0, 0, 1);
                        var t2 = Vector3d.VectorAngle(avec, bvec) * 180 / Math.PI;
                        theta2_4.Add(t2);//立面的な角度
                        text = obj.Attributes.GetUserString(name_mat);//材料情報
                        if (text == null) { text = "0"; }
                        sumFc += Fc[int.Parse(text)];
                        var lx = lj * Math.Pow(Math.Cos(t1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(t2 / 180.0 * Math.PI));//X方向壁量
                        var ly = lj * Math.Pow(Math.Sin(t1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(t2 / 180.0 * Math.PI));//Y方向壁量
                        var ra = new Point3d(r1[0] - lj / 2.0 * Math.Cos(t1 / 180.0 * Math.PI), r1[1] + lj / 2.0 * Math.Sin(t1 / 180.0 * Math.PI), r1[2]);
                        var rb = new Point3d(r1[0] + lj / 2.0 * Math.Cos(t1 / 180.0 * Math.PI), r1[1] - lj / 2.0 * Math.Sin(t1 / 180.0 * Math.PI), r1[2]);
                        ri.Add(new List<Point3d> { ra, rb }); Xmin = Math.Min(Math.Min(Xmin, ra[0]), rb[0]); Xmax = Math.Max(Math.Max(Xmax, ra[0]), rb[0]); Ymin = Math.Min(Math.Min(Ymin, ra[1]), rb[1]); Ymax = Math.Max(Math.Max(Ymax, ra[1]), rb[1]);
                        if (P1[sec] <= h * 0.3) { lx = 0.0; ly = 0.0; }
                        Lx_4.Add(lx); Ly_4.Add(ly); Ax_4.Add(lx * tj); Ay_4.Add(ly * tj);
                    }
                }
                if (wlayer4.Count != 0)
                {
                    Fci.Add(sumFc / nl); beta.Add(Math.Max(Math.Sqrt(18.0 / (sumFc / nl)), 1.0 / Math.Sqrt(2)));
                    L0.Add(L0_4); Lx.Add(Lx_4); Ly.Add(Ly_4); theta1.Add(theta1_4); theta2.Add(theta2_4); t.Add(t_4); H.Add(H_4); Ax.Add(Ax_4); Ay.Add(Ay_4); r.Add(ri);
                }

                var t_5 = new List<double>(); var L0_5 = new List<double>(); var theta1_5 = new List<double>(); var theta2_5 = new List<double>(); var H_5 = new List<double>(); var Ax_5 = new List<double>(); var Ay_5 = new List<double>(); var Lx_5 = new List<double>(); var Ly_5 = new List<double>();
                sumFc = 0.0; nl = 0; ri = new List<List<Point3d>>();
                for (int i = 0; i < wlayer5.Count; i++)//5F壁
                {
                    var line = doc.Objects.FindByLayer(wlayer5[i]); nl += line.Length;
                    for (int j = 0; j < line.Length; j++)
                    {
                        var obj = line[j]; var l = (new ObjRef(obj)).Curve(); var h = l.GetLength(); H_5.Add(h);
                        var text = obj.Attributes.GetUserString(name_sec);//断面情報
                        if (text == null) { text = "0"; }
                        int sec = int.Parse(text); var lj = P1[sec]; var tj = P2[sec];
                        L0_5.Add(lj); t_5.Add(tj);
                        text = obj.Attributes.GetUserString(name_angle);//角度情報
                        if (text == null) { text = "0"; }
                        var t1 = 90.0 - double.Parse(text); theta1_5.Add(t1);
                        var r1 = l.PointAt(0.0); var r2 = l.PointAt(1.0);
                        var bvec = r2 - r1; var avec = new Vector3d(0, 0, 1);
                        var t2 = Vector3d.VectorAngle(avec, bvec) * 180 / Math.PI;
                        theta2_5.Add(t2);//立面的な角度
                        text = obj.Attributes.GetUserString(name_mat);//材料情報
                        if (text == null) { text = "0"; }
                        sumFc += Fc[int.Parse(text)];
                        var lx = lj * Math.Pow(Math.Cos(t1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(t2 / 180.0 * Math.PI));//X方向壁量
                        var ly = lj * Math.Pow(Math.Sin(t1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(t2 / 180.0 * Math.PI));//Y方向壁量
                        var ra = new Point3d(r1[0] - lj / 2.0 * Math.Cos(t1 / 180.0 * Math.PI), r1[1] + lj / 2.0 * Math.Sin(t1 / 180.0 * Math.PI), r1[2]);
                        var rb = new Point3d(r1[0] + lj / 2.0 * Math.Cos(t1 / 180.0 * Math.PI), r1[1] - lj / 2.0 * Math.Sin(t1 / 180.0 * Math.PI), r1[2]);
                        ri.Add(new List<Point3d> { ra, rb }); Xmin = Math.Min(Math.Min(Xmin, ra[0]), rb[0]); Xmax = Math.Max(Math.Max(Xmax, ra[0]), rb[0]); Ymin = Math.Min(Math.Min(Ymin, ra[1]), rb[1]); Ymax = Math.Max(Math.Max(Ymax, ra[1]), rb[1]);
                        if (P1[sec] <= h * 0.3) { lx = 0.0; ly = 0.0; }
                        Lx_5.Add(lx); Ly_5.Add(ly); Ax_5.Add(lx * tj); Ay_5.Add(ly * tj);
                    }
                }
                if (wlayer5.Count != 0)
                {
                    Fci.Add(sumFc / nl); beta.Add(Math.Max(Math.Sqrt(18.0 / (sumFc / nl)), 1.0 / Math.Sqrt(2)));
                    L0.Add(L0_5); Lx.Add(Lx_5); Ly.Add(Ly_5); theta1.Add(theta1_5); theta2.Add(theta2_5); t.Add(t_5); H.Add(H_5); Ax.Add(Ax_5); Ay.Add(Ay_5); r.Add(ri);
                }
                var rangex = Xmax - Xmin; var rangey = Ymax - Ymin; var scaling = 0.95; var scale = Math.Min(594.0 / rangex * scaling, 842.0 / rangey * scaling); var offset = 25.0;
                for (int i = 0; i < t.Count; i++)
                {
                    var tlist = new List<GH_Number>();
                    for (int j = 0; j < t[i].Count; j++)
                    {
                        tlist.Add(new GH_Number(t[i][j]));
                    }
                    tall.AppendRange(tlist, new GH_Path(i));
                }
                for (int i = 0; i < L0.Count; i++)
                {
                    var L0list = new List<GH_Number>();
                    for (int j = 0; j < L0[i].Count; j++)
                    {
                        L0list.Add(new GH_Number(L0[i][j]));
                    }
                    L0all.AppendRange(L0list, new GH_Path(i));
                }
                for (int i = 0; i < theta1.Count; i++)
                {
                    var theta1list = new List<GH_Number>();
                    for (int j = 0; j < theta1[i].Count; j++)
                    {
                        theta1list.Add(new GH_Number(theta1[i][j]));
                    }
                    theta1all.AppendRange(theta1list, new GH_Path(i));
                }
                for (int i = 0; i < theta2.Count; i++)
                {
                    var theta2list = new List<GH_Number>();
                    for (int j = 0; j < theta2[i].Count; j++)
                    {
                        theta2list.Add(new GH_Number(theta2[i][j]));
                    }
                    theta2all.AppendRange(theta2list, new GH_Path(i));
                }
                for (int i = 0; i < Lx.Count; i++)
                {
                    var Lxlist = new List<GH_Number>();
                    for (int j = 0; j < Lx[i].Count; j++)
                    {
                        Lxlist.Add(new GH_Number(Lx[i][j]));
                    }
                    Lxall.AppendRange(Lxlist, new GH_Path(i));
                }
                for (int i = 0; i < Ly.Count; i++)
                {
                    var Lylist = new List<GH_Number>();
                    for (int j = 0; j < Ly[i].Count; j++)
                    {
                        Lylist.Add(new GH_Number(Ly[i][j]));
                    }
                    Lyall.AppendRange(Lylist, new GH_Path(i));
                }
                for (int i = 0; i < Ax.Count; i++)
                {
                    var Axlist = new List<GH_Number>();
                    for (int j = 0; j < Ax[i].Count; j++)
                    {
                        Axlist.Add(new GH_Number(Ax[i][j]));
                    }
                    Axall.AppendRange(Axlist, new GH_Path(i));
                }
                for (int i = 0; i < Ay.Count; i++)
                {
                    var Aylist = new List<GH_Number>();
                    for (int j = 0; j < Ay[i].Count; j++)
                    {
                        Aylist.Add(new GH_Number(Ay[i][j]));
                    }
                    Ayall.AppendRange(Aylist, new GH_Path(i));
                }
                if (A.Count - nBF >= 3)
                {
                    for (int i = 0; i < nBF; i++) { t0.Add(Math.Max(180, H0[i] * 1000 / 18.0)); }
                    for (int i = nBF; i < A.Count; i++)
                    {
                        if (i != A.Count - 1) { t0.Add(Math.Max(180, H0[i] * 1000 / 22.0)); }
                        else { t0.Add(Math.Max(150, H0[i] * 1000 / 22.0)); }
                    }
                }
                else if (A.Count - nBF >= 2)
                {
                    for (int i = 0; i < nBF; i++) { t0.Add(Math.Max(180, H0[i] * 1000 / 18.0)); }
                    for (int i = nBF; i < A.Count; i++)
                    {
                        t0.Add(Math.Max(150, H0[i] * 1000 / 22.0));
                    }
                }
                else if (A.Count - nBF >= 1)
                {
                    for (int i = 0; i < nBF; i++) { t0.Add(Math.Max(180, H0[i] * 1000 / 18.0)); }
                    for (int i = nBF; i < A.Count; i++)
                    {
                        t0.Add(Math.Max(120, H0[i] * 1000 / 26.0));
                    }
                }
                else { for (int i = 0; i < nBF; i++) { t0.Add(Math.Max(180, H0[i] * 1000 / 18.0)); } }
                for (int i = 0; i < nBF; i++) { Lw0.Add(200); Lwm.Add(150); }
                for (int i = nBF; i < A.Count; i++)
                {
                    if (A.Count - i <= 3) { Lw0.Add(120);Lwm.Add(70); }
                    else { Lw0.Add(150); Lwm.Add(100); }
                }
                var floor = new List<string>();
                for (int i = 0; i < nBF; i++) { floor.Add("B" + (nBF - i).ToString()); }
                for (int i = 0; i < A.Count- nBF; i++) { floor.Add((i + 1).ToString()); }
                if (PDF == 1)
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
                            "階","壁番号","t[mm]","L0[m]","θ1[°]","θ2[°]","h[m]","Lx[m]","Ly[m]","Ax[m2]","Ay[m2]"
                        };
                    var offset_x = 25; var offset_y = 25; var pitchy = 13; var text_width = 49.5; PdfPage page = new PdfPage(); page.Size = PageSize.A4;
                    for (int i = 0; i < Ax.Count; i++)
                    {
                        // 空白ページを作成。
                        page = document.AddPage(); gfx = XGraphics.FromPdfPage(page);
                        // 描画するためにXGraphicsオブジェクトを取得。
                        for (int ii = 0; ii < labels.Count; ii++)//ラベル列**************************************************************************
                        {
                            gfx.DrawLine(pen, offset_x + text_width * ii, offset_y, offset_x + text_width * (ii + 1), offset_y);//横線
                            gfx.DrawLine(pen, offset_x + text_width * ii, offset_y, offset_x + text_width * ii, offset_y + pitchy);//縦線
                            gfx.DrawString(labels[ii], font, XBrushes.Black, new XRect(offset_x + text_width * ii, offset_y, text_width, offset_y), XStringFormats.TopCenter);
                            gfx.DrawLine(pen, offset_x + text_width * ii, offset_y + pitchy, offset_x + text_width * (ii + 1), offset_y + pitchy);//横線
                        }
                        gfx.DrawLine(pen, offset_x + text_width * labels.Count, offset_y, offset_x + text_width * labels.Count, offset_y + pitchy);//縦線
                        int e = 0;
                        var sumLxi = 0.0; var sumLyi = 0.0; var sumAxi = 0.0; var sumAyi = 0.0; var sumL0i = 0.0; int n = 61;
                        for (int j = 0; j < Ax[i].Count; j++)
                        {
                            var values = new List<string>();
                            values.Add(floor[i]);//階
                            values.Add(floor[i] + "-"+j.ToString());//階-番号
                            values.Add(Math.Round(t[i][j]*1000,0).ToString()); values.Add(Math.Round(L0[i][j],3).ToString()); values.Add(Math.Round(theta1[i][j], 0).ToString()); values.Add(Math.Round(theta2[i][j], 0).ToString()); values.Add(Math.Round(H[i][j], 3).ToString()); values.Add(Math.Round(Lx[i][j], 3).ToString()); values.Add(Math.Round(Ly[i][j], 3).ToString()); values.Add(Math.Round(Ax[i][j], 3).ToString()); values.Add(Math.Round(Ay[i][j], 3).ToString());
                            sumLxi += Lx[i][j]; sumLyi += Ly[i][j]; sumAxi += Ax[i][j]; sumAyi += Ay[i][j]; sumL0i += L0[i][j];
                            if (e == n - 1)
                            {
                                gfx.DrawLine(pen, offset_x, offset_y + pitchy, offset_x + text_width * (labels.Count - 1), offset_y + pitchy);//横線
                                // 空白ページを作成。
                                page = document.AddPage(); gfx = XGraphics.FromPdfPage(page);
                                // 描画するためにXGraphicsオブジェクトを取得。
                                for (int ii = 0; ii < labels.Count; ii++)//ラベル列**************************************************************************
                                {
                                    gfx.DrawLine(pen, offset_x + text_width * ii, offset_y, offset_x + text_width * (ii + 1), offset_y);//横線
                                    gfx.DrawLine(pen, offset_x + text_width * ii, offset_y, offset_x + text_width * ii, offset_y + pitchy);//縦線
                                    gfx.DrawString(labels[ii], font, XBrushes.Black, new XRect(offset_x + text_width * ii, offset_y, text_width, offset_y), XStringFormats.TopCenter);
                                    gfx.DrawLine(pen, offset_x + text_width * ii, offset_y + pitchy, offset_x + text_width * (ii + 1), offset_y + pitchy);//横線
                                }
                                gfx.DrawLine(pen, offset_x + text_width * labels.Count, offset_y, offset_x + text_width * labels.Count, offset_y + pitchy);//縦線
                                e = 0;
                            }
                            else
                            {
                                for (int k = 0; k < values.Count; k++)
                                {
                                    gfx.DrawLine(pen, offset_x + text_width * k, offset_y + pitchy * (j % n + 1), offset_x + text_width * k, offset_y + pitchy * (j % n + 2));//縦線
                                    if (k == values.Count - 1)
                                    {
                                        gfx.DrawLine(pen, offset_x + text_width * (k + 1), offset_y + pitchy * (j % n + 1), offset_x + text_width * (k + 1), offset_y + pitchy * (j % n + 2));//縦線
                                    }
                                    gfx.DrawString(values[k], font, XBrushes.Black, new XRect(offset_x + text_width * k, offset_y + pitchy * (j % n + 1), text_width, offset_y + pitchy * (j % n + 1)), XStringFormats.TopCenter);
                                }
                            }
                            e += 1;
                        }
                        sumLx.Add(sumLxi); sumLy.Add(sumLyi); sumAx.Add(sumAxi); sumAy.Add(sumAyi); sumL0.Add(sumL0i);
                        gfx.DrawLine(pen, offset_x, offset_y + pitchy * (Ax[i].Count % n + 1), offset_x + text_width * labels.Count, offset_y + pitchy * (Ax[i].Count % n + 1));//横線
                        gfx.DrawLine(pen, offset_x, offset_y + pitchy * (Ax[i].Count % n + 2), offset_x + text_width * labels.Count, offset_y + pitchy * (Ax[i].Count % n + 2));//横線
                        gfx.DrawLine(pen, offset_x, offset_y + pitchy * (Ax[i].Count % n + 1), offset_x, offset_y + pitchy * (Ax[i].Count % n + 2));//縦線
                        gfx.DrawLine(pen, offset_x + text_width * 7, offset_y + pitchy * (Ax[i].Count % n + 1), offset_x + text_width * 7, offset_y + pitchy * (Ax[i].Count % n + 2));//縦線
                        gfx.DrawString(Math.Round(sumLx[i],3).ToString(), font, XBrushes.Black, new XRect(offset_x + text_width * 7, offset_y + pitchy * (Ax[i].Count % n + 1), text_width, offset_y + pitchy * (Ax[i].Count % n + 1)), XStringFormats.TopCenter);
                        gfx.DrawLine(pen, offset_x + text_width * 8, offset_y + pitchy * (Ax[i].Count % n + 1), offset_x + text_width * 8, offset_y + pitchy * (Ax[i].Count % n + 2));//縦線
                        gfx.DrawString(Math.Round(sumLy[i], 3).ToString(), font, XBrushes.Black, new XRect(offset_x + text_width * 8, offset_y + pitchy * (Ax[i].Count % n + 1), text_width, offset_y + pitchy * (Ax[i].Count % n + 1)), XStringFormats.TopCenter);
                        gfx.DrawLine(pen, offset_x + text_width * 9, offset_y + pitchy * (Ax[i].Count % n + 1), offset_x + text_width * 9, offset_y + pitchy * (Ax[i].Count % n + 2));//縦線
                        gfx.DrawString(Math.Round(sumAx[i], 3).ToString(), font, XBrushes.Black, new XRect(offset_x + text_width * 9, offset_y + pitchy * (Ax[i].Count % n + 1), text_width, offset_y + pitchy * (Ax[i].Count % n + 1)), XStringFormats.TopCenter);
                        gfx.DrawLine(pen, offset_x + text_width * 10, offset_y + pitchy * (Ax[i].Count % n + 1), offset_x + text_width * 10, offset_y + pitchy * (Ax[i].Count % n + 2));//縦線
                        gfx.DrawString(Math.Round(sumAy[i], 3).ToString(), font, XBrushes.Black, new XRect(offset_x + text_width * 10, offset_y + pitchy * (Ax[i].Count % n + 1), text_width, offset_y + pitchy * (Ax[i].Count % n + 1)), XStringFormats.TopCenter);
                        gfx.DrawLine(pen, offset_x + text_width * 11, offset_y + pitchy * (Ax[i].Count % n + 1), offset_x + text_width * 11, offset_y + pitchy * (Ax[i].Count % n + 2));//縦線
                    }
                    ///壁配置図の描画///
                    var pen2 = new XPen(XColors.Gray, 2.0);
                    var rxmin = new List<double>(); var rymin = new List<double>();
                    for (int i = 0; i < r.Count; i++)
                    {
                        var xmin = 9999.0; var ymin = 9999.0;
                        for (int j = 0; j < r[i].Count; j++)
                        {
                            xmin = Math.Min(xmin, Math.Min(r[i][j][0][0], r[i][j][1][0]));
                            ymin = Math.Min(ymin, Math.Min(r[i][j][0][1], r[i][j][1][1]));
                        }
                        rxmin.Add(xmin); rymin.Add(ymin);
                    }
                    for (int i = 0; i < r.Count; i++)
                    {
                        // 空白ページを作成。
                        page = document.AddPage(); gfx = XGraphics.FromPdfPage(page);
                        for (int j = 0; j < r[i].Count; j++)
                        {
                            var r1 = new List<double>(); r1.Add(offset + (r[i][j][0][0] - rxmin[i]) * scale); r1.Add(842 - offset - (r[i][j][0][1] - rymin[i]) * scale);
                            var r2 = new List<double>(); r2.Add(offset + (r[i][j][1][0] - rxmin[i]) * scale); r2.Add(842 - offset - (r[i][j][1][1] - rymin[i]) * scale);
                            var rc = new List<double> { (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0 };
                            gfx.DrawLine(pen2, r1[0], r1[1], r2[0], r2[1]);
                        }
                        for (int j = 0; j < r[i].Count; j++)
                        {
                            var r1 = new List<double>(); r1.Add(offset + (r[i][j][0][0] - rxmin[i]) * scale); r1.Add(842 - offset - (r[i][j][0][1] - rymin[i]) * scale);
                            var r2 = new List<double>(); r2.Add(offset + (r[i][j][1][0] - rxmin[i]) * scale); r2.Add(842 - offset - (r[i][j][1][1] - rymin[i]) * scale);
                            var rc = new List<double> { (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0 };
                            gfx.DrawString(floor[i] + "-" + j.ToString(), font, XBrushes.Blue, rc[0], rc[1], XStringFormats.Center);//階-番号
                        }
                    }
                    text_width = 42.0;
                    var labels0 = new List<string>
                        {
                            "","","","Lw0","Lwm","t0","","","A","Lw","","Lw","Lw"
                        };
                    var labels1 = new List<string>
                        {
                            "階","方向","Fc","","","","β","α","","","αβZLwo","",""
                        };
                    var labels2 = new List<string>
                        {
                            "","","","[mm/m2]","[mm/m2]","[mm]","","","[m2]","[mm/m2]","","αβZLwo","Lwm"
                        };
                    // 空白ページを作成。
                    page = document.AddPage(); gfx = XGraphics.FromPdfPage(page);
                    // 描画するためにXGraphicsオブジェクトを取得。
                    for (int ii = 0; ii < labels0.Count; ii++)//ラベル列**************************************************************************
                    {
                        gfx.DrawLine(pen, offset_x + text_width * ii, offset_y, offset_x + text_width * (ii + 1), offset_y);//横線
                        gfx.DrawLine(pen, offset_x + text_width * ii, offset_y, offset_x + text_width * ii, offset_y + pitchy * 2);//縦線
                        gfx.DrawString(labels0[ii], font, XBrushes.Black, new XRect(offset_x + text_width * ii, offset_y, text_width, offset_y), XStringFormats.TopCenter);
                        gfx.DrawLine(pen, offset_x + text_width * ii, offset_y + pitchy * 2, offset_x + text_width * (ii + 1), offset_y + pitchy * 2);//横線
                        if (ii==11 || ii==12)
                        {
                            gfx.DrawLine(pen, offset_x + text_width * ii, offset_y + pitchy, offset_x + text_width * (ii + 1), offset_y + pitchy);//横線
                        }
                        if (ii == labels0.Count - 1)
                        {
                            gfx.DrawLine(pen, offset_x + text_width * (ii + 1), offset_y, offset_x + text_width * (ii + 1), offset_y + pitchy * 2);//縦線
                        }
                    }
                    for (int ii = 0; ii < labels1.Count; ii++)//ラベル列**************************************************************************
                    {
                        gfx.DrawString(labels1[ii], font, XBrushes.Black, new XRect(offset_x + text_width * ii, offset_y + pitchy * 0.5, text_width, offset_y + pitchy * 0.5), XStringFormats.TopCenter);
                    }
                    for (int ii = 0; ii < labels2.Count; ii++)//ラベル列**************************************************************************
                    {
                        gfx.DrawString(labels2[ii], font, XBrushes.Black, new XRect(offset_x + text_width * ii, offset_y + pitchy, text_width, offset_y + pitchy), XStringFormats.TopCenter);
                    }
                    gfx.DrawLine(pen, offset_x + text_width * labels.Count, offset_y, offset_x + text_width * labels.Count, offset_y + pitchy);//縦線
                    for (int i = 0; i < A.Count; i++)
                    {
                        double t_ave = 0.0;
                        for (int j = 0; j< t[i].Count; j++) { t_ave += t[i][j]; } t_ave /= t[i].Count;
                        var alphax = t0[i] * sumLx[i] / sumAx[i] / 1000.0; var alphay = t0[i] * sumLy[i] / sumAy[i] / 1000.0;
                        var Lwx = sumLx[i] * 1000 / A[i]; var Lwy = sumLy[i] * 1000 / A[i];
                        var values0 = new List<string>(); var values1 = new List<string>(); var values2 = new List<string>();
                        values0.Add(""); values0.Add("X"); values0.Add(""); values0.Add(""); values0.Add(""); values0.Add(""); values0.Add(""); values0.Add(Math.Round(alphax,2).ToString()); values0.Add(""); values0.Add(Math.Round(Lwx, 2).ToString()); values0.Add("");
                        if (Lwx / (alphax * beta[i] * Z * Lw0[i]) >= 1.0)
                        {
                            values0.Add(Math.Round(Lwx / (alphax * beta[i] * Z * Lw0[i]), 2).ToString() + ":O.K.");
                        }
                        else
                        {
                            values0.Add(Math.Round(Lwx / (alphax * beta[i] * Z * Lw0[i]), 2).ToString() + ":N.G.");
                        }
                        if (Lwx / Lwm[i] >= 1.0)
                        {
                            values0.Add(Math.Round(Lwx / Lwm[i], 2).ToString() + ":O.K.");
                        }
                        else
                        {
                            values0.Add(Math.Round(Lwx / Lwm[i], 2).ToString() + ":N.G.");
                        }
                        values1.Add(floor[i]);//階
                        values1.Add(""); values1.Add(Math.Round(Fci[i], 1).ToString()); values1.Add(Lw0[i].ToString()); values1.Add(Lwm[i].ToString()); values1.Add(Math.Round(t0[i], 0).ToString()); values1.Add(Math.Round(beta[i], 2).ToString()); values1.Add(""); values1.Add(Math.Round(A[i],2).ToString()); values1.Add(""); values1.Add(Math.Round(alphax * beta[i] * Z * Lw0[i], 2).ToString()); values1.Add(""); values1.Add("");
                        values2.Add(""); values2.Add("Y"); values2.Add(""); values2.Add(""); values2.Add(""); values2.Add(""); values2.Add(""); values2.Add(Math.Round(alphay, 2).ToString()); values2.Add(""); values2.Add(Math.Round(Lwy, 2).ToString()); values2.Add("");
                        if (Lwy / (alphay * beta[i] * Z * Lw0[i]) >= 1.0)
                        {
                            values2.Add(Math.Round(Lwy / (alphay * beta[i] * Z * Lw0[i]), 2).ToString() + ":O.K.");
                        }
                        else
                        {
                            values2.Add(Math.Round(Lwy / (alphay * beta[i] * Z * Lw0[i]), 2).ToString() + ":N.G.");
                        }
                        if (Lwy / Lwm[i] >= 1.0)
                        {
                            values2.Add(Math.Round(Lwy / Lwm[i], 2).ToString() + ":O.K.");
                        }
                        else
                        {
                            values2.Add(Math.Round(Lwy / Lwm[i], 2).ToString() + ":N.G.");
                        }
                        for (int j = 0; j < values0.Count; j++)
                        {
                            gfx.DrawLine(pen, offset_x + text_width * j, offset_y + pitchy * (2 * i + 1 + 1), offset_x + text_width * j, offset_y + pitchy * (2 * i + 3 + 1));//縦線
                            if (values0[j] != "") { gfx.DrawLine(pen, offset_x + text_width * j, offset_y + pitchy * (2 * i + 2 + 1), offset_x + text_width * (j + 1), offset_y + pitchy * (2 * i + 2 + 1)); }//横線
                            gfx.DrawString(values0[j], font, XBrushes.Black, new XRect(offset_x + text_width * j, offset_y + pitchy * (2 * i + 1 + 1), text_width, offset_y + pitchy * (2 * i + 1 + 1)), XStringFormats.TopCenter);
                            if (j== values0.Count - 1)
                            {
                                gfx.DrawLine(pen, offset_x + text_width * (j + 1), offset_y + pitchy * (2 * i + 1 + 1), offset_x + text_width * (j + 1), offset_y + pitchy * (2 * i + 3 + 1));
                            }
                        }
                        for (int j = 0; j < values1.Count; j++)
                        {
                            gfx.DrawString(values1[j], font, XBrushes.Black, new XRect(offset_x + text_width * j, offset_y + pitchy * (2 * i + 1.5 + 1), text_width, offset_y + pitchy * (2 * i + 1.5 + 1)), XStringFormats.TopCenter);
                        }
                        for (int j = 0; j < values2.Count; j++)
                        {
                            gfx.DrawLine(pen, offset_x + text_width * j, offset_y + pitchy * (2 * i + 3 + 1), offset_x + text_width * (j + 1), offset_y + pitchy * (2 * i + 3 + 1));//横線
                            gfx.DrawString(values2[j], font, XBrushes.Black, new XRect(offset_x + text_width * j, offset_y + pitchy * (2 * i + 2 + 1), text_width, offset_y + pitchy * (2 * i + 2 + 1)), XStringFormats.TopCenter);
                        }
                    }
                    text_width = 60.0;
                    labels0 = new List<string>
                        {
                            "","","","Aw","IZWAi","2.5Aw/β",""
                        };
                    labels1 = new List<string>
                        {
                            "階","方向","β","","","","判定"
                        };
                    labels2 = new List<string>
                        {
                            "","","","[mm2]","[mm/m2]","[mm2]",""
                        };
                    if (Ai.Count!=0 & W.Count != 0)
                    {
                        // 空白ページを作成。
                        page = document.AddPage(); gfx = XGraphics.FromPdfPage(page);
                        // 描画するためにXGraphicsオブジェクトを取得。
                        for (int ii = 0; ii < labels0.Count; ii++)//ラベル列**************************************************************************
                        {
                            gfx.DrawLine(pen, offset_x + text_width * ii, offset_y, offset_x + text_width * (ii + 1), offset_y);//横線
                            gfx.DrawLine(pen, offset_x + text_width * ii, offset_y, offset_x + text_width * ii, offset_y + pitchy * 2);//縦線
                            gfx.DrawString(labels0[ii], font, XBrushes.Black, new XRect(offset_x + text_width * ii, offset_y, text_width, offset_y), XStringFormats.TopCenter);
                            gfx.DrawLine(pen, offset_x + text_width * ii, offset_y + pitchy * 2, offset_x + text_width * (ii + 1), offset_y + pitchy * 2);//横線
                            if (ii == 11 || ii == 12)
                            {
                                gfx.DrawLine(pen, offset_x + text_width * ii, offset_y + pitchy, offset_x + text_width * (ii + 1), offset_y + pitchy);//横線
                            }
                            if (ii == labels0.Count - 1)
                            {
                                gfx.DrawLine(pen, offset_x + text_width * (ii + 1), offset_y, offset_x + text_width * (ii + 1), offset_y + pitchy * 2);//縦線
                            }
                        }
                        for (int ii = 0; ii < labels1.Count; ii++)//ラベル列**************************************************************************
                        {
                            gfx.DrawString(labels1[ii], font, XBrushes.Black, new XRect(offset_x + text_width * ii, offset_y + pitchy * 0.5, text_width, offset_y + pitchy * 0.5), XStringFormats.TopCenter);
                        }
                        for (int ii = 0; ii < labels2.Count; ii++)//ラベル列**************************************************************************
                        {
                            gfx.DrawString(labels2[ii], font, XBrushes.Black, new XRect(offset_x + text_width * ii, offset_y + pitchy, text_width, offset_y + pitchy), XStringFormats.TopCenter);
                        }
                        gfx.DrawLine(pen, offset_x + text_width * labels.Count, offset_y, offset_x + text_width * labels.Count, offset_y + pitchy);//縦線
                        for (int i = 0; i < A.Count; i++)
                        {
                            var alphax = t0[i] * sumLx[i] / sumAx[i] / 1000.0; var alphay = t0[i] * sumLy[i] / sumAy[i] / 1000.0;
                            var IZWAi = Z * W[i] * Ai[i] * 1000;
                            var values0 = new List<string>(); var values1 = new List<string>(); var values2 = new List<string>();
                            values0.Add(""); values0.Add("X"); values0.Add(""); values0.Add(Math.Round(sumAx[i]*1000000,0).ToString()); values0.Add(""); values0.Add(Math.Round(sumAx[i] * 2500000/beta[i], 0).ToString());
                            if (sumAx[i] * 2500000 / beta[i] / IZWAi >= 1.0) { values0.Add(Math.Round(sumAx[i] * 2500000 / beta[i] / IZWAi, 2).ToString() + ":O.K."); }
                            else { values0.Add(Math.Round(sumAx[i] * 2500000 / beta[i] / IZWAi, 2).ToString() + ":N.G."); }
                            values1.Add(floor[i]);//階
                            values1.Add(""); values1.Add(Math.Round(beta[i], 2).ToString()); values1.Add(""); values1.Add(Math.Round(IZWAi, 0).ToString()); values1.Add(""); values1.Add("");
                            values2.Add(""); values2.Add("Y"); values2.Add(""); values2.Add(Math.Round(sumAy[i] * 1000000, 0).ToString()); values2.Add(""); values2.Add(Math.Round(sumAy[i] * 2500000 / beta[i], 0).ToString());
                            if (sumAy[i] * 2500000 / beta[i] / IZWAi >= 1.0) { values2.Add(Math.Round(sumAy[i] * 2500000 / beta[i] / IZWAi, 2).ToString() + ":O.K."); }
                            else { values2.Add(Math.Round(sumAy[i] * 2500000 / beta[i] / IZWAi, 2).ToString() + ":N.G."); }
                            for (int j = 0; j < values0.Count; j++)
                            {
                                gfx.DrawLine(pen, offset_x + text_width * j, offset_y + pitchy * (2 * i + 1 + 1), offset_x + text_width * j, offset_y + pitchy * (2 * i + 3 + 1));//縦線
                                if (values0[j] != "") { gfx.DrawLine(pen, offset_x + text_width * j, offset_y + pitchy * (2 * i + 2 + 1), offset_x + text_width * (j + 1), offset_y + pitchy * (2 * i + 2 + 1)); }//横線
                                gfx.DrawString(values0[j], font, XBrushes.Black, new XRect(offset_x + text_width * j, offset_y + pitchy * (2 * i + 1 + 1), text_width, offset_y + pitchy * (2 * i + 1 + 1)), XStringFormats.TopCenter);
                                if (j == values0.Count - 1)
                                {
                                    gfx.DrawLine(pen, offset_x + text_width * (j + 1), offset_y + pitchy * (2 * i + 1 + 1), offset_x + text_width * (j + 1), offset_y + pitchy * (2 * i + 3 + 1));
                                }
                            }
                            for (int j = 0; j < values1.Count; j++)
                            {
                                gfx.DrawString(values1[j], font, XBrushes.Black, new XRect(offset_x + text_width * j, offset_y + pitchy * (2 * i + 1.5 + 1), text_width, offset_y + pitchy * (2 * i + 1.5 + 1)), XStringFormats.TopCenter);
                            }
                            for (int j = 0; j < values2.Count; j++)
                            {
                                gfx.DrawLine(pen, offset_x + text_width * j, offset_y + pitchy * (2 * i + 3 + 1), offset_x + text_width * (j + 1), offset_y + pitchy * (2 * i + 3 + 1));//横線
                                gfx.DrawString(values2[j], font, XBrushes.Black, new XRect(offset_x + text_width * j, offset_y + pitchy * (2 * i + 2 + 1), text_width, offset_y + pitchy * (2 * i + 2 + 1)), XStringFormats.TopCenter);
                            }
                        }
                    }
                    var dir = Path.GetDirectoryName(RhinoDoc.ActiveDoc.Path);
                    // ドキュメントを保存。
                    var filename = dir + "/" + pdfname + ".pdf";
                    document.Save(filename);
                    // ビューアを起動。
                    Process.Start(filename);
                }
            }
            DA.SetDataTree(0, tall); DA.SetDataTree(1, L0all); DA.SetDataTree(2, theta1all); DA.SetDataTree(3, theta2all); DA.SetDataTree(4, Lxall); DA.SetDataTree(5, Lyall); DA.SetDataTree(6, Axall); DA.SetDataTree(7, Ayall);
            
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
                return OpenSeesUtility.Properties.Resources.kabecheckRC;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("af67fccd-77bc-4e87-8ff9-163bb214f46b"); }
        }
        ///ここからGUIの作成*****************************************************************************************
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
                int height = 15; int radi1 = 7; int radi2 = 4;
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