namespace SKCivilianIndustry
{
    public static class CivilianResourceHexColors
    {
        public static string[] Color;

        static CivilianResourceHexColors()
        {
            Color = new string[(int)CivilianResource.Length];

            Color[(int)CivilianResource.Atium] = "9a9a9a";
            Color[(int)CivilianResource.Saronite] = "43464B";
            Color[(int)CivilianResource.Collapsium] = "e0ca8b";
            Color[(int)CivilianResource.Byzanium] = "72eb6e";
            Color[(int)CivilianResource.Orichalcum] = "e8e28b";
            Color[(int)CivilianResource.Carbonadium] = "52689c";
            Color[(int)CivilianResource.Inerton] = "8f1579";
            Color[(int)CivilianResource.Naqahdah] = "a83e3e";
            Color[(int)CivilianResource.Frinkonium] = "10adb3";
            Color[(int)CivilianResource.Computronium] = "c2ffc6";

        }
    }
}
