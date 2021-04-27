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

namespace AssembleTree
{
    public class AssembleTree : GH_Component
    {
        public static int n = 5;
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public AssembleTree()
          : base("AssembleTree", "AssembleTree",
              "AssembleTrees( DataTrees must be same structure each other)",
              "OpenSees", "Utility")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            for(int i=0;i<n;i++){ pManager.AddNumberParameter("tree", "T", "DataTree(must be same structure)", GH_ParamAccess.tree, -9999); }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("assembled tree", "T", "Assembled DataTree", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var A = new GH_Structure<GH_Number>(); int e = 0;
            for (int k = 0; k < n; k++)
            {
                if (!DA.GetDataTree(k, out GH_Structure<GH_Number> _T)) { }
                else if(_T.Branches[0][0].Value != -9999) 
                {
                    var T = _T.Branches;
                    var ni = T.Count; var nj = T[0].Count;
                    for(int i = 0; i < ni; i++)
                    {
                        var tlist = new List<GH_Number>();
                        for (int j = 0; j < nj; j++) { tlist.Add(new GH_Number(T[i][j].Value)); }
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
                return OpenSeesUtility.Properties.Resources.assembletree;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("dd609588-bf92-4e51-95f7-e484312ea220"); }
        }
    }
}