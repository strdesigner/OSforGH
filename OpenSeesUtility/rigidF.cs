using System;
using System.Collections.Generic;


using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.Display;
using Grasshopper.Kernel.Attributes;

namespace OpenSeesUtility
{
    public class rigidF : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public rigidF()
          : base("rigidF", "rigidF",
              "make node group for rigidDiaphragm (rigid plane)",
              "OpenSees", "Utility")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("nodal_coordinates", "R", "[[x1,y1,z1],...](DataTree)", GH_ParamAccess.tree);///
            pManager.AddBrepParameter("S", "S", "surface of rigid plane", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("rigid", "rigid", "[[node,node,node,...],[node,node,node,...]]", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetDataTree("nodal_coordinates", out GH_Structure<GH_Number> _r); var r = _r.Branches;
            var surfaces = new List<Brep>(); DA.GetDataList("S", surfaces);
            var rigid = new GH_Structure<GH_Number>();
            for (int i = 0; i < surfaces.Count; i++)
            {
                var surf = surfaces[i]; var face = surf.Faces[0];
                var solid = Brep.CreateFromOffsetFace(face, 0.1, 5e-3, true, true);
                var nodes = new List<GH_Number>();
                for (int j = 0; j < r.Count; j++)
                {
                    var p = new Point3d(r[j][0].Value, r[j][1].Value, r[j][2].Value);
                    if (solid.IsPointInside(p, 5e-3, false) == true)
                    {
                        nodes.Add(new GH_Number(j));
                    }
                }
                rigid.AppendRange(nodes, new GH_Path(i));
            }
            DA.SetDataTree(0, rigid);
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
                return OpenSeesUtility.Properties.Resources.rigidplane;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("f43202a4-136e-48bc-bd27-ee3318dd1365"); }
        }
    }
}