using UnityEngine;

public class CameraShiftByDistance2D : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Targeting")]
    public string targetTag = "CameraShift"; // All objects with this tag influence the camera

    [Header("Camera Offset")]
    public Vector3 baseOffset = new Vector3(0f, 0f, -10f);
    public float maxShift = 3f;            // Max horizontal shift magnitude
    public float influenceDistance = 6f;   // Distance at which influence starts

    [Header("Smoothing")]
    public float smoothTime = 0.2f;

    private Vector3 velocity;

    void LateUpdate()
    {
        if (player == null) return;

        GameObject[] targets = GameObject.FindGameObjectsWithTag(targetTag);

        // Sum signed influences from every tagged object.
        // Each object pushes the camera away from its own side:
        //   object to the right => negative contribution (camera shifts left)
        //   object to the left  => positive contribution (camera shifts right)
        float totalInfluence = 0f;

        for (int i = 0; i < targets.Length; i++)
        {
            Vector3 targetPos = targets[i].transform.position;
            float dist = Vector2.Distance(player.position, targetPos);
            float magnitude = Mathf.Clamp01(1f - (dist / influenceDistance));

            // SmoothStep easing per object
            magnitude = magnitude * magnitude * (3f - 2f * magnitude);

            // Direction away from the object (positive = camera right, negative = camera left)
            float direction = Mathf.Sign(player.position.x - targetPos.x);

            totalInfluence += magnitude * direction;
        }

        // Clamp so multiple objects on the same side don't exceed the max shift
        totalInfluence = Mathf.Clamp(totalInfluence, -1f, 1f);

        Vector3 desiredPos = player.position + baseOffset + Vector3.right * (maxShift * totalInfluence);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothTime);
    }
}
