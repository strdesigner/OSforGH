using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace AssembleLines
{
    public class AssembleLines : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public AssembleLines()
          : base("Make R and IJ from Geometry", "AssembleLines",
              "AssembleGeometries(Lines)",
              "OpenSees", "Utility")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddLineParameter("lines", "BEAM", "Line of elements", GH_ParamAccess.list);
            pManager.AddNumberParameter("Material No. List", "mat", "[...](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddNumberParameter("Section No. List", "sec", "[...](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///
            pManager.AddNumberParameter("Element Angle List", "angle", "[...](DataList)", GH_ParamAccess.list, new List<double> { -9999 });///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("element_node_relationship", "IJ", "[[No.i,No.j,material No.,section No.,angle],...](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Line> lines = new List<Line>();
            DA.GetDataList("lines", lines);
            List<double> mat = new List<double>(); DA.GetDataList("Material No. List", mat);
            List<double> sec = new List<double>(); DA.GetDataList("Section No. List", sec);
            List<double> angle = new List<double>(); DA.GetDataList("Element Angle List", angle);
            var r = new GH_Structure<GH_Number>(); var ij = new GH_Structure<GH_Number>();
            List<Point3d> xyz = new List<Point3d>();
            for (int e = 0; e < lines.Count; e++)
            {
                var r1 = lines[e].From; var r2 = lines[e].To; var l1 = 10.0; var l2 = 10.0;
                for (int i = 0; i < xyz.Count; i++) { l1 = Math.Min(l1, (xyz[i] - r1).Length); }
                if (l1 > 5e-3) { xyz.Add(r1); }
                for (int i = 0; i < xyz.Count; i++) { l2 = Math.Min(l2, (xyz[i] - r2).Length); }
                if (l2 > 5e-3) { xyz.Add(r2); }
                //if (!xyz.Contains(r1)) { xyz.Add(r1); }
                //if (!xyz.Contains(r2)) { xyz.Add(r2); }
            }
            for (int i = 0; i < xyz.Count; i++)
            {
                List<GH_Number> rlist = new List<GH_Number>();
                rlist.Add(new GH_Number(xyz[i].X)); rlist.Add(new GH_Number(xyz[i].Y)); rlist.Add(new GH_Number(xyz[i].Z));
                r.AppendRange(rlist, new GH_Path(i));
            }
            for (int e = 0; e < lines.Count; e++)
            {
                var r1 = lines[e].From; var r2 = lines[e].To; int i = 0; int j = 0;
                for (i = 0; i < xyz.Count; i++) { if ((xyz[i] - r1).Length <= 5e-3) { break; }; }
                for (j = 0; j < xyz.Count; j++) { if ((xyz[j] - r2).Length <= 5e-3) { break; }; }
                //int i = xyz.IndexOf(r1); int j = xyz.IndexOf(r2);
                List<GH_Number> ijlist = new List<GH_Number>();
                ijlist.Add(new GH_Number(i)); ijlist.Add(new GH_Number(j));
                if (mat[0] == -9999){ ijlist.Add(new GH_Number(0)); }
                else { ijlist.Add(new GH_Number(mat[e])); }
                if (sec[0] == -9999) { ijlist.Add(new GH_Number(0)); }
                else { ijlist.Add(new GH_Number(sec[e])); }
                if (angle[0] == -9999) { ijlist.Add(new GH_Number(0)); }
                else { ijlist.Add(new GH_Number(angle[e])); }
                ij.AppendRange(ijlist, new GH_Path(e));
            }
            DA.SetDataTree(0, r);
            DA.SetDataTree(1, ij);
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
                return OpenSeesUtility.Properties.Resources.assemblelines;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("3e70b4a0-856e-43d5-8a7c-d821c535e1c4"); }
        }
    }
}