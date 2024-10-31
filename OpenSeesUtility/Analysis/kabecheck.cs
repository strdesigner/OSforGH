using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
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

namespace Kabecheck
{
    public class Kabecheck : GH_Component
    {
        static List<int> Sou = new List<int> { 0, 0, 0 };
        static int Left = 0; static int Right = 0; static int Top = 0; static int Bottom = 0; static int Value = 0;
        double fontsize = 10.0;
        static int PDF = 0;
        public static PdfCreate.JapaneseFontResolver fontresolver = new PdfCreate.JapaneseFontResolver();
        public static XGraphics gfx;
        public static void SetButton(string s, int i)
        {
            if (s == "c11")
            {
                Sou[0] = i;
            }
            else if (s == "c12")
            {
                Sou[1] = i;
            }
            else if (s == "c13")
            {
                Sou[2] = i;
            }
            else if (s == "c21")
            {
                Left = i;
            }
            else if (s == "c22")
            {
                Right = i;
            }
            else if (s == "c31")
            {
                Top = i;
            }
            else if (s == "c32")
            {
                Bottom = i;
            }
            else if (s == "c41")
            {
                Value = i;
            }
            else if (s == "c51")
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
        public Kabecheck()
          : base("Checking KABERYOU", "KabeCheck",
              "KABERYOU calculation based on Japanese Design Code (gray book)",
              "OpenSees", "Analysis")
        {
        }
        public override bool IsPreviewCapable { get { return true; } }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("floor 1", "floor 1", "[floor1layername1,floor1layername2,...](Datalist)", GH_ParamAccess.list);//0
            pManager.AddTextParameter("floor 2", "floor 2", "[floor2layername1,floor2layername2,...](Datalist)", GH_ParamAccess.list);//1
            pManager.AddTextParameter("floor 3", "floor 3", "[floor3layername1,floor3layername2,...](Datalist)", GH_ParamAccess.list);//2
            pManager.AddTextParameter("wall 1", "wall 1", "[wall1layername1,wall1layername2,...](Datalist)", GH_ParamAccess.list);//3
            pManager.AddTextParameter("wall 2", "wall 2", "[wall2layername1,wall2layername2,...](Datalist)", GH_ParamAccess.list);//4
            pManager.AddTextParameter("wall 3", "wall 3", "[wall3layername1,wall3layername2,...](Datalist)", GH_ParamAccess.list);//5
            pManager.AddTextParameter("wind 1X", "wind 1X", "[wind1Xlayername1,wind1Xlayername2,...](Datalist)", GH_ParamAccess.list);//6
            pManager.AddTextParameter("wind 2X", "wind 2X", "[wind2Xlayername1,wind2Xlayername2,...](Datalist)", GH_ParamAccess.list);//7
            pManager.AddTextParameter("wind 3X", "wind 3X", "[wind3Xlayername1,wind3Xlayername2,...](Datalist)", GH_ParamAccess.list);//8
            pManager.AddTextParameter("wind 1Y", "wind 1Y", "[wind1Ylayername1,wind1Ylayername2,...](Datalist)", GH_ParamAccess.list);//9
            pManager.AddTextParameter("wind 2Y", "wind 2Y", "[wind2Ylayername1,wind2Ylayername2,...](Datalist)", GH_ParamAccess.list);//10
            pManager.AddTextParameter("wind 3Y", "wind 3Y", "[wind3Ylayername1,wind3Ylayername2,...](Datalist)", GH_ParamAccess.list);//11
            pManager.AddTextParameter("name K", "name K", "usertextname for kabe/yuka bairitsu", GH_ParamAccess.item, "K");//12
            pManager.AddIntegerParameter("type1", "type1", "light weight roof:1, heavy weight roof:2", GH_ParamAccess.item, 1);//13
            pManager.AddIntegerParameter("type2", "type2", "normal wind(0.5):1, strong wind(0.75):2", GH_ParamAccess.item, 1);//14
            pManager.AddNumberParameter("p", "p", "Result output position", GH_ParamAccess.list, new List<double> { 0, 0, 0 });//15
            pManager.AddNumberParameter("fontsize", "fontsize", "fontsize", GH_ParamAccess.item, 12.0);//16
            pManager.AddTextParameter("outputname", "outputname", "output file name", GH_ParamAccess.item, "wkabe");//17
            pManager[0].Optional = true; pManager[1].Optional = true; pManager[2].Optional = true; pManager[3].Optional = true; pManager[4].Optional = true; pManager[5].Optional = true; pManager[6].Optional = true; pManager[7].Optional = true; pManager[8].Optional = true; pManager[9].Optional = true; pManager[10].Optional = true; pManager[11].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("A", "A", "Area of floors", GH_ParamAccess.list);
            pManager.AddNumberParameter("At", "At", "Area of floors", GH_ParamAccess.list);
            pManager.AddNumberParameter("Ab", "Ab", "Area of floors", GH_ParamAccess.list);
            pManager.AddNumberParameter("Al", "Al", "Area of floors", GH_ParamAccess.list);
            pManager.AddNumberParameter("Ar", "Ar", "Area of floors", GH_ParamAccess.list);
            pManager.AddNumberParameter("Ax", "Ax", "Pressure receiving area for X direction", GH_ParamAccess.list);
            pManager.AddNumberParameter("Ay", "Ay", "Pressure receiving area for Y direction", GH_ParamAccess.list);
            pManager.AddNumberParameter("beta", "beta", "required KABERITSU for seismic load", GH_ParamAccess.list);
            pManager.AddNumberParameter("gamma", "gamma", "required KABERITSU for wind load", GH_ParamAccess.list);
            pManager.AddNumberParameter("Lx", "Lx", "KABERYOU for X direction", GH_ParamAccess.list);
            pManager.AddNumberParameter("Ly", "Ly", "KABERYOU for Y direction", GH_ParamAccess.list);
            pManager.AddNumberParameter("Lxt", "Lxt", "KABERYOU for X direction (top quarter area)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Lxb", "Lxb", "KABERYOU for X direction (bottom quarter area)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Lyl", "Lyl", "KABERYOU for Y direction (left quarter area)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Lyr", "Lyr", "KABERYOU for Y direction (right quarter area)", GH_ParamAccess.list);
            pManager.AddNumberParameter("Qax", "Qax", "total allowable shear force for X direction", GH_ParamAccess.list);///
            pManager.AddNumberParameter("Qay", "Qay", "total allowable shear force for Y direction", GH_ParamAccess.list);///
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
            List<string> wlayer1 = new List<string>(); if (!DA.GetDataList("wall 1", wlayer1)) { wlayer1 = new List<string>(); };
            List<string> wlayer2 = new List<string>(); if (!DA.GetDataList("wall 2", wlayer2)) { wlayer2 = new List<string>(); };
            List<string> wlayer3 = new List<string>(); if (!DA.GetDataList("wall 3", wlayer3)) { wlayer3 = new List<string>(); };
            List<string> xlayer1 = new List<string>(); if (!DA.GetDataList("wind 1X", xlayer1)) { xlayer1 = new List<string>(); };
            List<string> xlayer2 = new List<string>(); if (!DA.GetDataList("wind 2X", xlayer2)) { xlayer2 = new List<string>(); };
            List<string> xlayer3 = new List<string>(); if (!DA.GetDataList("wind 3X", xlayer3)) { xlayer3 = new List<string>(); };
            List<string> ylayer1 = new List<string>(); if (!DA.GetDataList("wind 1Y", ylayer1)) { ylayer1 = new List<string>(); };
            List<string> ylayer2 = new List<string>(); if (!DA.GetDataList("wind 2Y", ylayer2)) { ylayer2 = new List<string>(); };
            List<string> ylayer3 = new List<string>(); if (!DA.GetDataList("wind 3Y", ylayer3)) { ylayer3 = new List<string>(); };
            List<double> P = new List<double> { 0.0, 0.0, 0.0 }; if (!DA.GetDataList("p", P)) { };
            DA.GetData("fontsize", ref fontsize); var pdfname = "wkabe"; DA.GetData("outputname", ref pdfname);
            string name_K = "K"; if (!DA.GetData("name K", ref name_K)) { }; var type1 = 1; DA.GetData("type1", ref type1); var type2 = 1; DA.GetData("type2", ref type2);
            var xy1 = new List<List<double>>(); var xy2 = new List<List<double>>(); var xy3 = new List<List<double>>();
            if (flayer1.Count!=0 && wlayer1.Count != 0)
            {
                var Qax = new List<double>(); var Qay = new List<double>();
                var A = new List<double>();//各階面積
                var K = new List<List<double>>(); var kabe = new List<List<double>>(); var yuka = new List<List<List<Point3d>>>(); var quarter = new List<Brep>();
                var doc = RhinoDoc.ActiveDoc;
                var Xmin = new List<double>(); var Xmax = new List<double>(); var Ymin = new List<double>(); var Ymax = new List<double>(); var Z = new List<double>();
                var xmin = 9999.0; var xmax = -9999.0; var ymin = 9999.0; var ymax = -9999.0; var a = 0.0; var z = 0.0; var f = new List<List<Point3d>>();
                for (int i = 0; i < flayer1.Count; i++)//1F床
                {
                    var shell = doc.Objects.FindByLayer(flayer1[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        a += s.GetArea();//床面積
                        var p = new ObjRef(obj).Surface().ToNurbsSurface().Points;
                        Point3d r1; p.GetPoint(0, 0, out r1); Point3d r2; p.GetPoint(1, 0, out r2); Point3d r3; p.GetPoint(1, 1, out r3); Point3d r4; p.GetPoint(0, 1, out r4);
                        var r = new List<Point3d> { r1, r2, r3, r4 };//床要素の座標を抽出
                        for (int k = 0; k < r.Count; k++)//その階の床の平面的な範囲を計算(4分割法のため)
                        {
                            xmin = Math.Min(xmin, r[k][0]); xmax = Math.Max(xmax, r[k][0]); ymin = Math.Min(ymin, r[k][1]); ymax = Math.Max(ymax, r[k][1]);
                        }
                        z += ((r1 + r2 + r3 + r4) / 4.0)[2];//床の高さ(頂点の平均値として加算しあとで床数で割る)
                        f.Add(new List<Point3d> { r1, r2, r3, r4 });
                    }
                    if (i == flayer1.Count - 1) { Xmin.Add(xmin); Xmax.Add(xmax); Ymin.Add(ymin); Ymax.Add(ymax); A.Add(a); Z.Add(z / (flayer1.Count * shell.Length)); yuka.Add(f); }
                }
                xmin = 9999.0; xmax = -9999.0; ymin = 9999.0; ymax = -9999.0; a = 0.0; z = 0.0; f = new List<List<Point3d>>();
                for (int i = 0; i < flayer2.Count; i++)//2F床
                {
                    var shell = doc.Objects.FindByLayer(flayer2[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        a += s.GetArea();
                        var p = new ObjRef(obj).Surface().ToNurbsSurface().Points;
                        Point3d r1; p.GetPoint(0, 0, out r1); Point3d r2; p.GetPoint(1, 0, out r2); Point3d r3; p.GetPoint(1, 1, out r3); Point3d r4; p.GetPoint(0, 1, out r4);
                        var r = new List<Point3d> { r1, r2, r3, r4 };
                        for (int k = 0; k < r.Count; k++)
                        {
                            xmin = Math.Min(xmin, r[k][0]); xmax = Math.Max(xmax, r[k][0]); ymin = Math.Min(ymin, r[k][1]); ymax = Math.Max(ymax, r[k][1]);
                        }
                        z += ((r1 + r2 + r3 + r4) / 4.0)[2];
                        f.Add(new List<Point3d> { r1, r2, r3, r4 });
                    }
                    if (i == flayer2.Count - 1) { Xmin.Add(xmin); Xmax.Add(xmax); Ymin.Add(ymin); Ymax.Add(ymax); A.Add(a); Z.Add(z / (flayer2.Count * shell.Length)); yuka.Add(f); }
                }
                xmin = 9999.0; xmax = -9999.0; ymin = 9999.0; ymax = -9999.0; a = 0.0; z = 0.0; f = new List<List<Point3d>>();
                for (int i = 0; i < flayer3.Count; i++)//3F床
                {
                    var shell = doc.Objects.FindByLayer(flayer3[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        a += s.GetArea();
                        var p = new ObjRef(obj).Surface().ToNurbsSurface().Points;
                        Point3d r1; p.GetPoint(0, 0, out r1); Point3d r2; p.GetPoint(1, 0, out r2); Point3d r3; p.GetPoint(1, 1, out r3); Point3d r4; p.GetPoint(0, 1, out r4);
                        var r = new List<Point3d> { r1, r2, r3, r4 };
                        for (int k = 0; k < r.Count; k++)
                        {
                            xmin = Math.Min(xmin, r[k][0]); xmax = Math.Max(xmax, r[k][0]); ymin = Math.Min(ymin, r[k][1]); ymax = Math.Max(ymax, r[k][1]);
                        }
                        z += ((r1 + r2 + r3 + r4) / 4.0)[2];
                        f.Add(new List<Point3d> { r1, r2, r3, r4 });
                    }
                    if (i == flayer3.Count - 1) { Xmin.Add(xmin); Xmax.Add(xmax); Ymin.Add(ymin); Ymax.Add(ymax); A.Add(a); Z.Add(z / (flayer3.Count * shell.Length)); yuka.Add(f); }
                }
                var qax = 0.0; var qay = 0.0;
                for (int i = 0; i < wlayer1.Count; i++)//1F壁
                {
                    var shell = doc.Objects.FindByLayer(wlayer1[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        var p = new ObjRef(obj).Surface().ToNurbsSurface().Points;
                        Point3d r1; p.GetPoint(0, 0, out r1); Point3d r2; p.GetPoint(1, 0, out r2); Point3d r3; p.GetPoint(1, 1, out r3); Point3d r4; p.GetPoint(0, 1, out r4);
                        var r = new List<Point3d> { r1, r2, r3, r4 }; var c = (r1 + r2 + r3 + r4) / 4.0; var x = c[0]; var y = c[1]; var alpha = 0.0;
                        var text = obj.Attributes.GetUserString(name_K);//壁量情報
                        if (text != null) { alpha = float.Parse(text); }
                        var width = 0.0; var theta1 = 90.0; var theta2 = 0.0; var alpha2 = Math.Min(5.0, alpha);
                        if (Math.Abs(r1[2] - r2[2]) < Math.Abs(r2[2] - r3[2]))//ij辺が幅方向の時
                        {
                            width = ((r2 - r1).Length + (r4 - r3).Length) / 2.0;
                            if ((new Point3d(r1[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length != 0)
                            {
                                theta1 = Math.Acos((new Point3d(r1[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length / (r2 - r1).Length) / Math.PI * 180.0;
                            }
                            theta2 = Math.Acos(Math.Sqrt(Math.Pow(r2[2] - r3[2], 2)) / (r2 - r3).Length) / Math.PI * 180.0;
                        }
                        else
                        {
                            width = ((r4 - r1).Length + (r2 - r3).Length) / 2.0;
                            if ((new Point3d(r3[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length != 0)
                            {
                                theta1 = Math.Acos((new Point3d(r3[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length / (r2 - r3).Length) / Math.PI * 180.0;
                            }
                            theta2 = Math.Acos(Math.Sqrt(Math.Pow(r1[2] - r2[2], 2)) / (r1 - r2).Length) / Math.PI * 180.0;
                        }
                        kabe.Add(new List<double> { 0, alpha2, width, theta1, theta2, x, y });//層番号，壁倍率，壁幅，θ1，θ2，壁中心のx,y座標
                        qax += alpha * width * Math.Abs(Math.Cos(theta1 / 180 * Math.PI)) * 1.96; qay += alpha * width * Math.Abs(Math.Sin(theta1 / 180 * Math.PI)) * 1.96;
                        xy1.Add(new List<double> { r[0][0], r[0][1], r[1][0], r[1][1] });
                    }
                }
                if (qax != 0.0 && qay != 0.0) { Qax.Add(qax); Qay.Add(qay); }
                qax = 0.0; qay = 0.0;
                for (int i = 0; i < wlayer2.Count; i++)//2F壁
                {
                    var shell = doc.Objects.FindByLayer(wlayer2[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        var p = new ObjRef(obj).Surface().ToNurbsSurface().Points;
                        Point3d r1; p.GetPoint(0, 0, out r1); Point3d r2; p.GetPoint(1, 0, out r2); Point3d r3; p.GetPoint(1, 1, out r3); Point3d r4; p.GetPoint(0, 1, out r4);
                        var r = new List<Point3d> { r1, r2, r3, r4 }; var c = (r1 + r2 + r3 + r4) / 4.0; var x = c[0]; var y = c[1]; var alpha = 0.0;
                        var text = obj.Attributes.GetUserString(name_K);//壁量情報
                        if (text != null) { alpha = float.Parse(text); }
                        var width = 0.0; var theta1 = 90.0; var theta2 = 0.0; var alpha2 = Math.Min(5.0, alpha);
                        if (Math.Abs(r1[2] - r2[2]) < Math.Abs(r2[2] - r3[2]))//ij辺が幅方向の時
                        {
                            width = ((r2 - r1).Length + (r4 - r3).Length) / 2.0;
                            if ((new Point3d(r1[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length != 0)
                            {
                                theta1 = Math.Acos((new Point3d(r1[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length / (r2 - r1).Length) / Math.PI * 180.0;
                            }
                            theta2 = Math.Acos(Math.Sqrt(Math.Pow(r2[2] - r3[2], 2)) / (r2 - r3).Length) / Math.PI * 180.0;
                        }
                        else
                        {
                            width = ((r4 - r1).Length + (r2 - r3).Length) / 2.0;
                            if ((new Point3d(r3[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length != 0)
                            {
                                theta1 = Math.Acos((new Point3d(r3[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length / (r2 - r3).Length) / Math.PI * 180.0;
                            }
                            theta2 = Math.Acos(Math.Sqrt(Math.Pow(r1[2] - r2[2], 2)) / (r1 - r2).Length) / Math.PI * 180.0;
                        }
                        kabe.Add(new List<double> { 1, alpha2, width, theta1, theta2, x, y });//層番号，壁倍率，壁幅，θ1，θ2，壁中心のx,y座標
                        qax += alpha * width * Math.Abs(Math.Cos(theta1 / 180 * Math.PI)) * 1.96; qay += alpha * width * Math.Abs(Math.Sin(theta1 / 180 * Math.PI)) * 1.96;
                        xy2.Add(new List<double> { r[0][0], r[0][1], r[1][0], r[1][1] });
                    }
                }
                if (qax != 0.0 && qay != 0.0) { Qax.Add(qax); Qay.Add(qay); }
                qax = 0.0; qay = 0.0;
                for (int i = 0; i < wlayer3.Count; i++)//3F壁
                {
                    var shell = doc.Objects.FindByLayer(wlayer3[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        var p = new ObjRef(obj).Surface().ToNurbsSurface().Points;
                        Point3d r1; p.GetPoint(0, 0, out r1); Point3d r2; p.GetPoint(1, 0, out r2); Point3d r3; p.GetPoint(1, 1, out r3); Point3d r4; p.GetPoint(0, 1, out r4);
                        var r = new List<Point3d> { r1, r2, r3, r4 }; var c = (r1 + r2 + r3 + r4) / 4.0; var x = c[0]; var y = c[1]; var alpha = 0.0;
                        var text = obj.Attributes.GetUserString(name_K);//壁量情報
                        if (text != null) { alpha = float.Parse(text); }
                        var width = 0.0; var theta1 = 90.0; var theta2 = 0.0; var alpha2 = Math.Min(5.0, alpha);
                        if (Math.Abs(r1[2] - r2[2]) < Math.Abs(r2[2] - r3[2]))//ij辺が幅方向の時
                        {
                            width = ((r2 - r1).Length + (r4 - r3).Length) / 2.0;
                            if ((new Point3d(r1[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length != 0)
                            {
                                theta1 = Math.Acos((new Point3d(r1[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length / (r2 - r1).Length) / Math.PI * 180.0;
                            }
                            theta2 = Math.Acos(Math.Sqrt(Math.Pow(r2[2] - r3[2], 2)) / (r2 - r3).Length) / Math.PI * 180.0;
                        }
                        else
                        {
                            width = ((r4 - r1).Length + (r2 - r3).Length) / 2.0;
                            if ((new Point3d(r3[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length != 0)
                            {
                                theta1 = Math.Acos((new Point3d(r3[0], 0, 0) - new Point3d(r2[0], 0, 0)).Length / (r2 - r3).Length) / Math.PI * 180.0;
                            }
                            theta2 = Math.Acos(Math.Sqrt(Math.Pow(r1[2] - r2[2], 2)) / (r1 - r2).Length) / Math.PI * 180.0;
                        }
                        kabe.Add(new List<double> { 2, alpha2, width, theta1, theta2, x, y });//層番号，壁倍率，壁幅，θ1，θ2，壁中心のx,y座標
                        qax += alpha * width * Math.Abs(Math.Cos(theta1 / 180 * Math.PI)) * 1.96; qay += alpha * width * Math.Abs(Math.Sin(theta1 / 180 * Math.PI)) * 1.96;
                        xy3.Add(new List<double> { r[0][0], r[0][1], r[1][0], r[1][1] });
                    }
                }
                if (qax != 0.0 && qay != 0.0) { Qax.Add(qax); Qay.Add(qay); }
                var beta = new List<double>(); var gamma = 0.5; if (type2 != 1) { gamma = 0.75; }
                var Lx = new List<double> { 0.0, 0.0, 0.0 }; var Ly = new List<double> { 0.0, 0.0, 0.0 };
                var Lnx = new List<double> { 0.0, 0.0, 0.0 }; var Lny = new List<double> { 0.0, 0.0, 0.0 };
                var Lxt = new List<double> { 0.0, 0.0, 0.0 }; var Lyl = new List<double> { 0.0, 0.0, 0.0 };
                var Lxb = new List<double> { 0.0, 0.0, 0.0 }; var Lyr = new List<double> { 0.0, 0.0, 0.0 };
                var Al = new List<double>(); var Ar = new List<double>(); var Ab = new List<double>(); var At = new List<double>();
                if (flayer2.Count == 0 && flayer3.Count == 0)
                {
                    if (type1 == 1) { beta.Add(0.11); }
                    else { beta.Add(0.15); }
                }
                else if (flayer3.Count == 0)
                {
                    if (type1 == 1) { beta.Add(0.29); beta.Add(0.15); }
                    else { beta.Add(0.33); beta.Add(0.21); }
                }
                else
                {
                    if (type1 == 1) { beta.Add(0.46); beta.Add(0.34); beta.Add(0.18); }
                    else { beta.Add(0.5); beta.Add(0.39); beta.Add(0.24); }
                }
                var q = new List<Brep>();
                for (int i = 0; i < beta.Count; i++)//4分割法
                {
                    xmin = Xmin[i]; xmax = Xmax[i]; ymin = Ymin[i]; ymax = Ymax[i]; z = Z[i];
                    var xl = xmin + (xmax - xmin) / 4.0; var xr = xmin + (xmax - xmin) / 4.0 * 3;
                    var yb = ymin + (ymax - ymin) / 4.0; var yt = ymin + (ymax - ymin) / 4.0 * 3;
                    var b = new Polyline(new List<Point3d> { new Point3d(xmin, ymin, z), new Point3d(xmax, ymin, z), new Point3d(xmax, yb, z), new Point3d(xmin, yb, z), new Point3d(xmin, ymin, z) });
                    var t = new Polyline(new List<Point3d> { new Point3d(xmin, yt, z), new Point3d(xmax, yt, z), new Point3d(xmax, ymax, z), new Point3d(xmin, ymax, z), new Point3d(xmin, yt, z) });
                    var l = new Polyline(new List<Point3d> { new Point3d(xmin, ymin, z), new Point3d(xl, ymin, z), new Point3d(xl, ymax, z), new Point3d(xmin, ymax, z), new Point3d(xmin, ymin, z) });
                    var r = new Polyline(new List<Point3d> { new Point3d(xr, ymin, z), new Point3d(xmax, ymin, z), new Point3d(xmax, ymax, z), new Point3d(xr, ymax, z), new Point3d(xr, ymin, z) });
                    q.Add(Brep.CreatePlanarBreps(b.ToNurbsCurve())[0]);
                    q.Add(Brep.CreatePlanarBreps(t.ToNurbsCurve())[0]);
                    q.Add(Brep.CreatePlanarBreps(l.ToNurbsCurve())[0]);
                    q.Add(Brep.CreatePlanarBreps(r.ToNurbsCurve())[0]);
                }
                for (int i = 0; i < beta.Count; i++)
                {
                    xmin = Xmin[i]; xmax = Xmax[i]; ymin = Ymin[i]; ymax = Ymax[i]; z = Z[i];
                    var xl = xmin + (xmax - xmin) / 4.0; var xr = xmin + (xmax - xmin) / 4.0 * 3;
                    var yb = ymin + (ymax - ymin) / 4.0; var yt = ymin + (ymax - ymin) / 4.0 * 3;
                    var al = 0.0; var ar = 0.0; var ab = 0.0; var at = 0.0;
                    for (int j = 0; j < yuka[i].Count; j++)
                    {
                        var r1 = yuka[i][j][0]; var r2 = yuka[i][j][1]; var r3 = yuka[i][j][2]; var r4 = yuka[i][j][3];
                        var x_min = Math.Min(Math.Min(r1[0], r2[0]), Math.Min(r3[0], r4[0])); var x_max = Math.Max(Math.Max(r1[0], r2[0]), Math.Max(r3[0], r4[0]));
                        var y_min = Math.Min(Math.Min(r1[1], r2[1]), Math.Min(r3[1], r4[1])); var y_max = Math.Max(Math.Max(r1[1], r2[1]), Math.Max(r3[1], r4[1]));
                        var b = Brep.CreatePlanarBreps(new Polyline(new List<Point3d> { r1, r2, r3, r4, r1 }).ToNurbsCurve())[0];
                        if (x_min < xl && xl < x_max)
                        {
                            var splitline = new Polyline(new List<Point3d> { new Point3d(xl, -9999, -9999), new Point3d(xl, -9999, 9999), new Point3d(xl, 9999, 9999), new Point3d(xl, 9999, -9999), new Point3d(xl, -9999, -9999) }).ToNurbsCurve();
                            var splitbrep = Brep.CreatePlanarBreps(splitline)[0];
                            var split_b = b.Split(splitbrep, 1e-3);
                            if (split_b.Length != 0)
                            {
                                var b1 = split_b[0]; var b2 = split_b[1];
                                var c1 = b1.GetBoundingBox(true).GetCorners(); var c2 = b2.GetBoundingBox(true).GetCorners();
                                if ((c1[0][0] + c1[1][0] + c1[2][0] + c1[3][0]) / 4.0 <= xl) { al += b1.GetArea(); quarter.Add(b1); if (Left == 1 && Sou[i] == 1) { _shells.Add(b1); _color.Add(Color.GreenYellow); } }
                                if ((c2[0][0] + c2[1][0] + c2[2][0] + c2[3][0]) / 4.0 <= xl) { al += b2.GetArea(); quarter.Add(b2); if (Left == 1 && Sou[i] == 1) { _shells.Add(b2); _color.Add(Color.GreenYellow); } }
                            }
                        }
                        if (x_min < xr && xr < x_max)
                        {
                            var splitline = new Polyline(new List<Point3d> { new Point3d(xr, -9999, -9999), new Point3d(xr, -9999, 9999), new Point3d(xr, 9999, 9999), new Point3d(xr, 9999, -9999), new Point3d(xr, -9999, -9999) }).ToNurbsCurve();
                            var splitbrep = Brep.CreatePlanarBreps(splitline)[0];
                            var split_b = b.Split(splitbrep, 1e-3);
                            if (split_b.Length != 0)
                            {
                                var b1 = split_b[0]; var b2 = split_b[1];
                                var c1 = b1.GetBoundingBox(true).GetCorners(); var c2 = b2.GetBoundingBox(true).GetCorners();
                                if (xr <= (c1[0][0] + c1[1][0] + c1[2][0] + c1[3][0]) / 4.0) { ar += b1.GetArea(); quarter.Add(b1); if (Right == 1 && Sou[i] == 1) { _shells.Add(b1); _color.Add(Color.LightPink); } }
                                if (xr <= (c2[0][0] + c2[1][0] + c2[2][0] + c2[3][0]) / 4.0) { ar += b2.GetArea(); quarter.Add(b2); if (Right == 1 && Sou[i] == 1) { _shells.Add(b2); _color.Add(Color.LightPink); } }
                            }
                        }
                        if (x_max <= xl) { al += b.GetArea(); quarter.Add(b); if (Left == 1 && Sou[i] == 1) { var c = b.GetBoundingBox(true).GetCorners(); _shells.Add(b); _color.Add(Color.GreenYellow); } }
                        if (xr <= x_min) { ar += b.GetArea(); quarter.Add(b); if (Right == 1 && Sou[i] == 1) { var c = b.GetBoundingBox(true).GetCorners(); _shells.Add(b); _color.Add(Color.LightPink); } }
                        if (y_min < yb && yb < y_max)
                        {
                            var splitline = new Polyline(new List<Point3d> { new Point3d(-9999, yb, -9999), new Point3d(-9999, yb, 9999), new Point3d(9999, yb, 9999), new Point3d(9999, yb, -9999), new Point3d(-9999, yb, -9999) }).ToNurbsCurve();
                            var splitbrep = Brep.CreatePlanarBreps(splitline)[0];
                            var split_b = b.Split(splitbrep, 1e-3);
                            if (split_b.Length != 0)
                            {
                                var b1 = split_b[0]; var b2 = split_b[1];
                                var c1 = b1.GetBoundingBox(true).GetCorners(); var c2 = b2.GetBoundingBox(true).GetCorners();
                                if ((c1[0][1] + c1[1][1] + c1[2][1] + c1[3][1]) / 4.0 <= yb) { ab += b1.GetArea(); quarter.Add(b1); if (Bottom == 1 && Sou[i] == 1) { _shells.Add(b1); _color.Add(Color.MidnightBlue); } }
                                if ((c2[0][1] + c2[1][1] + c2[2][1] + c2[3][1]) / 4.0 <= yb) { ab += b2.GetArea(); quarter.Add(b2); if (Bottom == 1 && Sou[i] == 1) { _shells.Add(b2); _color.Add(Color.MidnightBlue); } }
                            }
                        }
                        if (y_min < yt && yt < y_max)
                        {
                            var splitline = new Polyline(new List<Point3d> { new Point3d(-9999, yt, -9999), new Point3d(-9999, yt, 9999), new Point3d(9999, yt, 9999), new Point3d(9999, yt, -9999), new Point3d(-9999, yt, -9999) }).ToNurbsCurve();
                            var splitbrep = Brep.CreatePlanarBreps(splitline)[0];
                            var split_b = b.Split(splitbrep, 1e-3);
                            if (split_b.Length != 0)
                            {
                                var b1 = split_b[0]; var b2 = split_b[1];
                                var c1 = b1.GetBoundingBox(true).GetCorners(); var c2 = b2.GetBoundingBox(true).GetCorners();
                                if (yt <= (c1[0][1] + c1[1][1] + c1[2][1] + c1[3][1]) / 4.0) { at += b1.GetArea(); quarter.Add(b1); if (Top == 1 && Sou[i] == 1) { _shells.Add(b1); _color.Add(Color.Gold); } }
                                if (yt <= (c2[0][1] + c2[1][1] + c2[2][1] + c2[3][1]) / 4.0) { at += b2.GetArea(); quarter.Add(b2); if (Top == 1 && Sou[i] == 1) { _shells.Add(b2); _color.Add(Color.Gold); } }
                            }
                        }
                        if (y_max <= yb) { ab += b.GetArea(); quarter.Add(b); if (Bottom == 1 && Sou[i] == 1) { _shells.Add(b); _color.Add(Color.MidnightBlue); } }
                        if (yt <= y_min) { at += b.GetArea(); quarter.Add(b); if (Top == 1 && Sou[i] == 1) { _shells.Add(b); _color.Add(Color.Gold); } }
                    }
                    Al.Add(al); Ar.Add(ar); Ab.Add(ab); At.Add(at);
                }
                for (int i = 0; i < kabe.Count; i++)
                {
                    var floor = (int)kabe[i][0]; var alpha = kabe[i][1]; var width = kabe[i][2]; var theta1 = kabe[i][3]; var theta2 = kabe[i][4]; var x = kabe[i][5]; var y = kabe[i][6];
                    Lx[floor] += alpha * width * Math.Pow(Math.Cos(theta1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(theta2 / 180.0 * Math.PI));
                    Ly[floor] += alpha * width * Math.Pow(Math.Sin(theta1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(theta2 / 180.0 * Math.PI));
                    xmin = Xmin[floor]; xmax = Xmax[floor]; ymin = Ymin[floor]; ymax = Ymax[floor];
                    var xl = xmin + (xmax - xmin) / 4.0; var xr = xmin + (xmax - xmin) / 4.0 * 3;
                    var yb = ymin + (ymax - ymin) / 4.0; var yt = ymin + (ymax - ymin) / 4.0 * 3;
                    if (x <= xl + 1e-3)
                    {
                        Lyl[floor] += alpha * width * Math.Pow(Math.Sin(theta1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(theta2 / 180.0 * Math.PI)); kabe[i].Add(1);
                    }
                    else { kabe[i].Add(0); }
                    if (xr <= x + 1e-3)
                    {
                        Lyr[floor] += alpha * width * Math.Pow(Math.Sin(theta1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(theta2 / 180.0 * Math.PI)); kabe[i].Add(1);
                    }
                    else { kabe[i].Add(0); }
                    if (yt <= y + 1e-3)
                    {
                        Lxt[floor] += alpha * width * Math.Pow(Math.Cos(theta1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(theta2 / 180.0 * Math.PI)); kabe[i].Add(1);
                    }
                    else { kabe[i].Add(0); }
                    if (y <= yb + 1e-3)
                    {
                        Lxb[floor] += alpha * width * Math.Pow(Math.Cos(theta1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(theta2 / 180.0 * Math.PI)); kabe[i].Add(1);
                    }
                    else { kabe[i].Add(0); }
                }
                var Ax = new List<double>(); var Ay = new List<double>();
                var ax = 0.0;
                for (int i = 0; i < xlayer1.Count; i++)
                {
                    var shell = doc.Objects.FindByLayer(xlayer1[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        ax += s.GetArea();
                    }
                }
                if (xlayer1.Count != 0) { Ax.Add(ax); }
                ax = 0.0;
                for (int i = 0; i < xlayer2.Count; i++)
                {
                    var shell = doc.Objects.FindByLayer(xlayer2[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        ax += s.GetArea();
                    }
                }
                if (xlayer2.Count != 0) { Ax.Add(ax); }
                ax = 0.0;
                for (int i = 0; i < xlayer3.Count; i++)
                {
                    var shell = doc.Objects.FindByLayer(xlayer3[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        ax += s.GetArea();
                    }
                }
                if (xlayer3.Count != 0) { Ax.Add(ax); }
                var ay = 0.0;
                for (int i = 0; i < ylayer1.Count; i++)
                {
                    var shell = doc.Objects.FindByLayer(ylayer1[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        ay += s.GetArea();
                    }
                }
                if (ylayer1.Count != 0) { Ay.Add(ay); }
                ay = 0.0;
                for (int i = 0; i < ylayer2.Count; i++)
                {
                    var shell = doc.Objects.FindByLayer(ylayer2[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        ay += s.GetArea();
                    }
                }
                if (ylayer2.Count != 0) { Ay.Add(ay); }
                ay = 0.0;
                for (int i = 0; i < ylayer3.Count; i++)
                {
                    var shell = doc.Objects.FindByLayer(ylayer3[i]);
                    for (int j = 0; j < shell.Length; j++)
                    {
                        var obj = shell[j];
                        var s = (new ObjRef(obj)).Brep();
                        ay += s.GetArea();
                    }
                }
                if (ylayer3.Count != 0) { Ay.Add(ay); }

                DA.SetDataList("A", A); DA.SetDataList("At", At); DA.SetDataList("Ab", Ab); DA.SetDataList("Al", Al); DA.SetDataList("Ar", Ar); DA.SetDataList("Ax", Ax); DA.SetDataList("Ay", Ay);
                DA.SetDataList("beta", beta);
                DA.SetDataList("Lx", Lx);
                DA.SetDataList("Ly", Ly);
                DA.SetDataList("Lxt", Lxt);
                DA.SetDataList("Lxb", Lxb);
                DA.SetDataList("Lyl", Lyl);
                DA.SetDataList("Lyr", Lyr);
                DA.SetDataList("Qax", Qax);
                DA.SetDataList("Qay", Qay);
                if (Value == 1)
                {
                    _points.Add(new Point3d(P[0], P[1], P[2]));
                    for (int i = 0; i < beta.Count; i++)
                    {
                        _As.Add(A[beta.Count - 1 - i]); _Als.Add(Al[beta.Count - 1 - i]); _Ars.Add(Ar[beta.Count - 1 - i]); _Abs.Add(Ab[beta.Count - 1 - i]); _Ats.Add(At[beta.Count - 1 - i]); _betas.Add(beta[beta.Count - 1 - i]);
                        _Lxs.Add(Lx[beta.Count - 1 - i]); _Lys.Add(Ly[beta.Count - 1 - i]); _Lxts.Add(Lxt[beta.Count - 1 - i]); _Lyls.Add(Lyl[beta.Count - 1 - i]); _Lxbs.Add(Lxb[beta.Count - 1 - i]); _Lyrs.Add(Lyr[beta.Count - 1 - i]);
                    }
                }//pdf作成
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
                            "No.","階","X[m]","Y[m]","L[m]","壁倍率","r1[°]","r2[°]","Lx[m]","Ly[m]","左1/4","右1/4","上1/4","下1/4"
                        };
                    var offset_x = 25; var offset_y = 25; var pitchy = 13; var text_width = 40; PdfPage page = new PdfPage(); page.Size = PageSize.A4;
                    for (int e = 0; e < kabe.Count; e++)
                    {
                        var values = new List<string>();
                        var floor = (int)kabe[e][0]; var alpha = kabe[e][1]; var width = kabe[e][2]; var theta1 = Math.Round(kabe[e][3], 0); var theta2 = Math.Round(kabe[e][4], 0); var x = kabe[e][5]; var y = kabe[e][6];
                        var lx = alpha * width * Math.Pow(Math.Cos(theta1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(theta2 / 180.0 * Math.PI));
                        var ly = alpha * width * Math.Pow(Math.Sin(theta1 / 180.0 * Math.PI), 2) * Math.Abs(Math.Cos(theta2 / 180.0 * Math.PI));
                        values.Add(e.ToString());
                        values.Add(((int)(floor + 1)).ToString() + "F"); values.Add(x.ToString("F6").Substring(0, 4)); values.Add(y.ToString("F6").Substring(0, 4)); values.Add(width.ToString("F6").Substring(0, 4)); values.Add(alpha.ToString("F6").Substring(0, 4)); values.Add(((int)theta1).ToString()); values.Add(((int)theta2).ToString()); values.Add(lx.ToString("F6").Substring(0, 4)); values.Add(ly.ToString("F6").Substring(0, 4));
                        var l4 = ""; var r4 = ""; var t4 = ""; var b4 = "";
                        if (kabe[e][7] == 1) { l4 = "○"; }
                        if (kabe[e][8] == 1) { r4 = "○"; }
                        if (kabe[e][9] == 1) { t4 = "○"; }
                        if (kabe[e][10] == 1) { b4 = "○"; }
                        values.Add(l4); values.Add(r4); values.Add(t4); values.Add(b4);
                        if (e % 60 == 0)
                        {
                            // 空白ページを作成。
                            page = document.AddPage(); gfx = XGraphics.FromPdfPage(page);
                            // 描画するためにXGraphicsオブジェクトを取得。
                            for (int i = 0; i < labels.Count; i++)//ラベル列**************************************************************************
                            {
                                gfx.DrawLine(pen, offset_x + text_width * i, offset_y, offset_x + text_width * (i + 1), offset_y);//横線
                                gfx.DrawLine(pen, offset_x + text_width * i, offset_y, offset_x + text_width * i, offset_y + pitchy);//縦線
                                gfx.DrawString(labels[i], font, XBrushes.Black, new XRect(offset_x + text_width * i, offset_y, text_width, offset_y), XStringFormats.TopCenter);
                                gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy, offset_x + text_width * (i + 1), offset_y + pitchy);//横線
                            }
                            gfx.DrawLine(pen, offset_x + text_width * labels.Count, offset_y, offset_x + text_width * labels.Count, offset_y + pitchy);//縦線
                                                                                                                                                       //***********************************************************************************************************************
                        }
                        for (int i = 0; i < values.Count; i++)
                        {
                            var j = e % 60;
                            gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy * (j + 2), offset_x + text_width * (i + 1), offset_y + pitchy * (j + 2));//横線
                            gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy * (j + 1), offset_x + text_width * i, offset_y + pitchy * (j + 2));//縦線
                            if (i == values.Count - 1)
                            {
                                gfx.DrawLine(pen, offset_x + text_width * (i + 1), offset_y + pitchy * (j + 1), offset_x + text_width * (i + 1), offset_y + pitchy * (j + 2));//縦線
                            }
                            gfx.DrawString(values[i], font, XBrushes.Black, new XRect(offset_x + text_width * i, offset_y + pitchy * (j + 1), text_width, offset_y + pitchy * (j + 1)), XStringFormats.TopCenter);
                        }
                    }
                    var offset = 25.0; var scaling = 0.95; var rxmax = -9999.0; var rymax = -9999.0; var rxmin = 9999.0; var rymin = 9999.0;
                    for (int i = 0; i < Xmax.Count; i++) { rxmax = Math.Max(rxmax, Xmax[i]); }
                    for (int i = 0; i < Xmin.Count; i++) { rxmin = Math.Min(rxmin, Xmin[i]); }
                    for (int i = 0; i < Ymax.Count; i++) { rymax = Math.Max(rymax, Ymax[i]); }
                    for (int i = 0; i < Ymin.Count; i++) { rymin = Math.Min(rymin, Ymin[i]); }
                    var rangex = Xmax[0] - Xmin[0]; var rangey = Ymax[0] - Ymin[0];
                    ///壁配置図の描画1F///
                    if (xy1.Count != 0)
                    {
                        page = document.AddPage(); gfx = XGraphics.FromPdfPage(page);
                        var pen2 = new XPen(XColors.Gray, 2.0); var pen3 = new XPen(XColors.LightPink, 0.75);
                        var scale = Math.Min(594.0 / rangex * scaling, 842.0 / rangey * scaling);
                        for (int i = 0; i < xy1.Count; i++)
                        {
                            var r1 = new List<double>(); r1.Add(offset + (xy1[i][0] - rxmin) * scale); r1.Add(842 - offset - (xy1[i][1] - rymin) * scale);
                            var r2 = new List<double>(); r2.Add(offset + (xy1[i][2] - rxmin) * scale); r2.Add(842 - offset - (xy1[i][3] - rymin) * scale);
                            var rc = new List<double> { (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0 };
                            gfx.DrawLine(pen2, r1[0], r1[1], r2[0], r2[1]);
                            gfx.DrawString(i.ToString(), font, XBrushes.Blue, rc[0], rc[1], XStringFormats.Center);//番号
                        }
                        var q1 = q[0]; var q2 = q[1]; var q3 = q[2]; var q4 = q[3];
                        
                        var p1 = q1.DuplicateVertices(); var xp1list = new List<XPoint>();
                        for (int i = 0; i < p1.Length; i++)
                        {
                            var xpt = new XPoint(offset + (p1[i][0] - rxmin) * scale, 842 - offset - (p1[i][1] - rymin) * scale);
                            xp1list.Add(xpt);
                        }
                        var xp1 = xp1list.ToArray();
                        gfx.DrawPolygon(pen3, XBrushes.Transparent, xp1, XFillMode.Winding);

                        var p2 = q2.DuplicateVertices(); var xp2list = new List<XPoint>();
                        for (int i = 0; i < p2.Length; i++)
                        {
                            var xpt = new XPoint(offset + (p2[i][0] - rxmin) * scale, 842 - offset - (p2[i][1] - rymin) * scale);
                            xp2list.Add(xpt);
                        }
                        var xp2 = xp2list.ToArray();
                        gfx.DrawPolygon(pen3, XBrushes.Transparent, xp2, XFillMode.Winding);

                        var p3 = q3.DuplicateVertices(); var xp3list = new List<XPoint>();
                        for (int i = 0; i < p3.Length; i++)
                        {
                            var xpt = new XPoint(offset + (p3[i][0] - rxmin) * scale, 842 - offset - (p3[i][1] - rymin) * scale);
                            xp3list.Add(xpt);
                        }
                        var xp3 = xp3list.ToArray();
                        gfx.DrawPolygon(pen3, XBrushes.Transparent, xp3, XFillMode.Winding);

                        var p4 = q4.DuplicateVertices(); var xp4list = new List<XPoint>();
                        for (int i = 0; i < p4.Length; i++)
                        {
                            var xpt = new XPoint(offset + (p4[i][0] - rxmin) * scale, 842 - offset - (p4[i][1] - rymin) * scale);
                            xp4list.Add(xpt);
                        }
                        var xp4 = xp4list.ToArray();
                        gfx.DrawPolygon(pen3, XBrushes.Transparent, xp4, XFillMode.Winding);
                    }
                    ///壁配置図の描画2F///
                    int ii = xy1.Count;
                    if (xy2.Count != 0)
                    {
                        page = document.AddPage(); gfx = XGraphics.FromPdfPage(page);
                        var pen2 = new XPen(XColors.Gray, 2.0); var pen3 = new XPen(XColors.LightPink, 0.75);
                        var scale = Math.Min(594.0 / rangex * scaling, 842.0 / rangey * scaling);
                        for (int i = 0; i < xy2.Count; i++)
                        {
                            var r1 = new List<double>(); r1.Add(offset + (xy2[i][0] - rxmin) * scale); r1.Add(842 - offset - (xy2[i][1] - rymin) * scale);
                            var r2 = new List<double>(); r2.Add(offset + (xy2[i][2] - rxmin) * scale); r2.Add(842 - offset - (xy2[i][3] - rymin) * scale);
                            var rc = new List<double> { (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0 };
                            gfx.DrawLine(pen2, r1[0], r1[1], r2[0], r2[1]);
                            gfx.DrawString((ii + i).ToString(), font, XBrushes.Blue, rc[0], rc[1], XStringFormats.Center);//番号
                        }
                        var q1 = q[4]; var q2 = q[5]; var q3 = q[6]; var q4 = q[7];

                        var p1 = q1.DuplicateVertices(); var xp1list = new List<XPoint>();
                        for (int i = 0; i < p1.Length; i++)
                        {
                            var xpt = new XPoint(offset + (p1[i][0] - rxmin) * scale, 842 - offset - (p1[i][1] - rymin) * scale);
                            xp1list.Add(xpt);
                        }
                        var xp1 = xp1list.ToArray();
                        gfx.DrawPolygon(pen3, XBrushes.Transparent, xp1, XFillMode.Winding);

                        var p2 = q2.DuplicateVertices(); var xp2list = new List<XPoint>();
                        for (int i = 0; i < p2.Length; i++)
                        {
                            var xpt = new XPoint(offset + (p2[i][0] - rxmin) * scale, 842 - offset - (p2[i][1] - rymin) * scale);
                            xp2list.Add(xpt);
                        }
                        var xp2 = xp2list.ToArray();
                        gfx.DrawPolygon(pen3, XBrushes.Transparent, xp2, XFillMode.Winding);

                        var p3 = q3.DuplicateVertices(); var xp3list = new List<XPoint>();
                        for (int i = 0; i < p3.Length; i++)
                        {
                            var xpt = new XPoint(offset + (p3[i][0] - rxmin) * scale, 842 - offset - (p3[i][1] - rymin) * scale);
                            xp3list.Add(xpt);
                        }
                        var xp3 = xp3list.ToArray();
                        gfx.DrawPolygon(pen3, XBrushes.Transparent, xp3, XFillMode.Winding);

                        var p4 = q4.DuplicateVertices(); var xp4list = new List<XPoint>();
                        for (int i = 0; i < p4.Length; i++)
                        {
                            var xpt = new XPoint(offset + (p4[i][0] - rxmin) * scale, 842 - offset - (p4[i][1] - rymin) * scale);
                            xp4list.Add(xpt);
                        }
                        var xp4 = xp4list.ToArray();
                        gfx.DrawPolygon(pen3, XBrushes.Transparent, xp4, XFillMode.Winding);
                    }
                    ///壁配置図の描画3F///
                    ii += xy2.Count;
                    if (xy3.Count != 0)
                    {
                        page = document.AddPage(); gfx = XGraphics.FromPdfPage(page);
                        var pen2 = new XPen(XColors.Gray, 2.0); var pen3 = new XPen(XColors.LightPink, 0.75);
                        var scale = Math.Min(594.0 / rangex * scaling, 842.0 / rangey * scaling);
                        for (int i = 0; i < xy3.Count; i++)
                        {
                            var r1 = new List<double>(); r1.Add(offset + (xy3[i][0] - rxmin) * scale); r1.Add(842 - offset - (xy3[i][1] - rymin) * scale);
                            var r2 = new List<double>(); r2.Add(offset + (xy3[i][2] - rxmin) * scale); r2.Add(842 - offset - (xy3[i][3] - rymin) * scale);
                            var rc = new List<double> { (r1[0] + r2[0]) / 2.0, (r1[1] + r2[1]) / 2.0 };
                            gfx.DrawLine(pen2, r1[0], r1[1], r2[0], r2[1]);
                            gfx.DrawString((ii + i).ToString(), font, XBrushes.Blue, rc[0], rc[1], XStringFormats.Center);//番号
                        }
                        var q1 = q[8]; var q2 = q[9]; var q3 = q[10]; var q4 = q[11];

                        var p1 = q1.DuplicateVertices(); var xp1list = new List<XPoint>();
                        for (int i = 0; i < p1.Length; i++)
                        {
                            var xpt = new XPoint(offset + (p1[i][0] - rxmin) * scale, 842 - offset - (p1[i][1] - rymin) * scale);
                            xp1list.Add(xpt);
                        }
                        var xp1 = xp1list.ToArray();
                        gfx.DrawPolygon(pen3, XBrushes.Transparent, xp1, XFillMode.Winding);

                        var p2 = q2.DuplicateVertices(); var xp2list = new List<XPoint>();
                        for (int i = 0; i < p2.Length; i++)
                        {
                            var xpt = new XPoint(offset + (p2[i][0] - rxmin) * scale, 842 - offset - (p2[i][1] - rymin) * scale);
                            xp2list.Add(xpt);
                        }
                        var xp2 = xp2list.ToArray();
                        gfx.DrawPolygon(pen3, XBrushes.Transparent, xp2, XFillMode.Winding);

                        var p3 = q3.DuplicateVertices(); var xp3list = new List<XPoint>();
                        for (int i = 0; i < p3.Length; i++)
                        {
                            var xpt = new XPoint(offset + (p3[i][0] - rxmin) * scale, 842 - offset - (p3[i][1] - rymin) * scale);
                            xp3list.Add(xpt);
                        }
                        var xp3 = xp3list.ToArray();
                        gfx.DrawPolygon(pen3, XBrushes.Transparent, xp3, XFillMode.Winding);

                        var p4 = q4.DuplicateVertices(); var xp4list = new List<XPoint>();
                        for (int i = 0; i < p4.Length; i++)
                        {
                            var xpt = new XPoint(offset + (p4[i][0] - rxmin) * scale, 842 - offset - (p4[i][1] - rymin) * scale);
                            xp4list.Add(xpt);
                        }
                        var xp4 = xp4list.ToArray();
                        gfx.DrawPolygon(pen3, XBrushes.Transparent, xp4, XFillMode.Winding);
                    }

                    // 空白ページを作成。
                    page = document.AddPage(); gfx = XGraphics.FromPdfPage(page);
                    // 描画するためにXGraphicsオブジェクトを取得。
                    text_width = 67;
                    labels = new List<string> { "階", "面積A[m2]", "壁率β[m/m2]", "必要壁量Ln[m]", "X方向壁量Lx[m]", "X方向充足率", "Y方向壁量Lx[m]", "Y方向充足率" };
                    for (int i = 0; i < labels.Count; i++)//ラベル列**************************************************************************
                    {
                        gfx.DrawLine(pen, offset_x + text_width * i, offset_y, offset_x + text_width * (i + 1), offset_y);//横線
                        gfx.DrawLine(pen, offset_x + text_width * i, offset_y, offset_x + text_width * i, offset_y + pitchy);//縦線
                        gfx.DrawString(labels[i], font, XBrushes.Black, new XRect(offset_x + text_width * i, offset_y, text_width, offset_y), XStringFormats.TopCenter);
                        gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy, offset_x + text_width * (i + 1), offset_y + pitchy);//横線
                    }
                    gfx.DrawLine(pen, offset_x + text_width * labels.Count, offset_y, offset_x + text_width * labels.Count, offset_y + pitchy);//縦線
                    for (int e = beta.Count - 1; e > -1; e--)
                    {
                        var values = new List<string>();
                        values.Add(((int)(e + 1)).ToString() + "F"); values.Add(A[e].ToString("F6").Substring(0, Math.Max(4, Digit((int)A[e]) + 2))); values.Add(beta[e].ToString("F6").Substring(0, 4)); values.Add((A[e] * beta[e]).ToString("F6").Substring(0, Math.Max(4, Digit((int)(A[e] * beta[e])) + 2)));
                        var textx = ":N.G."; var texty = ":N.G.";
                        values.Add(Lx[e].ToString("F6").Substring(0, Math.Max(4, Digit((int)Lx[e]) + 2)));
                        if (Lx[e] / (A[e] * beta[e]) >= 1) { textx = ":O.K."; }
                        values.Add((Lx[e] / (A[e] * beta[e])).ToString("F6").Substring(0, 4) + textx);
                        values.Add(Ly[e].ToString("F6").Substring(0, Math.Max(4, Digit((int)Ly[e]) + 2)));
                        if (Ly[e] / (A[e] * beta[e]) >= 1) { texty = ":O.K."; }
                        values.Add((Ly[e] / (A[e] * beta[e])).ToString("F6").Substring(0, 4) + texty);
                        for (int i = 0; i < values.Count; i++)
                        {
                            var j = (beta.Count - 1 - e);
                            gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy * (j + 2), offset_x + text_width * (i + 1), offset_y + pitchy * (j + 2));//横線
                            gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy * (j + 1), offset_x + text_width * i, offset_y + pitchy * (j + 2));//縦線
                            if (i == values.Count - 1)
                            {
                                gfx.DrawLine(pen, offset_x + text_width * (i + 1), offset_y + pitchy * (j + 1), offset_x + text_width * (i + 1), offset_y + pitchy * (j + 2));//縦線
                            }
                            gfx.DrawString(values[i], font, XBrushes.Black, new XRect(offset_x + text_width * i, offset_y + pitchy * (j + 1), text_width, offset_y + pitchy * (j + 1)), XStringFormats.TopCenter);
                        }
                    }
                    // 空白ページを作成。
                    page = document.AddPage(); gfx = XGraphics.FromPdfPage(page);
                    // 描画するためにXGraphicsオブジェクトを取得。
                    text_width = 60;
                    labels = new List<string> { "階", "領域", "面積A[m2]", "壁率β[m/m2]", "必要壁量Ln[m]", "壁量L[m]", "充足率", "壁率比", "総合判定" };
                    for (int i = 0; i < labels.Count; i++)//ラベル列**************************************************************************
                    {
                        gfx.DrawLine(pen, offset_x + text_width * i, offset_y, offset_x + text_width * (i + 1), offset_y);//横線
                        gfx.DrawLine(pen, offset_x + text_width * i, offset_y, offset_x + text_width * i, offset_y + pitchy);//縦線
                        gfx.DrawString(labels[i], font, XBrushes.Black, new XRect(offset_x + text_width * i, offset_y, text_width, offset_y), XStringFormats.TopCenter);
                        gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy, offset_x + text_width * (i + 1), offset_y + pitchy);//横線
                    }
                    gfx.DrawLine(pen, offset_x + text_width * labels.Count, offset_y, offset_x + text_width * labels.Count, offset_y + pitchy);//縦線
                    for (int e = beta.Count - 1; e > -1; e--)
                    {
                        var text = ":N.G."; var text2 = ":N.G."; var hantei = "○";////////////上1/4
                        var kaberitsuhi = Math.Min((Lxt[e] / (At[e] * beta[e])) / (Lxb[e] / (Ab[e] * beta[e])), (Lxb[e] / (Ab[e] * beta[e])) / (Lxt[e] / (At[e] * beta[e])));
                        if (kaberitsuhi >= 0.5) { text2 = ":O.K."; }
                        var values = new List<string>();
                        values.Add(((int)(e + 1)).ToString() + "F"); values.Add("上1/4"); values.Add(At[e].ToString("F6").Substring(0, Math.Max(4, Digit((int)At[e]) + 2))); values.Add(beta[e].ToString("F6").Substring(0, 4)); values.Add((At[e] * beta[e]).ToString("F6").Substring(0, Math.Max(4, Digit((int)(At[e] * beta[e])) + 2)));
                        values.Add(Lxt[e].ToString("F6").Substring(0, Math.Max(4, Digit((int)Lxt[e]) + 2)));
                        if (Lxt[e] / (At[e] * beta[e]) >= 1) { text = ":O.K."; }
                        values.Add((Lxt[e] / (At[e] * beta[e])).ToString("F6").Substring(0, 4) + text);
                        values.Add(kaberitsuhi.ToString("F6").Substring(0, 4) + text2);
                        if (text == ":N.G." && text2 == ":N.G.") { hantei = "×"; }
                        values.Add(hantei);
                        for (int i = 0; i < values.Count; i++)
                        {
                            var j = (beta.Count - 1 - e) * 4;
                            if (i != 0 && i != 7 && i != 8)
                            {
                                gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy * (j + 2), offset_x + text_width * (i + 1), offset_y + pitchy * (j + 2));
                            }//横線
                            gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy * (j + 1), offset_x + text_width * i, offset_y + pitchy * (j + 2));//縦線
                            if (i == values.Count - 1)
                            {
                                gfx.DrawLine(pen, offset_x + text_width * (i + 1), offset_y + pitchy * (j + 1), offset_x + text_width * (i + 1), offset_y + pitchy * (j + 2));//縦線
                            }
                            if (i == 0)
                            {
                                gfx.DrawString(values[i], font, XBrushes.Black, new XRect(offset_x + text_width * i, offset_y + pitchy * (j + 2.5), text_width, offset_y + pitchy * (j + 2.5)), XStringFormats.TopCenter);
                            }
                            else if (i < 7)
                            {
                                gfx.DrawString(values[i], font, XBrushes.Black, new XRect(offset_x + text_width * i, offset_y + pitchy * (j + 1), text_width, offset_y + pitchy * (j + 1)), XStringFormats.TopCenter);
                            }
                            else
                            {
                                gfx.DrawString(values[i], font, XBrushes.Black, new XRect(offset_x + text_width * i, offset_y + pitchy * (j + 1.5), text_width, offset_y + pitchy * (j + 1.5)), XStringFormats.TopCenter);
                            }
                        }
                        text = ":N.G."; text2 = ":N.G."; hantei = "○";////////////下1/4
                        if (kaberitsuhi >= 0.5) { text2 = ":O.K."; }
                        values = new List<string>();
                        values.Add(""); values.Add("下1/4"); values.Add(Ab[e].ToString("F6").Substring(0, Math.Max(4, Digit((int)Ab[e]) + 2))); values.Add(beta[e].ToString("F6").Substring(0, 4)); values.Add((Ab[e] * beta[e]).ToString("F6").Substring(0, Math.Max(4, Digit((int)(Ab[e] * beta[e])) + 2)));
                        values.Add(Lxb[e].ToString("F6").Substring(0, Math.Max(4, Digit((int)Lxb[e]) + 2)));
                        if (Lxb[e] / (Ab[e] * beta[e]) >= 1) { text = ":O.K."; }
                        values.Add((Lxb[e] / (Ab[e] * beta[e])).ToString("F6").Substring(0, 4) + text);
                        values.Add(kaberitsuhi.ToString("F6").Substring(0, 4) + text2);
                        if (text == ":N.G." && text2 == ":N.G.") { hantei = "×"; }
                        values.Add(hantei);
                        for (int i = 0; i < values.Count; i++)
                        {
                            var j = (beta.Count - 1 - e) * 4 + 1;
                            if (i != 0)
                            {
                                gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy * (j + 2), offset_x + text_width * (i + 1), offset_y + pitchy * (j + 2));
                            }//横線
                            gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy * (j + 1), offset_x + text_width * i, offset_y + pitchy * (j + 2));//縦線
                            if (i == values.Count - 1)
                            {
                                gfx.DrawLine(pen, offset_x + text_width * (i + 1), offset_y + pitchy * (j + 1), offset_x + text_width * (i + 1), offset_y + pitchy * (j + 2));//縦線
                            }
                            if (i < 7)
                            {
                                gfx.DrawString(values[i], font, XBrushes.Black, new XRect(offset_x + text_width * i, offset_y + pitchy * (j + 1), text_width, offset_y + pitchy * (j + 1)), XStringFormats.TopCenter);
                            }
                        }
                        text = ":N.G."; text2 = ":N.G."; hantei = "○";////////////左1/4
                        kaberitsuhi = Math.Min((Lyl[e] / (Al[e] * beta[e])) / (Lyr[e] / (Ar[e] * beta[e])), (Lyr[e] / (Ar[e] * beta[e])) / (Lyl[e] / (Al[e] * beta[e])));
                        if (kaberitsuhi >= 0.5) { text2 = ":O.K."; }
                        values = new List<string>();
                        values.Add(""); values.Add("左1/4"); values.Add(Al[e].ToString("F6").Substring(0, Math.Max(4, Digit((int)Al[e]) + 2))); values.Add(beta[e].ToString("F6").Substring(0, 4)); values.Add((Al[e] * beta[e]).ToString("F6").Substring(0, Math.Max(4, Digit((int)(Al[e] * beta[e])) + 2)));
                        values.Add(Lyl[e].ToString("F6").Substring(0, Math.Max(4, Digit((int)Lyl[e]) + 2)));
                        if (Lyl[e] / (Al[e] * beta[e]) >= 1) { text = ":O.K."; }
                        values.Add((Lyl[e] / (Al[e] * beta[e])).ToString("F6").Substring(0, 4) + text);
                        values.Add(kaberitsuhi.ToString("F6").Substring(0, 4) + text2);
                        if (text == ":N.G." && text2 == ":N.G.") { hantei = "×"; }
                        values.Add(hantei);
                        for (int i = 0; i < values.Count; i++)
                        {
                            var j = (beta.Count - 1 - e) * 4 + 2;
                            if (i != 0 && i != 7 && i != 8)
                            {
                                gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy * (j + 2), offset_x + text_width * (i + 1), offset_y + pitchy * (j + 2));
                            }//横線
                            gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy * (j + 1), offset_x + text_width * i, offset_y + pitchy * (j + 2));//縦線
                            if (i == values.Count - 1)
                            {
                                gfx.DrawLine(pen, offset_x + text_width * (i + 1), offset_y + pitchy * (j + 1), offset_x + text_width * (i + 1), offset_y + pitchy * (j + 2));//縦線
                            }
                            if (i == 0)
                            {
                                gfx.DrawString(values[i], font, XBrushes.Black, new XRect(offset_x + text_width * i, offset_y + pitchy * (j + 2.5), text_width, offset_y + pitchy * (j + 2.5)), XStringFormats.TopCenter);
                            }
                            else if (i < 7)
                            {
                                gfx.DrawString(values[i], font, XBrushes.Black, new XRect(offset_x + text_width * i, offset_y + pitchy * (j + 1), text_width, offset_y + pitchy * (j + 1)), XStringFormats.TopCenter);
                            }
                            else
                            {
                                gfx.DrawString(values[i], font, XBrushes.Black, new XRect(offset_x + text_width * i, offset_y + pitchy * (j + 1.5), text_width, offset_y + pitchy * (j + 1.5)), XStringFormats.TopCenter);
                            }
                        }
                        text = ":N.G."; text2 = ":N.G."; hantei = "○";////////////右1/4
                        if (kaberitsuhi >= 0.5) { text2 = ":O.K."; }
                        values = new List<string>();
                        values.Add(""); values.Add("右1/4"); values.Add(Ar[e].ToString("F6").Substring(0, Math.Max(4, Digit((int)Ar[e]) + 2))); values.Add(beta[e].ToString("F6").Substring(0, 4)); values.Add((Ar[e] * beta[e]).ToString("F6").Substring(0, Math.Max(4, Digit((int)(Ar[e] * beta[e])) + 2)));
                        values.Add(Lyr[e].ToString("F6").Substring(0, Math.Max(4, Digit((int)Lyr[e]) + 2)));
                        if (Lyr[e] / (Ar[e] * beta[e]) >= 1) { text = ":O.K."; }
                        values.Add((Lyr[e] / (Ar[e] * beta[e])).ToString("F6").Substring(0, 4) + text);
                        values.Add(kaberitsuhi.ToString("F6").Substring(0, 4) + text2);
                        if (text == ":N.G." && text2 == ":N.G.") { hantei = "×"; }
                        values.Add(hantei);
                        for (int i = 0; i < values.Count; i++)
                        {
                            var j = (beta.Count - 1 - e) * 4 + 3;
                            gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy * (j + 2), offset_x + text_width * (i + 1), offset_y + pitchy * (j + 2));//横線
                            gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy * (j + 1), offset_x + text_width * i, offset_y + pitchy * (j + 2));//縦線
                            if (i == values.Count - 1)
                            {
                                gfx.DrawLine(pen, offset_x + text_width * (i + 1), offset_y + pitchy * (j + 1), offset_x + text_width * (i + 1), offset_y + pitchy * (j + 2));//縦線
                            }
                            if (i < 7)
                            {
                                gfx.DrawString(values[i], font, XBrushes.Black, new XRect(offset_x + text_width * i, offset_y + pitchy * (j + 1), text_width, offset_y + pitchy * (j + 1)), XStringFormats.TopCenter);
                            }
                        }

                    }
                    if (Ax.Count != 0 && Ay.Count != 0)
                    {

                        // 空白ページを作成。
                        page = document.AddPage(); gfx = XGraphics.FromPdfPage(page);
                        // 描画するためにXGraphicsオブジェクトを取得。
                        text_width = 75;
                        labels = new List<string> { "階", "受圧方向", "面積A[m2]", "壁率γ[m/m2]", "必要壁量Ln[m]", "壁量L[m]", "充足率" };
                        for (int i = 0; i < labels.Count; i++)//ラベル列**************************************************************************
                        {
                            gfx.DrawLine(pen, offset_x + text_width * i, offset_y, offset_x + text_width * (i + 1), offset_y);//横線
                            gfx.DrawLine(pen, offset_x + text_width * i, offset_y, offset_x + text_width * i, offset_y + pitchy);//縦線
                            gfx.DrawString(labels[i], font, XBrushes.Black, new XRect(offset_x + text_width * i, offset_y, text_width, offset_y), XStringFormats.TopCenter);
                            gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy, offset_x + text_width * (i + 1), offset_y + pitchy);//横線
                        }
                        gfx.DrawLine(pen, offset_x + text_width * labels.Count, offset_y, offset_x + text_width * labels.Count, offset_y + pitchy);//縦線
                        for (int e = beta.Count - 1; e > -1; e--)
                        {
                            var values = new List<string>();////////////////////X方向
                            values.Add(((int)(e + 1)).ToString() + "F"); values.Add("X方向"); values.Add(Ax[e].ToString("F6").Substring(0, Math.Max(4, Digit((int)Ax[e]) + 2))); values.Add(gamma.ToString("F6").Substring(0, 4)); values.Add((Ax[e] * gamma).ToString("F6").Substring(0, Math.Max(4, Digit((int)(Ax[e] * gamma)) + 2)));
                            var text = ":N.G.";
                            values.Add(Lx[e].ToString("F6").Substring(0, Math.Max(4, Digit((int)Lx[e]) + 2)));
                            if (Lx[e] / (Ax[e] * gamma) >= 1) { text = ":O.K."; }
                            values.Add((Lx[e] / (Ax[e] * gamma)).ToString("F6").Substring(0, 4) + text);
                            for (int i = 0; i < values.Count; i++)
                            {
                                var j = (beta.Count - 1 - e) * 2;
                                if (i != 0) { gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy * (j + 2), offset_x + text_width * (i + 1), offset_y + pitchy * (j + 2)); }//横線
                                gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy * (j + 1), offset_x + text_width * i, offset_y + pitchy * (j + 2));//縦線
                                if (i == values.Count - 1)
                                {
                                    gfx.DrawLine(pen, offset_x + text_width * (i + 1), offset_y + pitchy * (j + 1), offset_x + text_width * (i + 1), offset_y + pitchy * (j + 2));//縦線
                                }
                                if (i != 0)
                                {
                                    gfx.DrawString(values[i], font, XBrushes.Black, new XRect(offset_x + text_width * i, offset_y + pitchy * (j + 1), text_width, offset_y + pitchy * (j + 1)), XStringFormats.TopCenter);
                                }
                                else
                                {
                                    gfx.DrawString(values[i], font, XBrushes.Black, new XRect(offset_x + text_width * i, offset_y + pitchy * (j + 1.5), text_width, offset_y + pitchy * (j + 1.5)), XStringFormats.TopCenter);
                                }
                            }
                            values = new List<string>();////////////////////Y方向
                            values.Add(((int)(e + 1)).ToString() + "F"); values.Add("Y方向"); values.Add(Ay[e].ToString("F6").Substring(0, Math.Max(4, Digit((int)Ay[e]) + 2))); values.Add(gamma.ToString("F6").Substring(0, 4)); values.Add((Ay[e] * gamma).ToString("F6").Substring(0, Math.Max(4, Digit((int)(Ay[e] * gamma)) + 2)));
                            text = ":N.G.";
                            values.Add(Ly[e].ToString("F6").Substring(0, Math.Max(4, Digit((int)Ly[e]) + 2)));
                            if (Ly[e] / (Ay[e] * gamma) >= 1) { text = ":O.K."; }
                            values.Add((Ly[e] / (Ay[e] * gamma)).ToString("F6").Substring(0, 4) + text);
                            for (int i = 0; i < values.Count; i++)
                            {
                                var j = (beta.Count - 1 - e) * 2 + 1;
                                gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy * (j + 2), offset_x + text_width * (i + 1), offset_y + pitchy * (j + 2));//横線
                                gfx.DrawLine(pen, offset_x + text_width * i, offset_y + pitchy * (j + 1), offset_x + text_width * i, offset_y + pitchy * (j + 2));//縦線
                                if (i == values.Count - 1)
                                {
                                    gfx.DrawLine(pen, offset_x + text_width * (i + 1), offset_y + pitchy * (j + 1), offset_x + text_width * (i + 1), offset_y + pitchy * (j + 2));//縦線
                                }
                                if (i != 0)
                                {
                                    gfx.DrawString(values[i], font, XBrushes.Black, new XRect(offset_x + text_width * i, offset_y + pitchy * (j + 1), text_width, offset_y + pitchy * (j + 1)), XStringFormats.TopCenter);
                                }
                            }
                        }
                    }
                    var dir = Path.GetDirectoryName(RhinoDoc.ActiveDoc.Path);
                    // ドキュメントを保存。
                    var filename = dir + "/" + pdfname + ".pdf";
                    document.Save(filename);
                    // ビューアを起動。
                    Process.Start(new ProcessStartInfo(@filename) { UseShellExecute = true });
                }
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {get{ return OpenSeesUtility.Properties.Resources.kabecheck; }}
        public override Guid ComponentGuid
        {get { return new Guid("1bfc150c-947c-4411-8654-16e76006e6de"); }}
        ///ここからカスタム関数群********************************************************************************
        private readonly List<Color> _color = new List<Color>();
        private readonly List<Brep> _shells = new List<Brep>();
        private readonly List<Point3d> _points = new List<Point3d>();
        private readonly List<double> _betas = new List<double>();
        private readonly List<double> _As = new List<double>();
        private readonly List<double> _Als = new List<double>();
        private readonly List<double> _Ars = new List<double>();
        private readonly List<double> _Abs = new List<double>();
        private readonly List<double> _Ats = new List<double>();
        private readonly List<double> _Lxs = new List<double>();
        private readonly List<double> _Lys = new List<double>();
        private readonly List<double> _Lxts = new List<double>();
        private readonly List<double> _Lxbs = new List<double>();
        private readonly List<double> _Lyls = new List<double>();
        private readonly List<double> _Lyrs = new List<double>();
        protected override void BeforeSolveInstance()
        {
            _points.Clear();
            _shells.Clear();
            _color.Clear();
            _betas.Clear();
            _As.Clear();
            _Als.Clear();
            _Ars.Clear();
            _Abs.Clear();
            _Ats.Clear();
            _Lxs.Clear();
            _Lys.Clear();
            _Lxts.Clear();
            _Lxbs.Clear();
            _Lyls.Clear();
            _Lyrs.Clear();
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            args.Viewport.GetFrustumFarPlane(out Plane plane);
            RhinoViewport viewport = args.Viewport;
            ///シェル要素描画用関数*****************************************************************************
            for (int i = 0; i < _shells.Count; i++)
            {
                var material = new DisplayMaterial(_color[i]); material.Transparency = 0.5;
                args.Display.DrawBrepShaded(_shells[i], material);
            }
            ///*************************************************************************************************
            ///結果描画関数
            if (Value == 1)
            {
                for (int i = 0; i < _betas.Count; i++)
                {
                    double size = fontsize; Point3d point = _points[0]; plane.Origin = point;
                    viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                    var newline = ""; if (i == 1) { newline = "\n"; }
                    if (i == 2) { newline = "\n\n"; }
                    var judgex = ":O.K."; var judgey = ":O.K.";
                    if (_Lxs[i] / (_As[i] * _betas[i]) < 1) { judgex = ":N.G."; }
                    if (_Lys[i] / (_As[i] * _betas[i]) < 1) { judgey = ":N.G."; }
                    var text = newline + ((int)(_betas.Count - i)).ToString() + "F……" + "A=" + _As[i].ToString().Substring(0, Math.Min((_As[i].ToString()).Length, Digit((int)_As[i]))) + "[m2], "
                        + "β=" + _betas[i].ToString().Substring(0, Math.Min((_betas[i].ToString()).Length, 4)) + "[m/m2], "
                        + "Ln=" + (_As[i] * _betas[i]).ToString("F6").Substring(0, Math.Min(((_As[i] * _betas[i]).ToString("F6")).Length, 5)) + "[m], "
                        + "Lx=" + _Lxs[i].ToString("F6").Substring(0, Math.Min((_Lxs[i].ToString("F6")).Length, 5)) + "[m], "
                        + "充足率Lx/Ln=" + (_Lxs[i] / (_As[i] * _betas[i])).ToString("F6").Substring(0, Math.Min(((_Lxs[i] / (_As[i] * _betas[i])).ToString("F6")).Length, 4)) + judgex + ", "
                        + "Ly=" + _Lys[i].ToString("F6").Substring(0, Math.Min((_Lys[i].ToString("F6")).Length, 5)) + "[m], "
                        + "充足率Ly/Ln=" + (_Lys[i] / (_As[i] * _betas[i])).ToString("F6").Substring(0, Math.Min(((_Lys[i] / (_As[i] * _betas[i])).ToString("F6")).Length, 4)) + judgey;
                    args.Display.Draw3dText(text, Color.Black, plane, size, "", false, false, TextHorizontalAlignment.Left, TextVerticalAlignment.Bottom);
                }
            }
            /*
            double size = fontsize; Point3d point = _points[0]; plane.Origin = point;
            viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
            if (Value == 1)
            {
                for (int i = 0; i < _betas.Count; i++)
                {
                    var newline = ""; if (i == 1) { newline = "\n"; }
                    if (i == 2) { newline = "\n\n"; }
                    var judgex = ":O.K."; var judgey = ":O.K.";
                    if (_Lxs[i] / (_As[i] * _betas[i]) < 1) { judgex = ":N.G."; }
                    if (_Lys[i] / (_As[i] * _betas[i]) < 1) { judgey = ":N.G."; }
                    var text = newline + ((int)(_betas.Count - i)).ToString() + "F……" + "A=" + _As[i].ToString().Substring(0, Math.Min((_As[i].ToString()).Length,Digit((int)_As[i]))) + "[m2], "
                        + "β=" + _betas[i].ToString().Substring(0, Math.Min((_betas[i].ToString()).Length,4)) + "[m/m2], "
                        + "Ln=" + (_As[i] * _betas[i]).ToString("F6").Substring(0, Math.Min(((_As[i] * _betas[i]).ToString("F6")).Length,5)) + "[m], "
                        + "Lx=" + _Lxs[i].ToString("F6").Substring(0, Math.Min((_Lxs[i].ToString("F6")).Length,5)) + "[m], "
                        + "充足率Lx/Ln=" + (_Lxs[i] / (_As[i] * _betas[i])).ToString("F6").Substring(0, Math.Min(((_Lxs[i] / (_As[i] * _betas[i])).ToString("F6")).Length,4)) + judgex + ", "
                        + "Ly=" + _Lys[i].ToString("F6").Substring(0, Math.Min((_Lys[i].ToString("F6")).Length,5)) + "[m], "
                        + "充足率Ly/Ln=" + (_Lys[i] / (_As[i] * _betas[i])).ToString("F6").Substring(0, Math.Min(((_Lys[i] / (_As[i] * _betas[i])).ToString("F6")).Length,4)) + judgey;
                    args.Display.Draw3dText(text, Color.Black, plane, size, "", false, false, TextHorizontalAlignment.Left, TextVerticalAlignment.Bottom);
                }
            }*/
        }
        ///ここまでカスタム関数群********************************************************************************
        ///ここからGUIの作成*****************************************************************************************
        public class CustomGUI : GH_ComponentAttributes
        {
            public CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle title_rec;
            private Rectangle radio_rec; private Rectangle radio_rec23; private Rectangle radio_rec4; private Rectangle radio_rec5;
            private Rectangle radio_rec_11; private Rectangle text_rec_11; private Rectangle radio_rec_12; private Rectangle text_rec_12; private Rectangle radio_rec_13; private Rectangle text_rec_13;
            private Rectangle radio_rec_21; private Rectangle text_rec_21; private Rectangle radio_rec_22; private Rectangle text_rec_22;
            private Rectangle radio_rec_31; private Rectangle text_rec_31; private Rectangle radio_rec_32; private Rectangle text_rec_32;
            private Rectangle radio_rec_41; private Rectangle text_rec_41;
            private Rectangle radio_rec_51; private Rectangle text_rec_51;

            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int height = 110; int subwidth = 44; int radi1 = 7; int radi2 = 5; int radi3 = 3;
                int pitchx = 6; int textheight = 20;
                global_rec.Height += height;
                title_rec = global_rec;
                title_rec.Y = title_rec.Bottom - height;
                title_rec.Height = 22;

                radio_rec = title_rec;
                radio_rec.Y += title_rec.Height;
                radio_rec23 = radio_rec;
                radio_rec4 = radio_rec;
                radio_rec5 = radio_rec;

                radio_rec_11 = radio_rec;
                radio_rec_11.X += radi2 - 1; radio_rec_11.Y = title_rec.Bottom + radi2;
                radio_rec_11.Height = radi1; radio_rec_11.Width = radi1;

                text_rec_11 = radio_rec_11;
                text_rec_11.X += pitchx; text_rec_11.Y -= radi2;
                text_rec_11.Height = textheight; text_rec_11.Width = subwidth;

                radio_rec_12 = text_rec_11;
                radio_rec_12.X += text_rec_11.Width - radi2; radio_rec_12.Y = radio_rec_11.Y;
                radio_rec_12.Height = radi1; radio_rec_12.Width = radi1;

                text_rec_12 = radio_rec_12;
                text_rec_12.X += pitchx; text_rec_12.Y -= radi2;
                text_rec_12.Height = textheight; text_rec_12.Width = subwidth;

                radio_rec_13 = text_rec_12;
                radio_rec_13.X += text_rec_12.Width - radi2; radio_rec_13.Y = radio_rec_12.Y;
                radio_rec_13.Height = radi1; radio_rec_13.Width = radi1;

                text_rec_13 = radio_rec_13;
                text_rec_13.X += pitchx; text_rec_13.Y -= radi2;
                text_rec_13.Height = textheight; text_rec_13.Width = subwidth;

                radio_rec_21 = radio_rec_11;
                radio_rec_21.Y += text_rec_11.Height - radi3;
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

                radio_rec_31 = radio_rec_21;
                radio_rec_31.Y += text_rec_21.Height - radi1;
                radio_rec_31.Height = radi1; radio_rec_21.Width = radi1;

                text_rec_31 = radio_rec_31;
                text_rec_31.X += pitchx; text_rec_31.Y -= radi2;
                text_rec_31.Height = textheight; text_rec_31.Width = subwidth;

                radio_rec_32 = text_rec_31;
                radio_rec_32.X += text_rec_31.Width - radi2; radio_rec_32.Y = radio_rec_31.Y;
                radio_rec_32.Height = radi1; radio_rec_32.Width = radi1;

                text_rec_32 = radio_rec_32;
                text_rec_32.X += pitchx; text_rec_32.Y -= radi2;
                text_rec_32.Height = textheight; text_rec_32.Width = subwidth;

                radio_rec_41 = radio_rec_31;
                radio_rec_41.Y += text_rec_31.Height - radi3;
                radio_rec_41.Height = radi1; radio_rec_31.Width = radi1;

                text_rec_41 = radio_rec_41;
                text_rec_41.X += pitchx; text_rec_41.Y -= radi2;
                text_rec_41.Height = textheight; text_rec_41.Width = subwidth * 3;

                radio_rec_51 = radio_rec_41;
                radio_rec_51.Y += textheight;
                radio_rec_51.Height = radi1; radio_rec_41.Width = radi1;

                text_rec_51 = radio_rec_51;
                text_rec_51.X += pitchx; text_rec_51.Y -= radi2;
                text_rec_51.Height = textheight; text_rec_51.Width = subwidth * 3;

                radio_rec.Height = text_rec_11.Y + textheight - radio_rec.Y - radi3;
                radio_rec23.Y += radio_rec.Height;
                radio_rec23.Height = text_rec_31.Y + textheight - radio_rec23.Y - radi3;
                radio_rec4 = radio_rec23;
                radio_rec4.Y += radio_rec23.Height; radio_rec4.Height = textheight;
                radio_rec5 = radio_rec4;
                radio_rec5.Y += radio_rec4.Height; radio_rec5.Height = textheight;
                global_rec.Height = radio_rec5.Bottom - global_rec.Y;
                ///******************************************************************************************

                Bounds = global_rec;
            }
            Brush c11 = Brushes.White; Brush c12 = Brushes.White; Brush c13 = Brushes.White; Brush c21 = Brushes.White; Brush c22 = Brushes.White; Brush c23 = Brushes.White; Brush c31 = Brushes.White; Brush c32 = Brushes.White; Brush c41 = Brushes.White; Brush c51 = Brushes.White;
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
                    graphics.DrawString("1F", GH_FontServer.Standard, Brushes.Black, text_rec_11);

                    GH_Capsule radio_12 = GH_Capsule.CreateCapsule(radio_rec_12, GH_Palette.Black, 5, 5);
                    radio_12.Render(graphics, Selected, Owner.Locked, false); radio_12.Dispose();
                    graphics.FillEllipse(c12, radio_rec_12);
                    graphics.DrawString("2F", GH_FontServer.Standard, Brushes.Black, text_rec_12);

                    GH_Capsule radio_13 = GH_Capsule.CreateCapsule(radio_rec_13, GH_Palette.Black, 5, 5);
                    radio_13.Render(graphics, Selected, Owner.Locked, false); radio_13.Dispose();
                    graphics.FillEllipse(c13, radio_rec_13);
                    graphics.DrawString("3F", GH_FontServer.Standard, Brushes.Black, text_rec_13);

                    GH_Capsule radio23 = GH_Capsule.CreateCapsule(radio_rec23, GH_Palette.White, 2, 0);
                    radio23.Render(graphics, Selected, Owner.Locked, false); radio23.Dispose();

                    GH_Capsule radio_21 = GH_Capsule.CreateCapsule(radio_rec_21, GH_Palette.Black, 5, 5);
                    radio_21.Render(graphics, Selected, Owner.Locked, false); radio_21.Dispose();
                    graphics.FillEllipse(c21, radio_rec_21);
                    graphics.DrawString("←", GH_FontServer.Standard, Brushes.Black, text_rec_21);

                    GH_Capsule radio_22 = GH_Capsule.CreateCapsule(radio_rec_22, GH_Palette.Black, 5, 5);
                    radio_22.Render(graphics, Selected, Owner.Locked, false); radio_22.Dispose();
                    graphics.FillEllipse(c22, radio_rec_22);
                    graphics.DrawString("→", GH_FontServer.Standard, Brushes.Black, text_rec_22);

                    GH_Capsule radio_31 = GH_Capsule.CreateCapsule(radio_rec_31, GH_Palette.Black, 5, 5);
                    radio_31.Render(graphics, Selected, Owner.Locked, false); radio_31.Dispose();
                    graphics.FillEllipse(c31, radio_rec_31);
                    graphics.DrawString("↑", GH_FontServer.Standard, Brushes.Black, text_rec_31);

                    GH_Capsule radio_32 = GH_Capsule.CreateCapsule(radio_rec_32, GH_Palette.Black, 5, 5);
                    radio_32.Render(graphics, Selected, Owner.Locked, false); radio_32.Dispose();
                    graphics.FillEllipse(c32, radio_rec_32);
                    graphics.DrawString("↓", GH_FontServer.Standard, Brushes.Black, text_rec_32);

                    GH_Capsule radio4 = GH_Capsule.CreateCapsule(radio_rec4, GH_Palette.White, 2, 0);
                    radio4.Render(graphics, Selected, Owner.Locked, false); radio4.Dispose();

                    GH_Capsule radio_41 = GH_Capsule.CreateCapsule(radio_rec_41, GH_Palette.Black, 5, 5);
                    radio_41.Render(graphics, Selected, Owner.Locked, false); radio_41.Dispose();
                    graphics.FillEllipse(c41, radio_rec_41);
                    graphics.DrawString("Result", GH_FontServer.Standard, Brushes.Black, text_rec_41);

                    GH_Capsule radio5 = GH_Capsule.CreateCapsule(radio_rec5, GH_Palette.White, 2, 0);
                    radio5.Render(graphics, Selected, Owner.Locked, false); radio5.Dispose();

                    GH_Capsule radio_51 = GH_Capsule.CreateCapsule(radio_rec_51, GH_Palette.Black, 5, 5);
                    radio_51.Render(graphics, Selected, Owner.Locked, false); radio_51.Dispose();
                    graphics.FillEllipse(c51, radio_rec_51);
                    graphics.DrawString("PDF OUTPUT", GH_FontServer.Standard, Brushes.Black, text_rec_51);
                    ///******************************************************************************************
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec11 = radio_rec_11; RectangleF rec12 = radio_rec_12; RectangleF rec13 = radio_rec_13;
                    RectangleF rec21 = radio_rec_21; RectangleF rec22 = radio_rec_22;
                    RectangleF rec31 = radio_rec_31; RectangleF rec32 = radio_rec_32;
                    RectangleF rec41 = radio_rec_41;
                    RectangleF rec51 = radio_rec_51;
                    if (rec11.Contains(e.CanvasLocation))
                    {
                        if (c11 == Brushes.Black) { c11 = Brushes.White; SetButton("c11", 0); }
                        else
                        { c11 = Brushes.Black; SetButton("c11", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec12.Contains(e.CanvasLocation))
                    {
                        if (c12 == Brushes.Black) { c12 = Brushes.White; SetButton("c12", 0); }
                        else
                        { c12 = Brushes.Black; SetButton("c12", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec13.Contains(e.CanvasLocation))
                    {
                        if (c13 == Brushes.Black) { c13 = Brushes.White; SetButton("c13", 0); }
                        else
                        { c13 = Brushes.Black; SetButton("c13", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec21.Contains(e.CanvasLocation))
                    {
                        if (c21 == Brushes.Black) { c21 = Brushes.White; SetButton("c21", 0); }
                        else
                        { c21 = Brushes.Black; SetButton("c21", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec22.Contains(e.CanvasLocation))
                    {
                        if (c22 == Brushes.Black) { c22 = Brushes.White; SetButton("c22", 0); }
                        else
                        { c22 = Brushes.Black; SetButton("c22", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec31.Contains(e.CanvasLocation))
                    {
                        if (c31 == Brushes.Black) { c31 = Brushes.White; SetButton("c31", 0); }
                        else
                        { c31 = Brushes.Black; SetButton("c31", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec32.Contains(e.CanvasLocation))
                    {
                        if (c32 == Brushes.Black) { c32 = Brushes.White; SetButton("c32", 0); }
                        else
                        { c32 = Brushes.Black; SetButton("c32", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec41.Contains(e.CanvasLocation))
                    {
                        if (c41 == Brushes.Black) { c41 = Brushes.White; SetButton("c41", 0); }
                        else
                        { c41 = Brushes.Black; SetButton("c41", 1); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    if (rec51.Contains(e.CanvasLocation))
                    {
                        if (c51 == Brushes.Black) { c51 = Brushes.White; SetButton("c51", 0); }
                        else
                        { c51 = Brushes.Black; SetButton("c51", 1); }
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