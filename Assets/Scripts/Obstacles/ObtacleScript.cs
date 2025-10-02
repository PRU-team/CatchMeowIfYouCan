using CatchMeowIfYouCan.Enemies;
using UnityEngine;

public class ObtacleScript : MonoBehaviour
{
    private float leftEdge;
    void Start()
    {
        leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 2f;
    }

    void Update()
    {
        transform.position += Vector3.left * GameManager.Instance.gameSpeed * Time.deltaTime;

        if (transform.position.x < leftEdge)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("[Obstacle] Player touched obstacle → trigger catcher");

            // Kiểm tra có thể spawn catcher không
            CatcherManager catcherManager = FindObjectOfType<CatcherManager>();
            if (catcherManager != null && catcherManager.CanTriggerNewCatcher())
            {
                // Lấy ngẫu nhiên 1 catcher và kích hoạt
                catcherManager.DebugTriggerRandomCatcher();
            }

            Destroy(gameObject); // Xoá chướng ngại vật sau khi chạm
        }
    }
}
