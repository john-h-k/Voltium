//=============================================================================
// Performs a separable Guassian blur with a blur radius up to 5 pixels.
//=============================================================================

struct Constants
{
	// We cannot have an array entry in a constant buffer that gets mapped onto
	// root constants, so list each element.  
	
    int BlurRadius;

	// Support up to 11 blur weights.
    float w0;
    float w1;
    float w2;
    float w3;
    float w4;
    float w5;
    float w6;
    float w7;
    float w8;
    float w9;
    float w10;
};

ConstantBuffer<Constants> Settings : register(b0);

static const int MaxBlurRadius = 5;


RWTexture2D<float4> Image : register(u0, space1);

#define N 256
#define CacheSize (N + 2 * MaxBlurRadius)
groupshared float4 SharedCache[CacheSize];

[NumThreads(N, 1, 1)]
void BlurHorizontal(int3 groupThreadID : SV_GroupThreadID,
				int3 dispatchThreadID : SV_DispatchThreadID)
{
	// Put in an array for each indexing.
    float weights[11] = { Settings.w0, Settings.w1, Settings.w2, Settings.w3, Settings.w4, Settings.w5, Settings.w6, Settings.w7, Settings.w8, Settings.w9, Settings.w10 };

    
    int2 dimension;
    Image.GetDimensions(dimension.x, dimension.y);
    
	//
	// Fill local thread storage to reduce bandwidth.  To blur 
	// N pixels, we will need to load N + 2*BlurRadius pixels
	// due to the blur radius.
	//
	
	// This thread group runs N threads.  To get the extra 2*BlurRadius pixels, 
	// have 2*BlurRadius threads sample an extra pixel.
    if (groupThreadID.x < Settings.BlurRadius)
    {
		// Clamp out of bound samples that occur at image borders.
        int x = max(dispatchThreadID.x - Settings.BlurRadius, 0);
        SharedCache[groupThreadID.x] = Image[int2(x, dispatchThreadID.y)];
    }
    if (groupThreadID.x >= N - Settings.BlurRadius)
    {
		// Clamp out of bound samples that occur at image borders.
        int x = min(dispatchThreadID.x + Settings.BlurRadius, dimension.x - 1);
        SharedCache[groupThreadID.x + 2 * Settings.BlurRadius] = Image[int2(x, dispatchThreadID.y)];
    }

	// Clamp out of bound samples that occur at image borders.
    SharedCache[groupThreadID.x + Settings.BlurRadius] = Image[min(dispatchThreadID.xy, dimension.xy - 1)];

	// Wait for all threads to finish.
    GroupMemoryBarrierWithGroupSync();
	
	//
	// Now blur each pixel.
	//

    float4 blurColor = float4(0, 0, 0, 0);
	
    for (int i = -Settings.BlurRadius; i <= Settings.BlurRadius; ++i)
    {
        int k = groupThreadID.x + Settings.BlurRadius + i;
		
        blurColor += weights[i + Settings.BlurRadius] * SharedCache[k];
    }
	
    Image[dispatchThreadID.xy] = blurColor;
}

[numthreads(1, N, 1)]
void BlurVertical(int3 groupThreadID : SV_GroupThreadID,
				int3 dispatchThreadID : SV_DispatchThreadID)
{
	// Put in an array for each indexing.
    float weights[11] = { Settings.w0, Settings.w1, Settings.w2, Settings.w3, Settings.w4, Settings.w5, Settings.w6, Settings.w7, Settings.w8, Settings.w9, Settings.w10 };

    int2 dimension;
    Image.GetDimensions(dimension.x, dimension.y);
    
	//
	// Fill local thread storage to reduce bandwidth.  To blur 
	// N pixels, we will need to load N + 2*BlurRadius pixels
	// due to the blur radius.
	//
	
	// This thread group runs N threads.  To get the extra 2*BlurRadius pixels, 
	// have 2*BlurRadius threads sample an extra pixel.
    if (groupThreadID.y < Settings.BlurRadius)
    {
		// Clamp out of bound samples that occur at image borders.
        int y = max(dispatchThreadID.y - Settings.BlurRadius, 0);
        SharedCache[groupThreadID.y] = Image[int2(dispatchThreadID.x, y)];
    }
    if (groupThreadID.y >= N - Settings.BlurRadius)
    {
		// Clamp out of bound samples that occur at image borders.
        int y = min(dispatchThreadID.y + Settings.BlurRadius, dimension.y - 1);
        SharedCache[groupThreadID.y + 2 * Settings.BlurRadius] = Image[int2(dispatchThreadID.x, y)];
    }
	
	// Clamp out of bound samples that occur at image borders.
    SharedCache[groupThreadID.y + Settings.BlurRadius] = Image[min(dispatchThreadID.xy, dimension.xy - 1)];


	// Wait for all threads to finish.
    GroupMemoryBarrierWithGroupSync();
	
	//
	// Now blur each pixel.
	//

    float4 blurColor = float4(0, 0, 0, 0);
	
    for (int i = -Settings.BlurRadius; i <= Settings.BlurRadius; ++i)
    {
        int k = groupThreadID.y + Settings.BlurRadius + i;
		
        blurColor += weights[i + Settings.BlurRadius] * SharedCache[k];
    }
	
    Image[dispatchThreadID.xy] = blurColor;
}
