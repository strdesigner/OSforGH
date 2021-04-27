using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace OpenSeesUtility
{
    public class PickTree : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public PickTree()
          : base("PickTree", "PickTree",
              "Pick specific element of tree",
              "OpenSees", "Utility")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("T", "T", "tree", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("i", "i", "index i", GH_ParamAccess.item);
            pManager.AddIntegerParameter("j", "j", "index j", GH_ParamAccess.item);
            pManager.AddIntegerParameter("k", "k", "index k", GH_ParamAccess.item);
            pManager[1].Optional = true; pManager[2].Optional = true; pManager[3].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("T", "T", "tree", GH_ParamAccess.tree);
            pManager.AddNumberParameter("L", "L", "list", GH_ParamAccess.list);
            pManager.AddNumberParameter("I", "I", "item", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetDataTree("T", out GH_Structure<GH_Number> _T); int indexi = -1; if (!DA.GetData("i", ref indexi)) { }; int indexj = -1; if (!DA.GetData("j", ref indexj)) { }; int indexk = -1; if (!DA.GetData("k", ref indexk)) { };
            var T = new GH_Structure<GH_Number>(); var L = new List<double>(); var I = double.NaN;
            int n = 0; IList<List<GH_Number>> t; t = _T.Branches;
            if (indexi != -1 && indexj == -1 && indexk == -1)
            {
                while (_T.PathExists(new GH_Path(indexi, n)) == true)
                {
                    n += 1;
                }
                for (int i = 0; i < n; i++)
                {
                    T.AppendRange(t[indexi * n + i], new GH_Path(i));
                }
            }
            else if (indexi == -1 && indexj != -1 && indexk == -1)
            {
                while (_T.PathExists(new GH_Path(n, indexj)) == true)
                {
                    n += 1;
                }
                for (int i = 0; i < n; i++)
                {
                    int m = 0;
                    while (_T.PathExists(new GH_Path(i, m)) == true)
                    {
                        m += 1;
                    }
                    T.AppendRange(t[m * i + indexj], new GH_Path(i));
                }
            }
            else if (indexi == -1 && indexj == -1 && indexk != -1)
            {
                for (int i = 0; i < t.Count; i++)
                {
                    var tlist = new List<GH_Number>();
                    int m = 0;
                    while (_T.PathExists(new GH_Path(i, m)) == true)
                    {
                        tlist.Add(new GH_Number(t[i][m].Value));
                        m += 1;
                    }
                    T.AppendRange(tlist, new GH_Path(i));
                }
            }
            else if (indexi != -1 && indexj != -1 && indexk == -1)
            {
                var tlist = _T.get_Branch(new GH_Path(indexi, indexj));
                DA.SetDataList("L", tlist);
            }
            else if (indexi != -1 && indexj == -1 && indexk != -1)
            {
                while (_T.PathExists(new GH_Path(n, 0)) == true)
                {
                    n += 1;
                }
                int m = 0;
                while (_T.PathExists(new GH_Path(0, m)) == true)
                {
                    m += 1;
                }
                for (int j = 0; j < m; j++)
                {
                    L.Add(t[indexi * m + j][indexk].Value);
                }
                DA.SetDataList("L", L);
            }
            else if (indexi == -1 && indexj != -1 && indexk != -1)
            {
                while (_T.PathExists(new GH_Path(n, 0)) == true)
                {
                    n += 1;
                }
                int m = 0;
                while (_T.PathExists(new GH_Path(0, m)) == true)
                {
                    m += 1;
                }
                for (int i = 0; i < n; i++)
                {
                    L.Add(t[i * m + indexj][indexk].Value);
                }
                DA.SetDataList("L", L);
            }
            else if (indexi != -1 && indexj != -1 && indexk != -1)
            {
                var tlist = _T.get_Branch(new GH_Path(indexi, indexj));
                DA.SetData("I", tlist[indexk]);
            }
            DA.SetDataTree(0, T); //DA.SetData("I", I);
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
                return OpenSeesUtility.Properties.Resources.picktree;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("f376306e-3e14-452c-a1f5-64d6a4107325"); }
        }
    }
}