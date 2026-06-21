using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI References")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI dialogueText;
    public GameObject continuePromptText; // The "Press E to continue" text

    [Header("Settings")]
    public float typingSpeed = 0.03f;

    private Queue<string> sentences = new Queue<string>();
    private string currentSentence;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private Action onDialogueCompleteCallback;
    public bool IsDialogueActive => dialoguePanel != null && dialoguePanel.activeInHierarchy;

    private void Awake()
    {
        Instance = this;
        dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (!dialoguePanel.activeInHierarchy) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isTyping)
            {
                // Skip typing effect
                StopCoroutine(typingCoroutine);
                dialogueText.text = currentSentence;
                isTyping = false;
                continuePromptText.SetActive(true);
            }
            else
            {
                // Move to next sentence
                DisplayNextSentence();
            }
        }
    }

    public void StartDialogue(string[] lines, Action onComplete = null)
    {
        dialoguePanel.SetActive(true);
        sentences.Clear();
        onDialogueCompleteCallback = onComplete;

        foreach (string line in lines)
        {
            sentences.Enqueue(line);
        }

        DisplayNextSentence();
    }

    private void DisplayNextSentence()
    {
        if (sentences.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentSentence = sentences.Dequeue();
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeSentence(currentSentence));
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        continuePromptText.SetActive(false);
        dialogueText.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        continuePromptText.SetActive(true);
    }

    private void EndDialogue()
    {
        dialoguePanel.SetActive(false);
        onDialogueCompleteCallback?.Invoke();
    }
}