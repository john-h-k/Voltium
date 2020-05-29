# DDSTextureLoader.NET

 A DDS texture loader for .NET programs

## Texture Creation

First, a texture descriptor `DdsTextureDescription` must be created from the DDS file. To do this, you call  `DdsTextureLoader.CreateDdsTexture`.

 ```cs
public static DdsTextureDescription CreateDdsTexture(
            string filename,
            uint mipMapMaxSize = default,
            LoaderFlags loaderFlags = LoaderFlags.None
);

public static DdsTextureDescription CreateDdsTexture(
            Stream stream,
            uint mipMapMaxSize = default,
            LoaderFlags loaderFlags = LoaderFlags.None
);

public static DdsTextureDescription CreateDdsTexture(
            Memory<byte> ddsData,
            uint mipMapMaxSize = default,
            LoaderFlags loaderFlags = LoaderFlags.None
);
```

| Parameter                      | Type                         | Description                                                     |
|--------------------------------|------------------------------|-----------------------------------------------------------------|
| `fileName`/`stream`/`ddsData`  | `String/Stream/Memory<byte>` | The data to create the texture                                  |
| `mipMapMaxSize`                | `UInt32`                     | The largest size a mipmap can be (all larger will be discarded) |
| `loaderFlags`                  | `LoaderFlags`                | The flags used by the loader                                    |

LoaderFlags:
* `LoaderFlags.None` - The default. No flags
* `LoaderFlags.Srgb` - Convert the return format to the equivalent format with SRGB enabled. Note: No data conversion occurs
* `LoaderFlags.ReserveMips` - Reserves space for (but does not generate) MIPs


After creation, you can inspect the read-only struct `DdsTextureDescription`.
You then need to schedule it for upload using

```cs
public static void RecordTextureUpload(
            ID3D12Device* device,
            ID3D12GraphicsCommandList* cmdList,
            in DdsTextureDescription textureDescription,
            out ID3D12Resource* textureBuffer,
            out ID3D12Resource* textureBufferUploadHeap,
            D3D12_RESOURCE_FLAGS resourceFlags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE
);
```

| Parameter                 | Type                         | Description                                                                |
|---------------------------|------------------------------|----------------------------------------------------------------------------|
| `device`                  | `ID3D12Device*`              | The device used for resource creation                                      |
| `cmdList`                 | `ID3D12GraphicsCommandList*` | The command list that upload commands will be recorded to                  |
| `textureDescription`      | `DdsTextureDescription`      | The texture to be uploaded                                                 |
| `textureBuffer`           | `ID3D12Resource*`            | The resource that will contain uploaded texture                            |
| `textureBufferUploadHeap` | `ID3D12Resource*`            | The intermediate resource used to copy the texture between the CPU and GPU |
| `resourceFlags`           | `D3D12_RESOURCE_FLAGS`       | Resource flags used in creation of `textureBuffer`                         |


You must then execute the command list, and once it is done, the texture is present in `textureBuffer`.

To wait for the texture to be uploaded, you can use a fence. Useful tip - calling `ID3D12Fence::SetEventOnCompletion` with a `hEvent` parameter of `null` will block until the fence is signaled.