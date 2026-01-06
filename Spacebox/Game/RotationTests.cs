using Engine;
using OpenTK.Mathematics;
using System;

public static class RotationDebug
{
    public static void Run()
    {
        Debug.Log("=== START ROTATION TESTS ===");

        // Test 1: Yaw (Left/Right) - Should PASS now
        RunSingleTest(new Vector3(0, 90, 0), "Yaw (Y) 90");
        RunSingleTest(new Vector3(0, -90, 0), "Yaw (Y) -90");

        // Test 2: Pitch (Up/Down)
        RunSingleTest(new Vector3(45, 0, 0), "Pitch (X) 45");
        RunSingleTest(new Vector3(-45, 0, 0), "Pitch (X) -45");

        // Test 3: Roll (Tilt)
        RunSingleTest(new Vector3(0, 0, 45), "Roll (Z) 45");

        // Test 4: Combo
        RunSingleTest(new Vector3(30, 90, 0), "Combo (X=30, Y=90)");

        Debug.Log("=== END TESTS ===");
    }

    // Convert: Euler Angles (Degrees) -> Quaternion
    // CHANGED ORDER: Yaw(Y) -> Pitch(X) -> Roll(Z)
    // This allows Y to rotate 360 degrees without locking.
    public static Quaternion EulersToQuaternion(Vector3 eulerDegrees)
    {
        float xRad = MathHelper.DegreesToRadians(eulerDegrees.X);
        float yRad = MathHelper.DegreesToRadians(eulerDegrees.Y);
        float zRad = MathHelper.DegreesToRadians(eulerDegrees.Z);

        Quaternion qx = Quaternion.FromAxisAngle(Vector3.UnitX, xRad);
        Quaternion qy = Quaternion.FromAxisAngle(Vector3.UnitY, yRad);
        Quaternion qz = Quaternion.FromAxisAngle(Vector3.UnitZ, zRad);

        // Order: Y * X * Z
        return qy * qx * qz;
    }

    // Convert: Quaternion -> Euler Angles (Degrees)
    // Updated formulas for Y->X->Z order
    public static Vector3 QuaternionToEulers(Quaternion q)
    {
        q.Normalize();

        // Pitch (X axis)
        // In this order, X is the middle axis (subject to Gimbal Lock at +/- 90)
        float sinp = 2f * (q.W * q.X - q.Y * q.Z);
        float pitch;

        // Handle Gimbal Lock
        if (MathF.Abs(sinp) >= 1f)
            pitch = MathF.CopySign(MathF.PI / 2f, sinp);
        else
            pitch = MathF.Asin(sinp);

        // Yaw (Y axis)
        float siny_cosp = 2f * (q.W * q.Y + q.Z * q.X);
        float cosy_cosp = 1f - 2f * (q.X * q.X + q.Y * q.Y);
        float yaw = MathF.Atan2(siny_cosp, cosy_cosp);

        // Roll (Z axis)
        float sinr_cosp = 2f * (q.W * q.Z + q.X * q.Y);
        float cosr_cosp = 1f - 2f * (q.X * q.X + q.Z * q.Z);
        float roll = MathF.Atan2(sinr_cosp, cosr_cosp);

        return new Vector3(
            MathHelper.RadiansToDegrees(pitch),
            MathHelper.RadiansToDegrees(yaw),
            MathHelper.RadiansToDegrees(roll)
        );
    }

    private static void RunSingleTest(Vector3 inputEuler, string testName)
    {
        Quaternion packed = EulersToQuaternion(inputEuler);
        Vector3 resultEuler = QuaternionToEulers(packed);

        if (IsClose(inputEuler, resultEuler))
        {
            Debug.Log($"[SUCCESS] {testName}: Input {inputEuler} -> Result {resultEuler}");
        }
        else
        {
            Debug.Error($"[FAIL] {testName}: Input {inputEuler} -> Result {resultEuler}");

            if (IsClose(new Vector3(inputEuler.X, inputEuler.Z, inputEuler.Y), resultEuler))
                Debug.Error("   -> Hint: Axes might be swapped.");
        }
    }

    private static bool IsClose(Vector3 a, Vector3 b, float tolerance = 0.5f)
    {
        return MathF.Abs(a.X - b.X) < tolerance &&
               MathF.Abs(a.Y - b.Y) < tolerance &&
               MathF.Abs(a.Z - b.Z) < tolerance;
    }
}