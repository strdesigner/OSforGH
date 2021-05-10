using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace OpenSeesUtility
{
    public class SteelBase : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public SteelBase()
          : base("SteelBase", "SteelBase",
              "Calc rotational stiffness of exposed column-bases and check allowable stress",
              "OpenSees", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("name", "name", "[joint1,...](DataList) Joint name", GH_ParamAccess.list, "BJ1");///
            pManager.AddTextParameter("BOLT", "BOLT", "[n-M**,...](DataList) Tension-side anchor bolt", GH_ParamAccess.list,"2-M16");///
            pManager.AddNumberParameter("Lb", "Lb", "[Lb1,...](DataList) Bolt length [mm]", GH_ParamAccess.list, 400);///
            pManager.AddNumberParameter("d", "d", "[d1,...](DataList) Span between bolt center and column edge [mm]", GH_ParamAccess.list, 200);///
            pManager.AddNumberParameter("Eb", "Eb", "[Eb1,...](DataList) Young's modulus of A.bolt [kN/m2]", GH_ParamAccess.list, 2.05e+8);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("K", "K", "[K1,...](DataList) rotational stiffness [kNm/rad]", GH_ParamAccess.list);///
        }
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var boltname = new List<string>(); DA.GetDataList("BOLT", boltname);
            var nb = new List<double>(); var Mb = new List<double>(); var Ab = new List<double>(); var K = new List<double>();
            for (int i = 0; i < boltname.Count; i++)
            {
                nb.Add(double.Parse(boltname[i].Substring(0, boltname[i].IndexOf("-"))));
                Mb.Add(double.Parse(boltname[i].Substring(boltname[i].IndexOf("M"))));
                Ab.Add(nb[i] * Mb[i] * Mb[i] * Math.PI / 4.0);
            }
            var lb = new List<double>(); DA.GetDataList("Lb", lb);
            var d = new List<double>(); DA.GetDataList("d", d);
            var Eb = new List<double>(); DA.GetDataList("Eb", Eb);
            for (int i = 0; i < boltname.Count; i++)
            {
                K.Add(Eb[i] * Ab[i] * Math.Pow(d[i] / 1000.0, 2) / 2.0 / (lb[i] / 1000.0));
            }
            DA.SetDataList("K", K);
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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("35c33863-e7b3-46ed-b23a-d13480c526f5"); }
        }
    }
}