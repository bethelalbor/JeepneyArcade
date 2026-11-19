using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runs actual collision simulations in a separate physics scene without editing the map.
[InitializeOnLoad]
public static class VehicleCrashValidation
{
    private const string PendingKey = "Jeepney.VehicleCrashValidation.Pending";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    static VehicleCrashValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
            {
                SessionState.SetBool(PendingKey, false);
                EditorApplication.delayCall += Run;
            }
        };
    }

    [MenuItem("Tools/Jeepney/Validate Crash Response")]
    public static void Begin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run crash validation from Edit mode.");
        SessionState.SetBool(PendingKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void Run()
    {
        Scene scene = SceneManager.CreateScene("Crash validation", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        try
        {
            ValidateImpact(scene, new Vector3(0.65f, 1f, 4f), 10f, Vector3.back, "front corner pole");
            ValidateImpact(scene, new Vector3(-0.65f, 1f, -4f), -10f, Vector3.forward, "rear corner pole");
            ValidateSide(scene, -1f);
            ValidateSide(scene, 1f);
            ValidateSteeringAndRoad(scene);
            Debug.Log("CRASH VALIDATION PASSED: front/rear pole impacts, both sides, opposite-side lift, settling, sustained contact, reversing, steering and road filtering.");
        }
        catch (Exception exception)
        {
            Debug.LogError("CRASH VALIDATION FAILED: " + exception);
        }
        finally
        {
            SceneManager.UnloadSceneAsync(scene);
            EditorApplication.isPlaying = false;
        }
    }

    private static PlayerMovement CreateVehicle(Scene scene)
    {
        var root = new GameObject("Validation vehicle");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.position = new Vector3(0f, 0.55f, 0f);
        root.AddComponent<BoxCollider>().size = new Vector3(2f, 1f, 4f);
        Rigidbody body = root.AddComponent<Rigidbody>();
        body.mass = 1500f;
        body.useGravity = false;
        var visual = new GameObject("Visual").transform;
        visual.SetParent(root.transform, false);
        PlayerMovement movement = root.AddComponent<PlayerMovement>();
        movement.enabled = false;
        Set(movement, "crashVisual", visual);
        movement.maxSpeed = 20f;
        movement.minimumSteeringSpeed = 1f;
        Call(movement, "Start");
        return movement;
    }

    private static GameObject CreatePole(Scene scene, Vector3 position)
    {
        var pole = new GameObject("Validation pole");
        SceneManager.MoveGameObjectToScene(pole, scene);
        pole.transform.position = position;
        var collider = pole.AddComponent<CapsuleCollider>();
        collider.radius = 0.25f;
        collider.height = 4f;
        return pole;
    }

    private static void ValidateImpact(Scene scene, Vector3 polePosition, float speed, Vector3 liftedSide, string label)
    {
        PlayerMovement movement = CreateVehicle(scene);
        GameObject pole = CreatePole(scene, polePosition);
        Rigidbody body = movement.GetComponent<Rigidbody>();
        PhysicsScene physics = scene.GetPhysicsScene();
        bool hit = false;
        for (int i = 0; i < 80; i++)
        {
            Set(movement, "currentSpeed", speed);
            Call(movement, "FixedUpdate");
            physics.Simulate(0.02f);
            if (!float.IsNegativeInfinity(Get<float>(movement, "crashStartedAt")))
            {
                hit = true;
                break;
            }
        }
        Require(hit, label + " did not trigger crash feedback; position=" + body.position +
            ", visualReady=" + Get<bool>(movement, "visualReady"));
        Require(Quaternion.Angle(body.rotation, Quaternion.identity) < 0.05f, label + " changed heading");
        CheckVisual(movement, liftedSide, label);
        Vector3 stoppedAt = body.position;
        for (int i = 0; i < 40; i++)
        {
            // Simulate held throttle rebuilding speed between fixed updates.
            Set(movement, "currentSpeed", Mathf.Sign(speed) * 0.1f);
            Call(movement, "FixedUpdate");
            physics.Simulate(0.02f);
        }
        Require(Quaternion.Angle(body.rotation, Quaternion.identity) < 0.05f, label + " spun during sustained contact");
        Require(Mathf.Abs(body.position.z - stoppedAt.z) < 0.15f, label + " kept driving into pole");
        float beforeReverse = body.position.z;
        Set(movement, "currentSpeed", -Mathf.Sign(speed) * 4f);
        for (int i = 0; i < 20; i++)
        {
            Call(movement, "FixedUpdate");
            physics.Simulate(0.02f);
        }
        Require((body.position.z - beforeReverse) * Mathf.Sign(speed) < -0.5f, label + " cannot reverse away");
        UnityEngine.Object.DestroyImmediate(movement.gameObject);
        UnityEngine.Object.DestroyImmediate(pole);
    }

    private static void ValidateSide(Scene scene, float side)
    {
        PlayerMovement movement = CreateVehicle(scene);
        GameObject pole = CreatePole(scene, new Vector3(side * 2f, 1f, 0f));
        Rigidbody body = movement.GetComponent<Rigidbody>();
        body.linearVelocity = Vector3.right * side * 8f;
        for (int i = 0; i < 20 && float.IsNegativeInfinity(Get<float>(movement, "crashStartedAt")); i++)
            scene.GetPhysicsScene().Simulate(0.02f);
        Require(!float.IsNegativeInfinity(Get<float>(movement, "crashStartedAt")), "Side impact missing");
        CheckVisual(movement, Vector3.left * side, "side impact");
        Require(Quaternion.Angle(body.rotation, Quaternion.identity) < 0.05f, "Side impact spun vehicle");
        UnityEngine.Object.DestroyImmediate(movement.gameObject);
        UnityEngine.Object.DestroyImmediate(pole);
    }

    private static void CheckVisual(PlayerMovement movement, Vector3 liftedSide, string label)
    {
        Transform visual = Get<Transform>(movement, "crashVisual");
        Vector3 restPosition = visual.localPosition;
        Quaternion restRotation = visual.localRotation;
        Set(movement, "crashStartedAt", Time.time - 0.1f);
        Call(movement, "LateUpdate");
        Vector3 opposite = visual.localPosition + visual.localRotation * liftedSide;
        Require(opposite.y > 0.005f, label + " did not lift the opposite side");
        Require(Quaternion.Angle(visual.localRotation, restRotation) <= 4.01f, label + " exceeded tilt limit");
        Set(movement, "crashStartedAt", Time.time - 0.6f);
        Call(movement, "LateUpdate");
        Require(Vector3.Distance(visual.localPosition, restPosition) < 0.0001f &&
            Quaternion.Angle(visual.localRotation, restRotation) < 0.001f, label + " did not settle");
    }

    private static void ValidateSteeringAndRoad(Scene scene)
    {
        PlayerMovement movement = CreateVehicle(scene);
        Rigidbody body = movement.GetComponent<Rigidbody>();
        Set(movement, "currentSpeed", 10f);
        Set(movement, "turnInput", 1f);
        body.linearVelocity = Vector3.forward * 10f;
        for (int i = 0; i < 20; i++)
        {
            Call(movement, "FixedUpdate");
            scene.GetPhysicsScene().Simulate(0.02f);
        }
        Require(Quaternion.Angle(body.rotation, Quaternion.identity) > 1f, "Rotation lock prevented steering");
        body.position = new Vector3(0f, 2f, 0f);
        body.linearVelocity = Vector3.down * 5f;
        body.useGravity = true;
        var road = new GameObject("Validation road");
        SceneManager.MoveGameObjectToScene(road, scene);
        road.transform.position = new Vector3(0f, -0.5f, 0f);
        road.AddComponent<BoxCollider>().size = new Vector3(100f, 1f, 100f);
        for (int i = 0; i < 100; i++)
            scene.GetPhysicsScene().Simulate(0.02f);
        Require(float.IsNegativeInfinity(Get<float>(movement, "crashStartedAt")), "Road triggered crash tilt");
        Require(Mathf.Abs(body.position.y - 0.5f) < 0.05f, "Vehicle did not stay grounded");
        UnityEngine.Object.DestroyImmediate(movement.gameObject);
        UnityEngine.Object.DestroyImmediate(road);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Call(PlayerMovement target, string method) => typeof(PlayerMovement).GetMethod(method, Private).Invoke(target, null);
    private static void Set(PlayerMovement target, string field, object value) => typeof(PlayerMovement).GetField(field, Private).SetValue(target, value);
    private static T Get<T>(PlayerMovement target, string field) => (T)typeof(PlayerMovement).GetField(field, Private).GetValue(target);
}
