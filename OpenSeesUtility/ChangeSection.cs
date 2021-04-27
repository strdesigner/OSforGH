using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
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
            var name_B = "-width"; DA.GetData("name -B", ref name_B); var name_D = "-height"; DA.GetData("name -D", ref name_D);
            var burnwidth = new List<double>(); var burnheight = new List<double>();
            List<int> index = new List<int>(); List<Curve> lines = new List<Curve>();
            var doc = RhinoDoc.ActiveDoc;
            int k = 0;
            for (int i = 0; i < layers.Count; i++)
            {
                var line = doc.Objects.FindByLayer(layers[i]);
                for (int j = 0; j < line.Length; j++)
                {
                    if (layer.Contains(layers[i]))
                    {
                        index.Add(k);
                        var obj = line[j]; var B = 0.0; var D = 0.0;
                        var text = obj.Attributes.GetUserString(name_B);//燃えしろ幅
                        if (text == null) { B = 0.0; }
                        else { B = double.Parse(text); }
                        text = obj.Attributes.GetUserString(name_D);//燃えしろせい
                        if (text == null) { D = 0.0; }
                        else { D = double.Parse(text); }
                        burnwidth.Add(B); burnheight.Add(D);
                    }
                    k += 1;
                }
                        
            }
            DA.SetDataList("subtracted width", burnwidth);
            DA.SetDataList("subtracted height", burnheight);
            DA.SetDataList("index(burn)", index);

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