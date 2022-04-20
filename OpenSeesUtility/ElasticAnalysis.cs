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
using OpenSees;
using OpenSees.Materials.Uniaxials;
///****************************************

namespace ElasticAnalysis
{
    public class ElasticAnalysis : GH_Component
    {
        public ElasticAnalysis()
          : base("Analysis using OpenSees", "Elastic Analysis",
              "Analysis using OpenSees.NET",
              "OpenSees", "Analysis")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("element_node_relationship(shell)", "IJKL", "[[No.i,No.j,No.k,No.l(if triangle:-1),material No.,thickness],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("KABE_W", "KABE_W", "[[No.i,No.j,No.k,No.l,bairitsu,rad],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("nodal_force", "F", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("boundary_condition", "B", "[[node No.,X,Y,Z,MX,MY,MZ],...](DataTree) 0-1 variable", GH_ParamAccess.tree, -9999);///
            pManager.AddVectorParameter("local coordinates vector", "l_vec", "[...](DataList)", GH_ParamAccess.list, new Vector3d(-9999,-9999,-9999));///
            pManager.AddNumberParameter("joint condition", "joint", "[[Ele. No., 0 or 1(means i or j), kx, ky, kz, rx, ry, rz(spring value)],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("spring element", "spring", "[[No.i, No.j, kxt, ktc, kyt, kyc, kzt, kzc, rx, ry, rz(spring value)],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("Young's mudulus", "E", "[...](DataList)", GH_ParamAccess.list, new List<double> { 2.1e+8 });///
            pManager.AddNumberParameter("Shear modulus", "poi", "[...](DataList)", GH_ParamAccess.list, new List<double> { 0.3 });///
            pManager.AddNumberParameter("section area", "A", "[...](DataList)", GH_ParamAccess.list, new List<double> { 0.01 });///
            pManager.AddNumberParameter("Second moment of area around y-axis", "Iy", "[...](DataList)", GH_ParamAccess.list, new List<double> { Math.Pow(0.1, 4) / 12.0 });///
            pManager.AddNumberParameter("Second moment of area around z-axis", "Iz", "[...](DataList)", GH_ParamAccess.list, new List<double> { Math.Pow(0.1, 4) / 12.0 });///
            pManager.AddNumberParameter("St Venant's torsion constant", "J", "[...](DataList)", GH_ParamAccess.list, Math.Pow(0.1, 4) / 16.0 * (16.0 / 3.0 - 3.360 * (1.0 - 1.0 / 12.0)));///
            pManager.AddNumberParameter("nodal_force(local coordinate system)", "f", "[[element No.,Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("rigid", "rigid", "rigidDiaphragm nodes", GH_ParamAccess.tree, -9999);///
            pManager.AddIntegerParameter("option", "option", "[0 or 1, 0 or 1](DataList) when 1, sec_f and str_e is calculated ", GH_ParamAccess.list, new List<int> { 1, 1 });///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///0
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree);///1
            pManager.AddNumberParameter("nodal displacement", "D", "[[dxi,dyi,dzi,theta_xi,theta_yi,theta_zi,dxj,dyj,dzj,theta_xj,theta_yj,theta_zj],...](DataTree)", GH_ParamAccess.tree);///2
            pManager.AddNumberParameter("reaction_force", "reac_f", "[[Node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);///3
            pManager.AddNumberParameter("sectional_force", "sec_f", "[[element No.,Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree);///4
            pManager.AddVectorParameter("local coordinates vector", "l_vec", "[...](DataList)", GH_ParamAccess.list);///5
            pManager.AddNumberParameter("element_node_relationship(shell)", "IJKL", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree);///6
            pManager.AddNumberParameter("nodal displacement", "D(shell)", "[[dx1,dy1,dz1,theta_x,theta_y,theta_z],...](DataTree)", GH_ParamAccess.tree);///7
            pManager.AddNumberParameter("stress(shell)", "shell_f", "[[dx1,dy1,dz1,theta_x,theta_y,theta_z],...](DataTree)", GH_ParamAccess.tree);///8
            pManager.AddNumberParameter("KABE_W", "KABE_W", "[[No.i,No.j,No.k,No.l,kabebairitsu],...](DataTree)", GH_ParamAccess.tree);///9
            pManager.AddNumberParameter("shear_w", "shear_w", "[Q1,Q2,...](DataList)", GH_ParamAccess.list);///10
            pManager.AddNumberParameter("spring element", "spring", "[[No.i, No.j, kxt, ktc, kyt, kyc, kzt, kzc, rx, ry, rz(spring value)],...](DataTree)", GH_ParamAccess.tree);///11
            pManager.AddNumberParameter("spring_force", "spring_f", "[[element No.,Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree);///12
            pManager.AddNumberParameter("strain_energy", "str_e", "strain energy", GH_ParamAccess.item);///13
            pManager.AddNumberParameter("nodal displacement (each node)", "D(R)", "[[dxi,dyi,dzi,theta_xi,theta_yi,theta_zi,dxj,dyj,dzj,theta_xj,theta_yj,theta_zj],...](DataTree)", GH_ParamAccess.tree);///14
        }
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            IList<List<GH_Number>> r; IList<List<GH_Number>> ij; IList<List<GH_Number>> ijkl; IList<List<GH_Number>> f_v; List<Vector3d> l_vec = new List<Vector3d>(); IList<List<GH_Number>> rigid;
            List<double> E = new List<double>(); List<double> poi = new List<double>(); List<double> A = new List<double>(); List<double> A1 = new List<double>(); List<double> A2 = new List<double>(); List<double> Cos1 = new List<double>(); List<double> Cos2 = new List<double>();
            List<double> Iy = new List<double>(); List<double> Iz = new List<double>(); List<double> J = new List<double>();
            GH_Structure<GH_Number> disp = new GH_Structure<GH_Number>(); GH_Structure<GH_Number> reac_f = new GH_Structure<GH_Number>(); GH_Structure<GH_Number> sec_f = new GH_Structure<GH_Number>(); IList<List<GH_Number>> joint = new List<List<GH_Number>>(); GH_Structure<GH_Number> dispshell = new GH_Structure<GH_Number>(); GH_Structure<GH_Number> spring_f = new GH_Structure<GH_Number>();
            GH_Structure<GH_Number> shell_f = new GH_Structure<GH_Number>(); var qvec = new Vector3d(1, 0, 0);
            GH_Structure<GH_Number> dispR = new GH_Structure<GH_Number>();
            int n; int m = 0; int m2 = 0; int m3 = 0; int m4 = 0; int nf; int nc = 0; int mc = 0; int mc2 = 0; int e2 = 0;
            var theDomain = new OpenSees.Components.DomainWrapper();
            DA.GetDataList("local coordinates vector", l_vec); if (l_vec[0][0] != -9999 && l_vec[0][1] != -9999 && l_vec[0][2] != -9999) { DA.SetDataList("local coordinates vector", l_vec); }
            DA.GetDataList("Young's mudulus", E); DA.GetDataList("Shear modulus", poi);
            DA.GetDataList("section area", A);
            DA.GetDataList("Second moment of area around y-axis", Iy); DA.GetDataList("Second moment of area around z-axis", Iz); DA.GetDataList("St Venant's torsion constant", J);
            DA.GetDataTree("joint condition", out GH_Structure<GH_Number> _joint);
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r);
            DA.GetDataTree("rigid", out GH_Structure<GH_Number> _rigid);
            var shell_elements = new List<OpenSees.Elements.ElementWrapper>(); List<int> option = new List<int>(); DA.GetDataList("option", option);
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
            ///input nodes******************************************************************************
            r = _r.Branches; n = r.Count;
            for (int i = 0; i < n; i++)
            {
                var x = r[i][0].Value; var y = r[i][1].Value; var z = r[i][2].Value;
                var node = new OpenSees.Components.NodeWrapper(i + 1, 6, x, y, z);
                theDomain.AddNode(node);
            }
            ///*****************************************************************************************
            DA.GetDataTree("boundary_condition", out GH_Structure<GH_Number> _fix);
            var fix = _fix.Branches; var nfix = fix.Count;
            if (_fix.Branches[0][0].Value != -9999)
            {
                for (int i = 0; i < nfix; i++)
                {
                    var j = (int)fix[i][0].Value;
                    var fix1 = (int)fix[i][1].Value; var fix2 = (int)fix[i][2].Value; var fix3 = (int)fix[i][3].Value;
                    var fix4 = (int)fix[i][4].Value; var fix5 = (int)fix[i][5].Value; var fix6 = (int)fix[i][6].Value;
                    if (fix1 == 1) { theDomain.AddSP_Constraint(new OpenSees.Components.Constraints.SP_ConstraintWrapper(j + 1, 0, 0.0, true)); }
                    if (fix2 == 1) { theDomain.AddSP_Constraint(new OpenSees.Components.Constraints.SP_ConstraintWrapper(j + 1, 1, 0.0, true)); }
                    if (fix3 == 1) { theDomain.AddSP_Constraint(new OpenSees.Components.Constraints.SP_ConstraintWrapper(j + 1, 2, 0.0, true)); }
                    if (fix4 == 1) { theDomain.AddSP_Constraint(new OpenSees.Components.Constraints.SP_ConstraintWrapper(j + 1, 3, 0.0, true)); }
                    if (fix5 == 1) { theDomain.AddSP_Constraint(new OpenSees.Components.Constraints.SP_ConstraintWrapper(j + 1, 4, 0.0, true)); }
                    if (fix6 == 1) { theDomain.AddSP_Constraint(new OpenSees.Components.Constraints.SP_ConstraintWrapper(j + 1, 5, 0.0, true)); }
                }
            }
            DA.GetDataTree("element_node_relationship", out GH_Structure<GH_Number> _ij);
            ij = _ij.Branches;
            DA.GetDataTree("element_node_relationship(shell)", out GH_Structure<GH_Number> _ijkl);
            ijkl = _ijkl.Branches;
            DA.GetDataTree("KABE_W", out GH_Structure<GH_Number> _kabe_w);
            var kabe_w = _kabe_w.Branches;
            DA.GetDataTree("spring element", out GH_Structure<GH_Number> _spring);
            var spring = _spring.Branches;
            var new_ij = new List<List<int>>();
            if (_ij.Branches[0][0].Value != -9999) { m = ij.Count; }
            if (_ijkl.Branches[0][0].Value != -9999) { m2 = ijkl.Count; }
            if (_kabe_w.Branches[0][0].Value != -9999) { m3 = kabe_w.Count; }
            if (_spring.Branches[0][0].Value != -9999) { m4 = spring.Count; }
            if (_ij.Branches[0][0].Value != -9999)
            {
                joint = _joint.Branches;
                var joint_No = new List<int>();
                if (_joint.Branches[0][0].Value != -9999)
                {
                    for (int i = 0; i < joint.Count; i++)
                    {
                        joint_No.Add((int)joint[i][0].Value);
                    }
                }
                for (int e = 0; e < m; e++)
                {
                    int i = (int)ij[e][0].Value; int j = (int)ij[e][1].Value;
                    var local_y = new VectorWrapper(new double[] { l_vec[e][0], l_vec[e][1], l_vec[e][2] });
                    var geo = new OpenSees.Elements.CrdTransfs.LinearCrdTransf3dWrapper(local_y);
                    int mat = (int)ij[e][2].Value; int sec = (int)ij[e][3].Value;
                    if (joint_No.Contains(e))
                    {
                        int k = joint_No.IndexOf(e);
                        var x1 = r[i][0].Value; var y1 = r[i][1].Value; var z1 = r[i][2].Value; var x2 = r[j][0].Value; var y2 = r[j][1].Value; var z2 = r[j][2].Value;
                        var local_x = new VectorWrapper(new double[] { x2 - x1, y2 - y1, z2 - z1 });
                        var l_vec2 = rotation(l_vec[e], new Vector3d(x2 - x1, y2 - y1, z2 - z1), 90.0);
                        var local_z = new VectorWrapper(new double[] { l_vec2[0], l_vec2[1], l_vec2[2] });
                        if ((int)joint[k][1].Value == 0)
                        {
                            nc += 1;
                            var node = new OpenSees.Components.NodeWrapper(n + nc, 6, x1, y1, z1);
                            theDomain.AddNode(node);
                            if (joint[k][2].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][2].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, i + 1, local_x, local_y, jointmaterial, 1, 0));
                            }
                            if (joint[k][3].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][3].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, i + 1, local_y, local_z, jointmaterial, 1, 0));
                            }
                            if (joint[k][4].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][4].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, i + 1, local_z, local_x, jointmaterial, 1, 0));
                            }
                            if (joint[k][5].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][5].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, i + 1, local_z, local_x, jointmaterial, 4, 0));
                            }
                            if (joint[k][6].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][6].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, i + 1, local_y, local_z, jointmaterial, 4, 0));
                            }
                            if (joint[k][7].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][7].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, i + 1, local_x, local_y, jointmaterial, 4, 0));
                            }
                            theDomain.AddElement(new OpenSees.Elements.ElasticBeam3dWrapper(e + 1, A[sec], E[mat], E[mat] / 2.0 / (1 + poi[mat]), J[sec], Iy[sec], Iz[sec], n + nc, j + 1, geo, 0, 0, e + 1));
                            new_ij.Add(new List<int>() { n + nc, j + 1 });
                        }
                        else if ((int)joint[k][1].Value == 1)
                        {
                            nc += 1;
                            var node = new OpenSees.Components.NodeWrapper(n + nc, 6, x2, y2, z2);
                            theDomain.AddNode(node);
                            if (joint[k][8].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][8].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, j + 1, local_x, local_y, jointmaterial, 1, 0));
                            }
                            if (joint[k][9].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][9].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, j + 1, local_y, local_z, jointmaterial, 1, 0));
                            }
                            if (joint[k][10].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][10].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, j + 1, local_z, local_x, jointmaterial, 1, 0));
                            }
                            if (joint[k][11].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][11].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, j + 1, local_z, local_x, jointmaterial, 4, 0));
                            }
                            if (joint[k][12].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][12].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, j + 1, local_y, local_z, jointmaterial, 4, 0));
                            }
                            if (joint[k][13].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][13].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, j + 1, local_x, local_y, jointmaterial, 4, 0));
                            }
                            theDomain.AddElement(new OpenSees.Elements.ElasticBeam3dWrapper(e + 1, A[sec], E[mat], E[mat] / 2.0 / (1 + poi[mat]), J[sec], Iy[sec], Iz[sec], i + 1, n + nc, geo, 0, 0, e + 1));
                            new_ij.Add(new List<int>() { i + 1, n + nc });
                        }
                        else if ((int)joint[k][1].Value == 2)
                        {
                            nc += 1;
                            var node = new OpenSees.Components.NodeWrapper(n + nc, 6, x1, y1, z1);
                            theDomain.AddNode(node);
                            if (joint[k][2].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][2].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, i + 1, local_x, local_y, jointmaterial, 1, 0));
                            }
                            if (joint[k][3].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][3].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, i + 1, local_y, local_z, jointmaterial, 1, 0));
                            }
                            if (joint[k][4].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][4].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, i + 1, local_z, local_x, jointmaterial, 1, 0));
                            }
                            if (joint[k][5].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][5].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, i + 1, local_z, local_x, jointmaterial, 4, 0));
                            }
                            if (joint[k][6].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][6].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, i + 1, local_y, local_z, jointmaterial, 4, 0));
                            }
                            if (joint[k][7].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][7].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, i + 1, local_x, local_y, jointmaterial, 4, 0));
                            }
                            nc += 1;
                            node = new OpenSees.Components.NodeWrapper(n + nc, 6, x2, y2, z2);
                            theDomain.AddNode(node);
                            if (joint[k][8].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][8].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, j + 1, local_x, local_y, jointmaterial, 1, 0));
                            }
                            if (joint[k][9].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][9].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, j + 1, local_y, local_z, jointmaterial, 1, 0));
                            }
                            if (joint[k][10].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][10].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, j + 1, local_z, local_x, jointmaterial, 1, 0));
                            }
                            if (joint[k][11].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][11].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, j + 1, local_z, local_x, jointmaterial, 4, 0));
                            }
                            if (joint[k][12].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][12].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, j + 1, local_y, local_z, jointmaterial, 4, 0));
                            }
                            if (joint[k][13].Value != 0)
                            {
                                mc += 1; var jointmaterial = new ElasticMaterialWrapper(mc, joint[k][13].Value, 0.0);
                                theDomain.AddElement(new OpenSees.Elements.ZeroLengthWrapper(m + mc, 3, n + nc, j + 1, local_x, local_y, jointmaterial, 4, 0));
                            }
                            theDomain.AddElement(new OpenSees.Elements.ElasticBeam3dWrapper(e + 1, A[sec], E[mat], E[mat] / 2.0 / (1 + poi[mat]), J[sec], Iy[sec], Iz[sec], n + nc - 1, n + nc, geo, 0, 0, e + 1));
                            new_ij.Add(new List<int>() { n + nc - 1, n + nc });
                        }
                        else
                        {
                            theDomain.AddElement(new OpenSees.Elements.ElasticBeam3dWrapper(e + 1, A[sec], E[mat], E[mat] / 2.0 / (1 + poi[mat]), J[sec], Iy[sec], Iz[sec], i + 1, j + 1, geo, 0, 0, e + 1));
                            new_ij.Add(new List<int>() { i + 1, j + 1 });
                        }
                    }
                    else
                    {
                        theDomain.AddElement(new OpenSees.Elements.ElasticBeam3dWrapper(e + 1, A[sec], E[mat], E[mat] / 2.0 / (1 + poi[mat]), J[sec], Iy[sec], Iz[sec], i + 1, j + 1, geo, 0, 0, e + 1));
                        new_ij.Add(new List<int>() { i + 1, j + 1 });
                    }
                }
            }
            if (_ijkl.Branches[0][0].Value != -9999)
            {
                for (int e = 0; e < m2; e++)
                {
                    int i = (int)ijkl[e][0].Value; int j = (int)ijkl[e][1].Value; int k = (int)ijkl[e][2].Value; int l = (int)ijkl[e][3].Value;
                    int mat = (int)ijkl[e][4].Value; double t = ijkl[e][5].Value;
                    if (l != -1)
                    {
                        var shellmaterial = new OpenSees.Materials.Sections.ElasticMembranePlateSectionWrapper(e + 1, E[mat], poi[mat], t, 0);
                        var ele = new OpenSees.Elements.ShellDKGQWrapper(m + mc + e + 1, i + 1, j + 1, k + 1, l + 1, shellmaterial);
                        theDomain.AddElement(ele); shell_elements.Add(ele);
                    }
                    else
                    {
                        var shellmaterial = new OpenSees.Materials.Sections.ElasticMembranePlateSectionWrapper(e + 1, E[mat], poi[mat], t, 0);
                        var ele = new OpenSees.Elements.ShellDKGTWrapper(m + mc + e + 1, i + 1, j + 1, k + 1, shellmaterial, 0.0, 0.0, 0.0);
                        theDomain.AddElement(ele); shell_elements.Add(ele);
                    }
                }
            }
            if (_kabe_w.Branches[0][0].Value != -9999)
            {
                int ee = 0;
                for (int e = 0; e < m3; e++)
                {
                    int i = (int)kabe_w[e][0].Value; int j = (int)kabe_w[e][1].Value; int k = (int)kabe_w[e][2].Value; int l = (int)kabe_w[e][3].Value;
                    double alpha = kabe_w[e][4].Value;//倍率
                    double rad = kabe_w[e][5].Value;//変形角
                    if (alpha > 0.0)
                    {
                        var ri = new Point3d(r[i][0].Value, r[i][1].Value, r[i][2].Value); var rj = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value); var rk = new Point3d(r[k][0].Value, r[k][1].Value, r[k][2].Value); var rl = new Point3d(r[l][0].Value, r[l][1].Value, r[l][2].Value);
                        var h1 = (rl - ri).Length; var h2 = (rk - rj).Length; var width = (rj - ri).Length; var l1 = (ri - rk).Length; var l2 = (rj - rl).Length;
                        e2 += 1; var material = new ElasticMaterialWrapper(mc + e2, 1.0, 0.0);
                        var cos1 = width / l1; var cos2 = width / l2;
                        var a1 = 1.96 * rad * alpha * width * l1 / (2.0 * h1 * Math.Pow(cos1, 2)); var a2 = 1.96 * rad * alpha * width * l2 / (2.0 * h2 * Math.Pow(cos2, 2));
                        A1.Add(a1); A2.Add(a2); Cos1.Add(cos1); Cos2.Add(cos2);
                        ee += 1;
                        var ele = new OpenSees.Elements.TrussWrapper(m + mc + m2 + ee, 3, i + 1, k + 1, material, a1, 0.0, 0, 0);
                        theDomain.AddElement(ele);
                        ee += 1;
                        ele = new OpenSees.Elements.TrussWrapper(m + mc + m2 + ee, 3, j + 1, l + 1, material, a2, 0.0, 0, 0); theDomain.AddElement(ele);
                    }
                }
            }
            if (_spring.Branches[0][0].Value != -9999)
            {
                DA.SetDataTree(11, _spring);
                var a = new List<double>();
                for (int e = 0; e < m4; e++) { a.Add(0.0); }
                if (spring[0].Count == 12)
                {
                    for (int e = 0; e < m4; e++) { a[e] = spring[e][11].Value; }
                }
                for (int e = 0; e < m4; e++)
                {
                    int i = (int)spring[e][0].Value; int j = (int)spring[e][1].Value;
                    Vector3d x = new Vector3d(r[j][0].Value - r[i][0].Value, r[j][1].Value - r[i][1].Value, r[j][2].Value - r[i][2].Value);
                    Vector3d y = new Vector3d(0, 1, 0);
                    Vector3d z = new Vector3d(0, 0, 1);
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
                    var local_x = new VectorWrapper(new double[] { x[0], x[1], x[2] });
                    var local_y = new VectorWrapper(new double[] { y[0], y[1], y[2] });
                    var kxt = spring[e][2].Value; var kxc = spring[e][3].Value;
                    var kyt = spring[e][4].Value; var kyc = spring[e][5].Value;
                    var kzt = spring[e][6].Value; var kzc = spring[e][7].Value;
                    var mx = spring[e][8].Value; var my = spring[e][9].Value; var mz = spring[e][10].Value;
                    if (kxt == 0) { kxt = 1e-8; }
                    if (kxc == 0) { kxc = 1e-8; }
                    if (kyt == 0) { kyt = 1e-8; }
                    if (kyc == 0) { kyc = 1e-8; }
                    if (kzt == 0) { kzt = 1e-8; }
                    if (kzc == 0) { kzc = 1e-8; }
                    if (mx == 0) { mx = 1e-8; }
                    if (my == 0) { my = 1e-8; }
                    if (mz == 0) { mz = 1e-8; }
                    mc2 += 1; var mat1 = new ElasticMaterialWrapper(mc + mc2 + e2, kxt, 0.0, kxc);
                    mc2 += 1; var mat2 = new ElasticMaterialWrapper(mc + mc2 + e2, kyt, 0.0, kyc);
                    mc2 += 1; var mat3 = new ElasticMaterialWrapper(mc + mc2 + e2, kzt, 0.0, kzc);
                    mc2 += 1; var mat4 = new ElasticMaterialWrapper(mc + mc2 + e2, mx, 0.0, mx);
                    mc2 += 1; var mat5 = new ElasticMaterialWrapper(mc + mc2 + e2, my, 0.0, my);
                    mc2 += 1; var mat6 = new ElasticMaterialWrapper(mc + mc2 + e2, mz, 0.0, mz);
                    var direction = new IDWrapper(new int[] { 6, 1, 2, 3, 4, 5 });
                    var springmaterial = new UniaxialMaterialWrapper[] { mat1, mat2, mat3, mat4, mat5, mat6 };
                    theDomain.AddElement(new OpenSees.Elements.TwoNodeLinkWrapper(m + mc + m2 + m3 * 2 + e + 1, 3, i + 1, j + 1, direction, springmaterial, local_y, local_x, new VectorWrapper(0), new VectorWrapper(0), 0, 0));
                }
            }
            if (!DA.GetDataTree("nodal_force", out GH_Structure<GH_Number> _f_v)) { }
            else if (_f_v.Branches[0][0].Value != -9999)
            {
                f_v = _f_v.Branches; nf = f_v.Count;
                var theSeries = new OpenSees.Components.Timeseries.LinearSeriesWrapper();
                var theLoadPattern = new OpenSees.Components.LoadPatterns.LoadPatternWrapper(1);
                theLoadPattern.SetTimeSeries(theSeries);
                theDomain.AddLoadPattern(theLoadPattern);
                qvec = new Vector3d(0, 0, 0);//for yane(direction vector of Q)
                for (int i = 0; i < nf; i++)
                {
                    int j = (int)f_v[i][0].Value;
                    double fx = f_v[i][1].Value; double fy = f_v[i][2].Value; double fz = f_v[i][3].Value;
                    double mx = f_v[i][4].Value; double my = f_v[i][5].Value; double mz = f_v[i][6].Value;
                    var theLoadValues = new VectorWrapper(6);
                    theLoadValues.Set(0, fx); theLoadValues.Set(1, fy); theLoadValues.Set(2, fz);
                    theLoadValues.Set(3, mx); theLoadValues.Set(4, my); theLoadValues.Set(5, mz);
                    theDomain.AddNodalLoad(new OpenSees.Components.Loads.NodalLoadWrapper(i + 1, j + 1, theLoadValues, false), 1);
                    qvec[0] += fx; qvec[1] += fy;
                }
            }
            rigid = _rigid.Branches;//剛床
            if (rigid[0][0].Value != -9999)
            {
                var k = 0; var mrigid = 0;
                for (int i = 0; i < rigid.Count; i++)
                {
                    var nodes = new int[rigid[i].Count]; var xave = 0.0; var yave = 0.0; var zave = 0.0;
                    for (int j = 0; j < rigid[i].Count; j++)
                    {
                        nodes[j] = (int)(rigid[i][j].Value + 1);
                        xave += r[(int)rigid[i][j].Value][0].Value; yave += r[(int)rigid[i][j].Value][1].Value; zave += r[(int)rigid[i][j].Value][2].Value;
                    }
                    xave /= (double)rigid[i].Count; yave /= (double)rigid[i].Count; zave /= (double)rigid[i].Count;
                    int ii = theDomain.GetNumNodes();
                    var node = new OpenSees.Components.NodeWrapper(ii + 1, 6, xave, yave, zave);
                    theDomain.AddNode(node);
                    mrigid += 1; var mat1 = new ElasticMaterialWrapper(mc + mc2 + e2 + mrigid, 1e+12, 0.0, 1e+12);
                    mrigid += 1; var mat2 = new ElasticMaterialWrapper(mc + mc2 + e2 + mrigid, 1e-12, 0.0, 1e-12);
                    mrigid += 1; var mat3 = new ElasticMaterialWrapper(mc + mc2 + e2 + mrigid, 1e-12, 0.0, 1e-12);
                    mrigid += 1; var mat4 = new ElasticMaterialWrapper(mc + mc2 + e2 + mrigid, 1e-12, 0.0, 1e-12);
                    mrigid += 1; var mat5 = new ElasticMaterialWrapper(mc + mc2 + e2 + mrigid, 1e-12, 0.0, 1e-12);
                    mrigid += 1; var mat6 = new ElasticMaterialWrapper(mc + mc2 + e2 + mrigid, 1e-12, 0.0, 1e-12);
                    var direction = new IDWrapper(new int[] { 0, 1, 2, 3, 4, 5 });
                    var springmaterial = new UniaxialMaterialWrapper[] { mat1, mat2, mat3, mat4, mat5, mat6 };
                    for (int j = 0; j < nodes.Length; j++)
                    {
                        Vector3d x = new Vector3d(r[nodes[j] - 1][0].Value - xave, r[nodes[j] - 1][1].Value - yave, r[nodes[j] - 1][2].Value - zave);
                        Vector3d y = new Vector3d(0, 1, 0);
                        Vector3d z = new Vector3d(0, 0, 1);
                        if (Math.Abs(x[0]) <= 5e-3 && Math.Abs(x[1]) <= 5e-3)
                        {
                            y = rotation(x, new Vector3d(0, 1, 0), 90);
                            z = rotation(y, x, 90);
                            y = rotation(z, x, -90);
                        }
                        else
                        {
                            y = rotation(x, new Vector3d(0, 0, 1), 90);
                            y[2] = 0.0;
                            z = rotation(y, x, 90);
                            y = rotation(z, x, -90);
                        }
                        var local_x = new VectorWrapper(new double[] { x[0], x[1], x[2] });
                        var local_y = new VectorWrapper(new double[] { y[0], y[1], y[2] });
                        k += 1; theDomain.AddElement(new OpenSees.Elements.TwoNodeLinkWrapper(m + mc + m2 + m3 * 2 + m4 + k, 3, ii + 1, nodes[j], direction, springmaterial, local_y, local_x, new VectorWrapper(0), new VectorWrapper(0), 0, 0));
                    }
                    //theDomain.CreateRigidDiaphragm(ii + 1, new IDWrapper(nodes), 2);
                    theDomain.AddSP_Constraint(new OpenSees.Components.Constraints.SP_ConstraintWrapper(ii + 1, 2, 0.0, true));
                    //theDomain.AddSP_Constraint(new OpenSees.Components.Constraints.SP_ConstraintWrapper(ii + 1, 3, 0.0, true));
                    //theDomain.AddSP_Constraint(new OpenSees.Components.Constraints.SP_ConstraintWrapper(ii + 1, 4, 0.0, true));
                }
            }
            qvec = qvec / qvec.Length;
            var theModel = new AnalysisModelWrapper();
            var theSolnAlgo = new OpenSees.Algorithms.LinearWrapper();//var step = 10;
            //var theIntegrator = new OpenSees.Integrators.Static.LoadControlWrapper(1.0 / (double)step, step, 1.0 / (double)step, 1.0 / (double)step);
            var theIntegrator = new OpenSees.Integrators.Static.LoadControlWrapper(1.0, 1, 1.0, 1.0);
            var theHandler = new OpenSees.Handlers.PlainHandlerWrapper();
            var theRCM = new OpenSees.GraphNumberers.RCMWrapper(false);
            var theNumberer = new OpenSees.Numberers.DOF_NumbererWrapper(theRCM);
            var theSolver = new OpenSees.Systems.Linears.BandSPDLinLapackSolverWrapper();
            var theSOE = new OpenSees.Systems.Linears.BandSPDLinSOEWrapper(theSolver);
            var theTest = new OpenSees.ConvergenceTests.CTestNormDispIncrWrapper(1e-8, 6, 2, 2, 1.0e10);
            var theAnalysis = new OpenSees.Analysis.StaticAnalysisWrapper(theDomain, theHandler, theNumberer, theModel, theSolnAlgo, theSOE, theIntegrator, theTest);
            theAnalysis.Analyze(1);
            if (_ij.Branches[0][0].Value != -9999)
            {
                for (int e = 0; e < m; e++)
                {
                    List<GH_Number> dlist = new List<GH_Number>();
                    var d = theDomain.GetNode((int)new_ij[e][0]).GetCommitDisp();
                    for (int j = 0; j < 6; j++) { dlist.Add(new GH_Number(d[j])); }
                    d = theDomain.GetNode((int)new_ij[e][1]).GetCommitDisp();
                    for (int j = 0; j < 6; j++) { dlist.Add(new GH_Number(d[j])); }
                    disp.AppendRange(dlist, new GH_Path(e));
                }
                for (int i = 0; i < n; i++)
                {
                    List<GH_Number> dlist = new List<GH_Number>();
                    var d = theDomain.GetNode(i+1).GetCommitDisp();
                    for (int j = 0; j < 6; j++) { dlist.Add(new GH_Number(d[j])); }
                    dispR.AppendRange(dlist, new GH_Path(i));
                }
                DA.SetDataTree(2, disp);
                DA.SetDataTree(14, dispR);
            }
            if (_ijkl.Branches[0][0].Value != -9999)
            {
                for (int e = 0; e < m2; e++)
                {
                    List<GH_Number> dlist = new List<GH_Number>(); List<GH_Number> flist = new List<GH_Number>();
                    var d = theDomain.GetNode((int)ijkl[e][0].Value + 1).GetCommitDisp();
                    for (int j = 0; j < 6; j++) { dlist.Add(new GH_Number(d[j])); }
                    d = theDomain.GetNode((int)ijkl[e][1].Value + 1).GetCommitDisp();
                    for (int j = 0; j < 6; j++) { dlist.Add(new GH_Number(d[j])); }
                    d = theDomain.GetNode((int)ijkl[e][2].Value + 1).GetCommitDisp();
                    for (int j = 0; j < 6; j++) { dlist.Add(new GH_Number(d[j])); }
                    if (ijkl[e][3].Value >= 0)
                    {
                        d = theDomain.GetNode((int)ijkl[e][3].Value + 1).GetCommitDisp();
                        for (int j = 0; j < 6; j++) { dlist.Add(new GH_Number(d[j])); }
                    }
                    dispshell.AppendRange(dlist, new GH_Path(e));
                    var fe = theDomain.GetElement(m + mc + e + 1).GetResistingForceIncInertia();
                    for (int i = 0; i < fe.Length; i++) { flist.Add(new GH_Number(fe[i])); }
                    var K = theDomain.GetElement(m + mc + e + 1).GetTangentStiff();
                    int ni = (int)ijkl[e][0].Value; int nj = (int)ijkl[e][1].Value; int nk = (int)ijkl[e][2].Value; int nl = (int)ijkl[e][3].Value;
                    //if (K.Length == 144) { Rhino.RhinoApp.WriteLine(" e=" + e.ToString() + " " + ni.ToString() + " " + nj.ToString() + " " + nk.ToString() + " " + nl.ToString()); }
                    //var v = new VectorWrapper(); theDomain.GetElement(m + mc + e + 1).GetVectorResponse(1, ref v);
                    //for (int i = 0; i < v.Size(); i++) { flist.Add(new GH_Number(v[i])); }
                    shell_f.AppendRange(flist, new GH_Path(e));
                }
                DA.SetDataTree(7, dispshell); DA.SetDataTree(8, shell_f);
            }
            DA.SetDataTree(0, _r);
            DA.SetDataTree(1, _ij);
            DA.SetDataTree(6, _ijkl);
            theDomain.CalculateNodalReactions(1);
            for (int i = 0; i < fix.Count; i++)
            {
                int e = (int)fix[i][0].Value;
                var reac = theDomain.GetNode(e + 1).GetReactionsVector();
                List<GH_Number> reaclist = new List<GH_Number>();
                reaclist.Add(new GH_Number(e));
                for (int j = 0; j < 6; j++)
                {
                    reaclist.Add(new GH_Number(reac[j]));
                }
                reac_f.AppendRange(reaclist, new GH_Path(i));
            }
            DA.SetDataTree(3, reac_f);
            double[] element_sectional_force(Matrix u, double l, double x, double e, double g, double a, double iy, double iz, double j)
            {
                var N = -e * a / l * (-u[0, 0] + u[6, 0]);
                var Qy = e * iz * (12 / Math.Pow(l, 3) * u[1, 0] + 6 / Math.Pow(l, 2) * u[5, 0] - 12 / Math.Pow(l, 3) * u[7, 0] + 6 / Math.Pow(l, 2) * u[11, 0]);
                var Qz = e * iy * (12 / Math.Pow(l, 3) * u[2, 0] - 6 / Math.Pow(l, 2) * u[4, 0] - 12 / Math.Pow(l, 3) * u[8, 0] - 6 / Math.Pow(l, 2) * u[10, 0]);
                var Mx = g * j / l * (-u[3, 0] + u[9, 0]);
                var My = e * iy / Math.Pow(l, 3) * ((-6 * l + 12 * x) * u[2, 0] + (4 * Math.Pow(l, 2) - 6 * l * x) * u[4, 0] + (6 * l - 12 * x) * u[8, 0] + (2 * Math.Pow(l, 2) - 6 * l * x) * u[10, 0]);
                var Mz = e * iz / Math.Pow(l, 3) * ((6 * l - 12 * x) * u[1, 0] + (4 * Math.Pow(l, 2) - 6 * l * x) * u[5, 0] - (6 * l - 12 * x) * u[7, 0] + (2 * Math.Pow(l, 2) - 6 * l * x) * u[11, 0]);
                return new double[] { N, Qy, Qz, Mx, My, Mz };
            }
            if (option[0] == 1 && _ij.Branches[0][0].Value != -9999)
            {
                if (!DA.GetDataTree("nodal_force(local coordinate system)", out GH_Structure<GH_Number> _load_l)) { }
                else
                {
                    Matrix transmatrix(double l, double lx, double ly, double lz, double a)
                    {
                        lx /= l; double mx = ly / l; double nx = lz / l; a = a * Math.PI / 180.0;
                        double my; var ny = 0.0; double mz; double nz = 0.0;
                        if (Math.Abs(lx) <= 5e-3 && Math.Abs(ly) <= 5e-3)
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
                    ij = _ij.Branches; var d = disp.Branches;
                    var f_i = new double[] { 0, 0, 0, 0, 0, 0 }; var f_j = new double[] { 0, 0, 0, 0, 0, 0 }; var f_c = new double[] { 0, 0, 0, 0, 0, 0 }; DA.GetDataTree("nodal_force(local coordinate system)", out GH_Structure<GH_Number> _total_load_l); var total_load = _total_load_l.Branches;
                    for (int e = 0; e < ij.Count; e++)
                    {
                        var f_e = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                        int ni = (int)ij[e][0].Value; int nj = (int)ij[e][1].Value; double theta = ij[e][4].Value; int mat = (int)ij[e][2].Value; int sec = (int)ij[e][3].Value;
                        var lx = r[nj][0].Value - r[ni][0].Value; var ly = r[nj][1].Value - r[ni][1].Value; var lz = r[nj][2].Value - r[ni][2].Value;
                        double l = Math.Sqrt(Math.Pow(lx, 2) + Math.Pow(ly, 2) + Math.Pow(lz, 2));
                        Matrix tr = transmatrix(l, lx, ly, lz, theta);
                        Matrix ug = new Matrix(12, 1);
                        ///for (int i = 0; i < 12; i++) { ug[i, 0] = d[e][i].Value; }
                        var di = theDomain.GetNode(new_ij[e][0]).GetCommitDisp(); var dj = theDomain.GetNode(new_ij[e][1]).GetCommitDisp();
                        for (int i = 0; i < 6; i++) { ug[i, 0] = di[i]; }
                        for (int i = 0; i < 6; i++) { ug[i + 6, 0] = dj[i]; }
                        var ue = tr * ug;
                        if (_load_l.Branches[0][0].Value != -9999)
                        {
                            double rad = theta * Math.PI / 180; var cs = Math.Cos(rad); var sn = Math.Sin(rad);
                            f_e[0] += -total_load[e][1].Value;///Ni
                            f_e[1] += -total_load[e][2].Value * cs - total_load[e][3].Value * sn;///Qyi
                            f_e[2] += -total_load[e][2].Value * sn - total_load[e][3].Value * cs;///Qzi
                            f_e[3] += -total_load[e][4].Value;///Mxi
                            f_e[4] += -total_load[e][5].Value * cs - total_load[e][6].Value * sn;///Myi
                            f_e[5] += total_load[e][5].Value * sn - total_load[e][6].Value * cs;///Mzi***
                            f_e[6] += -total_load[e][7].Value;///Nj
                            f_e[7] += -total_load[e][8].Value * cs - total_load[e][9].Value * sn;///Qyj
                            f_e[8] += -total_load[e][9].Value * sn - total_load[e][9].Value * cs;///Qzj
                            f_e[9] += -total_load[e][10].Value;///Mxj
                            f_e[10] += -total_load[e][11].Value * cs - total_load[e][12].Value * sn;///Myj
                            f_e[11] += total_load[e][11].Value * sn - total_load[e][12].Value * cs;///Mzj***
                            f_e[12] += total_load[e][13].Value;///Nc
                            f_e[13] += total_load[e][14].Value * cs + total_load[e][15].Value * sn;///Qyc
                            f_e[14] += total_load[e][14].Value * sn + total_load[e][15].Value * cs;///Qzc
                            f_e[15] += total_load[e][16].Value;///Mxc
                            f_e[16] += total_load[e][17].Value * cs + total_load[e][18].Value * sn;///Myc
                            f_e[17] += -total_load[e][17].Value * sn + total_load[e][18].Value * cs;///Mzc***
                        }
                        var f_ei = element_sectional_force(ue, l, 0, E[mat], E[mat] / 2.0 / (1 + poi[mat]), A[sec], Iy[sec], Iz[sec], J[sec]);
                        var f_ej = element_sectional_force(ue, l, l, E[mat], E[mat] / 2.0 / (1 + poi[mat]), A[sec], Iy[sec], Iz[sec], J[sec]);
                        var f_ec = element_sectional_force(ue, l, l / 2.0, E[mat], E[mat] / 2.0 / (1 + poi[mat]), A[sec], Iy[sec], Iz[sec], J[sec]);
                        for (int i = 0; i < 6; i++) { f_e[i] += f_ei[i]; }
                        for (int i = 0; i < 6; i++) { f_e[i + 6] -= f_ej[i]; }
                        for (int i = 0; i < 6; i++) { f_e[i + 12] += f_ec[i]; }
                        List<GH_Number> flist = new List<GH_Number>();
                        for (int j = 0; j < 18; j++)
                        {
                            flist.Add(new GH_Number(f_e[j]));
                        }
                        sec_f.AppendRange(flist, new GH_Path(e));
                    }
                    DA.SetDataTree(4, sec_f);

                    if (_spring.Branches[0][0].Value != -9999)//ばねの断面力
                    {
                        for (int e = 0; e < m4; e++)
                        {
                            int ni = (int)spring[e][0].Value; int nj = (int)spring[e][1].Value; var theta = 0.0;
                            var kxt = spring[e][2].Value; var kxc = spring[e][3].Value; var kyt = spring[e][4].Value; var kyc = spring[e][5].Value; var kzt = spring[e][6].Value; var kzc = spring[e][7].Value; var mx = spring[e][8].Value; var my = spring[e][9].Value; var mz = spring[e][10].Value;
                            if (spring[0].Count == 12) { theta = spring[e][11].Value; }
                            var lx = r[nj][0].Value - r[ni][0].Value; var ly = r[nj][1].Value - r[ni][1].Value; var lz = r[nj][2].Value - r[ni][2].Value;
                            double l = Math.Sqrt(Math.Pow(lx, 2) + Math.Pow(ly, 2) + Math.Pow(lz, 2));
                            Matrix tr = transmatrix(l, lx, ly, lz, theta);
                            Matrix ug = new Matrix(12, 1);
                            var di = theDomain.GetNode(ni + 1).GetCommitDisp(); var dj = theDomain.GetNode(nj + 1).GetCommitDisp();
                            for (int i = 0; i < 6; i++) { ug[i, 0] = di[i]; }
                            for (int i = 0; i < 6; i++) { ug[i + 6, 0] = dj[i]; }
                            var ue = tr * ug;
                            var u = ue[6, 0] - ue[0, 0];//軸ひずみ
                            var v = ue[7, 0] - ue[1, 0];//せん断ひずみy
                            var w = ue[8, 0] - ue[2, 0];//せん断ひずみz
                            var rx = ue[9, 0] - ue[3, 0];//回転ひずみx(ねじり)
                            var ry = ue[10, 0] - ue[4, 0];//回転ひずみy
                            var rz = ue[11, 0] - ue[5, 0];//回転ひずみz
                            var N = 0.0; var Qy = 0.0; var Qz = 0.0; var Mx = 0.0; var My = 0.0; var Mz = 0.0;
                            if (u > 0) { N = kxt * u; } else { N = kxc * u; }//軸力
                            if (v > 0) { Qy = kyt * v; } else { Qy = kyc * v; }//せん断力y
                            if (w > 0) { Qz = kzt * w; } else { Qz = kzc * w; }//せん断力z
                            Mx = mx * rx; My = my * ry; Mz = mz * rz;
                            List<GH_Number> flist = new List<GH_Number>();
                            flist.Add(new GH_Number(N)); flist.Add(new GH_Number(Qy)); flist.Add(new GH_Number(Qz)); flist.Add(new GH_Number(Mx)); flist.Add(new GH_Number(My)); flist.Add(new GH_Number(Mz));
                            spring_f.AppendRange(flist, new GH_Path(e));
                        }
                        DA.SetDataTree(12, spring_f);
                    }
                }
                if (_kabe_w.Branches[0][0].Value != -9999)
                {
                    GH_Structure<GH_Number> kabe_w_new = new GH_Structure<GH_Number>(); e2 = 0; var shear_w = new List<double>();
                    for (int e = 0; e < m3; e++)
                    {
                        int i = (int)kabe_w[e][0].Value; int j = (int)kabe_w[e][1].Value; int k = (int)kabe_w[e][2].Value; int l = (int)kabe_w[e][3].Value; double alpha = kabe_w[e][4].Value;
                        if (alpha > 0.0)
                        {
                            var di = theDomain.GetNode(i + 1).GetCommitDisp(); var dj = theDomain.GetNode(j + 1).GetCommitDisp(); var dk = theDomain.GetNode(k + 1).GetCommitDisp(); var dl = theDomain.GetNode(l + 1).GetCommitDisp();//4隅の節点変位
                            var ri = new Point3d(r[i][0].Value, r[i][1].Value, r[i][2].Value); var rj = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value); var rk = new Point3d(r[k][0].Value, r[k][1].Value, r[k][2].Value); var rl = new Point3d(r[l][0].Value, r[l][1].Value, r[l][2].Value);//4隅の座標
                            var ri_after = new Point3d(r[i][0].Value + di[0], r[i][1].Value + di[1], r[i][2].Value + di[2]); var rj_after = new Point3d(r[j][0].Value + dj[0], r[j][1].Value + dj[1], r[j][2].Value + dj[2]); var rk_after = new Point3d(r[k][0].Value + dk[0], r[k][1].Value + dk[1], r[k][2].Value + dk[2]); var rl_after = new Point3d(r[l][0].Value + dl[0], r[l][1].Value + dl[1], r[l][2].Value + dl[2]);//4隅の座標(変位後)
                            var h1 = (rk - rj).Length; var h2 = (ri - rl).Length; var width = (rj - ri).Length; var l1 = (ri - rk).Length; var l2 = (rj - rl).Length; var l1_after = (ri_after - rk_after).Length; var l2_after = (rj_after - rl_after).Length;
                            var N1 = A1[e2] / l1 * (l1_after - l1); var N2 = A1[e2] / l2 * (l2_after - l2);
                            List<GH_Number> klist = new List<GH_Number>();
                            var v1 = (rl + rk) / 2.0 - (ri + rj) / 2.0; var v2 = (rk + rj) / 2.0 - (rl + ri) / 2.0; var v3 = v2; var direction = 0;
                            if (v1[2] > v2[2]) { v3 = v1; }//辺の中点を結んだベクトルのうちz座標に差があるものをv3としてその角度で壁か屋根か判断
                            if (v3[2] > Math.Sqrt(Math.Pow(v3[0], 2) + Math.Pow(v3[1], 2)))//壁の時
                            {
                                if (Math.Abs(ri[2] - rj[2]) < Math.Abs(rj[2] - rk[2]))//ij辺が幅方向の時
                                {
                                    var Q = Math.Abs(N1 * Cos1[e2]) + Math.Abs(N2 * Cos2[e2]); shear_w.Add(Q);
                                }
                                else//ij辺が高さ方向の時
                                {
                                    var Q = Math.Abs(N1 * (1 - Math.Pow(Cos1[e2], 2))) + Math.Abs(N2 * (1 - Math.Pow(Cos2[e2], 2))); shear_w.Add(Q);
                                    direction = 1;
                                }
                            }
                            else//屋根の時
                            {
                                if (qvec[0] > qvec[1])//X方向荷重寺
                                {
                                    if (Math.Abs((rj - ri)[0]) > Math.Abs((rk - rj)[0]))//ij辺がX方向の時
                                    {
                                        var Q = Math.Abs(N1 * Cos1[e2]) + Math.Abs(N2 * Cos2[e2]); shear_w.Add(Q);
                                    }
                                    else//ij辺がY方向の時
                                    {
                                        var Q = Math.Abs(N1 * (1 - Math.Pow(Cos1[e2], 2))) + Math.Abs(N2 * (1 - Math.Pow(Cos2[e2], 2))); shear_w.Add(Q);
                                        direction = 1;
                                    }
                                }
                                else//Y方向荷重寺
                                {
                                    if (Math.Abs((rj - ri)[0]) < Math.Abs((rk - rj)[0]))//ij辺がY方向の時
                                    {
                                        var Q = Math.Abs(N1 * Cos1[e2]) + Math.Abs(N2 * Cos2[e2]); shear_w.Add(Q);
                                    }
                                    else//ij辺がX方向の時
                                    {
                                        var Q = Math.Abs(N1 * (1 - Math.Pow(Cos1[e2], 2))) + Math.Abs(N2 * (1 - Math.Pow(Cos2[e2], 2))); shear_w.Add(Q);
                                        direction = 1;
                                    }
                                }
                            }
                            klist.Add(new GH_Number(i)); klist.Add(new GH_Number(j)); klist.Add(new GH_Number(k)); klist.Add(new GH_Number(l));
                            klist.Add(new GH_Number(kabe_w[e][4].Value)); klist.Add(new GH_Number(kabe_w[e][5].Value)); klist.Add(new GH_Number(direction));
                            kabe_w_new.AppendRange(klist, new GH_Path(e));
                            e2 += 1;
                        }
                        else
                        {
                            List<GH_Number> klist = new List<GH_Number>();
                            klist.Add(new GH_Number(i)); klist.Add(new GH_Number(j)); klist.Add(new GH_Number(k)); klist.Add(new GH_Number(l));
                            klist.Add(new GH_Number(kabe_w[e][4].Value)); klist.Add(new GH_Number(kabe_w[e][5].Value)); klist.Add(new GH_Number(-1));
                            kabe_w_new.AppendRange(klist, new GH_Path(e));
                            shear_w.Add(0.0);
                        }
                    }
                    DA.SetDataTree(9, kabe_w_new);
                    DA.SetDataList("shear_w", shear_w);
                }
            }
            if (option[1] == 1)
            {
                double str_e = 0;
                if (_f_v.Branches[0][0].Value != -9999)
                {
                    f_v = _f_v.Branches;
                    for (int i = 0; i < f_v.Count; i++)
                    {
                        int e = (int)f_v[i][0].Value;
                        List<GH_Number> dlist = new List<GH_Number>();
                        var d_v = theDomain.GetNode(e + 1).GetCommitDisp();
                        for (int j = 0; j < 6; j++)
                        {
                            str_e += f_v[i][j + 1].Value * d_v[j] * 0.50;
                        }
                    }
                }
                DA.SetData("strain_energy", str_e);
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
                return OpenSeesUtility.Properties.Resources.ElasticAnalysis;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("f1223eb8-24de-46d0-8141-b6c8cdae1197"); }
        }
    }
}