using UnityEngine;

public class TestMyStack : MonoBehaviour
{
    public StackManager stackManager;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // Example input to start "Test My Stack" mode
        {
            EnablePhysicsOnGlassBlocks();
        }
    }

    void EnablePhysicsOnGlassBlocks()
    {
        foreach (Transform stack in stackManager.stackPositions)
        {
            foreach (Transform block in stack)
            {
                if (block.CompareTag("Glass")) // Ensure you tag Glass blocks appropriately
                {
                    Rigidbody rb = block.gameObject.AddComponent<Rigidbody>();
                    rb.mass = 1; // Set appropriate mass
                }
            }
        }
    }
}
