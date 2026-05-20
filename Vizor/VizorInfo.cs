using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Vizor
{
    public class VizorInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "Vizor";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                return Vizor.Properties.Resources.vizor_logo_24;
            }
        }
        public override string Description
        {
            get
            {
                return "Toolkit for collaborative fabrication with augmented reality";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("f37f357b-45b7-4d76-8b88-53f187c993e5");
            }
        }

        public override string AuthorName
        {
            get
            {
                return "Xiliu Yang";
            }
        }
        public override string AuthorContact
        {
            get
            {
                return "xiliu.yang@icd.uni-stuttgart.de";
            }
        }
    }
}
