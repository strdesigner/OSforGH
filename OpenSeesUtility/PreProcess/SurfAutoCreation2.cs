using System;
using System.IO;
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
using Rhino.DocObjects;
using Rhino;
///****************************************

namespace OpenSeesUtility
{
    public class SurfAutoCreation2 : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public SurfAutoCreation2()
          : base("SurfAutoCreation2", "SurfAutoCreation2",
              "select multi areas and create surfaces which are divided by beams",
              "OpenSees", "PreProcess")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("P", "P", "floor surface edge points(DataTree)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("R", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("IJ", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("Sz", "Sz", "vertical pressure value [kN/m2]", GH_ParamAccess.list);
            pManager.AddIntegerParameter("mat", "mat", "material number", GH_ParamAccess.list);
            pManager.AddNumberParameter("thick", "thick", "thickness of the surfaces", GH_ParamAccess.list);
            pManager.AddNumberParameter("bairitsu", "bairitsu", "kabebairitsu", GH_ParamAccess.list);
            pManager.AddNumberParameter("rad", "rad", "in general, 120 angle", GH_ParamAccess.list);
            pManager[2].Optional = true; pManager[3].Optional = true; pManager[4].Optional = true; pManager[5].Optional = true; pManager[6].Optional = true; pManager[7].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("S", "S", "selected surface", GH_ParamAccess.list);
            pManager.AddCurveParameter("lines", "lines", "Line of elements", GH_ParamAccess.list);
            pManager.AddNumberParameter("surface or floor load vector", "sfload", "[[I,J,K,L,Wz[kN/m2]],...](DataTree)", GH_ParamAccess.tree);
            pManager.AddNumberParameter("element_node_relationship(shell)", "IJKL", "[[No.i,No.j,No.k,No.l(if triangle:-1),material No.,thickness],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("KABE_W", "KABE_W", "[[No.i,No.j,No.k,No.l,bairitsu,rad],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddBrepParameter("Sall", "Sall", "whole surface", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetDataTree("P", out GH_Structure<GH_Point> _P); var P = _P.Branches;
            //DA.GetDataList("P", pts); pts.Add(pts[0]);
            DA.GetDataTree("R", out GH_Structure<GH_Number> _R); var R = _R.Branches;
            DA.GetDataTree("IJ", out GH_Structure<GH_Number> _IJ); var IJ = _IJ.Branches;
            var sz = new List<double>(); if (!DA.GetDataList("Sz", sz)) { }; var s_load = new GH_Structure<GH_Number>();
            var m = new List<int>(); if (!DA.GetDataList("mat", m)) { }; var IJKL = new GH_Structure<GH_Number>();
            var t = new List<double>(); if (!DA.GetDataList("thick", t)) { };
            var b = new List<double>(); if (!DA.GetDataList("bairitsu", b)) { }; var kabe_w = new GH_Structure<GH_Number>();
            var r = new List<double>(); if (!DA.GetDataList("rad", r)) { };
            int ee = 0; int e2 = 0;
            for (int iii = 0; iii < P.Count; iii++)
            {
                var pts = new List<Point3d>();
                for (int i = 0; i < P[iii].Count; i++)
                {
                    pts.Add(P[iii][i].Value);
                }
                if (pts.Count != 0)
                {
                    pts.Add(pts[0]);
                    var ijkl = new List<List<int>>();
                    var lines_inside_surface = new List<Curve>(); var ij = new List<List<int>>();
                    var brep = Brep.CreatePlanarBreps(new Polyline(pts).ToNurbsCurve(), 0.001)[0];
                    var bb = brep.GetBoundingBox(true); var cc = bb.GetCorners();
                    var xmin = 9999.0; var xmax = -9999.0; var ymin = 9999.0; var ymax = -9999.0; var zmin = 9999.0; var zmax = -9999.0;
                    for (int i = 0; i < cc.Length; i++)
                    {
                        xmin = Math.Min(xmin, cc[i][0]); xmax = Math.Max(xmax, cc[i][0]);
                        ymin = Math.Min(ymin, cc[i][1]); ymax = Math.Max(ymax, cc[i][1]);
                        zmin = Math.Min(zmin, cc[i][2]); zmax = Math.Max(zmax, cc[i][2]);
                    }
                    for (int e = 0; e < IJ.Count; e++)
                    {
                        int ni = (int)IJ[e][0].Value; int nj = (int)IJ[e][1].Value;
                        var ri = new Point3d(R[ni][0].Value, R[ni][1].Value, R[ni][2].Value);
                        var rj = new Point3d(R[nj][0].Value, R[nj][1].Value, R[nj][2].Value);
                        var line = new Line(ri, rj).ToNurbsCurve();
                        if (xmin <= ri[0] + 5e-3 && ri[0] - 5e-3 <= xmax && ymin <= ri[1] + 5e-3 && ri[1] - 5e-3 <= ymax && zmin <= ri[2] + 5e-3 && ri[2] - 5e-3 <= zmax && xmin <= rj[0] + 5e-3 && rj[0] - 5e-3 <= xmax && ymin <= rj[1] + 5e-3 && rj[1] - 5e-3 <= ymax && zmin <= rj[2] + 5e-3 && rj[2] - 5e-3 <= zmax)
                        {
                            lines_inside_surface.Add(line);
                            ij.Add(new List<int> { ni, nj });
                        }
                    }
                    IEnumerable<Curve> curves = lines_inside_surface;
                    var breps = brep.Split(curves, 0.005);
                    DA.SetDataList("S", breps);
                    DA.SetDataList("lines", lines_inside_surface);
                    for (int e = 0; e < breps.Length; e++)
                    {
                        var l = breps[e].Edges;//brepのエッジライン(頂点を含まない線分も含まれている)
                        var edges = new List<Line>();//ここには頂点のみを結ぶエッジラインを格納する
                        for (int i = 0; i < l.Count; i++)
                        {
                            var v0 = new Vector3d(l[i].PointAtEnd - l[i].PointAtStart);
                            var k = 0;
                            for (int j = 0; j < edges.Count; j++)
                            {
                                var v1 = new Vector3d(edges[j].To - edges[j].From); var v2 = new Vector3d(edges[j].To - l[i].PointAtStart);
                                if (v2.Length < 5e-3) { v2 = new Vector3d(edges[j].From - l[i].PointAtStart); }
                                var p1 = l[i].PointAtStart; var p2 = l[i].PointAtEnd;//エッジラインの端部座標
                                if ((Math.Abs(Vector3d.VectorAngle(v0, v1)) <= 5e-3 || Math.Abs(Math.Abs(Vector3d.VectorAngle(v0, v1)) - Math.PI) <= 5e-3) && (Math.Abs(Vector3d.VectorAngle(v0, v2)) <= 5e-3 || Math.Abs(Math.Abs(Vector3d.VectorAngle(v0, v2)) - Math.PI) <= 5e-3) && (Math.Abs(Vector3d.VectorAngle(v1, v2)) <= 5e-3 || Math.Abs(Math.Abs(Vector3d.VectorAngle(v1, v2)) - Math.PI) <= 5e-3))//次のエッジラインが既に格納済のエッジライン群と同一直線上に存在するかどうか
                                {
                                    var p3 = edges[j].From; var p4 = edges[j].To;
                                    var l12 = (p2 - p1).Length; var l13 = (p3 - p1).Length; var l14 = (p4 - p1).Length; var l23 = (p3 - p2).Length; var l24 = (p4 - p2).Length; var l34 = (p4 - p3).Length;
                                    var lmax = Math.Max(Math.Max(Math.Max(l12, l13), Math.Max(l14, l23)), Math.Max(l24, l34));
                                    if (l12 == lmax) { edges[j] = new Line(p1, p2); }//既に同一直線上に存在するエッジラインを格納済の場合,それらを統合して1つのエッジラインを作る
                                    else if (l13 == lmax) { edges[j] = new Line(p1, p3); }
                                    else if (l14 == lmax) { edges[j] = new Line(p1, p4); }
                                    else if (l23 == lmax) { edges[j] = new Line(p2, p3); }
                                    else if (l24 == lmax) { edges[j] = new Line(p2, p4); }
                                    else if (l34 == lmax) { edges[j] = new Line(p3, p4); }
                                    k = 1;
                                    break;
                                }
                            }
                            if (k == 0) { edges.Add(new Line(l[i].PointAtStart, l[i].PointAtEnd)); }//同一直線上に存在しなければ新たに格納する
                        }
                        var r1 = edges[0].From; var r2 = edges[0].To; var r3 = new Point3d(-9999, -9999, -9999); var r4 = new Point3d(-9999, -9999, -9999);
                        for (int i = 1; i < edges.Count; i++)
                        {
                            var p1 = edges[i].From; var p2 = edges[i].To;
                            if (r2 == p1) { r3 = p2; }
                            if (r2 == p2) { r3 = p1; }
                        }
                        if (edges.Count == 4)//4角形要素の場合
                        {
                            for (int i = 1; i < edges.Count; i++)
                            {
                                var p1 = edges[i].From; var p2 = edges[i].To;
                                if (r3 == p1 && r2 != p2) { r4 = p2; }
                                if (r3 == p2 && r2 != p1) { r4 = p1; }
                            }
                        }
                        //var s = breps[e].Vertices;
                        //var s2 = breps[e].GetBoundingBox(true).GetCorners();
                        //var r1 = new Point3d(-9999, -9999, -9999); var r2 = new Point3d(-9999, -9999, -9999); var r3 = new Point3d(-9999, -9999, -9999); var r4 = new Point3d(-9999, -9999, -9999);
                        var n1 = 0; var n2 = 0; var n3 = 0; var n4 = -1;
                        for (int i = 0; i < lines_inside_surface.Count; i++)
                        {
                            var ri = lines_inside_surface[i].PointAtStart;
                            var rj = lines_inside_surface[i].PointAtEnd;
                            if ((r1 - ri).Length <= 5e-3) { n1 = ij[i][0]; }
                            if ((r1 - rj).Length <= 5e-3) { n1 = ij[i][1]; }
                            if ((r2 - ri).Length <= 5e-3) { n2 = ij[i][0]; }
                            if ((r2 - rj).Length <= 5e-3) { n2 = ij[i][1]; }
                            if ((r3 - ri).Length <= 5e-3) { n3 = ij[i][0]; }
                            if ((r3 - rj).Length <= 5e-3) { n3 = ij[i][1]; }
                            if ((r4 - ri).Length <= 5e-3) { n4 = ij[i][0]; }
                            if ((r4 - rj).Length <= 5e-3) { n4 = ij[i][1]; }
                        }
                        ijkl.Add(new List<int> { n1, n2, n3, n4 });
                        //RhinoApp.WriteLine(r1[0].ToString()+" ,"+r1[1].ToString() + " ," + r1[2].ToString());
                    }
                    for (int e = 0; e < breps.Length; e++)
                    {
                        if (sz.Count == 1)
                        {
                            s_load.AppendRange(new List<GH_Number> { new GH_Number(ijkl[e][0]), new GH_Number(ijkl[e][1]), new GH_Number(ijkl[e][2]), new GH_Number(ijkl[e][3]), new GH_Number(sz[0]) }, new GH_Path(e2));
                        }
                        else if (sz.Count != 0) { s_load.AppendRange(new List<GH_Number> { new GH_Number(ijkl[e][0]), new GH_Number(ijkl[e][1]), new GH_Number(ijkl[e][2]), new GH_Number(ijkl[e][3]), new GH_Number(sz[iii]) }, new GH_Path(e2)); }
                        var slist = new List<GH_Number>(); var klist = new List<GH_Number>();
                        slist.Add(new GH_Number(ijkl[e][0])); slist.Add(new GH_Number(ijkl[e][1])); slist.Add(new GH_Number(ijkl[e][2])); slist.Add(new GH_Number(ijkl[e][3]));
                        if (m.Count == 1) { slist.Add(new GH_Number(m[0])); }
                        else if (m.Count != 0) { slist.Add(new GH_Number(m[iii])); }
                        else { slist.Add(new GH_Number(0)); }
                        if (t.Count == 1) { slist.Add(new GH_Number(t[0])); }
                        else if (t.Count != 0) { slist.Add(new GH_Number(t[iii])); }
                        else { slist.Add(new GH_Number(0.2)); }
                        IJKL.AppendRange(slist, new GH_Path(e2));
                        if (ijkl[e][3] != -1)
                        {
                            klist.Add(new GH_Number(ijkl[e][0])); klist.Add(new GH_Number(ijkl[e][1])); klist.Add(new GH_Number(ijkl[e][2])); klist.Add(new GH_Number(ijkl[e][3]));
                            if (b.Count == 1) { klist.Add(new GH_Number(b[0])); }
                            else if (b.Count != 0) { klist.Add(new GH_Number(b[iii])); }
                            else { klist.Add(new GH_Number(2.5)); }
                            if (r.Count == 1) { klist.Add(new GH_Number(r[0])); }
                            else if (r.Count != 0) { klist.Add(new GH_Number(r[iii])); }
                            else { klist.Add(new GH_Number(120)); }
                            kabe_w.AppendRange(klist, new GH_Path(ee));
                            ee += 1;
                        }
                        e2 += 1;
                    }
                }
            }
            DA.SetDataTree(2, s_load);
            DA.SetDataTree(3, IJKL);
            DA.SetDataTree(4, kabe_w);
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
                return OpenSeesUtility.Properties.Resources.surfautocreation2;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("1caf1775-a3c4-4e9a-acbf-26ae8991449e"); }
        }
    }
}