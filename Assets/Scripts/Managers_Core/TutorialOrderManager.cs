using UnityEngine;
using System;
using System.Collections.Generic;

public class TutorialOrderManager : MonoBehaviour
{
    public static TutorialOrderManager Instance;
    public static event Action OnTutorialTicketCleared;

    [Header("Ticket UI")]
    public GameObject ticketPrefab;
    public Transform ticketContainerParent;
    public float ticketWidth = 250f;
    public float spacingAmount = 20f;

    [HideInInspector]
    public List<OrderTicketUI> activeTickets = new List<OrderTicketUI>();

    private void Awake()
    {
        Instance = this;
    }

    public void SpawnDummyTicket(DishData requiredDish)
    {
        GameObject newTicket = Instantiate(ticketPrefab, ticketContainerParent);
        newTicket.SetActive(true);
        OrderTicketUI ticketScript = newTicket.GetComponent<OrderTicketUI>();

        List<DishData> dishList = new List<DishData>();
        dishList.Add(requiredDish);

        ticketScript.SetupTicket(dishList);
        ticketScript.HidePatienceUI(); // Hides the bar!

        activeTickets.Add(ticketScript);
        UpdateTicketLayout();
    }

    private void UpdateTicketLayout()
    {
        float effectiveSpacing = ticketWidth + spacingAmount;
        for (int i = 0; i < activeTickets.Count; i++)
        {
            float targetX = i * effectiveSpacing;
            Vector2 newPosition = new Vector2(targetX, 0);
            activeTickets[i].SetTargetPosition(newPosition, i);
        }
    }

    public void TryServeTutorialDish(DishData servedDish)
    {
        // Find the oldest ticket that needs this dish
        foreach (OrderTicketUI ticket in activeTickets)
        {
            if (ticket.pendingDishes.Contains(servedDish))
            {
                ticket.MarkDishServed(servedDish);

                if (ticket.IsFullyServed())
                {
                    activeTickets.Remove(ticket);
                    Destroy(ticket.gameObject);
                    UpdateTicketLayout();

                    if (OnTutorialTicketCleared != null)
                    {
                        OnTutorialTicketCleared();
                    }
                }
                return; // Stop checking after serving one ticket
            }
        }
    }
}