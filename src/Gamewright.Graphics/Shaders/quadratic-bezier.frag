#version 330 core

in vec2 fragmentPosition;
in vec2 fragmentStart;
in vec2 fragmentControl;
in vec2 fragmentEnd;
in float fragmentRadius;
in vec4 fragmentColor;

out vec4 outputColor;

float lengthSquared(vec2 v)
{
    return dot(v, v);
}

float segmentDistance(vec2 position, vec2 a, vec2 b)
{
    vec2 ab = b - a;
    float t = clamp(dot(position - a, ab) / max(dot(ab, ab), 1e-12), 0.0, 1.0);
    return length(position - a - ab * t);
}

// Exact distance to the quadratic Bézier curve A-B-C (control point B), by solving the cubic
// for the closest curve parameter in closed form. After Inigo Quilez, "2D distance functions".
float bezierDistance(vec2 position, vec2 A, vec2 B, vec2 C)
{
    vec2 a = B - A;
    vec2 b = A - 2.0 * B + C;

    // a straight piece has no quadratic term, and the cubic below would divide by ~0
    if (lengthSquared(b) < 1e-4 * lengthSquared(C - A))
    {
        return segmentDistance(position, A, C);
    }

    vec2 c = a * 2.0;
    vec2 d = A - position;
    float kk = 1.0 / dot(b, b);
    float kx = kk * dot(a, b);
    float ky = kk * (2.0 * dot(a, a) + dot(d, b)) / 3.0;
    float kz = kk * dot(d, a);
    float p = ky - kx * kx;
    float q = kx * (2.0 * kx * kx - 3.0 * ky) + kz;
    float h = q * q + 4.0 * p * p * p;

    float result;
    if (h >= 0.0)
    {
        // one real root
        h = sqrt(h);
        vec2 x = (vec2(h, -h) - q) / 2.0;
        vec2 uv = sign(x) * pow(abs(x), vec2(1.0 / 3.0));
        float t = clamp(uv.x + uv.y - kx, 0.0, 1.0);
        result = lengthSquared(d + (c + b * t) * t);
    }
    else
    {
        // three real roots; the third one is never the closest
        float z = sqrt(-p);
        float v = acos(q / (p * z * 2.0)) / 3.0;
        float m = cos(v);
        float n = sin(v) * 1.732050808;
        vec2 t = clamp(vec2(m + m, -n - m) * z - kx, 0.0, 1.0);
        result = min(
            lengthSquared(d + (c + b * t.x) * t.x),
            lengthSquared(d + (c + b * t.y) * t.y));
    }

    return sqrt(result);
}

void main()
{
    float distanceValue = bezierDistance(fragmentPosition, fragmentStart, fragmentControl, fragmentEnd) - fragmentRadius;
    vec4 color = fragmentColor;
    color.a *= fillAlphaFromDistance(distanceValue);
    outputColor = color;
}
