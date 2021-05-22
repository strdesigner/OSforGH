using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Display;

using System.Drawing;
using System.Windows.Forms;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using System.Windows.Forms.VisualStyles;
///****************************************

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace VisualizeModel
{
    public class VisualizeModel : GH_Component
    {
        static int Node = 0; static int Node_No = 0; static int Sec_No = 0; static int Mat_No = 0; static int Angle = 0; static int Beam = 0; static int Beam_No = 0;
        static int Joints = 0; static int PxPyPz = 0; static int MxMyMz = 0; static int Beam_load = 0; static int Surf_Flr_load = 0; static int KabeYane = 0;
        static int Boundary = 0; static int Shell = 0; static int Shell_No = 0; static int Mat_No_Shell = 0; static int Thick = 0; public static int Value = 0; static int Spring = 0;
        public static double fontsize; public static double arrowsize; static double arcsize; static string unit_of_length = "m"; static string unit_of_force = "kN";
        public static void SetButton(string s, int i)
        {
            if (s == "Nodes")
            {
                Node = i;
            }
            else if (s == "Node No.")
            {
                Node_No = i;
            }
            else if (s == "Boundary")
            {
                Boundary = i;
            }
            else if (s == "PxPyPz")
            {
                PxPyPz = i;
            }
            else if (s == "MxMyMz")
            {
                MxMyMz = i;
            }
            else if (s == "Joints")
            {
                Joints = i;
            }
            else if (s == "Beam")
            {
                Beam = i;
            }
            else if (s == "Spring")
            {
                Spring = i;
            }
            else if (s == "Beam_No")
            {
                Beam_No = i;
            }
            else if (s == "Angle")
            {
                Angle = i;
            }
            else if (s == "Sec_No")
            {
                Sec_No = i;
            }
            else if (s == "Mat_No")
            {
                Mat_No = i;
            }
            else if (s == "Beam_load")
            {
                Beam_load = i;
            }
            else if (s == "Shell")
            {
                Shell = i;
            }
            else if (s == "Shell_No")
            {
                Shell_No = i;
            }
            else if (s == "sf_load")
            {
                Surf_Flr_load = i;
            }
            else if (s == "Thick")
            {
                Thick = i;
            }
            else if (s == "Mat_No(shell)")
            {
                Mat_No_Shell = i;
            }
            else if (s == "KabeYane")
            {
                KabeYane = i;
            }
            else if (s == "Value")
            {
                Value = i;
            }
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        public VisualizeModel()
          : base("VisualizeStructuralModel", "VisualizeModel",
              "Display structural model for OpenSees",
              "OpenSees", "Visualization")
        {
        }
        public override bool IsPreviewCapable { get { return true; } }
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("element_node_relationship(shell)", "IJKL", "[[No.i,No.j,No.k,No.l(if triangle:-1),material No.,thickness],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("KABE_W", "KABE_W", "[[No.i,No.j,No.k,No.l,kabebairitsu],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("nodal_force", "p_load", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("line_force", "e_load", "[[element No.,Wx,Wy,Wz],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("surface_force", "sf_load", "[[No.i,No.j,No.k,No.l,Surf_Flr_load],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("boundary_condition", "Boundary", "[[node No.,X,Y,Z,MX,MY,MZ],...](DataTree) 0-1 variable", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("joint condition", "joint", "[[Ele. No., 0 or 1(means i or j), 0 or 1, 0 or 1, 0 or 1, 0 or 1, 0 or 1, 0 or 1(free or fix)],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("spring element", "spring", "[[No.i, No.j, kxt, ktc, kyt, kyc, kzt, kzc, rx, ry, rz(spring value)],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("index", "index", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("index(shell)", "index(shell)", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("index(kabe)", "index(kabe)", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("index(spring)", "index(spring)", "[...](element No. List to show)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("fontsize", "FS", "font size for display texts", GH_ParamAccess.item, 20.0);///
            pManager.AddNumberParameter("arrowsize", "AS", "length parameter for force vector", GH_ParamAccess.item, 0.15);///
            pManager.AddNumberParameter("arcsize", "CS", "radius parameter for moment arc with arrow", GH_ParamAccess.item, 0.25);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///0
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree);///1
            pManager.AddNumberParameter("element_node_relationship(shell)", "IJKL", "[[No.i,No.j,No.k,No.l(if triangle:-1),material No.,thickness],...](DataTree)", GH_ParamAccess.tree);///2
            pManager.AddNumberParameter("KABE_W", "KABE_W", "[[No.i,No.j,No.k,No.l,kabebairitsu],...](DataTree)", GH_ParamAccess.tree);///3
            pManager.AddNumberParameter("nodal_force", "p_load", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);///4
            pManager.AddNumberParameter("line_force", "e_load", "[[element No.,Wx,Wy,Wz],...](DataTree)", GH_ParamAccess.tree);///5
            pManager.AddNumberParameter("surface_force", "sf_load", "[[No.i,No.j,No.k,No.l,Surf_Flr_load],...](DataTree)", GH_ParamAccess.tree);///6
            pManager.AddNumberParameter("boundary_condition", "Boundary", "[[node No.,X,Y,Z,MX,MY,MZ],...](DataTree) 0-1 variable", GH_ParamAccess.tree);///7
            pManager.AddNumberParameter("joint condition", "joint", "[[Ele. No., 0 or 1(means i or j), 0 or 1, 0 or 1, 0 or 1, 0 or 1, 0 or 1, 0 or 1(free or fix)],...](DataTree)", GH_ParamAccess.tree);///8
            pManager.AddNumberParameter("spring element", "spring", "[[No.i, No.j, kxt, ktc, kyt, kyc, kzt, kzc, rx, ry, rz(spring value)],...](DataTree)", GH_ParamAccess.tree);///9
            pManager.AddNumberParameter("index", "index", "[...](element No. List to show)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("index(shell)", "index(shell)", "[...](element No. List to show)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("index(kabe)", "index(kabe)", "[...](element No. List to show)", GH_ParamAccess.list);///
            pManager.AddNumberParameter("index(spring)", "index(spring)", "[...](element No. List to show)", GH_ParamAccess.list);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            ///変数の型宣言**************************************************************************************
            IList<List<GH_Number>> r; IList<List<GH_Number>> ij; IList<List<GH_Number>> ijkl; IList<List<GH_Number>> f_v; IList<List<GH_Number>> s_load; IList<List<GH_Number>> fix;
            List<double> index = new List<double>(); List<double> index2 = new List<double>(); List<double> index3 = new List<double>(); List<double> index4 = new List<double>();
            fontsize = double.NaN; if (!DA.GetData("fontsize", ref fontsize)) return; arrowsize = double.NaN; if (!DA.GetData("arrowsize", ref arrowsize)) return; arcsize = double.NaN; if (!DA.GetData("arcsize", ref arcsize)) return; DA.GetDataList("index", index); DA.GetDataList("index(shell)", index2);DA.GetDataList("index(kabe)", index3); DA.GetDataList("index(spring)", index4);
            Point3d r1; Point3d r2; Point3d r3;
            ///*************************************************************************************************
            if (!DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r)) { }
            else
            {
                r = _r.Branches; int n = r.Count;DA.SetDataTree(0, _r);
                ///節点の描画****************************************************************************************
                for (int i = 0; i < n; i++)
                {
                    r1 = new Point3d(r[i][0].Value, r[i][1].Value, r[i][2].Value);
                    _point.Add(r1);
                    _node_No.Add(i.ToString());
                }
                ///*************************************************************************************************
                ///要素の描画****************************************************************************************
                if (!DA.GetDataTree("element_node_relationship", out GH_Structure<GH_Number> _ij)) { }
                else if (_ij.Branches[0][0].Value != -9999)
                {
                    ij = _ij.Branches; int m = ij.Count; DA.SetDataTree(1, _ij);
                    if (index[0] != 9999)
                    {
                        if (index[0] == -9999)
                        {
                            index = new List<double>();
                            for (int e = 0; e < m; e++) { index.Add(e); }
                        }
                        DA.SetDataList("index", index);
                        for (int ind = 0; ind < index.Count; ind++)
                        {
                            int e = (int)index[ind];
                            if (Beam == 1 || Beam_No == 1 || Beam_load == 1 || Angle == 1 || Sec_No == 1 || Mat_No == 1)
                            {
                                int i = (int)ij[e][0].Value; int j = (int)ij[e][1].Value; int mat = (int)ij[e][2].Value; int sec = (int)ij[e][3].Value;
                                r1 = new Point3d(r[i][0].Value, r[i][1].Value, r[i][2].Value); r2 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value);
                                _p1.Add(r1); _p2.Add(r2);
                                _beam_No.Add(e.ToString());
                                _angle.Add(ij[e][4].Value.ToString());
                                _sec_No.Add(sec.ToString());
                                _mat_No.Add(mat.ToString());
                            }
                        }
                    }
                    if (!DA.GetDataTree("joint condition", out GH_Structure<GH_Number> _joint)) { }
                    else if (_joint.Branches[0][0].Value != -9999)
                    {
                        if (Joints == 1)
                        {
                            var joint = _joint.Branches;
                            for (int i = 0; i < joint.Count; i++)
                            {
                                int e = (int)joint[i][0].Value;
                                if (index.Contains(e) == true)
                                {
                                    var n1 = (int)ij[e][0].Value; var n2 = (int)ij[e][1].Value;
                                    r1 = new Point3d(r[n1][0].Value, r[n1][1].Value, r[n1][2].Value); r2 = new Point3d(r[n2][0].Value, r[n2][1].Value, r[n2][2].Value);
                                    if ((int)joint[i][1].Value == 0 || (int)joint[i][1].Value == 2) { _pin.Add(r1); _v.Add(r2 - r1); }
                                    if ((int)joint[i][1].Value == 1 || (int)joint[i][1].Value == 2) { _pin.Add(r2); _v.Add(r1 - r2); }
                                }
                            }
                        }
                    }
                    DA.SetDataTree(8, _joint);
                    if (!DA.GetDataTree("spring element", out GH_Structure<GH_Number> _spring)) { }
                    else if (_spring.Branches[0][0].Value != -9999)
                    {
                        Vector3d rotation(Vector3d a, Vector3d b, double theta)
                        {
                            double rad = theta * Math.PI / 180;
                            double s = Math.Sin(rad); double c = Math.Cos(rad);
                            b /= Math.Sqrt(Vector3d.Multiply(b, b));
                            double b1 = b[0]; double b2 = b[1]; double b3 = b[2];
                            Vector3d v1 = new Vector3d(c + Math.Pow(b1, 2) * (1 - c), b1 * b2 * (1 - c) - b3 * s, b1 * b3 * (1 - c) + b2 * s);
                            Vector3d v2 = new Vector3d(b2 * b1 * (1 - c) + b3 * s, c + Math.Pow(b2, 2) * (1 - c), b2 * b3 * (1 - c) - b1 * s);
                            Vector3d v3 = new Vector3d(b3 * b1 * (1 - c) - b2 * s, b3 * b2 * (1 - c) + b1 * s, c + Math.Pow(b3, 2) * (1 - c));
                            return new Vector3d(Vector3d.Multiply(v1, a), Vector3d.Multiply(v2, a), Vector3d.Multiply(v3, a));
                        }
                        if (index4[0] != 9999)
                        {
                            var spring = _spring.Branches; var m4 = spring.Count;
                            if (index4[0] == -9999)
                            {
                                index4 = new List<double>();
                                for (int e = 0; e < m4; e++) { index4.Add(e); }
                            }
                            DA.SetDataList("index(spring)", index4);
                            if (Spring == 1)//ばねの描画
                            {
                                var a = new List<double>();
                                for (int e = 0; e < m4; e++) { a.Add(0.0); }
                                if (spring[0].Count == 12)
                                {
                                    for (int e = 0; e < m4; e++) { a[e] = spring[e][11].Value; }
                                }
                                for (int ind = 0; ind < index4.Count; ind++)
                                {
                                    int e = (int)index4[ind];
                                    var n1 = (int)spring[e][0].Value; var n2 = (int)spring[e][1].Value;
                                    r1 = new Point3d(r[n1][0].Value, r[n1][1].Value, r[n1][2].Value); r2 = new Point3d(r[n2][0].Value, r[n2][1].Value, r[n2][2].Value); var rc = (r1 + r2) / 2.0;
                                    Vector3d x = r2 - r1; Vector3d y = new Vector3d(0, 1, 0); Vector3d z = new Vector3d(0, 0, 1); var divx = x / 12.0;
                                    if (Math.Abs(x[0]) <= 5e-3 && Math.Abs(x[1]) <= 5e-3)
                                    {
                                        y = rotation(x, new Vector3d(0, 1, 0), 90);
                                        z = rotation(y, x, 90 + a[e]);
                                        y = rotation(z, x, -90);
                                    }
                                    else
                                    {
                                        y = rotation(x, new Vector3d(0, 0, 1), 90);
                                        y[2] = 0.0;
                                        z = rotation(y, x, 90 + a[e]);
                                        y = rotation(z, x, -90);
                                    }
                                    var divy = y / 4.0;
                                    var pts = new List<Point3d>(); pts.Add(r1);
                                    pts.Add(r1 + divx * 2); pts.Add(r1 + divx * 3 + divy); pts.Add(r1 + divx * 5 - divy); pts.Add(r1 + divx * 7 + divy); pts.Add(r1 + divx * 9 - divy); pts.Add(r1 + divx * 10); pts.Add(r2);
                                    for (int i = 0; i < pts.Count - 1; i++)
                                    {
                                        _springline.Add(new Line(pts[i], pts[i + 1]));
                                    }
                                    _springv.Add(new Line(rc, rc + y / 3.0)); _springv.Add(new Line(rc, rc + z / 3.0));
                                    if (Beam_No == 1)
                                    {
                                        _spring_pc.Add((r1 + r2) / 2.0);
                                        _spring_No.Add(e.ToString());
                                    }
                                }
                            }
                        }
                    }
                    DA.SetDataTree(9, _spring);
                    ///要素外力の描画************************************************************************************
                    DA.GetDataTree("line_force", out GH_Structure<GH_Number> _l_v);
                    if (_l_v.Branches[0][0].Value != -9999)
                    {
                        var l_v = _l_v.Branches;
                        for (int i = 0; i < l_v.Count; i++)
                        {
                            int e = (int)l_v[i][0].Value;
                            var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                            var ri = new Point3d(r[ni][0].Value, r[ni][1].Value, r[ni][2].Value); var rj = new Point3d(r[nj][0].Value, r[nj][1].Value, r[nj][2].Value);
                            int div = 10; var v = (rj - ri)/div;
                            for (int k = 0; k < div+1; k++)
                            {
                                if (l_v[i][1].Value != 0 && Beam_load == 1)
                                {
                                    r1 = ri + v * k;
                                    r2 = new Point3d(r1[0] - l_v[i][1].Value * arrowsize, r1[1], r1[2]);
                                    _arrows.Add(new Line(r2, r1));
                                }
                                if (l_v[i][1].Value != 0 && Value == 1)
                                {
                                    _epts.Add(new Point3d((ri[0] + rj[0]) / 2.0 - l_v[i][1].Value * arrowsize, (ri[1] + rj[1]) / 2.0, (ri[2] + rj[2]) / 2.0));
                                    _e_load.Add(Math.Abs(l_v[i][1].Value).ToString());
                                }
                                if (l_v[i][2].Value != 0 && Beam_load == 1)
                                {
                                    r1 = ri + v * k;
                                    r2 = new Point3d(r1[0], r1[1] - l_v[i][2].Value * arrowsize, r1[2]);
                                    _value.Add(Math.Abs(l_v[i][2].Value));
                                    _arrows.Add(new Line(r2, r1));
                                }
                                if (l_v[i][2].Value != 0 && Value == 1)
                                {
                                    _epts.Add(new Point3d((ri[0] + rj[0]) / 2.0, (ri[1] + rj[1]) / 2.0 - l_v[i][2].Value * arrowsize, (ri[2] + rj[2]) / 2.0));
                                    _e_load.Add(Math.Abs(l_v[i][2].Value).ToString());
                                }
                                if (l_v[i][3].Value != 0 && Beam_load == 1)
                                {
                                    r1 = ri + v * k;
                                    r2 = new Point3d(r1[0], r1[1], r1[2] - l_v[i][3].Value * arrowsize);
                                    _value.Add(Math.Abs(l_v[i][3].Value));
                                    _arrows.Add(new Line(r2, r1));
                                }
                                if (l_v[i][3].Value != 0 && Value == 1)
                                {
                                    _epts.Add(new Point3d((ri[0] + rj[0]) / 2.0, (ri[1] + rj[1]) / 2.0, (ri[2] + rj[2]) / 2.0 - l_v[i][3].Value * arrowsize));
                                    _e_load.Add(Math.Round(Math.Abs(l_v[i][3].Value),2).ToString());
                                }
                            }
                        }
                    }
                    DA.SetDataTree(5, _l_v);
                    ///*************************************************************************************************
                }
                if (!DA.GetDataTree("element_node_relationship(shell)", out GH_Structure<GH_Number> _ijkl)) { }
                else if (_ijkl.Branches[0][0].Value != -9999)
                {
                    DA.SetDataTree(2, _ijkl);
                    if (Shell == 1)
                    {
                        if (index2[0] != 9999)
                        {
                            ijkl = _ijkl.Branches; int m2 = ijkl.Count; DA.SetDataTree(2, _ijkl);
                            if (index2[0] == -9999)
                            {
                                index2 = new List<double>();
                                for (int e = 0; e < m2; e++) { index2.Add(e); }
                            }
                            DA.SetDataList("index(shell)", index2);
                            for (int ind = 0; ind < index2.Count; ind++)
                            {
                                int e = (int)index2[ind];
                                int i = (int)ijkl[e][0].Value; int j = (int)ijkl[e][1].Value; int k = (int)ijkl[e][2].Value; int l = (int)ijkl[e][3].Value; int mat = (int)ijkl[e][4].Value; double t = ijkl[e][5].Value;
                                r1 = new Point3d(r[i][0].Value, r[i][1].Value, r[i][2].Value); r2 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value); r3 = new Point3d(r[k][0].Value, r[k][1].Value, r[k][2].Value);
                                _sline.Add(new Line(r1, r2)); _sline.Add(new Line(r2, r3));
                                if (l >= 0)
                                {
                                    Point3d r4 = new Point3d(r[l][0].Value, r[l][1].Value, r[l][2].Value);
                                    Brep brep = Brep.CreatePlanarBreps(new Polyline(new List<Point3d> { r1, r2, r3, r4, r1 }).ToNurbsCurve(), 9999)[0];
                                    _shells.Add(brep);
                                    _sline.Add(new Line(r3, r4)); _sline.Add(new Line(r4, r1));
                                    _points.Add((r1 + r2 + r3 + r4) / 4.0);
                                    _shell_No.Add(e.ToString());
                                    _shell_mat_No.Add(mat.ToString());
                                    _valuet.Add(t);
                                }
                                else
                                {
                                    Brep brep = Brep.CreatePlanarBreps(new Polyline(new List<Point3d> { r1, r2, r3, r1 }).ToNurbsCurve(), 0.001)[0];
                                    _shells.Add(brep);
                                    _sline.Add(new Line(r3, r1));
                                    _points.Add((r1 + r2 + r3) / 3.0);
                                    _shell_No.Add(e.ToString());
                                    _shell_mat_No.Add(mat.ToString());
                                    _valuet.Add(t);
                                }
                            }
                        }
                    }
                }
                ///線材置換壁の描画************************************************************************************
                DA.GetDataTree("KABE_W", out GH_Structure<GH_Number> _kabe); DA.SetDataTree(3, _kabe);
                if (_kabe.Branches[0][0].Value != -9999)
                {
                    var kabe_w = _kabe.Branches;
                    if (KabeYane == 1)
                    {
                        if (index3[0] != 9999)
                        {
                            if (index3[0] == -9999)
                            {
                                index3 = new List<double>();
                                for (int e = 0; e < kabe_w.Count; e++) { index3.Add(e); }
                            }
                            DA.SetDataList("index(kabe)", index3);
                            for (int ind = 0; ind < index3.Count; ind++)
                            {
                                int e = (int)index3[ind];
                                int i = (int)kabe_w[e][0].Value; int j = (int)kabe_w[e][1].Value; int k = (int)kabe_w[e][2].Value; int l = (int)kabe_w[e][3].Value; double alpha = Math.Round(kabe_w[e][4].Value,3);
                                if (alpha != 0.0)
                                {
                                    _kabew.Add(new Line(r[i][0].Value, r[i][1].Value, r[i][2].Value, r[k][0].Value, r[k][1].Value, r[k][2].Value));
                                    _kabew.Add(new Line(r[j][0].Value, r[j][1].Value, r[j][2].Value, r[l][0].Value, r[l][1].Value, r[l][2].Value));
                                    if (Value == 1)
                                    {
                                        _kabew_p.Add(new Point3d((r[i][0].Value + r[j][0].Value + r[k][0].Value + r[l][0].Value) / 4.0, (r[i][1].Value + r[j][1].Value + r[k][1].Value + r[l][1].Value) / 4.0, (r[i][2].Value + r[j][2].Value + r[k][2].Value + r[l][2].Value) / 4.0));
                                        _kabebairitsu.Add(alpha);
                                    }
                                }
                            }
                        }
                    }
                }
                ///*************************************************************************************************
                ///節点外力の描画************************************************************************************
                if (!DA.GetDataTree("nodal_force", out GH_Structure<GH_Number> _f_v)) { }
                else if (_f_v.Branches[0][0].Value != -9999)
                {
                    f_v = _f_v.Branches; int np = f_v.Count;
                    for (int i = 0; i < np; i++)
                    {
                        int j = (int)f_v[i][0].Value;
                        r1 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value);
                        if (f_v[i][1].Value != 0)
                        {
                            r2 = new Point3d(r[j][0].Value - f_v[i][1].Value * arrowsize, r[j][1].Value, r[j][2].Value);
                            _value.Add(Math.Abs(f_v[i][1].Value));
                            _color2.Add(Color.Yellow);
                            _arrow.Add(new Line(r2, r1));
                            _point2.Add(r2);
                        }
                        if (f_v[i][2].Value != 0)
                        {
                            r2 = new Point3d(r[j][0].Value, r[j][1].Value - f_v[i][2].Value * arrowsize, r[j][2].Value);
                            _value.Add(Math.Abs(f_v[i][2].Value));
                            _color2.Add(Color.Yellow);
                            _arrow.Add(new Line(r2, r1));
                            _point2.Add(r2);
                        }
                        if (f_v[i][3].Value != 0)
                        {
                            r2 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value - f_v[i][3].Value * arrowsize);
                            _value.Add(Math.Abs(f_v[i][3].Value));
                            _color2.Add(Color.Yellow);
                            _arrow.Add(new Line(r2, r1));
                            _point2.Add(r2);
                        }
                        if (f_v[i][4].Value != 0)
                        {
                            _value2.Add(Math.Abs(f_v[i][4].Value));
                            _color2.Add(Color.Yellow);
                            if (f_v[i][4].Value < 0)///2021.05.22 bug fixed
                            {
                                r1 = new Point3d(r[j][0].Value, r[j][1].Value - arcsize, r[j][2].Value);
                                r2 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value + arcsize);
                                r3 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value - arcsize);
                                Arc arc = new Arc(r1, r2, r3);
                                _arc.Add(arc);
                                _vec.Add(new Vector3d(0, -1, -0.325));
                            }
                            else
                            {
                                r1 = new Point3d(r[j][0].Value, r[j][1].Value + arcsize, r[j][2].Value);
                                r2 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value + arcsize);
                                r3 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value - arcsize);
                                Arc arc = new Arc(r1, r2, r3);
                                _arc.Add(arc);
                                _vec.Add(new Vector3d(0, 1, -0.325));
                            }
                        }
                        if (f_v[i][5].Value != 0)
                        {
                            _value2.Add(Math.Abs(f_v[i][5].Value));
                            _color2.Add(Color.Yellow);
                            if (f_v[i][5].Value > 0)
                            {
                                r1 = new Point3d(r[j][0].Value - arcsize, r[j][1].Value, r[j][2].Value);
                                r2 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value + arcsize);
                                r3 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value - arcsize);
                                Arc arc = new Arc(r1, r2, r3);
                                _arc.Add(arc);
                                _vec.Add(new Vector3d(-1, 0, -0.325));
                            }
                            else
                            {
                                r1 = new Point3d(r[j][0].Value + arcsize, r[j][1].Value, r[j][2].Value);
                                r2 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value + arcsize);
                                r3 = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value - arcsize);
                                Arc arc = new Arc(r1, r2, r3);
                                _arc.Add(arc);
                                _vec.Add(new Vector3d(1, 0, -0.325));
                            }
                        }
                        if (f_v[i][6].Value != 0)
                        {
                            _value2.Add(Math.Abs(f_v[i][6].Value));
                            _color2.Add(Color.Yellow);
                            if (f_v[i][6].Value > 0)
                            {
                                r1 = new Point3d(r[j][0].Value - arcsize, r[j][1].Value, r[j][2].Value);
                                r2 = new Point3d(r[j][0].Value, r[j][1].Value + arcsize, r[j][2].Value);
                                r3 = new Point3d(r[j][0].Value, r[j][1].Value - arcsize, r[j][2].Value);
                                Arc arc = new Arc(r1, r2, r3);
                                _arc.Add(arc);
                                _vec.Add(new Vector3d(-1, -0.325, 0));
                            }
                            else
                            {
                                r1 = new Point3d(r[j][0].Value + arcsize, r[j][1].Value, r[j][2].Value);
                                r2 = new Point3d(r[j][0].Value, r[j][1].Value + arcsize, r[j][2].Value);
                                r3 = new Point3d(r[j][0].Value, r[j][1].Value - arcsize, r[j][2].Value);
                                Arc arc = new Arc(r1, r2, r3);
                                _arc.Add(arc);
                                _vec.Add(new Vector3d(1, -0.325, 0));
                            }
                        }
                    }
                }
                ///*************************************************************************************************
                ///面荷重の描画**************************************************************************************
                DA.GetDataTree("surface_force", out GH_Structure<GH_Number> _s_load);
                if (_s_load.Branches[0][0].Value != -9999)
                {
                    s_load = _s_load.Branches; int ns = s_load.Count;
                    if (Surf_Flr_load == 1)
                    {
                        //var color = new ColorHSL((1 - Math.Min(k / 7.0, 1.0)) * 1.9 / 3.0, 1, 0.5);
                        var smax = 0.0; var smin = 0.0;
                        for (int e = 0; e < ns; e++) { smax = Math.Max(Math.Abs(s_load[e][4].Value), smax); smin = Math.Min(Math.Abs(s_load[e][4].Value), smin); }
                        for (int e = 0; e < ns; e++)
                        {
                            List<List<double>> ri = new List<List<double>>();
                            int i = (int)s_load[e][0].Value; int j = (int)s_load[e][1].Value; int k = (int)s_load[e][2].Value; int l = (int)s_load[e][3].Value;
                            List<double> r_i = new List<double>(); List<double> r_j = new List<double>(); List<double> r_k = new List<double>(); List<double> r_l = new List<double>();
                            r_i.Add(r[i][0].Value); r_i.Add(r[i][1].Value); r_i.Add(r[i][2].Value);
                            r_j.Add(r[j][0].Value); r_j.Add(r[j][1].Value); r_j.Add(r[j][2].Value);
                            r_k.Add(r[k][0].Value); r_k.Add(r[k][1].Value); r_k.Add(r[k][2].Value);
                            ri.Add(r_i); ri.Add(r_j); ri.Add(r_k);
                            if (l != -1)
                            {
                                r_l.Add(r[l][0].Value); r_l.Add(r[l][1].Value); r_l.Add(r[l][2].Value);
                                ri.Add(r_l);
                                if (Value == 1)
                                {
                                    _rc.Add(new Point3d((ri[0][0] + ri[1][0] + ri[2][0] + ri[3][0]) / 4.0, (ri[0][1] + ri[1][1] + ri[2][1] + ri[3][1]) / 4.0, (ri[0][2] + ri[1][2] + ri[2][2] + ri[3][2]) / 4.0));
                                }
                            }
                            else
                            {
                                if (Value == 1)
                                {
                                    _rc.Add(new Point3d((ri[0][0] + ri[1][0] + ri[2][0]) / 3.0, (ri[0][1] + ri[1][1] + ri[2][1]) / 3.0, (ri[0][2] + ri[1][2] + ri[2][2]) / 3.0));
                                }
                            }
                            var s = Math.Round(s_load[e][4].Value, 3);
                            _r4.Add(ri); _vsurf.Add(s);_sloadcolor.Add(new ColorHSL((1 - Math.Min((-s-smin) / (smax-smin), 1.0)) * 1.9 / 3.0, 1, 0.5));
                        }
                    }
                }
                ///*************************************************************************************************
                ///境界条件の描画***********************************************************************************
                if (!DA.GetDataTree("boundary_condition", out GH_Structure<GH_Number> _fix)) { }
                else if (_fix.Branches[0][0].Value != -9999)
                {
                    fix = _fix.Branches; int nfix = fix.Count;
                    for (int i = 0; i < nfix; i++)
                    {
                        List<double> ri = new List<double>(); List<int> boundary = new List<int>();
                        ri.Add(r[(int)fix[i][0].Value][0].Value); ri.Add(r[(int)fix[i][0].Value][1].Value); ri.Add(r[(int)fix[i][0].Value][2].Value);
                        _ri.Add(ri);
                        boundary.Add((int)fix[i][1].Value); boundary.Add((int)fix[i][2].Value); boundary.Add((int)fix[i][3].Value); boundary.Add((int)fix[i][4].Value); boundary.Add((int)fix[i][5].Value); boundary.Add((int)fix[i][6].Value);
                        _boundary.Add(boundary);
                    }
                }
                DA.SetDataTree(4, _f_v); DA.SetDataTree(6, _s_load); DA.SetDataTree(7, _fix);
                ///*************************************************************************************************
            }
        }
        protected override System.Drawing.Bitmap Icon { get { return OpenSeesUtility.Properties.Resources.VisMod; } }
        public override Guid ComponentGuid { get { return new Guid("6ce3cc91-38ad-4638-93b1-b661fe3ac0e7"); } }
        ///ここからカスタム関数群********************************************************************************
        private readonly List<string> _node_No = new List<string>();
        private readonly List<string> _beam_No = new List<string>();
        private readonly List<string> _angle = new List<string>();
        private readonly List<string> _sec_No = new List<string>();
        private readonly List<string> _mat_No = new List<string>();
        private readonly List<string> _shell_No = new List<string>();
        private readonly List<string> _shell_mat_No = new List<string>();
        private readonly List<string> _e_load = new List<string>();
        private readonly List<Point3d> _pin = new List<Point3d>();
        private readonly List<Vector3d> _v = new List<Vector3d>();
        private readonly List<Point3d> _point = new List<Point3d>();
        private readonly List<Point3d> _epts = new List<Point3d>();
        private readonly List<Point3d> _points = new List<Point3d>();
        private readonly List<Point3d> _p1 = new List<Point3d>();
        private readonly List<Point3d> _p2 = new List<Point3d>();
        private readonly List<Color> _color = new List<Color>();
        private readonly List<Line> _springline = new List<Line>();
        private readonly List<Line> _springv= new List<Line>();
        private readonly List<Point3d> _spring_pc = new List<Point3d>();
        private readonly List<string> _spring_No = new List<string>();

        private readonly List<Line> _arrow = new List<Line>();
        private readonly List<Line> _arrows = new List<Line>();
        private readonly List<double> _value = new List<double>();
        private readonly List<Point3d> _point2 = new List<Point3d>();
        private readonly List<Color> _color2 = new List<Color>();
        private readonly List<Arc> _arc = new List<Arc>();
        private readonly List<Vector3d> _vec = new List<Vector3d>();
        private readonly List<double> _value2 = new List<double>();

        private readonly List<double> _vsurf = new List<double>();
        private readonly List<List<List<double>>> _r4 = new List<List<List<double>>>();
        private readonly List<Point3d> _rc = new List<Point3d>();
        private readonly List<Color> _sloadcolor = new List<Color>();
        private readonly List<double> _scale = new List<double>();

        private readonly List<List<double>> _ri = new List<List<double>>();
        private readonly List<List<int>> _boundary = new List<List<int>>();
        private readonly List<Brep> _shells = new List<Brep>();
        private readonly List<double> _valuet = new List<double>();
        private readonly List<Line> _sline = new List<Line>();

        private readonly List<Line> _kabew = new List<Line>();
        private readonly List<double> _kabebairitsu = new List<double>();
        private readonly List<Point3d> _kabew_p = new List<Point3d>();
        protected override void BeforeSolveInstance()
        {
            _node_No.Clear();
            _beam_No.Clear();
            _angle.Clear();
            _sec_No.Clear();
            _mat_No.Clear();
            _shell_No.Clear();
            _shell_mat_No.Clear();
            _e_load.Clear();
            _point.Clear();
            _epts.Clear();
            _p1.Clear();
            _p2.Clear();
            _color.Clear();
            _springline.Clear();
            _springv.Clear();
            _spring_pc.Clear();
            _spring_No.Clear();

            _arrow.Clear();
            _arrows.Clear();
            _value.Clear();
            _point2.Clear();
            _color2.Clear();
            _arc.Clear();
            _vec.Clear();
            _value2.Clear();

            _vsurf.Clear();
            _r4.Clear();
            _rc.Clear();
            _sloadcolor.Clear();
            _scale.Clear();

            _ri.Clear();
            _boundary.Clear();
            _shells.Clear();
            _points.Clear();
            _valuet.Clear();
            _pin.Clear();
            _v.Clear();
            _sline.Clear();
            _kabew.Clear();
            _kabebairitsu.Clear();
            _kabew_p.Clear();
        }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            args.Viewport.GetFrustumFarPlane(out Plane plane);
            RhinoViewport viewport = args.Viewport;
            ///節点の描画用関数*********************************************************************************
            for (int i = 0; i < _point.Count; i++)
            {
                Point3d point = _point[i];
                if (Node == 1)
                {
                    args.Display.DrawPoint(point, PointStyle.Square, 2, Color.Black);
                }
                if (Node_No == 1)
                {
                    double size = fontsize; plane.Origin = point;
                    viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                    Text3d drawText = new Text3d(_node_No[i], plane, size);
                    args.Display.Draw3dText(drawText, Color.Red);
                    drawText.Dispose();
                }
            }
            ///*************************************************************************************************
            ///ジョイントの描画*********************************************************************************
            for (int i = 0; i < _pin.Count; i++)
            {
                if (Joints == 1)
                {
                    Point3d point = _pin[i];
                    plane.Origin = point;
                    viewport.GetWorldToScreenScale(point, out double pixPerUnit);
                    var vec = _v[i] / _v[i].Length * 10 / (double)pixPerUnit;
                    args.Display.DrawPoint(point + vec, PointStyle.Circle, Color.Black, Color.PaleGreen, 3, 1, 0, 0, true, true);
                }
            }
            ///*************************************************************************************************
            ///*************************************************************************************************
            ///梁要素描画用関数*********************************************************************************
            for (int i = 0; i < _p1.Count; i++)
            {
                double size = fontsize; Point3d point = (_p1[i] +_p2[i]) /2.0; plane.Origin = point;
                viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                if (Beam == 1)
                {
                    args.Display.DrawLine(_p1[i], _p2[i], Color.Black);
                    if (Beam_No == 1)
                    {
                        Text3d drawText = new Text3d(_beam_No[i], plane, size);
                        args.Display.Draw3dText(drawText, Color.Purple);
                        drawText.Dispose();
                    }
                    if (Angle == 1)
                    {
                        Text3d drawText = new Text3d(_angle[i], plane, size);
                        args.Display.Draw3dText(drawText, Color.PapayaWhip);
                        drawText.Dispose();
                    }
                    if (Sec_No == 1)
                    {
                        Text3d drawText = new Text3d(_sec_No[i], plane, size);
                        args.Display.Draw3dText(drawText, Color.Crimson);
                        drawText.Dispose();
                    }
                    if (Mat_No == 1)
                    {
                        Text3d drawText = new Text3d(_mat_No[i], plane, size);
                        args.Display.Draw3dText(drawText, Color.Tomato);
                        drawText.Dispose();
                    }
                }
            }
            if (KabeYane == 1)///線材置換壁の描画
            {
                for (int i = 0; i < _kabew.Count; i++)
                    {
                    args.Display.DrawLine(_kabew[i], Color.DarkViolet);
                    if (Joints == 1)
                    {
                        Point3d p1 = _kabew[i].From; Point3d p2 = _kabew[i].To; var v1 = p2 - p1; var v2 = p1 - p2;
                        plane.Origin = p1; viewport.GetWorldToScreenScale(p1, out double pixPerUnit);
                        var vec = v1 / v1.Length * 10 / (double)pixPerUnit;
                        args.Display.DrawPoint(p1 + vec, PointStyle.Circle, Color.Black, Color.PaleGreen, 3, 1, 0, 0, true, true);
                        plane.Origin = p2; viewport.GetWorldToScreenScale(p2, out pixPerUnit);
                        vec = v2 / v2.Length * 10 / (double)pixPerUnit;
                        args.Display.DrawPoint(p2 + vec, PointStyle.Circle, Color.Black, Color.PaleGreen, 3, 1, 0, 0, true, true);
                    }
                }
                if (Value == 1)
                {
                    for (int i = 0; i < _kabebairitsu.Count; i++)
                    {
                        double size = fontsize; Point3d point = _kabew_p[i]; plane.Origin = point;
                        viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                        var k = _kabebairitsu[i];
                        var color = new ColorHSL((1 - Math.Min(k/7.0, 1.0)) * 1.9 / 3.0, 1, 0.5);
                        Text3d drawText = new Text3d(k.ToString(), plane, size);
                        args.Display.Draw3dText(drawText, color); drawText.Dispose();
                    }
                }
            }
            ///ばねの描画
            if (Spring == 1)
            {
                for (int i = 0; i < _springline.Count; i++)
                {
                    Line springline = _springline[i];
                    args.Display.DrawLine(springline, Color.Chocolate, 1);
                }
                for (int i = 0; i < _springv.Count; i++)
                {
                    Line springv = _springv[i];
                    args.Display.DrawLine(springv, Color.CadetBlue, 1);
                }
                if (Beam_No == 1)
                {
                    for (int i = 0; i < _spring_pc.Count; i++)
                    {
                        double size = fontsize; Point3d point = _spring_pc[i]; plane.Origin = point;
                        viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                        Text3d drawText = new Text3d(_spring_No[i], plane, size);
                        args.Display.Draw3dText(drawText, Color.HotPink);
                        drawText.Dispose();
                    }
                }
            }
            ///*************************************************************************************************
            ///*************************************************************************************************
            ///軸外力の描画用関数*******************************************************************************
            for (int i = 0; i < _arrow.Count; i++)
            {
                double size = fontsize;
                Point3d point = _point2[i];
                plane.Origin = point;
                viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                Text3d drawText = new Text3d(_value[i].ToString() + unit_of_force, plane, size);
                if (PxPyPz == 1)
                {
                    Line arrow = _arrow[i];
                    args.Display.DrawLine(arrow, _color2[i], 2);
                    args.Display.DrawArrowHead(arrow.To, arrow.Direction, _color2[i], 25, 0);
                }
                if (PxPyPz == 1 && Value == 1)
                {
                    args.Display.Draw3dText(drawText, _color2[i]); drawText.Dispose();
                }
            }
            ///*************************************************************************************************
            ///要素外力の描画用関数*****************************************************************************
            for (int i = 0; i < _arrows.Count; i++)
            {
                if (Beam_load == 1)
                {
                    Line arrow = _arrows[i];
                    args.Display.DrawLine(arrow, Color.SpringGreen, 2);
                    args.Display.DrawArrowHead(arrow.To, arrow.Direction, Color.SpringGreen, 25, 0);
                }
            }
            for (int i = 0; i < _epts.Count; i++)
            {
                if (Beam_load == 1 && Value == 1)
                {
                    double size = fontsize;
                    Point3d point = _epts[i];
                    plane.Origin = point;
                    viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                    //Text3d drawText = new Text3d(_e_load[i] + unit_of_force + "/" + unit_of_length, plane, size);
                    //args.Display.Draw3dText(drawText, Color.SpringGreen); drawText.Dispose();
                    var text = _e_load[i] + unit_of_force + "/" + unit_of_length;
                    args.Display.Draw3dText(text, Color.SpringGreen, plane, size, "", false, false, Rhino.DocObjects.TextHorizontalAlignment.Center, Rhino.DocObjects.TextVerticalAlignment.Middle);
                }
            }
            ///*************************************************************************************************
            ///モーメント外力の描画用関数***********************************************************************
            for (int i = 0; i < _arc.Count; i++)
            {
                double size = fontsize;
                Point3d point = _arc[i].StartPoint;
                plane.Origin = point;
                viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                Text3d drawText = new Text3d(_value2[i].ToString() + unit_of_force + unit_of_length, plane, size);
                if (MxMyMz == 1)
                {
                    args.Display.DrawArc(_arc[i], _color2[i], 2);
                    args.Display.DrawArrowHead(_arc[i].EndPoint, _vec[i], _color2[i], 25, 0);
                }
                if (MxMyMz == 1 && Value == 1)
                {
                    args.Display.Draw3dText(drawText, _color2[i]); drawText.Dispose();
                }
            }
            ///*************************************************************************************************
            ///面荷重用の描画用関数******************************************************************************
            for (int i = 0; i < _vsurf.Count; i++)
            {
                for (int j = 0; j < _r4[i].Count; j++)
                {
                    if (Surf_Flr_load == 1)
                    {
                        Point3d p1 = new Point3d(_r4[i][j][0], _r4[i][j][1], _r4[i][j][2]);
                        Point3d p2 = new Point3d(_r4[i][j][0], _r4[i][j][1], _r4[i][j][2] - _vsurf[i] * arrowsize);
                        Line arrow = new Line(p1, p2);
                        args.Display.DrawLine(arrow, _sloadcolor[i], 2);
                        if (arrowsize > 0)
                        {
                            if (_vsurf[i] < 0) { args.Display.DrawArrowHead(p1, new Vector3d(0, 0, -1), _sloadcolor[i], 25, 0); }
                            else { args.Display.DrawArrowHead(p1, new Vector3d(0, 0, 1), _sloadcolor[i], 25, 0); }
                        }
                        if (j != _r4[i].Count - 1)
                        {
                            Point3d p3 = new Point3d(_r4[i][j + 1][0], _r4[i][j + 1][1], _r4[i][j + 1][2] - _vsurf[i] * arrowsize);
                            Line arrow2 = new Line(p2, p3);
                            args.Display.DrawLine(arrow2, _sloadcolor[i], 2);
                        }
                        else
                        {
                            Point3d p3 = new Point3d(_r4[i][0][0], _r4[i][0][1], _r4[i][0][2] - _vsurf[i] * arrowsize);
                            Line arrow2 = new Line(p2, p3);
                            args.Display.DrawLine(arrow2, _sloadcolor[i], 2);
                        }
                    }
                }
            }
            if (Surf_Flr_load == 1 && Value == 1)
            {
                for (int i = 0; i < _rc.Count; i++)
                {
                    double size = fontsize; var point = new Point3d(_rc[i][0],_rc[i][1],_rc[i][2] - _vsurf[i] * arrowsize);
                    plane.Origin = point;
                    viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                    //Text3d drawText = new Text3d(Math.Abs(_vsurf[i]).ToString().Substring(0, Math.Min(5, Math.Abs(_vsurf[i]).ToString().Length)) + unit_of_force + '/' + unit_of_length + '2', plane, size);
                    var text = Math.Abs(_vsurf[i]).ToString().Substring(0, Math.Min(5, Math.Abs(_vsurf[i]).ToString().Length)) + unit_of_force + '/' + unit_of_length + '2';
                    args.Display.Draw3dText(text, _sloadcolor[i],plane,size,"",false,false, Rhino.DocObjects.TextHorizontalAlignment.Center, Rhino.DocObjects.TextVerticalAlignment.Bottom); //drawText.Dispose();
                }
            }
            ///*************************************************************************************************
            ///境界条件描画用関数*******************************************************************************
            if (Boundary == 1)
            {
                for (int i = 0; i < _boundary.Count; i++)
                {
                    List<int> ifix = _boundary[i];
                    List<double> ri = _ri[i];
                    if (ifix[0] == 1)
                    {
                        args.Display.DrawArrowHead(new Point3d(ri[0], ri[1], ri[2]), new Vector3d(1, 0, 0), Color.Green, 25, 0);
                    }
                    if (ifix[1] == 1)
                    {
                        args.Display.DrawArrowHead(new Point3d(ri[0], ri[1], ri[2]), new Vector3d(0, 1, 0), Color.Green, 25, 0);
                    }
                    if (ifix[2] == 1)
                    {
                        args.Display.DrawArrowHead(new Point3d(ri[0], ri[1], ri[2]), new Vector3d(0, 0, 1), Color.Green, 25, 0);
                    }
                    if (ifix[3] == 1)
                    {
                        args.Display.DrawArrowHead(new Point3d(ri[0], ri[1], ri[2]), new Vector3d(-1, 0, 0), Color.Pink, 25, 0);
                    }
                    if (ifix[4] == 1)
                    {
                        args.Display.DrawArrowHead(new Point3d(ri[0], ri[1], ri[2]), new Vector3d(0, -1, 0), Color.Pink, 25, 0);
                    }
                    if (ifix[5] == 1)
                    {
                        args.Display.DrawArrowHead(new Point3d(ri[0], ri[1], ri[2]), new Vector3d(0, 0, -1), Color.Pink, 25, 0);
                    }
                }
            }
            ///*************************************************************************************************
            ///シェル要素描画用関数*****************************************************************************
            for (int i = 0; i < _shells.Count; i++)
            {
                double size = fontsize; Point3d point = _points[i];
                plane.Origin = point;
                viewport.GetWorldToScreenScale(point, out double pixPerUnit); size /= pixPerUnit;
                if (Thick == 1)
                {
                    Text3d drawText = new Text3d(_valuet[i].ToString().Substring(0, Math.Min(5, _valuet[i].ToString().Length)) + unit_of_length, plane, size);
                    args.Display.Draw3dText(drawText, Color.DarkOliveGreen); drawText.Dispose();
                }
                if (Shell == 1)
                {
                    var material = new DisplayMaterial(Color.Brown); material.Transparency = 0.8; material.BackTransparency = 0.8;
                    args.Display.DrawBrepShaded(_shells[i], material);
                }
                if (Shell_No == 1)
                {
                    Text3d drawText = new Text3d(_shell_No[i], plane, size);
                    args.Display.Draw3dText(drawText, Color.MediumTurquoise); drawText.Dispose();
                }
                if (Mat_No_Shell == 1)
                {
                    Text3d drawText = new Text3d(_shell_mat_No[i], plane, size);
                    args.Display.Draw3dText(drawText, Color.Orange); drawText.Dispose();
                }
            }
            for (int i = 0; i < _sline.Count; i++)
            {
                if (Shell == 1) 
                {
                    args.Display.DrawLine(_sline[i],Color.Black,1);
                }
            }
            ///*************************************************************************************************
        }
        ///ここまでカスタム関数群********************************************************************************
        //////ここからGUIの作成*****************************************************************************************
        public class CustomGUI : GH_ComponentAttributes
        {
            public CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle title_rec; private Rectangle title1_rec; private Rectangle title2_rec; private Rectangle title3_rec;
            private Rectangle radio_rec; private Rectangle radio2_rec; private Rectangle radio3_rec; private Rectangle radio4_rec;
            private Rectangle radio_rec_11; private Rectangle text_rec_11; private Rectangle radio_rec_12; private Rectangle text_rec_12; private Rectangle radio_rec_13; private Rectangle text_rec_13;
            private Rectangle radio_rec_21; private Rectangle text_rec_21; private Rectangle radio_rec_22; private Rectangle text_rec_22; private Rectangle radio_rec_23; private Rectangle text_rec_23;
            private Rectangle radio_rec2_11; private Rectangle text_rec2_11; private Rectangle radio_rec2_12; private Rectangle text_rec2_12; private Rectangle radio_rec2_13; private Rectangle text_rec2_13; private Rectangle radio_rec2_14; private Rectangle text_rec2_14;
            private Rectangle radio_rec2_21; private Rectangle text_rec2_21; private Rectangle radio_rec2_22; private Rectangle text_rec2_22; private Rectangle radio_rec2_23; private Rectangle text_rec2_23;
            private Rectangle radio_rec3_11; private Rectangle text_rec3_11; private Rectangle radio_rec3_12; private Rectangle text_rec3_12; private Rectangle radio_rec3_13; private Rectangle text_rec3_13;
            private Rectangle radio_rec3_21; private Rectangle text_rec3_21; private Rectangle radio_rec3_22; private Rectangle text_rec3_22; private Rectangle radio_rec3_23; private Rectangle text_rec3_23;
            private Rectangle radio_rec4_11; private Rectangle text_rec4_11;

            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int subwidth = 38; int radi1 = 7; int radi2 = 4;
                int pitchx = 6; int textheight = 20; int subtitleheight = 18;
                title_rec = global_rec;
                title_rec.Y = title_rec.Bottom;
                title_rec.Height = 22;

                radio_rec = title_rec;
                radio_rec.Y += title_rec.Height;

                title1_rec = radio_rec;
                title1_rec.Height = subtitleheight;

                radio_rec_11 = title1_rec;
                radio_rec_11.X += radi2-1; radio_rec_11.Y += title1_rec.Height + radi2;
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
                radio_rec_21.Y += text_rec_11.Height - radi1;
                radio_rec_21.Height = radi1; radio_rec_11.Width = radi1;

                text_rec_21 = radio_rec_21;
                text_rec_21.X += pitchx; text_rec_21.Y -= radi2;
                text_rec_21.Height = textheight; text_rec_21.Width = subwidth;

                radio_rec_22 = text_rec_21;
                radio_rec_22.X += text_rec_21.Width-radi2; radio_rec_22.Y = radio_rec_21.Y;
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
                ///******************************************************************************************
                radio2_rec = radio_rec;
                radio2_rec.Y = radio_rec.Y + radio_rec.Height;

                title2_rec = title1_rec;
                title2_rec.Y = radio2_rec.Y;

                radio_rec2_11 = title2_rec;
                radio_rec2_11.X += radi2 - 1; radio_rec2_11.Y += title2_rec.Height + radi2;
                radio_rec2_11.Height = radi1; radio_rec2_11.Width = radi1;

                text_rec2_11 = radio_rec2_11;
                text_rec2_11.X += pitchx; text_rec2_11.Y -= radi2;
                text_rec2_11.Height = textheight; text_rec2_11.Width = subwidth;

                radio_rec2_12 = text_rec2_11;
                radio_rec2_12.X += text_rec2_11.Width - radi2; radio_rec2_12.Y = radio_rec2_11.Y;
                radio_rec2_12.Height = radi1; radio_rec2_12.Width = radi1;

                text_rec2_12 = radio_rec2_12;
                text_rec2_12.X += pitchx; text_rec2_12.Y -= radi2;
                text_rec2_12.Height = textheight; text_rec2_12.Width = subwidth;

                radio_rec2_13 = text_rec2_12;
                radio_rec2_13.X += text_rec2_12.Width - radi2; radio_rec2_13.Y = radio_rec2_12.Y;
                radio_rec2_13.Height = radi1; radio_rec2_13.Width = radi1;

                text_rec2_13 = radio_rec2_13;
                text_rec2_13.X += pitchx; text_rec2_13.Y -= radi2;
                text_rec2_13.Height = textheight; text_rec2_13.Width = subwidth;

                radio_rec2_14 = text_rec2_13;
                radio_rec2_14.X += text_rec2_13.Width - radi2; radio_rec2_14.Y = radio_rec2_13.Y;
                radio_rec2_14.Height = radi1; radio_rec2_14.Width = radi1;

                text_rec2_14 = radio_rec2_14;
                text_rec2_14.X += pitchx; text_rec2_14.Y -= radi2;
                text_rec2_14.Height = textheight; text_rec2_14.Width = subwidth + 50;

                radio_rec2_21 = radio_rec2_11;
                radio_rec2_21.Y += text_rec2_11.Height - radi1;
                radio_rec2_21.Height = radi1; radio_rec2_11.Width = radi1;

                text_rec2_21 = radio_rec2_21;
                text_rec2_21.X += pitchx; text_rec2_21.Y -= radi2;
                text_rec2_21.Height = textheight; text_rec2_21.Width = subwidth;

                radio_rec2_22 = text_rec2_21;
                radio_rec2_22.X += text_rec2_21.Width - radi2; radio_rec2_22.Y = radio_rec2_21.Y;
                radio_rec2_22.Height = radi1; radio_rec2_22.Width = radi1;

                text_rec2_22 = radio_rec2_22;
                text_rec2_22.X += pitchx; text_rec2_22.Y -= radi2;
                text_rec2_22.Height = textheight; text_rec2_22.Width = subwidth;

                radio_rec2_23 = text_rec2_22;
                radio_rec2_23.X += text_rec2_22.Width - radi2; radio_rec2_23.Y = radio_rec2_22.Y;
                radio_rec2_23.Height = radi1; radio_rec2_23.Width = radi1;

                text_rec2_23 = radio_rec2_23;
                text_rec2_23.X += pitchx; text_rec2_23.Y -= radi2;
                text_rec2_23.Height = textheight; text_rec2_23.Width = subwidth + 30;

                radio2_rec.Height = text_rec2_23.Y + textheight - radio2_rec.Y - radi2;
                ///******************************************************************************************
                radio3_rec = radio2_rec;
                radio3_rec.Y = radio2_rec.Y + radio2_rec.Height;

                title3_rec = title1_rec;
                title3_rec.Y = radio3_rec.Y;

                radio_rec3_11 = title3_rec;
                radio_rec3_11.X += radi2 - 1; radio_rec3_11.Y += title3_rec.Height + radi2;
                radio_rec3_11.Height = radi1; radio_rec3_11.Width = radi1;

                text_rec3_11 = radio_rec3_11;
                text_rec3_11.X += pitchx; text_rec3_11.Y -= radi2;
                text_rec3_11.Height = textheight; text_rec3_11.Width = subwidth;

                radio_rec3_12 = text_rec3_11;
                radio_rec3_12.X += text_rec3_11.Width - radi2; radio_rec3_12.Y = radio_rec3_11.Y;
                radio_rec3_12.Height = radi1; radio_rec3_12.Width = radi1;

                text_rec3_12 = radio_rec3_12;
                text_rec3_12.X += pitchx; text_rec3_12.Y -= radi2;
                text_rec3_12.Height = textheight; text_rec3_12.Width = subwidth;

                radio_rec3_13 = text_rec3_12;
                radio_rec3_13.X += text_rec3_12.Width - radi2; radio_rec3_13.Y = radio_rec3_12.Y;
                radio_rec3_13.Height = radi1; radio_rec3_13.Width = radi1;

                text_rec3_13 = radio_rec3_13;
                text_rec3_13.X += pitchx; text_rec3_13.Y -= radi2;
                text_rec3_13.Height = textheight; text_rec3_13.Width = subwidth + 30;

                radio_rec3_21 = radio_rec3_11;
                radio_rec3_21.Y += text_rec3_11.Height - radi1;
                radio_rec3_21.Height = radi1; radio_rec3_11.Width = radi1;

                text_rec3_21 = radio_rec3_21;
                text_rec3_21.X += pitchx; text_rec3_21.Y -= radi2;
                text_rec3_21.Height = textheight; text_rec3_21.Width = subwidth;

                radio_rec3_22 = text_rec3_21;
                radio_rec3_22.X += text_rec3_21.Width - radi2; radio_rec3_22.Y = radio_rec3_21.Y;
                radio_rec3_22.Height = radi1; radio_rec3_22.Width = radi1;

                text_rec3_22 = radio_rec3_22;
                text_rec3_22.X += pitchx; text_rec3_22.Y -= radi2;
                text_rec3_22.Height = textheight; text_rec3_22.Width = subwidth;

                radio_rec3_23 = text_rec3_22;
                radio_rec3_23.X += text_rec3_22.Width - radi2; radio_rec3_23.Y = radio_rec3_22.Y;
                radio_rec3_23.Height = radi1; radio_rec3_23.Width = radi1;

                text_rec3_23 = radio_rec3_23;
                text_rec3_23.X += pitchx; text_rec3_23.Y -= radi2;
                text_rec3_23.Height = textheight; text_rec3_23.Width = subwidth + 30;

                radio3_rec.Height = text_rec3_23.Y + textheight - radio3_rec.Y - radi2;
                ///******************************************************************************************
                radio4_rec = radio3_rec; radio4_rec.Height = textheight-radi2;
                radio4_rec.Y += radio3_rec.Height;

                radio_rec4_11 = radio4_rec;
                radio_rec4_11.X += radi2 - 1; radio_rec4_11.Y += radi2;
                radio_rec4_11.Height = radi1; radio_rec4_11.Width = radi1;

                text_rec4_11 = radio_rec4_11;
                text_rec4_11.X += pitchx; text_rec4_11.Y -= radi2;
                text_rec4_11.Height = textheight; text_rec4_11.Width = subwidth+30;

                global_rec.Height += (radio_rec4_11.Bottom - global_rec.Bottom);
                Bounds = global_rec;
            }
            Brush c11 = Brushes.White; Brush c12 = Brushes.White; Brush c13 = Brushes.White; Brush c21 = Brushes.White; Brush c22 = Brushes.White; Brush c23 = Brushes.White;
            Brush c211 = Brushes.White; Brush c212 = Brushes.White; Brush c213 = Brushes.White; Brush c214 = Brushes.White; Brush c221 = Brushes.White; Brush c222 = Brushes.White; Brush c223 = Brushes.White;
            Brush c311 = Brushes.White; Brush c312 = Brushes.White; Brush c313 = Brushes.White; Brush c321 = Brushes.White; Brush c322 = Brushes.White; Brush c323 = Brushes.White;
            Brush c411 = Brushes.White;
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

                    GH_Capsule title1 = GH_Capsule.CreateCapsule(title1_rec, GH_Palette.Blue, 2, 0);
                    title1.Render(graphics, Selected, Owner.Locked, false);
                    title1.Dispose();

                    RectangleF textRectangle1 = title1_rec;
                    textRectangle1.Height = 20;
                    graphics.DrawString("About node", GH_FontServer.Standard, Brushes.White, textRectangle1, format);

                    GH_Capsule radio_11 = GH_Capsule.CreateCapsule(radio_rec_11, GH_Palette.Black, 5, 5);
                    radio_11.Render(graphics, Selected, Owner.Locked, false); radio_11.Dispose();
                    graphics.FillEllipse(c11, radio_rec_11);
                    graphics.DrawString("Pt", GH_FontServer.Standard, Brushes.Black, text_rec_11);
                    
                    GH_Capsule radio_12 = GH_Capsule.CreateCapsule(radio_rec_12, GH_Palette.Black, 5, 5);
                    radio_12.Render(graphics, Selected, Owner.Locked, false); radio_12.Dispose();
                    graphics.FillEllipse(c12, radio_rec_12);
                    graphics.DrawString("No.", GH_FontServer.Standard, Brushes.Black, text_rec_12);
                    
                    GH_Capsule radio_13 = GH_Capsule.CreateCapsule(radio_rec_13, GH_Palette.Black, 5, 5);
                    radio_13.Render(graphics, Selected, Owner.Locked, false); radio_13.Dispose();
                    graphics.FillEllipse(c13, radio_rec_13);
                    graphics.DrawString("Bnds", GH_FontServer.Standard, Brushes.Black, text_rec_13);
                    
                    GH_Capsule radio_21 = GH_Capsule.CreateCapsule(radio_rec_21, GH_Palette.Black, 5, 5);
                    radio_21.Render(graphics, Selected, Owner.Locked, false); radio_21.Dispose();
                    graphics.FillEllipse(c21, radio_rec_21);
                    graphics.DrawString("P_f", GH_FontServer.Standard, Brushes.Black, text_rec_21);
                    
                    GH_Capsule radio_22 = GH_Capsule.CreateCapsule(radio_rec_22, GH_Palette.Black, 5, 5);
                    radio_22.Render(graphics, Selected, Owner.Locked, false); radio_22.Dispose();
                    graphics.FillEllipse(c22, radio_rec_22);
                    graphics.DrawString("M_f", GH_FontServer.Standard, Brushes.Black, text_rec_22);
                    
                    GH_Capsule radio_23 = GH_Capsule.CreateCapsule(radio_rec_23, GH_Palette.Black, 5, 5);
                    radio_23.Render(graphics, Selected, Owner.Locked, false); radio_23.Dispose();
                    graphics.FillEllipse(c23, radio_rec_23);
                    graphics.DrawString("Joint", GH_FontServer.Standard, Brushes.Black, text_rec_23);
                    ///******************************************************************************************
                    GH_Capsule radio2 = GH_Capsule.CreateCapsule(radio2_rec, GH_Palette.White, 2, 0);
                    radio2.Render(graphics, Selected, Owner.Locked, false); radio2.Dispose();

                    GH_Capsule title2 = GH_Capsule.CreateCapsule(title2_rec, GH_Palette.Blue, 2, 0);
                    title2.Render(graphics, Selected, Owner.Locked, false);
                    title2.Dispose();

                    RectangleF textRectangle2 = title2_rec;
                    textRectangle2.Height = 20;
                    graphics.DrawString("About beam", GH_FontServer.Standard, Brushes.White, textRectangle2, format);
                    ///******************************************************************************************

                    GH_Capsule radio2_11 = GH_Capsule.CreateCapsule(radio_rec2_11, GH_Palette.Black, 5, 5);
                    radio2_11.Render(graphics, Selected, Owner.Locked, false); radio2_11.Dispose();
                    graphics.FillEllipse(c211, radio_rec2_11);
                    graphics.DrawString("Line", GH_FontServer.Standard, Brushes.Black, text_rec2_11);

                    GH_Capsule radio2_12 = GH_Capsule.CreateCapsule(radio_rec2_12, GH_Palette.Black, 5, 5);
                    radio2_12.Render(graphics, Selected, Owner.Locked, false); radio2_12.Dispose();
                    graphics.FillEllipse(c212, radio_rec2_12);
                    graphics.DrawString("No", GH_FontServer.Standard, Brushes.Black, text_rec2_12);

                    GH_Capsule radio2_13 = GH_Capsule.CreateCapsule(radio_rec2_13, GH_Palette.Black, 5, 5);
                    radio2_13.Render(graphics, Selected, Owner.Locked, false); radio2_13.Dispose();
                    graphics.FillEllipse(c213, radio_rec2_13);
                    graphics.DrawString("θ", GH_FontServer.Standard, Brushes.Black, text_rec2_13);

                    GH_Capsule radio2_14 = GH_Capsule.CreateCapsule(radio_rec2_14, GH_Palette.Black, 5, 5);
                    radio2_14.Render(graphics, Selected, Owner.Locked, false); radio2_14.Dispose();
                    graphics.FillEllipse(c214, radio_rec2_14);
                    graphics.DrawString("Spring", GH_FontServer.Standard, Brushes.Black, text_rec2_14);


                    GH_Capsule radio2_21 = GH_Capsule.CreateCapsule(radio_rec2_21, GH_Palette.Black, 5, 5);
                    radio2_21.Render(graphics, Selected, Owner.Locked, false); radio2_21.Dispose();
                    graphics.FillEllipse(c221, radio_rec2_21);
                    graphics.DrawString("Sec", GH_FontServer.Standard, Brushes.Black, text_rec2_21);

                    GH_Capsule radio2_22 = GH_Capsule.CreateCapsule(radio_rec2_22, GH_Palette.Black, 5, 5);
                    radio2_22.Render(graphics, Selected, Owner.Locked, false); radio2_22.Dispose();
                    graphics.FillEllipse(c222, radio_rec2_22);
                    graphics.DrawString("Mat", GH_FontServer.Standard, Brushes.Black, text_rec2_22);

                    GH_Capsule radio2_23 = GH_Capsule.CreateCapsule(radio_rec2_23, GH_Palette.Black, 5, 5);
                    radio2_23.Render(graphics, Selected, Owner.Locked, false); radio2_23.Dispose();
                    graphics.FillEllipse(c223, radio_rec2_23);
                    graphics.DrawString("Ele_f", GH_FontServer.Standard, Brushes.Black, text_rec2_23);
                    ///******************************************************************************************
                    GH_Capsule radio3 = GH_Capsule.CreateCapsule(radio3_rec, GH_Palette.White, 2, 0);
                    radio3.Render(graphics, Selected, Owner.Locked, false); radio3.Dispose();

                    GH_Capsule title3 = GH_Capsule.CreateCapsule(title3_rec, GH_Palette.Blue, 2, 0);
                    title3.Render(graphics, Selected, Owner.Locked, false);
                    title3.Dispose();

                    RectangleF textRectangle3 = title3_rec;
                    textRectangle3.Height = 20;
                    graphics.DrawString("About surface", GH_FontServer.Standard, Brushes.White, textRectangle3, format);
                    ///******************************************************************************************

                    GH_Capsule radio3_11 = GH_Capsule.CreateCapsule(radio_rec3_11, GH_Palette.Black, 5, 5);
                    radio3_11.Render(graphics, Selected, Owner.Locked, false); radio3_11.Dispose();
                    graphics.FillEllipse(c311, radio_rec3_11);
                    graphics.DrawString("Surf", GH_FontServer.Standard, Brushes.Black, text_rec3_11);

                    GH_Capsule radio3_12 = GH_Capsule.CreateCapsule(radio_rec3_12, GH_Palette.Black, 5, 5);
                    radio3_12.Render(graphics, Selected, Owner.Locked, false); radio3_12.Dispose();
                    graphics.FillEllipse(c312, radio_rec3_12);
                    graphics.DrawString("No", GH_FontServer.Standard, Brushes.Black, text_rec3_12);

                    GH_Capsule radio3_13 = GH_Capsule.CreateCapsule(radio_rec3_13, GH_Palette.Black, 5, 5);
                    radio3_13.Render(graphics, Selected, Owner.Locked, false); radio3_13.Dispose();
                    graphics.FillEllipse(c313, radio_rec3_13);
                    graphics.DrawString("Prs_f", GH_FontServer.Standard, Brushes.Black, text_rec3_13);


                    GH_Capsule radio3_21 = GH_Capsule.CreateCapsule(radio_rec3_21, GH_Palette.Black, 5, 5);
                    radio3_21.Render(graphics, Selected, Owner.Locked, false); radio3_21.Dispose();
                    graphics.FillEllipse(c321, radio_rec3_21);
                    graphics.DrawString("Thick", GH_FontServer.Standard, Brushes.Black, text_rec3_21);

                    GH_Capsule radio3_22 = GH_Capsule.CreateCapsule(radio_rec3_22, GH_Palette.Black, 5, 5);
                    radio3_22.Render(graphics, Selected, Owner.Locked, false); radio3_22.Dispose();
                    graphics.FillEllipse(c322, radio_rec3_22);
                    graphics.DrawString("Mat", GH_FontServer.Standard, Brushes.Black, text_rec3_22);

                    GH_Capsule radio3_23 = GH_Capsule.CreateCapsule(radio_rec3_23, GH_Palette.Black, 5, 5);
                    radio3_23.Render(graphics, Selected, Owner.Locked, false); radio3_23.Dispose();
                    graphics.FillEllipse(c323, radio_rec3_23);
                    graphics.DrawString("KabeYane", GH_FontServer.Standard, Brushes.Black, text_rec3_23);
                    ///******************************************************************************************
                    GH_Capsule radio4 = GH_Capsule.CreateCapsule(radio4_rec, GH_Palette.White, 2, 0);
                    radio4.Render(graphics, Selected, Owner.Locked, false); radio4.Dispose();

                    GH_Capsule radio4_11 = GH_Capsule.CreateCapsule(radio_rec4_11, GH_Palette.Black, 5, 5);
                    radio4_11.Render(graphics, Selected, Owner.Locked, false); radio4_11.Dispose();
                    graphics.FillEllipse(c411, radio_rec4_11);
                    graphics.DrawString("Value", GH_FontServer.Standard, Brushes.Black, text_rec4_11);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec11 = radio_rec_11; RectangleF rec12 = radio_rec_12; RectangleF rec13 = radio_rec_13;
                    RectangleF rec21 = radio_rec_21; RectangleF rec22 = radio_rec_22; RectangleF rec23 = radio_rec_23;
                    RectangleF rec211 = radio_rec2_11; RectangleF rec212 = radio_rec2_12; RectangleF rec213 = radio_rec2_13; RectangleF rec214 = radio_rec2_14;
                    RectangleF rec221 = radio_rec2_21; RectangleF rec222 = radio_rec2_22; RectangleF rec223 = radio_rec2_23;
                    RectangleF rec311 = radio_rec3_11; RectangleF rec312 = radio_rec3_12; RectangleF rec313 = radio_rec3_13;
                    RectangleF rec321 = radio_rec3_21; RectangleF rec322 = radio_rec3_22; RectangleF rec323 = radio_rec3_23;
                    RectangleF rec411 = radio_rec4_11;
                    if (rec11.Contains(e.CanvasLocation))
                    {
                        if (c11 == Brushes.White) { c11 = Brushes.Black; SetButton("Nodes", 1); }
                        else { c11 = Brushes.White; SetButton("Nodes", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    
                    else if (rec12.Contains(e.CanvasLocation))
                    {
                        if (c12 == Brushes.White) { c12 = Brushes.Black; SetButton("Node No.", 1); }
                        else { c12 = Brushes.White; SetButton("Node No.", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec13.Contains(e.CanvasLocation))
                    {
                        if (c13 == Brushes.White) { c13 = Brushes.Black; SetButton("Boundary", 1); }
                        else { c13 = Brushes.White; SetButton("Boundary", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec21.Contains(e.CanvasLocation))
                    {
                        if (c21 == Brushes.White) { c21 = Brushes.Black; SetButton("PxPyPz", 1); }
                        else { c21 = Brushes.White; SetButton("PxPyPz", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec22.Contains(e.CanvasLocation))
                    {
                        if (c22 == Brushes.White) { c22 = Brushes.Black; SetButton("MxMyMz", 1); }
                        else { c22 = Brushes.White; SetButton("MxMyMz", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec23.Contains(e.CanvasLocation))
                    {
                        if (c23 == Brushes.White) { c23 = Brushes.Black; SetButton("Joints", 1); }
                        else { c23 = Brushes.White; SetButton("Joints", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec211.Contains(e.CanvasLocation))
                    {
                        if (c211 == Brushes.White) { c211 = Brushes.Black; SetButton("Beam", 1); }
                        else { c211 = Brushes.White; SetButton("Beam", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec212.Contains(e.CanvasLocation))
                    {
                        if (c212 == Brushes.White) { c212 = Brushes.Black; SetButton("Beam_No", 1); c213 = Brushes.White; SetButton("Angle", 0); c221 = Brushes.White; SetButton("Sec_No", 0); c222 = Brushes.White; SetButton("Mat_No", 0); }
                        else { c212 = Brushes.White; SetButton("Beam_No", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec213.Contains(e.CanvasLocation))
                    {
                        if (c213 == Brushes.White) { c212 = Brushes.White; SetButton("Beam_No", 0); c213 = Brushes.Black; SetButton("Angle", 1); c221 = Brushes.White; SetButton("Sec_No", 0); c222 = Brushes.White; SetButton("Mat_No", 0); }
                        else { c213 = Brushes.White; SetButton("Angle", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec214.Contains(e.CanvasLocation))
                    {
                        if (c214 == Brushes.White) { c214 = Brushes.Black; SetButton("Spring", 1); }
                        else { c214 = Brushes.White; SetButton("Spring", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec221.Contains(e.CanvasLocation))
                    {
                        if (c221 == Brushes.White) { c212 = Brushes.White; SetButton("Beam_No", 0); c213 = Brushes.White; SetButton("Angle", 0); c221 = Brushes.Black; SetButton("Sec_No", 1); c222 = Brushes.White; SetButton("Mat_No", 0); }
                        else { c221 = Brushes.White; SetButton("Sec_No", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec222.Contains(e.CanvasLocation))
                    {
                        if (c222 == Brushes.White) { c212 = Brushes.White; SetButton("Beam_No", 0); c213 = Brushes.White; SetButton("Angle", 0); c221 = Brushes.White; SetButton("Sec_No", 0); c222 = Brushes.Black; SetButton("Mat_No", 1); }
                        else { c222 = Brushes.White; SetButton("Mat_No", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec223.Contains(e.CanvasLocation))
                    {
                        if (c223 == Brushes.White) { c223 = Brushes.Black; SetButton("Beam_load", 1); }
                        else { c223 = Brushes.White; SetButton("Beam_load", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec311.Contains(e.CanvasLocation))
                    {
                        if (c311 == Brushes.White) { c311 = Brushes.Black; SetButton("Shell", 1); }
                        else { c311 = Brushes.White; SetButton("Shell", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec312.Contains(e.CanvasLocation))
                    {
                        if (c312 == Brushes.White) { c312 = Brushes.Black; SetButton("Shell_No", 1); c321 = Brushes.White; SetButton("Thick", 0); c322 = Brushes.White; SetButton("Mat_No(shell)", 0); }
                        else { c312 = Brushes.White; SetButton("Shell_No", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec313.Contains(e.CanvasLocation))
                    {
                        if (c313 == Brushes.White) { c313 = Brushes.Black; SetButton("sf_load", 1); }
                        else { c313 = Brushes.White; SetButton("sf_load", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec321.Contains(e.CanvasLocation))
                    {
                        if (c321 == Brushes.White) { c321 = Brushes.Black; SetButton("Thick", 1); c312 = Brushes.White; SetButton("Shell_No", 0); c322 = Brushes.White; SetButton("Mat_No(shell)", 0); }
                        else { c321 = Brushes.White; SetButton("Thick", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec322.Contains(e.CanvasLocation))
                    {
                        if (c322 == Brushes.White) { c322 = Brushes.Black; SetButton("Mat_No(shell)", 1); c312 = Brushes.White; SetButton("Shell_No", 0); c321 = Brushes.White; SetButton("Thick", 0); }
                        else { c322 = Brushes.White; SetButton("Mat_No(shell)", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec323.Contains(e.CanvasLocation))
                    {
                        if (c323 == Brushes.White) { c323 = Brushes.Black; SetButton("KabeYane", 1); }
                        else { c323 = Brushes.White; SetButton("KabeYane", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                    else if (rec411.Contains(e.CanvasLocation))
                    {
                        if (c411 == Brushes.White) { c411 = Brushes.Black; SetButton("Value", 1); }
                        else { c411 = Brushes.White; SetButton("Value", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}
