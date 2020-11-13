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
    float z;
    float w;
}


#if TEXTURE
#define RWType2D RWTexture2D
#define Type2D Texture2D
#else
#define RWType2D RWStructuredBuffer
#define Type2D StructuredBuffer
#endif

// ReadWriteBuffer<Vector4> buffer "source"
RWType2D<float4> source : register(u0);

// ReadWriteBuffer<Vector4> buffer "target"
RWType2D<float4> target : register(u1);

// ReadOnlyBuffer<Vector2> buffer "kernel"
Type2D<float2> kernel : register(t0);

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
        int num2 = clamp(ids.y, 0, maxY);
        for (int i = 0; i < kernelLength; i++)
        {
            int num3 = clamp(x + i - num, 0, maxX);

            #if TEXTURE
            int2 num4 = (num2 * 2, num3 * 2);
            #else
            int num4 = num2 * width * 2 + num3 * 2;
            #endif
            
            float4 right = source[num4];

            #if TEXTURE
            float4 right2 = source[num4 + (0, 1)];
            #else
            float4 right2 = source[num4 + 1];
            #endif
 
            float2 _vector = kernel[i];
            
            _zero += _vector.x * right - _vector.y * right2;
            zero2 += _vector.x * right2 + _vector.y * right;
        }
        #if TEXTURE
        int2 targetIndex = (num2 * 2, num3 * 2);
        #else
        int targetIndex = ids.y * width + ids.x;
        #endif
      
        target[targetIndex] += _zero * z + zero2 * w;
    }
}
