using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace OpenSeesUtility
{
    public class OpenSeesUtilityInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "OpenSeesUtility";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("fe33c20e-af10-4b62-ba76-f5c74808d663");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "SHINNOSUKE FUJITA(The Univ. of Kitakyu. Assoc. prof., / DN-Archi Co.,Ltd. Co-founder)";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "shinnosuke@dn-archi.com";
            }
        }
    }
}
