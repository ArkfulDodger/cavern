using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxCave : MonoBehaviour
{
    public Vector3 basePosition;
    Transform cam;

    [SerializeField] Vector3 referencePoint;
    [SerializeField] float minBoundsY;
    [SerializeField] float maxBoundsY;
    [SerializeField] float minBoundsX;
    [SerializeField] float maxBoundsX;

    [SerializeField] float parallaxX;
    [SerializeField] float parallaxY;
    [SerializeField] float parallaxStrengthX = 0.1f;
    [SerializeField] float parallaxStrengthY = 0.1f;

    private void Awake()
    {
        basePosition = transform.position;
        cam = Camera.main.transform;
    }

    // Start is called before the first frame update
    void Start()
    {
        parallaxX = transform.position.z * parallaxStrengthX;
        parallaxY = transform.position.z * parallaxStrengthY;
    }

    // Update is called once per frame
    void Update()
    {
        parallaxX = transform.position.z * parallaxStrengthX;
        parallaxY = transform.position.z * parallaxStrengthY;
        //Debug.Log("NAME: " + gameObject.name);

        float xShift = (cam.position.x - referencePoint.x) * parallaxX;
        // xShift = Mathf.Max(xShift, minBoundsX);
        // xShift = Mathf.Min(xShift, maxBoundsX);

        float yShift = (cam.position.y - referencePoint.y) * parallaxY;
        // yShift = Mathf.Max(yShift, minBoundsY);
        // yShift = Mathf.Min(yShift, maxBoundsY);

        float xPosition = Mathf.Clamp(basePosition.x + xShift, minBoundsX, maxBoundsX);
        float yPosition = Mathf.Clamp(basePosition.y + yShift, minBoundsY, maxBoundsY);

        transform.position = new Vector3(xPosition, yPosition, basePosition.z);

        // Debug.Log("camera: " + cam.position);
        // Debug.Log("reference pt: " + referencePoint);
        //Debug.Log("base: (" + basePosition.x + ", " + basePosition.y + ")");
        //Debug.Log("World Pos of " + gameObject.name + ": (" + transform.position.x + ", " + transform.position.y + ")");
        // Debug.Log("parallaxX: " + parallaxX);
        // Debug.Log("parallaxY: " + parallaxY);
        //Debug.Log("xShift: " + xShift);
        // Debug.Log("yShift: " + yShift);
    }
}
