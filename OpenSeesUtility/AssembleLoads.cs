using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

using System.Drawing;
using System.Windows.Forms;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
///****************************************

namespace AssembleLoads
{
    public class AssembleLoads : GH_Component
    {
        static int Kamenoko = 0;
        public static void SetButton(string s, int i)
        {
            if (s == "Kamenoko")
            {
                Kamenoko = i;
            }
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomGUI(this);
        }
        public AssembleLoads()
          : base("Assemble nodal, element, and surface Loads", "AssembleLoads",
              "Assemble nodal, element, and surface Loads for OpenSees",
              "OpenSees", "PreProcess")
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
            pManager.AddNumberParameter("joint condition", "joint", "[[Ele. No., 0 or 1(means i or j), kx, ky, kz, rx, ry, rz(spring value)],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("nodal_load", "p_load", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("element_load", "e_load", "[[Element No.,line_load],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("floor_load", "f_load", "[[No.i,No.j,No.k,No.l,floor_load],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("surface_load", "s_load", "[[No.i,No.j,No.k,No.l,surface_load],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("wall_load", "w_load", "[[No.i,No.j,No.k,No.l,wall_load],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("section area", "A", "[...](DataList)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("weight density", "rho", "[...](DataList)", GH_ParamAccess.list, -9999);///
            pManager.AddNumberParameter("accuracy", "accuracy", "intersection accuracy", GH_ParamAccess.item, 1e-10);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_load", "F", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("nodal_load(local coordinate system)", "f", "[[element No.,Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("nodal_coordinates(considering joints)", "Rj", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            IList<List<GH_Number>> r; IList<List<GH_Number>> ij; IList<List<GH_Number>> p_load; IList<List<GH_Number>> s_load; IList<List<GH_Number>> f_load; IList<List<GH_Number>> l_load; List<double> lgh; IList<List<GH_Number>> w_load; var acc = 1e-10;DA.GetData("accuracy", ref acc);
            List<List<int>> ij_new = new List<List<int>>();
            List<double> length()
            {
                List<double> l = new List<double>();
                if (ij[0][0].Value != -9999)
                {
                    for (int e = 0; e < ij.Count; e++)
                    {
                        int i = (int)ij[e][0].Value; int j = (int)ij[e][1].Value;
                        l.Add(Math.Sqrt(Math.Pow(r[i][0].Value - r[j][0].Value, 2) + Math.Pow(r[i][1].Value - r[j][1].Value, 2) + Math.Pow(r[i][2].Value - r[j][2].Value, 2)));
                    }
                }
                return l;
            }
            Vector3d calc_cross_point(Vector3d x, Vector3d y, Vector3d x_c, Vector3d y_c)
            {
                double d1 = Vector3d.CrossProduct(y_c, x - y).Length;
                double d2 = Vector3d.CrossProduct(y_c, x + x_c - y).Length;
                double t = d1 / (d1 + d2);
                return x + x_c * t;
            }
            Vector3d tri_center(Vector3d a, Vector3d b, Vector3d c)
            {
                double a_bar = (c - b).Length; double b_bar = (c - a).Length; double c_bar = (b - a).Length;
                return (a_bar * a + b_bar * b + c_bar * c) / (a_bar + b_bar + c_bar);
            }
            IList<List<Vector3d>> calc_4cross_points(Vector3d a, Vector3d b, Vector3d c, Vector3d d)
            {
                Vector3d ab = (b - a) / ((b - a).Length); Vector3d bc = (c - b) / ((c - b).Length); Vector3d cd = (d - c) / ((d - c).Length); Vector3d da = (a - d) / ((a - d).Length);
                Vector3d ba = -ab; Vector3d cb = -bc; Vector3d dc = -cd; Vector3d ad = -da;
                Vector3d a_c = (ab + ad) / 2.0 * 1000; Vector3d b_c = (ba + bc) / 2.0 * 1000; Vector3d c_c = (cb + cd) / 2.0 * 1000; Vector3d d_c = (dc + da) / 2.0 * 1000;
                Vector3d pab = calc_cross_point(a, b, a_c, b_c); Vector3d pbc = calc_cross_point(b, c, b_c, c_c); Vector3d pcd = calc_cross_point(c, d, c_c, d_c); Vector3d pda = calc_cross_point(d, a, d_c, a_c);
                double A1 = (pab-a).Length;
                double A2 = (pda-a).Length;
                double B1 = (pbc-b).Length;
                double B2 = (pab-b).Length;
                double C1 = (pcd-c).Length;
                double C2 = (pbc-c).Length;
                double D1 = (pda-d).Length;
                double D2 = (pcd-d).Length;
                List<Vector3d> la = new List<Vector3d>(); List<Vector3d> lb = new List<Vector3d>(); List<Vector3d> lc = new List<Vector3d>(); List<Vector3d> ld = new List<Vector3d>();
                if (A1 <= A2) { la.Add(a); la.Add(pab); la.Add(b); }
                else { la.Add(a); la.Add(pda); la.Add(pbc); la.Add(b); }
                if (B1 <= B2) { lb.Add(b); lb.Add(pbc); lb.Add(c); }
                else { lb.Add(b); lb.Add(pab); lb.Add(pcd); lb.Add(c); }
                if (C1 <= C2) { lc.Add(c); lc.Add(pcd); lc.Add(d); }
                else { lc.Add(c); lc.Add(pbc); lc.Add(pda); lc.Add(d); }
                if (D1 <= D2) { ld.Add(d); ld.Add(pda); ld.Add(a); }
                else { ld.Add(d); ld.Add(pcd); ld.Add(pab); ld.Add(a); }
                IList<List<Vector3d>> labcd = new List<List<Vector3d>>(); labcd.Add(la); labcd.Add(lb); labcd.Add(lc); labcd.Add(ld);
                return labcd;
            }
            Tuple<IList<List<Vector3d>>, List<double>> load_expansion(IList<List<GH_Number>> f_l)
            {
                int n = f_l.Count;
                IList<List<Vector3d>> s = new List<List<Vector3d>>(); List<double> p = new List<double>();
                for (int i = 0; i < n; i++)
                {
                    int n1 = (int)f_l[i][0].Value; int n2 = (int)f_l[i][1].Value; int n3 = (int)f_l[i][2].Value; int n4 = (int)f_l[i][3].Value;
                    p.Add(f_l[i][4].Value); p.Add(f_l[i][4].Value); p.Add(f_l[i][4].Value);
                    if (n4 >= 0)
                    {
                        Vector3d a = new Vector3d(r[n1][0].Value, r[n1][1].Value, r[n1][2].Value);
                        Vector3d b = new Vector3d(r[n2][0].Value, r[n2][1].Value, r[n2][2].Value);
                        Vector3d c = new Vector3d(r[n3][0].Value, r[n3][1].Value, r[n3][2].Value);
                        Vector3d d = new Vector3d(r[n4][0].Value, r[n4][1].Value, r[n4][2].Value);
                        IList<List<Vector3d>> labcd = calc_4cross_points(a, b, c, d);
                        s.Add(labcd[0]); s.Add(labcd[1]); s.Add(labcd[2]); s.Add(labcd[3]); p.Add(f_l[i][4].Value);
                    }
                    else
                    {
                        Vector3d a = new Vector3d(r[n1][0].Value, r[n1][1].Value, r[n1][2].Value);
                        Vector3d b = new Vector3d(r[n2][0].Value, r[n2][1].Value, r[n2][2].Value);
                        Vector3d c = new Vector3d(r[n3][0].Value, r[n3][1].Value, r[n3][2].Value);
                        Vector3d center = tri_center(a, b, c);
                        List<Vector3d> la = new List<Vector3d>(); List<Vector3d> lb = new List<Vector3d>(); List<Vector3d> lc = new List<Vector3d>();
                        la.Add(a); la.Add(center); la.Add(b); lb.Add(b); lb.Add(center); lb.Add(c); lc.Add(c); lc.Add(center); lc.Add(a);
                        s.Add(la); s.Add(lb); s.Add(lc);
                    }
                }
                return Tuple.Create(s, p);
            }
            Tuple<IList<List<Vector3d>>, List<double>> load_expansion2(IList<List<GH_Number>> w_l)
            {
                int n = w_l.Count;
                IList<List<Vector3d>> s = new List<List<Vector3d>>(); List<double> p = new List<double>();
                for (int i = 0; i < n; i++)
                {
                    int n1 = (int)w_l[i][0].Value; int n2 = (int)w_l[i][1].Value; int n3 = (int)w_l[i][2].Value; int n4 = (int)w_l[i][3].Value;
                    p.Add(w_l[i][4].Value); p.Add(w_l[i][4].Value);
                    if (n4 >= 0)
                    {
                        Vector3d a = new Vector3d(r[n1][0].Value, r[n1][1].Value, r[n1][2].Value);
                        Vector3d b = new Vector3d(r[n2][0].Value, r[n2][1].Value, r[n2][2].Value);
                        Vector3d c = new Vector3d(r[n3][0].Value, r[n3][1].Value, r[n3][2].Value);
                        Vector3d d = new Vector3d(r[n4][0].Value, r[n4][1].Value, r[n4][2].Value);
                        var e = (a + d) / 2.0; var f = (b + c) / 2.0;
                        var la = new List<Vector3d>(); var lb = new List<Vector3d>();
                        la.Add(a); la.Add(e); la.Add(f); la.Add(b); lb.Add(d); lb.Add(e); lb.Add(f); lb.Add(c);
                        s.Add(la); s.Add(lb);
                    }
                    else
                    {
                        Vector3d a = new Vector3d(r[n1][0].Value, r[n1][1].Value, r[n1][2].Value);
                        Vector3d b = new Vector3d(r[n2][0].Value, r[n2][1].Value, r[n2][2].Value);
                        Vector3d c = new Vector3d(r[n3][0].Value, r[n3][1].Value, r[n3][2].Value);
                        var d = (a + b) / 2.0;
                        var la = new List<Vector3d>(); var lb = new List<Vector3d>();
                        la.Add(a); la.Add(c); la.Add(d); lb.Add(d); lb.Add(c); lb.Add(b);
                        s.Add(la); s.Add(lb);
                    }
                }
                return Tuple.Create(s, p);
            }
            Tuple<List<Tuple<int, List<Vector3d>>>, List<double>> split_f_load(IList<List<Vector3d>> s, List<double> prs)
            {
                var ex_load = new List<Tuple<int, List<Vector3d>>>(); var pressure = new List<double>();
                var si = new List<List<int>>(); var sv = new List<List<Vector3d>>();
                for (int i = 0; i < s.Count; i++)
                {
                    var s1 = s[i][0]; var s2 = s[i][s[i].Count - 1]; var ls = (s2 - s1).Length; var vec = (s2 - s1) / ls;//面荷重展開図形の接する辺
                    var se = new List<int>();
                    for (int e = 0; e < ij.Count; e++)
                    {
                        int n1 = (int)ij[e][0].Value; int n2 = (int)ij[e][1].Value;
                        var r1 = new Vector3d(r[n1][0].Value, r[n1][1].Value, r[n1][2].Value); var r2 = new Vector3d(r[n2][0].Value, r[n2][1].Value, r[n2][2].Value);
                        var l_e = lgh[e]; var vec_e = (r2 - r1) / l_e;
                        var l1 = (s1 - r1).Length; var l2 = (s1 - r2).Length; var l3 = (s2 - r1).Length; var l4 = (s2 - r2).Length;
                        if ((vec - vec_e).Length < acc || (vec + vec_e).Length < acc)
                        {
                            if (ls >= Math.Max(Math.Max(l1, l2), Math.Max(l3, l4))) { se.Add(e); }//辺上に要素があれば要素番号を追加
                        }
                    }
                    si.Add(se); var sj = new List<Vector3d>(); sj.Add(s[i][0]);
                    for (int j = 1; j < s[i].Count - 1; j++)
                    {
                        var p = s[i][j] - s[i][0];
                        var x = s[i][0] + (vec * Vector3d.Multiply(vec, p));
                        sj.Add(x);
                    }
                    sj.Add(s[i][s[i].Count - 1]);
                    sv.Add(sj);
                    for (int k = 0; k < si[i].Count; k++)
                    {
                        var ex_l = new List<Vector3d>();
                        int e = si[i][k];
                        int n1 = (int)ij[e][0].Value; int n2 = (int)ij[e][1].Value;
                        var r1 = new Vector3d(r[n1][0].Value, r[n1][1].Value, r[n1][2].Value); var r2 = new Vector3d(r[n2][0].Value, r[n2][1].Value, r[n2][2].Value); var l_e = lgh[e];
                        var vec_e = (r2 - r1) / l_e; var direction = '+';
                        if ((vec - vec_e).Length > acc)//辺と要素が逆向き
                        {
                            direction = '-'; r1 = new Vector3d(r[n2][0].Value, r[n2][1].Value, r[n2][2].Value); r2 = new Vector3d(r[n1][0].Value, r[n1][1].Value, r[n1][2].Value);
                            vec_e = (r2 - r1) / l_e;
                        }
                        for (int j = 0; j < sv[i].Count; j++)
                        {
                            var p = sv[i][j];
                            var l_p = (p - r1).Length; var vec_p = new Vector3d(0, 0, 0);
                            if (l_p > acc) { vec_p = (p - r1) / l_p; }
                            if ((vec_e - vec_p).Length < acc)//#i点より奥にp
                            {
                                if ((p - r1).Length < acc) { ex_l.Add(s[i][j]); }//頂点とiorj点一致
                                else if (l_e >= l_p) { ex_l.Add(s[i][j]); }//i点とj点の間にp
                                else//j点より奥にp
                                {
                                    var p0 = sv[i][j - 1]; var l_pp = (p - p0).Length; var l_p2 = (p0 - r2).Length;
                                    ex_l.Add(s[i][j - 1] + (s[i][j] - s[i][j - 1]) * l_p2 / l_pp); ex_l.Add(r2);
                                    break;
                                }
                            }
                            else//i点より手前にp
                            {
                                var p1 = sv[i][(int)Math.Min(sv[i].Count - 1, j + 1)]; var l_p1 = (p1 - r1).Length;
                                var vec_p1 = (p1 - r1) / l_p1;
                                if ((vec_e - vec_p1).Length < acc && (vec_e - vec_p).Length > acc)
                                {
                                    ex_l.Add(r1); var l_pp = (p1 - p).Length; var point = s[i][j] + (s[i][j + 1] - s[i][j]) * l_p / l_pp;
                                    if ((r1 - point).Length > acc) { ex_l.Add(point); }
                                }
                            }
                        }
                        if (direction == '-') { ex_l.Reverse(); }
                        ex_load.Add(new Tuple<int, List<Vector3d>>(e, ex_l));
                        pressure.Add(prs[i]);
                    }
                }
                return Tuple.Create(ex_load, pressure);
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
            Matrix transmatrix(double l, double lx, double ly, double lz, double a)
            {
                lx /= l; double mx = ly / l; double nx = lz / l; a = a * Math.PI / 180.0;
                double my; var ny = 0.0; double mz; double nz = 0.0;
                if (lx == 0.0 && ly == 0.0)
                {
                    ly = nx * Math.Cos(a); my = Math.Sin(a);
                    lz = -nx * Math.Sin(a); mz = Math.Cos(a);
                }
                else
                {
                    var ll = Math.Sqrt(Math.Pow(lx, 2) + Math.Pow(mx, 2));
                    ly = -mx * Math.Cos(a) / ll - nx * lx * Math.Sin(a) / ll;
                    my = lx * Math.Cos(a) / ll - nx * mx * Math.Sin(a) / ll;
                    ny = Math.Sin(a) * ll;
                    lz = mx * Math.Sin(a) / ll - nx * lx * Math.Cos(a) / ll;
                    mz = -lx * Math.Sin(a) / ll - nx * mx * Math.Cos(a) / ll;
                    nz = Math.Cos(a) * ll;
                }
                var tr = new Matrix(12, 12);
                tr[0, 0] = lx; tr[0, 1] = mx; tr[0, 2] = nx;
                tr[1, 0] = ly; tr[1, 1] = my; tr[1, 2] = ny;
                tr[2, 0] = lz; tr[2, 1] = mz; tr[2, 2] = nz;
                tr[3, 3] = lx; tr[3, 4] = mx; tr[3, 5] = nx;
                tr[4, 3] = ly; tr[4, 4] = my; tr[4, 5] = ny;
                tr[5, 3] = lz; tr[5, 4] = mz; tr[5, 5] = nz;
                tr[6, 6] = lx; tr[6, 7] = mx; tr[6, 8] = nx;
                tr[7, 6] = ly; tr[7, 7] = my; tr[7, 8] = ny;
                tr[8, 6] = lz; tr[8, 7] = mz; tr[8, 8] = nz;
                tr[9, 9] = lx; tr[9, 10] = mx; tr[9, 11] = nx;
                tr[10, 9] = ly; tr[10, 10] = my; tr[10, 11] = ny;
                tr[11, 9] = lz; tr[11, 10] = mz; tr[11, 11] = nz;
                return tr;
            }
            Tuple<List<List<double>>, List<List<double>>> equivalentS(List<Tuple<int, List<Vector3d>>> s, List<double> p)
            {
                var f_l = new List<List<double>>(); var f = new List<List<double>>();
                for (int i = 0; i < s.Count; i++)
                {
                    var ss =new List<Vector3d>();
                    for (int j = 0; j < s[i].Item2.Count; j++)
                    {
                        if (j <= 1) { ss.Add(s[i].Item2[j]); }
                        else if((s[i].Item2[j-2]- s[i].Item2[j]).Length > 1e-8 && (s[i].Item2[j - 1] - s[i].Item2[j]).Length > 1e-8) { ss.Add(s[i].Item2[j]); }
                    }
                    var fe = new Matrix(12, 1); var fc = new Matrix(6, 1);
                    int el = s[i].Item1; double l_e = lgh[el]; int n1 = (int)ij[el][0].Value; int n2 = (int)ij[el][1].Value;
                    double lx = r[n2][0].Value - r[n1][0].Value; double ly = r[n2][1].Value - r[n1][1].Value; double lz = r[n2][2].Value - r[n1][2].Value;
                    double sn = lz / l_e; double cs = Math.Sqrt(Math.Pow(lx, 2) + Math.Pow(ly, 2)) / l_e;
                    if (ss.Count == 3)
                    {
                        var a = ss[0]; var b = ss[1]; var c = ss[2];
                        var p1 = (Vector3d.CrossProduct(b - a, c - a) / (c - a).Length).Length;
                        var p2 = (b - a) * (c - a) / (c - a).Length;
                        fe[2, 0] = p1 * (7 * Math.Pow(l_e, 3) - 3 * Math.Pow(l_e, 2) * p2 - 3 * l_e * Math.Pow(p2, 2) + 2 * Math.Pow(p2, 3)) / (20 * Math.Pow(l_e, 2));
                        fe[4, 0] = -p1 * (3 * Math.Pow(l_e, 3) + 3 * Math.Pow(l_e, 2) * p2 - 7 * l_e * Math.Pow(p2, 2) + 3 * Math.Pow(p2, 3)) / (60 * l_e);
                        fe[8, 0] = p1 * (3 * Math.Pow(l_e, 3) + 3 * Math.Pow(l_e, 2) * p2 + 3 * l_e * Math.Pow(p2, 2) - 2 * Math.Pow(p2, 3)) / (20 * Math.Pow(l_e, 2));
                        fe[10, 0] = Math.Pow(l_e, 2) * p1 / 30.0 + l_e * p1 * p2 / 30.0 + p1 * Math.Pow(p2, 2) / 30.0 - p1 * Math.Pow(p2, 3) / (20 * l_e);
                        if (l_e / 2.0 < p2)
                        {
                            fc[2, 0] = fe[2, 0] - Math.Pow(l_e, 2) * p1 / (8 * p2);
                            fc[4, 0] = -(fe[4, 0] - Math.Pow(l_e, 3) * p1 / (48 * p2) + fe[2, 0] * l_e / 2.0);
                        }
                        else
                        {
                            fc[2, 0] = fe[2, 0] - l_e * p1 * (-3 * l_e + 4 * p2) / (8.0 * (-l_e + p2));
                            fc[4, 0] = -(fe[4, 0] - l_e * p1 * (5 * Math.Pow(l_e, 2) - 12 * l_e * p2 + 8 * Math.Pow(p2, 2)) / (48 * l_e - 48 * p2) + fe[2, 0] * l_e / 2.0);
                        }
                    }
                    else if (ss.Count == 4)
                    {
                        var a = ss[0]; var b = ss[1]; var c = ss[2]; var d = ss[3];
                        var p1 = (Vector3d.CrossProduct(b - a, d - a) / (d - a).Length).Length;
                        var p2 = (b - a) * (d - a) / (d - a).Length;
                        var p3 = (Vector3d.CrossProduct(c - d, a - d) / (a - d).Length).Length;
                        var p4 = (c - d) * (a - d) / (a - d).Length;
                        fe[2, 0] = (7 * Math.Pow(l_e, 4) * p1 + 3 * Math.Pow(l_e, 4) * p3 - 3 * Math.Pow(l_e, 3) * p1 * p2 - 3 * Math.Pow(l_e, 3) * p1 * p4 - 7 * Math.Pow(l_e, 3) * p2 * p3 + 3 * Math.Pow(l_e, 3) * p3 * p4 - 3 * Math.Pow(l_e, 2) * p1 * Math.Pow(p2, 2) + 4 * Math.Pow(l_e, 2) * p1 * p2 * p4 - 3 * Math.Pow(l_e, 2) * p1 * Math.Pow(p4, 2) + 3 * Math.Pow(l_e, 2) * Math.Pow(p2, 2) * p3 - 4 * Math.Pow(l_e, 2) * p2 * p3 * p4 + 3 * Math.Pow(l_e, 2) * p3 * Math.Pow(p4, 2) + 2 * l_e * p1 * Math.Pow(p2, 3) + l_e * p1 * Math.Pow(p2, 2) * p4 + l_e * p1 * p2 * Math.Pow(p4, 2) - 3 * l_e * p1 * Math.Pow(p4, 3) + 3 * l_e * Math.Pow(p2, 3) * p3 - l_e * Math.Pow(p2, 2) * p3 * p4 - l_e * p2 * p3 * Math.Pow(p4, 2) - 2 * l_e * p3 * Math.Pow(p4, 3) - 2 * p1 * Math.Pow(p2, 3) * p4 + 2 * p1 * Math.Pow(p2, 2) * Math.Pow(p4, 2) - 2 * p1 * p2 * Math.Pow(p4, 3) + 2 * p1 * Math.Pow(p4, 4) - 2 * Math.Pow(p2, 4) * p3 + 2 * Math.Pow(p2, 3) * p3 * p4 - 2 * Math.Pow(p2, 2) * p3 * Math.Pow(p4, 2) + 2 * p2 * p3 * Math.Pow(p4, 3)) / (20 * Math.Pow(l_e, 3));
                        fe[4, 0] = (-3 * Math.Pow(l_e, 4) * p1 - 2 * Math.Pow(l_e, 4) * p3 - 3 * Math.Pow(l_e, 3) * p1 * p2 + 2 * Math.Pow(l_e, 3) * p1 * p4 + 3 * Math.Pow(l_e, 3) * p2 * p3 - 2 * Math.Pow(l_e, 3) * p3 * p4 + 7 * Math.Pow(l_e, 2) * p1 * Math.Pow(p2, 2) - Math.Pow(l_e, 2) * p1 * p2 * p4 + 2 * Math.Pow(l_e, 2) * p1 * Math.Pow(p4, 2) + 3 * Math.Pow(l_e, 2) * Math.Pow(p2, 2) * p3 + Math.Pow(l_e, 2) * p2 * p3 * p4 - 2 * Math.Pow(l_e, 2) * p3 * Math.Pow(p4, 2) - 3 * l_e * p1 * Math.Pow(p2, 3) - 4 * l_e * p1 * Math.Pow(p2, 2) * p4 + l_e * p1 * p2 * Math.Pow(p4, 2) + 2 * l_e * p1 * Math.Pow(p4, 3) - 7 * l_e * Math.Pow(p2, 3) * p3 + 4 * l_e * Math.Pow(p2, 2) * p3 * p4 - l_e * p2 * p3 * Math.Pow(p4, 2) + 3 * l_e * p3 * Math.Pow(p4, 3) + 3 * p1 * Math.Pow(p2, 3) * p4 - 3 * p1 * Math.Pow(p2, 2) * Math.Pow(p4, 2) + 3 * p1 * p2 * Math.Pow(p4, 3) - 3 * p1 * Math.Pow(p4, 4) + 3 * Math.Pow(p2, 4) * p3 - 3 * Math.Pow(p2, 3) * p3 * p4 + 3 * Math.Pow(p2, 2) * p3 * Math.Pow(p4, 2) - 3 * p2 * p3 * Math.Pow(p4, 3)) / (60 * Math.Pow(l_e, 2));
                        if (p4 > 1e-10)
                        {
                            if (Math.Abs(-l_e + p2 + p4) < 1e-10)
                            {
                                fe[8, 0] = (3 * Math.Pow(l_e, 5) * p3 + 60 * Math.Pow(l_e, 2) * p1 * Math.Pow(p2, 2) * p4 - 20 * Math.Pow(l_e, 2) * Math.Pow(p2, 3) * p3 - 85 * l_e * p1 * Math.Pow(p2, 3) * p4 - 60 * l_e * p1 * Math.Pow(p2, 2) * Math.Pow(p4, 2) + 25 * l_e * Math.Pow(p2, 4) * p3 + 32 * p1 * Math.Pow(p2, 4) * p4 + 40 * p1 * Math.Pow(p2, 3) * Math.Pow(p4, 2) - 8 * Math.Pow(p2, 5) * p3) / (20 * Math.Pow(l_e, 3) * p4);
                                fe[10, 0] = (2 * Math.Pow(l_e, 5) * p3 + 60 * Math.Pow(l_e, 2) * p1 * Math.Pow(p2, 2) * p4 - 20 * Math.Pow(l_e, 2) * Math.Pow(p2, 3) * p3 - 105 * l_e * p1 * Math.Pow(p2, 3) * p4 - 60 * l_e * p1 * Math.Pow(p2, 2) * Math.Pow(p4, 2) + 30 * l_e * Math.Pow(p2, 4) * p3 + 48 * p1 * Math.Pow(p2, 4) * p4 + 60 * p1 * Math.Pow(p2, 3) * Math.Pow(p4, 2) - 12 * Math.Pow(p2, 5) * p3) / (60 * Math.Pow(l_e, 2) * p4);
                            }
                            else
                            {
                                fe[8, 0] = (3 * Math.Pow(l_e, 5) * p3 * (-l_e + p2 + p4) - 20 * Math.Pow(l_e, 2) * p3 * Math.Pow((l_e - p4), 3) * (-l_e + p2 + p4) + 15 * l_e * p1 * Math.Pow(p2, 3) * p4 * (-l_e + p2 + p4) + 25 * l_e * p3 * Math.Pow((l_e - p4), 4) * (-l_e + p2 + p4) + 20 * l_e * p4 * (Math.Pow(p2, 3) * (l_e * p1 - p1 * p4 - p2 * p3) + Math.Pow((l_e - p4), 3) * (-l_e * p1 + p1 * p4 + p2 * p3)) - 8 * p1 * Math.Pow(p2, 4) * p4 * (-l_e + p2 + p4) - 8 * p3 * Math.Pow((l_e - p4), 5) * (-l_e + p2 + p4) + 5 * p4 * (Math.Pow(p2, 4) * (-5 * l_e * p1 + 3 * l_e * p3 + 2 * p1 * p4 + 2 * p2 * p3) + Math.Pow((l_e - p4), 4) * (5 * l_e * p1 - 3 * l_e * p3 - 2 * p1 * p4 - 2 * p2 * p3)) + 8 * p4 * (Math.Pow(p2, 5) * (p1 - p3) + Math.Pow((l_e - p4), 5) * (-p1 + p3))) / (20 * Math.Pow(l_e, 3) * p4 * (-l_e + p2 + p4));
                                fe[10, 0] = (Math.Pow(l_e, 2) * p3 * (Math.Pow(l_e, 3) - 10 * Math.Pow((l_e - p4), 3)) * (-l_e + p2 + p4) / 30.0 + l_e * p1 * Math.Pow(p2, 3) * p4 * (-l_e + p2 + p4) / 4.0 + l_e * p3 * Math.Pow((l_e - p4), 4) * (-l_e + p2 + p4) / 2.0 + l_e * p4 * (Math.Pow(p2, 3) * (l_e * p1 - p1 * p4 - p2 * p3) + Math.Pow((l_e - p4), 3) * (-l_e * p1 + p1 * p4 + p2 * p3)) / 3.0 - p1 * Math.Pow(p2, 4) * p4 * (-l_e + p2 + p4) / 5.0 - p3 * Math.Pow((l_e - p4), 5) * (-l_e + p2 + p4) / 5.0 + p4 * (Math.Pow(p2, 4) * (-2 * l_e * p1 + l_e * p3 + p1 * p4 + p2 * p3) + Math.Pow((l_e - p4), 4) * (2 * l_e * p1 - l_e * p3 - p1 * p4 - p2 * p3)) / 4.0 + p4 * (Math.Pow(p2, 5) * (p1 - p3) + Math.Pow((l_e - p4), 5) * (-p1 + p3)) / 5.0) / (Math.Pow(l_e, 2) * p4 * (-l_e + p2 + p4));
                            }
                        }
                        else
                        {
                            fe[8, 0] = 3 * l_e * p1 / 20.0 + 7 * l_e * p3 / 20.0 + 3 * p1 * p2 / 20.0 - 3 * p2 * p3 / 20.0 + 3 * p1 * Math.Pow(p2, 2) / (20 * l_e) - 3 * Math.Pow(p2, 2) * p3 / (20 * l_e) - p1 * Math.Pow(p2, 3) / (10 * Math.Pow(l_e, 2)) - 3 * Math.Pow(p2, 3) * p3 / (20 * Math.Pow(l_e, 2)) + Math.Pow(p2, 4) * p3 / (10 * Math.Pow(l_e, 3));
                            fe[10, 0] = Math.Pow(l_e, 2) * p1 / 30.0 + Math.Pow(l_e, 2) * p3 / 20.0 + l_e * p1 * p2 / 30.0 - l_e * p2 * p3 / 30 + p1 * Math.Pow(p2, 2) / 30.0 - Math.Pow(p2, 2) * p3 / 30.0 - p1 * Math.Pow(p2, 3) / (20 * l_e) - Math.Pow(p2, 3) * p3 / (30 * l_e) + Math.Pow(p2, 4) * p3 / (20 * Math.Pow(l_e, 2));
                        }
                        if (l_e / 2.0 < p2 + 1.0e-10)
                        {
                            fc[2, 0] = fe[2, 0] - Math.Pow(l_e, 2) * p1 / (8.0 * p2);
                            fc[4, 0] = -(fe[4, 0] - Math.Pow(l_e, 3) * p1 / (48.0 * p2) + fe[2, 0] * l_e / 2.0);
                        }
                        else if (l_e / 2.0 < l_e - p4)
                        {
                            if (Math.Abs(-l_e + p2 + p4) < 1e-10) { Rhino.RhinoApp.Write("***warning s5-1***"); }
                            fc[2, 0] = fe[2, 0] - (-3 * Math.Pow(l_e, 2) * p1 / 8.0 - Math.Pow(l_e, 2) * p3 / 8.0 + l_e * p1 * p2 / 2.0 + l_e * p1 * p4 / 2.0 + l_e * p2 * p3 / 2.0 - p1 * p2 * p4 / 2.0 - Math.Pow(p2, 2) * p3 / 2.0) / (-l_e + p2 + p4);
                            fc[4, 0] = -(fe[4, 0] - (-5 * Math.Pow(l_e, 3) * p1 / 48.0 - Math.Pow(l_e, 3) * p3 / 48.0 + Math.Pow(l_e, 2) * p1 * p2 / 4.0 + Math.Pow(l_e, 2) * p1 * p4 / 8.0 + Math.Pow(l_e, 2) * p2 * p3 / 8.0 - l_e * p1 * Math.Pow(p2, 2) / 6.0 - l_e * p1 * p2 * p4 / 4.0 - l_e * Math.Pow(p2, 2) * p3 / 4.0 + p1 * Math.Pow(p2, 2) * p4 / 6.0 + Math.Pow(p2, 3) * p3 / 6.0) / (-l_e + p2 + p4) + fe[2, 0] * l_e / 2.0);
                        }
                        else
                        {
                            if (p4 < 1e-10) { Rhino.RhinoApp.Write("***warning s5-2***"); }
                            fc[2, 0] = fe[8, 0] - l_e / 2.0 * p3 * (l_e / 2.0) / p4 / 2.0;
                            fc[4, 0] = -(-fe[10, 0] - (l_e / 2.0 * p3 * (l_e / 2.0) / p4 / 2.0) * l_e / 6.0 + fe[8, 0] * l_e / 2.0);
                        }
                    }
                    else if (ss.Count == 5)
                    {
                        var a = ss[0]; var b = ss[1]; var c = ss[2]; var d = ss[3]; var e = ss[4];
                        var p1 = (Vector3d.CrossProduct(b - a, e - a) / (e - a).Length).Length;
                        var p2 = (b - a) * (e - a) / (e - a).Length;
                        var p3 = (Vector3d.CrossProduct(d - e, a - e) / (a - e).Length).Length;
                        var p4 = (d - e) * (a - e) / (a - e).Length;
                        var p5 = (Vector3d.CrossProduct(c - a, e - a) / (e - a).Length).Length;
                        var p6 = (c - a) * (e - a) / (e - a).Length - p2;
                        if (p2 > 1e-10 && p4 > 1e-10)
                        {
                            fe[2, 0] = (10 * Math.Pow(l_e, 3) * p1 * p2 * p4 * p6 * (-l_e + p2 + p4 + p6) + Math.Pow(l_e, 3) * p3 * p6 * (7 * Math.Pow(l_e, 2) - 20 * l_e * (l_e - p4) + 10 * Math.Pow((l_e - p4), 2)) * (-l_e + p2 + p4 + p6) + 10 * Math.Pow(l_e, 3) * p4 * p6 * (Math.Pow((l_e - p4), 2) * (-p3 + p5) + Math.Pow((p2 + p6), 2) * (p3 - p5)) + 20 * Math.Pow(l_e, 3) * p4 * p6 * (l_e - p2 - p4 - p6) * (-l_e * p5 + p2 * p3 + p3 * p6 + p4 * p5) + 10 * Math.Pow(l_e, 3) * p4 * (-l_e + p2 + p4 + p6) * (Math.Pow(p2, 2) * (p1 - p5) - 2 * p2 * (p1 * p2 + p1 * p6 - p2 * p5) + (-p1 + p5) * Math.Pow((p2 + p6), 2) + 2 * (p2 + p6) * (p1 * p2 + p1 * p6 - p2 * p5)) + 20 * Math.Pow(l_e, 2) * p3 * p6 * Math.Pow((l_e - p4), 3) * (-l_e + p2 + p4 + p6) - 15 * l_e * p1 * Math.Pow(p2, 3) * p4 * p6 * (-l_e + p2 + p4 + p6) - 25 * l_e * p3 * p6 * Math.Pow((l_e - p4), 4) * (-l_e + p2 + p4 + p6) + 20 * l_e * p4 * p6 * (Math.Pow((l_e - p4), 3) * (l_e * p5 - p2 * p3 - p3 * p6 - p4 * p5) + Math.Pow((p2 + p6), 3) * (-l_e * p5 + p2 * p3 + p3 * p6 + p4 * p5)) + 20 * l_e * p4 * (Math.Pow(p2, 3) * (p1 * p2 + p1 * p6 - p2 * p5) + Math.Pow((p2 + p6), 3) * (-p1 * p2 - p1 * p6 + p2 * p5)) * (-l_e + p2 + p4 + p6) + 8 * p1 * Math.Pow(p2, 4) * p4 * p6 * (-l_e + p2 + p4 + p6) + 8 * p3 * p6 * Math.Pow((l_e - p4), 5) * (-l_e + p2 + p4 + p6) + 5 * p4 * p6 * (Math.Pow((l_e - p4), 4) * (3 * l_e * p3 - 5 * l_e * p5 + 2 * p2 * p3 + 2 * p3 * p6 + 2 * p4 * p5) + Math.Pow((p2 + p6), 4) * (-3 * l_e * p3 + 5 * l_e * p5 - 2 * p2 * p3 - 2 * p3 * p6 - 2 * p4 * p5)) + 8 * p4 * p6 * (Math.Pow((l_e - p4), 5) * (-p3 + p5) + Math.Pow((p2 + p6), 5) * (p3 - p5)) + p4 * (-l_e + p2 + p4 + p6) * (8 * Math.Pow(p2, 5) * (p1 - p5) + 5 * Math.Pow(p2, 4) * (-3 * l_e * p1 + 3 * l_e * p5 - 2 * p1 * p2 - 2 * p1 * p6 + 2 * p2 * p5) + 8 * (-p1 + p5) * Math.Pow((p2 + p6), 5) + 5 * Math.Pow((p2 + p6), 4) * (3 * l_e * p1 - 3 * l_e * p5 + 2 * p1 * p2 + 2 * p1 * p6 - 2 * p2 * p5))) / (20 * Math.Pow(l_e, 3) * p4 * p6 * (-l_e + p2 + p4 + p6));
                            fe[4, 0] = (-20 * Math.Pow(l_e, 2) * p1 * Math.Pow(p2, 2) * p4 * p6 * (-l_e + p2 + p4 + p6) + 3 * Math.Pow(l_e, 2) * p3 * p6 * (-Math.Pow(l_e, 3) + 10 * l_e * Math.Pow((l_e - p4), 2) - 20 * Math.Pow((l_e - p4), 3)) * (-l_e + p2 + p4 + p6) + 30 * Math.Pow(l_e, 2) * p4 * p6 * (Math.Pow((l_e - p4), 2) * (l_e * p5 - p2 * p3 - p3 * p6 - p4 * p5) + Math.Pow((p2 + p6), 2) * (-l_e * p5 + p2 * p3 + p3 * p6 + p4 * p5)) + 30 * Math.Pow(l_e, 2) * p4 * (Math.Pow(p2, 2) * (p1 * p2 + p1 * p6 - p2 * p5) + Math.Pow((p2 + p6), 2) * (-p1 * p2 - p1 * p6 + p2 * p5)) * (-l_e + p2 + p4 + p6) + 30 * l_e * p1 * Math.Pow(p2, 3) * p4 * p6 * (-l_e + p2 + p4 + p6) + 45 * l_e * p3 * p6 * Math.Pow((l_e - p4), 4) * (-l_e + p2 + p4 + p6) + 20 * l_e * p4 * p6 * (Math.Pow((l_e - p4), 3) * (l_e * p3 - 3 * l_e * p5 + 2 * p2 * p3 + 2 * p3 * p6 + 2 * p4 * p5) + Math.Pow((p2 + p6), 3) * (-l_e * p3 + 3 * l_e * p5 - 2 * p2 * p3 - 2 * p3 * p6 - 2 * p4 * p5)) + 20 * l_e * p4 * (Math.Pow(p2, 3) * (-l_e * p1 + l_e * p5 - 2 * p1 * p2 - 2 * p1 * p6 + 2 * p2 * p5) + Math.Pow((p2 + p6), 3) * (l_e * p1 - l_e * p5 + 2 * p1 * p2 + 2 * p1 * p6 - 2 * p2 * p5)) * (-l_e + p2 + p4 + p6) - 12 * p1 * Math.Pow(p2, 4) * p4 * p6 * (-l_e + p2 + p4 + p6) - 12 * p3 * p6 * Math.Pow((l_e - p4), 5) * (-l_e + p2 + p4 + p6) + 15 * p4 * p6 * (Math.Pow((l_e - p4), 4) * (-2 * l_e * p3 + 3 * l_e * p5 - p2 * p3 - p3 * p6 - p4 * p5) + Math.Pow((p2 + p6), 4) * (2 * l_e * p3 - 3 * l_e * p5 + p2 * p3 + p3 * p6 + p4 * p5)) + 12 * p4 * p6 * (Math.Pow((l_e - p4), 5) * (p3 - p5) + Math.Pow((p2 + p6), 5) * (-p3 + p5)) + 3 * p4 * (-l_e + p2 + p4 + p6) * (4 * Math.Pow(p2, 5) * (-p1 + p5) + 5 * Math.Pow(p2, 4) * (2 * l_e * p1 - 2 * l_e * p5 + p1 * p2 + p1 * p6 - p2 * p5) + 4 * (p1 - p5) * Math.Pow((p2 + p6), 5) + 5 * Math.Pow((p2 + p6), 4) * (-2 * l_e * p1 + 2 * l_e * p5 - p1 * p2 - p1 * p6 + p2 * p5))) / (60 * Math.Pow(l_e, 2) * p4 * p6 * (-l_e + p2 + p4 + p6));
                            fe[8, 0] = (3 * Math.Pow(l_e, 5) * p3 * p6 * (-l_e + p2 + p4 + p6) - 20 * Math.Pow(l_e, 2) * p3 * p6 * Math.Pow((l_e - p4), 3) * (-l_e + p2 + p4 + p6) + 15 * l_e * p1 * Math.Pow(p2, 3) * p4 * p6 * (-l_e + p2 + p4 + p6) + 25 * l_e * p3 * p6 * Math.Pow((l_e - p4), 4) * (-l_e + p2 + p4 + p6) + 20 * l_e * p4 * p6 * (Math.Pow((l_e - p4), 3) * (-l_e * p5 + p2 * p3 + p3 * p6 + p4 * p5) + Math.Pow((p2 + p6), 3) * (l_e * p5 - p2 * p3 - p3 * p6 - p4 * p5)) + 20 * l_e * p4 * (Math.Pow(p2, 3) * (-p1 * p2 - p1 * p6 + p2 * p5) + Math.Pow((p2 + p6), 3) * (p1 * p2 + p1 * p6 - p2 * p5)) * (-l_e + p2 + p4 + p6) - 8 * p1 * Math.Pow(p2, 4) * p4 * p6 * (-l_e + p2 + p4 + p6) - 8 * p3 * p6 * Math.Pow((l_e - p4), 5) * (-l_e + p2 + p4 + p6) + 5 * p4 * p6 * (Math.Pow((l_e - p4), 4) * (-3 * l_e * p3 + 5 * l_e * p5 - 2 * p2 * p3 - 2 * p3 * p6 - 2 * p4 * p5) + Math.Pow((p2 + p6), 4) * (3 * l_e * p3 - 5 * l_e * p5 + 2 * p2 * p3 + 2 * p3 * p6 + 2 * p4 * p5)) + 8 * p4 * p6 * (Math.Pow((l_e - p4), 5) * (p3 - p5) + Math.Pow((p2 + p6), 5) * (-p3 + p5)) + p4 * (-l_e + p2 + p4 + p6) * (8 * Math.Pow(p2, 5) * (-p1 + p5) + 5 * Math.Pow(p2, 4) * (3 * l_e * p1 - 3 * l_e * p5 + 2 * p1 * p2 + 2 * p1 * p6 - 2 * p2 * p5) + 8 * (p1 - p5) * Math.Pow((p2 + p6), 5) + 5 * Math.Pow((p2 + p6), 4) * (-3 * l_e * p1 + 3 * l_e * p5 - 2 * p1 * p2 - 2 * p1 * p6 + 2 * p2 * p5))) / (20 * Math.Pow(l_e, 3) * p4 * p6 * (-l_e + p2 + p4 + p6));
                            fe[10, 0] = (2 * Math.Pow(l_e, 2) * p3 * p6 * (Math.Pow(l_e, 3) - 10 * Math.Pow((l_e - p4), 3)) * (-l_e + p2 + p4 + p6) + 15 * l_e * p1 * Math.Pow(p2, 3) * p4 * p6 * (-l_e + p2 + p4 + p6) + 30 * l_e * p3 * p6 * Math.Pow((l_e - p4), 4) * (-l_e + p2 + p4 + p6) + 20 * l_e * p4 * p6 * (Math.Pow((l_e - p4), 3) * (-l_e * p5 + p2 * p3 + p3 * p6 + p4 * p5) + Math.Pow((p2 + p6), 3) * (l_e * p5 - p2 * p3 - p3 * p6 - p4 * p5)) + 20 * l_e * p4 * (Math.Pow(p2, 3) * (-p1 * p2 - p1 * p6 + p2 * p5) + Math.Pow((p2 + p6), 3) * (p1 * p2 + p1 * p6 - p2 * p5)) * (-l_e + p2 + p4 + p6) - 12 * p1 * Math.Pow(p2, 4) * p4 * p6 * (-l_e + p2 + p4 + p6) - 12 * p3 * p6 * Math.Pow((l_e - p4), 5) * (-l_e + p2 + p4 + p6) + 15 * p4 * p6 * (Math.Pow((l_e - p4), 4) * (-l_e * p3 + 2 * l_e * p5 - p2 * p3 - p3 * p6 - p4 * p5) + Math.Pow((p2 + p6), 4) * (l_e * p3 - 2 * l_e * p5 + p2 * p3 + p3 * p6 + p4 * p5)) + 12 * p4 * p6 * (Math.Pow((l_e - p4), 5) * (p3 - p5) + Math.Pow((p2 + p6), 5) * (-p3 + p5)) + 3 * p4 * (-l_e + p2 + p4 + p6) * (4 * Math.Pow(p2, 5) * (-p1 + p5) + 5 * Math.Pow(p2, 4) * (l_e * p1 - l_e * p5 + p1 * p2 + p1 * p6 - p2 * p5) + 4 * (p1 - p5) * Math.Pow((p2 + p6), 5) + 5 * Math.Pow((p2 + p6), 4) * (-l_e * p1 + l_e * p5 - p1 * p2 - p1 * p6 + p2 * p5))) / (60 * Math.Pow(l_e, 2) * p4 * p6 * (-l_e + p2 + p4 + p6));
                        }
                        else if (p2 < 1e-10 && p4 > 1e-10)
                        {
                            fe[2, 0] = (3 * Math.Pow(l_e, 4) * p3 + 7 * Math.Pow(l_e, 4) * p5 + 10 * Math.Pow(l_e, 3) * p1 * p6 + 3 * Math.Pow(l_e, 3) * p3 * p4 - 7 * Math.Pow(l_e, 3) * p3 * p6 - 3 * Math.Pow(l_e, 3) * p4 * p5 - 3 * Math.Pow(l_e, 3) * p5 * p6 + 3 * Math.Pow(l_e, 2) * p3 * Math.Pow(p4, 2) - 4 * Math.Pow(l_e, 2) * p3 * p4 * p6 + 3 * Math.Pow(l_e, 2) * p3 * Math.Pow(p6, 2) - 3 * Math.Pow(l_e, 2) * Math.Pow(p4, 2) * p5 + 4 * Math.Pow(l_e, 2) * p4 * p5 * p6 - 3 * Math.Pow(l_e, 2) * p5 * Math.Pow(p6, 2) - 5 * l_e * p1 * Math.Pow(p6, 3) - 2 * l_e * p3 * Math.Pow(p4, 3) - l_e * p3 * Math.Pow(p4, 2) * p6 - l_e * p3 * p4 * Math.Pow(p6, 2) + 3 * l_e * p3 * Math.Pow(p6, 3) - 3 * l_e * Math.Pow(p4, 3) * p5 + l_e * Math.Pow(p4, 2) * p5 * p6 + l_e * p4 * p5 * Math.Pow(p6, 2) + 2 * l_e * p5 * Math.Pow(p6, 3) + 2 * p1 * Math.Pow(p6, 4) + 2 * p3 * Math.Pow(p4, 3) * p6 - 2 * p3 * Math.Pow(p4, 2) * Math.Pow(p6, 2) + 2 * p3 * p4 * Math.Pow(p6, 3) - 2 * p3 * Math.Pow(p6, 4) + 2 * Math.Pow(p4, 4) * p5 - 2 * Math.Pow(p4, 3) * p5 * p6 + 2 * Math.Pow(p4, 2) * p5 * Math.Pow(p6, 2) - 2 * p4 * p5 * Math.Pow(p6, 3)) / (20 * Math.Pow(l_e, 3));
                            fe[4, 0] = -(2 * Math.Pow(l_e, 4) * p3 + 3 * Math.Pow(l_e, 4) * p5 + 2 * Math.Pow(l_e, 3) * p3 * p4 - 3 * Math.Pow(l_e, 3) * p3 * p6 - 2 * Math.Pow(l_e, 3) * p4 * p5 + 3 * Math.Pow(l_e, 3) * p5 * p6 + 10 * Math.Pow(l_e, 2) * p1 * Math.Pow(p6, 2) + 2 * Math.Pow(l_e, 2) * p3 * Math.Pow(p4, 2) - Math.Pow(l_e, 2) * p3 * p4 * p6 - 3 * Math.Pow(l_e, 2) * p3 * Math.Pow(p6, 2) - 2 * Math.Pow(l_e, 2) * Math.Pow(p4, 2) * p5 + Math.Pow(l_e, 2) * p4 * p5 * p6 - 7 * Math.Pow(l_e, 2) * p5 * Math.Pow(p6, 2) - 10 * l_e * p1 * Math.Pow(p6, 3) - 3 * l_e * p3 * Math.Pow(p4, 3) + l_e * p3 * Math.Pow(p4, 2) * p6 - 4 * l_e * p3 * p4 * Math.Pow(p6, 2) + 7 * l_e * p3 * Math.Pow(p6, 3) - 2 * l_e * Math.Pow(p4, 3) * p5 - l_e * Math.Pow(p4, 2) * p5 * p6 + 4 * l_e * p4 * p5 * Math.Pow(p6, 2) + 3 * l_e * p5 * Math.Pow(p6, 3) + 3 * p1 * Math.Pow(p6, 4) + 3 * p3 * Math.Pow(p4, 3) * p6 - 3 * p3 * Math.Pow(p4, 2) * Math.Pow(p6, 2) + 3 * p3 * p4 * Math.Pow(p6, 3) - 3 * p3 * Math.Pow(p6, 4) + 3 * Math.Pow(p4, 4) * p5 - 3 * Math.Pow(p4, 3) * p5 * p6 + 3 * Math.Pow(p4, 2) * p5 * Math.Pow(p6, 2) - 3 * p4 * p5 * Math.Pow(p6, 3)) / (60 * Math.Pow(l_e, 2));
                            fe[8, 0] = (3 * Math.Pow(l_e, 5) * p3 * (-l_e + p4 + p6) - 20 * Math.Pow(l_e, 2) * p3 * Math.Pow((l_e - p4), 3) * (-l_e + p4 + p6) + 20 * l_e * p1 * p4 * Math.Pow(p6, 3) * (-l_e + p4 + p6) + 25 * l_e * p3 * Math.Pow((l_e - p4), 4) * (-l_e + p4 + p6) + 20 * l_e * p4 * (Math.Pow(p6, 3) * (l_e * p5 - p3 * p6 - p4 * p5) + Math.Pow((l_e - p4), 3) * (-l_e * p5 + p3 * p6 + p4 * p5)) - 8 * p3 * Math.Pow((l_e - p4), 5) * (-l_e + p4 + p6) + p4 * Math.Pow(p6, 3) * (-l_e + p4 + p6) * (-15 * l_e * p1 + 15 * l_e * p5 - 10 * p1 * p6 + 8 * p6 * (p1 - p5)) + 5 * p4 * (Math.Pow(p6, 4) * (3 * l_e * p3 - 5 * l_e * p5 + 2 * p3 * p6 + 2 * p4 * p5) + Math.Pow((l_e - p4), 4) * (-3 * l_e * p3 + 5 * l_e * p5 - 2 * p3 * p6 - 2 * p4 * p5)) + 8 * p4 * (Math.Pow(p6, 5) * (-p3 + p5) + Math.Pow((l_e - p4), 5) * (p3 - p5))) / (20 * Math.Pow(l_e, 3) * p4 * (-l_e + p4 + p6));
                            fe[10, 0] = (Math.Pow(l_e, 2) * p3 * (Math.Pow(l_e, 3) - 10 * Math.Pow((l_e - p4), 3)) * (-l_e + p4 + p6) / 30.0 + l_e * p1 * p4 * Math.Pow(p6, 3) * (-l_e + p4 + p6) / 3.0 + l_e * p3 * Math.Pow((l_e - p4), 4) * (-l_e + p4 + p6) / 2.0 + l_e * p4 * (Math.Pow(p6, 3) * (l_e * p5 - p3 * p6 - p4 * p5) + Math.Pow((l_e - p4), 3) * (-l_e * p5 + p3 * p6 + p4 * p5)) / 3.0 - p3 * Math.Pow((l_e - p4), 5) * (-l_e + p4 + p6) / 5.0 + p4 * Math.Pow(p6, 3) * (-l_e + p4 + p6) * (-5 * l_e * p1 + 5 * l_e * p5 - 5 * p1 * p6 + 4 * p6 * (p1 - p5)) / 20.0 + p4 * (Math.Pow(p6, 4) * (l_e * p3 - 2 * l_e * p5 + p3 * p6 + p4 * p5) + Math.Pow((l_e - p4), 4) * (-l_e * p3 + 2 * l_e * p5 - p3 * p6 - p4 * p5)) / 4.0 + p4 * (Math.Pow(p6, 5) * (-p3 + p5) + Math.Pow((l_e - p4), 5) * (p3 - p5)) / 5.0) / (Math.Pow(l_e, 2) * p4 * (-l_e + p4 + p6));
                        }
                        else if (p2 > 1e-10 && p4 < 1e-10)
                        {
                            fe[2, 0] = (3 * Math.Pow(l_e, 4) * p3 + 7 * Math.Pow(l_e, 4) * p5 + 10 * Math.Pow(l_e, 3) * p1 * p2 + 10 * Math.Pow(l_e, 3) * p1 * p6 - 7 * Math.Pow(l_e, 3) * p2 * p3 - 13 * Math.Pow(l_e, 3) * p2 * p5 - 7 * Math.Pow(l_e, 3) * p3 * p6 - 3 * Math.Pow(l_e, 3) * p5 * p6 + 3 * Math.Pow(l_e, 2) * Math.Pow(p2, 2) * p3 - 3 * Math.Pow(l_e, 2) * Math.Pow(p2, 2) * p5 + 6 * Math.Pow(l_e, 2) * p2 * p3 * p6 - 6 * Math.Pow(l_e, 2) * p2 * p5 * p6 + 3 * Math.Pow(l_e, 2) * p3 * Math.Pow(p6, 2) - 3 * Math.Pow(l_e, 2) * p5 * Math.Pow(p6, 2) - 15 * l_e * p1 * Math.Pow(p2, 3) - 30 * l_e * p1 * Math.Pow(p2, 2) * p6 - 20 * l_e * p1 * p2 * Math.Pow(p6, 2) - 5 * l_e * p1 * Math.Pow(p6, 3) + 3 * l_e * Math.Pow(p2, 3) * p3 + 17 * l_e * Math.Pow(p2, 3) * p5 + 9 * l_e * Math.Pow(p2, 2) * p3 * p6 + 21 * l_e * Math.Pow(p2, 2) * p5 * p6 + 9 * l_e * p2 * p3 * Math.Pow(p6, 2) + 11 * l_e * p2 * p5 * Math.Pow(p6, 2) + 3 * l_e * p3 * Math.Pow(p6, 3) + 2 * l_e * p5 * Math.Pow(p6, 3) + 8 * p1 * Math.Pow(p2, 4) + 20 * p1 * Math.Pow(p2, 3) * p6 + 20 * p1 * Math.Pow(p2, 2) * Math.Pow(p6, 2) + 10 * p1 * p2 * Math.Pow(p6, 3) + 2 * p1 * Math.Pow(p6, 4) - 2 * Math.Pow(p2, 4) * p3 - 8 * Math.Pow(p2, 4) * p5 - 8 * Math.Pow(p2, 3) * p3 * p6 - 12 * Math.Pow(p2, 3) * p5 * p6 - 12 * Math.Pow(p2, 2) * p3 * Math.Pow(p6, 2) - 8 * Math.Pow(p2, 2) * p5 * Math.Pow(p6, 2) - 8 * p2 * p3 * Math.Pow(p6, 3) - 2 * p2 * p5 * Math.Pow(p6, 3) - 2 * p3 * Math.Pow(p6, 4)) / (20 * Math.Pow(l_e, 3));
                            fe[4, 0] = (-2 * Math.Pow(l_e, 4) * p3 - 3 * Math.Pow(l_e, 4) * p5 + 3 * Math.Pow(l_e, 3) * p2 * p3 - 3 * Math.Pow(l_e, 3) * p2 * p5 + 3 * Math.Pow(l_e, 3) * p3 * p6 - 3 * Math.Pow(l_e, 3) * p5 * p6 - 20 * Math.Pow(l_e, 2) * p1 * Math.Pow(p2, 2) - 30 * Math.Pow(l_e, 2) * p1 * p2 * p6 - 10 * Math.Pow(l_e, 2) * p1 * Math.Pow(p6, 2) + 3 * Math.Pow(l_e, 2) * Math.Pow(p2, 2) * p3 + 27 * Math.Pow(l_e, 2) * Math.Pow(p2, 2) * p5 + 6 * Math.Pow(l_e, 2) * p2 * p3 * p6 + 24 * Math.Pow(l_e, 2) * p2 * p5 * p6 + 3 * Math.Pow(l_e, 2) * p3 * Math.Pow(p6, 2) + 7 * Math.Pow(l_e, 2) * p5 * Math.Pow(p6, 2) + 30 * l_e * p1 * Math.Pow(p2, 3) + 60 * l_e * p1 * Math.Pow(p2, 2) * p6 + 40 * l_e * p1 * p2 * Math.Pow(p6, 2) + 10 * l_e * p1 * Math.Pow(p6, 3) - 7 * l_e * Math.Pow(p2, 3) * p3 - 33 * l_e * Math.Pow(p2, 3) * p5 - 21 * l_e * Math.Pow(p2, 2) * p3 * p6 - 39 * l_e * Math.Pow(p2, 2) * p5 * p6 - 21 * l_e * p2 * p3 * Math.Pow(p6, 2) - 19 * l_e * p2 * p5 * Math.Pow(p6, 2) - 7 * l_e * p3 * Math.Pow(p6, 3) - 3 * l_e * p5 * Math.Pow(p6, 3) - 12 * p1 * Math.Pow(p2, 4) - 30 * p1 * Math.Pow(p2, 3) * p6 - 30 * p1 * Math.Pow(p2, 2) * Math.Pow(p6, 2) - 15 * p1 * p2 * Math.Pow(p6, 3) - 3 * p1 * Math.Pow(p6, 4) + 3 * Math.Pow(p2, 4) * p3 + 12 * Math.Pow(p2, 4) * p5 + 12 * Math.Pow(p2, 3) * p3 * p6 + 18 * Math.Pow(p2, 3) * p5 * p6 + 18 * Math.Pow(p2, 2) * p3 * Math.Pow(p6, 2) + 12 * Math.Pow(p2, 2) * p5 * Math.Pow(p6, 2) + 12 * p2 * p3 * Math.Pow(p6, 3) + 3 * p2 * p5 * Math.Pow(p6, 3) + 3 * p3 * Math.Pow(p6, 4)) / (60 * Math.Pow(l_e, 2));
                            fe[8, 0] = (15 * l_e * p1 * Math.Pow(p2, 3) * p6 * (-l_e + p2 + p6) + 20 * l_e * p6 * (Math.Pow(l_e, 3) * (-l_e * p5 + p2 * p3 + p3 * p6) + Math.Pow((p2 + p6), 3) * (l_e * p5 - p2 * p3 - p3 * p6)) + 20 * l_e * (Math.Pow(p2, 3) * (-p1 * p2 - p1 * p6 + p2 * p5) + Math.Pow((p2 + p6), 3) * (p1 * p2 + p1 * p6 - p2 * p5)) * (-l_e + p2 + p6) - 8 * p1 * Math.Pow(p2, 4) * p6 * (-l_e + p2 + p6) + 5 * p6 * (Math.Pow(l_e, 4) * (-3 * l_e * p3 + 5 * l_e * p5 - 2 * p2 * p3 - 2 * p3 * p6) + Math.Pow((p2 + p6), 4) * (3 * l_e * p3 - 5 * l_e * p5 + 2 * p2 * p3 + 2 * p3 * p6)) + 8 * p6 * (Math.Pow(l_e, 5) * (p3 - p5) + Math.Pow((p2 + p6), 5) * (-p3 + p5)) + (-l_e + p2 + p6) * (8 * Math.Pow(p2, 5) * (-p1 + p5) + 5 * Math.Pow(p2, 4) * (3 * l_e * p1 - 3 * l_e * p5 + 2 * p1 * p2 + 2 * p1 * p6 - 2 * p2 * p5) + 8 * (p1 - p5) * Math.Pow((p2 + p6), 5) + 5 * Math.Pow((p2 + p6), 4) * (-3 * l_e * p1 + 3 * l_e * p5 - 2 * p1 * p2 - 2 * p1 * p6 + 2 * p2 * p5))) / (20 * Math.Pow(l_e, 3) * p6 * (-l_e + p2 + p6));
                            fe[10, 0] = (15 * l_e * p1 * Math.Pow(p2, 3) * p6 * (-l_e + p2 + p6) + 20 * l_e * p6 * (Math.Pow(l_e, 3) * (-l_e * p5 + p2 * p3 + p3 * p6) + Math.Pow((p2 + p6), 3) * (l_e * p5 - p2 * p3 - p3 * p6)) + 20 * l_e * (Math.Pow(p2, 3) * (-p1 * p2 - p1 * p6 + p2 * p5) + Math.Pow((p2 + p6), 3) * (p1 * p2 + p1 * p6 - p2 * p5)) * (-l_e + p2 + p6) - 12 * p1 * Math.Pow(p2, 4) * p6 * (-l_e + p2 + p6) + 15 * p6 * (Math.Pow(l_e, 4) * (-l_e * p3 + 2 * l_e * p5 - p2 * p3 - p3 * p6) + Math.Pow((p2 + p6), 4) * (l_e * p3 - 2 * l_e * p5 + p2 * p3 + p3 * p6)) + 12 * p6 * (Math.Pow(l_e, 5) * (p3 - p5) + Math.Pow((p2 + p6), 5) * (-p3 + p5)) + 3 * (-l_e + p2 + p6) * (4 * Math.Pow(p2, 5) * (-p1 + p5) + 5 * Math.Pow(p2, 4) * (l_e * p1 - l_e * p5 + p1 * p2 + p1 * p6 - p2 * p5) + 4 * (p1 - p5) * Math.Pow((p2 + p6), 5) + 5 * Math.Pow((p2 + p6), 4) * (-l_e * p1 + l_e * p5 - p1 * p2 - p1 * p6 + p2 * p5))) / (60 * Math.Pow(l_e, 2) * p6 * (-l_e + p2 + p6));
                        }
                        else
                        {
                            fe[2, 0] = (3 * Math.Pow(l_e, 4) * p3 + 7 * Math.Pow(l_e, 4) * p5 + 10 * Math.Pow(l_e, 3) * p1 * p6 - 7 * Math.Pow(l_e, 3) * p3 * p6 - 3 * Math.Pow(l_e, 3) * p5 * p6 + 3 * Math.Pow(l_e, 2) * p3 * Math.Pow(p6, 2) - 3 * Math.Pow(l_e, 2) * p5 * Math.Pow(p6, 2) - 5 * l_e * p1 * Math.Pow(p6, 3) + 3 * l_e * p3 * Math.Pow(p6, 3) + 2 * l_e * p5 * Math.Pow(p6, 3) + 2 * p1 * Math.Pow(p6, 4) - 2 * p3 * Math.Pow(p6, 4)) / (20 * Math.Pow(l_e, 3));
                            fe[4, 0] = -Math.Pow(l_e, 2) * p3 / 30.0 - Math.Pow(l_e, 2) * p5 / 20.0 + l_e * p3 * p6 / 20.0 - l_e * p5 * p6 / 20.0 - p1 * Math.Pow(p6, 2) / 6.0 + p3 * Math.Pow(p6, 2) / 20.0 + 7 * p5 * Math.Pow(p6, 2) / 60.0 + p1 * Math.Pow(p6, 3) / (6 * l_e) - 7 * p3 * Math.Pow(p6, 3) / (60 * l_e) - p5 * Math.Pow(p6, 3) / (20 * l_e) - p1 * Math.Pow(p6, 4) / (20 * Math.Pow(l_e, 2)) + p3 * Math.Pow(p6, 4) / (20 * Math.Pow(l_e, 2));
                            fe[8, 0] = (7 * Math.Pow(l_e, 4) * p3 + 3 * Math.Pow(l_e, 4) * p5 - 3 * Math.Pow(l_e, 3) * p3 * p6 + 3 * Math.Pow(l_e, 3) * p5 * p6 - 3 * Math.Pow(l_e, 2) * p3 * Math.Pow(p6, 2) + 3 * Math.Pow(l_e, 2) * p5 * Math.Pow(p6, 2) + 5 * l_e * p1 * Math.Pow(p6, 3) - 3 * l_e * p3 * Math.Pow(p6, 3) - 2 * l_e * p5 * Math.Pow(p6, 3) - 2 * p1 * Math.Pow(p6, 4) + 2 * p3 * Math.Pow(p6, 4)) / (20 * Math.Pow(l_e, 3));
                            fe[10, 0] = Math.Pow(l_e, 2) * p3 / 20.0 + Math.Pow(l_e, 2) * p5 / 30.0 - l_e * p3 * p6 / 30.0 + l_e * p5 * p6 / 30.0 - p3 * Math.Pow(p6, 2) / 30.0 + p5 * Math.Pow(p6, 2) / 30.0 + p1 * Math.Pow(p6, 3) / (12 * l_e) - p3 * Math.Pow(p6, 3) / (30 * l_e) - p5 * Math.Pow(p6, 3) / (20 * l_e) - p1 * Math.Pow(p6, 4) / (20 * Math.Pow(l_e, 2)) + p3 * Math.Pow(p6, 4) / (20 * Math.Pow(l_e, 2));
                        }
                        if (l_e / 2.0 < p2)
                        {
                            fc[2, 0] = fe[2, 0] - Math.Pow(l_e, 2) * p1 / (8 * p2);
                            fc[4, 0] = -(fe[4, 0] - Math.Pow(l_e, 3) * p1 / (48 * p2) + fe[2, 0] * l_e / 2.0);
                        }
                        else if (l_e / 2.0 < p2 + p6)
                        {
                            if (Math.Abs(p6) > 1e-10)
                            {
                                if (Math.Abs(-l_e + p2 + p4) < 1e-10) { System.Console.WriteLine("***warning s6-1***"); }
                                fc[2, 0] = fe[2, 0] - (Math.Pow(l_e, 2) * (-p1 + p5) + 4 * l_e * (p1 * p2 + p1 * p6 - p2 * p5) + 4 * p1 * p2 * p6 + 4 * Math.Pow(p2, 2) * (p1 - p5) - 8 * p2 * (p1 * p2 + p1 * p6 - p2 * p5)) / (8 * p6);
                                fc[4, 0] = -(fe[4, 0] - (-Math.Pow(l_e, 3) * p1 + Math.Pow(l_e, 3) * p5 + 6 * Math.Pow(l_e, 2) * p1 * p2 + 6 * Math.Pow(l_e, 2) * p1 * p6 - 6 * Math.Pow(l_e, 2) * p2 * p5 - 12 * l_e * p1 * Math.Pow(p2, 2) - 12 * l_e * p1 * p2 * p6 + 12 * l_e * Math.Pow(p2, 2) * p5 + 8 * p1 * Math.Pow(p2, 3) + 8 * p1 * Math.Pow(p2, 2) * p6 - 8 * Math.Pow(p2, 3) * p5) / (48 * p6) + fe[2, 0] * l_e / 2.0);
                            }
                        }
                        else if (l_e / 2.0 < l_e - p4)
                        {
                            if (Math.Abs(p6) > 1e-10)
                            {
                                fc[2, 0] = fe[2, 0] - (-Math.Pow(l_e, 2) * p6 * (p3 - p5) / 8.0 + p1 * p2 * p6 * (-l_e + p2 + p4 + p6) / 2.0 - p6 * (p2 + p6) * (-l_e * p5 + p2 * p3 + p3 * p6 + p4 * p5) + p6 * (l_e * (-l_e * p5 + p2 * p3 + p3 * p6 + p4 * p5) + Math.Pow((p2 + p6), 2) * (p3 - p5)) / 2.0 + (-l_e + p2 + p4 + p6) * (Math.Pow(p2, 2) * (p1 - p5) - 2 * p2 * (p1 * p2 + p1 * p6 - p2 * p5) + (-p1 + p5) * Math.Pow((p2 + p6), 2) + 2 * (p2 + p6) * (p1 * p2 + p1 * p6 - p2 * p5)) / 2.0) / (p6 * (-l_e + p2 + p4 + p6));
                                fc[4, 0] = -(fe[4, 0] - (2 * Math.Pow(l_e, 3) * p6 * (p3 - p5) - 3 * Math.Pow(l_e, 2) * p6 * (l_e * p3 - 3 * l_e * p5 + 2 * p2 * p3 + 2 * p3 * p6 + 2 * p4 * p5) - 24 * l_e * p6 * (p2 + p6) * (-l_e * p5 + p2 * p3 + p3 * p6 + p4 * p5) + 4 * p1 * p2 * p6 * (3 * l_e - 4 * p2) * (-l_e + p2 + p4 + p6) - 16 * p6 * Math.Pow((p2 + p6), 3) * (p3 - p5) + 12 * p6 * (Math.Pow(l_e, 2) * (-l_e * p5 + p2 * p3 + p3 * p6 + p4 * p5) + Math.Pow((p2 + p6), 2) * (l_e * p3 - 3 * l_e * p5 + 2 * p2 * p3 + 2 * p3 * p6 + 2 * p4 * p5)) + 4 * (-l_e + p2 + p4 + p6) * (-6 * l_e * p2 * (p1 * p2 + p1 * p6 - p2 * p5) + 6 * l_e * (p2 + p6) * (p1 * p2 + p1 * p6 - p2 * p5) + 4 * Math.Pow(p2, 3) * (-p1 + p5) + 3 * Math.Pow(p2, 2) * (l_e * p1 - l_e * p5 + 2 * p1 * p2 + 2 * p1 * p6 - 2 * p2 * p5) + 4 * (p1 - p5) * Math.Pow((p2 + p6), 3) + 3 * Math.Pow((p2 + p6), 2) * (-l_e * p1 + l_e * p5 - 2 * p1 * p2 - 2 * p1 * p6 + 2 * p2 * p5))) / (48 * p6 * (-l_e + p2 + p4 + p6)) + fe[2, 0] * l_e / 2.0);
                            }
                        }
                        else
                        {
                            if (p4 < 1e-10) { System.Console.WriteLine("***warning s6-2***"); }
                            else
                            {
                                fc[2, 0] = fe[8, 0] - l_e / 2.0 * p3 * (l_e / 2.0) / p4 / 2.0;
                                fc[4, 0] = -(-fe[10, 0] - (l_e / 2.0 * p3 * (l_e / 2.0) / p4 / 2.0) * l_e / 6.0 + fe[8, 0] * l_e / 2.0);
                            }
                        }
                    }
                    else if (ss.Count == 6)
                    {
                        var a = ss[0]; var b = ss[1]; var c = ss[2]; var d = ss[3]; var e = ss[4]; var g = ss[5];
                        var p1 = (Vector3d.CrossProduct(b - a, g - a) / (g - a).Length).Length;
                        var p2 = (b - a) * (g - a) / (g - a).Length;
                        var p3 = (Vector3d.CrossProduct(e - g, a - g) / (a - g).Length).Length;
                        var p4 = (e - g) * (a - g) / (a - g).Length;
                        var p5 = (Vector3d.CrossProduct(c - a, g - a) / (g - a).Length).Length;
                        var p6 = (c - a) * (g - a) / (g - a).Length - p2;
                        var p7 = (Vector3d.CrossProduct(d - g, a - g) / (a - g).Length).Length;
                        var p8 = (d - g) * (a - g) / (a - g).Length - p4;
                        if (p2 > 1e-10 && p4 > 1e-10)
                        {
                            fe[2, 0] = (7 * Math.Pow(l_e,4) * p5 + 3 * Math.Pow(l_e,4) * p7 + 10 * Math.Pow(l_e,3) * p1 * p2 + 10 * Math.Pow(l_e,3) * p1 * p6 - 13 * Math.Pow(l_e,3) * p2 * p5 - 7 * Math.Pow(l_e,3) * p2 * p7 - 3 * Math.Pow(l_e,3) * p4 * p5 + 3 * Math.Pow(l_e,3) * p4 * p7 - 3 * Math.Pow(l_e,3) * p5 * p6 - 3 * Math.Pow(l_e,3) * p5 * p8 - 7 * Math.Pow(l_e,3) * p6 * p7 + 3 * Math.Pow(l_e,3) * p7 * p8 - 3 * Math.Pow(l_e,2) * Math.Pow(p2,2) * p5 + 3 * Math.Pow(l_e,2) * Math.Pow(p2,2) * p7 + 4 * Math.Pow(l_e,2) * p2 * p4 * p5 - 4 * Math.Pow(l_e,2) * p2 * p4 * p7 - 6 * Math.Pow(l_e,2) * p2 * p5 * p6 + 4 * Math.Pow(l_e,2) * p2 * p5 * p8 + 6 * Math.Pow(l_e,2) * p2 * p6 * p7 - 4 * Math.Pow(l_e,2) * p2 * p7 * p8 - 3 * Math.Pow(l_e,2) * Math.Pow(p4,2) * p5 + 3 * Math.Pow(l_e,2) * Math.Pow(p4,2) * p7 + 4 * Math.Pow(l_e,2) * p4 * p5 * p6 - 6 * Math.Pow(l_e,2) * p4 * p5 * p8 - 4 * Math.Pow(l_e,2) * p4 * p6 * p7 + 6 * Math.Pow(l_e,2) * p4 * p7 * p8 - 3 * Math.Pow(l_e,2) * p5 * Math.Pow(p6,2) + 4 * Math.Pow(l_e,2) * p5 * p6 * p8 - 3 * Math.Pow(l_e,2) * p5 * Math.Pow(p8,2) + 3 * Math.Pow(l_e,2) * Math.Pow(p6,2) * p7 - 4 * Math.Pow(l_e,2) * p6 * p7 * p8 + 3 * Math.Pow(l_e,2) * p7 * Math.Pow(p8,2) - 15 * l_e * p1 * Math.Pow(p2,3) - 30 * l_e * p1 * Math.Pow(p2,2) * p6 - 20 * l_e * p1 * p2 * Math.Pow(p6,2) - 5 * l_e * p1 * Math.Pow(p6,3) + 17 * l_e * Math.Pow(p2,3) * p5 + 3 * l_e * Math.Pow(p2,3) * p7 + l_e * Math.Pow(p2,2) * p4 * p5 - l_e * Math.Pow(p2,2) * p4 * p7 + 21 * l_e * Math.Pow(p2,2) * p5 * p6 + l_e * Math.Pow(p2,2) * p5 * p8 + 9 * l_e * Math.Pow(p2,2) * p6 * p7 - l_e * Math.Pow(p2,2) * p7 * p8 + l_e * p2 * Math.Pow(p4,2) * p5 - l_e * p2 * Math.Pow(p4,2) * p7 + 2 * l_e * p2 * p4 * p5 * p6 + 2 * l_e * p2 * p4 * p5 * p8 - 2 * l_e * p2 * p4 * p6 * p7 - 2 * l_e * p2 * p4 * p7 * p8 + 11 * l_e * p2 * p5 * Math.Pow(p6,2) + 2 * l_e * p2 * p5 * p6 * p8 + l_e * p2 * p5 * Math.Pow(p8,2) + 9 * l_e * p2 * Math.Pow(p6,2) * p7 - 2 * l_e * p2 * p6 * p7 * p8 - l_e * p2 * p7 * Math.Pow(p8,2) + 15 * l_e * p3 * Math.Pow(p4,3) + 30 * l_e * p3 * Math.Pow(p4,2) * p8 + 20 * l_e * p3 * p4 * Math.Pow(p8,2) + 5 * l_e * p3 * Math.Pow(p8,3) - 3 * l_e * Math.Pow(p4,3) * p5 - 17 * l_e * Math.Pow(p4,3) * p7 + l_e * Math.Pow(p4,2) * p5 * p6 - 9 * l_e * Math.Pow(p4,2) * p5 * p8 - l_e * Math.Pow(p4,2) * p6 * p7 - 21 * l_e * Math.Pow(p4,2) * p7 * p8 + l_e * p4 * p5 * Math.Pow(p6,2) + 2 * l_e * p4 * p5 * p6 * p8 - 9 * l_e * p4 * p5 * Math.Pow(p8,2) - l_e * p4 * Math.Pow(p6,2) * p7 - 2 * l_e * p4 * p6 * p7 * p8 - 11 * l_e * p4 * p7 * Math.Pow(p8,2) + 2 * l_e * p5 * Math.Pow(p6,3) + l_e * p5 * Math.Pow(p6,2) * p8 + l_e * p5 * p6 * Math.Pow(p8,2) - 3 * l_e * p5 * Math.Pow(p8,3) + 3 * l_e * Math.Pow(p6,3) * p7 - l_e * Math.Pow(p6,2) * p7 * p8 - l_e * p6 * p7 * Math.Pow(p8,2) - 2 * l_e * p7 * Math.Pow(p8,3) + 8 * p1 * Math.Pow(p2,4) + 20 * p1 * Math.Pow(p2,3) * p6 + 20 * p1 * Math.Pow(p2,2) * Math.Pow(p6,2) + 10 * p1 * p2 * Math.Pow(p6,3) + 2 * p1 * Math.Pow(p6,4) - 8 * Math.Pow(p2,4) * p5 - 2 * Math.Pow(p2,4) * p7 - 2 * Math.Pow(p2,3) * p4 * p5 + 2 * Math.Pow(p2,3) * p4 * p7 - 12 * Math.Pow(p2,3) * p5 * p6 - 2 * Math.Pow(p2,3) * p5 * p8 - 8 * Math.Pow(p2,3) * p6 * p7 + 2 * Math.Pow(p2,3) * p7 * p8 + 2 * Math.Pow(p2,2) * Math.Pow(p4,2) * p5 - 2 * Math.Pow(p2,2) * Math.Pow(p4,2) * p7 - 6 * Math.Pow(p2,2) * p4 * p5 * p6 + 4 * Math.Pow(p2,2) * p4 * p5 * p8 + 6 * Math.Pow(p2,2) * p4 * p6 * p7 - 4 * Math.Pow(p2,2) * p4 * p7 * p8 - 8 * Math.Pow(p2,2) * p5 * Math.Pow(p6,2) - 6 * Math.Pow(p2,2) * p5 * p6 * p8 + 2 * Math.Pow(p2,2) * p5 * Math.Pow(p8,2) - 12 * Math.Pow(p2,2) * Math.Pow(p6,2) * p7 + 6 * Math.Pow(p2,2) * p6 * p7 * p8 - 2 * Math.Pow(p2,2) * p7 * Math.Pow(p8,2) - 2 * p2 * Math.Pow(p4,3) * p5 + 2 * p2 * Math.Pow(p4,3) * p7 + 4 * p2 * Math.Pow(p4,2) * p5 * p6 - 6 * p2 * Math.Pow(p4,2) * p5 * p8 - 4 * p2 * Math.Pow(p4,2) * p6 * p7 + 6 * p2 * Math.Pow(p4,2) * p7 * p8 - 6 * p2 * p4 * p5 * Math.Pow(p6,2) + 8 * p2 * p4 * p5 * p6 * p8 - 6 * p2 * p4 * p5 * Math.Pow(p8,2) + 6 * p2 * p4 * Math.Pow(p6,2) * p7 - 8 * p2 * p4 * p6 * p7 * p8 + 6 * p2 * p4 * p7 * Math.Pow(p8,2) - 2 * p2 * p5 * Math.Pow(p6,3) - 6 * p2 * p5 * Math.Pow(p6,2) * p8 + 4 * p2 * p5 * p6 * Math.Pow(p8,2) - 2 * p2 * p5 * Math.Pow(p8,3) - 8 * p2 * Math.Pow(p6,3) * p7 + 6 * p2 * Math.Pow(p6,2) * p7 * p8 - 4 * p2 * p6 * p7 * Math.Pow(p8,2) + 2 * p2 * p7 * Math.Pow(p8,3) - 8 * p3 * Math.Pow(p4,4) - 20 * p3 * Math.Pow(p4,3) * p8 - 20 * p3 * Math.Pow(p4,2) * Math.Pow(p8,2) - 10 * p3 * p4 * Math.Pow(p8,3) - 2 * p3 * Math.Pow(p8,4) + 2 * Math.Pow(p4,4) * p5 + 8 * Math.Pow(p4,4) * p7 - 2 * Math.Pow(p4,3) * p5 * p6 + 8 * Math.Pow(p4,3) * p5 * p8 + 2 * Math.Pow(p4,3) * p6 * p7 + 12 * Math.Pow(p4,3) * p7 * p8 + 2 * Math.Pow(p4,2) * p5 * Math.Pow(p6,2) - 6 * Math.Pow(p4,2) * p5 * p6 * p8 + 12 * Math.Pow(p4,2) * p5 * Math.Pow(p8,2) - 2 * Math.Pow(p4,2) * Math.Pow(p6,2) * p7 + 6 * Math.Pow(p4,2) * p6 * p7 * p8 + 8 * Math.Pow(p4,2) * p7 * Math.Pow(p8,2) - 2 * p4 * p5 * Math.Pow(p6,3) + 4 * p4 * p5 * Math.Pow(p6,2) * p8 - 6 * p4 * p5 * p6 * Math.Pow(p8,2) + 8 * p4 * p5 * Math.Pow(p8,3) + 2 * p4 * Math.Pow(p6,3) * p7 - 4 * p4 * Math.Pow(p6,2) * p7 * p8 + 6 * p4 * p6 * p7 * Math.Pow(p8,2) + 2 * p4 * p7 * Math.Pow(p8,3) - 2 * p5 * Math.Pow(p6,3) * p8 + 2 * p5 * Math.Pow(p6,2) * Math.Pow(p8,2) - 2 * p5 * p6 * Math.Pow(p8,3) + 2 * p5 * Math.Pow(p8,4) - 2 * Math.Pow(p6,4) * p7 + 2 * Math.Pow(p6,3) * p7 * p8 - 2 * Math.Pow(p6,2) * p7 * Math.Pow(p8,2) + 2 * p6 * p7 * Math.Pow(p8,3)) / (20 * Math.Pow(l_e,3));
                            fe[4, 0] = (-3 * Math.Pow(l_e,4) * p5 - 2 * Math.Pow(l_e,4) * p7 - 3 * Math.Pow(l_e,3) * p2 * p5 + 3 * Math.Pow(l_e,3) * p2 * p7 + 2 * Math.Pow(l_e,3) * p4 * p5 - 2 * Math.Pow(l_e,3) * p4 * p7 - 3 * Math.Pow(l_e,3) * p5 * p6 + 2 * Math.Pow(l_e,3) * p5 * p8 + 3 * Math.Pow(l_e,3) * p6 * p7 - 2 * Math.Pow(l_e,3) * p7 * p8 - 20 * Math.Pow(l_e,2) * p1 * Math.Pow(p2,2) - 30 * Math.Pow(l_e,2) * p1 * p2 * p6 - 10 * Math.Pow(l_e,2) * p1 * Math.Pow(p6,2) + 27 * Math.Pow(l_e,2) * Math.Pow(p2,2) * p5 + 3 * Math.Pow(l_e,2) * Math.Pow(p2,2) * p7 - Math.Pow(l_e,2) * p2 * p4 * p5 + Math.Pow(l_e,2) * p2 * p4 * p7 + 24 * Math.Pow(l_e,2) * p2 * p5 * p6 - Math.Pow(l_e,2) * p2 * p5 * p8 + 6 * Math.Pow(l_e,2) * p2 * p6 * p7 + Math.Pow(l_e,2) * p2 * p7 * p8 + 2 * Math.Pow(l_e,2) * Math.Pow(p4,2) * p5 - 2 * Math.Pow(l_e,2) * Math.Pow(p4,2) * p7 - Math.Pow(l_e,2) * p4 * p5 * p6 + 4 * Math.Pow(l_e,2) * p4 * p5 * p8 + Math.Pow(l_e,2) * p4 * p6 * p7 - 4 * Math.Pow(l_e,2) * p4 * p7 * p8 + 7 * Math.Pow(l_e,2) * p5 * Math.Pow(p6,2) - Math.Pow(l_e,2) * p5 * p6 * p8 + 2 * Math.Pow(l_e,2) * p5 * Math.Pow(p8,2) + 3 * Math.Pow(l_e,2) * Math.Pow(p6,2) * p7 + Math.Pow(l_e,2) * p6 * p7 * p8 - 2 * Math.Pow(l_e,2) * p7 * Math.Pow(p8,2) + 30 * l_e * p1 * Math.Pow(p2,3) + 60 * l_e * p1 * Math.Pow(p2,2) * p6 + 40 * l_e * p1 * p2 * Math.Pow(p6,2) + 10 * l_e * p1 * Math.Pow(p6,3) - 33 * l_e * Math.Pow(p2,3) * p5 - 7 * l_e * Math.Pow(p2,3) * p7 - 4 * l_e * Math.Pow(p2,2) * p4 * p5 + 4 * l_e * Math.Pow(p2,2) * p4 * p7 - 39 * l_e * Math.Pow(p2,2) * p5 * p6 - 4 * l_e * Math.Pow(p2,2) * p5 * p8 - 21 * l_e * Math.Pow(p2,2) * p6 * p7 + 4 * l_e * Math.Pow(p2,2) * p7 * p8 + l_e * p2 * Math.Pow(p4,2) * p5 - l_e * p2 * Math.Pow(p4,2) * p7 - 8 * l_e * p2 * p4 * p5 * p6 + 2 * l_e * p2 * p4 * p5 * p8 + 8 * l_e * p2 * p4 * p6 * p7 - 2 * l_e * p2 * p4 * p7 * p8 - 19 * l_e * p2 * p5 * Math.Pow(p6,2) - 8 * l_e * p2 * p5 * p6 * p8 + l_e * p2 * p5 * Math.Pow(p8,2) - 21 * l_e * p2 * Math.Pow(p6,2) * p7 + 8 * l_e * p2 * p6 * p7 * p8 - l_e * p2 * p7 * Math.Pow(p8,2) - 15 * l_e * p3 * Math.Pow(p4,3) - 30 * l_e * p3 * Math.Pow(p4,2) * p8 - 20 * l_e * p3 * p4 * Math.Pow(p8,2) - 5 * l_e * p3 * Math.Pow(p8,3) + 2 * l_e * Math.Pow(p4,3) * p5 + 18 * l_e * Math.Pow(p4,3) * p7 + l_e * Math.Pow(p4,2) * p5 * p6 + 6 * l_e * Math.Pow(p4,2) * p5 * p8 - l_e * Math.Pow(p4,2) * p6 * p7 + 24 * l_e * Math.Pow(p4,2) * p7 * p8 - 4 * l_e * p4 * p5 * Math.Pow(p6,2) + 2 * l_e * p4 * p5 * p6 * p8 + 6 * l_e * p4 * p5 * Math.Pow(p8,2) + 4 * l_e * p4 * Math.Pow(p6,2) * p7 - 2 * l_e * p4 * p6 * p7 * p8 + 14 * l_e * p4 * p7 * Math.Pow(p8,2) - 3 * l_e * p5 * Math.Pow(p6,3) - 4 * l_e * p5 * Math.Pow(p6,2) * p8 + l_e * p5 * p6 * Math.Pow(p8,2) + 2 * l_e * p5 * Math.Pow(p8,3) - 7 * l_e * Math.Pow(p6,3) * p7 + 4 * l_e * Math.Pow(p6,2) * p7 * p8 - l_e * p6 * p7 * Math.Pow(p8,2) + 3 * l_e * p7 * Math.Pow(p8,3) - 12 * p1 * Math.Pow(p2,4) - 30 * p1 * Math.Pow(p2,3) * p6 - 30 * p1 * Math.Pow(p2,2) * Math.Pow(p6,2) - 15 * p1 * p2 * Math.Pow(p6,3) - 3 * p1 * Math.Pow(p6,4) + 12 * Math.Pow(p2,4) * p5 + 3 * Math.Pow(p2,4) * p7 + 3 * Math.Pow(p2,3) * p4 * p5 - 3 * Math.Pow(p2,3) * p4 * p7 + 18 * Math.Pow(p2,3) * p5 * p6 + 3 * Math.Pow(p2,3) * p5 * p8 + 12 * Math.Pow(p2,3) * p6 * p7 - 3 * Math.Pow(p2,3) * p7 * p8 - 3 * Math.Pow(p2,2) * Math.Pow(p4,2) * p5 + 3 * Math.Pow(p2,2) * Math.Pow(p4,2) * p7 + 9 * Math.Pow(p2,2) * p4 * p5 * p6 - 6 * Math.Pow(p2,2) * p4 * p5 * p8 - 9 * Math.Pow(p2,2) * p4 * p6 * p7 + 6 * Math.Pow(p2,2) * p4 * p7 * p8 + 12 * Math.Pow(p2,2) * p5 * Math.Pow(p6,2) + 9 * Math.Pow(p2,2) * p5 * p6 * p8 - 3 * Math.Pow(p2,2) * p5 * Math.Pow(p8,2) + 18 * Math.Pow(p2,2) * Math.Pow(p6,2) * p7 - 9 * Math.Pow(p2,2) * p6 * p7 * p8 + 3 * Math.Pow(p2,2) * p7 * Math.Pow(p8,2) + 3 * p2 * Math.Pow(p4,3) * p5 - 3 * p2 * Math.Pow(p4,3) * p7 - 6 * p2 * Math.Pow(p4,2) * p5 * p6 + 9 * p2 * Math.Pow(p4,2) * p5 * p8 + 6 * p2 * Math.Pow(p4,2) * p6 * p7 - 9 * p2 * Math.Pow(p4,2) * p7 * p8 + 9 * p2 * p4 * p5 * Math.Pow(p6,2) - 12 * p2 * p4 * p5 * p6 * p8 + 9 * p2 * p4 * p5 * Math.Pow(p8,2) - 9 * p2 * p4 * Math.Pow(p6,2) * p7 + 12 * p2 * p4 * p6 * p7 * p8 - 9 * p2 * p4 * p7 * Math.Pow(p8,2) + 3 * p2 * p5 * Math.Pow(p6,3) + 9 * p2 * p5 * Math.Pow(p6,2) * p8 - 6 * p2 * p5 * p6 * Math.Pow(p8,2) + 3 * p2 * p5 * Math.Pow(p8,3) + 12 * p2 * Math.Pow(p6,3) * p7 - 9 * p2 * Math.Pow(p6,2) * p7 * p8 + 6 * p2 * p6 * p7 * Math.Pow(p8,2) - 3 * p2 * p7 * Math.Pow(p8,3) + 12 * p3 * Math.Pow(p4,4) + 30 * p3 * Math.Pow(p4,3) * p8 + 30 * p3 * Math.Pow(p4,2) * Math.Pow(p8,2) + 15 * p3 * p4 * Math.Pow(p8,3) + 3 * p3 * Math.Pow(p8,4) - 3 * Math.Pow(p4,4) * p5 - 12 * Math.Pow(p4,4) * p7 + 3 * Math.Pow(p4,3) * p5 * p6 - 12 * Math.Pow(p4,3) * p5 * p8 - 3 * Math.Pow(p4,3) * p6 * p7 - 18 * Math.Pow(p4,3) * p7 * p8 - 3 * Math.Pow(p4,2) * p5 * Math.Pow(p6,2) + 9 * Math.Pow(p4,2) * p5 * p6 * p8 - 18 * Math.Pow(p4,2) * p5 * Math.Pow(p8,2) + 3 * Math.Pow(p4,2) * Math.Pow(p6,2) * p7 - 9 * Math.Pow(p4,2) * p6 * p7 * p8 - 12 * Math.Pow(p4,2) * p7 * Math.Pow(p8,2) + 3 * p4 * p5 * Math.Pow(p6,3) - 6 * p4 * p5 * Math.Pow(p6,2) * p8 + 9 * p4 * p5 * p6 * Math.Pow(p8,2) - 12 * p4 * p5 * Math.Pow(p8,3) - 3 * p4 * Math.Pow(p6,3) * p7 + 6 * p4 * Math.Pow(p6,2) * p7 * p8 - 9 * p4 * p6 * p7 * Math.Pow(p8,2) - 3 * p4 * p7 * Math.Pow(p8,3) + 3 * p5 * Math.Pow(p6,3) * p8 - 3 * p5 * Math.Pow(p6,2) * Math.Pow(p8,2) + 3 * p5 * p6 * Math.Pow(p8,3) - 3 * p5 * Math.Pow(p8,4) + 3 * Math.Pow(p6,4) * p7 - 3 * Math.Pow(p6,3) * p7 * p8 + 3 * Math.Pow(p6,2) * p7 * Math.Pow(p8,2) - 3 * p6 * p7 * Math.Pow(p8,3)) / (60 * Math.Pow(l_e,2));
                            fe[8, 0] = (3 * Math.Pow(l_e,4) * p5 + 7 * Math.Pow(l_e,4) * p7 + 3 * Math.Pow(l_e,3) * p2 * p5 - 3 * Math.Pow(l_e,3) * p2 * p7 + 10 * Math.Pow(l_e,3) * p3 * p4 + 10 * Math.Pow(l_e,3) * p3 * p8 - 7 * Math.Pow(l_e,3) * p4 * p5 - 13 * Math.Pow(l_e,3) * p4 * p7 + 3 * Math.Pow(l_e,3) * p5 * p6 - 7 * Math.Pow(l_e,3) * p5 * p8 - 3 * Math.Pow(l_e,3) * p6 * p7 - 3 * Math.Pow(l_e,3) * p7 * p8 + 3 * Math.Pow(l_e,2) * Math.Pow(p2,2) * p5 - 3 * Math.Pow(l_e,2) * Math.Pow(p2,2) * p7 - 4 * Math.Pow(l_e,2) * p2 * p4 * p5 + 4 * Math.Pow(l_e,2) * p2 * p4 * p7 + 6 * Math.Pow(l_e,2) * p2 * p5 * p6 - 4 * Math.Pow(l_e,2) * p2 * p5 * p8 - 6 * Math.Pow(l_e,2) * p2 * p6 * p7 + 4 * Math.Pow(l_e,2) * p2 * p7 * p8 + 3 * Math.Pow(l_e,2) * Math.Pow(p4,2) * p5 - 3 * Math.Pow(l_e,2) * Math.Pow(p4,2) * p7 - 4 * Math.Pow(l_e,2) * p4 * p5 * p6 + 6 * Math.Pow(l_e,2) * p4 * p5 * p8 + 4 * Math.Pow(l_e,2) * p4 * p6 * p7 - 6 * Math.Pow(l_e,2) * p4 * p7 * p8 + 3 * Math.Pow(l_e,2) * p5 * Math.Pow(p6,2) - 4 * Math.Pow(l_e,2) * p5 * p6 * p8 + 3 * Math.Pow(l_e,2) * p5 * Math.Pow(p8,2) - 3 * Math.Pow(l_e,2) * Math.Pow(p6,2) * p7 + 4 * Math.Pow(l_e,2) * p6 * p7 * p8 - 3 * Math.Pow(l_e,2) * p7 * Math.Pow(p8,2) + 15 * l_e * p1 * Math.Pow(p2,3) + 30 * l_e * p1 * Math.Pow(p2,2) * p6 + 20 * l_e * p1 * p2 * Math.Pow(p6,2) + 5 * l_e * p1 * Math.Pow(p6,3) - 17 * l_e * Math.Pow(p2,3) * p5 - 3 * l_e * Math.Pow(p2,3) * p7 - l_e * Math.Pow(p2,2) * p4 * p5 + l_e * Math.Pow(p2,2) * p4 * p7 - 21 * l_e * Math.Pow(p2,2) * p5 * p6 - l_e * Math.Pow(p2,2) * p5 * p8 - 9 * l_e * Math.Pow(p2,2) * p6 * p7 + l_e * Math.Pow(p2,2) * p7 * p8 - l_e * p2 * Math.Pow(p4,2) * p5 + l_e * p2 * Math.Pow(p4,2) * p7 - 2 * l_e * p2 * p4 * p5 * p6 - 2 * l_e * p2 * p4 * p5 * p8 + 2 * l_e * p2 * p4 * p6 * p7 + 2 * l_e * p2 * p4 * p7 * p8 - 11 * l_e * p2 * p5 * Math.Pow(p6,2) - 2 * l_e * p2 * p5 * p6 * p8 - l_e * p2 * p5 * Math.Pow(p8,2) - 9 * l_e * p2 * Math.Pow(p6,2) * p7 + 2 * l_e * p2 * p6 * p7 * p8 + l_e * p2 * p7 * Math.Pow(p8,2) - 15 * l_e * p3 * Math.Pow(p4,3) - 30 * l_e * p3 * Math.Pow(p4,2) * p8 - 20 * l_e * p3 * p4 * Math.Pow(p8,2) - 5 * l_e * p3 * Math.Pow(p8,3) + 3 * l_e * Math.Pow(p4,3) * p5 + 17 * l_e * Math.Pow(p4,3) * p7 - l_e * Math.Pow(p4,2) * p5 * p6 + 9 * l_e * Math.Pow(p4,2) * p5 * p8 + l_e * Math.Pow(p4,2) * p6 * p7 + 21 * l_e * Math.Pow(p4,2) * p7 * p8 - l_e * p4 * p5 * Math.Pow(p6,2) - 2 * l_e * p4 * p5 * p6 * p8 + 9 * l_e * p4 * p5 * Math.Pow(p8,2) + l_e * p4 * Math.Pow(p6,2) * p7 + 2 * l_e * p4 * p6 * p7 * p8 + 11 * l_e * p4 * p7 * Math.Pow(p8,2) - 2 * l_e * p5 * Math.Pow(p6,3) - l_e * p5 * Math.Pow(p6,2) * p8 - l_e * p5 * p6 * Math.Pow(p8,2) + 3 * l_e * p5 * Math.Pow(p8,3) - 3 * l_e * Math.Pow(p6,3) * p7 + l_e * Math.Pow(p6,2) * p7 * p8 + l_e * p6 * p7 * Math.Pow(p8,2) + 2 * l_e * p7 * Math.Pow(p8,3) - 8 * p1 * Math.Pow(p2,4) - 20 * p1 * Math.Pow(p2,3) * p6 - 20 * p1 * Math.Pow(p2,2) * Math.Pow(p6,2) - 10 * p1 * p2 * Math.Pow(p6,3) - 2 * p1 * Math.Pow(p6,4) + 8 * Math.Pow(p2,4) * p5 + 2 * Math.Pow(p2,4) * p7 + 2 * Math.Pow(p2,3) * p4 * p5 - 2 * Math.Pow(p2,3) * p4 * p7 + 12 * Math.Pow(p2,3) * p5 * p6 + 2 * Math.Pow(p2,3) * p5 * p8 + 8 * Math.Pow(p2,3) * p6 * p7 - 2 * Math.Pow(p2,3) * p7 * p8 - 2 * Math.Pow(p2,2) * Math.Pow(p4,2) * p5 + 2 * Math.Pow(p2,2) * Math.Pow(p4,2) * p7 + 6 * Math.Pow(p2,2) * p4 * p5 * p6 - 4 * Math.Pow(p2,2) * p4 * p5 * p8 - 6 * Math.Pow(p2,2) * p4 * p6 * p7 + 4 * Math.Pow(p2,2) * p4 * p7 * p8 + 8 * Math.Pow(p2,2) * p5 * Math.Pow(p6,2) + 6 * Math.Pow(p2,2) * p5 * p6 * p8 - 2 * Math.Pow(p2,2) * p5 * Math.Pow(p8,2) + 12 * Math.Pow(p2,2) * Math.Pow(p6,2) * p7 - 6 * Math.Pow(p2,2) * p6 * p7 * p8 + 2 * Math.Pow(p2,2) * p7 * Math.Pow(p8,2) + 2 * p2 * Math.Pow(p4,3) * p5 - 2 * p2 * Math.Pow(p4,3) * p7 - 4 * p2 * Math.Pow(p4,2) * p5 * p6 + 6 * p2 * Math.Pow(p4,2) * p5 * p8 + 4 * p2 * Math.Pow(p4,2) * p6 * p7 - 6 * p2 * Math.Pow(p4,2) * p7 * p8 + 6 * p2 * p4 * p5 * Math.Pow(p6,2) - 8 * p2 * p4 * p5 * p6 * p8 + 6 * p2 * p4 * p5 * Math.Pow(p8,2) - 6 * p2 * p4 * Math.Pow(p6,2) * p7 + 8 * p2 * p4 * p6 * p7 * p8 - 6 * p2 * p4 * p7 * Math.Pow(p8,2) + 2 * p2 * p5 * Math.Pow(p6,3) + 6 * p2 * p5 * Math.Pow(p6,2) * p8 - 4 * p2 * p5 * p6 * Math.Pow(p8,2) + 2 * p2 * p5 * Math.Pow(p8,3) + 8 * p2 * Math.Pow(p6,3) * p7 - 6 * p2 * Math.Pow(p6,2) * p7 * p8 + 4 * p2 * p6 * p7 * Math.Pow(p8,2) - 2 * p2 * p7 * Math.Pow(p8,3) + 8 * p3 * Math.Pow(p4,4) + 20 * p3 * Math.Pow(p4,3) * p8 + 20 * p3 * Math.Pow(p4,2) * Math.Pow(p8,2) + 10 * p3 * p4 * Math.Pow(p8,3) + 2 * p3 * Math.Pow(p8,4) - 2 * Math.Pow(p4,4) * p5 - 8 * Math.Pow(p4,4) * p7 + 2 * Math.Pow(p4,3) * p5 * p6 - 8 * Math.Pow(p4,3) * p5 * p8 - 2 * Math.Pow(p4,3) * p6 * p7 - 12 * Math.Pow(p4,3) * p7 * p8 - 2 * Math.Pow(p4,2) * p5 * Math.Pow(p6,2) + 6 * Math.Pow(p4,2) * p5 * p6 * p8 - 12 * Math.Pow(p4,2) * p5 * Math.Pow(p8,2) + 2 * Math.Pow(p4,2) * Math.Pow(p6,2) * p7 - 6 * Math.Pow(p4,2) * p6 * p7 * p8 - 8 * Math.Pow(p4,2) * p7 * Math.Pow(p8,2) + 2 * p4 * p5 * Math.Pow(p6,3) - 4 * p4 * p5 * Math.Pow(p6,2) * p8 + 6 * p4 * p5 * p6 * Math.Pow(p8,2) - 8 * p4 * p5 * Math.Pow(p8,3) - 2 * p4 * Math.Pow(p6,3) * p7 + 4 * p4 * Math.Pow(p6,2) * p7 * p8 - 6 * p4 * p6 * p7 * Math.Pow(p8,2) - 2 * p4 * p7 * Math.Pow(p8,3) + 2 * p5 * Math.Pow(p6,3) * p8 - 2 * p5 * Math.Pow(p6,2) * Math.Pow(p8,2) + 2 * p5 * p6 * Math.Pow(p8,3) - 2 * p5 * Math.Pow(p8,4) + 2 * Math.Pow(p6,4) * p7 - 2 * Math.Pow(p6,3) * p7 * p8 + 2 * Math.Pow(p6,2) * p7 * Math.Pow(p8,2) - 2 * p6 * p7 * Math.Pow(p8,3)) / (20 * Math.Pow(l_e,3));
                            fe[10, 0] = (2 * Math.Pow(l_e,4) * p5 + 3 * Math.Pow(l_e,4) * p7 + 2 * Math.Pow(l_e,3) * p2 * p5 - 2 * Math.Pow(l_e,3) * p2 * p7 - 3 * Math.Pow(l_e,3) * p4 * p5 + 3 * Math.Pow(l_e,3) * p4 * p7 + 2 * Math.Pow(l_e,3) * p5 * p6 - 3 * Math.Pow(l_e,3) * p5 * p8 - 2 * Math.Pow(l_e,3) * p6 * p7 + 3 * Math.Pow(l_e,3) * p7 * p8 + 2 * Math.Pow(l_e,2) * Math.Pow(p2,2) * p5 - 2 * Math.Pow(l_e,2) * Math.Pow(p2,2) * p7 - Math.Pow(l_e,2) * p2 * p4 * p5 + Math.Pow(l_e,2) * p2 * p4 * p7 + 4 * Math.Pow(l_e,2) * p2 * p5 * p6 - Math.Pow(l_e,2) * p2 * p5 * p8 - 4 * Math.Pow(l_e,2) * p2 * p6 * p7 + Math.Pow(l_e,2) * p2 * p7 * p8 + 20 * Math.Pow(l_e,2) * p3 * Math.Pow(p4,2) + 30 * Math.Pow(l_e,2) * p3 * p4 * p8 + 10 * Math.Pow(l_e,2) * p3 * Math.Pow(p8,2) - 3 * Math.Pow(l_e,2) * Math.Pow(p4,2) * p5 - 27 * Math.Pow(l_e,2) * Math.Pow(p4,2) * p7 - Math.Pow(l_e,2) * p4 * p5 * p6 - 6 * Math.Pow(l_e,2) * p4 * p5 * p8 + Math.Pow(l_e,2) * p4 * p6 * p7 - 24 * Math.Pow(l_e,2) * p4 * p7 * p8 + 2 * Math.Pow(l_e,2) * p5 * Math.Pow(p6,2) - Math.Pow(l_e,2) * p5 * p6 * p8 - 3 * Math.Pow(l_e,2) * p5 * Math.Pow(p8,2) - 2 * Math.Pow(l_e,2) * Math.Pow(p6,2) * p7 + Math.Pow(l_e,2) * p6 * p7 * p8 - 7 * Math.Pow(l_e,2) * p7 * Math.Pow(p8,2) + 15 * l_e * p1 * Math.Pow(p2,3) + 30 * l_e * p1 * Math.Pow(p2,2) * p6 + 20 * l_e * p1 * p2 * Math.Pow(p6,2) + 5 * l_e * p1 * Math.Pow(p6,3) - 18 * l_e * Math.Pow(p2,3) * p5 - 2 * l_e * Math.Pow(p2,3) * p7 + l_e * Math.Pow(p2,2) * p4 * p5 - l_e * Math.Pow(p2,2) * p4 * p7 - 24 * l_e * Math.Pow(p2,2) * p5 * p6 + l_e * Math.Pow(p2,2) * p5 * p8 - 6 * l_e * Math.Pow(p2,2) * p6 * p7 - l_e * Math.Pow(p2,2) * p7 * p8 - 4 * l_e * p2 * Math.Pow(p4,2) * p5 + 4 * l_e * p2 * Math.Pow(p4,2) * p7 + 2 * l_e * p2 * p4 * p5 * p6 - 8 * l_e * p2 * p4 * p5 * p8 - 2 * l_e * p2 * p4 * p6 * p7 + 8 * l_e * p2 * p4 * p7 * p8 - 14 * l_e * p2 * p5 * Math.Pow(p6,2) + 2 * l_e * p2 * p5 * p6 * p8 - 4 * l_e * p2 * p5 * Math.Pow(p8,2) - 6 * l_e * p2 * Math.Pow(p6,2) * p7 - 2 * l_e * p2 * p6 * p7 * p8 + 4 * l_e * p2 * p7 * Math.Pow(p8,2) - 30 * l_e * p3 * Math.Pow(p4,3) - 60 * l_e * p3 * Math.Pow(p4,2) * p8 - 40 * l_e * p3 * p4 * Math.Pow(p8,2) - 10 * l_e * p3 * Math.Pow(p8,3) + 7 * l_e * Math.Pow(p4,3) * p5 + 33 * l_e * Math.Pow(p4,3) * p7 - 4 * l_e * Math.Pow(p4,2) * p5 * p6 + 21 * l_e * Math.Pow(p4,2) * p5 * p8 + 4 * l_e * Math.Pow(p4,2) * p6 * p7 + 39 * l_e * Math.Pow(p4,2) * p7 * p8 + l_e * p4 * p5 * Math.Pow(p6,2) - 8 * l_e * p4 * p5 * p6 * p8 + 21 * l_e * p4 * p5 * Math.Pow(p8,2) - l_e * p4 * Math.Pow(p6,2) * p7 + 8 * l_e * p4 * p6 * p7 * p8 + 19 * l_e * p4 * p7 * Math.Pow(p8,2) - 3 * l_e * p5 * Math.Pow(p6,3) + l_e * p5 * Math.Pow(p6,2) * p8 - 4 * l_e * p5 * p6 * Math.Pow(p8,2) + 7 * l_e * p5 * Math.Pow(p8,3) - 2 * l_e * Math.Pow(p6,3) * p7 - l_e * Math.Pow(p6,2) * p7 * p8 + 4 * l_e * p6 * p7 * Math.Pow(p8,2) + 3 * l_e * p7 * Math.Pow(p8,3) - 12 * p1 * Math.Pow(p2,4) - 30 * p1 * Math.Pow(p2,3) * p6 - 30 * p1 * Math.Pow(p2,2) * Math.Pow(p6,2) - 15 * p1 * p2 * Math.Pow(p6,3) - 3 * p1 * Math.Pow(p6,4) + 12 * Math.Pow(p2,4) * p5 + 3 * Math.Pow(p2,4) * p7 + 3 * Math.Pow(p2,3) * p4 * p5 - 3 * Math.Pow(p2,3) * p4 * p7 + 18 * Math.Pow(p2,3) * p5 * p6 + 3 * Math.Pow(p2,3) * p5 * p8 + 12 * Math.Pow(p2,3) * p6 * p7 - 3 * Math.Pow(p2,3) * p7 * p8 - 3 * Math.Pow(p2,2) * Math.Pow(p4,2) * p5 + 3 * Math.Pow(p2,2) * Math.Pow(p4,2) * p7 + 9 * Math.Pow(p2,2) * p4 * p5 * p6 - 6 * Math.Pow(p2,2) * p4 * p5 * p8 - 9 * Math.Pow(p2,2) * p4 * p6 * p7 + 6 * Math.Pow(p2,2) * p4 * p7 * p8 + 12 * Math.Pow(p2,2) * p5 * Math.Pow(p6,2) + 9 * Math.Pow(p2,2) * p5 * p6 * p8 - 3 * Math.Pow(p2,2) * p5 * Math.Pow(p8,2) + 18 * Math.Pow(p2,2) * Math.Pow(p6,2) * p7 - 9 * Math.Pow(p2,2) * p6 * p7 * p8 + 3 * Math.Pow(p2,2) * p7 * Math.Pow(p8,2) + 3 * p2 * Math.Pow(p4,3) * p5 - 3 * p2 * Math.Pow(p4,3) * p7 - 6 * p2 * Math.Pow(p4,2) * p5 * p6 + 9 * p2 * Math.Pow(p4,2) * p5 * p8 + 6 * p2 * Math.Pow(p4,2) * p6 * p7 - 9 * p2 * Math.Pow(p4,2) * p7 * p8 + 9 * p2 * p4 * p5 * Math.Pow(p6,2) - 12 * p2 * p4 * p5 * p6 * p8 + 9 * p2 * p4 * p5 * Math.Pow(p8,2) - 9 * p2 * p4 * Math.Pow(p6,2) * p7 + 12 * p2 * p4 * p6 * p7 * p8 - 9 * p2 * p4 * p7 * Math.Pow(p8,2) + 3 * p2 * p5 * Math.Pow(p6,3) + 9 * p2 * p5 * Math.Pow(p6,2) * p8 - 6 * p2 * p5 * p6 * Math.Pow(p8,2) + 3 * p2 * p5 * Math.Pow(p8,3) + 12 * p2 * Math.Pow(p6,3) * p7 - 9 * p2 * Math.Pow(p6,2) * p7 * p8 + 6 * p2 * p6 * p7 * Math.Pow(p8,2) - 3 * p2 * p7 * Math.Pow(p8,3) + 12 * p3 * Math.Pow(p4,4) + 30 * p3 * Math.Pow(p4,3) * p8 + 30 * p3 * Math.Pow(p4,2) * Math.Pow(p8,2) + 15 * p3 * p4 * Math.Pow(p8,3) + 3 * p3 * Math.Pow(p8,4) - 3 * Math.Pow(p4,4) * p5 - 12 * Math.Pow(p4,4) * p7 + 3 * Math.Pow(p4,3) * p5 * p6 - 12 * Math.Pow(p4,3) * p5 * p8 - 3 * Math.Pow(p4,3) * p6 * p7 - 18 * Math.Pow(p4,3) * p7 * p8 - 3 * Math.Pow(p4,2) * p5 * Math.Pow(p6,2) + 9 * Math.Pow(p4,2) * p5 * p6 * p8 - 18 * Math.Pow(p4,2) * p5 * Math.Pow(p8,2) + 3 * Math.Pow(p4,2) * Math.Pow(p6,2) * p7 - 9 * Math.Pow(p4,2) * p6 * p7 * p8 - 12 * Math.Pow(p4,2) * p7 * Math.Pow(p8,2) + 3 * p4 * p5 * Math.Pow(p6,3) - 6 * p4 * p5 * Math.Pow(p6,2) * p8 + 9 * p4 * p5 * p6 * Math.Pow(p8,2) - 12 * p4 * p5 * Math.Pow(p8,3) - 3 * p4 * Math.Pow(p6,3) * p7 + 6 * p4 * Math.Pow(p6,2) * p7 * p8 - 9 * p4 * p6 * p7 * Math.Pow(p8,2) - 3 * p4 * p7 * Math.Pow(p8,3) + 3 * p5 * Math.Pow(p6,3) * p8 - 3 * p5 * Math.Pow(p6,2) * Math.Pow(p8,2) + 3 * p5 * p6 * Math.Pow(p8,3) - 3 * p5 * Math.Pow(p8,4) + 3 * Math.Pow(p6,4) * p7 - 3 * Math.Pow(p6,3) * p7 * p8 + 3 * Math.Pow(p6,2) * p7 * Math.Pow(p8,2) - 3 * p6 * p7 * Math.Pow(p8,3)) / (60 * Math.Pow(l_e,2));
                        }
                        else if (p2 < 1e-10 && p4 > 1e-10)
                        {
                            fe[2, 0] = (7 * Math.Pow(l_e,4) * p5 + 3 * Math.Pow(l_e,4) * p7 + 10 * Math.Pow(l_e,3) * p1 * p6 - 3 * Math.Pow(l_e,3) * p4 * p5 + 3 * Math.Pow(l_e,3) * p4 * p7 - 3 * Math.Pow(l_e,3) * p5 * p6 - 3 * Math.Pow(l_e,3) * p5 * p8 - 7 * Math.Pow(l_e,3) * p6 * p7 + 3 * Math.Pow(l_e,3) * p7 * p8 - 3 * Math.Pow(l_e,2) * Math.Pow(p4,2) * p5 + 3 * Math.Pow(l_e,2) * Math.Pow(p4,2) * p7 + 4 * Math.Pow(l_e,2) * p4 * p5 * p6 - 6 * Math.Pow(l_e,2) * p4 * p5 * p8 - 4 * Math.Pow(l_e,2) * p4 * p6 * p7 + 6 * Math.Pow(l_e,2) * p4 * p7 * p8 - 3 * Math.Pow(l_e,2) * p5 * Math.Pow(p6,2) + 4 * Math.Pow(l_e,2) * p5 * p6 * p8 - 3 * Math.Pow(l_e,2) * p5 * Math.Pow(p8,2) + 3 * Math.Pow(l_e,2) * Math.Pow(p6,2) * p7 - 4 * Math.Pow(l_e,2) * p6 * p7 * p8 + 3 * Math.Pow(l_e,2) * p7 * Math.Pow(p8,2) - 5 * l_e * p1 * Math.Pow(p6,3) + 15 * l_e * p3 * Math.Pow(p4,3) + 30 * l_e * p3 * Math.Pow(p4,2) * p8 + 20 * l_e * p3 * p4 * Math.Pow(p8,2) + 5 * l_e * p3 * Math.Pow(p8,3) - 3 * l_e * Math.Pow(p4,3) * p5 - 17 * l_e * Math.Pow(p4,3) * p7 + l_e * Math.Pow(p4,2) * p5 * p6 - 9 * l_e * Math.Pow(p4,2) * p5 * p8 - l_e * Math.Pow(p4,2) * p6 * p7 - 21 * l_e * Math.Pow(p4,2) * p7 * p8 + l_e * p4 * p5 * Math.Pow(p6,2) + 2 * l_e * p4 * p5 * p6 * p8 - 9 * l_e * p4 * p5 * Math.Pow(p8,2) - l_e * p4 * Math.Pow(p6,2) * p7 - 2 * l_e * p4 * p6 * p7 * p8 - 11 * l_e * p4 * p7 * Math.Pow(p8,2) + 2 * l_e * p5 * Math.Pow(p6,3) + l_e * p5 * Math.Pow(p6,2) * p8 + l_e * p5 * p6 * Math.Pow(p8,2) - 3 * l_e * p5 * Math.Pow(p8,3) + 3 * l_e * Math.Pow(p6,3) * p7 - l_e * Math.Pow(p6,2) * p7 * p8 - l_e * p6 * p7 * Math.Pow(p8,2) - 2 * l_e * p7 * Math.Pow(p8,3) + 2 * p1 * Math.Pow(p6,4) - 8 * p3 * Math.Pow(p4,4) - 20 * p3 * Math.Pow(p4,3) * p8 - 20 * p3 * Math.Pow(p4,2) * Math.Pow(p8,2) - 10 * p3 * p4 * Math.Pow(p8,3) - 2 * p3 * Math.Pow(p8,4) + 2 * Math.Pow(p4,4) * p5 + 8 * Math.Pow(p4,4) * p7 - 2 * Math.Pow(p4,3) * p5 * p6 + 8 * Math.Pow(p4,3) * p5 * p8 + 2 * Math.Pow(p4,3) * p6 * p7 + 12 * Math.Pow(p4,3) * p7 * p8 + 2 * Math.Pow(p4,2) * p5 * Math.Pow(p6,2) - 6 * Math.Pow(p4,2) * p5 * p6 * p8 + 12 * Math.Pow(p4,2) * p5 * Math.Pow(p8,2) - 2 * Math.Pow(p4,2) * Math.Pow(p6,2) * p7 + 6 * Math.Pow(p4,2) * p6 * p7 * p8 + 8 * Math.Pow(p4,2) * p7 * Math.Pow(p8,2) - 2 * p4 * p5 * Math.Pow(p6,3) + 4 * p4 * p5 * Math.Pow(p6,2) * p8 - 6 * p4 * p5 * p6 * Math.Pow(p8,2) + 8 * p4 * p5 * Math.Pow(p8,3) + 2 * p4 * Math.Pow(p6,3) * p7 - 4 * p4 * Math.Pow(p6,2) * p7 * p8 + 6 * p4 * p6 * p7 * Math.Pow(p8,2) + 2 * p4 * p7 * Math.Pow(p8,3) - 2 * p5 * Math.Pow(p6,3) * p8 + 2 * p5 * Math.Pow(p6,2) * Math.Pow(p8,2) - 2 * p5 * p6 * Math.Pow(p8,3) + 2 * p5 * Math.Pow(p8,4) - 2 * Math.Pow(p6,4) * p7 + 2 * Math.Pow(p6,3) * p7 * p8 - 2 * Math.Pow(p6,2) * p7 * Math.Pow(p8,2) + 2 * p6 * p7 * Math.Pow(p8,3)) / (20 * Math.Pow(l_e,3));
                            fe[4, 0] = (-3 * Math.Pow(l_e,4) * p5 - 2 * Math.Pow(l_e,4) * p7 + 2 * Math.Pow(l_e,3) * p4 * p5 - 2 * Math.Pow(l_e,3) * p4 * p7 - 3 * Math.Pow(l_e,3) * p5 * p6 + 2 * Math.Pow(l_e,3) * p5 * p8 + 3 * Math.Pow(l_e,3) * p6 * p7 - 2 * Math.Pow(l_e,3) * p7 * p8 - 10 * Math.Pow(l_e,2) * p1 * Math.Pow(p6,2) + 2 * Math.Pow(l_e,2) * Math.Pow(p4,2) * p5 - 2 * Math.Pow(l_e,2) * Math.Pow(p4,2) * p7 - Math.Pow(l_e,2) * p4 * p5 * p6 + 4 * Math.Pow(l_e,2) * p4 * p5 * p8 + Math.Pow(l_e,2) * p4 * p6 * p7 - 4 * Math.Pow(l_e,2) * p4 * p7 * p8 + 7 * Math.Pow(l_e,2) * p5 * Math.Pow(p6,2) - Math.Pow(l_e,2) * p5 * p6 * p8 + 2 * Math.Pow(l_e,2) * p5 * Math.Pow(p8,2) + 3 * Math.Pow(l_e,2) * Math.Pow(p6,2) * p7 + Math.Pow(l_e,2) * p6 * p7 * p8 - 2 * Math.Pow(l_e,2) * p7 * Math.Pow(p8,2) + 10 * l_e * p1 * Math.Pow(p6,3) - 15 * l_e * p3 * Math.Pow(p4,3) - 30 * l_e * p3 * Math.Pow(p4,2) * p8 - 20 * l_e * p3 * p4 * Math.Pow(p8,2) - 5 * l_e * p3 * Math.Pow(p8,3) + 2 * l_e * Math.Pow(p4,3) * p5 + 18 * l_e * Math.Pow(p4,3) * p7 + l_e * Math.Pow(p4,2) * p5 * p6 + 6 * l_e * Math.Pow(p4,2) * p5 * p8 - l_e * Math.Pow(p4,2) * p6 * p7 + 24 * l_e * Math.Pow(p4,2) * p7 * p8 - 4 * l_e * p4 * p5 * Math.Pow(p6,2) + 2 * l_e * p4 * p5 * p6 * p8 + 6 * l_e * p4 * p5 * Math.Pow(p8,2) + 4 * l_e * p4 * Math.Pow(p6,2) * p7 - 2 * l_e * p4 * p6 * p7 * p8 + 14 * l_e * p4 * p7 * Math.Pow(p8,2) - 3 * l_e * p5 * Math.Pow(p6,3) - 4 * l_e * p5 * Math.Pow(p6,2) * p8 + l_e * p5 * p6 * Math.Pow(p8,2) + 2 * l_e * p5 * Math.Pow(p8,3) - 7 * l_e * Math.Pow(p6,3) * p7 + 4 * l_e * Math.Pow(p6,2) * p7 * p8 - l_e * p6 * p7 * Math.Pow(p8,2) + 3 * l_e * p7 * Math.Pow(p8,3) - 3 * p1 * Math.Pow(p6,4) + 12 * p3 * Math.Pow(p4,4) + 30 * p3 * Math.Pow(p4,3) * p8 + 30 * p3 * Math.Pow(p4,2) * Math.Pow(p8,2) + 15 * p3 * p4 * Math.Pow(p8,3) + 3 * p3 * Math.Pow(p8,4) - 3 * Math.Pow(p4,4) * p5 - 12 * Math.Pow(p4,4) * p7 + 3 * Math.Pow(p4,3) * p5 * p6 - 12 * Math.Pow(p4,3) * p5 * p8 - 3 * Math.Pow(p4,3) * p6 * p7 - 18 * Math.Pow(p4,3) * p7 * p8 - 3 * Math.Pow(p4,2) * p5 * Math.Pow(p6,2) + 9 * Math.Pow(p4,2) * p5 * p6 * p8 - 18 * Math.Pow(p4,2) * p5 * Math.Pow(p8,2) + 3 * Math.Pow(p4,2) * Math.Pow(p6,2) * p7 - 9 * Math.Pow(p4,2) * p6 * p7 * p8 - 12 * Math.Pow(p4,2) * p7 * Math.Pow(p8,2) + 3 * p4 * p5 * Math.Pow(p6,3) - 6 * p4 * p5 * Math.Pow(p6,2) * p8 + 9 * p4 * p5 * p6 * Math.Pow(p8,2) - 12 * p4 * p5 * Math.Pow(p8,3) - 3 * p4 * Math.Pow(p6,3) * p7 + 6 * p4 * Math.Pow(p6,2) * p7 * p8 - 9 * p4 * p6 * p7 * Math.Pow(p8,2) - 3 * p4 * p7 * Math.Pow(p8,3) + 3 * p5 * Math.Pow(p6,3) * p8 - 3 * p5 * Math.Pow(p6,2) * Math.Pow(p8,2) + 3 * p5 * p6 * Math.Pow(p8,3) - 3 * p5 * Math.Pow(p8,4) + 3 * Math.Pow(p6,4) * p7 - 3 * Math.Pow(p6,3) * p7 * p8 + 3 * Math.Pow(p6,2) * p7 * Math.Pow(p8,2) - 3 * p6 * p7 * Math.Pow(p8,3)) / (60 * Math.Pow(l_e,2));
                            fe[8, 0] = (3 * Math.Pow(l_e,4) * p5 + 7 * Math.Pow(l_e,4) * p7 + 10 * Math.Pow(l_e,3) * p3 * p4 + 10 * Math.Pow(l_e,3) * p3 * p8 - 7 * Math.Pow(l_e,3) * p4 * p5 - 13 * Math.Pow(l_e,3) * p4 * p7 + 3 * Math.Pow(l_e,3) * p5 * p6 - 7 * Math.Pow(l_e,3) * p5 * p8 - 3 * Math.Pow(l_e,3) * p6 * p7 - 3 * Math.Pow(l_e,3) * p7 * p8 + 3 * Math.Pow(l_e,2) * Math.Pow(p4,2) * p5 - 3 * Math.Pow(l_e,2) * Math.Pow(p4,2) * p7 - 4 * Math.Pow(l_e,2) * p4 * p5 * p6 + 6 * Math.Pow(l_e,2) * p4 * p5 * p8 + 4 * Math.Pow(l_e,2) * p4 * p6 * p7 - 6 * Math.Pow(l_e,2) * p4 * p7 * p8 + 3 * Math.Pow(l_e,2) * p5 * Math.Pow(p6,2) - 4 * Math.Pow(l_e,2) * p5 * p6 * p8 + 3 * Math.Pow(l_e,2) * p5 * Math.Pow(p8,2) - 3 * Math.Pow(l_e,2) * Math.Pow(p6,2) * p7 + 4 * Math.Pow(l_e,2) * p6 * p7 * p8 - 3 * Math.Pow(l_e,2) * p7 * Math.Pow(p8,2) + 5 * l_e * p1 * Math.Pow(p6,3) - 15 * l_e * p3 * Math.Pow(p4,3) - 30 * l_e * p3 * Math.Pow(p4,2) * p8 - 20 * l_e * p3 * p4 * Math.Pow(p8,2) - 5 * l_e * p3 * Math.Pow(p8,3) + 3 * l_e * Math.Pow(p4,3) * p5 + 17 * l_e * Math.Pow(p4,3) * p7 - l_e * Math.Pow(p4,2) * p5 * p6 + 9 * l_e * Math.Pow(p4,2) * p5 * p8 + l_e * Math.Pow(p4,2) * p6 * p7 + 21 * l_e * Math.Pow(p4,2) * p7 * p8 - l_e * p4 * p5 * Math.Pow(p6,2) - 2 * l_e * p4 * p5 * p6 * p8 + 9 * l_e * p4 * p5 * Math.Pow(p8,2) + l_e * p4 * Math.Pow(p6,2) * p7 + 2 * l_e * p4 * p6 * p7 * p8 + 11 * l_e * p4 * p7 * Math.Pow(p8,2) - 2 * l_e * p5 * Math.Pow(p6,3) - l_e * p5 * Math.Pow(p6,2) * p8 - l_e * p5 * p6 * Math.Pow(p8,2) + 3 * l_e * p5 * Math.Pow(p8,3) - 3 * l_e * Math.Pow(p6,3) * p7 + l_e * Math.Pow(p6,2) * p7 * p8 + l_e * p6 * p7 * Math.Pow(p8,2) + 2 * l_e * p7 * Math.Pow(p8,3) - 2 * p1 * Math.Pow(p6,4) + 8 * p3 * Math.Pow(p4,4) + 20 * p3 * Math.Pow(p4,3) * p8 + 20 * p3 * Math.Pow(p4,2) * Math.Pow(p8,2) + 10 * p3 * p4 * Math.Pow(p8,3) + 2 * p3 * Math.Pow(p8,4) - 2 * Math.Pow(p4,4) * p5 - 8 * Math.Pow(p4,4) * p7 + 2 * Math.Pow(p4,3) * p5 * p6 - 8 * Math.Pow(p4,3) * p5 * p8 - 2 * Math.Pow(p4,3) * p6 * p7 - 12 * Math.Pow(p4,3) * p7 * p8 - 2 * Math.Pow(p4,2) * p5 * Math.Pow(p6,2) + 6 * Math.Pow(p4,2) * p5 * p6 * p8 - 12 * Math.Pow(p4,2) * p5 * Math.Pow(p8,2) + 2 * Math.Pow(p4,2) * Math.Pow(p6,2) * p7 - 6 * Math.Pow(p4,2) * p6 * p7 * p8 - 8 * Math.Pow(p4,2) * p7 * Math.Pow(p8,2) + 2 * p4 * p5 * Math.Pow(p6,3) - 4 * p4 * p5 * Math.Pow(p6,2) * p8 + 6 * p4 * p5 * p6 * Math.Pow(p8,2) - 8 * p4 * p5 * Math.Pow(p8,3) - 2 * p4 * Math.Pow(p6,3) * p7 + 4 * p4 * Math.Pow(p6,2) * p7 * p8 - 6 * p4 * p6 * p7 * Math.Pow(p8,2) - 2 * p4 * p7 * Math.Pow(p8,3) + 2 * p5 * Math.Pow(p6,3) * p8 - 2 * p5 * Math.Pow(p6,2) * Math.Pow(p8,2) + 2 * p5 * p6 * Math.Pow(p8,3) - 2 * p5 * Math.Pow(p8,4) + 2 * Math.Pow(p6,4) * p7 - 2 * Math.Pow(p6,3) * p7 * p8 + 2 * Math.Pow(p6,2) * p7 * Math.Pow(p8,2) - 2 * p6 * p7 * Math.Pow(p8,3)) / (20 * Math.Pow(l_e,3));
                            fe[10, 0] = (2 * Math.Pow(l_e,4) * p5 + 3 * Math.Pow(l_e,4) * p7 - 3 * Math.Pow(l_e,3) * p4 * p5 + 3 * Math.Pow(l_e,3) * p4 * p7 + 2 * Math.Pow(l_e,3) * p5 * p6 - 3 * Math.Pow(l_e,3) * p5 * p8 - 2 * Math.Pow(l_e,3) * p6 * p7 + 3 * Math.Pow(l_e,3) * p7 * p8 + 20 * Math.Pow(l_e,2) * p3 * Math.Pow(p4,2) + 30 * Math.Pow(l_e,2) * p3 * p4 * p8 + 10 * Math.Pow(l_e,2) * p3 * Math.Pow(p8,2) - 3 * Math.Pow(l_e,2) * Math.Pow(p4,2) * p5 - 27 * Math.Pow(l_e,2) * Math.Pow(p4,2) * p7 - Math.Pow(l_e,2) * p4 * p5 * p6 - 6 * Math.Pow(l_e,2) * p4 * p5 * p8 + Math.Pow(l_e,2) * p4 * p6 * p7 - 24 * Math.Pow(l_e,2) * p4 * p7 * p8 + 2 * Math.Pow(l_e,2) * p5 * Math.Pow(p6,2) - Math.Pow(l_e,2) * p5 * p6 * p8 - 3 * Math.Pow(l_e,2) * p5 * Math.Pow(p8,2) - 2 * Math.Pow(l_e,2) * Math.Pow(p6,2) * p7 + Math.Pow(l_e,2) * p6 * p7 * p8 - 7 * Math.Pow(l_e,2) * p7 * Math.Pow(p8,2) + 5 * l_e * p1 * Math.Pow(p6,3) - 30 * l_e * p3 * Math.Pow(p4,3) - 60 * l_e * p3 * Math.Pow(p4,2) * p8 - 40 * l_e * p3 * p4 * Math.Pow(p8,2) - 10 * l_e * p3 * Math.Pow(p8,3) + 7 * l_e * Math.Pow(p4,3) * p5 + 33 * l_e * Math.Pow(p4,3) * p7 - 4 * l_e * Math.Pow(p4,2) * p5 * p6 + 21 * l_e * Math.Pow(p4,2) * p5 * p8 + 4 * l_e * Math.Pow(p4,2) * p6 * p7 + 39 * l_e * Math.Pow(p4,2) * p7 * p8 + l_e * p4 * p5 * Math.Pow(p6,2) - 8 * l_e * p4 * p5 * p6 * p8 + 21 * l_e * p4 * p5 * Math.Pow(p8,2) - l_e * p4 * Math.Pow(p6,2) * p7 + 8 * l_e * p4 * p6 * p7 * p8 + 19 * l_e * p4 * p7 * Math.Pow(p8,2) - 3 * l_e * p5 * Math.Pow(p6,3) + l_e * p5 * Math.Pow(p6,2) * p8 - 4 * l_e * p5 * p6 * Math.Pow(p8,2) + 7 * l_e * p5 * Math.Pow(p8,3) - 2 * l_e * Math.Pow(p6,3) * p7 - l_e * Math.Pow(p6,2) * p7 * p8 + 4 * l_e * p6 * p7 * Math.Pow(p8,2) + 3 * l_e * p7 * Math.Pow(p8,3) - 3 * p1 * Math.Pow(p6,4) + 12 * p3 * Math.Pow(p4,4) + 30 * p3 * Math.Pow(p4,3) * p8 + 30 * p3 * Math.Pow(p4,2) * Math.Pow(p8,2) + 15 * p3 * p4 * Math.Pow(p8,3) + 3 * p3 * Math.Pow(p8,4) - 3 * Math.Pow(p4,4) * p5 - 12 * Math.Pow(p4,4) * p7 + 3 * Math.Pow(p4,3) * p5 * p6 - 12 * Math.Pow(p4,3) * p5 * p8 - 3 * Math.Pow(p4,3) * p6 * p7 - 18 * Math.Pow(p4,3) * p7 * p8 - 3 * Math.Pow(p4,2) * p5 * Math.Pow(p6,2) + 9 * Math.Pow(p4,2) * p5 * p6 * p8 - 18 * Math.Pow(p4,2) * p5 * Math.Pow(p8,2) + 3 * Math.Pow(p4,2) * Math.Pow(p6,2) * p7 - 9 * Math.Pow(p4,2) * p6 * p7 * p8 - 12 * Math.Pow(p4,2) * p7 * Math.Pow(p8,2) + 3 * p4 * p5 * Math.Pow(p6,3) - 6 * p4 * p5 * Math.Pow(p6,2) * p8 + 9 * p4 * p5 * p6 * Math.Pow(p8,2) - 12 * p4 * p5 * Math.Pow(p8,3) - 3 * p4 * Math.Pow(p6,3) * p7 + 6 * p4 * Math.Pow(p6,2) * p7 * p8 - 9 * p4 * p6 * p7 * Math.Pow(p8,2) - 3 * p4 * p7 * Math.Pow(p8,3) + 3 * p5 * Math.Pow(p6,3) * p8 - 3 * p5 * Math.Pow(p6,2) * Math.Pow(p8,2) + 3 * p5 * p6 * Math.Pow(p8,3) - 3 * p5 * Math.Pow(p8,4) + 3 * Math.Pow(p6,4) * p7 - 3 * Math.Pow(p6,3) * p7 * p8 + 3 * Math.Pow(p6,2) * p7 * Math.Pow(p8,2) - 3 * p6 * p7 * Math.Pow(p8,3)) / (60 * Math.Pow(l_e,2));
                        }
                        else if (p2 > 1e-10 && p4 < 1e-10)
                        {
                            fe[2, 0] = (7 * Math.Pow(l_e,4) * p5 + 3 * Math.Pow(l_e,4) * p7 + 10 * Math.Pow(l_e,3) * p1 * p2 + 10 * Math.Pow(l_e,3) * p1 * p6 - 13 * Math.Pow(l_e,3) * p2 * p5 - 7 * Math.Pow(l_e,3) * p2 * p7 - 3 * Math.Pow(l_e,3) * p5 * p6 - 3 * Math.Pow(l_e,3) * p5 * p8 - 7 * Math.Pow(l_e,3) * p6 * p7 + 3 * Math.Pow(l_e,3) * p7 * p8 - 3 * Math.Pow(l_e,2) * Math.Pow(p2,2) * p5 + 3 * Math.Pow(l_e,2) * Math.Pow(p2,2) * p7 - 6 * Math.Pow(l_e,2) * p2 * p5 * p6 + 4 * Math.Pow(l_e,2) * p2 * p5 * p8 + 6 * Math.Pow(l_e,2) * p2 * p6 * p7 - 4 * Math.Pow(l_e,2) * p2 * p7 * p8 - 3 * Math.Pow(l_e,2) * p5 * Math.Pow(p6,2) + 4 * Math.Pow(l_e,2) * p5 * p6 * p8 - 3 * Math.Pow(l_e,2) * p5 * Math.Pow(p8,2) + 3 * Math.Pow(l_e,2) * Math.Pow(p6,2) * p7 - 4 * Math.Pow(l_e,2) * p6 * p7 * p8 + 3 * Math.Pow(l_e,2) * p7 * Math.Pow(p8,2) - 15 * l_e * p1 * Math.Pow(p2,3) - 30 * l_e * p1 * Math.Pow(p2,2) * p6 - 20 * l_e * p1 * p2 * Math.Pow(p6,2) - 5 * l_e * p1 * Math.Pow(p6,3) + 17 * l_e * Math.Pow(p2,3) * p5 + 3 * l_e * Math.Pow(p2,3) * p7 + 21 * l_e * Math.Pow(p2,2) * p5 * p6 + l_e * Math.Pow(p2,2) * p5 * p8 + 9 * l_e * Math.Pow(p2,2) * p6 * p7 - l_e * Math.Pow(p2,2) * p7 * p8 + 11 * l_e * p2 * p5 * Math.Pow(p6,2) + 2 * l_e * p2 * p5 * p6 * p8 + l_e * p2 * p5 * Math.Pow(p8,2) + 9 * l_e * p2 * Math.Pow(p6,2) * p7 - 2 * l_e * p2 * p6 * p7 * p8 - l_e * p2 * p7 * Math.Pow(p8,2) + 5 * l_e * p3 * Math.Pow(p8,3) + 2 * l_e * p5 * Math.Pow(p6,3) + l_e * p5 * Math.Pow(p6,2) * p8 + l_e * p5 * p6 * Math.Pow(p8,2) - 3 * l_e * p5 * Math.Pow(p8,3) + 3 * l_e * Math.Pow(p6,3) * p7 - l_e * Math.Pow(p6,2) * p7 * p8 - l_e * p6 * p7 * Math.Pow(p8,2) - 2 * l_e * p7 * Math.Pow(p8,3) + 8 * p1 * Math.Pow(p2,4) + 20 * p1 * Math.Pow(p2,3) * p6 + 20 * p1 * Math.Pow(p2,2) * Math.Pow(p6,2) + 10 * p1 * p2 * Math.Pow(p6,3) + 2 * p1 * Math.Pow(p6,4) - 8 * Math.Pow(p2,4) * p5 - 2 * Math.Pow(p2,4) * p7 - 12 * Math.Pow(p2,3) * p5 * p6 - 2 * Math.Pow(p2,3) * p5 * p8 - 8 * Math.Pow(p2,3) * p6 * p7 + 2 * Math.Pow(p2,3) * p7 * p8 - 8 * Math.Pow(p2,2) * p5 * Math.Pow(p6,2) - 6 * Math.Pow(p2,2) * p5 * p6 * p8 + 2 * Math.Pow(p2,2) * p5 * Math.Pow(p8,2) - 12 * Math.Pow(p2,2) * Math.Pow(p6,2) * p7 + 6 * Math.Pow(p2,2) * p6 * p7 * p8 - 2 * Math.Pow(p2,2) * p7 * Math.Pow(p8,2) - 2 * p2 * p5 * Math.Pow(p6,3) - 6 * p2 * p5 * Math.Pow(p6,2) * p8 + 4 * p2 * p5 * p6 * Math.Pow(p8,2) - 2 * p2 * p5 * Math.Pow(p8,3) - 8 * p2 * Math.Pow(p6,3) * p7 + 6 * p2 * Math.Pow(p6,2) * p7 * p8 - 4 * p2 * p6 * p7 * Math.Pow(p8,2) + 2 * p2 * p7 * Math.Pow(p8,3) - 2 * p3 * Math.Pow(p8,4) - 2 * p5 * Math.Pow(p6,3) * p8 + 2 * p5 * Math.Pow(p6,2) * Math.Pow(p8,2) - 2 * p5 * p6 * Math.Pow(p8,3) + 2 * p5 * Math.Pow(p8,4) - 2 * Math.Pow(p6,4) * p7 + 2 * Math.Pow(p6,3) * p7 * p8 - 2 * Math.Pow(p6,2) * p7 * Math.Pow(p8,2) + 2 * p6 * p7 * Math.Pow(p8,3)) / (20 * Math.Pow(l_e,3));
                            fe[4, 0] = (-3 * Math.Pow(l_e,4) * p5 - 2 * Math.Pow(l_e,4) * p7 - 3 * Math.Pow(l_e,3) * p2 * p5 + 3 * Math.Pow(l_e,3) * p2 * p7 - 3 * Math.Pow(l_e,3) * p5 * p6 + 2 * Math.Pow(l_e,3) * p5 * p8 + 3 * Math.Pow(l_e,3) * p6 * p7 - 2 * Math.Pow(l_e,3) * p7 * p8 - 20 * Math.Pow(l_e,2) * p1 * Math.Pow(p2,2) - 30 * Math.Pow(l_e,2) * p1 * p2 * p6 - 10 * Math.Pow(l_e,2) * p1 * Math.Pow(p6,2) + 27 * Math.Pow(l_e,2) * Math.Pow(p2,2) * p5 + 3 * Math.Pow(l_e,2) * Math.Pow(p2,2) * p7 + 24 * Math.Pow(l_e,2) * p2 * p5 * p6 - Math.Pow(l_e,2) * p2 * p5 * p8 + 6 * Math.Pow(l_e,2) * p2 * p6 * p7 + Math.Pow(l_e,2) * p2 * p7 * p8 + 7 * Math.Pow(l_e,2) * p5 * Math.Pow(p6,2) - Math.Pow(l_e,2) * p5 * p6 * p8 + 2 * Math.Pow(l_e,2) * p5 * Math.Pow(p8,2) + 3 * Math.Pow(l_e,2) * Math.Pow(p6,2) * p7 + Math.Pow(l_e,2) * p6 * p7 * p8 - 2 * Math.Pow(l_e,2) * p7 * Math.Pow(p8,2) + 30 * l_e * p1 * Math.Pow(p2,3) + 60 * l_e * p1 * Math.Pow(p2,2) * p6 + 40 * l_e * p1 * p2 * Math.Pow(p6,2) + 10 * l_e * p1 * Math.Pow(p6,3) - 33 * l_e * Math.Pow(p2,3) * p5 - 7 * l_e * Math.Pow(p2,3) * p7 - 39 * l_e * Math.Pow(p2,2) * p5 * p6 - 4 * l_e * Math.Pow(p2,2) * p5 * p8 - 21 * l_e * Math.Pow(p2,2) * p6 * p7 + 4 * l_e * Math.Pow(p2,2) * p7 * p8 - 19 * l_e * p2 * p5 * Math.Pow(p6,2) - 8 * l_e * p2 * p5 * p6 * p8 + l_e * p2 * p5 * Math.Pow(p8,2) - 21 * l_e * p2 * Math.Pow(p6,2) * p7 + 8 * l_e * p2 * p6 * p7 * p8 - l_e * p2 * p7 * Math.Pow(p8,2) - 5 * l_e * p3 * Math.Pow(p8,3) - 3 * l_e * p5 * Math.Pow(p6,3) - 4 * l_e * p5 * Math.Pow(p6,2) * p8 + l_e * p5 * p6 * Math.Pow(p8,2) + 2 * l_e * p5 * Math.Pow(p8,3) - 7 * l_e * Math.Pow(p6,3) * p7 + 4 * l_e * Math.Pow(p6,2) * p7 * p8 - l_e * p6 * p7 * Math.Pow(p8,2) + 3 * l_e * p7 * Math.Pow(p8,3) - 12 * p1 * Math.Pow(p2,4) - 30 * p1 * Math.Pow(p2,3) * p6 - 30 * p1 * Math.Pow(p2,2) * Math.Pow(p6,2) - 15 * p1 * p2 * Math.Pow(p6,3) - 3 * p1 * Math.Pow(p6,4) + 12 * Math.Pow(p2,4) * p5 + 3 * Math.Pow(p2,4) * p7 + 18 * Math.Pow(p2,3) * p5 * p6 + 3 * Math.Pow(p2,3) * p5 * p8 + 12 * Math.Pow(p2,3) * p6 * p7 - 3 * Math.Pow(p2,3) * p7 * p8 + 12 * Math.Pow(p2,2) * p5 * Math.Pow(p6,2) + 9 * Math.Pow(p2,2) * p5 * p6 * p8 - 3 * Math.Pow(p2,2) * p5 * Math.Pow(p8,2) + 18 * Math.Pow(p2,2) * Math.Pow(p6,2) * p7 - 9 * Math.Pow(p2,2) * p6 * p7 * p8 + 3 * Math.Pow(p2,2) * p7 * Math.Pow(p8,2) + 3 * p2 * p5 * Math.Pow(p6,3) + 9 * p2 * p5 * Math.Pow(p6,2) * p8 - 6 * p2 * p5 * p6 * Math.Pow(p8,2) + 3 * p2 * p5 * Math.Pow(p8,3) + 12 * p2 * Math.Pow(p6,3) * p7 - 9 * p2 * Math.Pow(p6,2) * p7 * p8 + 6 * p2 * p6 * p7 * Math.Pow(p8,2) - 3 * p2 * p7 * Math.Pow(p8,3) + 3 * p3 * Math.Pow(p8,4) + 3 * p5 * Math.Pow(p6,3) * p8 - 3 * p5 * Math.Pow(p6,2) * Math.Pow(p8,2) + 3 * p5 * p6 * Math.Pow(p8,3) - 3 * p5 * Math.Pow(p8,4) + 3 * Math.Pow(p6,4) * p7 - 3 * Math.Pow(p6,3) * p7 * p8 + 3 * Math.Pow(p6,2) * p7 * Math.Pow(p8,2) - 3 * p6 * p7 * Math.Pow(p8,3)) / (60 * Math.Pow(l_e,2));
                            fe[8, 0] = (3 * Math.Pow(l_e,4) * p5 + 7 * Math.Pow(l_e,4) * p7 + 3 * Math.Pow(l_e,3) * p2 * p5 - 3 * Math.Pow(l_e,3) * p2 * p7 + 10 * Math.Pow(l_e,3) * p3 * p8 + 3 * Math.Pow(l_e,3) * p5 * p6 - 7 * Math.Pow(l_e,3) * p5 * p8 - 3 * Math.Pow(l_e,3) * p6 * p7 - 3 * Math.Pow(l_e,3) * p7 * p8 + 3 * Math.Pow(l_e,2) * Math.Pow(p2,2) * p5 - 3 * Math.Pow(l_e,2) * Math.Pow(p2,2) * p7 + 6 * Math.Pow(l_e,2) * p2 * p5 * p6 - 4 * Math.Pow(l_e,2) * p2 * p5 * p8 - 6 * Math.Pow(l_e,2) * p2 * p6 * p7 + 4 * Math.Pow(l_e,2) * p2 * p7 * p8 + 3 * Math.Pow(l_e,2) * p5 * Math.Pow(p6,2) - 4 * Math.Pow(l_e,2) * p5 * p6 * p8 + 3 * Math.Pow(l_e,2) * p5 * Math.Pow(p8,2) - 3 * Math.Pow(l_e,2) * Math.Pow(p6,2) * p7 + 4 * Math.Pow(l_e,2) * p6 * p7 * p8 - 3 * Math.Pow(l_e,2) * p7 * Math.Pow(p8,2) + 15 * l_e * p1 * Math.Pow(p2,3) + 30 * l_e * p1 * Math.Pow(p2,2) * p6 + 20 * l_e * p1 * p2 * Math.Pow(p6,2) + 5 * l_e * p1 * Math.Pow(p6,3) - 17 * l_e * Math.Pow(p2,3) * p5 - 3 * l_e * Math.Pow(p2,3) * p7 - 21 * l_e * Math.Pow(p2,2) * p5 * p6 - l_e * Math.Pow(p2,2) * p5 * p8 - 9 * l_e * Math.Pow(p2,2) * p6 * p7 + l_e * Math.Pow(p2,2) * p7 * p8 - 11 * l_e * p2 * p5 * Math.Pow(p6,2) - 2 * l_e * p2 * p5 * p6 * p8 - l_e * p2 * p5 * Math.Pow(p8,2) - 9 * l_e * p2 * Math.Pow(p6,2) * p7 + 2 * l_e * p2 * p6 * p7 * p8 + l_e * p2 * p7 * Math.Pow(p8,2) - 5 * l_e * p3 * Math.Pow(p8,3) - 2 * l_e * p5 * Math.Pow(p6,3) - l_e * p5 * Math.Pow(p6,2) * p8 - l_e * p5 * p6 * Math.Pow(p8,2) + 3 * l_e * p5 * Math.Pow(p8,3) - 3 * l_e * Math.Pow(p6,3) * p7 + l_e * Math.Pow(p6,2) * p7 * p8 + l_e * p6 * p7 * Math.Pow(p8,2) + 2 * l_e * p7 * Math.Pow(p8,3) - 8 * p1 * Math.Pow(p2,4) - 20 * p1 * Math.Pow(p2,3) * p6 - 20 * p1 * Math.Pow(p2,2) * Math.Pow(p6,2) - 10 * p1 * p2 * Math.Pow(p6,3) - 2 * p1 * Math.Pow(p6,4) + 8 * Math.Pow(p2,4) * p5 + 2 * Math.Pow(p2,4) * p7 + 12 * Math.Pow(p2,3) * p5 * p6 + 2 * Math.Pow(p2,3) * p5 * p8 + 8 * Math.Pow(p2,3) * p6 * p7 - 2 * Math.Pow(p2,3) * p7 * p8 + 8 * Math.Pow(p2,2) * p5 * Math.Pow(p6,2) + 6 * Math.Pow(p2,2) * p5 * p6 * p8 - 2 * Math.Pow(p2,2) * p5 * Math.Pow(p8,2) + 12 * Math.Pow(p2,2) * Math.Pow(p6,2) * p7 - 6 * Math.Pow(p2,2) * p6 * p7 * p8 + 2 * Math.Pow(p2,2) * p7 * Math.Pow(p8,2) + 2 * p2 * p5 * Math.Pow(p6,3) + 6 * p2 * p5 * Math.Pow(p6,2) * p8 - 4 * p2 * p5 * p6 * Math.Pow(p8,2) + 2 * p2 * p5 * Math.Pow(p8,3) + 8 * p2 * Math.Pow(p6,3) * p7 - 6 * p2 * Math.Pow(p6,2) * p7 * p8 + 4 * p2 * p6 * p7 * Math.Pow(p8,2) - 2 * p2 * p7 * Math.Pow(p8,3) + 2 * p3 * Math.Pow(p8,4) + 2 * p5 * Math.Pow(p6,3) * p8 - 2 * p5 * Math.Pow(p6,2) * Math.Pow(p8,2) + 2 * p5 * p6 * Math.Pow(p8,3) - 2 * p5 * Math.Pow(p8,4) + 2 * Math.Pow(p6,4) * p7 - 2 * Math.Pow(p6,3) * p7 * p8 + 2 * Math.Pow(p6,2) * p7 * Math.Pow(p8,2) - 2 * p6 * p7 * Math.Pow(p8,3)) / (20 * Math.Pow(l_e,3));
                            fe[10, 0] = (2 * Math.Pow(l_e,4) * p5 + 3 * Math.Pow(l_e,4) * p7 + 2 * Math.Pow(l_e,3) * p2 * p5 - 2 * Math.Pow(l_e,3) * p2 * p7 + 2 * Math.Pow(l_e,3) * p5 * p6 - 3 * Math.Pow(l_e,3) * p5 * p8 - 2 * Math.Pow(l_e,3) * p6 * p7 + 3 * Math.Pow(l_e,3) * p7 * p8 + 2 * Math.Pow(l_e,2) * Math.Pow(p2,2) * p5 - 2 * Math.Pow(l_e,2) * Math.Pow(p2,2) * p7 + 4 * Math.Pow(l_e,2) * p2 * p5 * p6 - Math.Pow(l_e,2) * p2 * p5 * p8 - 4 * Math.Pow(l_e,2) * p2 * p6 * p7 + Math.Pow(l_e,2) * p2 * p7 * p8 + 10 * Math.Pow(l_e,2) * p3 * Math.Pow(p8,2) + 2 * Math.Pow(l_e,2) * p5 * Math.Pow(p6,2) - Math.Pow(l_e,2) * p5 * p6 * p8 - 3 * Math.Pow(l_e,2) * p5 * Math.Pow(p8,2) - 2 * Math.Pow(l_e,2) * Math.Pow(p6,2) * p7 + Math.Pow(l_e,2) * p6 * p7 * p8 - 7 * Math.Pow(l_e,2) * p7 * Math.Pow(p8,2) + 15 * l_e * p1 * Math.Pow(p2,3) + 30 * l_e * p1 * Math.Pow(p2,2) * p6 + 20 * l_e * p1 * p2 * Math.Pow(p6,2) + 5 * l_e * p1 * Math.Pow(p6,3) - 18 * l_e * Math.Pow(p2,3) * p5 - 2 * l_e * Math.Pow(p2,3) * p7 - 24 * l_e * Math.Pow(p2,2) * p5 * p6 + l_e * Math.Pow(p2,2) * p5 * p8 - 6 * l_e * Math.Pow(p2,2) * p6 * p7 - l_e * Math.Pow(p2,2) * p7 * p8 - 14 * l_e * p2 * p5 * Math.Pow(p6,2) + 2 * l_e * p2 * p5 * p6 * p8 - 4 * l_e * p2 * p5 * Math.Pow(p8,2) - 6 * l_e * p2 * Math.Pow(p6,2) * p7 - 2 * l_e * p2 * p6 * p7 * p8 + 4 * l_e * p2 * p7 * Math.Pow(p8,2) - 10 * l_e * p3 * Math.Pow(p8,3) - 3 * l_e * p5 * Math.Pow(p6,3) + l_e * p5 * Math.Pow(p6,2) * p8 - 4 * l_e * p5 * p6 * Math.Pow(p8,2) + 7 * l_e * p5 * Math.Pow(p8,3) - 2 * l_e * Math.Pow(p6,3) * p7 - l_e * Math.Pow(p6,2) * p7 * p8 + 4 * l_e * p6 * p7 * Math.Pow(p8,2) + 3 * l_e * p7 * Math.Pow(p8,3) - 12 * p1 * Math.Pow(p2,4) - 30 * p1 * Math.Pow(p2,3) * p6 - 30 * p1 * Math.Pow(p2,2) * Math.Pow(p6,2) - 15 * p1 * p2 * Math.Pow(p6,3) - 3 * p1 * Math.Pow(p6,4) + 12 * Math.Pow(p2,4) * p5 + 3 * Math.Pow(p2,4) * p7 + 18 * Math.Pow(p2,3) * p5 * p6 + 3 * Math.Pow(p2,3) * p5 * p8 + 12 * Math.Pow(p2,3) * p6 * p7 - 3 * Math.Pow(p2,3) * p7 * p8 + 12 * Math.Pow(p2,2) * p5 * Math.Pow(p6,2) + 9 * Math.Pow(p2,2) * p5 * p6 * p8 - 3 * Math.Pow(p2,2) * p5 * Math.Pow(p8,2) + 18 * Math.Pow(p2,2) * Math.Pow(p6,2) * p7 - 9 * Math.Pow(p2,2) * p6 * p7 * p8 + 3 * Math.Pow(p2,2) * p7 * Math.Pow(p8,2) + 3 * p2 * p5 * Math.Pow(p6,3) + 9 * p2 * p5 * Math.Pow(p6,2) * p8 - 6 * p2 * p5 * p6 * Math.Pow(p8,2) + 3 * p2 * p5 * Math.Pow(p8,3) + 12 * p2 * Math.Pow(p6,3) * p7 - 9 * p2 * Math.Pow(p6,2) * p7 * p8 + 6 * p2 * p6 * p7 * Math.Pow(p8,2) - 3 * p2 * p7 * Math.Pow(p8,3) + 3 * p3 * Math.Pow(p8,4) + 3 * p5 * Math.Pow(p6,3) * p8 - 3 * p5 * Math.Pow(p6,2) * Math.Pow(p8,2) + 3 * p5 * p6 * Math.Pow(p8,3) - 3 * p5 * Math.Pow(p8,4) + 3 * Math.Pow(p6,4) * p7 - 3 * Math.Pow(p6,3) * p7 * p8 + 3 * Math.Pow(p6,2) * p7 * Math.Pow(p8,2) - 3 * p6 * p7 * Math.Pow(p8,3)) / (60 * Math.Pow(l_e,2));
                        }
                        else
                        {
                            fe[2, 0] = (Math.Pow(l_e,3) * (7 * l_e * p5 + 3 * l_e * p7 + 10 * p1 * p6 - 3 * p5 * p6 - 3 * p5 * p8 - 7 * p6 * p7 + 3 * p7 * p8) + Math.Pow(l_e,2) * (-3 * p5 * Math.Pow(p6,2) + 4 * p5 * p6 * p8 - 3 * p5 * Math.Pow(p8,2) + 3 * Math.Pow(p6,2) * p7 - 4 * p6 * p7 * p8 + 3 * p7 * Math.Pow(p8,2)) + l_e * (-5 * p1 * Math.Pow(p6,3) + 5 * p3 * Math.Pow(p8,3) + 2 * p5 * Math.Pow(p6,3) + p5 * Math.Pow(p6,2) * p8 + p5 * p6 * Math.Pow(p8,2) - 3 * p5 * Math.Pow(p8,3) + 3 * Math.Pow(p6,3) * p7 - Math.Pow(p6,2) * p7 * p8 - p6 * p7 * Math.Pow(p8,2) - 2 * p7 * Math.Pow(p8,3)) + 2 * p1 * Math.Pow(p6,4) - 2 * p3 * Math.Pow(p8,4) - 2 * p5 * Math.Pow(p6,3) * p8 + 2 * p5 * Math.Pow(p6,2) * Math.Pow(p8,2) - 2 * p5 * p6 * Math.Pow(p8,3) + 2 * p5 * Math.Pow(p8,4) - 2 * Math.Pow(p6,4) * p7 + 2 * Math.Pow(p6,3) * p7 * p8 - 2 * Math.Pow(p6,2) * p7 * Math.Pow(p8,2) + 2 * p6 * p7 * Math.Pow(p8,3)) / (20 * Math.Pow(l_e,3));
                            fe[4, 0] = (Math.Pow(l_e,2) * (-3 * Math.Pow(l_e,2) * p5 - 2 * Math.Pow(l_e,2) * p7 - 3 * l_e * p5 * p6 + 2 * l_e * p5 * p8 + 3 * l_e * p6 * p7 - 2 * l_e * p7 * p8 - 10 * p1 * Math.Pow(p6,2) + 7 * p5 * Math.Pow(p6,2) - p5 * p6 * p8 + 2 * p5 * Math.Pow(p8,2) + 3 * Math.Pow(p6,2) * p7 + p6 * p7 * p8 - 2 * p7 * Math.Pow(p8,2)) + l_e * (10 * p1 * Math.Pow(p6,3) - 5 * p3 * Math.Pow(p8,3) - 3 * p5 * Math.Pow(p6,3) - 4 * p5 * Math.Pow(p6,2) * p8 + p5 * p6 * Math.Pow(p8,2) + 2 * p5 * Math.Pow(p8,3) - 7 * Math.Pow(p6,3) * p7 + 4 * Math.Pow(p6,2) * p7 * p8 - p6 * p7 * Math.Pow(p8,2) + 3 * p7 * Math.Pow(p8,3)) - 3 * p1 * Math.Pow(p6,4) + 3 * p3 * Math.Pow(p8,4) + 3 * p5 * Math.Pow(p6,3) * p8 - 3 * p5 * Math.Pow(p6,2) * Math.Pow(p8,2) + 3 * p5 * p6 * Math.Pow(p8,3) - 3 * p5 * Math.Pow(p8,4) + 3 * Math.Pow(p6,4) * p7 - 3 * Math.Pow(p6,3) * p7 * p8 + 3 * Math.Pow(p6,2) * p7 * Math.Pow(p8,2) - 3 * p6 * p7 * Math.Pow(p8,3)) / (60 * Math.Pow(l_e,2));
                            fe[8, 0] = (3 * Math.Pow(l_e,4) * p5 + 7 * Math.Pow(l_e,4) * p7 + 10 * Math.Pow(l_e,3) * p3 * p8 + 3 * Math.Pow(l_e,3) * p5 * p6 - 7 * Math.Pow(l_e,3) * p5 * p8 - 3 * Math.Pow(l_e,3) * p6 * p7 - 3 * Math.Pow(l_e,3) * p7 * p8 + 3 * Math.Pow(l_e,2) * p5 * Math.Pow(p6,2) - 4 * Math.Pow(l_e,2) * p5 * p6 * p8 + 3 * Math.Pow(l_e,2) * p5 * Math.Pow(p8,2) - 3 * Math.Pow(l_e,2) * Math.Pow(p6,2) * p7 + 4 * Math.Pow(l_e,2) * p6 * p7 * p8 - 3 * Math.Pow(l_e,2) * p7 * Math.Pow(p8,2) + 5 * l_e * p1 * Math.Pow(p6,3) - 5 * l_e * p3 * Math.Pow(p8,3) - 2 * l_e * p5 * Math.Pow(p6,3) - l_e * p5 * Math.Pow(p6,2) * p8 - l_e * p5 * p6 * Math.Pow(p8,2) + 3 * l_e * p5 * Math.Pow(p8,3) - 3 * l_e * Math.Pow(p6,3) * p7 + l_e * Math.Pow(p6,2) * p7 * p8 + l_e * p6 * p7 * Math.Pow(p8,2) + 2 * l_e * p7 * Math.Pow(p8,3) - 2 * p1 * Math.Pow(p6,4) + 2 * p3 * Math.Pow(p8,4) + 2 * p5 * Math.Pow(p6,3) * p8 - 2 * p5 * Math.Pow(p6,2) * Math.Pow(p8,2) + 2 * p5 * p6 * Math.Pow(p8,3) - 2 * p5 * Math.Pow(p8,4) + 2 * Math.Pow(p6,4) * p7 - 2 * Math.Pow(p6,3) * p7 * p8 + 2 * Math.Pow(p6,2) * p7 * Math.Pow(p8,2) - 2 * p6 * p7 * Math.Pow(p8,3)) / (20 * Math.Pow(l_e,3));
                            fe[10, 0] = (-2 * Math.Pow(l_e,5) * p5 - 3 * Math.Pow(l_e,5) * p7 + 5 * Math.Pow(l_e,4) * p5 * p8 + 5 * Math.Pow(l_e,4) * p6 * p7 - 10 * Math.Pow(l_e,3) * p3 * Math.Pow(p8,2) + 10 * Math.Pow(l_e,3) * p7 * Math.Pow(p8,2) - 5 * Math.Pow(l_e,2) * p1 * Math.Pow(p6,3) + 10 * Math.Pow(l_e,2) * p3 * p6 * Math.Pow(p8,2) + 20 * Math.Pow(l_e,2) * p3 * Math.Pow(p8,3) + 5 * Math.Pow(l_e,2) * p5 * Math.Pow(p6,3) - 10 * Math.Pow(l_e,2) * p5 * Math.Pow(p8,3) - 10 * Math.Pow(l_e,2) * p6 * p7 * Math.Pow(p8,2) - 10 * Math.Pow(l_e,2) * p7 * Math.Pow(p8,3) + 8 * l_e * p1 * Math.Pow(p6,4) + 5 * l_e * p1 * Math.Pow(p6,3) * p8 - 10 * l_e * p3 * p6 * Math.Pow(p8,3) - 13 * l_e * p3 * Math.Pow(p8,4) - 3 * l_e * p5 * Math.Pow(p6,4) - 5 * l_e * p5 * Math.Pow(p6,3) * p8 + 10 * l_e * p5 * Math.Pow(p8,4) - 5 * l_e * Math.Pow(p6,4) * p7 + 10 * l_e * p6 * p7 * Math.Pow(p8,3) + 3 * l_e * p7 * Math.Pow(p8,4) - 3 * p1 * Math.Pow(p6,5) - 3 * p1 * Math.Pow(p6,4) * p8 + 3 * p3 * p6 * Math.Pow(p8,4) + 3 * p3 * Math.Pow(p8,5) + 3 * p5 * Math.Pow(p6,4) * p8 - 3 * p5 * Math.Pow(p8,5) + 3 * Math.Pow(p6,5) * p7 - 3 * p6 * p7 * Math.Pow(p8,4)) / (60 * Math.Pow(l_e,2) * (-l_e + p6 + p8));
                        }
                        if (l_e / 2.0 < p2)
                        {
                            fc[2, 0] = fe[2, 0] - Math.Pow(l_e, 2) * p1 / (8 * p2);
                            fc[4, 0] = -(fe[4, 0] - Math.Pow(l_e, 3) * p1 / (48 * p2) + fe[2, 0] * l_e / 2.0);
                        }
                        else if (l_e / 2.0 < p2 + p6)
                        {
                            if (Math.Abs(p6) > 1e-10)
                            {
                                if (Math.Abs(-l_e + p2 + p4) < 1e-10) { System.Console.WriteLine("***warning s6-1***"); }
                                fc[2, 0] = fe[2, 0] - (Math.Pow(l_e, 2) * (-p1 + p5) + 4 * l_e * (p1 * p2 + p1 * p6 - p2 * p5) + 4 * p1 * p2 * p6 + 4 * Math.Pow(p2, 2) * (p1 - p5) - 8 * p2 * (p1 * p2 + p1 * p6 - p2 * p5)) / (8 * p6);
                                fc[4, 0] = -(fe[4, 0] - (-Math.Pow(l_e, 3) * p1 + Math.Pow(l_e, 3) * p5 + 6 * Math.Pow(l_e, 2) * p1 * p2 + 6 * Math.Pow(l_e, 2) * p1 * p6 - 6 * Math.Pow(l_e, 2) * p2 * p5 - 12 * l_e * p1 * Math.Pow(p2, 2) - 12 * l_e * p1 * p2 * p6 + 12 * l_e * Math.Pow(p2, 2) * p5 + 8 * p1 * Math.Pow(p2, 3) + 8 * p1 * Math.Pow(p2, 2) * p6 - 8 * Math.Pow(p2, 3) * p5) / (48 * p6) + fe[2, 0] * l_e / 2.0);
                            }
                        }
                        else if (l_e / 2.0 < l_e - p4 - p8)
                        {
                            if (Math.Abs(p6) > 1e-10)
                            {
                                fc[2, 0] = (-Math.Pow(l_e,2) * p6 * (p5 - p7) + 4 * p2 * (2 * p1 * p2 + 2 * p1 * p6 - 2 * p2 * p5 + p2 * (-p1 + p5)) * (-l_e + p2 + p4 + p6 + p8) + 8 * Math.Pow(p6,2) * (-l_e * p5 + p2 * p7 + p4 * p5 + p5 * p8 + p6 * p7) + 4 * p6 * (-l_e * (-l_e * p5 + p2 * p7 + p4 * p5 + p5 * p8 + p6 * p7) + Math.Pow(p6,2) * (p5 - p7)) + 4 * p6 * (2 * fe[2, 0] - 3 * p1 * p2 - 2 * p1 * p6 + 2 * p2 * p5 + p6 * (p1 - p5)) * (-l_e + p2 + p4 + p6 + p8)) / (8 * p6 * (-l_e + p2 + p4 + p6 + p8));
                                fc[4, 0] = (-2 * Math.Pow(l_e,3) * p6 * (p5 - p7) - 6 * Math.Pow(l_e,2) * p6 * (-l_e * p5 + p2 * p7 + p4 * p5 + p5 * p8 + p6 * p7) - 3 * l_e * (-Math.Pow(l_e,2) * p6 * (p5 - p7) + 4 * p2 * (2 * p1 * p2 + 2 * p1 * p6 - 2 * p2 * p5 + p2 * (-p1 + p5)) * (-l_e + p2 + p4 + p6 + p8) + 8 * Math.Pow(p6,2) * (-l_e * p5 + p2 * p7 + p4 * p5 + p5 * p8 + p6 * p7) + 4 * p6 * (-l_e * (-l_e * p5 + p2 * p7 + p4 * p5 + p5 * p8 + p6 * p7) + Math.Pow(p6,2) * (p5 - p7)) + 4 * p6 * (2 * fe[2, 0] - 3 * p1 * p2 - 2 * p1 * p6 + 2 * p2 * p5 + p6 * (p1 - p5)) * (-l_e + p2 + p4 + p6 + p8)) + 8 * Math.Pow(p2,2) * (3 * p1 * p2 + 3 * p1 * p6 - 3 * p2 * p5 + 2 * p2 * (-p1 + p5)) * (-l_e + p2 + p4 + p6 + p8) + 16 * Math.Pow(p6,4) * (p5 - p7) + 24 * Math.Pow(p6,3) * (-l_e * p5 + p2 * p7 + p4 * p5 + p5 * p8 + p6 * p7) + 8 * p6 * (-6 * fe[4, 0] - 2 * p1 * Math.Pow(p2,2) + 2 * Math.Pow(p6,2) * (p1 - p5) - 3 * p6 * (p1 * p2 + p1 * p6 - p2 * p5)) * (-l_e + p2 + p4 + p6 + p8)) / (48 * p6 * (-l_e + p2 + p4 + p6 + p8));
                            }
                        }
                        else if (l_e / 2.0 < l_e - p4)
                        {
                            fc[2, 0] = (4 * p2 * p8 * (2 * p1 * p2 + 2 * p1 * p6 - 2 * p2 * p5 + p2 * (-p1 + p5)) * (-l_e + p2 + p4 + p6 + p8) + 4 * p6 * p8 * (Math.Pow(p6,2) * (p5 - p7) + (-p5 + p7) * Math.Pow(-l_e + p4 + p8,2)) + 8 * p6 * p8 * (-l_e + p4 + p6 + p8) * (-l_e * p5 + p2 * p7 + p4 * p5 + p5 * p8 + p6 * p7) + 4 * p6 * p8 * (2 * fe[2, 0] - 3 * p1 * p2 - 2 * p1 * p6 + 2 * p2 * p5 + p6 * (p1 - p5)) * (-l_e + p2 + p4 + p6 + p8) + p6 * (Math.Pow(l_e,2) * (-p3 + p7) - 4 * l_e * (-l_e * p3 + l_e * p7 + p3 * p4 + p3 * p8 - p4 * p7) + 4 * (p3 - p7) * Math.Pow(-l_e + p4 + p8,2) - 8 * (-l_e + p4 + p8) * (-l_e * p3 + l_e * p7 + p3 * p4 + p3 * p8 - p4 * p7)) * (-l_e + p2 + p4 + p6 + p8)) / (8 * p6 * p8 * (-l_e + p2 + p4 + p6 + p8));
                            fc[4, 0] = (-3 * l_e * (4 * p2 * p8 * (2 * p1 * p2 + 2 * p1 * p6 - 2 * p2 * p5 + p2 * (-p1 + p5)) * (-l_e + p2 + p4 + p6 + p8) + 4 * p6 * p8 * (Math.Pow(p6,2) * (p5 - p7) + (-p5 + p7) * Math.Pow(-l_e + p4 + p8,2)) + 8 * p6 * p8 * (-l_e + p4 + p6 + p8) * (-l_e * p5 + p2 * p7 + p4 * p5 + p5 * p8 + p6 * p7) + 4 * p6 * p8 * (2 * fe[2, 0] - 3 * p1 * p2 - 2 * p1 * p6 + 2 * p2 * p5 + p6 * (p1 - p5)) * (-l_e + p2 + p4 + p6 + p8) + p6 * (Math.Pow(l_e,2) * (-p3 + p7) - 4 * l_e * (-l_e * p3 + l_e * p7 + p3 * p4 + p3 * p8 - p4 * p7) + 4 * (p3 - p7) * Math.Pow(-l_e + p4 + p8,2) - 8 * (-l_e + p4 + p8) * (-l_e * p3 + l_e * p7 + p3 * p4 + p3 * p8 - p4 * p7)) * (-l_e + p2 + p4 + p6 + p8)) + 8 * Math.Pow(p2,2) * p8 * (3 * p1 * p2 + 3 * p1 * p6 - 3 * p2 * p5 + 2 * p2 * (-p1 + p5)) * (-l_e + p2 + p4 + p6 + p8) + 16 * p6 * p8 * (p5 - p7) * (Math.Pow(p6,3) + Math.Pow(-l_e + p4 + p8,3)) + 24 * p6 * p8 * (Math.Pow(p6,2) * (-l_e * p5 + p2 * p7 + p4 * p5 + p5 * p8 + p6 * p7) + Math.Pow(-l_e + p4 + p8,2) * (l_e * p5 - p2 * p7 - p4 * p5 - p5 * p8 - p6 * p7)) + 8 * p6 * p8 * (-6 * fe[4, 0] - 2 * p1 * Math.Pow(p2,2) + 2 * Math.Pow(p6,2) * (p1 - p5) - 3 * p6 * (p1 * p2 + p1 * p6 - p2 * p5)) * (-l_e + p2 + p4 + p6 + p8) + 2 * p6 * (Math.Pow(l_e,3) * (-p3 + p7) + 3 * Math.Pow(l_e,2) * (l_e * p3 - l_e * p7 - p3 * p4 - p3 * p8 + p4 * p7) + 8 * (-p3 + p7) * Math.Pow(-l_e + p4 + p8,3) + 12 * Math.Pow(-l_e + p4 + p8,2) * (-l_e * p3 + l_e * p7 + p3 * p4 + p3 * p8 - p4 * p7)) * (-l_e + p2 + p4 + p6 + p8)) / (48 * p6 * p8 * (-l_e + p2 + p4 + p6 + p8));
                        }
                    }
                    fe.Scale(p[i]); fc.Scale(p[i]);
                    fe[3, 0] = fe[3, 0] * cs; fe[4, 0] = fe[4, 0] * cs; fe[5, 0] = fe[5, 0] * cs; fe[9, 0] = fe[9, 0] * cs; fe[10, 0] = fe[10, 0] * cs; fe[11, 0] = fe[11, 0] * cs; fc[3, 0] = fc[3, 0] * cs; fc[4, 0] = fc[4, 0] * cs; fc[5, 0] = fc[5, 0] * cs;
                    fe[0, 0] = fe[2, 0] * sn; fe[2, 0] = fe[2, 0] * cs; fe[6, 0] = fe[8, 0] * sn; fe[8, 0] = fe[8, 0] * cs; fc[0, 0] = fc[2, 0] * sn; fc[2, 0] = fc[2, 0] * cs;
                    f_l.Add(new List<double>(new double[] { el, fe[0, 0], fe[1, 0], fe[2, 0], fe[3, 0], fe[4, 0], fe[5, 0], fe[6, 0], fe[7, 0], fe[8, 0], fe[9, 0], fe[10, 0], fe[11, 0], fc[0, 0], fc[1, 0], fc[2, 0], fc[3, 0], fc[4, 0], fc[5, 0] }));
                    var tr = transmatrix(l_e, lx, ly, lz, 0);
                    tr.Transpose();
                    fe = tr * fe;
                    f.Add(new List<double>(new double[] { ij_new[el][0], fe[0, 0], fe[1, 0], fe[2, 0], fe[3, 0], fe[4, 0], fe[5, 0] }));///joint考慮でエキストラ節点番号を割り当て
                    f.Add(new List<double>(new double[] { ij_new[el][1], fe[6, 0], fe[7, 0], fe[8, 0], fe[9, 0], fe[10, 0], fe[11, 0] }));///joint考慮でエキストラ節点番号を割り当て
                }
                return Tuple.Create(f, f_l);
            }
            Matrix cal_eq(double l, double qx, double qy, double qz)
            {
                Matrix fe = new Matrix(12, 1);
                fe[0, 0] = l * qx / 2.0;
                fe[1, 0] = l * qy / 2.0;
                fe[2, 0] = l * qz / 2.0;
                fe[4, 0] = -Math.Pow(l, 2) * qz / 12.0;
                fe[5, 0] = Math.Pow(l, 2) * qy / 12.0;
                fe[6, 0] = l * qx / 2.0;
                fe[7, 0] = l * qy / 2.0;
                fe[8, 0] = l * qz / 2.0;
                fe[10, 0] = Math.Pow(l, 2) * qz / 12.0;
                fe[11, 0] = -Math.Pow(l, 2) * qy / 12.0;
                return fe;
            }
            Matrix cal_eq_quad(double l, double qx1, double qy1, double qz1, double qx2, double qy2, double qz2, double l1, double l2)
            {
                Matrix fe = new Matrix(12, 1);
                fe[0, 0] = (2 * Math.Pow(l,2) * qx1 + Math.Pow(l, 2) * qx2 - l * l1 * qx1 - 2 * l * l1 * qx2 - l * l2 * qx1 + l * l2 * qx2 + Math.Pow(l1, 2) * qx2 + l1 * l2 * qx1 - l1 * l2 * qx2 - Math.Pow(l2, 2) * qx1) / (6 * l);
                fe[1, 0] = (7 * Math.Pow(l, 4) * qy1 + 3 * Math.Pow(l, 4) * qy2 - 3 * Math.Pow(l, 3) * l1 * qy1 - 7 * Math.Pow(l, 3) * l1 * qy2 - 3 * Math.Pow(l, 3) * l2 * qy1 + 3 * Math.Pow(l, 3) * l2 * qy2 - 3 * Math.Pow(l, 2) * Math.Pow(l1, 2) * qy1 + 3 * Math.Pow(l, 2) * Math.Pow(l1, 2) * qy2 + 4 * Math.Pow(l, 2) * l1 * l2 * qy1 - 4 * Math.Pow(l, 2) * l1 * l2 * qy2 - 3 * Math.Pow(l, 2) * Math.Pow(l2, 2) * qy1 + 3 * Math.Pow(l, 2) * Math.Pow(l2, 2) * qy2 + 2 * l * Math.Pow(l1, 3) * qy1 + 3 * l * Math.Pow(l1, 3) * qy2 + l * Math.Pow(l1, 2) * l2 * qy1 - l * Math.Pow(l1, 2) * l2 * qy2 + l * l1 * Math.Pow(l2, 2) * qy1 - l * l1 * Math.Pow(l2, 2) * qy2 - 3 * l * Math.Pow(l2, 3) * qy1 - 2 * l * Math.Pow(l2, 3) * qy2 - 2 * Math.Pow(l1, 4) * qy2 - 2 * Math.Pow(l1, 3) * l2 * qy1 + 2 * Math.Pow(l1, 3) * l2 * qy2 + 2 * Math.Pow(l1, 2) * Math.Pow(l2, 2) * qy1 - 2 * Math.Pow(l1, 2) * Math.Pow(l2, 2) * qy2 - 2 * l1 * Math.Pow(l2, 3) * qy1 + 2 * l1 * Math.Pow(l2, 3) * qy2 + 2 * Math.Pow(l2, 4) * qy1) / (20 * Math.Pow(l, 3));
                fe[2, 0] = (7 * Math.Pow(l, 4) * qz1 + 3 * Math.Pow(l, 4) * qz2 - 3 * Math.Pow(l, 3) * l1 * qz1 - 7 * Math.Pow(l, 3) * l1 * qz2 - 3 * Math.Pow(l, 3) * l2 * qz1 + 3 * Math.Pow(l, 3) * l2 * qz2 - 3 * Math.Pow(l, 2) * Math.Pow(l1, 2) * qz1 + 3 * Math.Pow(l, 2) * Math.Pow(l1, 2) * qz2 + 4 * Math.Pow(l, 2) * l1 * l2 * qz1 - 4 * Math.Pow(l, 2) * l1 * l2 * qz2 - 3 * Math.Pow(l, 2) * Math.Pow(l2, 2) * qz1 + 3 * Math.Pow(l, 2) * Math.Pow(l2, 2) * qz2 + 2 * l * Math.Pow(l1, 3) * qz1 + 3 * l * Math.Pow(l1, 3) * qz2 + l * Math.Pow(l1, 2) * l2 * qz1 - l * Math.Pow(l1, 2) * l2 * qz2 + l * l1 * Math.Pow(l2, 2) * qz1 - l * l1 * Math.Pow(l2, 2) * qz2 - 3 * l * Math.Pow(l2, 3) * qz1 - 2 * l * Math.Pow(l2, 3) * qz2 - 2 * Math.Pow(l1, 4) * qz2 - 2 * Math.Pow(l1, 3) * l2 * qz1 + 2 * Math.Pow(l1, 3) * l2 * qz2 + 2 * Math.Pow(l1, 2) * Math.Pow(l2, 2) * qz1 - 2 * Math.Pow(l1, 2) * Math.Pow(l2, 2) * qz2 - 2 * l1 * Math.Pow(l2, 3) * qz1 + 2 * l1 * Math.Pow(l2, 3) * qz2 + 2 * Math.Pow(l2, 4) * qz1) / (20 * Math.Pow(l, 3));
                fe[4, 0] = -(3 * Math.Pow(l, 4) * qz1 + 2 * Math.Pow(l, 4) * qz2 + 3 * Math.Pow(l, 3) * l1 * qz1 - 3 * Math.Pow(l, 3) * l1 * qz2 - 2 * Math.Pow(l, 3) * l2 * qz1 + 2 * Math.Pow(l, 3) * l2 * qz2 - 7 * Math.Pow(l, 2) * Math.Pow(l1, 2) * qz1 - 3 * Math.Pow(l, 2) * Math.Pow(l1, 2) * qz2 + Math.Pow(l, 2) * l1 * l2 * qz1 - Math.Pow(l, 2) * l1 * l2 * qz2 - 2 * Math.Pow(l, 2) * Math.Pow(l2, 2) * qz1 + 2 * Math.Pow(l, 2) * Math.Pow(l2, 2) * qz2 + 3 * l * Math.Pow(l1, 3) * qz1 + 7 * l * Math.Pow(l1, 3) * qz2 + 4 * l * Math.Pow(l1, 2) * l2 * qz1 - 4 * l * Math.Pow(l1, 2) * l2 * qz2 - l * l1 * Math.Pow(l2, 2) * qz1 + l * l1 * Math.Pow(l2, 2) * qz2 - 2 * l * Math.Pow(l2, 3) * qz1 - 3 * l * Math.Pow(l2, 3) * qz2 - 3 * Math.Pow(l1, 4) * qz2 - 3 * Math.Pow(l1, 3) * l2 * qz1 + 3 * Math.Pow(l1, 3) * l2 * qz2 + 3 * Math.Pow(l1, 2) * Math.Pow(l2, 2) * qz1 - 3 * Math.Pow(l1, 2) * Math.Pow(l2, 2) * qz2 - 3 * l1 * Math.Pow(l2, 3) * qz1 + 3 * l1 * Math.Pow(l2, 3) * qz2 + 3 * Math.Pow(l2, 4) * qz1) / (60 * Math.Pow(l, 2));
                fe[5, 0] = (3 * Math.Pow(l, 4) * qy1 + 2 * Math.Pow(l, 4) * qy2 + 3 * Math.Pow(l, 3) * l1 * qy1 - 3 * Math.Pow(l, 3) * l1 * qy2 - 2 * Math.Pow(l, 3) * l2 * qy1 + 2 * Math.Pow(l, 3) * l2 * qy2 - 7 * Math.Pow(l, 2) * Math.Pow(l1, 2) * qy1 - 3 * Math.Pow(l, 2) * Math.Pow(l1, 2) * qy2 + Math.Pow(l, 2) * l1 * l2 * qy1 - Math.Pow(l, 2) * l1 * l2 * qy2 - 2 * Math.Pow(l, 2) * Math.Pow(l2, 2) * qy1 + 2 * Math.Pow(l, 2) * Math.Pow(l2, 2) * qy2 + 3 * l * Math.Pow(l1, 3) * qy1 + 7 * l * Math.Pow(l1, 3) * qy2 + 4 * l * Math.Pow(l1, 2) * l2 * qy1 - 4 * l * Math.Pow(l1, 2) * l2 * qy2 - l * l1 * Math.Pow(l2, 2) * qy1 + l * l1 * Math.Pow(l2, 2) * qy2 - 2 * l * Math.Pow(l2, 3) * qy1 - 3 * l * Math.Pow(l2, 3) * qy2 - 3 * Math.Pow(l1, 4) * qy2 - 3 * Math.Pow(l1, 3) * l2 * qy1 + 3 * Math.Pow(l1, 3) * l2 * qy2 + 3 * Math.Pow(l1, 2) * Math.Pow(l2, 2) * qy1 - 3 * Math.Pow(l1, 2) * Math.Pow(l2, 2) * qy2 - 3 * l1 * Math.Pow(l2, 3) * qy1 + 3 * l1 * Math.Pow(l2, 3) * qy2 + 3 * Math.Pow(l2, 4) * qy1) / (60 * Math.Pow(l, 2));
                fe[6, 0] = (Math.Pow(l, 2) * qx1 + 2 * Math.Pow(l, 2) * qx2 + l * l1 * qx1 - l * l1 * qx2 - 2 * l * l2 * qx1 - l * l2 * qx2 - Math.Pow(l1, 2) * qx2 - l1 * l2 * qx1 + l1 * l2 * qx2 + Math.Pow(l2, 2) * qx1) / (6 * l);
                fe[7, 0] = (3 * Math.Pow(l, 4) * qy1 + 7 * Math.Pow(l, 4) * qy2 + 3 * Math.Pow(l, 3) * l1 * qy1 - 3 * Math.Pow(l, 3) * l1 * qy2 - 7 * Math.Pow(l, 3) * l2 * qy1 - 3 * Math.Pow(l, 3) * l2 * qy2 + 3 * Math.Pow(l, 2) * Math.Pow(l1, 2) * qy1 - 3 * Math.Pow(l, 2) * Math.Pow(l1, 2) * qy2 - 4 * Math.Pow(l, 2) * l1 * l2 * qy1 + 4 * Math.Pow(l, 2) * l1 * l2 * qy2 + 3 * Math.Pow(l, 2) * Math.Pow(l2, 2) * qy1 - 3 * Math.Pow(l, 2) * Math.Pow(l2, 2) * qy2 - 2 * l * Math.Pow(l1, 3) * qy1 - 3 * l * Math.Pow(l1, 3) * qy2 - l * Math.Pow(l1, 2) * l2 * qy1 + l * Math.Pow(l1, 2) * l2 * qy2 - l * l1 * Math.Pow(l2, 2) * qy1 + l * l1 * Math.Pow(l2, 2) * qy2 + 3 * l * Math.Pow(l2, 3) * qy1 + 2 * l * Math.Pow(l2, 3) * qy2 + 2 * Math.Pow(l1, 4) * qy2 + 2 * Math.Pow(l1, 3) * l2 * qy1 - 2 * Math.Pow(l1, 3) * l2 * qy2 - 2 * Math.Pow(l1, 2) * Math.Pow(l2, 2) * qy1 + 2 * Math.Pow(l1, 2) * Math.Pow(l2, 2) * qy2 + 2 * l1 * Math.Pow(l2, 3) * qy1 - 2 * l1 * Math.Pow(l2, 3) * qy2 - 2 * Math.Pow(l2, 4) * qy1) / (20 * Math.Pow(l, 3));
                fe[8, 0] = (3 * Math.Pow(l, 4) * qz1 + 7 * Math.Pow(l, 4) * qz2 + 3 * Math.Pow(l, 3) * l1 * qz1 - 3 * Math.Pow(l, 3) * l1 * qz2 - 7 * Math.Pow(l, 3) * l2 * qz1 - 3 * Math.Pow(l, 3) * l2 * qz2 + 3 * Math.Pow(l, 2) * Math.Pow(l1, 2) * qz1 - 3 * Math.Pow(l, 2) * Math.Pow(l1, 2) * qz2 - 4 * Math.Pow(l, 2) * l1 * l2 * qz1 + 4 * Math.Pow(l, 2) * l1 * l2 * qz2 + 3 * Math.Pow(l, 2) * Math.Pow(l2, 2) * qz1 - 3 * Math.Pow(l, 2) * Math.Pow(l2, 2) * qz2 - 2 * l * Math.Pow(l1, 3) * qz1 - 3 * l * Math.Pow(l1, 3) * qz2 - l * Math.Pow(l1, 2) * l2 * qz1 + l * Math.Pow(l1, 2) * l2 * qz2 - l * l1 * Math.Pow(l2, 2) * qz1 + l * l1 * Math.Pow(l2, 2) * qz2 + 3 * l * Math.Pow(l2, 3) * qz1 + 2 * l * Math.Pow(l2, 3) * qz2 + 2 * Math.Pow(l1, 4) * qz2 + 2 * Math.Pow(l1, 3) * l2 * qz1 - 2 * Math.Pow(l1, 3) * l2 * qz2 - 2 * Math.Pow(l1, 2) * Math.Pow(l2, 2) * qz1 + 2 * Math.Pow(l1, 2) * Math.Pow(l2, 2) * qz2 + 2 * l1 * Math.Pow(l2, 3) * qz1 - 2 * l1 * Math.Pow(l2, 3) * qz2 - 2 * Math.Pow(l2, 4) * qz1) / (20 * Math.Pow(l, 3));
                fe[10, 0] = (2 * Math.Pow(l, 4) * qz1 + 3 * Math.Pow(l, 4) * qz2 + 2 * Math.Pow(l, 3) * l1 * qz1 - 2 * Math.Pow(l, 3) * l1 * qz2 - 3 * Math.Pow(l, 3) * l2 * qz1 + 3 * Math.Pow(l, 3) * l2 * qz2 + 2 * Math.Pow(l, 2) * Math.Pow(l1, 2) * qz1 - 2 * Math.Pow(l, 2) * Math.Pow(l1, 2) * qz2 - Math.Pow(l, 2) * l1 * l2 * qz1 + Math.Pow(l, 2) * l1 * l2 * qz2 - 3 * Math.Pow(l, 2) * Math.Pow(l2, 2) * qz1 - 7 * Math.Pow(l, 2) * Math.Pow(l2, 2) * qz2 - 3 * l * Math.Pow(l1, 3) * qz1 - 2 * l * Math.Pow(l1, 3) * qz2 + l * Math.Pow(l1, 2) * l2 * qz1 - l * Math.Pow(l1, 2) * l2 * qz2 - 4 * l * l1 * Math.Pow(l2, 2) * qz1 + 4 * l * l1 * Math.Pow(l2, 2) * qz2 + 7 * l * Math.Pow(l2, 3) * qz1 + 3 * l * Math.Pow(l2, 3) * qz2 + 3 * Math.Pow(l1, 4) * qz2 + 3 * Math.Pow(l1, 3) * l2 * qz1 - 3 * Math.Pow(l1, 3) * l2 * qz2 - 3 * Math.Pow(l1, 2) * Math.Pow(l2, 2) * qz1 + 3 * Math.Pow(l1, 2) * Math.Pow(l2, 2) * qz2 + 3 * l1 * Math.Pow(l2, 3) * qz1 - 3 * l1 * Math.Pow(l2, 3) * qz2 - 3 * Math.Pow(l2, 4) * qz1) / (60 * Math.Pow(l, 2));
                fe[11, 0] = -(2 * Math.Pow(l, 4) * qy1 + 3 * Math.Pow(l, 4) * qy2 + 2 * Math.Pow(l, 3) * l1 * qy1 - 2 * Math.Pow(l, 3) * l1 * qy2 - 3 * Math.Pow(l, 3) * l2 * qy1 + 3 * Math.Pow(l, 3) * l2 * qy2 + 2 * Math.Pow(l, 2) * Math.Pow(l1, 2) * qy1 - 2 * Math.Pow(l, 2) * Math.Pow(l1, 2) * qy2 - Math.Pow(l, 2) * l1 * l2 * qy1 + Math.Pow(l, 2) * l1 * l2 * qy2 - 3 * Math.Pow(l, 2) * Math.Pow(l2, 2) * qy1 - 7 * Math.Pow(l, 2) * Math.Pow(l2, 2) * qy2 - 3 * l * Math.Pow(l1, 3) * qy1 - 2 * l * Math.Pow(l1, 3) * qy2 + l * Math.Pow(l1, 2) * l2 * qy1 - l * Math.Pow(l1, 2) * l2 * qy2 - 4 * l * l1 * Math.Pow(l2, 2) * qy1 + 4 * l * l1 * Math.Pow(l2, 2) * qy2 + 7 * l * Math.Pow(l2, 3) * qy1 + 3 * l * Math.Pow(l2, 3) * qy2 + 3 * Math.Pow(l1, 4) * qy2 + 3 * Math.Pow(l1, 3) * l2 * qy1 - 3 * Math.Pow(l1, 3) * l2 * qy2 - 3 * Math.Pow(l1, 2) * Math.Pow(l2, 2) * qy1 + 3 * Math.Pow(l1, 2) * Math.Pow(l2, 2) * qy2 + 3 * l1 * Math.Pow(l2, 3) * qy1 - 3 * l1 * Math.Pow(l2, 3) * qy2 - 3 * Math.Pow(l2, 4) * qy1) / (60 * Math.Pow(l, 2));
                return fe;
            }
            if (!DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r)) { }
            else if (DA.GetDataTree("element_node_relationship", out GH_Structure<GH_Number> _ij))
            {
                r = _r.Branches; ij = _ij.Branches; List<List<double>> se_load; List<List<double>> se_load_l;
                List<List<double>> total_load = new List<List<double>>(); List<List<double>> total_load_l = new List<List<double>>();
                lgh = length();
                for (int e = 0; e < ij.Count; e++) { total_load_l.Add(new List<double> { e, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 }); }
                IList<List<GH_Number>> joint = new List<List<GH_Number>>();
                DA.GetDataTree("joint condition", out GH_Structure<GH_Number> _joint); var joint_No = new List<int>(); int nc = 0; int n = r.Count;
                if (_joint.Branches[0][0].Value != -9999)
                {
                    joint = _joint.Branches;
                    if (_joint.Branches[0][0].Value != -9999)
                    {
                        for (int i = 0; i < joint.Count; i++)
                        {
                            joint_No.Add((int)joint[i][0].Value);
                        }
                    }
                }
                GH_Structure<GH_Number> rj = new GH_Structure<GH_Number>();
                for (int i = 0; i < r.Count; i++) { rj.AppendRange(new List<GH_Number> { new GH_Number(r[i][0].Value), new GH_Number(r[i][1].Value), new GH_Number(r[i][2].Value) }, new GH_Path(i)); }
                if (ij[0][0].Value != -9999)
                {
                    for (int e = 0; e < ij.Count; e++)
                    {
                        if (joint_No.Contains(e))
                        {
                            int i = (int)ij[e][0].Value; int j = (int)ij[e][1].Value;
                            int k = joint_No.IndexOf(e);
                            if (joint[k][1].Value == 0)
                            {
                                nc += 1;
                                ij_new.Add(new List<int> { n + nc - 1, (int)ij[e][1].Value });
                                rj.AppendRange(new List<GH_Number> { new GH_Number(r[i][0].Value), new GH_Number(r[i][1].Value), new GH_Number(r[i][2].Value) }, new GH_Path(n + nc - 1));
                            }
                            else if (joint[k][1].Value == 1)
                            {
                                nc += 1;
                                ij_new.Add(new List<int> { (int)ij[e][0].Value, n + nc - 1 });
                                rj.AppendRange(new List<GH_Number> { new GH_Number(r[j][0].Value), new GH_Number(r[j][1].Value), new GH_Number(r[j][2].Value) }, new GH_Path(n + nc - 1));
                            }
                            else if (joint[k][1].Value == 2)
                            {
                                nc += 1;
                                nc += 1;
                                ij_new.Add(new List<int> { n + nc - 2, n + nc - 1 });
                                rj.AppendRange(new List<GH_Number> { new GH_Number(r[i][0].Value), new GH_Number(r[i][1].Value), new GH_Number(r[i][2].Value) }, new GH_Path(n + nc - 2));
                                rj.AppendRange(new List<GH_Number> { new GH_Number(r[j][0].Value), new GH_Number(r[j][1].Value), new GH_Number(r[j][2].Value) }, new GH_Path(n + nc - 1));
                            }
                            else { ij_new.Add(new List<int> { (int)ij[e][0].Value, (int)ij[e][1].Value }); }
                        }
                        else { ij_new.Add(new List<int> { (int)ij[e][0].Value, (int)ij[e][1].Value }); }
                    }
                }
                DA.SetDataTree(2, rj);
                for (int i = 0; i < n + nc; i++) { total_load.Add(new List<double> { i, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0 }); }
                if (!DA.GetDataTree("nodal_load", out GH_Structure<GH_Number> _p_load)) { }
                else if (_p_load.Branches[0][0].Value != -9999)
                {
                    p_load = _p_load.Branches; int np_l = p_load.Count;
                    for (int e = 0; e < np_l; e++)
                    {
                        int i = (int)p_load[e][0].Value;
                        total_load[i][1] += p_load[e][1].Value; total_load[i][2] += p_load[e][2].Value; total_load[i][3] += p_load[e][3].Value; total_load[i][4] += p_load[e][4].Value; total_load[i][5] += p_load[e][5].Value; total_load[i][6] += p_load[e][6].Value;
                    }

                }
                if (!DA.GetDataTree("floor_load", out GH_Structure<GH_Number> _f_load)) { }
                else if (_f_load.Branches[0][0].Value != -9999)
                {
                    f_load = _f_load.Branches;
                    var sp = load_expansion(f_load); var s = sp.Item1; var p = sp.Item2;
                    var exprs = split_f_load(s, p); var ex_load = exprs.Item1; var pressure = exprs.Item2;
                    if (Kamenoko == 1)
                    {
                        for (int i = 0; i < ex_load.Count; i++)
                        {
                            for (int j = 1; j < ex_load[i].Item2.Count; j++)
                            {
                                _l.Add(new Line(new Point3d(ex_load[i].Item2[j].X, ex_load[i].Item2[j].Y, ex_load[i].Item2[j].Z), new Point3d(ex_load[i].Item2[j - 1].X, ex_load[i].Item2[j - 1].Y, ex_load[i].Item2[j - 1].Z)));
                            }
                        }
                    }
                    var eq_f_load = equivalentS(ex_load, pressure); se_load = eq_f_load.Item1; se_load_l = eq_f_load.Item2;
                    for (int e = 0; e < se_load.Count; e++)
                    {
                        int i = (int)se_load[e][0];
                        total_load[i][1] += se_load[e][1]; total_load[i][2] += se_load[e][2]; total_load[i][3] += se_load[e][3]; total_load[i][4] += se_load[e][4]; total_load[i][5] += se_load[e][5]; total_load[i][6] += se_load[e][6];
                    }
                    for (int e = 0; e < se_load_l.Count; e++)
                    {
                        int i = (int)se_load_l[e][0];
                        for (int j = 1; j < 19; j++)
                        {
                            total_load_l[i][j] += se_load_l[e][j];
                        }
                    }
                }
                if (!DA.GetDataTree("surface_load", out GH_Structure<GH_Number> _s_load)) { }
                else if (_s_load.Branches[0][0].Value != -9999)
                {
                    s_load = _s_load.Branches; int ne_s = s_load.Count;
                    for (int i = 0; i < ne_s; i++)
                    {
                        int n1 = (int)s_load[i][0].Value; int n2 = (int)s_load[i][1].Value; int n3 = (int)s_load[i][2].Value; int n4 = (int)s_load[i][3].Value; double s = s_load[i][4].Value;
                        var r1 = new Point3d(r[n1][0].Value, r[n1][1].Value, r[n1][2].Value); var r2 = new Point3d(r[n2][0].Value, r[n2][1].Value, r[n2][2].Value); var r3 = new Point3d(r[n3][0].Value, r[n3][1].Value, r[n3][2].Value);
                        var q1 = 0.0; var q2 = 0.0; var q3 = 0.0; var q4 = 0.0;
                        if (n4 < 0)
                        {
                            var s1 = NurbsSurface.CreateFromCorners(r1, r2, r3, r3); var a1 = s1.ToBrep().GetArea();
                            q1 = a1 * s / 3.0; q2 = a1 * s / 3.0; q3 = a1 * s / 3.0;
                            total_load[n1][3] += q1; total_load[n2][3] += q2; total_load[n3][3] += q3;
                        }
                        else
                        {
                            var r4 = new Point3d(r[n4][0].Value, r[n4][1].Value, r[n4][2].Value);
                            var s1 = NurbsSurface.CreateFromCorners(r1, r2, r3, r3); var a1 = s1.ToBrep().GetArea();
                            var s2 = NurbsSurface.CreateFromCorners(r1, r3, r4, r4); var a2 = s2.ToBrep().GetArea();
                            var s3 = NurbsSurface.CreateFromCorners(r1, r2, r4, r4); var a3 = s1.ToBrep().GetArea();
                            var s4 = NurbsSurface.CreateFromCorners(r2, r3, r4, r4); var a4 = s2.ToBrep().GetArea();
                            q1 = a1 * s / 3.0 + a2 * s / 3.0 + a3 * s / 3.0; q2 = a1 * s / 3.0 + a3 * s / 3.0 + a4 * s / 3.0; q3 = a1 * s / 3.0 + a2 * s / 3.0 + a4 * s / 3.0; q4 = a2 * s / 3.0 + a3 * s / 3.0 + a4 * s / 3.0;
                            q1 /= 2.0; q2 /= 2.0; q3 /= 2.0; q4 /= 2.0;
                            total_load[n1][3] += q1; total_load[n2][3] += q2; total_load[n3][3] += q3; total_load[n4][3] += q4;
                        }
                    }
                }
                if (!DA.GetDataTree("wall_load", out GH_Structure<GH_Number> _w_load)) { }
                else if (_w_load.Branches[0][0].Value != -9999)
                {
                    w_load = _w_load.Branches;
                    var sp = load_expansion2(w_load); var s = sp.Item1; var p = sp.Item2;
                    var exprs = split_f_load(s, p); var ex_load = exprs.Item1; var pressure = exprs.Item2;
                    if (Kamenoko == 1)
                    {
                        for (int i = 0; i < ex_load.Count; i++)
                        {
                            for (int j = 1; j < ex_load[i].Item2.Count; j++)
                            {
                                _l.Add(new Line(new Point3d(ex_load[i].Item2[j].X, ex_load[i].Item2[j].Y, ex_load[i].Item2[j].Z), new Point3d(ex_load[i].Item2[j - 1].X, ex_load[i].Item2[j - 1].Y, ex_load[i].Item2[j - 1].Z)));
                            }
                        }
                    }
                    var eq_f_load = equivalentS(ex_load, pressure); se_load = eq_f_load.Item1; se_load_l = eq_f_load.Item2;
                    for (int e = 0; e < se_load.Count; e++)
                    {
                        int i = (int)se_load[e][0];
                        total_load[i][1] += se_load[e][1]; total_load[i][2] += se_load[e][2]; total_load[i][3] += se_load[e][3]; total_load[i][4] += se_load[e][4]; total_load[i][5] += se_load[e][5]; total_load[i][6] += se_load[e][6];
                    }
                    for (int e = 0; e < se_load_l.Count; e++)
                    {
                        int i = (int)se_load_l[e][0];
                        for (int j = 1; j < 19; j++)
                        {
                            total_load_l[i][j] += se_load_l[e][j];
                        }
                    }
                }
                if (!DA.GetDataTree("element_load", out GH_Structure<GH_Number> _l_load)) { }
                else if (_l_load.Branches[0][0].Value != -9999)
                {
                    l_load = _l_load.Branches; int ne_l = l_load.Count;
                    for (int i = 0; i < ne_l; i++)
                    {
                        int e = (int)l_load[i][0].Value; double wx = l_load[i][1].Value; double wy = l_load[i][2].Value; double wz = l_load[i][3].Value;
                        int n1 = (int)ij[e][0].Value; int n2 = (int)ij[e][1].Value; var a_e = ij[e][4].Value;
                        double l = lgh[e]; double lx = r[n2][0].Value - r[n1][0].Value; double ly = r[n2][1].Value - r[n1][1].Value; double lz = r[n2][2].Value - r[n1][2].Value;
                        Vector3d x = new Vector3d(lx, ly, lz); Vector3d y = rotation(x, new Vector3d(0, 0, 1), 90); y[2] = 0.0; Vector3d z = rotation(y, x, 90 + a_e); y = rotation(z, x, -90);
                        if (Math.Abs(lx) <= 5e-3 && Math.Abs(ly) <= 5e-3)
                        {
                            y = rotation(x, new Vector3d(0, 1, 0), 90); z = rotation(y, x, 90 + a_e); y = rotation(z, x, -90);
                        }
                        var x_x = Vector3d.VectorAngle(new Vector3d(1, 0, 0), x); var x_y = Vector3d.VectorAngle(new Vector3d(1, 0, 0), y); var x_z = Vector3d.VectorAngle(new Vector3d(1, 0, 0), z);
                        var y_x = Vector3d.VectorAngle(new Vector3d(0, 1, 0), x); var y_y = Vector3d.VectorAngle(new Vector3d(0, 1, 0), y); var y_z = Vector3d.VectorAngle(new Vector3d(0, 1, 0), z);
                        var z_x = Vector3d.VectorAngle(new Vector3d(0, 0, 1), x); var z_y = Vector3d.VectorAngle(new Vector3d(0, 0, 1), y); var z_z = Vector3d.VectorAngle(new Vector3d(0, 0, 1), z);
                        var qx = wx * Math.Cos(x_x) + wy * Math.Cos(y_x) + wz * Math.Cos(z_x);
                        var qy = wx * Math.Cos(x_y) + wy * Math.Cos(y_y) + wz * Math.Cos(z_y);
                        var qz = wx * Math.Cos(x_z) + wy * Math.Cos(y_z) + wz * Math.Cos(z_z);
                        //double s = lz / l; double c = Math.Sqrt(Math.Pow(lx, 2) + Math.Pow(ly, 2)) / l; var s2 = Math.Sin(angle); var c2 = Math.Cos(angle);
                        //double qx = wz * s; double qy = wz * c * s2; double qz = wz * c * c2;
                        //s = ly / l; c = Math.Sqrt(Math.Pow(lx, 2) + Math.Pow(lz, 2)) / l; s2 = Math.Sin(Math.PI / 2.0 + angle); c2 = Math.Cos(Math.PI / 2.0 + angle);
                        //if (Math.Abs(lx) <= 5e-3 && Math.Abs(ly) <= 5e-3)
                        //{
                        //    s2 = Math.Sin(angle); c2 = Math.Cos(angle);
                        //}
                        //qx += wy * s; qy += wy * c * s2; qz += wy * c * c2;
                        //s = lx / l; c = Math.Sqrt(Math.Pow(ly, 2) + Math.Pow(lz, 2)) / l;
                        //qx += wx * s; qy += wx * c * s2; qz += wx * c * c2;
                        //s = Math.Sqrt(Math.Pow(ly, 2) + Math.Pow(lz, 2)) / l; c = lx / l;
                        //qx += wx * c; double qy = wx * s;
                        //s = Math.Sqrt(Math.Pow(lx, 2) + Math.Pow(lz, 2)) / l; c = ly / l;
                        //qx += wy * c; qy += wy * s;
                        Matrix tr = transmatrix(l, lx, ly, lz, a_e);
                        Matrix fe = cal_eq(l, qx, qy, qz); Matrix fe2 = cal_eq(l / 2.0, qx, qy, qz); tr.Transpose();//uniform load
                        if (l_load[i].Count == 9)
                        {
                            double wx2 = l_load[i][4].Value; double wy2 = l_load[i][5].Value; double wz2 = l_load[i][6].Value; var l1 = l_load[i][7].Value; var l2 = l_load[i][8].Value;
                            var qx2 = wx2 * Math.Cos(x_x) + wy2 * Math.Cos(y_x) + wz2 * Math.Cos(z_x);
                            var qy2 = wx2 * Math.Cos(x_y) + wy2 * Math.Cos(y_y) + wz2 * Math.Cos(z_y);
                            var qz2 = wx2 * Math.Cos(x_z) + wy2 * Math.Cos(y_z) + wz2 * Math.Cos(z_z);
                            fe = cal_eq_quad(l, qx, qy, qz, qx2, qy2, qz2, l1, l2);
                            fe2 = cal_eq_quad(l / 2.0, qx, qy, qz, qx2, qy2, qz2, l1, l2);
                        }
                        Matrix e_vec = tr * fe;
                        n1 = ij_new[e][0]; n2 = ij_new[e][1];///joint考慮でエキストラ節点番号を割り当て
                        total_load[n1][1] += e_vec[0, 0]; total_load[n1][2] += e_vec[1, 0]; total_load[n1][3] += e_vec[2, 0]; total_load[n1][4] += e_vec[3, 0]; total_load[n1][5] += e_vec[4, 0]; total_load[n1][6] += e_vec[5, 0];
                        total_load[n2][1] += e_vec[6, 0]; total_load[n2][2] += e_vec[7, 0]; total_load[n2][3] += e_vec[8, 0]; total_load[n2][4] += e_vec[9, 0]; total_load[n2][5] += e_vec[10, 0]; total_load[n2][6] += e_vec[11, 0];
                        total_load_l[e][1] += fe[0, 0]; total_load_l[e][2] += fe[1, 0]; total_load_l[e][3] += fe[2, 0]; total_load_l[e][4] += fe[3, 0]; total_load_l[e][5] += fe[4, 0]; total_load_l[e][6] += fe[5, 0]; total_load_l[e][7] += fe[6, 0]; total_load_l[e][8] += fe[7, 0]; total_load_l[e][9] += fe[8, 0]; total_load_l[e][10] += fe[9, 0]; total_load_l[e][11] += fe[10, 0]; total_load_l[e][12] += fe[11, 0]; total_load_l[e][13] += (fe2[0, 0] - fe2[6, 0]); total_load_l[e][14] += (fe2[1, 0] - fe2[7, 0]); total_load_l[e][15] += (fe2[2, 0] - fe2[8, 0]); total_load_l[e][16] += (fe2[3, 0] - fe2[9, 0]); total_load_l[e][17] += (fe2[4, 0] - fe2[10, 0]); total_load_l[e][18] += (fe2[5, 0] - fe2[11, 0]);
                    }
                }
                List<double> A = new List<double>(); List<double> rho = new List<double>();
                DA.GetDataList("section area", A); DA.GetDataList("weight density", rho);
                if (A[0] != -9999 && rho[0] != -9999)
                {
                    for (int e = 0; e < ij.Count; e++)
                    {
                        int n1 = (int)ij[e][0].Value; int n2 = (int)ij[e][1].Value; double q = -A[(int)ij[e][3].Value] * rho[(int)ij[e][2].Value];
                        double l = lgh[e]; double lx = r[n2][0].Value - r[n1][0].Value; double ly = r[n2][1].Value - r[n1][1].Value; double lz = r[n2][2].Value - r[n1][2].Value;
                        double s = lz / l; double c = Math.Sqrt(Math.Pow(lx, 2) + Math.Pow(ly, 2)) / l;
                        double qx = q * s; double qz = q * c;
                        Matrix tr = transmatrix(l, lx, ly, lz, 0); Matrix fe = cal_eq(l, qx, 0, qz); Matrix fe2 = cal_eq(l / 2.0, qx, 0, qz); tr.Transpose();
                        Matrix e_vec = tr * fe;
                        n1 = ij_new[e][0]; n2 = ij_new[e][1];///joint考慮でエキストラ節点番号を割り当て
                        total_load[n1][1] += e_vec[0, 0]; total_load[n1][2] += e_vec[1, 0]; total_load[n1][3] += e_vec[2, 0]; total_load[n1][4] += e_vec[3, 0]; total_load[n1][5] += e_vec[4, 0]; total_load[n1][6] += e_vec[5, 0];
                        total_load[n2][1] += e_vec[6, 0]; total_load[n2][2] += e_vec[7, 0]; total_load[n2][3] += e_vec[8, 0]; total_load[n2][4] += e_vec[9, 0]; total_load[n2][5] += e_vec[10, 0]; total_load[n2][6] += e_vec[11, 0];
                        total_load_l[e][1] += fe[0, 0]; total_load_l[e][2] += fe[1, 0]; total_load_l[e][3] += fe[2, 0]; total_load_l[e][4] += fe[3, 0]; total_load_l[e][5] += fe[4, 0]; total_load_l[e][6] += fe[5, 0]; total_load_l[e][7] += fe[6, 0]; total_load_l[e][8] += fe[7, 0]; total_load_l[e][9] += fe[8, 0]; total_load_l[e][10] += fe[9, 0]; total_load_l[e][11] += fe[10, 0]; total_load_l[e][12] += fe[11, 0]; total_load_l[e][13] += (fe2[0, 0] - fe2[6, 0]); total_load_l[e][14] += (fe2[1, 0] - fe2[7, 0]); total_load_l[e][15] += (fe2[2, 0] - fe2[8, 0]); total_load_l[e][16] += (fe2[3, 0] - fe2[9, 0]); total_load_l[e][17] += (fe2[4, 0] - fe2[10, 0]); total_load_l[e][18] += (fe2[5, 0] - fe2[11, 0]);
                    }
                }
                GH_Structure<GH_Number> load = new GH_Structure<GH_Number>();
                for (int i = 0; i < n + nc; i++)
                {
                    List<GH_Number> loadlist = new List<GH_Number>();
                    for (int j = 0; j < 7; j++)
                    {
                        loadlist.Add(new GH_Number(total_load[i][j]));
                    }
                    load.AppendRange(loadlist, new GH_Path(i));
                }
                DA.SetDataTree(0, load);
                load = new GH_Structure<GH_Number>();
                if (ij[0][0].Value != -9999)
                {
                    for (int i = 0; i < ij.Count; i++)
                    {
                        List<GH_Number> loadlist = new List<GH_Number>();
                        for (int j = 0; j < 19; j++)
                        {
                            loadlist.Add(new GH_Number(total_load_l[i][j]));
                        }
                        load.AppendRange(loadlist, new GH_Path(i));
                    }
                    DA.SetDataTree(1, load);
                }
            }

        }
        protected override System.Drawing.Bitmap Icon
        {
            get { return OpenSeesUtility.Properties.Resources.Loads; }
        }
        public override Guid ComponentGuid
        {
            get { return new Guid("6c698d22-dab1-41c0-bb79-b0a3e71a1545"); }
        }
        ///ここまでカスタム関数群***************************************************************************************
        private readonly List<Line> _l = new List<Line>();
        protected override void BeforeSolveInstance() { _l.Clear(); }
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            args.Viewport.GetFrustumFarPlane(out Plane plane);
            Rhino.Display.RhinoViewport viewport = args.Viewport;
            for (int i = 0; i < _l.Count; i++)
            {
                args.Display.DrawLine(_l[i], Color.LemonChiffon);
            }
        }
        //////ここからGUIの作成*****************************************************************************************
        public class CustomGUI : GH_ComponentAttributes
        {
            public CustomGUI(GH_Component owner) : base(owner)
            {
            }
            private Rectangle title_rec;
            private Rectangle radio_rec;
            private Rectangle radio_rec_11; private Rectangle text_rec_11;

            protected override void Layout()
            {
                base.Layout();
                Rectangle global_rec = GH_Convert.ToRectangle(Bounds);
                int height = 40; int subwidth = 60; int radi1 = 7; int radi2 = 4;
                int pitchx = 6; int textheight = 20;
                global_rec.Height += height;
                title_rec = global_rec;
                title_rec.Y = title_rec.Bottom - height;
                title_rec.Height = 22;

                radio_rec = title_rec; radio_rec.Height = textheight;
                radio_rec.Y += radio_rec.Height;

                radio_rec_11 = title_rec;
                radio_rec_11.X += radi2 - 1; radio_rec_11.Y += title_rec.Height + radi2;
                radio_rec_11.Height = radi1; radio_rec_11.Width = radi1;

                text_rec_11 = radio_rec_11;
                text_rec_11.X += pitchx; text_rec_11.Y -= radi2;
                text_rec_11.Height = textheight; text_rec_11.Width = subwidth;

                Bounds = global_rec;
            }
            Brush c11 = Brushes.White;
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
                    graphics.DrawString("Kamenoko", GH_FontServer.Standard, Brushes.Black, text_rec_11);
                }

            }
            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    RectangleF rec11 = radio_rec_11;
                    if (rec11.Contains(e.CanvasLocation))
                    {
                        if (c11 == Brushes.White) { c11 = Brushes.Black; SetButton("Kamenoko", 1); }
                        else { c11 = Brushes.White; SetButton("Kamenoko", 0); }
                        Owner.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }
                }
                return base.RespondToMouseDown(sender, e);
            }
        }
    }
}