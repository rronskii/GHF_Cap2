using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class ServiceBell : MonoBehaviour
{
    public static event Action OnTutorialBellRung;

    [Header("Station References")]
    public PlateStation linkedPlateStation;

    [Header("Audio")]
    public AudioClip validDingSound;
    public AudioClip invalidBuzzSound;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnMouseDown()
    {
        if (Time.timeScale == 0f) return;
        if (DialogueManager.Instance != null)
        {
            if (DialogueManager.Instance.IsDialogueActive) return;
        }

        if (linkedPlateStation != null)
        {
            if (linkedPlateStation.isServing) return;
        }

        ProcessOrder();
    }

    private void ProcessOrder()
    {
        if (linkedPlateStation == null) return;

        List<IngredientData> ingredientsOnPlate = linkedPlateStation.GetIngredientsOnPlate();

        // If the plate is completely empty, reject it with a buzz
        if (ingredientsOnPlate.Count == 0)
        {
            PlaySound(invalidBuzzSound);
            return;
        }

        DishData validatedDish = null;

        if (OrderManager.Instance != null)
        {
            validatedDish = OrderManager.Instance.ValidateRecipe(ingredientsOnPlate);
        }
        else if (TutorialOrderManager.Instance != null)
        {
            foreach (OrderTicketUI ticket in TutorialOrderManager.Instance.activeTickets)
            {
                if (ticket.pendingDishes.Count > 0)
                {
                    if (ticket.pendingDishes[0].MatchesIngredients(ingredientsOnPlate))
                    {
                        validatedDish = ticket.pendingDishes[0];
                        break;
                    }
                }
            }
        }

        if (validatedDish == null)
        {
            Debug.Log("[Service Bell] Invalid combination!");
            PlaySound(invalidBuzzSound);
            return;
        }

        bool sentToWindowSuccess = false;

        if (OrderManager.Instance != null)
        {
            sentToWindowSuccess = OrderManager.Instance.TrySpawnDishToWindow(validatedDish);
            if (sentToWindowSuccess == false)
            {
                Debug.Log("[Service Bell] Counter window is full!");
                PlaySound(invalidBuzzSound);
                return;
            }
        }
        else if (TutorialOrderManager.Instance != null)
        {
            TutorialOrderManager.Instance.TryServeTutorialDish(validatedDish);
            sentToWindowSuccess = true;
        }

        if (sentToWindowSuccess)
        {
            PlaySound(validDingSound);

            if (OnTutorialBellRung != null)
            {
                OnTutorialBellRung();
            }

            // Tell the plate station to pack up the physical items and animate!
            linkedPlateStation.ServePlate();
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null)
        {
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}