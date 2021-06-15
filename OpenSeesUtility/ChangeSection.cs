using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace ChangeSection
{
    public class ChangeSection : GH_Component
    {
        public ChangeSection()
          : base("Change Section Size", "ChangeSection",
              "Change the width and height of the cross-section for burning design, etc.",
              "OpenSees", "PreProcess")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("layer(all)", "layer(all)", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("layer", "layer", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("name -B", "name -B", "userstring for width subtracted from the original cross-section", GH_ParamAccess.item, "burnB");
            pManager.AddTextParameter("name -D", "name -D", "userstring for height subtracted from the original cross-section", GH_ParamAccess.item, "burnD");
            pManager.AddNumberParameter("accuracy", "accuracy", "oversection accuracy", GH_ParamAccess.item, 5e-3);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("subtracted width", "burn -B", "[double,double,...](Datalist)[m]", GH_ParamAccess.list);
            pManager.AddNumberParameter("subtracted height", "burn -D", "[double,double,...](Datalist)[m]", GH_ParamAccess.list);
            pManager.AddIntegerParameter("index(burn)", "index(burn)", "[int,int,...](Datalist)", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> layers = new List<string>(); if (!DA.GetDataList("layer(all)", layers)) { };
            List<string> layer = new List<string>(); if (!DA.GetDataList("layer", layer)) { };
            var name_B = "-width"; DA.GetData("name -B", ref name_B); var name_D = "-height"; DA.GetData("name -D", ref name_D); var acc = 5e-3; DA.GetData("accuracy", ref acc);
            var burnwidth = new List<double>(); var burnheight = new List<double>();
            List<int> index = new List<int>(); List<Curve> lines = new List<Curve>();
            var index_new = new List<int>(); List<Curve> lines2 = new List<Curve>(); var lines_new = new List<Line>();
            var burnwidth_new = new List<double>(); var burnheight_new = new List<double>();
            var doc = RhinoDoc.ActiveDoc;
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
                            index.Add(k);//
                        }
                        lines.Add(l[jj]);
                        var B = 0.0; var D = 0.0;
                        var text = obj.Attributes.GetUserString(name_B);//燃えしろ幅
                        if (text == null) { B = 0.0; }
                        else { B = double.Parse(text); }
                        text = obj.Attributes.GetUserString(name_D);//燃えしろせい
                        if (text == null) { D = 0.0; }
                        else { D = double.Parse(text); }
                        burnwidth.Add(B); burnheight.Add(D);
                        k += 1;
                    }
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
                        lines_new.Add(new Line(r1, rc[idx[0]])); k += 1;
                        if (index.Contains(e))
                        {
                            index_new.Add(k);
                            burnwidth_new.Add(burnwidth[e]);
                            burnheight_new.Add(burnheight[e]);
                        }
                        for (int i = 0; i < idx.Length - 1; i++)
                        {
                            k += 1;
                            if (index.Contains(e))
                            {
                                index_new.Add(k);
                                lines_new.Add(new Line(rc[idx[i]], rc[idx[i + 1]]));
                                burnwidth_new.Add(burnwidth[e]);
                                burnheight_new.Add(burnheight[e]);
                            }
                        }
                        k += 1;
                        if (index.Contains(e))
                        {
                            index_new.Add(k);
                            lines_new.Add(new Line(rc[idx[idx.Length - 1]], r2));
                            burnwidth_new.Add(burnwidth[e]);
                            burnheight_new.Add(burnheight[e]);
                        }
                    }
                    else
                    {
                        k += 1;
                        if (index.Contains(e))
                        {
                            index_new.Add(k);
                            lines_new.Add(new Line(r1, r2));
                            burnwidth_new.Add(burnwidth[e]);
                            burnheight_new.Add(burnheight[e]);
                        }
                    }
                }
            }
            DA.SetDataList("subtracted width", burnwidth_new);
            DA.SetDataList("subtracted height", burnheight_new);
            DA.SetDataList("index(burn)", index_new);

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
                return OpenSeesUtility.Properties.Resources.burn;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("4e49da2a-330e-4b83-9b4b-cee369151b9b"); }
        }
    }
}