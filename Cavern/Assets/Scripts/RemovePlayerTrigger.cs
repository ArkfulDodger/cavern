using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemovePlayerTrigger : MonoBehaviour
{
    bool active = true;
    [SerializeField] float vanishTime = 6f;
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Debug.Log(gameObject.name + "triggered");
        if (active && other.tag == "Player" && other is CircleCollider2D)
        {
            active = false;
            EventBroker.ExitPlayerBeginCall();
            StartCoroutine(MovePlayerOffToSide(other.transform));
        }
    }

    IEnumerator MovePlayerOffToSide(Transform player)
    {
        yield return null;

        Vector3 movementX = player.position.x > transform.position.x ? Vector3.right : Vector3.left;

        Vector3 startPosition = player.position;
        Vector3 endPosition = startPosition + movementX;

        float timer = 0f;

        while (timer < vanishTime)
        {
            player.position = Vector3.Lerp(startPosition, endPosition, timer/vanishTime);

            timer += Time.deltaTime;
            yield return null;
        }
        player.position = endPosition;
    }
}
