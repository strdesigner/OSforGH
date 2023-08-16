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
    public class FromGridPoints2 : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public FromGridPoints2()
          : base("FromGridPoints2", "FromGridPts2",
              "Create BEAM and SHELL on B-spline surface from grid points",
              "OpenSees", "Utility")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("P", "P", "[[[x00,y00,z00],...],[x10,y10,z10],...]](DataTree)", GH_ParamAccess.tree);///
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("lines", "BEAM", "Line of elements", GH_ParamAccess.list);
            pManager.AddSurfaceParameter("shells", "SHELL", "Plate of elements", GH_ParamAccess.list);
            pManager.AddSurfaceParameter("shells2", "SHELL(tri)", "Plate of elements", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (!DA.GetDataTree("P", out GH_Structure<GH_Point> _r)) { return; }
            else
            {
                var r = _r.Branches; var U = r.Count; var V = r[0].Count; var lines = new List<Line>(); var shell = new List<Surface>(); var shell2 = new List<Surface>();
                for (int i = 0; i < U; i++)
                {
                    for (int j = 0; j < V - 1; j++)
                    {
                        var r1 = r[i][j].Value; var r2 = r[i][j + 1].Value;
                        lines.Add(new Line(r1, r2));
                    }
                }
                for (int i = 0; i < U - 1; i++)
                {
                    for (int j = 0; j < V; j++)
                    {
                        var r1 = r[i][j].Value; var r2 = r[i + 1][j].Value;
                        lines.Add(new Line(r1, r2));
                    }
                }
                DA.SetDataList("lines", lines);
                for (int i = 0; i < U - 1; i++)
                {
                    for (int j = 0; j < V - 1; j++)
                    {
                        var r1 = r[i][j].Value; var r2 = r[i + 1][j].Value; var r3 = r[i + 1][j + 1].Value; var r4 = r[i][j + 1].Value;
                        shell.Add(NurbsSurface.CreateFromCorners(r1, r2, r3, r4));
                    }
                }
                DA.SetDataList("shells", shell);
                for (int i = 0; i < U - 1; i++)
                {
                    for (int j = 0; j < V - 1; j++)
                    {
                        if ((i < (int)(U / 2) && j < (int)(V / 2)) || (i >= (int)(U / 2) && j >= (int)(V / 2)))
                        {
                            var r1 = r[i][j].Value; var r2 = r[i + 1][j].Value; var r3 = r[i + 1][j + 1].Value; var r4 = r[i][j + 1].Value;
                            shell2.Add(NurbsSurface.CreateFromCorners(r1, r2, r4, r4));
                            shell2.Add(NurbsSurface.CreateFromCorners(r2, r3, r4, r4));
                        }
                        else
                        {
                            var r1 = r[i][j].Value; var r2 = r[i + 1][j].Value; var r3 = r[i + 1][j + 1].Value; var r4 = r[i][j + 1].Value;
                            shell2.Add(NurbsSurface.CreateFromCorners(r1, r3, r4, r4));
                            shell2.Add(NurbsSurface.CreateFromCorners(r1, r2, r3, r3));
                        }
                    }
                }
                DA.SetDataList("shells2", shell2);
            }
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
                return OpenSeesUtility.Properties.Resources.fromgridpoints2;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5068667c-52b9-425b-95a1-190a8763eb60"); }
        }
    }
}