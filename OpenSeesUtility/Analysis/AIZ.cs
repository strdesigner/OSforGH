using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace OpenSeesUtility
{
    public class AIZ : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public AIZ()
          : base("Calc section performance of an arbitrary section", "AIZ",
              "Calc section performance of an arbitrary section loaded from the Rhino canvas.",
              "OpenSees", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("layer", "layer", "layername", GH_ParamAccess.item,"");
            pManager.AddCurveParameter("curves", "curves", "If a layer is specified, this input will be ignored.", GH_ParamAccess.list);
            pManager[1].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("S", "S", "created surface from closed curve", GH_ParamAccess.item);
            pManager.AddPointParameter("C", "C", "center of gravity", GH_ParamAccess.item);
            pManager.AddNumberParameter("A", "A", "get area of the closed curve", GH_ParamAccess.item);
            pManager.AddNumberParameter("Ix", "Ix", "get second moment around global x axis", GH_ParamAccess.item);
            pManager.AddNumberParameter("Iy", "Iy", "get second moment around global y axis", GH_ParamAccess.item);
            pManager.AddNumberParameter("Zx1", "Zx1", "get cross-sectional coefficient around global x axis(top)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Zx2", "Zx2", "get cross-sectional coefficient around global x axis(bottom)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Zy1", "Zy1", "get cross-sectional coefficient around global y axis(right)", GH_ParamAccess.item);
            pManager.AddNumberParameter("Zy2", "Zy2", "get cross-sectional coefficient around global y axis(left)", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var lines = new List<Curve>();
            string layer = ""; DA.GetData("layer", ref layer);
            if (layer != "")
            {
                var doc = RhinoDoc.ActiveDoc;
                var obj = doc.Objects.FindByLayer(layer);
                for (int i = 0; i < obj.Length; i++)
                {
                    lines.Add((new ObjRef(obj[i])).Curve());
                }
            }
            else
            {
                DA.GetDataList("curves", lines);
            }
            var surf = new Brep();
            if (lines.Count == 1)
            {
                surf = Brep.CreatePlanarBreps(lines[0])[0];
            }
            else
            {
                surf = Brep.CreateEdgeSurface(lines);
            }
            var A = surf.GetArea();
            var mass = AreaMassProperties.Compute(surf);
            var C = mass.Centroid;
            var I = mass.CentroidCoordinatesSecondMoments;
            var Ix = I[1]; var Iy = I[0];
            var bb = surf.GetBoundingBox(true);
            var cc = bb.GetCorners();
            var c1 = cc[0]; var c2 = cc[2];
            var ex1 = c2[1] - C[1]; var ex2 = C[1] - c1[1];
            var ey1 = c2[0] - C[0]; var ey2 = C[0] - c1[0];
            var Zx1 = Ix / ex1; var Zx2 = Ix / ex2; var Zy1 = Iy / ey1; var Zy2 = Iy / ey2;
            DA.SetData("S", surf);
            DA.SetData("A", A);
            DA.SetData("C", C);
            DA.SetData("Ix", Ix); DA.SetData("Iy", Iy);
            DA.SetData("Zx1", Zx1); DA.SetData("Zx2", Zx2); DA.SetData("Zy1", Zy1); DA.SetData("Zy2", Zy2);
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
                return OpenSeesUtility.Properties.Resources.aiz;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0e93bace-0148-40af-8f9c-0ac548077138"); }
        }
    }
}