# Bulk city colliders

Select objects or their collection parents, then use **Tools > City Map > Generate Colliders on Selection**, or the existing Collider Builder window. Rerun the same command to regenerate building colliders. Generation is editor-only and one Undo step.

Buildings use actual mesh vertices transformed through the complete FBX hierarchy. A minimum-area horizontal footprint determines rotation independently of the mesh pivot. Each building gets a `Building Colliders` child with independently positioned and rotated box children. The visible mesh, transform, materials and importer remain unchanged. No Rigidbodies are added.

Rectangular buildings normally use one box. Irregular footprints are divided into at most twelve disjoint boxes to reduce empty space and preserve courtyards where practical. Footprints use projected mesh triangles, vertex-aligned cells, and a bounded 64-by-64 grid for dense geometry. These are simplified collision volumes: curved/diagonal boundaries and very complex footprints remain approximate, and each box spans the building's full measured height.

GeneratedBuildingColliders stores ownership metadata only; it has no runtime behaviour. Regeneration replaces only this building's generated children. Direct plain Box Colliders matching the previous generator's bounds fit are migrated automatically. Other authored colliders, triggers, material-bearing colliders and colliders on ancestors are preserved; the building is skipped rather than given overlapping collision. Overlapping selections are deduplicated.

Building recognition includes Mid_Mesh, building, market and mall names, plus inherited building parent/collection names for generic children. Barriers and fences keep their existing direct box fitting. Streetlights keep their shaft boxes, and trees keep their base-trunk fitting. Existing pole/trunk/barrier colliders are untouched. Dynamic/character hierarchies and empty or flat meshes are skipped.

Save the scene after generation. FBX instances stay linked to their sources; generated children are scene/prefab overrides. Old profile assets remain compatible but are not used.
