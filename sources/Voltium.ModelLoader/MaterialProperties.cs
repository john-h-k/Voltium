namespace Voltium.ModelLoading
{
    internal struct MaterialProperties
    {
        public MaterialProperties(string name) : this()
        {
            Name = name;
        }

        public string Name;
        public double SpecularHighlights;
        public Rgb AmbientColor;
        public Rgb DiffuseColor;
        public Rgb SpecularColor;
        public Rgb EmissiveCoefficient;
        public double OpticalDensity;
        public double Dissolve;
        public IlluminationMode IlluminationMode;
    }
}
