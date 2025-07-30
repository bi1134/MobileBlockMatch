Shader "Custom/StencilWriter"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-10" }

        Pass
        {
            ColorMask 0 // Không vẽ màu
            ZWrite Off

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }
        }
    }
}
