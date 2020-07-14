namespace Voltium.Core
{
    /// <summary>
    /// Represents a screen viewport
    /// </summary>
    public readonly struct Viewport
    {
        /// <summary>
        /// X position of the left hand side of the viewport
        /// </summary>
        public readonly float TopLeftX;

        /// <summary>
        /// Y position of the top of the viewport
        /// </summary>
        public readonly float TopLeftY;

        /// <summary>
        /// Width of the viewport
        /// </summary>
        public readonly float Width;

        /// <summary>
        /// Height of the viewport
        /// </summary>
        public readonly float Height;

        /// <summary>
        /// Minimum depth of the viewport. Ranges between 0 and 1
        /// </summary>
        public readonly float MinDepth;

        /// <summary>
        /// Maximum depth of the viewport. Ranges between 0 and 1
        /// </summary>
        public readonly float MaxDepth;

        /// <summary>
        /// Creates a new instance of <see cref="Viewport"/>
        /// </summary>
        public Viewport(float topLeftX, float topLeftY, float width, float height, float minDepth, float maxDepth)
        {
            TopLeftX = topLeftX;
            TopLeftY = topLeftY;
            Width = width;
            Height = height;
            MinDepth = minDepth;
            MaxDepth = maxDepth;
        }
    }
}
