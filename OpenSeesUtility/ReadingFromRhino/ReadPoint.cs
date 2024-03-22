using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace OpenSeesUtility
{
    public class ReadPoint : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ReadPoint()
          : base("ReadPoint", "ReadPoint",
              "Read Point data from Rhinoceros with selected layer and export boundary conditon or nodal load information for OpenSees",
              "OpenSees", "Reading from Rhino")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("R", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddTextParameter("layer", "layer", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("name B", "name B", "[fx,fy,fz,rx,ry,rz](Datalist) boundary condition 0 or 1", GH_ParamAccess.list, new List<string> { "fx", "fy", "fz", "rx", "ry", "rz" });
            pManager.AddTextParameter("name P", "name P", "[Px,Py,Pz,Mx,My,Mz](Datalist) nodal load", GH_ParamAccess.list, new List<string> { "px", "py", "pz", "mx", "my", "mz" });
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("boundary_condition", "Bounds", "[[node No.,X,Y,Z,MX,MY,MZ],...](DataTree) 0-1 variable", GH_ParamAccess.tree);///
            pManager.AddNumberParameter("nodal load vector", "p_load", "[[node No.,Px,Py,Pz,Mx,My,Mz],...](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetDataTree("R", out GH_Structure<GH_Number> _r); var r = _r.Branches;
            var layer = new List<string>(); DA.GetDataList("layer", layer);
            var Bname = new List<string> { "fx", "fy", "fz", "rx", "ry", "rz" }; DA.GetDataList("name B", Bname);
            var Pname = new List<string> { "px", "py", "pz", "mx", "my", "mz" }; DA.GetDataList("name P", Pname);
            var bounds = new GH_Structure<GH_Number>(); var p_load = new GH_Structure<GH_Number>();
            var doc = RhinoDoc.ActiveDoc; int nb = 0; int np = 0;
            for (int i = 0; i < layer.Count; i++)
            {
                var point = doc.Objects.FindByLayer(layer[i]);
                for (int j = 0; j < point.Length; j++)
                {
                    var obj = point[j]; var p = (new ObjRef(obj)).Point().Location; int number = 0;
                    for (int k = 0; k < r.Count; k++)
                    {
                        if (Math.Abs(p[0] - r[k][0].Value) < 5e-3 && Math.Abs(p[1] - r[k][1].Value) < 5e-3 && Math.Abs(p[2] - r[k][2].Value) < 5e-3)
                        {
                            number = k;
                            break;
                        }
                    }
                    var B = new List<int>(); var P = new List<double>();
                    for (int k = 0; k < 6; k++)
                    {
                        if (obj.Attributes.GetUserString(Bname[k]) == null) { B.Add(0); }
                        else { B.Add(int.Parse(obj.Attributes.GetUserString(Bname[k]))); }
                        if (obj.Attributes.GetUserString(Pname[k]) == null) { P.Add(0); }
                        else { P.Add(float.Parse(obj.Attributes.GetUserString(Pname[k]))); }
                    }
                    if (B[0] != 0 || B[1] != 0 || B[2] != 0 || B[3] != 0 || B[4] != 0 || B[5] != 0)
                    {
                        var blist = new List<GH_Number>();
                        blist.Add(new GH_Number(number)); for (int k = 0; k < 6; k++) { blist.Add(new GH_Number(B[k])); }
                        bounds.AppendRange(blist, new GH_Path(nb)); nb += 1;
                    }
                    if (P[0] != 0 || P[1] != 0 || P[2] != 0 || P[3] != 0 || P[4] != 0 || P[5] != 0)
                    {
                        var plist = new List<GH_Number>();
                        plist.Add(new GH_Number(number)); for (int k = 0; k < 6; k++) { plist.Add(new GH_Number(P[k])); }
                        p_load.AppendRange(plist, new GH_Path(np)); np += 1;
                    }
                }
            }
            DA.SetDataTree(0, bounds); DA.SetDataTree(1, p_load);
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
                return OpenSeesUtility.Properties.Resources.readpoint;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("ad06a327-02fd-438a-aeb4-4d798270e894"); }
        }
    }
}