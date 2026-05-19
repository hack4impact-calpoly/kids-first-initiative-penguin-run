using UnityEngine;
using TMPro;

public class DialogueManager: MonoBehaviour
{
  [SerializeField] private GameObject dialogueBox;
  [SerializeField] private TMP_Text dialogueText;

  [TextArea]
  /* For this we can make each new string for each level 
     That way, we just use one script for dialogue
     To access the nexts levels dialogue, we can do
     dialogueMessages[currentLevel][dialogueOption]
  */
  [SerializeField] private string[][] dialogueMessages =
{
    new string[] 
      { 
        "Use the right arrow on your keyboard to go through the tutorial!",
        "Help me get back to my igloo! Build a track of solid pieces and I will slide down it!!", 
        "Drag a track piece from the box at the bottom. Snap pieces together to make a path."
      },

    new string[] { "Level 2 Dialogue." },

    new string[] { "level 3 Dialogue" }
};

  private int currentMessageIndex = 0;

  // Due to 0 indexing, 0 = Level1, 1 = Level2, etc.
  private int currentLevel = 0;

  // We will use this to prevent interaction while dialogue boxes are open
  public static bool IsDialogueOpen { get; private set; }
  private void Start()
  {
    ShowMessage(0);
  }
  private void Update()
  {
    if (dialogueBox.activeSelf && Input.GetKeyDown(KeyCode.RightArrow))
    {
      NextMessage();
    }
  }
  public void ShowMessage(int index)
  {
    if (index < 0 || index >= dialogueMessages[currentLevel].Length)
    {
      HideDialogue();
      return;
    }
    
    IsDialogueOpen = true;
    currentMessageIndex = index;
    dialogueText.text = dialogueMessages[currentLevel][currentMessageIndex];
    dialogueBox.SetActive(true);
  }

  private void NextMessage()
  {
    currentMessageIndex++;
    // If we are done with the messages in this level, hide the tutorial
    if (currentMessageIndex >= dialogueMessages[currentLevel].Length)
    {
      // Reset the message Index for the next level
      currentMessageIndex = 0;
      HideDialogue();
    }
    else
    {
      dialogueText.text = dialogueMessages[currentLevel][currentMessageIndex];
    }
  }

  public void HideDialogue()
  {
    IsDialogueOpen = false;
    dialogueBox.SetActive(false);
  }
}