# HotReloadSpike

A test of whether splitting a Silk.NET app into two sides makes .NET Hot Reload work:

- **Managed game code** records draw commands into a byte buffer on its own thread.
- **The native render loop** only replays that buffer against OpenGL.

## How it works

```
game thread                                      main thread (GLFW)
───────────                                      ──────────────────
GameLoop.Tick (60 Hz)                            RenderHost.HandleRender
  scene.Update(dt, input)                          frame = frames.AcquireLatest()
  buf = frames.BeginWrite()                        ctx.BeginFrame(fbSize)
  scene.Draw(new Canvas(buf, size))                RenderCommandExecutor.Execute(frame.Data, ctx)
    canvas.DrawCircle(..) ── buf.Write(cmd) ──►
  frames.Publish()          [op][struct]...          switch(op) → XxxCommand.Execute(ctx, ref cmd)
                                                   ctx.EndFrame()
        ▲                                                │
        └──────── HostChannel: input queue, framebuffer size, close request ◄──┘
```

- `Game/Canvas`: what game code draws with. `Clear`, `DrawRectangle`,
  `DrawRoundedRectangle`, `DrawCircle`, `DrawLine`, `DrawPolyline` and `DrawTriangle`
  validate their arguments and write the matching command struct. It's a thin struct over
  the buffer, and its names follow Gamewright.Graphics' `Canvas`.
- `Commands/`: one unmanaged struct per command. Each has a `public const RenderOp OpCode`
  and a `static Execute(RenderContext, ref T)`.
- `Rendering/RenderCommandBuffer`: `[1-byte op][struct bytes]`, packed with no padding and
  grown on demand. The opcode is read by reflection once per type.
- `Rendering/RenderCommandReader` / `RenderCommandExecutor`: decode the stream and dispatch
  each command through a `switch`.
- `Rendering/RenderContext`: instanced SDF batches (rounded box, capsule, triangle). A
  batch is flushed when the command kind changes, so draw order is kept.
- `Resources/`: textures and fonts (see below).
- `Threading/FrameExchange`: a triple buffer. Neither side blocks, and the renderer
  re-renders the last frame when no new one has arrived.
- `Game/GameLoop`: if the scene throws, the loop logs the error and keeps the last good frame.

## Resources: textures and fonts

GL objects can only be created on the main thread, but scene code runs on the game thread.
So loading is split in two, and the halves are joined by a handle:

| Step | Thread | Where |
|---|---|---|
| Ask for a resource | game | `IScene.Load(ResourceLoader)`, called once before the first tick |
| Decode the PNG, rasterize the font, compute glyph metrics | game | `ResourceLoader.Texture` / `.Font` (CPU only) |
| Upload to the GPU | main | `TextureStore.ProcessUploads()`, called in `RenderHost.HandleRender` before the replay |
| Delete | main | `TextureStore.Dispose()` in `RenderHost.HandleClosing` |

- `ResourceLoader` returns a `Texture` (a `TextureId` plus its size) right away and queues the
  pixels on the `TextureUploadQueue`.
- Commands carry the unmanaged `TextureId`. `DrawSpriteCommand.Execute` looks up the GL handle
  in `TextureStore`.
- An upload is always queued before any frame that uses it is published, and the render thread
  uploads before it replays. So a command never refers to a texture that isn't on the GPU yet.
- `Font` keeps the glyph metrics on the game side. `Canvas.DrawText` lays out the text there and
  writes one `DrawSprite` per glyph, so no strings go into the command buffer.
- `ResourceLoader` caches by path. `DemoScene` keeps the loader, so a Hot Reload edit to `Draw`
  can load a new sprite sheet with no restart.

## Run

```bash
dotnet watch --project src/Spikes/HotReloadSpike --hot-reload
```

Hot Reload is on by default in `dotnet watch`. `--hot-reload` in that position is passed through
to the app as an argument (the process runs as `HotReloadSpike --hot-reload`), and the app
ignores it.

Edit `Game/DemoScene.cs` and save. Each applied update prints `hot reload applied: …`
(from `HotReloadHandler`).

## Findings (2026-09-27, macOS, .NET SDK 10.0.302, Silk.NET 2.23.0)

| Test | Result |
|---|---|
| Spike under `dotnet watch`: change the clear color in `DemoScene.Draw` | ✅ Applied in ~330 ms, no restart |
| Spike: add a `Console.WriteLine` to `Draw` | ✅ The new line ran on the next tick |
| Spike: make `Draw` throw | ✅ Logged once, window kept the last frame, app stayed alive |
| Spike: remove the throw | ✅ `scene recovered`, rendering resumed |
| **Baseline: Amazons (existing `Window`/`Canvas`) under `dotnet watch`, edit `OnRender`** | ✅ **Also applied in place (~380 ms). The edited `OnRender` ran on the next frames** |
| Spike: in a hot edit to `Draw`, call `_resources.Texture(...)` for a new file and draw it | ✅ Loaded once, then cached (same `TextureId` every tick), no errors |
| Spike: GL check after adding sprites and text | ✅ No `glGetError`; a read-back pixel showed the sprite drawn over its cell |
| Either app under the VS Code debugger | ⏳ Not tested yet |

**Conclusion so far:** with `dotnet watch`, Hot Reload of method bodies already works in
the current architecture. `OnRender` is called again every frame, so the new body is
picked up even though the main thread sits in native GLFW code. Under `dotnet watch` the
command-buffer split is **not needed** for basic Hot Reload.

What the split still adds:
- **Crash isolation.** In the baseline, an exception in `OnRender` escapes into the Silk.NET
  loop and ends the app. In the spike it's logged and the last frame stays on screen.
- **Game code never runs inside a native callback**, so it can't stall or crash the render
  thread, and it can be ticked, paused or stepped separately.
- **A serialisable frame.** The command stream could be recorded, diffed or replayed.

Still open:
- Hot Reload under the VS Code debugger (C# Dev Kit), where the debugger has to suspend
  threads that may be inside native code.
- Whether the baseline's startup is flaky. On the first `dotnet watch` run Amazons exited
  right after starting, before any edit. The cause is unknown; it may have been closed by hand.

## Known limits

- Hot Reload applies to method bodies in the scene, the drawing logic, colors and layout.
- Adding or reordering fields in a command struct, or adding a `RenderOp` member, is a
  rude edit and needs a restart.
- `OpCode` values are cached per type, so changing one also needs a restart.
- `GameLoop.Run` stays on the stack for the app's lifetime, so edits to it only apply after
  a restart. `Tick` and the scene are re-entered every tick.
