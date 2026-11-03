
using UnityEngine;

public class TeleportValidatorTest : MonoBehaviour
{
    [SerializeField]
    private TeleportDestinationValidator validator;

    [ContextMenu("Run Teleport Tests")]
    public void RunTests()
    {
        if (validator == null)
        {
            Debug.LogError("Validator reference missing.");
            return;
        }

        Test("Open floor", new Vector3(0, 0, -4), true);
        Test("Outside arena", new Vector3(25, 0, 0), false);
        Test("Beyond north corner", new Vector3(0, 0, 19), false);
        Test("Inside fountain", new Vector3(0, 0, 0), false);
    }

    private void Test(
        string name,
        Vector3 position,
        bool expected)
    {
        bool actual = validator.IsValidDestination(position);

        if (actual == expected)
        {
            Debug.Log("PASS: " + name);
        }
        else
        {
            Debug.LogError(
                "FAIL: " + name +
                " | Expected: " + expected +
                " | Actual: " + actual
            );
        }
    }
}
