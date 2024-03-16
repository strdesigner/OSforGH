using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System.Drawing;
using System.Windows.Forms;
///for GUI*********************************
using Grasshopper.Kernel.Attributes;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
///****************************************

namespace OpenSeesUtility
{
    public class AssembleTreeText : GH_Component
    {
        public static int n = 5;
        public AssembleTreeText()
          : base("AssembleTreeText", "AssembleTreeText",
              "AssembleTrees( DataTrees must be same structure each other)",
              "OpenSees", "Utility")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            for (int i = 0; i < n; i++) { pManager.AddTextParameter("tree", "T", "DataTree(must be same structure)", GH_ParamAccess.tree, "-9999"); }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("assembled tree", "T", "Assembled DataTree", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var A = new GH_Structure<GH_String>(); int e = 0;
            for (int k = 0; k < n; k++)
            {
                if (!DA.GetDataTree(k, out GH_Structure<GH_String> _T)) { }
                else if (_T.Branches[0][0].Value != "-9999")
                {
                    var T = _T.Branches;
                    var ni = T.Count;
                    for (int i = 0; i < ni; i++)
                    {
                        var nj = T[i].Count;
                        var tlist = new List<GH_String>();
                        for (int j = 0; j < nj; j++) { tlist.Add(new GH_String(T[i][j].Value)); }
                        A.AppendRange(tlist, new GH_Path(e));
                        e += 1;
                    }
                }
            }
            DA.SetDataTree(0, A);
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
                return OpenSeesUtility.Properties.Resources.assembletreetext;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("3009b6f7-1b38-4ab8-ac06-ed0e1d4c9133"); }
        }
    }
}