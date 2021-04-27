using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;

namespace OpenSeesUtility
{
    public class ReadViscousDamper : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ReadViscousDamper()
          : base("ReadViscousDamper", "ReadDamper",
              "Read line data from Rhinoceros with selected layer and export viscous damper information for OpenSees",
              "OpenSees", "Reading from Rhino")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("layer", "layer", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("name K", "name K", "usertextname for internal stiffness[kN/m]", GH_ParamAccess.item, "K");
            pManager.AddTextParameter("name Cd", "name Cd", "usertextname for damping coefficient[kN/(m/sec)^alpha]", GH_ParamAccess.item, "Cd");
            pManager.AddTextParameter("name alpha", "name alpha", "usertextname for velocity exponent", GH_ParamAccess.item, "alpha");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("dlines", "dlines", "Line of viscous damper elements", GH_ParamAccess.list);
            pManager.AddNumberParameter("KCa", "KCa", "[[Kd[kN/m], ad, Cd[kN/(m/sec)^(1/ad)],...(DataTree)]", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> layer = new List<string>(); DA.GetDataList("layer", layer);
            var name_K = "K"; DA.GetData("name K", ref name_K); var name_Cd = "Cd"; DA.GetData("name Cd", ref name_Cd); var name_alpha = "Cd"; DA.GetData("name alpha", ref name_alpha);
            var doc = RhinoDoc.ActiveDoc; List<Curve> lines = new List<Curve>(); GH_Structure<GH_Number> KCa = new GH_Structure<GH_Number>();
            for (int i = 0; i < layer.Count; i++)
            {
                var line = doc.Objects.FindByLayer(layer[i]);
                for (int j = 0; j < line.Length; j++)
                {
                    var obj = line[j]; Curve[] l = new Curve[] { (new ObjRef(obj)).Curve() };
                    int nl = (new ObjRef(obj)).Curve().SpanCount;//ポリラインのセグメント数
                    if (nl > 1) { l = (new ObjRef(obj)).Curve().DuplicateSegments(); }
                    for (int jj = 0; jj < nl; jj++)
                    {
                        lines.Add(l[jj]);
                        List<GH_Number> kcalist = new List<GH_Number>();
                        var text = obj.Attributes.GetUserString(name_K);
                        if (text == null) { kcalist.Add(new GH_Number(25000)); }
                        else { kcalist.Add(new GH_Number(float.Parse(text))); }
                        text = obj.Attributes.GetUserString(name_Cd);
                        if (text == null) { kcalist.Add(new GH_Number(450)); }
                        else { kcalist.Add(new GH_Number(float.Parse(text))); }
                        text = obj.Attributes.GetUserString(name_alpha);
                        if (text == null) { kcalist.Add(new GH_Number(0.3)); }
                        else { kcalist.Add(new GH_Number(float.Parse(text))); }
                        KCa.AppendRange(kcalist, new GH_Path(jj));
                    }
                }
            }
            DA.SetDataList("dlines", lines);
            DA.SetDataTree(1, KCa);
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
                return OpenSeesUtility.Properties.Resources.readdamper;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("befa4e88-af48-468e-9ac4-00fa512f20b2"); }
        }
    }
}