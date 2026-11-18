using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace JeepneyArcade.EditorTools
{
    // Work in world geometry, not the FBX pivot or its nominal rotation.
    public static class BuildingColliderFitter
    {
        public struct Fit { public Vector3 center, size; public Quaternion rotation; }
        struct Triangle { public Vector2 a, b, c; }
        struct Region { public int x0, x1, z0, z1; }

        public static List<Fit> FitMesh(MeshFilter mf)
        {
            var result = new List<Fit>();
            using (var data = MeshUtility.AcquireReadOnlyMeshData(mf.sharedMesh))
            using (var raw = new NativeArray<Vector3>(data[0].vertexCount, Allocator.Temp))
            {
                data[0].GetVertices(raw);
                if (raw.Length < 3) return result;
                var world = new Vector3[raw.Length];
                // Subtract a nearby origin to retain precision for offset FBX vertices.
                Vector3 origin = mf.transform.TransformPoint(mf.sharedMesh.bounds.center);
                for (int i = 0; i < raw.Length; i++)
                {
                    world[i] = mf.transform.TransformPoint(raw[i]) - origin;
                    if (!float.IsFinite(world[i].x) || !float.IsFinite(world[i].y) || !float.IsFinite(world[i].z)) return result;
                }
                var hull = Hull(world.Select(v => new Vector2(v.x, v.z)).ToList());
                if (hull.Count < 3) return result;
                Vector2 axis = Vector2.right;
                float bestArea = float.PositiveInfinity;
                foreach (var edge in Enumerable.Range(0, hull.Count))
                {
                    var u = (hull[(edge + 1) % hull.Count] - hull[edge]).normalized;
                    var v = new Vector2(-u.y, u.x);
                    float area = (hull.Max(p => Vector2.Dot(p, u)) - hull.Min(p => Vector2.Dot(p, u))) *
                        (hull.Max(p => Vector2.Dot(p, v)) - hull.Min(p => Vector2.Dot(p, v)));
                    if (area < bestArea) { bestArea = area; axis = u; }
                }
                var perpendicular = new Vector2(-axis.y, axis.x);
                var points = world.Select(p => new Vector2(Vector2.Dot(new Vector2(p.x, p.z), axis),
                    Vector2.Dot(new Vector2(p.x, p.z), perpendicular))).ToArray();
                float low = world.Min(p => p.y), high = world.Max(p => p.y);
                if (high <= low || bestArea <= 0) return result;
                var triangles = new List<Triangle>();
                for (int sub = 0; sub < data[0].subMeshCount; sub++)
                {
                    var desc = data[0].GetSubMesh(sub);
                    if (desc.topology != MeshTopology.Triangles) continue;
                    using (var indices = new NativeArray<int>(desc.indexCount, Allocator.Temp))
                    {
                        data[0].GetIndices(indices, sub, true);
                        for (int i = 0; i + 2 < indices.Length; i += 3)
                        {
                            var t = new Triangle { a = points[indices[i]], b = points[indices[i + 1]], c = points[indices[i + 2]] };
                            if (Mathf.Abs(Cross(t.b - t.a, t.c - t.a)) > 1e-8f) triangles.Add(t);
                        }
                    }
                }
                // Vertex-aligned cells preserve rectangular wings/courtyards exactly.
                // Dense decorative meshes use a bounded grid instead.
                var xs = Coordinates(points.Select(p => p.x));
                var zs = Coordinates(points.Select(p => p.y));
                var occupied = new bool[xs.Length - 1, zs.Length - 1];
                for (int x = 0; x < xs.Length - 1; x++)
                    for (int z = 0; z < zs.Length - 1; z++)
                    {
                        var p = new Vector2((xs[x] + xs[x + 1]) * .5f, (zs[z] + zs[z + 1]) * .5f);
                        occupied[x, z] = triangles.Any(t => Inside(p, t));
                    }
                var regions = new List<Region>();
                var all = new Region { x1 = xs.Length - 1, z1 = zs.Length - 1 };
                if (!Trim(ref all, occupied)) return result;
                regions.Add(all);
                // At most twelve non-overlapping boxes; greedily remove the largest
                // empty footprint area at each split, never splitting through cells.
                while (regions.Count < 12)
                {
                    float gain = bestArea * .002f;
                    int chosen = -1;
                    Region left = default, right = default;
                    for (int i = 0; i < regions.Count; i++)
                    {
                        var r = regions[i];
                        for (int direction = 0; direction < 2; direction++)
                            for (int cut = (direction == 0 ? r.x0 : r.z0) + 1; cut < (direction == 0 ? r.x1 : r.z1); cut++)
                            {
                                var a = r; var b = r;
                                if (direction == 0) { a.x1 = cut; b.x0 = cut; }
                                else { a.z1 = cut; b.z0 = cut; }
                                if (!Trim(ref a, occupied) || !Trim(ref b, occupied)) continue;
                                float saving = Area(r, xs, zs) - Area(a, xs, zs) - Area(b, xs, zs);
                                if (saving > gain) { gain = saving; chosen = i; left = a; right = b; }
                            }
                    }
                    // Closed courtyards may need a cut that exposes an empty
                    // interior before any bounding-area reduction is possible.
                    if (chosen < 0)
                        for (int i = 0; i < regions.Count && chosen < 0; i++)
                        {
                            var r = regions[i];
                            if (Filled(r, occupied)) continue;
                            for (int x = r.x0; x < r.x1 && chosen < 0; x++)
                                for (int z = r.z0; z < r.z1 && chosen < 0; z++)
                                {
                                    if (occupied[x, z]) continue;
                                    var a = r; var b = r;
                                    if (x > r.x0) { a.x1 = x; b.x0 = x; }
                                    else if (z > r.z0) { a.z1 = z; b.z0 = z; }
                                    else if (x + 1 < r.x1) { a.x1 = x + 1; b.x0 = x + 1; }
                                    else continue;
                                    if (!Trim(ref a, occupied) || !Trim(ref b, occupied)) continue;
                                    chosen = i; left = a; right = b;
                                }
                        }
                    if (chosen < 0) break;
                    regions[chosen] = left; regions.Add(right);
                }
                var rotation = Quaternion.LookRotation(new Vector3(perpendicular.x, 0, perpendicular.y), Vector3.up);
                foreach (var r in regions)
                {
                    var center = axis * ((xs[r.x0] + xs[r.x1]) * .5f) + perpendicular * ((zs[r.z0] + zs[r.z1]) * .5f);
                    result.Add(new Fit { center = origin + new Vector3(center.x, (low + high) * .5f, center.y),
                        size = new Vector3(xs[r.x1] - xs[r.x0], high - low, zs[r.z1] - zs[r.z0]), rotation = rotation });
                }
            }
            return result;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        static bool Inside(Vector2 p, Triangle t)
        {
            float a = Cross(t.b - t.a, p - t.a), b = Cross(t.c - t.b, p - t.b), c = Cross(t.a - t.c, p - t.c);
            return (a >= -1e-6f && b >= -1e-6f && c >= -1e-6f) || (a <= 1e-6f && b <= 1e-6f && c <= 1e-6f);
        }
        static List<Vector2> Hull(List<Vector2> points)
        {
            points = points.Distinct().OrderBy(p => p.x).ThenBy(p => p.y).ToList();
            var hull = new List<Vector2>();
            foreach (var p in points) { while (hull.Count >= 2 && Cross(hull[hull.Count - 1] - hull[hull.Count - 2], p - hull[hull.Count - 1]) <= 0) hull.RemoveAt(hull.Count - 1); hull.Add(p); }
            int lower = hull.Count;
            for (int i = points.Count - 2; i >= 0; i--) { var p = points[i]; while (hull.Count > lower && Cross(hull[hull.Count - 1] - hull[hull.Count - 2], p - hull[hull.Count - 1]) <= 0) hull.RemoveAt(hull.Count - 1); hull.Add(p); }
            if (hull.Count > 1) hull.RemoveAt(hull.Count - 1);
            return hull;
        }
        static float[] Coordinates(IEnumerable<float> values)
        {
            var sorted = values.OrderBy(v => v).ToArray();
            float tolerance = Mathf.Max((sorted.Last() - sorted[0]) * .0001f, .00001f);
            var list = new List<float> { sorted[0] };
            foreach (float v in sorted) if (v - list.Last() > tolerance) list.Add(v);
            list[list.Count - 1] = sorted.Last();
            if (list.Count > 65) return Enumerable.Range(0, 65).Select(i => Mathf.Lerp(sorted[0], sorted.Last(), i / 64f)).ToArray();
            return list.ToArray();
        }
        static float Area(Region r, float[] xs, float[] zs) => (xs[r.x1] - xs[r.x0]) * (zs[r.z1] - zs[r.z0]);
        static bool Filled(Region r, bool[,] cells)
        {
            for (int x = r.x0; x < r.x1; x++) for (int z = r.z0; z < r.z1; z++) if (!cells[x, z]) return false;
            return true;
        }
        static bool Trim(ref Region r, bool[,] cells)
        {
            int x0 = r.x1, x1 = r.x0, z0 = r.z1, z1 = r.z0;
            for (int x = r.x0; x < r.x1; x++) for (int z = r.z0; z < r.z1; z++) if (cells[x, z])
                { x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x + 1); z0 = Mathf.Min(z0, z); z1 = Mathf.Max(z1, z + 1); }
            r = new Region { x0 = x0, x1 = x1, z0 = z0, z1 = z1 };
            return x1 > x0 && z1 > z0;
        }
    }
}

