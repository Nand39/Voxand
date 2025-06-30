[Flags]
public enum BufferTargetFlags
{
    None = 0,
    ParameterBuffer = 1 << 0,
    ArrayBuffer = 1 << 1,
    ElementArrayBuffer = 1 << 2,
    PixelPackBuffer = 1 << 3,
    PixelUnpackBuffer = 1 << 4,
    UniformBuffer = 1 << 5,
    TextureBuffer = 1 << 6, 
    TransformFeedbackBuffer = 1 << 7,
    CopyReadBuffer = 1 << 8, 
    CopyWriteBuffer = 1 << 9,          
    DrawIndirectBuffer = 1 << 10,         
    ShaderStorageBuffer = 1 << 11,
    DispatchIndirectBuffer = 1 << 12,
    QueryBuffer = 1 << 13,
    AtomicCounterBuffer = 1 << 14,
}