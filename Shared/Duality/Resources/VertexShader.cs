﻿using System.IO;

namespace Duality.Resources
{
    /// <summary>
    /// Represents an OpenGL VertexShader.
    /// </summary>
    public class VertexShader : AbstractShader
    {
        /// <summary>
        /// [GET] A minimal VertexShader. It performs OpenGLs default transformation
        /// and forwards a single texture coordinate and color to the fragment stage.
        /// </summary>
        public static ContentRef<VertexShader> Minimal { get; private set; }

        internal static void InitDefaultContent()
        {
            InitDefaultContent(".vert", stream => {
                using (StreamReader reader = new StreamReader(stream)) {
                    string code = reader.ReadToEnd();
                    return new VertexShader(code);
                }
            });
        }


        protected override ShaderType Type
        {
            get { return ShaderType.Vertex; }
        }

        public VertexShader() : base(Minimal.IsAvailable ? Minimal.Res.Source : string.Empty) { }
        public VertexShader(string sourceCode) : base(sourceCode) { }
    }
}