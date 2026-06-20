using UnityEngine;

public class Void : MonoBehaviour
{
  private void OnCollisionEnter2D(Collision2D other)
  {
    if (other.gameObject.CompareTag("Player"))
    {
      Destroy(other.gameObject);
    }
  }
}
