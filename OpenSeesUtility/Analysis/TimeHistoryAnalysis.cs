using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using OpenSees;
using OpenSees.Materials.Uniaxials;
///****************************************

namespace OpenSeesUtility
{
    public class TimeHistoryAnalysis : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public TimeHistoryAnalysis()
          : base("TimeHistory Analysis using OpenSees", "Time-history Analysis",
              "Time-history Analysis using OpenSees.NET",
              "OpenSees", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("element_node_relationship(shell)", "IJKL", "[[No.i,No.j,No.k,No.l(if triangle:-1),material No.,thickness],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("KABE_W", "KABE_W", "[[No.i,No.j,No.k,No.l,bairitsu,rad],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("nodal_force", "F", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("boundary_condition", "B", "[[node No.,X,Y,Z,MX,MY,MZ],...](DataTree) 0-1 variable", GH_ParamAccess.tree, -9999);///
            pManager.AddVectorParameter("local coordinates vector", "l_vec", "[...](DataList)", GH_ParamAccess.list, new Vector3d(-9999, -9999, -9999));///
            pManager.AddNumberParameter("joint condition", "joint", "[[Ele. No., 0 or 1(means i or j), kx, ky, kz, rx, ry, rz(spring value)],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("spring element", "spring", "[[No.i, No.j, kxt, ktc, kyt, kyc, kzt, kzc, rx, ry, rz(spring value)],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("damper", "damper", "[[No.i, No.j, Kd[kN/m], Cd[kN/(m/sec)^(1/ad), ad]],...](DataTree)", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("Young's mudulus", "E", "[...](DataList)", GH_ParamAccess.list, new List<double> { 2.1e+8 });///
            pManager.AddNumberParameter("Shear modulus", "poi", "[...](DataList)", GH_ParamAccess.list, new List<double> { 0.3 });///
            pManager.AddNumberParameter("section area", "A", "[...](DataList)", GH_ParamAccess.list, new List<double> { 0.01 });///
            pManager.AddNumberParameter("Second moment of area around y-axis", "Iy", "[...](DataList)", GH_ParamAccess.list, new List<double> { Math.Pow(0.1, 4) / 12.0 });///
            pManager.AddNumberParameter("Second moment of area around z-axis", "Iz", "[...](DataList)", GH_ParamAccess.list, new List<double> { Math.Pow(0.1, 4) / 12.0 });///
            pManager.AddNumberParameter("St Venant's torsion constant", "J", "[...](DataList)", GH_ParamAccess.list, Math.Pow(0.1, 4) / 16.0 * (16.0 / 3.0 - 3.360 * (1.0 - 1.0 / 12.0)));///
            pManager.AddNumberParameter("rigid", "rigid", "rigidDiaphragm nodes", GH_ParamAccess.tree, -9999);///
            pManager.AddNumberParameter("T", "T", "Natural period", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("time", "time", "[time1,time2,...](DataList)", GH_ParamAccess.list);
            pManager.AddNumberParameter("acc", "acc", "[acc1,acc2,...](DataList)", GH_ParamAccess.list);
            pManager.AddIntegerParameter("step", "step", "The number of calculation steps (dt is automatically calculated by (time size) / ( step - 1 ))", GH_ParamAccess.item, 5000);
            pManager.AddIntegerParameter("damping", "damping", "0:stiffness proportional, 1:mass proportional, 2:rayleigh", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("h", "h", "damping ratio", GH_ParamAccess.item, 0.02);
            pManager.AddNumberParameter("alpha/beta", "alpha/beta", "alphaM+betaK when 0, alpha=2*pi*omega1,beta=2*pi/omega1", GH_ParamAccess.list, new List<double> { 0, 0});
            pManager.AddTextParameter("name", "name", "output folder name (default:wave1)", GH_ParamAccess.item, "wave1");
            pManager.AddBooleanParameter("start", "start", "if true, time-history analysis will be carried out", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("T", "T", "natural period", GH_ParamAccess.list);
            pManager.AddNumberParameter("Displacement history during earthquake in X direction", "D(X)", "[[dxi,dyi,dzi,theta_xi,theta_yi,theta_zi,dxj,dyj,dzj,theta_xj,theta_yj,theta_zj],...](DataTree)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Displacement history during earthquake in Y direction", "D(Y)", "[[dxi,dyi,dzi,theta_xi,theta_yi,theta_zi,dxj,dyj,dzj,theta_xj,theta_yj,theta_zj],...](DataTree)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Sectional force history during earthquake in X direction", "sec_f(X)", "[[Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Sectional force history during earthquake in Y direction", "sec_f(Y)", "[[Pxi,Pyi,Pzi,Mxi,Myi,Mzi,Pxj,Pyj,Pzj,Mxj,Myj,Mzj,Pxc,Pyc,Pzc,Mxc,Myc,Mzc],...](DataTree)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("time", "time", "[time1,time2,...](DataList)", GH_ParamAccess.list);
            pManager.AddNumberParameter("acc", "acc", "[acc1,acc2,...](DataTree)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("damping displacement history during earthquake in X direction", "dampD(X)", "[[disp1,disp2...]...](DataTree)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("damping displacement history during earthquake in Y direction", "dampD(Y)", "[[disp1,disp2...]...](DataTree)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("damping force history during earthquake in X direction", "damp_f(X)", "[[force1,force2...]...](DataTree)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("damping force history during earthquake in Y direction", "damp_f(Y)", "[[force1,force2...]...](DataTree)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Velocity history during earthquake in X direction", "V(X)", "[[dxi,dyi,dzi,theta_xi,theta_yi,theta_zi,dxj,dyj,dzj,theta_xj,theta_yj,theta_zj],...](DataTree)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Velocity history during earthquake in Y direction", "V(Y)", "[[dxi,dyi,dzi,theta_xi,theta_yi,theta_zi,dxj,dyj,dzj,theta_xj,theta_yj,theta_zj],...](DataTree)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Acceleration history during earthquake in X direction", "A(X)", "[[dxi,dyi,dzi,theta_xi,theta_yi,theta_zi,dxj,dyj,dzj,theta_xj,theta_yj,theta_zj],...](DataTree)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("Acceleration history during earthquake in Y direction", "A(Y)", "[[dxi,dyi,dzi,theta_xi,theta_yi,theta_zi,dxj,dyj,dzj,theta_xj,theta_yj,theta_zj],...](DataTree)", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var time = new List<double>(); var acc = new List<double>(); var foldername = "wave1";　DA.GetData("name", ref foldername);
            if (!DA.GetDataList("time", time)) return; if (!DA.GetDataList("acc", acc)) return;
            if (!DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r)) return; var T = 0.0; DA.GetData("T", ref T); var Tlist = new List<double> { T, T };
            var start = false; DA.GetData("start", ref start);
            var damping = 0; DA.GetData("damping", ref damping);
            var l_vec = new List<Vector3d>();
            List<double> E = new List<double>(); List<double> poi = new List<double>(); List<double> A = new List<double>(); List<double> A1 = new List<double>(); List<double> A2 = new List<double>(); List<double> Cos1 = new List<double>(); List<double> Cos2 = new List<double>();
            List<double> Iy = new List<double>(); List<double> Iz = new List<double>(); List<double> J = new List<double>(); IList<List<GH_Number>> joint = new List<List<GH_Number>>(); IList<List<GH_Number>> rigid;
            int n; int m = 0; int m2 = 0; int m3 = 0; int m4 = 0; int m5 = 0; int nf; int nc = 0; int mc = 0; int step = 5000; int mc2 = 0; int e2 = 0;
            DA.GetDataList("local coordinates vector", l_vec); DA.GetData("step", ref step);
            DA.GetDataList("Young's mudulus", E); DA.GetDataList("Shear modulus", poi);
            DA.GetDataList("section area", A); DA.GetDataList("Second moment of area around y-axis", Iy); DA.GetDataList("Second moment of area around z-axis", Iz); DA.GetDataList("St Venant's torsion constant", J);
            DA.GetDataTree("joint condition", out GH_Structure<GH_Number> _joint);
            DA.GetDataTree("element_node_relationship", out GH_Structure<GH_Number> _ij);
            DA.GetDataTree("boundary_condition", out GH_Structure<GH_Number> _fix);
            DA.GetDataTree("element_node_relationship(shell)", out GH_Structure<GH_Number> _ijkl);
            DA.GetDataTree("nodal_force", out GH_Structure<GH_Number> _f_v);
            DA.GetDataTree("KABE_W", out GH_Structure<GH_Number> _kabe_w);
            DA.GetDataTree("spring element", out GH_Structure<GH_Number> _spring);
            DA.GetDataTree("damper", out GH_Structure<GH_Number> _damper);
            DA.GetDataTree("rigid", out GH_Structure<GH_Number> _rigid);
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
            var XY = new string[]{"X","Y"};
            var r = _r.Branches; n = r.Count;
            var ij = _ij.Branches;
            var ijkl = _ijkl.Branches;
            var kabe_w = _kabe_w.Branches;
            var spring = _spring.Branches;
            var damper = _damper.Branches;
            var new_ij = new List<List<int>>();
            if (_ij.Branches[0][0].Value != -9999) { m = ij.Count; }
            if (_ijkl.Branches[0][0].Value != -9999) { m2 = ijkl.Count; }
            if (_kabe_w.Branches[0][0].Value != -9999) { m3 = kabe_w.Count; }
            if (_spring.Branches[0][0].Value != -9999) { m4 = spring.Count; }
            if (_damper.Branches[0][0].Value != -9999) { m5 = damper.Count; }
            var dt = time[time.Count - 1] / (double)step;
            if (start == true)
            {
                if (System.IO.Directory.Exists(foldername)) { } else { System.IO.Directory.CreateDirectory(foldername); }
                foreach (int pattern in new int[] { 0, 1 })
                {
                    var theDomain = new OpenSees.Components.DomainWrapper();
                    ///input nodes******************************************************************************
                    var nodelist = new int[n];
                    for (int i = 0; i < n; i++)
                    {
                        var x = r[i][0].Value; var y = r[i][1].Value; var z = r[i][2].Value;
                        var node = new OpenSees.Components.NodeWrapper(i + 1, 6, x, y, z);
                        theDomain.AddNode(node);
                        nodelist.SetValue(i + 1, i);
                    }
                    ///*****************************************************************************************
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
                                var local_z = new VectorWrapper(new double[] { l_vec2[0], l_vec2[1], 0.0 });
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
                        var shell_elements = new List<OpenSees.Elements.ElementWrapper>();
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
                        int ee = 1;
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
                                var ele = new OpenSees.Elements.TrussWrapper(m + mc + m2 + ee, 3, i + 1, k + 1, material, a1, 0.0, 0, 0); ee += 1; theDomain.AddElement(ele);
                                ele = new OpenSees.Elements.TrussWrapper(m + mc + m2 + ee, 3, j + 1, l + 1, material, a2, 0.0, 0, 0); ee += 1; theDomain.AddElement(ele);
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
                    rigid = _rigid.Branches;//剛床
                    var mrigid = 0;
                    if (rigid[0][0].Value != -9999)
                    {
                        var k = 0;
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
                                k += 1; theDomain.AddElement(new OpenSees.Elements.TwoNodeLinkWrapper(m + mc + m2 + m3 * 2 + k, 3, ii + 1, nodes[j], direction, springmaterial, local_y, local_x, new VectorWrapper(0), new VectorWrapper(0), 0, 0));
                            }
                            theDomain.AddSP_Constraint(new OpenSees.Components.Constraints.SP_ConstraintWrapper(ii + 1, 2, 0.0, true));
                        }
                    }
                    if (_damper.Branches[0][0].Value != -9999)
                    {
                        for (int e = 0; e < m5; e++)
                        {
                            int i = (int)damper[e][0].Value; int j = (int)damper[e][1].Value;
                            Vector3d x = new Vector3d(r[j][0].Value - r[i][0].Value, r[j][1].Value - r[i][1].Value, r[j][2].Value - r[i][2].Value);
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
                            var Kd = damper[e][2].Value; var Cd = damper[e][3].Value; var ad = damper[e][4].Value;
                            mc2 += 1; var dampermaterial = new ViscousDamperWrapper(mc + mc2 + e2, Kd, Cd, ad, 0, 1, 1e-6, 1e-10, 15);
                            mc2 += 1; var mat2 = new ElasticMaterialWrapper(mc + mc2 + e2 + mrigid, 1e-8, 0.0, 1e-8);
                            mc2 += 1; var mat3 = new ElasticMaterialWrapper(mc + mc2 + e2 + mrigid, 1e-8, 0.0, 1e-8);
                            mc2 += 1; var mat4 = new ElasticMaterialWrapper(mc + mc2 + e2 + mrigid, 1e-8, 0.0, 1e-8);
                            mc2 += 1; var mat5 = new ElasticMaterialWrapper(mc + mc2 + e2 + mrigid, 1e-8, 0.0, 1e-8);
                            mc2 += 1; var mat6 = new ElasticMaterialWrapper(mc + mc2 + e2 + mrigid, 1e-8, 0.0, 1e-8);
                            var direction = new IDWrapper(new int[] { 0, 1, 2, 3, 4, 5 });
                            var springmaterial = new UniaxialMaterialWrapper[] { dampermaterial, mat2, mat3, mat4, mat5, mat6 };
                            theDomain.AddElement(new OpenSees.Elements.TwoNodeLinkWrapper(m + mc + m2 + m3 * 2 + m4 + e + 1, 3, i + 1, j + 1, direction, springmaterial, local_y, local_x, new VectorWrapper(0), new VectorWrapper(0), 0, 0));
                        }
                    }
                    var theModel = new AnalysisModelWrapper();
                    var theSolnAlgo = new OpenSees.Algorithms.KrylovNewtonWrapper();
                    var theIntegrator = new OpenSees.Integrators.Transient.NewmarkWrapper(0.5, 0.25);
                    var theHandler = new OpenSees.Handlers.TransformationConstraintHandlerWrapper();
                    var theRCM = new OpenSees.GraphNumberers.RCMWrapper(false);
                    var theNumberer = new OpenSees.Numberers.DOF_NumbererWrapper(theRCM);
                    var theSolver = new OpenSees.Systems.Linears.BandSPDLinLapackSolverWrapper();
                    var theSOE = new OpenSees.Systems.Linears.BandSPDLinSOEWrapper(theSolver);
                    var theTest = new OpenSees.ConvergenceTests.CTestEnergyIncrWrapper(1e-8, 100, 0, 2);
                    var freq = Tlist[pattern] * (2 * Math.PI);
                    if (_f_v.Branches[0][0].Value != -9999)
                    {
                        var timevector = new VectorWrapper(time.Count); var accvector = new VectorWrapper(time.Count); var velvector = new VectorWrapper(time.Count); var dispvector = new VectorWrapper(time.Count);
                        for (int d = 0; d < time.Count; d++)
                        {
                            timevector.Set(d, time[d]); accvector.Set(d, acc[d]);
                        }
                        var f_v = _f_v.Branches; nf = f_v.Count;
                        var accwrapper = new OpenSees.Components.Timeseries.PathTimeSeriesWrapper(accvector, timevector, 1.0, true);
                        var theSeries = new OpenSees.Components.Timeseries.TrapezoidalTimeSeriesIntegratorWrapper();
                        var motion = new OpenSees.Components.GroundMotions.GroundMotionWrapper(null, null, accwrapper, null, 0.01, 1.0);
                        var theLoadPattern = new OpenSees.Components.LoadPatterns.UniformExcitationWrapper(1, motion, pattern , 0.0, 9.81);
                        theDomain.AddLoadPattern(theLoadPattern);
                        // recorder
                        var outputname = "vel";
                        var recorder1 = new OpenSees.Recorders.NodeRecorderWrapper(new IDWrapper(new int[] { 0 }), new IDWrapper(nodelist), 0, outputname, theDomain, new OpenSees.Handlers.DataFileStreamWrapper(foldername + "/" + outputname + "_" + XY[pattern] + "1.csv")); theDomain.AddRecorder(recorder1);
                        var recorder2 = new OpenSees.Recorders.NodeRecorderWrapper(new IDWrapper(new int[] { 1 }), new IDWrapper(nodelist), 0, outputname, theDomain, new OpenSees.Handlers.DataFileStreamWrapper(foldername + "/" + outputname + "_" + XY[pattern] + "2.csv")); theDomain.AddRecorder(recorder2);
                        var recorder3 = new OpenSees.Recorders.NodeRecorderWrapper(new IDWrapper(new int[] { 2 }), new IDWrapper(nodelist), 0, outputname, theDomain, new OpenSees.Handlers.DataFileStreamWrapper(foldername + "/" + outputname + "_" + XY[pattern] + "3.csv")); theDomain.AddRecorder(recorder3);
                        var recorder4 = new OpenSees.Recorders.NodeRecorderWrapper(new IDWrapper(new int[] { 3 }), new IDWrapper(nodelist), 0, outputname, theDomain, new OpenSees.Handlers.DataFileStreamWrapper(foldername + "/" + outputname + "_" + XY[pattern] + "4.csv")); theDomain.AddRecorder(recorder4);
                        var recorder5 = new OpenSees.Recorders.NodeRecorderWrapper(new IDWrapper(new int[] { 4 }), new IDWrapper(nodelist), 0, outputname, theDomain, new OpenSees.Handlers.DataFileStreamWrapper(foldername + "/" + outputname + "_" + XY[pattern] + "5.csv")); theDomain.AddRecorder(recorder5);
                        var recorder6 = new OpenSees.Recorders.NodeRecorderWrapper(new IDWrapper(new int[] { 5 }), new IDWrapper(nodelist), 0, outputname, theDomain, new OpenSees.Handlers.DataFileStreamWrapper(foldername + "/" + outputname + "_" + XY[pattern] + "6.csv")); theDomain.AddRecorder(recorder6);
                        outputname = "accel";
                        recorder1 = new OpenSees.Recorders.NodeRecorderWrapper(new IDWrapper(new int[] { 0 }), new IDWrapper(nodelist), 0, outputname, theDomain, new OpenSees.Handlers.DataFileStreamWrapper(foldername + "/" + outputname + "_" + XY[pattern] + "1.csv")); theDomain.AddRecorder(recorder1);
                        recorder2 = new OpenSees.Recorders.NodeRecorderWrapper(new IDWrapper(new int[] { 1 }), new IDWrapper(nodelist), 0, outputname, theDomain, new OpenSees.Handlers.DataFileStreamWrapper(foldername + "/" + outputname + "_" + XY[pattern] + "2.csv")); theDomain.AddRecorder(recorder2);
                        recorder3 = new OpenSees.Recorders.NodeRecorderWrapper(new IDWrapper(new int[] { 2 }), new IDWrapper(nodelist), 0, outputname, theDomain, new OpenSees.Handlers.DataFileStreamWrapper(foldername + "/" + outputname + "_" + XY[pattern] + "3.csv")); theDomain.AddRecorder(recorder3);
                        recorder4 = new OpenSees.Recorders.NodeRecorderWrapper(new IDWrapper(new int[] { 3 }), new IDWrapper(nodelist), 0, outputname, theDomain, new OpenSees.Handlers.DataFileStreamWrapper(foldername + "/" + outputname + "_" + XY[pattern] + "4.csv")); theDomain.AddRecorder(recorder4);
                        recorder5 = new OpenSees.Recorders.NodeRecorderWrapper(new IDWrapper(new int[] { 4 }), new IDWrapper(nodelist), 0, outputname, theDomain, new OpenSees.Handlers.DataFileStreamWrapper(foldername + "/" + outputname + "_" + XY[pattern] + "5.csv")); theDomain.AddRecorder(recorder5);
                        recorder6 = new OpenSees.Recorders.NodeRecorderWrapper(new IDWrapper(new int[] { 5 }), new IDWrapper(nodelist), 0, outputname, theDomain, new OpenSees.Handlers.DataFileStreamWrapper(foldername + "/" + outputname + "_" + XY[pattern] + "6.csv")); theDomain.AddRecorder(recorder6);
                        for (int e = 0; e < m5; e++)
                        {
                            var recorder7 = new OpenSees.Recorders.ElementRecorderWrapper(new IDWrapper(new int[] { m + mc + m2 + m3 + m4 + e + 1 }), new string[] { "force" }, true, theDomain, new OpenSees.Handlers.DataFileStreamWrapper(foldername + "/damperforce_" + XY[pattern] + e.ToString()+".csv"), 0, new OpenSees.IDWrapper(0)); theDomain.AddRecorder(recorder7);
                            var recorder8 = new OpenSees.Recorders.ElementRecorderWrapper(new IDWrapper(new int[] { m + mc + m2 + m3 + m4 + e + 1 }), new string[] { "deformations" }, true, theDomain, new OpenSees.Handlers.DataFileStreamWrapper(foldername + "/damperdisp_" + XY[pattern] + e.ToString() + ".csv"), 0, new OpenSees.IDWrapper(0)); theDomain.AddRecorder(recorder8);
                        }
                        if (Tlist[pattern] == 0)
                        {
                            var theDomain2 = theDomain;
                            for (int i = 0; i < nf; i++)
                            {
                                int j = (int)f_v[i][0].Value; double mi = Math.Abs(f_v[i][3].Value) / 9.81;
                                if (mi > 0)
                                {
                                    var mass = new MatrixWrapper(6, 6);
                                    if (pattern == 0) { mass[0, 0] = mi; } else { mass[0, 0] = 0.0; }
                                    if (pattern == 1) { mass[1, 1] = mi; } else { mass[1, 1] = 0.0; }
                                    mass[2, 2] = 0; mass[3, 3] = 0; mass[4, 4] = 0; mass[5, 5] = 0;
                                    theDomain2.SetMass(j + 1, mass);
                                }
                            }
                            var theAnalysis2 = new OpenSees.Analysis.DirectIntegrationAnalysisWrapper(theDomain, theHandler, theNumberer, theModel, theSolnAlgo, theSOE, theIntegrator, theTest);
                            var theEigenSOE = new OpenSees.Systems.Eigens.FullGenEigenSOEWrapper(new OpenSees.Systems.Eigens.FullGenEigenSolverWrapper(), theModel);
                            theAnalysis2.SetEigenSOE(theEigenSOE);
                            theAnalysis2.Eigen(1, true, true); var eigen = theDomain.GetEigenValues()[0];
                            freq = Math.Sqrt(eigen);
                            Tlist[pattern] = freq / 2 / Math.PI;
                        }
                        for (int i = 0; i < nf; i++)
                        {
                            int j = (int)f_v[i][0].Value; double mi = Math.Abs(f_v[i][3].Value) / 9.81;
                            if (mi > 0)
                            {
                                var mass = new MatrixWrapper(6, 6);
                                mass[0, 0] = mi; mass[1, 1] = mi; mass[2, 2] = mi; mass[3, 3] = mi; mass[4, 4] = mi; mass[5, 5] = mi;
                                theDomain.SetMass(j + 1, mass);
                            }
                        }
                    }
                    var theAnalysis = new OpenSees.Analysis.DirectIntegrationAnalysisWrapper(theDomain, theHandler, theNumberer, theModel, theSolnAlgo, theSOE, theIntegrator, theTest);
                    // set damping factor
                    if (Tlist[pattern] == 0)
                    {
                        var theEigenSOE = new OpenSees.Systems.Eigens.FullGenEigenSOEWrapper(new OpenSees.Systems.Eigens.FullGenEigenSolverWrapper(), theModel);
                        theAnalysis.SetEigenSOE(theEigenSOE);
                        theAnalysis.Eigen(1, true, true); var eigen = theDomain.GetEigenValues()[0];
                        freq = Math.Sqrt(eigen);
                        Tlist[pattern] = freq / 2 / Math.PI;
                    }
                    var dampRatio = 0.02; DA.GetData("h", ref dampRatio); var alphabeta = new List<double> { 0, 0 }; DA.GetDataList("alpha/beta", alphabeta);
                    if (alphabeta[0] <= 1e-10) { alphabeta[0] = 2 * dampRatio * freq; }
                    if (alphabeta[1] <= 1e-10) { alphabeta[1] = 2 * dampRatio / freq; }
                    if (damping == 0) { theDomain.SetRayleighDampingFactors(0, 0, alphabeta[1], 0); }
                    else if (damping == 1) { theDomain.SetRayleighDampingFactors(alphabeta[0], 0, 0, 0); }
                    else { theDomain.SetRayleighDampingFactors(alphabeta[0], 0, alphabeta[1], 0); }
                    theAnalysis.Analyze(step, dt);
                    if (pattern == 1) { DA.SetDataList("T", Tlist); }
                    var recorders = theDomain.GetRecorders(); for (int i = 0; i < recorders.Length; i++) { recorders[i].CloseOutputStreamHandler(); }
                    theDomain.ClearAll();
                    //System.Threading.Tasks.Task.Delay(1000);
                }
            }
            if (System.IO.File.Exists(foldername + "/" + "vel_X1.csv"))
            {
                var accelX1 = new List<List<double>>(); var accelX2 = new List<List<double>>(); var accelX3 = new List<List<double>>(); var accelX4 = new List<List<double>>(); var accelX5 = new List<List<double>>(); var accelX6 = new List<List<double>>();
                var accelY1 = new List<List<double>>(); var accelY2 = new List<List<double>>(); var accelY3 = new List<List<double>>(); var accelY4 = new List<List<double>>(); var accelY5 = new List<List<double>>(); var accelY6 = new List<List<double>>();
                var velX1 = new List<List<double>>(); var velX2 = new List<List<double>>(); var velX3 = new List<List<double>>(); var velX4 = new List<List<double>>(); var velX5 = new List<List<double>>(); var velX6 = new List<List<double>>();
                var velY1 = new List<List<double>>(); var velY2 = new List<List<double>>(); var velY3 = new List<List<double>>(); var velY4 = new List<List<double>>(); var velY5 = new List<List<double>>(); var velY6 = new List<List<double>>();
                var dispX1 = new List<List<double>>(); var dispX2 = new List<List<double>>(); var dispX3 = new List<List<double>>(); var dispX4 = new List<List<double>>(); var dispX5 = new List<List<double>>(); var dispX6 = new List<List<double>>();
                var dispY1 = new List<List<double>>(); var dispY2 = new List<List<double>>(); var dispY3 = new List<List<double>>(); var dispY4 = new List<List<double>>(); var dispY5 = new List<List<double>>(); var dispY6 = new List<List<double>>();
                var times = new List<double>();
                var vec0 = new List<double>();
                times.Add(0); for (int i = 0; i < _r.Branches.Count; i++) { vec0.Add(0); }
                accelX1.Add(vec0); accelX2.Add(vec0); accelX3.Add(vec0); accelX4.Add(vec0); accelX5.Add(vec0); accelX6.Add(vec0);
                accelY1.Add(vec0); accelY2.Add(vec0); accelY3.Add(vec0); accelY4.Add(vec0); accelY5.Add(vec0); accelY6.Add(vec0);
                velX1.Add(vec0); velX2.Add(vec0); velX3.Add(vec0); velX4.Add(vec0); velX5.Add(vec0); velX6.Add(vec0);
                velY1.Add(vec0); velY2.Add(vec0); velY3.Add(vec0); velY4.Add(vec0); velY5.Add(vec0); velY6.Add(vec0);
                dispX1.Add(vec0); dispX2.Add(vec0); dispX3.Add(vec0); dispX4.Add(vec0); dispX5.Add(vec0); dispX6.Add(vec0);
                dispY1.Add(vec0); dispY2.Add(vec0); dispY3.Add(vec0); dispY4.Add(vec0); dispY5.Add(vec0); dispY6.Add(vec0);
                var sr = new System.IO.StreamReader(foldername + "/" + "accel_X1.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var accel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        accel.Add(double.Parse(values[i]));
                    }
                    accelX1.Add(accel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername + "/" + "accel_X2.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var accel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        accel.Add(double.Parse(values[i]));
                    }
                    accelX2.Add(accel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername + "/" + "accel_X3.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var accel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        accel.Add(double.Parse(values[i]));
                    }
                    accelX3.Add(accel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername + "/" + "accel_X4.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var accel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        accel.Add(double.Parse(values[i]));
                    }
                    accelX4.Add(accel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername + "/" + "accel_X5.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var accel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        accel.Add(double.Parse(values[i]));
                    }
                    accelX5.Add(accel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername + "/" + "accel_X6.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var accel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        accel.Add(double.Parse(values[i]));
                    }
                    accelX6.Add(accel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername + "/" + "accel_Y1.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var accel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        accel.Add(double.Parse(values[i]));
                    }
                    accelY1.Add(accel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername + "/" + "accel_Y2.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var accel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        accel.Add(double.Parse(values[i]));
                    }
                    accelY2.Add(accel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername + "/" + "accel_Y3.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var accel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        accel.Add(double.Parse(values[i]));
                    }
                    accelY3.Add(accel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername + "/" + "accel_Y4.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var accel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        accel.Add(double.Parse(values[i]));
                    }
                    accelY4.Add(accel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername + "/" + "accel_Y5.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var accel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        accel.Add(double.Parse(values[i]));
                    }
                    accelY5.Add(accel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername + "/" + "accel_Y6.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var accel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        accel.Add(double.Parse(values[i]));
                    }
                    accelY6.Add(accel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername+"/"+"vel_X1.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var vel = new List<double>(); times.Add(double.Parse(values[0]));
                    for (int i = 1; i < values.Length; i++)
                    {
                        vel.Add(double.Parse(values[i]));
                    }
                    velX1.Add(vel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername+"/"+"vel_X2.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var vel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        vel.Add(double.Parse(values[i]));
                    }
                    velX2.Add(vel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername+"/"+"vel_X3.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var vel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        vel.Add(double.Parse(values[i]));
                    }
                    velX3.Add(vel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername+"/"+"vel_X4.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var vel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        vel.Add(double.Parse(values[i]));
                    }
                    velX4.Add(vel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername+"/"+"vel_X5.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var vel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        vel.Add(double.Parse(values[i]));
                    }
                    velX5.Add(vel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername+"/"+"vel_X6.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var vel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        vel.Add(double.Parse(values[i]));
                    }
                    velX6.Add(vel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername+"/"+"vel_Y1.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var vel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        vel.Add(double.Parse(values[i]));
                    }
                    velY1.Add(vel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername+"/"+"vel_Y2.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var vel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        vel.Add(double.Parse(values[i]));
                    }
                    velY2.Add(vel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername+"/"+"vel_Y3.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var vel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        vel.Add(double.Parse(values[i]));
                    }
                    velY3.Add(vel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername+"/"+"vel_Y4.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var vel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        vel.Add(double.Parse(values[i]));
                    }
                    velY4.Add(vel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername+"/"+"vel_Y5.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var vel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        vel.Add(double.Parse(values[i]));
                    }
                    velY5.Add(vel);
                }
                sr.Close();
                sr = new System.IO.StreamReader(foldername+"/"+"vel_Y6.csv");
                while (!sr.EndOfStream)// 末尾まで繰り返す
                {
                    string line = sr.ReadLine();// CSVファイルの一行を読み込む
                    string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                    var vel = new List<double>();
                    for (int i = 1; i < values.Length; i++)
                    {
                        vel.Add(double.Parse(values[i]));
                    }
                    velY6.Add(vel);
                }
                sr.Close();
                for (int i = 1; i < times.Count; i++)
                {
                    var dispi = new List<double>();
                    for (int j = 0; j < velX1[i].Count; j++)
                    {
                        dispi.Add((velX1[i - 1][j] + velX1[i][j]) / 2.0 * (times[i] - times[i - 1]) + dispX1[i - 1][j]);
                    }
                    dispX1.Add(dispi);
                }
                for (int i = 1; i < times.Count; i++)
                {
                    var dispi = new List<double>();
                    for (int j = 0; j < velX2[i].Count; j++)
                    {
                        dispi.Add((velX2[i - 1][j] + velX2[i][j]) / 2.0 * (times[i] - times[i - 1]) + dispX2[i - 1][j]);
                    }
                    dispX2.Add(dispi);
                }
                for (int i = 1; i < times.Count; i++)
                {
                    var dispi = new List<double>();
                    for (int j = 0; j < velX3[i].Count; j++)
                    {
                        dispi.Add((velX3[i - 1][j] + velX3[i][j]) / 2.0 * (times[i] - times[i - 1]) + dispX3[i - 1][j]);
                    }
                    dispX3.Add(dispi);
                }
                for (int i = 1; i < times.Count; i++)
                {
                    var dispi = new List<double>();
                    for (int j = 0; j < velX4[i].Count; j++)
                    {
                        dispi.Add((velX4[i - 1][j] + velX4[i][j]) / 2.0 * (times[i] - times[i - 1]) + dispX4[i - 1][j]);
                    }
                    dispX4.Add(dispi);
                }
                for (int i = 1; i < times.Count; i++)
                {
                    var dispi = new List<double>();
                    for (int j = 0; j < velX5[i].Count; j++)
                    {
                        dispi.Add((velX5[i - 1][j] + velX5[i][j]) / 2.0 * (times[i] - times[i - 1]) + dispX5[i - 1][j]);
                    }
                    dispX5.Add(dispi);
                }
                for (int i = 1; i < times.Count; i++)
                {
                    var dispi = new List<double>();
                    for (int j = 0; j < velX6[i].Count; j++)
                    {
                        dispi.Add((velX6[i - 1][j] + velX6[i][j]) / 2.0 * (times[i] - times[i - 1]) + dispX6[i - 1][j]);
                    }
                    dispX6.Add(dispi);
                }
                for (int i = 1; i < times.Count; i++)
                {
                    var dispi = new List<double>();
                    for (int j = 0; j < velY1[i].Count; j++)
                    {
                        dispi.Add((velY1[i - 1][j] + velY1[i][j]) / 2.0 * (times[i] - times[i - 1]) + dispY1[i - 1][j]);
                    }
                    dispY1.Add(dispi);
                }
                for (int i = 1; i < times.Count; i++)
                {
                    var dispi = new List<double>();
                    for (int j = 0; j < velY2[i].Count; j++)
                    {
                        dispi.Add((velY2[i - 1][j] + velY2[i][j]) / 2.0 * (times[i] - times[i - 1]) + dispY2[i - 1][j]);
                    }
                    dispY2.Add(dispi);
                }
                for (int i = 1; i < times.Count; i++)
                {
                    var dispi = new List<double>();
                    for (int j = 0; j < velY3[i].Count; j++)
                    {
                        dispi.Add((velY3[i - 1][j] + velY3[i][j]) / 2.0 * (times[i] - times[i - 1]) + dispY3[i - 1][j]);
                    }
                    dispY3.Add(dispi);
                }
                for (int i = 1; i < times.Count; i++)
                {
                    var dispi = new List<double>();
                    for (int j = 0; j < velY4[i].Count; j++)
                    {
                        dispi.Add((velY4[i - 1][j] + velY4[i][j]) / 2.0 * (times[i] - times[i - 1]) + dispY4[i - 1][j]);
                    }
                    dispY4.Add(dispi);
                }
                for (int i = 1; i < times.Count; i++)
                {
                    var dispi = new List<double>();
                    for (int j = 0; j < velY5[i].Count; j++)
                    {
                        dispi.Add((velY5[i - 1][j] + velY5[i][j]) / 2.0 * (times[i] - times[i - 1]) + dispY5[i - 1][j]);
                    }
                    dispY5.Add(dispi);
                }
                for (int i = 1; i < times.Count; i++)
                {
                    var dispi = new List<double>();
                    for (int j = 0; j < velY6[i].Count; j++)
                    {
                        dispi.Add((velY6[i - 1][j] + velY6[i][j]) / 2.0 * (times[i] - times[i - 1]) + dispY6[i - 1][j]);
                    }
                    dispY6.Add(dispi);
                }
                var dX1 = new System.IO.StreamWriter(foldername+"/"+"disp_X1.csv", false);
                for (int i = 0; i < times.Count; i++)
                {
                    var texts = times[i].ToString();
                    for (int j = 0; j < dispX1[i].Count; j++) { texts += " " + dispX1[i][j].ToString(); }
                    dX1.WriteLine(texts);
                }
                var dX2 = new System.IO.StreamWriter(foldername+"/"+"disp_X2.csv", false);
                for (int i = 0; i < times.Count; i++)
                {
                    var texts = times[i].ToString();
                    for (int j = 0; j < dispX2[i].Count; j++) { texts += " " + dispX2[i][j].ToString(); }
                    dX2.WriteLine(texts);
                }
                var dX3 = new System.IO.StreamWriter(foldername+"/"+"disp_X3.csv", false);
                for (int i = 0; i < times.Count; i++)
                {
                    var texts = times[i].ToString();
                    for (int j = 0; j < dispX3[i].Count; j++) { texts += " " + dispX3[i][j].ToString(); }
                    dX3.WriteLine(texts);
                }
                var dX4 = new System.IO.StreamWriter(foldername+"/"+"disp_X4.csv", false);
                for (int i = 0; i < times.Count; i++)
                {
                    var texts = times[i].ToString();
                    for (int j = 0; j < dispX4[i].Count; j++) { texts += " " + dispX4[i][j].ToString(); }
                    dX4.WriteLine(texts);
                }
                var dX5 = new System.IO.StreamWriter(foldername+"/"+"disp_X5.csv", false);
                for (int i = 0; i < times.Count; i++)
                {
                    var texts = times[i].ToString();
                    for (int j = 0; j < dispX5[i].Count; j++) { texts += " " + dispX5[i][j].ToString(); }
                    dX5.WriteLine(texts);
                }
                var dX6 = new System.IO.StreamWriter(foldername+"/"+"disp_X6.csv", false);
                for (int i = 0; i < times.Count; i++)
                {
                    var texts = times[i].ToString();
                    for (int j = 0; j < dispX6[i].Count; j++) { texts += " " + dispX6[i][j].ToString(); }
                    dX6.WriteLine(texts);
                }
                var dY1 = new System.IO.StreamWriter(foldername+"/"+"disp_Y1.csv", false);
                for (int i = 0; i < times.Count; i++)
                {
                    var texts = times[i].ToString();
                    for (int j = 0; j < dispY1[i].Count; j++) { texts += " " + dispY1[i][j].ToString(); }
                    dY1.WriteLine(texts);
                }
                var dY2 = new System.IO.StreamWriter(foldername+"/"+"disp_Y2.csv", false);
                for (int i = 0; i < times.Count; i++)
                {
                    var texts = times[i].ToString();
                    for (int j = 0; j < dispY2[i].Count; j++) { texts += " " + dispY2[i][j].ToString(); }
                    dY2.WriteLine(texts);
                }
                var dY3 = new System.IO.StreamWriter(foldername+"/"+"disp_Y3.csv", false);
                for (int i = 0; i < times.Count; i++)
                {
                    var texts = times[i].ToString();
                    for (int j = 0; j < dispY3[i].Count; j++) { texts += " " + dispY3[i][j].ToString(); }
                    dY3.WriteLine(texts);
                }
                var dY4 = new System.IO.StreamWriter(foldername+"/"+"disp_Y4.csv", false);
                for (int i = 0; i < times.Count; i++)
                {
                    var texts = times[i].ToString();
                    for (int j = 0; j < dispY4[i].Count; j++) { texts += " " + dispY4[i][j].ToString(); }
                    dY4.WriteLine(texts);
                }
                var dY5 = new System.IO.StreamWriter(foldername+"/"+"disp_Y5.csv", false);
                for (int i = 0; i < times.Count; i++)
                {
                    var texts = times[i].ToString();
                    for (int j = 0; j < dispY5[i].Count; j++) { texts += " " + dispY5[i][j].ToString(); }
                    dY5.WriteLine(texts);
                }
                var dY6 = new System.IO.StreamWriter(foldername+"/"+"disp_Y6.csv", false);
                for (int i = 0; i < times.Count; i++)
                {
                    var texts = times[i].ToString();
                    for (int j = 0; j < dispY6[i].Count; j++) { texts += " " + dispY6[i][j].ToString(); }
                    dY6.WriteLine(texts);
                }
                dX1.Close(); dX2.Close(); dX3.Close(); dX4.Close(); dX5.Close(); dX6.Close();
                dY1.Close(); dY2.Close(); dY3.Close(); dY4.Close(); dY5.Close(); dY6.Close();
                var accelX = new GH_Structure<GH_Number>(); var accelY = new GH_Structure<GH_Number>();
                for (int i = 0; i < step; i++)
                {
                    for (int e = 0; e < m; e++)
                    {
                        var dlistX = new List<GH_Number>(); var dlistY = new List<GH_Number>();
                        var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                        dlistX.Add(new GH_Number(accelX1[i][ni])); dlistX.Add(new GH_Number(accelX2[i][ni])); dlistX.Add(new GH_Number(accelX3[i][ni])); dlistX.Add(new GH_Number(accelX4[i][ni])); dlistX.Add(new GH_Number(accelX5[i][ni])); dlistX.Add(new GH_Number(accelX6[i][ni]));
                        dlistX.Add(new GH_Number(accelX1[i][nj])); dlistX.Add(new GH_Number(accelX2[i][nj])); dlistX.Add(new GH_Number(accelX3[i][nj])); dlistX.Add(new GH_Number(accelX4[i][nj])); dlistX.Add(new GH_Number(accelX5[i][nj])); dlistX.Add(new GH_Number(accelX6[i][nj]));
                        dlistY.Add(new GH_Number(accelY1[i][ni])); dlistY.Add(new GH_Number(accelY2[i][ni])); dlistY.Add(new GH_Number(accelY3[i][ni])); dlistY.Add(new GH_Number(accelY4[i][ni])); dlistY.Add(new GH_Number(accelY5[i][ni])); dlistY.Add(new GH_Number(accelY6[i][ni]));
                        dlistY.Add(new GH_Number(accelY1[i][nj])); dlistY.Add(new GH_Number(accelY2[i][nj])); dlistY.Add(new GH_Number(accelY3[i][nj])); dlistY.Add(new GH_Number(accelY4[i][nj])); dlistY.Add(new GH_Number(accelY5[i][nj])); dlistY.Add(new GH_Number(accelY6[i][nj]));
                        accelX.AppendRange(dlistX, new GH_Path(i, e)); accelY.AppendRange(dlistY, new GH_Path(i, e));
                    }
                }
                var velX = new GH_Structure<GH_Number>(); var velY = new GH_Structure<GH_Number>();
                for (int i = 0; i < step; i++)
                {
                    for (int e = 0; e < m; e++)
                    {
                        var dlistX = new List<GH_Number>(); var dlistY = new List<GH_Number>();
                        var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                        dlistX.Add(new GH_Number(velX1[i][ni])); dlistX.Add(new GH_Number(velX2[i][ni])); dlistX.Add(new GH_Number(velX3[i][ni])); dlistX.Add(new GH_Number(velX4[i][ni])); dlistX.Add(new GH_Number(velX5[i][ni])); dlistX.Add(new GH_Number(velX6[i][ni]));
                        dlistX.Add(new GH_Number(velX1[i][nj])); dlistX.Add(new GH_Number(velX2[i][nj])); dlistX.Add(new GH_Number(velX3[i][nj])); dlistX.Add(new GH_Number(velX4[i][nj])); dlistX.Add(new GH_Number(velX5[i][nj])); dlistX.Add(new GH_Number(velX6[i][nj]));
                        dlistY.Add(new GH_Number(velY1[i][ni])); dlistY.Add(new GH_Number(velY2[i][ni])); dlistY.Add(new GH_Number(velY3[i][ni])); dlistY.Add(new GH_Number(velY4[i][ni])); dlistY.Add(new GH_Number(velY5[i][ni])); dlistY.Add(new GH_Number(velY6[i][ni]));
                        dlistY.Add(new GH_Number(velY1[i][nj])); dlistY.Add(new GH_Number(velY2[i][nj])); dlistY.Add(new GH_Number(velY3[i][nj])); dlistY.Add(new GH_Number(velY4[i][nj])); dlistY.Add(new GH_Number(velY5[i][nj])); dlistY.Add(new GH_Number(velY6[i][nj]));
                        velX.AppendRange(dlistX, new GH_Path(i, e)); velY.AppendRange(dlistY, new GH_Path(i, e));
                    }
                }
                var dispX = new GH_Structure<GH_Number>(); var dispY = new GH_Structure<GH_Number>();
                for (int i = 0; i < step; i++)
                {
                    for (int e = 0; e < m; e++)
                    {
                        var dlistX = new List<GH_Number>(); var dlistY = new List<GH_Number>();
                        var ni = (int)ij[e][0].Value; var nj = (int)ij[e][1].Value;
                        dlistX.Add(new GH_Number(dispX1[i][ni])); dlistX.Add(new GH_Number(dispX2[i][ni])); dlistX.Add(new GH_Number(dispX3[i][ni])); dlistX.Add(new GH_Number(dispX4[i][ni])); dlistX.Add(new GH_Number(dispX5[i][ni])); dlistX.Add(new GH_Number(dispX6[i][ni]));
                        dlistX.Add(new GH_Number(dispX1[i][nj])); dlistX.Add(new GH_Number(dispX2[i][nj])); dlistX.Add(new GH_Number(dispX3[i][nj])); dlistX.Add(new GH_Number(dispX4[i][nj])); dlistX.Add(new GH_Number(dispX5[i][nj])); dlistX.Add(new GH_Number(dispX6[i][nj]));
                        dlistY.Add(new GH_Number(dispY1[i][ni])); dlistY.Add(new GH_Number(dispY2[i][ni])); dlistY.Add(new GH_Number(dispY3[i][ni])); dlistY.Add(new GH_Number(dispY4[i][ni])); dlistY.Add(new GH_Number(dispY5[i][ni])); dlistY.Add(new GH_Number(dispY6[i][ni]));
                        dlistY.Add(new GH_Number(dispY1[i][nj])); dlistY.Add(new GH_Number(dispY2[i][nj])); dlistY.Add(new GH_Number(dispY3[i][nj])); dlistY.Add(new GH_Number(dispY4[i][nj])); dlistY.Add(new GH_Number(dispY5[i][nj])); dlistY.Add(new GH_Number(dispY6[i][nj]));
                        dispX.AppendRange(dlistX, new GH_Path(i, e)); dispY.AppendRange(dlistY, new GH_Path(i, e));
                    }
                }
                var sec_fX = new GH_Structure<GH_Number>(); var sec_fY = new GH_Structure<GH_Number>();
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
                for (int i = 0; i < step; i++) {
                    for (int e = 0; e < ij.Count; e++)
                    {
                        var fx_e = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; var fy_e = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                        int ni = (int)ij[e][0].Value; int nj = (int)ij[e][1].Value; double theta = ij[e][4].Value; int mat = (int)ij[e][2].Value; int sec = (int)ij[e][3].Value;
                        var lx = r[nj][0].Value - r[ni][0].Value; var ly = r[nj][1].Value - r[ni][1].Value; var lz = r[nj][2].Value - r[ni][2].Value;
                        double l = Math.Sqrt(Math.Pow(lx, 2) + Math.Pow(ly, 2) + Math.Pow(lz, 2));
                        Matrix tr = transmatrix(l, lx, ly, lz, theta);
                        Matrix ugx = new Matrix(12, 1); Matrix ugy = new Matrix(12, 1);
                        ugx[0, 0] = dispX1[i][ni]; ugx[1, 0] = dispX2[i][ni]; ugx[2, 0] = dispX3[i][ni]; ugx[3, 0] = dispX4[i][ni]; ugx[4, 0] = dispX5[i][ni]; ugx[5, 0] = dispX6[i][ni];
                        ugx[6, 0] = dispX1[i][nj]; ugx[7, 0] = dispX2[i][nj]; ugx[8, 0] = dispX3[i][nj]; ugx[9, 0] = dispX4[i][nj]; ugx[10, 0] = dispX5[i][nj]; ugx[11, 0] = dispX6[i][nj];
                        ugy[0, 0] = dispY1[i][ni]; ugy[1, 0] = dispY2[i][ni]; ugy[2, 0] = dispY3[i][ni]; ugy[3, 0] = dispY4[i][ni]; ugy[4, 0] = dispY5[i][ni]; ugy[5, 0] = dispY6[i][ni];
                        ugy[6, 0] = dispY1[i][nj]; ugy[7, 0] = dispY2[i][nj]; ugy[8, 0] = dispY3[i][nj]; ugy[9, 0] = dispY4[i][nj]; ugy[10, 0] = dispY5[i][nj]; ugy[11, 0] = dispY6[i][nj];
                        var uex = tr * ugx; var uey = tr * ugy;
                        var fx_ei = element_sectional_force(uex, l, 0, E[mat], E[mat] / 2.0 / (1 + poi[mat]), A[sec], Iy[sec], Iz[sec], J[sec]);
                        var fx_ej = element_sectional_force(uex, l, l, E[mat], E[mat] / 2.0 / (1 + poi[mat]), A[sec], Iy[sec], Iz[sec], J[sec]);
                        var fx_ec = element_sectional_force(uex, l, l / 2.0, E[mat], E[mat] / 2.0 / (1 + poi[mat]), A[sec], Iy[sec], Iz[sec], J[sec]);
                        var fy_ei = element_sectional_force(uey, l, 0, E[mat], E[mat] / 2.0 / (1 + poi[mat]), A[sec], Iy[sec], Iz[sec], J[sec]);
                        var fy_ej = element_sectional_force(uey, l, l, E[mat], E[mat] / 2.0 / (1 + poi[mat]), A[sec], Iy[sec], Iz[sec], J[sec]);
                        var fy_ec = element_sectional_force(uey, l, l / 2.0, E[mat], E[mat] / 2.0 / (1 + poi[mat]), A[sec], Iy[sec], Iz[sec], J[sec]);
                        for (int j = 0; j < 6; j++) { fx_e[j] += fx_ei[j]; }
                        for (int j = 0; j < 6; j++) { fx_e[j + 6] -= fx_ej[j]; }
                        for (int j = 0; j < 6; j++) { fx_e[j + 12] += fx_ec[j]; }
                        for (int j = 0; j < 6; j++) { fy_e[j] += fy_ei[j]; }
                        for (int j = 0; j < 6; j++) { fy_e[j + 6] -= fy_ej[j]; }
                        for (int j = 0; j < 6; j++) { fy_e[j + 12] += fy_ec[j]; }
                        List<GH_Number> fxlist = new List<GH_Number>(); List<GH_Number> fylist = new List<GH_Number>();
                        for (int j = 0; j < 18; j++)
                        {
                            fxlist.Add(new GH_Number(fx_e[j])); fylist.Add(new GH_Number(fy_e[j]));
                        }
                        sec_fX.AppendRange(fxlist, new GH_Path(i, e)); sec_fY.AppendRange(fylist, new GH_Path(i, e));
                    }
                }
                DA.SetDataTree(1, dispX); DA.SetDataTree(2, dispY); DA.SetDataTree(3, sec_fX); DA.SetDataTree(4, sec_fY);
                var accdata = new List<double>(); int k = 0; accdata.Add(0);
                for (int i = 1; i <= step; i++)
                {
                    var t = dt * i;
                    if (time[k + 1] + 1e-8 < t) { k += 1; }
                    var t1 = time[k]; var t2 = time[k + 1];
                    var a1 = acc[k]; var a2 = acc[k + 1];
                    var accel = a1 + (t - t1) * (a2 - a1) / (t2 - t1); accdata.Add(accel);
                }
                DA.SetDataList("acc", accdata); DA.SetDataList("time", times);
                var dampingforceX = new GH_Structure<GH_Number>(); var dampingforceY = new GH_Structure<GH_Number>();
                var dampingdispX = new GH_Structure<GH_Number>(); var dampingdispY = new GH_Structure<GH_Number>();
                for (int i = 0; i < m5; i++)
                {
                    var damp_dX = new List<GH_Number>(); var damp_dY = new List<GH_Number>();
                    var damp_fX = new List<GH_Number>(); var damp_fY = new List<GH_Number>();
                    var sr1 = new System.IO.StreamReader(foldername + "/damperdisp_X" + i.ToString() + ".csv");
                    var sr2 = new System.IO.StreamReader(foldername + "/damperforce_X" + i.ToString() + ".csv");
                    var sr3 = new System.IO.StreamReader(foldername + "/damperdisp_Y" + i.ToString() + ".csv");
                    var sr4 = new System.IO.StreamReader(foldername + "/damperforce_Y" + i.ToString() + ".csv");
                    damp_dX.Add(new GH_Number(0)); damp_dY.Add(new GH_Number(0)); damp_fX.Add(new GH_Number(0)); damp_fY.Add(new GH_Number(0));
                    while (!sr1.EndOfStream)// 末尾まで繰り返す
                    {
                        string line = sr1.ReadLine();// CSVファイルの一行を読み込む
                        string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                        damp_dX.Add(new GH_Number((double.Parse(values[1]))));
                    }
                    while (!sr2.EndOfStream)// 末尾まで繰り返す
                    {
                        string line = sr2.ReadLine();// CSVファイルの一行を読み込む
                        string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                        damp_fX.Add(new GH_Number(-(double.Parse(values[1]))));
                    }
                    while (!sr3.EndOfStream)// 末尾まで繰り返す
                    {
                        string line = sr3.ReadLine();// CSVファイルの一行を読み込む
                        string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                        damp_dY.Add(new GH_Number((double.Parse(values[1]))));
                    }
                    while (!sr4.EndOfStream)// 末尾まで繰り返す
                    {
                        string line = sr4.ReadLine();// CSVファイルの一行を読み込む
                        string[] values = line.Split(' ');// 読み込んだ一行をカンマ毎に分けて配列に格納する
                        damp_fY.Add(new GH_Number(-(double.Parse(values[1]))));
                    }
                    sr1.Close(); sr2.Close(); sr3.Close(); sr4.Close();
                    dampingforceX.AppendRange(damp_fX, new GH_Path(i)); dampingforceY.AppendRange(damp_fY, new GH_Path(i));
                    dampingdispX.AppendRange(damp_dX, new GH_Path(i)); dampingdispY.AppendRange(damp_dY, new GH_Path(i));
                }
                DA.SetDataTree(7, dampingdispX); DA.SetDataTree(8, dampingdispY); DA.SetDataTree(9, dampingforceX); DA.SetDataTree(10, dampingforceY);
                DA.SetDataTree(11, velX); DA.SetDataTree(12, velY); DA.SetDataTree(13, accelX); DA.SetDataTree(14, accelY);
            }
        }
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return OpenSeesUtility.Properties.Resources.timehistoryanalysis;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("c9c71acc-0f66-4026-a122-a15da4ce3880"); }
        }
    }
}