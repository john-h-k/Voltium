/* ===============
 * AUTO GENERATED
 * ===============
 * This shader was created by ComputeSharp.
 * For info or issues: https://github.com/Sergio0694/ComputeSharp */

// Scalar/vector variables
cbuffer _ : register(b0)
{
    uint __x__; // Target X iterations
    uint __y__; // Target Y iterations
    uint __z__; // Target Z iterations
    int width;
    int maxY;
    int maxX;
    int kernelLength;
}


#if TEXTURE
#define RWType2D RWTexture2D
#define Type2D Texture2D
#else
#define RWType2D RWStructuredBuffer
#define Type2D StructuredBuffer
#endif


// ReadOnlyBuffer<Vector4> buffer "source"
Type2D<float4> source : register(t0);

// ReadWriteBuffer<Vector4> buffer "target"
RWType2D<float4> target : register(u0);

// ReadOnlyBuffer<Vector2> buffer "kernel"
Type2D<float2> kernel : register(t1);

// Shader body
[Shader("compute")]
[NumThreads(8, 8, 1)]
void CSMain(uint3 ids : SV_DispatchThreadId)
{
    if (ids.x < __x__ &&
        ids.y < __y__ &&
        ids.z < __z__) // Automatic bounds check
    {
        float4 _zero = (float4) 0;
        float4 zero2 = (float4) 0;
        int num = kernelLength >> 1;
        int x = ids.x;
        for (int i = 0; i < kernelLength; i++)
        {
            int num2 = clamp(ids.y + i - num, 0, maxY);
            int num3 = clamp(x, 0, maxX);

            #if TEXTURE
            float4 right = source[num2, num3];
            #else
            float4 right = source[num2 * width + num3];
            #endif
            
            float2 _vector = kernel[i];
            _zero += _vector.x * right;
            zero2 += _vector.y * right;
        }
        #if TEXTURE
        int num4 = (ids.y * 2, ids.x * 2);
        #else
        int num4 = ids.y * width * 2 + ids.x * 2;
        #endif
        
        target[num4] = _zero;
        
#if TEXTURE
        target[num4 + (0, 1)] = zero2;
#else
        target[num4 + 1] = zero2;
#endif
    }
}
