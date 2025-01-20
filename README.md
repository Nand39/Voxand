# Voxand
**Voxand is a software ray tracing engine** specifically designed for **voxels**. It simulates light behavior on a per-pixel basis, targeting real-time performance.

## About
**Voxand uses OpenGL via OpenTK for graphics**. Raw rendering is done in GLSL compute shaders.  
The renderer can partially simulate indirect illumination but currently lacks path tracing recursion.
The random nature of secondary rays introduces noise, which is reduced by temporal anti-aliasing with reprojection to account for changes in the scene or camera orientation.
The image is then upscaled and postprocessed, as the rendering resolution might differ from the target display resolution.

## Requirements
* Target framework: .NET 8.0;
* Supported OS: Windows (Linux support might be added in the future);
* OpenGL version: OpenGL 4.3 or higher.

## License
This work is licensed under the **GNU Affero General Public License v3**. See [LICENSE](./LICENSE.txt) for more details.