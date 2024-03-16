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
            pManager.AddTextParameter("name", "name", "[joint1,...](DataList) Joint name", GH_ParamAccess.list, "BJ1");///0
            pManager.AddTextParameter("BOLT", "BOLT", "[n-M**,...](DataList) Tension-side anchor bolt", GH_ParamAccess.list,"2-M16");///1
            pManager.AddNumberParameter("Ab", "Ab", "if inputted, BOLT text is ignored", GH_ParamAccess.list);///2
            pManager.AddNumberParameter("Lb", "Lb", "[Lb1,...](DataList) Bolt length [mm]", GH_ParamAccess.list, 400);///3
            pManager.AddNumberParameter("d", "d", "[d1,...](DataList) Span between bolt center and column edge [mm]", GH_ParamAccess.list, 200);///4
            pManager.AddNumberParameter("Eb", "Eb", "[Eb1,...](DataList) Young's modulus of A.bolt [kN/m2]", GH_ParamAccess.list, 2.05e+8);///5
            pManager.AddNumberParameter("B", "B", "[B1,...](DataList) Base plate width [mm]", GH_ParamAccess.list);///6
            pManager.AddNumberParameter("D", "D", "[D1,...](DataList) Base plate height [mm]", GH_ParamAccess.list);///7
            pManager.AddNumberParameter("dt", "dt", "[dt1,...](DataList) Length between bolt center and column center [mm]", GH_ParamAccess.list);///8
            pManager.AddNumberParameter("fc", "fc", "Allowable stress of concrete [N/mm2]", GH_ParamAccess.item, 16.0);///9
            pManager.AddNumberParameter("ft", "ft", "Allowable stress of steel [N/mm2]", GH_ParamAccess.item, 235.0);///10
            pManager.AddNumberParameter("Nmax", "Nmax", "[Nmax1,...](DataList) Axial force [kN]", GH_ParamAccess.list);///11
            pManager.AddNumberParameter("Nmin", "Nmin", "[Nmin1,...](DataList) Axial force [kN]", GH_ParamAccess.list);///12
            pManager[2].Optional = true; pManager[6].Optional = true; pManager[7].Optional = true; pManager[8].Optional = true; pManager[11].Optional = true; pManager[12].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("K", "K", "[K1,...](DataList) rotational stiffness [kNm/rad]", GH_ParamAccess.list);///
            pManager.AddNumberParameter("Mamax", "Mamax", "[Mamax1,...](DataList) allowable moment under Nmax [kNm]", GH_ParamAccess.list);///
            pManager.AddNumberParameter("Mamin", "Mamin", "[Mamin1,...](DataList) allowable moment under Nmin [kNm]", GH_ParamAccess.list);///
        }
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var boltname = new List<string>(); DA.GetDataList("BOLT", boltname);
            var nb = new List<double>(); var Mb = new List<double>(); var Ab = new List<double>(); var K = new List<double>();
            if (!DA.GetDataList("Ab", Ab))
            {
                for (int i = 0; i < boltname.Count; i++)
                {
                    nb.Add(double.Parse(boltname[i].Substring(0, boltname[i].IndexOf("-"))));//
                    Mb.Add(double.Parse(boltname[i].Substring(boltname[i].IndexOf("M") + 1)));//[mm]
                    Ab.Add(nb[i] * Mb[i] * Mb[i] * Math.PI / 4.0);//[mm2]
                }
            }
            var lb = new List<double>(); DA.GetDataList("Lb", lb);
            var d = new List<double>(); DA.GetDataList("d", d);
            var Eb = new List<double>(); DA.GetDataList("Eb", Eb);
            for (int i = 0; i < boltname.Count; i++)
            {
                K.Add(Eb[i] * (Ab[i] / 1e+6) * Math.Pow(d[i] / 1000.0, 2) / 2.0 / (lb[i] / 1000.0));//[kNm/rad]
            }
            DA.SetDataList("K", K);
            var B = new List<double>(); var D = new List<double>(); var fc = 16.0; var ft = 235.0; var N_max = new List<double>(); var N_min = new List<double>(); var dt = new List<double>(); var Nu = new List<double>(); var Tu = new List<double>(); var Ma_max = new List<double>(); var Ma_min = new List<double>();
            if (!DA.GetDataList("B", B) || !DA.GetDataList("D", D) || !DA.GetDataList("dt", dt) || !DA.GetData("fc", ref fc) || !DA.GetData("ft", ref ft) || !DA.GetDataList("Nmax", N_max) || !DA.GetDataList("Nmin", N_min)) return;
            else
            {
                for (int i = 0; i < boltname.Count; i++)
                {
                    Nu.Add(0.85 * B[i] * D[i] * fc / 1000.0);///[kN]
                    Tu.Add(Ab[i] * ft / 1000.0);///[kN]
                    if (N_max[i] > Nu[i]) { Ma_max.Add(0); }
                    else if (N_max[i] > Nu[i] - Tu[i]) { Ma_max.Add(N_max[i] * dt[i] / 1000.0 * (Nu[i] / N_max[i] - 1)); }
                    else if (N_max[i] > -Tu[i]) { Ma_max.Add(Tu[i] * dt[i] / 1000.0 + (N_max[i] + Tu[i]) * (D[i] / 1000.0) / 2.0 * (1 - (N_max[i] + Tu[i]) / Nu[i])); }
                    else if (N_max[i] > -2 * Tu[i]) { Ma_max.Add((N_max[i] + 2 * Tu[i]) * dt[i] / 1000.0); }
                    else { Ma_max.Add(0); }
                    if (N_min[i] > Nu[i]) { Ma_min.Add(0); }
                    else if (N_min[i] > Nu[i] - Tu[i]) { Ma_min.Add(N_min[i] * dt[i] / 1000.0 * (Nu[i] / N_min[i] - 1)); }
                    else if (N_min[i] > -Tu[i]) { Ma_min.Add(Tu[i] * dt[i] / 1000.0 + (N_min[i] + Tu[i]) * (D[i] / 1000.0) / 2.0 * (1 - (N_min[i] + Tu[i]) / Nu[i])); }
                    else if (N_min[i] > -2 * Tu[i]) { Ma_min.Add((N_min[i] + 2 * Tu[i]) * dt[i] / 1000.0); }
                    else { Ma_min.Add(0); }
                }
            }
            DA.SetDataList("Mamax", Ma_max); DA.SetDataList("Mamin", Ma_min);
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
                return OpenSeesUtility.Properties.Resources.steelbase;
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