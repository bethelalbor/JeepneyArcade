using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JeepneyArcade.EditorTools
{
    public static class CityColliderBuilder
    {
        public sealed class Result
        {
            public int created, existing, ignored;
            public readonly List<GameObject> manual = new List<GameObject>();
            public string Summary => $"Created {created} boxes. Skipped {existing} existing/unsafe objects and {ignored} unrelated meshes. {manual.Count} need manual correction. Undo reverses this operation.";
        }

        [MenuItem("Tools/City Map/Generate Colliders on Selection")]
        public static void GenerateSelection()
        {
            var result = Generate(Selection.gameObjects);
            Debug.Log("City colliders: " + result.Summary);
        }

        [MenuItem("Tools/City Map/Generate Colliders on Selection", true)]
        static bool CanGenerate() => !EditorApplication.isPlayingOrWillChangePlaymode &&
            Selection.gameObjects.Any(g => g && !EditorUtility.IsPersistent(g));

        public static Result Generate(GameObject[] roots)
        {
            var result = new Result();
            if (EditorApplication.isPlayingOrWillChangePlaymode) return result;
            var meshes = roots.Where(g => g && !EditorUtility.IsPersistent(g) && g.scene.IsValid())
                .SelectMany(g => g.GetComponentsInChildren<MeshFilter>(true)).Distinct().ToArray();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Generate city colliders");
            try
            {
                foreach (var mf in meshes)
                {
                    if (!mf.sharedMesh || !mf.GetComponent<MeshRenderer>()) { result.ignored++; continue; }
                    int kind = Kind(mf.transform);
                    if (kind == 0) { result.ignored++; continue; }
                    if (kind == 1 && !IsBarrier(mf.transform))
                    {
                        GenerateBuilding(mf, result);
                        continue;
                    }
                    if (Blocked(mf.gameObject, kind == 1)) { result.existing++; continue; }
                    if (!TryFit(mf, kind, out var bounds))
                    { result.manual.Add(mf.gameObject); continue; }
                    var box = Undo.AddComponent<BoxCollider>(mf.gameObject);
                    Undo.RecordObject(box, "Fit city collider");
                    box.center = bounds.center;
                    box.size = bounds.size;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(box);
                    EditorSceneManager.MarkSceneDirty(mf.gameObject.scene);
                    result.created++;
                }
            }
            catch
            {
                // A failed batch must not leave a partially generated map.
                Undo.RevertAllDownToGroup(group);
                throw;
            }
            finally { Undo.CollapseUndoOperations(group); SceneView.RepaintAll(); }
            return result;
        }

        static bool IsBarrier(Transform transform)
        {
            for (var t = transform; t; t = t.parent)
            {
                string n = t.name.ToLowerInvariant();
                if (n.Contains("barrier") || n.Contains("fence")) return true;
            }
            return false;
        }

        static void GenerateBuilding(MeshFilter mf, Result result)
        {
            var go = mf.gameObject;
            if (go.GetComponentInParent<Rigidbody>(true) || go.GetComponentInChildren<Rigidbody>(true) ||
                go.GetComponentInParent<ArticulationBody>(true) || go.GetComponentInChildren<ArticulationBody>(true) ||
                go.GetComponentInParent<CharacterController>(true)) { result.existing++; return; }
            for (var t = go.transform.parent; t; t = t.parent)
                if (t.GetComponent<Collider>()) { result.existing++; return; }
            var owned = go.GetComponentsInChildren<GeneratedBuildingColliders>(true).Where(m => m.source == mf).ToArray();
            // Migrate only plain direct boxes matching the previous generator's fit.
            // Other authored collision shapes are preserved, not stacked with new boxes.
            TryFit(mf, 1, out var legacyBounds);
            var legacy = go.GetComponents<BoxCollider>().Where(c => !c.isTrigger && !c.sharedMaterial &&
                (c.center - legacyBounds.center).sqrMagnitude < .000001f &&
                (c.size - legacyBounds.size).sqrMagnitude < .000001f).ToArray();
            var other = go.GetComponentsInChildren<Collider>(true).Where(c => !legacy.Contains(c) &&
                !owned.Any(m => c.transform.IsChildOf(m.transform))).ToArray();
            if (other.Length > 0) { result.existing++; return; }
            var scale = go.transform.lossyScale;
            if (!Finite(scale) || scale.x == 0 || scale.y == 0 || scale.z == 0) { result.manual.Add(go); return; }
            var fits = BuildingColliderFitter.FitMesh(mf);
            if (fits.Count == 0) { result.manual.Add(go); return; }
            foreach (var marker in owned) Undo.DestroyObjectImmediate(marker.gameObject);
            foreach (var collider in legacy) Undo.DestroyObjectImmediate(collider);
            var root = new GameObject("Building Colliders");
            Undo.RegisterCreatedObjectUndo(root, "Create building colliders");
            root.transform.SetParent(mf.transform, false);
            root.layer = go.layer;
            var ownership = Undo.AddComponent<GeneratedBuildingColliders>(root);
            ownership.source = mf;
            for (int i = 0; i < fits.Count; i++)
            {
                var fit = fits[i];
                var child = new GameObject("Box " + (i + 1));
                Undo.RegisterCreatedObjectUndo(child, "Create building box");
                child.transform.SetParent(root.transform, false);
                child.transform.SetPositionAndRotation(fit.center, fit.rotation);
                child.layer = go.layer;
                // Center at the child origin: PhysX's TRS cannot shear, so use its
                // actual scale to express the measured world dimensions.
                var s = child.transform.lossyScale;
                var box = Undo.AddComponent<BoxCollider>(child);
                box.center = Vector3.zero;
                box.size = new Vector3(fit.size.x / Mathf.Abs(s.x), fit.size.y / Mathf.Abs(s.y), fit.size.z / Mathf.Abs(s.z));
                result.created++;
            }
            EditorSceneManager.MarkSceneDirty(go.scene);
        }

        // 1: building/barrier, 2: lamp pole, 3: tree trunk.
        public static int Kind(Transform transform)
        {
            int kind = 0;
            // FBX children inherit their role from named parents/collections.
            for (var t = transform; t; t = t.parent)
            {
                string n = t.name.ToLowerInvariant();
                if (n.Contains("ads") || n.Contains("jeepney") || n.Contains("vehicle") ||
                    n.Contains("wheel") || n.Contains("bush")) return 0;
                int candidate = Kind(t.name);
                if (candidate == 2 || candidate == 3) return candidate;
                if (candidate == 1) kind = 1;
            }
            return kind;
        }

        public static int Kind(string name)
        {
            string n = name.ToLowerInvariant();
            if (n.Contains("ads") || n.Contains("jeepney") || n.Contains("vehicle") ||
                n.Contains("wheel") || n.Contains("bush")) return 0;
            if (n.Contains("streetlight") || n.Contains("street_light") || n.Contains("lamp_post")) return 2;
            if (n.Contains("tree") || n.Contains("trunk")) return 3;
            return n.Contains("mesh") || n.Contains("building") || n.Contains("market") || n.Contains("mall") || n.Contains("barrier") ||
                n.Contains("fence") || n.Contains("shed") ? 1 : 0;
        }

        public static bool Blocked(GameObject go, bool building = false)
        {
            if (go.GetComponentInParent<Rigidbody>(true) || go.GetComponentInChildren<Rigidbody>(true) ||
                go.GetComponentInParent<ArticulationBody>(true) || go.GetComponentInChildren<ArticulationBody>(true) ||
                go.GetComponentInParent<CharacterController>(true)) return true;
            if (go.GetComponentInChildren<Collider>(true)) return true;
            for (var t = go.transform.parent; t; t = t.parent)
                if (t.GetComponent<Collider>()) return true;
            Vector3 s = go.transform.lossyScale;
            if (building) return !Finite(s) || s.x == 0 || s.y == 0 || s.z == 0;
            if (s.x <= 0 || s.y <= 0 || s.z <= 0) return true;
            var m = go.transform.localToWorldMatrix;
            var x = ((Vector3)m.GetColumn(0)).normalized;
            var y = ((Vector3)m.GetColumn(1)).normalized;
            var z = ((Vector3)m.GetColumn(2)).normalized;
            return Mathf.Abs(Vector3.Dot(x,y)) > .001f || Mathf.Abs(Vector3.Dot(x,z)) > .001f ||
                Mathf.Abs(Vector3.Dot(y,z)) > .001f;
        }

        public static bool TryFit(MeshFilter mf, int kind, out Bounds fitted)
        {
            fitted = mf.sharedMesh.bounds;
            var b = fitted;
            if (kind == 1)
            {
                // Fit on the mesh object, retaining its imported pivot and local
                // axes. No closed-shell or arbitrary minimum-dimension checks.
                if (mf.sharedMesh.vertexCount == 0 || !Finite(b.center) || !Finite(b.size) ||
                    b.size.x <= 0 || b.size.y <= 0 || b.size.z <= 0) return false;
                var t = mf.transform;
                var s = t.lossyScale;
                if (!Finite(s) || s.x == 0 || s.y == 0 || s.z == 0) return false;
                var matrix = t.localToWorldMatrix;
                var axisX = ((Vector3)matrix.GetColumn(0)).normalized;
                var axisY = ((Vector3)matrix.GetColumn(1)).normalized;
                var axisZ = ((Vector3)matrix.GetColumn(2)).normalized;
                if (Mathf.Abs(Vector3.Dot(axisX, axisY)) < .00001f &&
                    Mathf.Abs(Vector3.Dot(axisX, axisZ)) < .00001f &&
                    Mathf.Abs(Vector3.Dot(axisY, axisZ)) < .00001f) return true;
                // PhysX boxes cannot shear: enclose transformed mesh half-axes
                // in the collider's rotation frame for nonuniform nested scales.
                // Its center also uses the physics TRS, not the sheared matrix.
                fitted.center = Matrix4x4.TRS(t.position, t.rotation, s).inverse
                    .MultiplyPoint3x4(t.TransformPoint(b.center));
                var frame = Matrix4x4.Rotate(Quaternion.Inverse(t.rotation)) * t.localToWorldMatrix;
                var x = frame.MultiplyVector(Vector3.right * b.extents.x);
                var y = frame.MultiplyVector(Vector3.up * b.extents.y);
                var z = frame.MultiplyVector(Vector3.forward * b.extents.z);
                fitted.size = new Vector3(
                    (Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x)) * 2 / Mathf.Abs(s.x),
                    (Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y)) * 2 / Mathf.Abs(s.y),
                    (Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z)) * 2 / Mathf.Abs(s.z));
                return Finite(fitted.center) && Finite(fitted.size);
            }
            if (!Finite(b.center) || !Finite(b.size) || Mathf.Min(b.size.x,b.size.y,b.size.z) < .001f) return false;
            // Editor-only mesh data access works without changing the importer's Read/Write setting.
            using (var data = MeshUtility.AcquireReadOnlyMeshData(mf.sharedMesh))
            using (var vertices = new NativeArray<Vector3>(data[0].vertexCount, Allocator.Temp))
            {
                data[0].GetVertices(vertices);
                if (kind != 2 && kind != 3) return false;
                // Blender trees in this map are Z-up, while the lamp meshes are Y-up.
                var directions = new[] { mf.transform.right, mf.transform.up, mf.transform.forward };
                int vertical = 0;
                for (int axis=1;axis<3;axis++)
                    if (Mathf.Abs(directions[axis].y)>Mathf.Abs(directions[vertical].y)) vertical=axis;
                if (Mathf.Abs(directions[vertical].y)<.98f) return false;
                float sign = Mathf.Sign(directions[vertical].y);
                int horizontal = (vertical+1)%3, depth = (vertical+2)%3;
                var orientedVertices = vertices;
                for (int i=0;i<vertices.Length;i++)
                {
                    var v=vertices[i]; orientedVertices[i]=new Vector3(v[horizontal],v[vertical]*sign,v[depth]);
                }
                b=new Bounds(new Vector3(b.center[horizontal],b.center[vertical]*sign,b.center[depth]),
                    new Vector3(b.size[horizontal],b.size[vertical],b.size[depth]));
                var originalScale=mf.transform.lossyScale;
                var scale=new Vector3(originalScale[horizontal],originalScale[vertical],originalScale[depth]);
                bool tree = kind == 3;
                float height = tree ? Mathf.Min(b.size.y * .25f, 2.5f / scale.y) : b.size.y;
                var shaft = new Bounds();
                int hits = 0;
                // Slice triangles, not just vertices: simple poles/trunks may have only two vertex rings.
                foreach (float fraction in tree ? new[] { .15f, .5f, .9f } : new[] { .15f, .35f, .55f })
                {
                    float level = b.min.y + (tree ? height : b.size.y) * fraction;
                    if (!Slice(data[0], vertices, level, out var section)) return false;
                    if (hits++ == 0) shaft = section; else shaft.Encapsulate(section);
                }
                float maxWidth = tree ? 2f : 1f;
                if (shaft.size.x < .005f || shaft.size.z < .005f ||
                    shaft.size.x * scale.x > maxWidth || shaft.size.z * scale.z > maxWidth ||
                    Mathf.Max(shaft.size.x,shaft.size.z) > b.size.y * (tree ? .25f : .15f)) return false;
                Vector3 center=Vector3.zero, size=Vector3.zero;
                center[horizontal]=shaft.center.x; center[vertical]=(b.min.y+height*.5f)*sign; center[depth]=shaft.center.z;
                size[horizontal]=shaft.size.x; size[vertical]=height; size[depth]=shaft.size.z;
                fitted = new Bounds(center,size);
                return true;
            }
        }

        static bool Slice(Mesh.MeshData data, NativeArray<Vector3> vertices, float y, out Bounds section)
        {
            section = new Bounds(); int count=0;
            for(int sub=0;sub<data.subMeshCount;sub++)
            {
                var desc=data.GetSubMesh(sub);
                if(desc.topology!=MeshTopology.Triangles) return false;
                using(var indices=new NativeArray<int>(desc.indexCount,Allocator.Temp))
                {
                    data.GetIndices(indices,sub,true);
                    for(int i=0;i+2<indices.Length;i+=3)
                        for(int edge=0;edge<3;edge++)
                        {
                            var a=vertices[indices[i+edge]]; var b=vertices[indices[i+(edge+1)%3]];
                            if((a.y<=y && b.y>y)||(b.y<=y && a.y>y))
                            {
                                var point=Vector3.Lerp(a,b,(y-a.y)/(b.y-a.y));
                                if(count++==0) section=new Bounds(point,Vector3.zero); else section.Encapsulate(point);
                            }
                        }
                }
            }
            return count>=4;
        }

        static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}


