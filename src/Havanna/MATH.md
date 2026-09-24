# HEX math

Here's the derivation, built up from scratch.
Setup
Place unit cubes at integer lattice points in 3D. The "cube diagonal" direction (the one you're looking along) is

$$
\mathbf{n} = \tfrac{1}{\sqrt{3}}(1,1,1)
$$

You want to project onto the plane through the origin with normal n. For that you need two orthonormal vectors spanning that plane. A symmetric choice:

$$
\mathbf{u} = \tfrac{1}{\sqrt{2}}(1,-1,0), \qquad \mathbf{v} = \tfrac{1}{\sqrt{6}}(1,1,-2)
$$

Quick checks: \(u·n = 0, v·n = 0, u·v = 0, |u|=|v|=1.\)

The projection
For any point P = (x, y, z), the 2D coordinates in the plane are

$$u = \mathbf{P}\!\cdot\!\mathbf{u} = \frac{x-y}{\sqrt{2}}, \qquad v = \mathbf{P}\!\cdot\!\mathbf{v} = \frac{x+y-2z}{\sqrt{6}}$$

Apply this to the three basis vectors:

| 3D vector | → projected (u, v) |
| --------- | ------------------ |
| (1,0,0)   | (1/√2, 1/√6)       |
| (0,1,0)   | (−1/√2, 1/√6)      |
| (0,0,1)   | (0, −2/√6)         |

Each projected axis has length √(2/3) (since the component along n is 1/√3, so the perpendicular part is √(1−1/3) = √(2/3)), and the dot product between any two of them is −1/3 of that, giving 120° between any pair. That's exactly the three directions of a triangular/hexagonal lattice.

## Why a hexagon per cube

Project the 8 vertices of the unit cube. The two vertices (0,0,0) and (1,1,1) both lie on the diagonal n, so they collapse to the origin. The remaining 6 vertices map to 6 points at equal distance √(2/3) from the origin, equally spaced at 60° — a regular hexagon. That's the image of one cube, and it's where the three-rhombi / "impossible stacked cubes" illusion comes from.

## Cleaning up — the simple formula

Rescale so the hexagon side length is 1
(i.e. multiply (u, v) by √(3/2)):

$$
\boxed{
    X = \tfrac{\sqrt{3}}{2}\,(i-j), \quad
    Y = \tfrac{1}{2}\,(i+j-2k)
}
$$

​
This is the projection of the integer lattice point (i, j, k) into the 2D hex plane, with unit hexagon edge.
What this formula buys you
The three in-plane lattice vectors — images of the 3D basis — are

$$
    \mathbf{a}_1=(\tfrac{\sqrt3}{2},\tfrac12)\quad
    \mathbf{a}_2=(-\tfrac{\sqrt3}{2},\tfrac12),\quad
    \mathbf{a}_3=(0,-1)
$$

each of unit length, mutually 120° apart, and satisfying a₁ + a₂ + a₃ = 0 (because (1,1,1) ↦ 0). So:

| Moving in 3D | step by \_ in 2D |
| ------------ | ---------------- |
| +1 in x      | a₁               |
| +1 in y      | a₂               |
| +1 in z      | a₃               |

Any integer (i, j, k) maps to i·a₁ + j·a₂ + k·a₃, and because a₁+a₂+a₃ = 0, the triples (i, j, k), (i+1, j+1, k+1), … all land on the same hex center — which is exactly why the stack reads as a 2D pattern.

## How to draw the pattern

For a corner of stacked cubes occupying 0 ≤ i, j, k ≤ N, iterate (i, j, k), compute (X, Y), and for each one draw a unit hexagon. Each hexagon is tiled by three unit rhombi (the three "visible faces" of the cube), whose centers are at the three face centers of the cube projected the same way — giving you the classic triangular/hex pattern.
If you'd rather think in "axial hex coordinates" (q, r): set q = i − k and r = j − k (this quotients out the diagonal invariant). Then
X=32(q−r),Y=12(q+r),X = \tfrac{\sqrt{3}}{2}(q - r), \qquad Y = \tfrac{1}{2}(q + r),X=23​​(q−r),Y=21​(q+r),
which is a standard axial→cartesian hex-grid formula with side 1.
Want me to turn this into a small HTML/SVG artifact that draws the pattern for a chosen N?

Want me to turn this into a small HTML/SVG artifact that draws the pattern for a chosen N?
