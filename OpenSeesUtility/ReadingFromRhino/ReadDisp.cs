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
    public class ReadDisp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ReadDisp()
          : base("ReadDisp", "ReadDisp",
              "Read Point data from Rhinoceros with selected layer and export predescribed displacement information for OpenSees",
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
            pManager.AddTextParameter("name D", "name D", "[dx,dy,dz,thetax,thetay,thetaz](Datalist) boundary condition 0 or 1", GH_ParamAccess.list, new List<string> { "dx", "dy", "dz", "thetax", "thetay", "thetaz" });
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("prescribed displacement", "Disp", "[[node No.,DOF,value],...](DataTree)", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetDataTree("R", out GH_Structure<GH_Number> _r); var r = _r.Branches;
            var layer = new List<string>(); DA.GetDataList("layer", layer);
            var Dname = new List<string> { "dx", "dy", "dz", "thetax", "thetay", "thetaz" }; DA.GetDataList("name D", Dname);
            var pred = new GH_Structure<GH_Number>();
            var doc = RhinoDoc.ActiveDoc; int nd = 0;
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
                    var D = new List<int>();
                    for (int k = 0; k < 6; k++)
                    {
                        if (obj.Attributes.GetUserString(Dname[k]) != null)
                        {
                            var dlist = new List<GH_Number>();
                            dlist.Add(new GH_Number(number));
                            dlist.Add(new GH_Number(k));
                            dlist.Add(new GH_Number(float.Parse(obj.Attributes.GetUserString(Dname[k]))));
                            pred.AppendRange(dlist, new GH_Path(nd)); nd += 1;
                        }
                    }
                }
            }
            DA.SetDataTree(0, pred);
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
                return OpenSeesUtility.Properties.Resources.readdisp;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("DF8A5621-91F2-4A73-B5C5-8ACD775785FC"); }
        }
    }
}