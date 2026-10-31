using UnityEngine;
using System.Collections.Generic;

public class Room : MonoBehaviour
{
    public bool wasRecentlyVisitedByPlayer = false;
    public List<HidingSpot> hidingSpotsInRoom = new List<HidingSpot>();
    private float visitTimer = 0f;

    [Header("AI Patrol Behavior")]
    [Tooltip("Отметьте этот флажок, если это большая зона (например, коридор), в которой монстр должен задержаться и попатрулировать, прежде чем уходить.")]
    public bool isLargePatrolArea = false;

    void Start()
    {
        // Находим все укрытия в комнате
        FindHidingSpotsInRoom();
    }

    void FindHidingSpotsInRoom()
    {
        Collider roomCollider = GetComponent<Collider>();
        if (roomCollider == null) return;

        HidingSpot[] allHidingSpots = FindObjectsByType<HidingSpot>(FindObjectsSortMode.None);
        foreach (HidingSpot spot in allHidingSpots)
        {
            if (roomCollider.bounds.Contains(spot.transform.position))
            {
                hidingSpotsInRoom.Add(spot);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            wasRecentlyVisitedByPlayer = true;
            visitTimer = 30f;
        }
    }

    private void Update()
    {
        if (wasRecentlyVisitedByPlayer)
        {
            visitTimer -= Time.deltaTime;
            if (visitTimer <= 0f)
            {
                wasRecentlyVisitedByPlayer = false;
            }
        }
    }
}