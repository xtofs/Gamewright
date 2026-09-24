# Shader Codegen Design — Silk.NET / OpenGL 2D Library

## Goal

Replace stringly-typed `GetUniformLocation`/attribute lookups with generated,
compile-time-checked C# bindings, and lower the friction of splitting logic
across multiple small shaders instead of one uber-shader with `#ifdef` soup.

## Approach: parse GLSL, generate C# (not the reverse)

Source of truth stays as hand-written `.vert`/`.frag`/`.comp` files. A tool
parses their top-level declarations and emits typed C# bindings. Rejected
alternative: generating GLSL from C# (ShaderGen-style) — much larger
investment (a shader-compiler frontend) for a single 2D-focused library.

## Parser: tokenizer, not a full grammar

Uniform/buffer-block declarations are syntactically flat — no expressions,
no control flow. A token scan is sufficient:

- Scan top-level tokens for `uniform`/`buffer`/`in`/`out`.
- If followed by `{`, it's a block: walk members via brace-depth counting
  until the matching `}`.
- If not, it's a loose declaration: `type name[;\[]`, optional `[N]` array.
- `layout(...)` qualifiers: simple comma-separated `key = value` list.
- Skip past any `{...}` body that isn't a target keyword (function bodies
  etc.) via brace-depth counting — no need to parse them at all.
- Wrinkle: array sizes given via `#define` need a minimal preprocessor pass
  (handle `#define`, ignore/skip `#ifdef` nesting) before tokenizing.

## Build integration: plain console app, not a Roslyn generator

For a single 2D library, a Roslyn incremental source generator is not worth
the cost (API version pinning, awkward debugging, caching-semantics
footguns). Instead: a normal console app run as a pre-build MSBuild target,
writing generated `.cs` into the intermediate output folder and including it
in the compile.

```xml
<Target Name="GenShaderBindings" BeforeTargets="CoreCompile">
  <Exec Command="dotnet run --project ../ShaderGen -- $(ProjectDir)Shaders $(IntermediateOutputPath)Generated" />
</Target>
<ItemGroup>
  <Compile Include="$(IntermediateOutputPath)Generated/**/*.cs" />
</ItemGroup>
```

Debuggable like any other executable; regenerates on build (acceptable —
uniform declarations don't change every keystroke).

## Uniform locations

Prefer explicit `layout(location = N)` in shader source, mirrored as a
`const int` in generated code. Purely static — no GL context needed at
generation time, locations known before linking. `GetUniformLocation(string)`
remains only a fallback for uniforms not explicitly pinned.

## UBO / SSBO — highest-ROI target

- **UBO**: read-only, size-limited (~16–64KB, driver-dependent). Good for
  data shared across shaders (camera matrix, screen size, time).
- **SSBO**: read/write, effectively unbounded, supports a trailing unsized
  array (`data[]`), needed for compute/particle-scale data.
- Binding index (`layout(binding = N)` + `glBindBufferBase`) is the
  shader↔host contract — same footgun class as uniform locations if it's a
  bare `int` on the CPU side.

**Packing rules — the real bug source (silent GPU-side data corruption, not
a crash):**

- `std140` (UBO): scalars align to 4B, `vec2`→8B, `vec3`/`vec4`/struct→16B.
  Array elements are padded to a 16-byte stride *regardless of element
  size* — `float[8]` occupies 128 bytes, not 32.
- `std430` (SSBO, GL 4.3+): drops the array padding rule — tighter packing
  at natural alignment.

Generator computes correct per-field byte offsets from the parsed block and
emits `[StructLayout(LayoutKind.Explicit)]` C# structs with `[FieldOffset]`
per field — this is where generation earns its keep, more than scalar
uniform typing.

## Handling the "common denominator" / shared-uniform problem

Don't collapse shaders into one universal struct. Two separate mechanisms:

**1. Per-frame globals** (projection matrix, screen size, time — same for
every draw regardless of shader): one fixed `std140` UBO, bound once per
frame. Every shader wanting these declares the same block layout/binding;
set once, not per-draw/per-shader. Eliminates the "pay even when unused"
cost.

**2. Uniforms/attributes only some shaders need** (tint, rotation, extra UV):
generate a marker interface per distinct name+type signature found across
parsed shaders; each generated shader class implements only the interfaces
matching what it actually declared.

```csharp
interface IHasTint     { Vector4 Tint { set; } }
interface IHasRotation { float Rotation { set; } }

partial class SpriteShader   : IHasTint { }
partial class ParticleShader : IHasTint, IHasRotation { }
```

Render/batch code: `if (shader is IHasTint t) t.Tint = color;` — draws pay
only for what the shader declares. Comes "for free" from the same parse
pass; no schema needed up front.

**Collision rule**: same uniform *name* with different *type* across
shaders (e.g. `tint` as `vec3` in one, `vec4` in another) is a generator
error, not two silently-different `Tint` properties on two interfaces.

Vertex attributes follow the same pattern: generate a `VertexLayout` per
shader from its `in` declarations; group draw calls by layout compatibility
rather than assuming one universal vertex struct.

## Shape families: how many shaders, for rect/circle/line/polyline/text/sprite

Use SDF (signed-distance-function) shading for the geometric primitives
rather than raw triangle fills — this collapses several apparently-distinct
shapes into one shader each, and gives free anti-aliasing.

**Rounded-box family** — covers rect, circle, and rounded corners as *one*
shader. A rounded-rectangle SDF (half-extents + per-corner radius) degenerates
to a circle when half-extents shrink to a point and radius equals the
half-size, and to a plain rect when radii are zero. Corner radius is a
per-instance parameter, not a structural branch — no separate circle shader
needed.

**Capsule family** — covers line and polyline as *one* shader. A line
segment is a capsule SDF (distance to a segment, minus thickness). A
polyline is N capsule instances (one per segment) plus optional round joins,
where a round join is just the rounded-box/circle shape at radius =
half-thickness. Polylines are handled by the batching layer emitting
multiple capsule instances, not by a dedicated polyline shader.

**Fill vs stroke** is not a shape property — it's a shared post-processing
step on the distance value `d`: fill alpha via `smoothstep` around `d ≈ 0`,
stroke alpha via a band around `|d| ≈ strokeWidth/2`. Write this once as a
shared `alphaFromDistance`-style helper, shared across shaders via the
include mechanism below, rather than duplicating it per shader.

**Glyph (text) family** — genuinely distinct, not a variant of the above.
Samples a glyph atlas (bitmap or MSDF) instead of computing an analytic
distance, and needs its own vertex layout (glyph quad + atlas UV). If using
an MSDF atlas, it still ends with "distance → alpha" and can call the same
shared alpha helper, just sourced from a texture lookup instead of a formula.

**Sprite / sprite-set family** — shares *structure*, not fragment logic,
with glyph. Same vertex layout (quad + atlas UV/offset + instance transform)
and the same instanced-batch draw path, since both are "instanced textured
quad from a shared atlas" — genuinely the same bucket if shaders are grouped
by `VertexLayout` compatibility. But the fragment shader differs: glyph
samples a single/MSDF-channel distance and tints it (texture supplies shape,
uniform supplies color); sprite samples full RGBA directly (texture supplies
color). Don't force both through `alphaFromDistance` — sprite has no
distance to feed it, unless deliberately doing SDF-sprites for
resolution-independent scaling (skip unless specifically needed).
Overlap points: `IHasTint` applies to glyph always and to sprite when
per-instance color multiply is wanted (e.g. particle tinting) — legitimate
reuse of the same interface. A masked/rounded/9-slice sprite variant can
additionally reuse the rounded-box SDF, composited against the sampled
texture color instead of a flat fill.

**Arbitrary filled polygons** (concave/general shapes beyond rects) don't
fit the SDF model — tessellate on the CPU (ear clipping) into flat-filled
triangles, no analytic distance / free AA. Reuse the capsule shader for a
stroked outline on such a polygon rather than inventing a polygon-stroke
shader. Treat as an optional addition only if actually needed.

## Shader include mechanism (textual prelude injection)

GLSL has no real `#include`, and shared code (`alphaFromDistance`, the
per-frame globals UBO declaration, any other cross-shader helper) shouldn't
be hand-copy-pasted into every source file. The generator handles this as a
build-time textual stitch, not a runtime GL feature:

- Author shared code once, in its own file(s) (e.g. `common/alpha.glsl`,
  `common/frame_ubo.glsl`).
- Before tokenizing/parsing a shader, the generator textually prepends the
  relevant shared file(s) to that shader's source (simple string
  concatenation — order matters, no macro/conditional logic needed).
- Parsing then proceeds over the combined source as normal, so the shared
  UBO/helper declarations get picked up as if hand-written into each file.
- This is purely a generator-time preprocessing step — the GPU only ever
  sees one flattened source string per compiled shader, same as if it had
  been written out by hand.

## Shader program count

Four shader programs, driven by the shape-family analysis above:

1. **Rounded-box** — rect, circle, rounded corners; fill + stroke.
2. **Capsule** — line, polyline (segments + round joins); fill + stroke.
3. **Glyph** — text, via bitmap/MSDF atlas sampling + tint.
4. **Sprite** — textured quad, full RGBA sample, optional tint.

Plus an optional fifth, added only if needed: a flat-fill triangle-mesh
shader for arbitrary/concave polygons (CPU-tessellated, no analytic
distance/free AA).

Rounded-box and capsule share `IHasFill`/`IHasStroke` (rounded-box adds
`IHasCornerRadius`) and the `alphaFromDistance` include. Glyph and sprite
share vertex layout and instanced-batch draw path, with `IHasTint` applying
to both.
