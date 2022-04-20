using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace MakeIndex2
{
    public class MakeIndex2 : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public MakeIndex2()
          : base("MakeIndex2", "MakeIndex2",
              "Read non-divided line data from Rhinoceros with all selected layers and export indexes of specified layer",
              "OpenSees", "Reading from Rhino")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("layer(beamall)", "layer(beamall)", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("layer(springall)", "layer(springall)", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("layer", "layer", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("name wick", "name wick", "usertextname for wick", GH_ParamAccess.list, new List<string> { "wickX", "wickY", "wickZ" });
            pManager.AddTextParameter("wick", "wick", "[wickname1,wickname2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddNumberParameter("accuracy", "accuracy", "oversection accuracy", GH_ParamAccess.item, 5e-3);
            pManager[0].Optional = true; pManager[1].Optional = true; pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("index", "index", "[int,int,...](Datalist)", GH_ParamAccess.list);
            pManager.AddCurveParameter("line", "line", "Line of elements", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> layers = new List<string>(); if (!DA.GetDataList("layer(beamall)", layers)) { };
            List<string> layers2 = new List<string>(); if (!DA.GetDataList("layer(springall)", layers2)) { };
            List<string> layer = new List<string>(); if (!DA.GetDataList("layer", layer)) { };
            List<string> wick = new List<string>(); if (!DA.GetDataList("wick", wick)) { };
            var lines = new List<Curve>(); var lines_new = new List<Line>(); var lines2 = new List<Curve>();
            var name_x = "wickX"; var name_y = "wickY"; var name_z = "wickZ"; var name_w = "wickW"; var name_xyz = new List<string> { "wickX", "wickY", "wickZ", "wickW" }; DA.GetDataList("name wick", name_xyz); name_x = name_xyz[0]; name_y = name_xyz[1]; name_z = name_xyz[2]; name_w = name_xyz[3]; var acc = 5e-3; DA.GetData("accuracy", ref acc);
            var doc = RhinoDoc.ActiveDoc; var index = new List<int>(); var index_new = new List<int>(); var index2 = new List<int>();
            if (layer.Count == 0 && layers.Count != 0) { layer = layers; }
            int k = 0;
            for (int i = 0; i < layers.Count; i++)
            {
                var line = doc.Objects.FindByLayer(layers[i]);
                for (int j = 0; j < line.Length; j++)
                {
                    var obj = line[j]; Curve[] l = new Curve[] { (new ObjRef(obj)).Curve() };
                    int nl = (new ObjRef(obj)).Curve().SpanCount;//ポリラインのセグメント数
                    if (nl > 1) { l = (new ObjRef(obj)).Curve().DuplicateSegments(); }
                    for (int jj = 0; jj < nl; jj++)
                    {
                        if (layer.Contains(layers[i]))
                        {
                            if (wick.Count == 0 || wick == new List<string>())
                            {
                                index.Add(k);
                            }
                            else
                            {
                                string text1 = obj.Attributes.GetUserString(name_x); string text2 = obj.Attributes.GetUserString(name_y); string text3 = obj.Attributes.GetUserString(name_z); string text4 = obj.Attributes.GetUserString(name_w);//軸ラベル
                                if (wick.Contains(text1) == true || wick.Contains(text2) == true || wick.Contains(text3) == true || wick.Contains(text4) == true)
                                {
                                    index.Add(k);//指定軸が含まれていればindexを格納
                                }
                            }
                        }
                        lines.Add(l[jj]);
                        k += 1;
                    }
                }
            }
            if (layer.Count == 0 && layers2.Count != 0) { layer = layers2; }
            k = 0;
            for (int i = 0; i < layers2.Count; i++)
            {
                var line = doc.Objects.FindByLayer(layers2[i]);
                for (int j = 0; j < line.Length; j++)
                {
                    var obj = line[j];
                    if (layer.Contains(layers2[i]))
                    {
                        if (wick.Count == 0 || wick == new List<string>())
                        {
                            index2.Add(k);
                        }
                        else
                        {
                            string text1 = obj.Attributes.GetUserString(name_x); string text2 = obj.Attributes.GetUserString(name_y); string text3 = obj.Attributes.GetUserString(name_z); string text4 = obj.Attributes.GetUserString(name_w);//軸ラベル
                            if (wick.Contains(text1) == true || wick.Contains(text2) == true || wick.Contains(text3) == true || wick.Contains(text4) == true)
                            {
                                index2.Add(k);//指定軸が含まれていればindexを格納
                            }
                        }
                    }
                    var l = (new ObjRef(obj)).Curve(); lines2.Add(l);
                    k += 1;
                }
            }
            if (lines.Count != 0)
            {
                var xyz = new List<Point3d>();
                for (int e = 0; e < lines.Count; e++)//節点生成
                {
                    var r1 = lines[e].PointAtStart; var r2 = lines[e].PointAtEnd; var l1 = 10.0; var l2 = 10.0;
                    for (int i = 0; i < xyz.Count; i++) { l1 = Math.Min(l1, (xyz[i] - r1).Length); }
                    if (l1 > acc) { xyz.Add(r1); }
                    for (int i = 0; i < xyz.Count; i++) { l2 = Math.Min(l2, (xyz[i] - r2).Length); }
                    if (l2 > acc) { xyz.Add(r2); }
                    for (int e2 = 0; e2 < lines.Count; e2++)//中間交差点も考慮
                    {
                        if (e2 != e)
                        {
                            var cp = Rhino.Geometry.Intersect.Intersection.CurveCurve(lines[e], lines[e2], acc, acc);
                            if (cp != null && cp.Count != 0)
                            {
                                var rc = cp[0].PointA;
                                l1 = 10.0;
                                for (int i = 0; i < xyz.Count; i++) { l1 = Math.Min(l1, (xyz[i] - rc).Length); }
                                if (l1 > acc) { xyz.Add(rc); }
                            }
                        }
                    }
                }
                for (int e = 0; e < lines2.Count; e++)//節点生成(spring)
                {
                    var r1 = lines2[e].PointAtStart; var r2 = lines2[e].PointAtEnd; var l1 = 10.0; var l2 = 10.0;
                    for (int i = 0; i < xyz.Count; i++) { l1 = Math.Min(l1, (xyz[i] - r1).Length); }
                    if (l1 > acc) { xyz.Add(r1); }
                    for (int i = 0; i < xyz.Count; i++) { l2 = Math.Min(l2, (xyz[i] - r2).Length); }
                    if (l2 > acc) { xyz.Add(r2); }
                }
                k = -1;
                for (int e = 0; e < lines.Count; e++)//交差判定を行い交差部で要素分割する
                {
                    var r1 = lines[e].PointAtStart; var r2 = lines[e].PointAtEnd; var l0 = r2 - r1; var rc = new List<Point3d>(); 
                    for (int i = 0; i < xyz.Count; i++)
                    {
                        var l1 = xyz[i] - r1;
                        if (l1.Length > acc && (r2 - xyz[i]).Length > acc)//線分上に節点がいるかどうかチェック
                        {
                            if ((l0 / l0.Length - l1 / l1.Length).Length < 1e-5 && l0.Length - l1.Length > acc)
                            {
                                rc.Add(xyz[i]);
                            }
                        }
                    }
                    if (rc.Count != 0)
                    {
                        var llist = new List<double>();
                        for (int i = 0; i < rc.Count; i++)
                        {
                            llist.Add((rc[i] - r1).Length);
                        }
                        int[] idx = Enumerable.Range(0, rc.Count).ToArray<int>();//r1とr2の間の点のソート
                        Array.Sort<int>(idx, (a, b) => llist[a].CompareTo(llist[b]));
                        k += 1;
                        if (index.Contains(e)) { index_new.Add(k); lines_new.Add(new Line(r1, rc[idx[0]])); }
                        for (int i = 0; i < idx.Length - 1; i++)
                        {
                            k += 1; if (index.Contains(e)) { index_new.Add(k); lines_new.Add(new Line(rc[idx[i]], rc[idx[i + 1]])); }
                        }
                        k += 1; if (index.Contains(e)) { index_new.Add(k); lines_new.Add(new Line(rc[idx[idx.Length - 1]], r2)); }
                    }
                    else
                    {
                        k += 1; if (index.Contains(e)) { index_new.Add(k); lines_new.Add(new Line(r1, r2)); }
                    }
                }
                DA.SetDataList("index", index_new); DA.SetDataList("line", lines_new);
            }
            if(index_new.Count == 0 && index2.Count != 0){ DA.SetDataList("index", index2); }
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
                return OpenSeesUtility.Properties.Resources.index2;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("47862932-7f86-44c9-b16f-bdc5dd1e2df5"); }
        }
    }
}