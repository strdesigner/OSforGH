using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;

namespace MakeIndex
{
    public class MakeIndex : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public MakeIndex()
          : base("MakeIndex", "MakeIndex",
              "Read line data from Rhinoceros with all selected layers and export indexes of specified layer",
              "OpenSees", "Reading from Rhino")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("layer(all)", "layer(all)", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("layer", "layer", "[layername1,layername2,...](Datalist)", GH_ParamAccess.list);
            pManager.AddTextParameter("name wick1", "name wick1", "usertextname for wick1", GH_ParamAccess.item, "wickX");
            pManager.AddTextParameter("name wick2", "name wick2", "usertextname for wick2", GH_ParamAccess.item, "wickY");
            pManager.AddTextParameter("wick", "wick", "[wickname1,wickname2,...](Datalist)", GH_ParamAccess.list);
            pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("index", "index", "[int,int,...](Datalist)", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> layers = new List<string>(); if (!DA.GetDataList("layer(all)", layers)) { };
            List<string> layer = new List<string>(); if (!DA.GetDataList("layer", layer)) { };
            List<string> wick = new List<string>(); if (!DA.GetDataList("wick", wick)) { };
            string name_x = "wickX"; string name_y = "wickY";
            DA.GetData("name wick1", ref name_x); DA.GetData("name wick2", ref name_y);
            var doc = RhinoDoc.ActiveDoc; var index = new List<int>(); int k = 0;
            for (int i = 0; i < layers.Count; i++)
            {
                var line = doc.Objects.FindByLayer(layers[i]);
                for (int j = 0; j < line.Length; j++)
                {
                    if (layer.Contains(layers[i]))
                    {
                        if (wick.Count == 0 || wick == new List<string>())
                        {
                            index.Add(k);
                        }
                        else
                        {
                            var obj = line[j];
                            string text1 = obj.Attributes.GetUserString(name_x); string text2 = obj.Attributes.GetUserString(name_y);//軸ラベル
                            if (wick.Contains(text1) == true || wick.Contains(text2) == true)
                            {
                                index.Add(k);//指定軸が含まれていればindexを格納
                            }
                        }
                    }
                    k += 1;
                }
            }
            DA.SetDataList("index", index);
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
                return OpenSeesUtility.Properties.Resources.index;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("d512bbcd-6035-4ee3-b957-be97fe1b3f0c"); }
        }
    }
}